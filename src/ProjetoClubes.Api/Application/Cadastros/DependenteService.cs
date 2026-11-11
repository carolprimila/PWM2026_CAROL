using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Application.Usuarios;
using ProjetoClubes.Domain.Arquivos;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Application.Cadastros;

public interface IDependenteService
{
    Task<Pagina<DependenteListaDto>> ListarAsync(ConsultaDependentes consulta, CancellationToken ct);
    Task<DependenteDetalheDto> ObterAsync(Guid id, CancellationToken ct);
    Task<DependenteDetalheDto> CriarAsync(CriarDependenteRequest request, CancellationToken ct);
    Task<DependenteDetalheDto> AtualizarAsync(Guid id, AtualizarDependenteRequest request, CancellationToken ct);
    Task<DependenteDetalheDto> AlterarSituacaoAsync(Guid id, AcaoSituacao acao, string? motivo, CancellationToken ct);
    Task<DependenteDetalheDto> TransferirAsync(Guid id, TransferirDependenteRequest request, CancellationToken ct);
    Task<DependenteDetalheDto> AtualizarPermissoesAsync(Guid id, PermissoesDependenteDto request, CancellationToken ct);
    Task ExcluirAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<HistoricoCadastroDto>> HistoricoAsync(Guid id, CancellationToken ct);
    Task<CriarAcessoAppResponse> CriarAcessoAppAsync(Guid id, CriarAcessoAppRequest request, CancellationToken ct);
    Task<ConfiguracaoDependentesDto> ObterConfiguracaoAsync(CancellationToken ct);
    Task<ConfiguracaoDependentesDto> AtualizarConfiguracaoAsync(ConfiguracaoDependentesDto request, CancellationToken ct);
}

public sealed class DependenteService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    GestaoDependentes gestao,
    IProvisionamentoUsuarios provisionamento,
    Comunicacao.INotificador notificador,
    ITransacoes transacoes,
    TimeProvider relogio,
    IValidator<CriarDependenteRequest> criarValidator,
    IValidator<AtualizarDependenteRequest> atualizarValidator,
    IValidator<TransferirDependenteRequest> transferirValidator,
    IValidator<ConfiguracaoDependentesDto> configValidator,
    IValidator<CriarAcessoAppRequest> acessoValidator) : IDependenteService
{
    private static readonly Dictionary<string, Expression<Func<Dependente, object?>>> Ordenacoes = new()
    {
        ["nome"] = d => d.Pessoa.NomeCompleto,
        ["numero"] = d => d.NumeroIdentificacao,
        ["titular"] = d => d.AssociadoTitular.Pessoa.NomeCompleto,
        ["situacao"] = d => d.Situacao,
        ["datainclusao"] = d => d.DataInclusao,
    };

    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateTimeOffset Agora => relogio.GetUtcNow();

    private IQueryable<Dependente> DoClube() => db.Dependentes.Where(d => d.ClubeId == ClubeId);

    public async Task<Pagina<DependenteListaDto>> ListarAsync(ConsultaDependentes c, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Visualizar, ct);
        var q = DoClube().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(c.Busca))
        {
            var termo = c.Busca.Trim().ToLower();
            var digitos = Documentos.SomenteDigitos(c.Busca);
            q = q.Where(d => d.Pessoa.NomeCompleto.ToLower().Contains(termo) || d.NumeroIdentificacao.ToLower().Contains(termo)
                             || d.AssociadoTitular.Pessoa.NomeCompleto.ToLower().Contains(termo)
                             || (digitos.Length >= 3 && d.Pessoa.Cpf != null && d.Pessoa.Cpf.Contains(digitos)));
        }
        if (c.Situacao is { } s) q = q.Where(d => d.Situacao == s);
        if (c.Parentesco is { } p) q = q.Where(d => d.Parentesco == p);
        if (c.TitularId is { } t) q = q.Where(d => d.AssociadoTitularId == t);

        var pagina = await q.Ordenar(c, Ordenacoes, "nome")
            .Select(d => new
            {
                d.Id, d.PessoaId, d.Pessoa.NomeCompleto, d.Pessoa.Cpf, d.Pessoa.DataNascimento, d.Parentesco, d.ParentescoDescricao,
                d.NumeroIdentificacao, d.Situacao, d.AssociadoTitularId, TitularNome = d.AssociadoTitular.Pessoa.NomeCompleto,
                TitularMatricula = d.AssociadoTitular.Matricula, TemFoto = d.Pessoa.FotoArquivoId != null, d.DataInclusao,
            })
            .PaginarAsync(c, ct);

        var hoje = gestao.Hoje;
        return new Pagina<DependenteListaDto>(pagina.Itens.Select(d => new DependenteListaDto(d.Id, d.PessoaId, d.NomeCompleto,
                Pessoa.MascararCpf(d.Cpf), d.DataNascimento is { } n ? Pessoa.CalcularIdade(n, hoje) : null, d.Parentesco,
                d.ParentescoDescricao, d.NumeroIdentificacao, d.Situacao, d.AssociadoTitularId, d.TitularNome, d.TitularMatricula,
                d.TemFoto, d.DataInclusao)).ToList(),
            pagina.PaginaAtual, pagina.TamanhoPagina, pagina.TotalItens);
    }

    public async Task<DependenteDetalheDto> ObterAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Visualizar, ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task<DependenteDetalheDto> CriarAsync(CriarDependenteRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Cadastrar, ct);
        await criarValidator.ValidarAsync(r, ct);
        var podeAprovar = await permissoes.PossuiAsync(Permissoes.Dependentes.Aprovar, ct);
        var titular = await gestao.ObterTitularAsync(r.AssociadoTitularId, ct);
        // Bloqueio por titular: duas inclusões simultâneas não passam juntas do limite de dependentes.
        var d = await transacoes.ComBloqueioAsync($"titular:{titular.Id}", async () =>
        {
            var novo = await gestao.IncluirAsync(titular, r.Pessoa, r.Parentesco, r.ParentescoDescricao,
                r.JustificativaExcecaoMultiplosTitulares, podeAutorizarExcecao: podeAprovar, aprovacaoDireta: podeAprovar, ct);
            await db.SaveChangesAsync(ct);
            return novo;
        }, ct);
        return await MontarDetalheAsync(d.Id, ct);
    }

    public async Task<DependenteDetalheDto> AtualizarAsync(Guid id, AtualizarDependenteRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Editar, ct);
        await atualizarValidator.ValidarAsync(r, ct);
        var d = await CarregarAsync(id, ct);
        await AplicarAtualizacaoAsync(d, r, ct);
        await db.SaveChangesAsync(ct);
        return await MontarDetalheAsync(id, ct);
    }

    /// <summary>Atualização de dados (usada também pelo portal enquanto o cadastro está pendente).</summary>
    internal async Task AplicarAtualizacaoAsync(Dependente d, AtualizarDependenteRequest r, CancellationToken ct)
    {
        var cfg = await gestao.ConfiguracaoAsync(ct);
        if (!cfg.ParentescosPermitidos.Contains(r.Parentesco))
            throw new DomainException("dependente.parentesco_nao_permitido", $"O clube não permite dependentes com parentesco \"{r.Parentesco}\".");
        RegrasDependentes.ValidarIdade(cfg, r.Parentesco, r.Pessoa.DataNascimento, gestao.Hoje);

        var cpf = Documentos.SomenteDigitos(r.Pessoa.Cpf);
        if (cpf.Length > 0 && await db.Pessoas.AnyAsync(p => p.ClubeId == ClubeId && p.Cpf == cpf && p.Id != d.PessoaId, ct))
            throw new ConflitoException("pessoa.cpf_duplicado", "Já existe outra pessoa com este CPF neste clube.");

        PessoaMapeamento.Aplicar(d.Pessoa, r.Pessoa);
        d.DefinirParentesco(r.Parentesco, r.ParentescoDescricao);
        gestao.Historico(d.Id, "Edicao", "Dados cadastrais atualizados.");
    }

    public async Task<DependenteDetalheDto> AlterarSituacaoAsync(Guid id, AcaoSituacao acao, string? motivo, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Aprovar, ct);
        var d = await CarregarAsync(id, ct);
        var m = motivo ?? string.Empty;
        switch (acao)
        {
            case AcaoSituacao.Aprovar:
                await ValidarAprovacaoAsync(d, ct);
                d.Aprovar(Agora);
                break;
            case AcaoSituacao.Rejeitar: d.Rejeitar(m, Agora); break;
            case AcaoSituacao.Suspender: d.Suspender(m, Agora); break;
            case AcaoSituacao.Reativar:
                if (d.AssociadoTitular.Situacao != SituacaoCadastral.Ativo)
                    throw new DomainException("dependente.titular_nao_ativo", "Reative o titular antes de reativar o dependente.");
                d.Reativar(Agora);
                break;
            case AcaoSituacao.Inativar: d.Inativar(m, Agora); break;
        }
        gestao.Historico(d.Id, acao.ToString(),
            string.IsNullOrWhiteSpace(motivo) ? $"Situação: {d.Situacao}." : $"Situação: {d.Situacao}. Motivo: {motivo}");
        await db.SaveChangesAsync(ct);
        if (acao is AcaoSituacao.Aprovar or AcaoSituacao.Rejeitar)
            await notificador.NotificarPessoasAsync([d.AssociadoTitular.PessoaId], Domain.Comunicacao.TipoNotificacao.Dependente,
                acao == AcaoSituacao.Aprovar ? "Dependente aprovado" : "Dependente não aprovado",
                acao == AcaoSituacao.Aprovar ? $"{d.Pessoa.NomeCompleto} foi aprovado(a) como seu dependente."
                    : $"A inclusão de {d.Pessoa.NomeCompleto} não foi aprovada. Motivo: {motivo}", d.Id, ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task<DependenteDetalheDto> TransferirAsync(Guid id, TransferirDependenteRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Aprovar, ct);
        await transferirValidator.ValidarAsync(r, ct);
        var d = await CarregarAsync(id, ct);
        if (!d.Vigente) throw new DomainException("dependente.nao_vigente", "Somente dependentes vigentes podem ser transferidos.");
        var novo = await gestao.ObterTitularAsync(r.NovoTitularId, ct);
        await gestao.TransferirAsync(d, novo, r.Parentesco, r.ParentescoDescricao, r.Motivo, ct);
        await db.SaveChangesAsync(ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task<DependenteDetalheDto> AtualizarPermissoesAsync(Guid id, PermissoesDependenteDto r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Editar, ct);
        var d = await CarregarAsync(id, ct);
        var cfg = await gestao.ConfiguracaoAsync(ct);
        var nova = r.ParaDominio();
        if (nova.SolicitacaoReservas && d.Pessoa.MenorDeIdade(gestao.Hoje, cfg.IdadeMaioridade) && !cfg.MenoresPodemSolicitarReservas)
            throw new DomainException("dependente.menor_reservas", "Pelas regras do clube, menores de idade não podem solicitar reservas.");
        d.AtualizarPermissoes(nova);
        gestao.Historico(d.Id, "Permissoes", "Permissões do dependente atualizadas.");
        await db.SaveChangesAsync(ct);
        return await MontarDetalheAsync(id, ct);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Excluir, ct);
        var d = await CarregarAsync(id, ct);
        db.Dependentes.Remove(d); // exclusão lógica; vínculos são encerrados
        gestao.Historico(d.Id, "Exclusao", "Dependente excluído (exclusão lógica).");
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<HistoricoCadastroDto>> HistoricoAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Dependentes.Visualizar, ct);
        if (!await db.Dependentes.IgnoreQueryFilters([FiltrosConsulta.ExclusaoLogica]).AnyAsync(d => d.Id == id && d.ClubeId == ClubeId, ct))
            throw new NaoEncontradoException("Dependente");
        return await db.HistoricoDependentes.AsNoTracking().Where(h => h.DependenteId == id)
            .OrderByDescending(h => h.OcorridoEm)
            .Select(h => new HistoricoCadastroDto(h.Evento, h.Descricao, h.UsuarioId,
                db.Usuarios.IgnoreQueryFilters().Where(u => u.Id == h.UsuarioId).Select(u => u.Nome).FirstOrDefault(), h.OcorridoEm))
            .ToListAsync(ct);
    }

    public async Task<CriarAcessoAppResponse> CriarAcessoAppAsync(Guid id, CriarAcessoAppRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Usuarios.Gerenciar, ct);
        await acessoValidator.ValidarAsync(request, ct);
        var d = await CarregarAsync(id, ct);
        var r = await CriarAcessoAppInternoAsync(d, request.SenhaInicial, ct);
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return r;
    }

    internal async Task<CriarAcessoAppResponse> CriarAcessoAppInternoAsync(Dependente d, string? senha, CancellationToken ct)
    {
        if (d.Situacao != SituacaoCadastral.Ativo)
            throw new DomainException("dependente.nao_ativo", "O dependente precisa estar ativo para receber acesso ao aplicativo.");
        var cfg = await gestao.ConfiguracaoAsync(ct);
        if (!RegrasDependentes.PodeTerLoginProprio(cfg, d.Pessoa.DataNascimento, gestao.Hoje))
            throw new DomainException("dependente.idade_login",
                $"Pelas regras do clube, dependentes só podem ter login próprio a partir de {cfg.IdadeMinimaLoginProprio} anos.");
        var r = await AcessoApp.CriarAsync(db, provisionamento, d.Pessoa, ModelosPerfis.Dependente, senha, ct);
        gestao.Historico(d.Id, "AcessoApp", "Acesso próprio ao aplicativo criado.");
        return r;
    }

    public async Task<ConfiguracaoDependentesDto> ObterConfiguracaoAsync(CancellationToken ct)
    {
        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        if (!minhas.Contains(Permissoes.Dependentes.Visualizar) && !minhas.Contains(Permissoes.Clubes.Configurar))
            throw new AcessoNegadoException();
        return ParaDto(await gestao.ConfiguracaoAsync(ct));
    }

    public async Task<ConfiguracaoDependentesDto> AtualizarConfiguracaoAsync(ConfiguracaoDependentesDto r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Configurar, ct);
        await configValidator.ValidarAsync(r, ct);
        var c = await gestao.ConfiguracaoAsync(ct);
        c.LimitePadraoDependentes = r.LimitePadraoDependentes;
        c.MaximoConjuges = r.MaximoConjuges;
        c.IdadeMaximaFilhos = r.IdadeMaximaFilhos;
        c.IdadeMaximaIrmaos = r.IdadeMaximaIrmaos;
        c.IdadeMaioridade = r.IdadeMaioridade;
        c.IdadeMinimaLoginProprio = r.IdadeMinimaLoginProprio;
        c.ParentescosPermitidos = r.ParentescosPermitidos.Distinct().ToList();
        c.ExigeDocumentoComprobatorio = r.ExigeDocumentoComprobatorio;
        c.MenoresPodemSolicitarReservas = r.MenoresPodemSolicitarReservas;
        c.IncluidosNaMensalidadeDoTitular = r.IncluidosNaMensalidadeDoTitular;
        c.ValorAdicionalPorDependente = r.ValorAdicionalPorDependente;
        c.PermissoesPadrao = r.PermissoesPadrao.ParaDominio();
        await db.SaveChangesAsync(ct);
        return ParaDto(c);
    }

    // ---------------------------------------------------------------- auxiliares

    internal async Task<Dependente> CarregarAsync(Guid id, CancellationToken ct) =>
        await DoClube().Include(d => d.Pessoa).Include(d => d.Vinculos)
            .Include(d => d.AssociadoTitular).ThenInclude(a => a.Pessoa)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
        ?? throw new NaoEncontradoException("Dependente");

    private async Task ValidarAprovacaoAsync(Dependente d, CancellationToken ct)
    {
        if (d.AssociadoTitular.Situacao is not SituacaoCadastral.Ativo)
            throw new DomainException("dependente.titular_nao_ativo", "Aprove ou reative o titular antes de aprovar o dependente.");
        var cfg = await gestao.ConfiguracaoAsync(ct);
        if (cfg.ExigeDocumentoComprobatorio && !await db.DocumentosPessoais.AnyAsync(
                x => x.PessoaId == d.PessoaId && x.Situacao != SituacaoDocumento.Rejeitado, ct))
            throw new DomainException("dependente.documento_obrigatorio",
                "O clube exige documento comprobatório. Anexe o documento antes de aprovar.");
    }

    internal async Task<DependenteDetalheDto> MontarDetalheAsync(Guid id, CancellationToken ct)
    {
        var d = await DoClube().AsNoTracking()
                    .Include(x => x.Pessoa).Include(x => x.Vinculos)
                    .Include(x => x.AssociadoTitular).ThenInclude(a => a.Pessoa)
                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException("Dependente");
        var cfg = await gestao.ConfiguracaoAsync(ct);
        var hoje = gestao.Hoje;

        var titularIds = d.Vinculos.Select(v => v.AssociadoTitularId).Distinct().ToList();
        var nomes = await db.Associados.IgnoreQueryFilters([FiltrosConsulta.ExclusaoLogica])
            .Where(a => titularIds.Contains(a.Id) && a.ClubeId == ClubeId)
            .Select(a => new { a.Id, a.Pessoa.NomeCompleto }).ToDictionaryAsync(a => a.Id, a => a.NomeCompleto, ct);

        return new DependenteDetalheDto(d.Id, PessoaMapeamento.ParaDto(d.Pessoa, hoje), d.Parentesco, d.ParentescoDescricao,
            d.NumeroIdentificacao, d.Situacao, d.MotivoSituacao, d.DataInclusao,
            new TitularResumoDto(d.AssociadoTitularId, d.AssociadoTitular.Pessoa.NomeCompleto, d.AssociadoTitular.Matricula,
                d.AssociadoTitular.Situacao),
            PermissoesDependenteDto.De(d.Permissoes), d.Pessoa.MenorDeIdade(hoje, cfg.IdadeMaioridade),
            RegrasDependentes.PodeTerLoginProprio(cfg, d.Pessoa.DataNascimento, hoje),
            d.ExcecaoMultiplosTitulares, d.JustificativaExcecao,
            d.Vinculos.OrderBy(v => v.Inicio).Select(v => new VinculoDto(v.AssociadoTitularId,
                nomes.GetValueOrDefault(v.AssociadoTitularId, "—"), v.Inicio, v.Fim, v.Motivo)).ToList(),
            d.CriadoEm);
    }

    private static ConfiguracaoDependentesDto ParaDto(ConfiguracaoDependentes c) => new(c.LimitePadraoDependentes,
        c.MaximoConjuges, c.IdadeMaximaFilhos, c.IdadeMaximaIrmaos, c.IdadeMaioridade, c.IdadeMinimaLoginProprio,
        c.ParentescosPermitidos.Order().ToList(), c.ExigeDocumentoComprobatorio, c.MenoresPodemSolicitarReservas,
        c.IncluidosNaMensalidadeDoTitular, c.ValorAdicionalPorDependente, PermissoesDependenteDto.De(c.PermissoesPadrao));
}
