using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Identidade;

namespace ProjetoClubes.Application.Privacidade;

public sealed record AnonimizarRequest(string Motivo);

public sealed record AnonimizacaoDto(Guid PessoaId, string NovoNome, DateTimeOffset AnonimizadaEm, int DocumentosRemovidos, bool LoginDesativado);

public interface IPrivacidadeService
{
    /// <summary>Portal: todos os dados que o clube guarda sobre o próprio usuário (LGPD, art. 18, II).</summary>
    Task<object> MeusDadosAsync(CancellationToken ct);

    /// <summary>Administração: os mesmos dados de uma pessoa do clube (atendimento a pedido do titular).</summary>
    Task<object> DadosDaPessoaAsync(Guid pessoaId, CancellationToken ct);

    /// <summary>Apaga os dados pessoais de quem já não tem vínculo vigente com o clube (LGPD, art. 18, VI).</summary>
    Task<AnonimizacaoDto> AnonimizarAsync(Guid pessoaId, AnonimizarRequest r, CancellationToken ct);
}

/// <summary>
/// Direitos do titular dos dados (LGPD): acesso/portabilidade (exportação em JSON) e eliminação (anonimização).
/// O histórico financeiro e de participação continua existindo, mas sem identificar a pessoa.
/// </summary>
public sealed class PrivacidadeService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IAcessoPessoas acessoPessoas,
    ITransacoes transacoes,
    IArmazenamentoArquivos armazenamento,
    ITokenService tokens,
    TimeProvider relogio,
    ILogger<PrivacidadeService> log) : IPrivacidadeService
{
    private Guid ClubeId => atual.ClubeIdObrigatorio;

    public async Task<object> MeusDadosAsync(CancellationToken ct)
    {
        var portal = await acessoPessoas.ObterPortalAsync(ct);
        return await MontarAsync(portal.Pessoa.Id, ct);
    }

    public async Task<object> DadosDaPessoaAsync(Guid pessoaId, CancellationToken ct)
    {
        await acessoPessoas.ExigirAsync(pessoaId, AcaoPessoa.VerDados, ct);
        return await MontarAsync(pessoaId, ct);
    }

    public async Task<AnonimizacaoDto> AnonimizarAsync(Guid pessoaId, AnonimizarRequest r, CancellationToken ct)
    {
        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        if (!minhas.Contains(Permissoes.Associados.Excluir) && !minhas.Contains(Permissoes.Dependentes.Excluir))
            throw new AcessoNegadoException();
        if (string.IsNullOrWhiteSpace(r.Motivo) || r.Motivo.Trim().Length < 5)
            throw new DomainException("lgpd.motivo", "Informe o motivo (ex.: pedido do titular em 10/11/2026).");

        var arquivosParaApagar = new List<string>();
        var resultado = await transacoes.ComBloqueioAsync($"pessoa:{pessoaId}", async () =>
        {
            var pessoa = await db.Pessoas.FirstOrDefaultAsync(p => p.Id == pessoaId && p.ClubeId == ClubeId, ct)
                         ?? throw new NaoEncontradoException("Pessoa");

            // Só quem não tem mais vínculo vigente (ativo, pendente ou suspenso) pode ter os dados apagados.
            var vigentes = new[] { SituacaoCadastral.Ativo, SituacaoCadastral.Pendente, SituacaoCadastral.Suspenso };
            if (await db.Associados.AnyAsync(a => a.PessoaId == pessoaId && vigentes.Contains(a.Situacao), ct)
                || await db.Dependentes.AnyAsync(d => d.PessoaId == pessoaId && vigentes.Contains(d.Situacao), ct))
                throw new DomainException("lgpd.vinculo_vigente",
                    "Inative o associado/dependente antes de apagar os dados pessoais. O histórico financeiro é mantido.");
            var associadoIds = await db.Associados.Where(a => a.PessoaId == pessoaId).Select(a => a.Id).ToListAsync(ct);
            if (await db.Dependentes.AnyAsync(d => associadoIds.Contains(d.AssociadoTitularId) && vigentes.Contains(d.Situacao), ct))
                throw new DomainException("lgpd.dependentes_vigentes", "Há dependentes vigentes ligados a este titular.");

            var agora = relogio.GetUtcNow();
            var usuarioId = pessoa.UsuarioId;

            // Documentos e foto: apaga o conteúdo e o nome original.
            var documentos = await db.DocumentosPessoais.Include(d => d.Arquivo).Where(d => d.PessoaId == pessoaId).ToListAsync(ct);
            var arquivoIds = documentos.Select(d => d.ArquivoId).ToList();
            if (pessoa.FotoArquivoId is { } foto) arquivoIds.Add(foto);
            var arquivos = await db.Arquivos.Where(a => arquivoIds.Contains(a.Id)).ToListAsync(ct);
            foreach (var a in arquivos)
            {
                arquivosParaApagar.Add(a.Chave);
                a.Descartar();
            }
            foreach (var d in documentos) db.DocumentosPessoais.Remove(d); // exclusão lógica pelo interceptor

            pessoa.Anonimizar(tokens.GerarTokenOpaco(), agora);

            // Login: sai deste clube; se não tiver acesso a nenhum outro, a conta inteira é anonimizada e bloqueada.
            var loginDesativado = false;
            if (usuarioId is { } u)
            {
                var vinculos = await db.UsuarioClubes.IgnoreQueryFilters().Where(v => v.UsuarioId == u).ToListAsync(ct);
                foreach (var v in vinculos.Where(v => v.ClubeId == ClubeId)) v.Inativar();
                var sessoes = await db.SessoesUsuarios.Where(s => s.UsuarioId == u && s.RevogadaEm == null).ToListAsync(ct);
                var usuario = await db.Usuarios.FirstAsync(x => x.Id == u, ct);
                if (!vinculos.Any(v => v.ClubeId != ClubeId && v.Ativo) && !usuario.SuperAdmin)
                {
                    usuario.Nome = "Usuário anonimizado";
                    usuario.DefinirEmail($"anonimizado-{u:N}@anonimizado.invalid");
                    usuario.Bloquear();
                    usuario.RenovarCarimboSeguranca();
                    foreach (var s in sessoes) s.Revogar(agora, "Dados anonimizados (LGPD)");
                    loginDesativado = true;
                }
                else foreach (var s in sessoes.Where(s => s.ClubeId == ClubeId)) s.Revogar(agora, "Dados anonimizados (LGPD)");
            }
            await db.SaveChangesAsync(ct);

            // A trilha de auditoria guarda valores antigos (diff em JSON): o conteúdo sai, o registro de que houve a operação fica.
            var ids = new List<string> { pessoaId.ToString() };
            ids.AddRange(documentos.Select(d => d.Id.ToString()));
            ids.AddRange(arquivos.Select(a => a.Id.ToString()));
            if (loginDesativado && usuarioId is { } uid) ids.Add(uid.ToString());
            await db.LogsAuditoria.Where(l => ids.Contains(l.EntidadeId))
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Alteracoes, (string?)null), ct);
            if (loginDesativado && usuarioId is { } uid2)
                await db.HistoricoAcessos.Where(h => h.UsuarioId == uid2)
                    .ExecuteUpdateAsync(s => s.SetProperty(h => h.EmailInformado, (string?)null), ct);

            return new AnonimizacaoDto(pessoaId, pessoa.NomeCompleto, agora, documentos.Count, loginDesativado);
        }, ct);

        // Arquivos físicos só depois do commit (se o banco falhar, nada some do disco).
        foreach (var chave in arquivosParaApagar)
        {
            try { await armazenamento.RemoverAsync(chave, ct); }
            catch (Exception e) { log.LogWarning(e, "Arquivo de pessoa anonimizada não pôde ser removido do armazenamento"); }
        }
        log.LogInformation("Pessoa {PessoaId} anonimizada por {UsuarioId}. Motivo: {Motivo}", pessoaId, atual.UsuarioId, r.Motivo.Trim());
        return resultado;
    }

    // ---------------------------------------------------------------- exportação

    private async Task<object> MontarAsync(Guid pessoaId, CancellationToken ct)
    {
        var p = await db.Pessoas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pessoaId && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Pessoa");
        var clube = await db.Clubes.Where(c => c.Id == ClubeId).Select(c => c.Nome).FirstAsync(ct);
        var associados = await db.Associados.AsNoTracking().Where(a => a.PessoaId == pessoaId)
            .Select(a => new { a.Id, a.Matricula, Categoria = a.Categoria.Nome, a.DataAssociacao, a.Situacao, a.MotivoSituacao }).ToListAsync(ct);
        var associadoIds = associados.Select(a => a.Id).ToList();
        var dependencias = await db.Dependentes.AsNoTracking().Where(d => d.PessoaId == pessoaId)
            .Select(d => new { d.NumeroIdentificacao, d.Parentesco, d.Situacao, Titular = d.AssociadoTitular.Pessoa.NomeCompleto }).ToListAsync(ct);
        var meusDependentes = await db.Dependentes.AsNoTracking().Where(d => associadoIds.Contains(d.AssociadoTitularId))
            .Select(d => new { d.Pessoa.NomeCompleto, d.Parentesco, d.Situacao }).ToListAsync(ct);
        var mensalidades = await db.Mensalidades.AsNoTracking().Where(m => associadoIds.Contains(m.AssociadoId)).OrderBy(m => m.Competencia)
            .Select(m => new
            {
                m.Competencia, m.Vencimento, m.Situacao, Valor = m.ValorPlano + m.ValorDependentes,
                Pagamentos = db.Pagamentos.Where(x => x.MensalidadeId == m.Id)
                    .Select(x => new { x.NumeroRecibo, x.Data, x.Valor, x.Encargos, x.Forma, x.Estornado }).ToList(),
            }).ToListAsync(ct);
        var inscricoes = await (from i in db.InscricoesEventos
                                join e in db.Eventos on i.EventoId equals e.Id
                                where i.PessoaId == pessoaId
                                select new { Evento = e.Nome, e.Inicio, i.Acompanhantes, i.Situacao, i.PresencaEm }).ToListAsync(ct);
        var reservas = await db.Reservas.AsNoTracking().Where(r => r.PessoaId == pessoaId)
            .Select(r => new { Espaco = r.Espaco.Nome, r.Inicio, r.Fim, r.Finalidade, r.Situacao, r.Motivo }).ToListAsync(ct);
        var solicitacoes = await db.Solicitacoes.AsNoTracking().Where(s => s.PessoaId == pessoaId)
            .Select(s => new
            {
                s.Protocolo, s.Tipo, s.Assunto, s.Descricao, s.Situacao, AbertaEm = s.CriadoEm,
                Respostas = s.Respostas.Where(x => !x.Interna).OrderBy(x => x.CriadaEm)
                    .Select(x => new { x.Texto, x.DaAdministracao, x.CriadaEm }).ToList(),
            }).ToListAsync(ct);
        var acessos = await db.AcessosClube.AsNoTracking().Where(a => a.PessoaId == pessoaId).OrderByDescending(a => a.OcorridoEm).Take(500)
            .Select(a => new { a.OcorridoEm, a.Tipo, a.Autorizado, a.Motivo }).ToListAsync(ct);
        var documentos = await db.DocumentosPessoais.AsNoTracking().Where(d => d.PessoaId == pessoaId)
            .Select(d => new { d.Tipo, d.Descricao, d.Situacao, EnviadoEm = d.CriadoEm }).ToListAsync(ct);
        var leituras = await (from l in db.LeiturasComunicados
                              join c in db.Comunicados on l.ComunicadoId equals c.Id
                              where l.PessoaId == pessoaId
                              select new { Comunicado = c.Titulo, l.LidoEm, l.ConfirmadoEm }).ToListAsync(ct);
        var presencas = await (from pr in db.PresencasAssembleias
                               join a in db.Assembleias on pr.AssembleiaId equals a.Id
                               where associadoIds.Contains(pr.AssociadoId)
                               select new { Assembleia = a.Titulo, a.DataHora, pr.AptoVotar, pr.MotivoInapto }).ToListAsync(ct);
        object? login = null;
        if (p.UsuarioId is { } u)
        {
            var usuario = await db.Usuarios.AsNoTracking().Where(x => x.Id == u).Select(x => new { x.Email, x.UltimoAcessoEm, x.SenhaAlteradaEm }).FirstOrDefaultAsync(ct);
            var historico = await db.HistoricoAcessos.AsNoTracking().Where(h => h.UsuarioId == u).OrderByDescending(h => h.OcorridoEm).Take(100)
                .Select(h => new { h.OcorridoEm, h.Evento, h.Sucesso, h.Ip }).ToListAsync(ct);
            login = new { usuario?.Email, usuario?.UltimoAcessoEm, usuario?.SenhaAlteradaEm, UltimosAcessos = historico };
        }

        return new
        {
            GeradoEm = relogio.GetUtcNow(),
            Clube = clube,
            Observacao = "Dados pessoais mantidos pelo clube sobre você. Votos em assembleias são secretos e não constam aqui.",
            Pessoa = new
            {
                p.NomeCompleto, p.Cpf, p.Rg, p.DataNascimento, p.Sexo, p.EstadoCivil, p.Telefone, p.Email, p.Endereco,
                PossuiFoto = p.FotoArquivoId is not null, CadastradaEm = p.CriadoEm, p.AnonimizadaEm,
            },
            Associado = associados,
            DependenteDe = dependencias,
            MeusDependentes = meusDependentes,
            Mensalidades = mensalidades,
            InscricoesEmEventos = inscricoes,
            Reservas = reservas,
            Solicitacoes = solicitacoes,
            EntradasESaidasDoClube = acessos,
            Documentos = documentos,
            ComunicadosLidos = leituras,
            PresencasEmAssembleias = presencas,
            Login = login,
        };
    }
}
