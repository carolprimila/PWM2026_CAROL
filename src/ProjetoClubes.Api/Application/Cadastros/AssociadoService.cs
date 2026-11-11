using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Application.Usuarios;
using ProjetoClubes.Domain.Associados;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Application.Cadastros;

public interface IAssociadoService
{
    Task<Pagina<AssociadoListaDto>> ListarAsync(ConsultaAssociados consulta, CancellationToken ct);
    Task<AssociadoDetalheDto> ObterAsync(Guid id, CancellationToken ct);
    Task<AssociadoDetalheDto> CriarAsync(CriarAssociadoRequest request, CancellationToken ct);
    Task<AssociadoDetalheDto> AtualizarAsync(Guid id, AtualizarAssociadoRequest request, CancellationToken ct);
    Task<AssociadoDetalheDto> AlterarSituacaoAsync(Guid id, AcaoSituacao acao, string? motivo, CancellationToken ct);
    Task ExcluirAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<HistoricoCadastroDto>> HistoricoAsync(Guid id, CancellationToken ct);
    Task<CriarAcessoAppResponse> CriarAcessoAppAsync(Guid id, CriarAcessoAppRequest request, CancellationToken ct);
    Task<ResumoCadastrosDto> ResumoAsync(CancellationToken ct);
}

public enum AcaoSituacao
{
    Aprovar,
    Rejeitar,
    Suspender,
    Reativar,
    Inativar,
}

public sealed class AssociadoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    IGeradorSequencia sequencias,
    ITokenService tokens,
    IProvisionamentoUsuarios provisionamento,
    TimeProvider relogio,
    IValidator<CriarAssociadoRequest> criarValidator,
    IValidator<AtualizarAssociadoRequest> atualizarValidator,
    IValidator<CriarAcessoAppRequest> acessoValidator) : IAssociadoService
{
    public const string SequenciaMatricula = "matricula";

    private static readonly SituacaoCadastral[] Vigentes =
        [SituacaoCadastral.Pendente, SituacaoCadastral.Ativo, SituacaoCadastral.Suspenso];

    private static readonly Dictionary<string, Expression<Func<Associado, object?>>> Ordenacoes = new()
    {
        ["nome"] = a => a.Pessoa.NomeCompleto,
        ["matricula"] = a => a.Matricula,
        ["dataassociacao"] = a => a.DataAssociacao,
        ["situacao"] = a => a.Situacao,
        ["criadoem"] = a => a.CriadoEm,
    };

    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();
    private DateOnly Hoje => DateOnly.FromDateTime(Agora.UtcDateTime);

    private IQueryable<Associado> DoClube() => db.Associados.Where(a => a.ClubeId == ClubeId);

    public async Task<Pagina<AssociadoListaDto>> ListarAsync(ConsultaAssociados c, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Visualizar, ct);
        var q = DoClube().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(c.Busca))
        {
            var termo = c.Busca.Trim().ToLower();
            var digitos = Documentos.SomenteDigitos(c.Busca);
            q = q.Where(a => a.Pessoa.NomeCompleto.ToLower().Contains(termo) || a.Matricula.ToLower().Contains(termo)
                             || (digitos.Length >= 3 && a.Pessoa.Cpf != null && a.Pessoa.Cpf.Contains(digitos)));
        }
        if (c.Situacao is { } s) q = q.Where(a => a.Situacao == s);
        if (c.CategoriaId is { } cat) q = q.Where(a => a.CategoriaId == cat);
        if (c.InteresseId is { } i) q = q.Where(a => a.Interesses.Any(x => x.InteresseId == i));

        var pagina = await q.Ordenar(c, Ordenacoes, "nome")
            .Select(a => new
            {
                a.Id, a.PessoaId, a.Matricula, a.Pessoa.NomeCompleto, a.Pessoa.Cpf, a.CategoriaId, Categoria = a.Categoria.Nome,
                a.Situacao, a.DataAssociacao, TemFoto = a.Pessoa.FotoArquivoId != null,
                TotalDependentes = a.Dependentes.Count(d => Vigentes.Contains(d.Situacao)),
            })
            .PaginarAsync(c, ct);

        return new Pagina<AssociadoListaDto>(pagina.Itens.Select(a => new AssociadoListaDto(a.Id, a.PessoaId, a.Matricula,
                a.NomeCompleto, Pessoa.MascararCpf(a.Cpf), a.CategoriaId, a.Categoria, a.Situacao, a.DataAssociacao,
                a.TotalDependentes, a.TemFoto)).ToList(),
            pagina.PaginaAtual, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<AssociadoDetalheDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Visualizar, ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task<AssociadoDetalheDto> CriarAsync(CriarAssociadoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Cadastrar, ct);
        await criarValidator.ValidarAsync(r, ct);
        await ValidarCategoriaAsync(r.CategoriaId, ct);
        var interesses = await ValidarInteressesAsync(r.InteresseIds, ct);

        var pessoa = await ObterOuCriarPessoaAsync(r.Pessoa, ct);
        var matricula = await DefinirMatriculaAsync(r.Matricula, ct);
        var exigeAprovacao = await db.ConfiguracoesClube.Where(x => x.ClubeId == ClubeId)
            .Select(x => x.ExigeAprovacaoAssociados).FirstAsync(ct);
        // Quem tem permissão de aprovar não precisa aprovar o próprio cadastro em uma segunda etapa.
        if (exigeAprovacao && await permissoes.PossuiAsync(Permissoes.Associados.Aprovar, ct)) exigeAprovacao = false;

        var associado = new Associado(ClubeId, pessoa, matricula, r.CategoriaId, r.DataAssociacao ?? Hoje, exigeAprovacao, Agora);
        associado.DefinirInteresses(interesses);
        db.Associados.Add(associado);
        Historico(associado.Id, "Cadastro", $"Cadastro realizado. Matrícula {matricula}. Situação: {associado.Situacao}.");
        await db.SaveChangesAsync(ct);
        return await MontarDetalheAsync(associado.Id, ct);
    }

    public async Task<AssociadoDetalheDto> AtualizarAsync(Guid id, AtualizarAssociadoRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Editar, ct);
        await atualizarValidator.ValidarAsync(r, ct);
        var a = await DoClube().Include(x => x.Pessoa).Include(x => x.Interesses).Include(x => x.Categoria)
                    .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NaoEncontradoException("Associado");

        await GarantirCpfLivreAsync(r.Pessoa.Cpf, a.PessoaId, ct);
        if (a.CategoriaId != r.CategoriaId)
        {
            var nova = await ValidarCategoriaAsync(r.CategoriaId, ct);
            Historico(a.Id, "Categoria", $"Categoria alterada de \"{a.Categoria.Nome}\" para \"{nova.Nome}\".");
            a.AlterarCategoria(r.CategoriaId);
        }
        PessoaMapeamento.Aplicar(a.Pessoa, r.Pessoa);
        a.DataAssociacao = r.DataAssociacao;
        a.DefinirInteresses(await ValidarInteressesAsync(r.InteresseIds, ct));
        Historico(a.Id, "Edicao", "Dados cadastrais atualizados.");
        await db.SaveChangesAsync(ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task<AssociadoDetalheDto> AlterarSituacaoAsync(Guid id, AcaoSituacao acao, string? motivo, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Aprovar, ct);
        var a = await DoClube().Include(x => x.Dependentes).FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException("Associado");
        var m = motivo ?? string.Empty;

        switch (acao)
        {
            case AcaoSituacao.Aprovar: a.Aprovar(Agora); break;
            case AcaoSituacao.Rejeitar: a.Rejeitar(m, Agora); break;
            case AcaoSituacao.Suspender: a.Suspender(m, Agora); break;
            case AcaoSituacao.Reativar: a.Reativar(Agora); break;
            case AcaoSituacao.Inativar: a.Inativar(m, Agora); break;
        }
        Historico(a.Id, acao.ToString(), string.IsNullOrWhiteSpace(motivo) ? $"Situação: {a.Situacao}." : $"Situação: {a.Situacao}. Motivo: {motivo}");

        // Ao inativar ou rejeitar o titular, os dependentes ainda vigentes acompanham.
        if (acao is AcaoSituacao.Inativar or AcaoSituacao.Rejeitar)
            foreach (var d in a.Dependentes.Where(d => Vigentes.Contains(d.Situacao)))
            {
                var motivoDep = $"Titular {(acao == AcaoSituacao.Inativar ? "inativado" : "rejeitado")}";
                if (d.Situacao == SituacaoCadastral.Pendente && acao == AcaoSituacao.Rejeitar) d.Rejeitar(motivoDep, Agora);
                else d.Inativar(motivoDep, Agora);
                db.HistoricoDependentes.Add(new Domain.Dependentes.HistoricoDependente(ClubeId, d.Id, acao.ToString(),
                    $"{motivoDep}. Situação: {d.Situacao}.", atual.UsuarioId, Agora));
            }

        await db.SaveChangesAsync(ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Excluir, ct);
        var a = await DoClube().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NaoEncontradoException("Associado");
        if (await db.Dependentes.AnyAsync(d => d.AssociadoTitularId == id && Vigentes.Contains(d.Situacao), ct))
            throw new ConflitoException("associado.possui_dependentes",
                "O associado possui dependentes vigentes. Inative ou transfira os dependentes antes de excluir.");
        db.Associados.Remove(a);
        Historico(a.Id, "Exclusao", "Cadastro excluído (exclusão lógica).");
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<HistoricoCadastroDto>> HistoricoAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Visualizar, ct);
        if (!await db.Associados.IgnoreQueryFilters([FiltrosConsulta.ExclusaoLogica]).AnyAsync(a => a.Id == id && a.ClubeId == ClubeId, ct))
            throw new NaoEncontradoException("Associado");
        return await db.HistoricoAssociados.AsNoTracking().Where(h => h.AssociadoId == id)
            .OrderByDescending(h => h.OcorridoEm)
            .Select(h => new HistoricoCadastroDto(h.Evento, h.Descricao, h.UsuarioId,
                db.Usuarios.IgnoreQueryFilters().Where(u => u.Id == h.UsuarioId).Select(u => u.Nome).FirstOrDefault(), h.OcorridoEm))
            .ToListAsync(ct);
    }

    public async Task<CriarAcessoAppResponse> CriarAcessoAppAsync(Guid id, CriarAcessoAppRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Gerenciar, ct);
        await acessoValidator.ValidarAsync(request, ct);
        var a = await DoClube().Include(x => x.Pessoa).FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException("Associado");
        if (a.Situacao is not (SituacaoCadastral.Ativo or SituacaoCadastral.Pendente))
            throw new DomainException("associado.nao_ativo", "Somente associados ativos ou pendentes podem receber acesso ao aplicativo.");

        var resultado = await AcessoApp.CriarAsync(db, provisionamento, a.Pessoa, ModelosPerfis.Associado, request.SenhaInicial, ct);
        Historico(a.Id, "AcessoApp", "Acesso ao aplicativo criado.");
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return resultado;
    }

    public async Task<ResumoCadastrosDto> ResumoAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Associados.Visualizar, ct);
        var porSituacao = await DoClube().GroupBy(a => a.Situacao).Select(g => new { g.Key, Total = g.Count() }).ToListAsync(ct);
        var porCategoria = await DoClube().Where(a => a.Situacao == SituacaoCadastral.Ativo)
            .GroupBy(a => a.Categoria.Nome).Select(g => new { g.Key, Total = g.Count() }).ToListAsync(ct);
        var deps = await db.Dependentes.Where(d => d.ClubeId == ClubeId).GroupBy(d => d.Situacao)
            .Select(g => new { g.Key, Total = g.Count() }).ToListAsync(ct);
        return new ResumoCadastrosDto(porSituacao.Sum(x => x.Total),
            porSituacao.ToDictionary(x => x.Key.ToString(), x => x.Total),
            porCategoria.ToDictionary(x => x.Key, x => x.Total),
            deps.Sum(x => x.Total), deps.ToDictionary(x => x.Key.ToString(), x => x.Total));
    }

    // ---------------------------------------------------------------- auxiliares

    private async Task<AssociadoDetalheDto> MontarDetalheAsync(Guid id, CancellationToken ct)
    {
        var a = await DoClube().AsNoTracking()
                    .Include(x => x.Pessoa).Include(x => x.Categoria)
                    .Include(x => x.Interesses).ThenInclude(i => i.Interesse)
                    .Include(x => x.Dependentes).ThenInclude(d => d.Pessoa)
                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException("Associado");

        return new AssociadoDetalheDto(a.Id, PessoaMapeamento.ParaDto(a.Pessoa, Hoje), a.Matricula,
            new CategoriaResumoDto(a.Categoria.Id, a.Categoria.Nome, a.Categoria.DireitoVoto), a.DataAssociacao, a.Situacao,
            a.MotivoSituacao, a.Categoria.DireitoVoto && a.Situacao == SituacaoCadastral.Ativo,
            a.Interesses.Where(i => i.Interesse is not null).Select(i => new InteresseDto(i.InteresseId, i.Interesse.Nome))
                .OrderBy(i => i.Nome).ToList(),
            a.Dependentes.OrderBy(d => d.NumeroIdentificacao).Select(d => new DependenteResumoDto(d.Id, d.PessoaId,
                d.Pessoa.NomeCompleto, d.Parentesco, d.ParentescoDescricao, d.NumeroIdentificacao, d.Situacao, d.Pessoa.Idade(Hoje))).ToList(),
            a.CriadoEm);
    }

    private async Task<CategoriaAssociado> ValidarCategoriaAsync(Guid categoriaId, CancellationToken ct) =>
        await db.CategoriasAssociados.FirstOrDefaultAsync(c => c.Id == categoriaId && c.ClubeId == ClubeId && c.Ativa, ct)
        ?? throw new ValidationException([new ValidationFailure("CategoriaId", "Categoria inexistente ou inativa.")]);

    private async Task<List<Guid>> ValidarInteressesAsync(IReadOnlyList<Guid>? ids, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0) return [];
        var distintos = ids.Distinct().ToList();
        var validos = await db.Interesses.Where(i => i.ClubeId == ClubeId && distintos.Contains(i.Id)).Select(i => i.Id).ToListAsync(ct);
        if (validos.Count != distintos.Count)
            throw new ValidationException([new ValidationFailure("InteresseIds", "Interesse inválido.")]);
        return validos;
    }

    private async Task<Pessoa> ObterOuCriarPessoaAsync(DadosPessoaisRequest dados, CancellationToken ct)
    {
        var cpf = Documentos.SomenteDigitos(dados.Cpf);
        var existente = await db.Pessoas.FirstOrDefaultAsync(p => p.ClubeId == ClubeId && p.Cpf == cpf, ct);
        if (existente is not null)
        {
            if (await db.Associados.AnyAsync(a => a.PessoaId == existente.Id && Vigentes.Contains(a.Situacao), ct))
                throw new ConflitoException("associado.cpf_duplicado", "Já existe um associado com este CPF neste clube.");
            if (await db.Dependentes.AnyAsync(d => d.PessoaId == existente.Id && Vigentes.Contains(d.Situacao), ct))
                throw new ConflitoException("associado.e_dependente",
                    "Esta pessoa está cadastrada como dependente. Inative o vínculo de dependente antes de torná-la titular.");
            PessoaMapeamento.Aplicar(existente, dados);
            return existente;
        }

        var pessoa = new Pessoa(ClubeId, dados.NomeCompleto, tokens.GerarTokenOpaco(), Agora);
        PessoaMapeamento.Aplicar(pessoa, dados);
        db.Pessoas.Add(pessoa);
        return pessoa;
    }

    private async Task GarantirCpfLivreAsync(string? cpf, Guid pessoaId, CancellationToken ct)
    {
        var d = Documentos.SomenteDigitos(cpf);
        if (d.Length == 0) return;
        if (await db.Pessoas.AnyAsync(p => p.ClubeId == ClubeId && p.Cpf == d && p.Id != pessoaId, ct))
            throw new ConflitoException("pessoa.cpf_duplicado", "Já existe outra pessoa com este CPF neste clube.");
    }

    private async Task<string> DefinirMatriculaAsync(string? informada, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(informada))
        {
            var m = informada.Trim().ToUpperInvariant();
            if (await db.Associados.IgnoreQueryFilters([FiltrosConsulta.ExclusaoLogica]).AnyAsync(a => a.ClubeId == ClubeId && a.Matricula == m, ct))
                throw new ConflitoException("associado.matricula_duplicada", "Esta matrícula já está em uso.");
            return m;
        }

        var prefixo = await db.ConfiguracoesClube.Where(c => c.ClubeId == ClubeId)
            .Select(c => c.PrefixoMatricula).FirstOrDefaultAsync(ct);
        while (true)
        {
            var numero = await sequencias.ProximoAsync(ClubeId, SequenciaMatricula, ct);
            var matricula = $"{prefixo}{numero:000000}";
            // Pula números ocupados por matrículas informadas manualmente.
            if (!await db.Associados.IgnoreQueryFilters([FiltrosConsulta.ExclusaoLogica]).AnyAsync(a => a.ClubeId == ClubeId && a.Matricula == matricula, ct))
                return matricula;
        }
    }

    private void Historico(Guid associadoId, string evento, string descricao) =>
        db.HistoricoAssociados.Add(new HistoricoAssociado(ClubeId, associadoId, evento, descricao, atual.UsuarioId, Agora));
}

/// <summary>Criação de login do aplicativo para uma pessoa (associado ou dependente).</summary>
internal static class AcessoApp
{
    public static async Task<CriarAcessoAppResponse> CriarAsync(IAppDbContext db, IProvisionamentoUsuarios provisionamento,
        Pessoa pessoa, string perfilChave, string? senhaInicial, CancellationToken ct)
    {
        if (pessoa.UsuarioId is not null)
            throw new ConflitoException("pessoa.ja_possui_acesso", "Esta pessoa já possui acesso ao aplicativo.");
        if (string.IsNullOrWhiteSpace(pessoa.Email))
            throw new DomainException("pessoa.sem_email", "Cadastre um e-mail para criar o acesso ao aplicativo.");

        var perfil = await db.Perfis.FirstOrDefaultAsync(p => p.ClubeId == pessoa.ClubeId && p.ChaveModelo == perfilChave, ct)
                     ?? throw new NaoEncontradoException("Perfil padrão do aplicativo");
        var r = await provisionamento.CriarOuVincularAsync(pessoa.NomeCompleto, pessoa.Email, pessoa.ClubeId, [perfil.Id],
            senhaInicial, ct);
        pessoa.UsuarioId = r.Usuario.Id;
        return new CriarAcessoAppResponse(r.Usuario.Id, r.ConviteEnviado);
    }
}
