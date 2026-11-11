using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Application.Cadastros;

/// <summary>
/// Regras de inclusão e transferência de dependentes, compartilhadas pela administração (Web) e pelo
/// portal do titular (app). Não grava: o chamador chama SaveChanges.
/// </summary>
public sealed class GestaoDependentes(IAppDbContext db, IUsuarioAtual atual, ITokenService tokens, TimeProvider relogio)
{
    private static readonly SituacaoCadastral[] SituacoesVigentes =
        [SituacaoCadastral.Pendente, SituacaoCadastral.Ativo, SituacaoCadastral.Suspenso];

    public DateOnly Hoje => DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);

    public async Task<ConfiguracaoDependentes> ConfiguracaoAsync(CancellationToken ct) =>
        await db.ConfiguracoesDependentes.FirstOrDefaultAsync(c => c.ClubeId == atual.ClubeIdObrigatorio, ct)
        ?? throw new NaoEncontradoException("Configuração de dependentes");

    public async Task<Associado> ObterTitularAsync(Guid associadoId, CancellationToken ct)
    {
        var titular = await db.Associados.Include(a => a.Categoria).Include(a => a.Pessoa)
                          .FirstOrDefaultAsync(a => a.Id == associadoId && a.ClubeId == atual.ClubeIdObrigatorio, ct)
                      ?? throw new NaoEncontradoException("Associado titular");
        if (titular.Situacao is not (SituacaoCadastral.Ativo or SituacaoCadastral.Pendente))
            throw new DomainException("dependente.titular_nao_ativo",
                "Só é possível incluir dependentes para titulares ativos ou com cadastro pendente.");
        return titular;
    }

    /// <summary>
    /// Inclui um dependente. <c>podeAutorizarExcecao</c>: usuário pode aprovar dependentes (exceção de múltiplos titulares).
    /// <c>aprovacaoDireta</c>: inclusão feita por quem pode aprovar, dispensando a etapa de aprovação.
    /// </summary>
    public async Task<Dependente> IncluirAsync(Associado titular, DadosPessoaisRequest dados, Parentesco parentesco,
        string? parentescoDescricao, string? justificativaExcecao, bool podeAutorizarExcecao, bool aprovacaoDireta,
        CancellationToken ct)
    {
        var cfg = await ConfiguracaoAsync(ct);
        var (vigentes, conjuges) = await ContarAsync(titular.Id, null, ct);
        RegrasDependentes.ValidarInclusao(cfg, titular.Categoria, parentesco, dados.DataNascimento, Hoje, vigentes, conjuges);

        var pessoa = await ObterOuCriarPessoaAsync(dados, titular, justificativaExcecao, podeAutorizarExcecao, ct);
        var exigeAprovacao = cfg.ExigeDocumentoComprobatorio || (!aprovacaoDireta && await ClubeExigeAprovacaoAsync(ct));
        var numero = await ProximoNumeroAsync(titular, ct);
        var agora = relogio.GetUtcNow();

        var dependente = new Dependente(titular.ClubeId, pessoa, titular, parentesco, parentescoDescricao, numero,
            RegrasDependentes.PermissoesIniciais(cfg, dados.DataNascimento, Hoje), exigeAprovacao, atual.UsuarioId, agora);
        if (!string.IsNullOrWhiteSpace(justificativaExcecao) && await PessoaJaDependenteVigenteAsync(pessoa.Id, ct))
            dependente.AutorizarExcecaoMultiplosTitulares(justificativaExcecao, atual.UsuarioIdObrigatorio);

        db.Dependentes.Add(dependente);
        Historico(dependente.Id, "Inclusao",
            $"Incluído como {parentesco} de {titular.Pessoa.NomeCompleto} ({titular.Matricula}). Situação: {dependente.Situacao}.");
        return dependente;
    }

    public async Task TransferirAsync(Dependente dependente, Associado novoTitular, Parentesco parentesco, string? descricao,
        string motivo, CancellationToken ct)
    {
        var cfg = await ConfiguracaoAsync(ct);
        var (vigentes, conjuges) = await ContarAsync(novoTitular.Id, dependente.Id, ct);
        RegrasDependentes.ValidarInclusao(cfg, novoTitular.Categoria, parentesco, dependente.Pessoa.DataNascimento, Hoje,
            vigentes, conjuges);
        if (await db.Dependentes.AnyAsync(d => d.PessoaId == dependente.PessoaId && d.AssociadoTitularId == novoTitular.Id
                                               && d.Id != dependente.Id && SituacoesVigentes.Contains(d.Situacao), ct))
            throw new ConflitoException("dependente.ja_vinculado", "Esta pessoa já é dependente do novo titular.");

        var anterior = dependente.AssociadoTitularId;
        dependente.DefinirParentesco(parentesco, descricao);
        dependente.TransferirTitular(novoTitular, await ProximoNumeroAsync(novoTitular, ct), motivo, atual.UsuarioId,
            relogio.GetUtcNow());
        Historico(dependente.Id, "Transferencia",
            $"Titularidade transferida de {anterior} para {novoTitular.Pessoa.NomeCompleto} ({novoTitular.Matricula}). Motivo: {motivo}");
    }

    public void Historico(Guid dependenteId, string evento, string descricao) =>
        db.HistoricoDependentes.Add(new HistoricoDependente(atual.ClubeIdObrigatorio, dependenteId, evento, descricao,
            atual.UsuarioId, relogio.GetUtcNow()));

    private async Task<(int Vigentes, int Conjuges)> ContarAsync(Guid titularId, Guid? ignorar, CancellationToken ct)
    {
        var lista = await db.Dependentes
            .Where(d => d.AssociadoTitularId == titularId && d.Id != ignorar && SituacoesVigentes.Contains(d.Situacao))
            .Select(d => d.Parentesco).ToListAsync(ct);
        return (lista.Count, lista.Count(p => p == Parentesco.Conjuge));
    }

    private async Task<Pessoa> ObterOuCriarPessoaAsync(DadosPessoaisRequest dados, Associado titular,
        string? justificativaExcecao, bool podeAutorizarExcecao, CancellationToken ct)
    {
        var clubeId = titular.ClubeId;
        Pessoa? pessoa = null;
        if (!string.IsNullOrWhiteSpace(dados.Cpf))
        {
            var cpf = Documentos.SomenteDigitos(dados.Cpf);
            pessoa = await db.Pessoas.FirstOrDefaultAsync(p => p.ClubeId == clubeId && p.Cpf == cpf, ct);
        }

        if (pessoa is not null)
        {
            if (pessoa.Id == titular.PessoaId)
                throw new DomainException("dependente.titular_como_dependente", "O titular não pode ser dependente de si mesmo.");
            if (await db.Associados.AnyAsync(a => a.PessoaId == pessoa.Id && SituacoesVigentes.Contains(a.Situacao), ct))
                throw new ConflitoException("dependente.pessoa_associada",
                    "Esta pessoa é associada titular do clube e não pode ser incluída como dependente.");
            if (await db.Dependentes.AnyAsync(d => d.PessoaId == pessoa.Id && d.AssociadoTitularId == titular.Id
                                                  && SituacoesVigentes.Contains(d.Situacao), ct))
                throw new ConflitoException("dependente.ja_vinculado", "Esta pessoa já é dependente deste titular.");
            if (await PessoaJaDependenteVigenteAsync(pessoa.Id, ct))
            {
                if (string.IsNullOrWhiteSpace(justificativaExcecao))
                    throw new ConflitoException("dependente.outro_titular",
                        "Esta pessoa já é dependente de outro titular. A administração pode autorizar a exceção informando uma justificativa.");
                if (!podeAutorizarExcecao)
                    throw new AcessoNegadoException("Somente a administração pode autorizar dependente com mais de um titular.");
            }
            // Pessoa já conhecida (ex.: ex-dependente): reaproveita o registro e atualiza os dados.
            PessoaMapeamento.Aplicar(pessoa, dados);
            return pessoa;
        }

        pessoa = new Pessoa(clubeId, dados.NomeCompleto, tokens.GerarTokenOpaco(), relogio.GetUtcNow());
        PessoaMapeamento.Aplicar(pessoa, dados);
        db.Pessoas.Add(pessoa);
        return pessoa;
    }

    private Task<bool> PessoaJaDependenteVigenteAsync(Guid pessoaId, CancellationToken ct) =>
        db.Dependentes.AnyAsync(d => d.PessoaId == pessoaId && SituacoesVigentes.Contains(d.Situacao), ct);

    private async Task<bool> ClubeExigeAprovacaoAsync(CancellationToken ct) =>
        await db.ConfiguracoesClube.Where(c => c.ClubeId == atual.ClubeIdObrigatorio)
            .Select(c => c.ExigeAprovacaoDependentes).FirstOrDefaultAsync(ct);

    private async Task<string> ProximoNumeroAsync(Associado titular, CancellationToken ct)
    {
        // Sequencial pelos vínculos já abertos para o titular (inclui ex-dependentes): números nunca são reutilizados.
        // Em inclusões simultâneas, o índice único de NumeroIdentificacao rejeita a segunda (HTTP 409).
        var usados = await db.VinculosDependentes.IgnoreQueryFilters(FiltrosConsulta.SomenteTenant)
            .CountAsync(v => v.AssociadoTitularId == titular.Id, ct);
        return $"{titular.Matricula}-{usados + 1:00}";
    }
}
