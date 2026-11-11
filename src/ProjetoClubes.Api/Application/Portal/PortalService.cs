using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Cadastros;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;

namespace ProjetoClubes.Application.Portal;

public sealed record PortalPerfilDto(
    PessoaDto Pessoa,
    string Tipo,
    Guid? AssociadoId,
    Guid? DependenteId,
    string NumeroIdentificacao,
    SituacaoCadastral Situacao,
    string? Categoria,
    string? TitularNome,
    bool PodeEditarContato,
    bool PodeGerenciarDependentes);

public sealed record AtualizarContatoRequest(string? Telefone, string? Email, EnderecoDto? Endereco);

/// <summary>Inclusão de dependente pelo titular (sempre sujeita às regras do clube; aprovação conforme configuração).</summary>
public sealed record CriarDependentePortalRequest(DadosPessoaisRequest Pessoa, Parentesco Parentesco, string? ParentescoDescricao);

public sealed class AtualizarContatoRequestValidator : AbstractValidator<AtualizarContatoRequest>
{
    public AtualizarContatoRequestValidator()
    {
        RuleFor(x => x.Telefone).Telefone();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("E-mail inválido.");
        When(x => x.Endereco is not null, () =>
        {
            RuleFor(x => x.Endereco!.Uf).Uf();
            RuleFor(x => x.Endereco!.Cep).Cep();
        });
    }
}

public sealed class CriarDependentePortalRequestValidator : AbstractValidator<CriarDependentePortalRequest>
{
    public CriarDependentePortalRequestValidator()
    {
        RuleFor(x => x.Pessoa).NotNull().SetValidator(new DadosPessoaisValidator(exigeCpf: false));
        RuleFor(x => x.Parentesco).IsInEnum();
        RuleFor(x => x.ParentescoDescricao).NotEmpty().When(x => x.Parentesco == Parentesco.Outro)
            .WithMessage("Descreva o grau de parentesco.");
    }
}

public interface IPortalService
{
    Task<PortalPerfilDto> PerfilAsync(CancellationToken ct);
    Task<PortalPerfilDto> AtualizarContatoAsync(AtualizarContatoRequest request, CancellationToken ct);
    Task<IReadOnlyList<DependenteListaDto>> ListarDependentesAsync(CancellationToken ct);
    Task<DependenteDetalheDto> ObterDependenteAsync(Guid id, CancellationToken ct);
    Task<DependenteDetalheDto> CriarDependenteAsync(CriarDependentePortalRequest request, CancellationToken ct);
    Task<DependenteDetalheDto> AtualizarDependenteAsync(Guid id, AtualizarDependenteRequest request, CancellationToken ct);
    Task<CriarAcessoAppResponse> AutorizarAcessoAppDependenteAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Área do associado/dependente no aplicativo. Tudo é resolvido a partir do usuário autenticado:
/// o titular só enxerga a si e aos próprios dependentes; dependentes só enxergam a si mesmos.
/// </summary>
public sealed class PortalService(
    IAppDbContext db,
    IAcessoPessoas acesso,
    IPermissaoResolver permissoes,
    GestaoDependentes gestao,
    DependenteService dependentes,
    ITransacoes transacoes,
    IValidator<AtualizarContatoRequest> contatoValidator,
    IValidator<CriarDependentePortalRequest> criarValidator,
    IValidator<AtualizarDependenteRequest> atualizarValidator) : IPortalService
{
    public async Task<PortalPerfilDto> PerfilAsync(CancellationToken ct)
    {
        var p = await acesso.ObterPortalAsync(ct);
        var cfg = await gestao.ConfiguracaoAsync(ct);
        var menor = p.Pessoa.MenorDeIdade(gestao.Hoje, cfg.IdadeMaioridade);
        var dto = PessoaMapeamento.ParaDto(p.Pessoa, gestao.Hoje);
        return p.Associado is { } a
            ? new PortalPerfilDto(dto, "Associado", a.Id, null, a.Matricula, a.Situacao, a.Categoria.Nome, null, !menor,
                a.Situacao is SituacaoCadastral.Ativo or SituacaoCadastral.Pendente)
            : new PortalPerfilDto(dto, "Dependente", null, p.Dependente!.Id, p.Dependente.NumeroIdentificacao, p.Dependente.Situacao,
                null, p.Dependente.AssociadoTitular.Pessoa.NomeCompleto, !menor, false);
    }

    public async Task<PortalPerfilDto> AtualizarContatoAsync(AtualizarContatoRequest r, CancellationToken ct)
    {
        await contatoValidator.ValidarAsync(r, ct);
        var p = await acesso.ObterPortalAsync(ct);
        var cfg = await gestao.ConfiguracaoAsync(ct);
        if (p.Pessoa.MenorDeIdade(gestao.Hoje, cfg.IdadeMaioridade))
            throw new AcessoNegadoException("Dados de menores de idade são atualizados pelo titular responsável.");
        p.Pessoa.Telefone = string.IsNullOrWhiteSpace(r.Telefone) ? null : Documentos.SomenteDigitos(r.Telefone);
        p.Pessoa.Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim();
        // Endereço só muda quando enviado (o app pode atualizar apenas telefone e e-mail).
        if (r.Endereco is not null) PessoaMapeamento.AplicarEndereco(p.Pessoa, r.Endereco);
        await db.SaveChangesAsync(ct);
        return await PerfilAsync(ct);
    }

    public async Task<IReadOnlyList<DependenteListaDto>> ListarDependentesAsync(CancellationToken ct)
    {
        var titular = await TitularAsync(ct);
        var hoje = gestao.Hoje;
        var lista = await db.Dependentes.AsNoTracking().Include(d => d.Pessoa)
            .Where(d => d.AssociadoTitularId == titular.Id).OrderBy(d => d.NumeroIdentificacao).ToListAsync(ct);
        return lista.Select(d => new DependenteListaDto(d.Id, d.PessoaId, d.Pessoa.NomeCompleto, Domain.Pessoas.Pessoa.MascararCpf(d.Pessoa.Cpf),
            d.Pessoa.Idade(hoje), d.Parentesco, d.ParentescoDescricao, d.NumeroIdentificacao, d.Situacao, titular.Id,
            titular.Pessoa.NomeCompleto, titular.Matricula, d.Pessoa.FotoArquivoId is not null, d.DataInclusao)).ToList();
    }

    public async Task<DependenteDetalheDto> ObterDependenteAsync(Guid id, CancellationToken ct)
    {
        await DependenteDoTitularAsync(id, ct);
        return await dependentes.MontarDetalheAsync(id, ct);
    }

    public async Task<DependenteDetalheDto> CriarDependenteAsync(CriarDependentePortalRequest r, CancellationToken ct)
    {
        await criarValidator.ValidarAsync(r, ct);
        var titularPortal = await TitularAsync(ct);
        var titular = await gestao.ObterTitularAsync(titularPortal.Id, ct);
        var d = await transacoes.ComBloqueioAsync($"titular:{titular.Id}", async () =>
        {
            var novo = await gestao.IncluirAsync(titular, r.Pessoa, r.Parentesco, r.ParentescoDescricao, justificativaExcecao: null,
                podeAutorizarExcecao: false, aprovacaoDireta: false, ct);
            gestao.Historico(novo.Id, "SolicitacaoTitular", "Inclusão solicitada pelo titular no aplicativo.");
            await db.SaveChangesAsync(ct);
            return novo;
        }, ct);
        return await dependentes.MontarDetalheAsync(d.Id, ct);
    }

    public async Task<DependenteDetalheDto> AtualizarDependenteAsync(Guid id, AtualizarDependenteRequest r, CancellationToken ct)
    {
        await atualizarValidator.ValidarAsync(r, ct);
        await DependenteDoTitularAsync(id, ct);
        var d = await dependentes.CarregarAsync(id, ct);
        if (d.Situacao != SituacaoCadastral.Pendente)
            throw new ConflitoException("dependente.ja_analisado",
                "Após a análise da administração, alterações devem ser solicitadas à secretaria do clube.");
        await dependentes.AplicarAtualizacaoAsync(d, r, ct);
        await db.SaveChangesAsync(ct);
        return await dependentes.MontarDetalheAsync(id, ct);
    }

    /// <summary>Autorização do titular para o dependente ter login próprio (respeita a idade mínima do clube).</summary>
    public async Task<CriarAcessoAppResponse> AutorizarAcessoAppDependenteAsync(Guid id, CancellationToken ct)
    {
        await DependenteDoTitularAsync(id, ct);
        var d = await dependentes.CarregarAsync(id, ct);
        var r = await dependentes.CriarAcessoAppInternoAsync(d, senha: null, ct);
        gestao.Historico(d.Id, "AutorizacaoTitular", "Login próprio autorizado pelo titular.");
        await db.SaveChangesAsync(ct);
        permissoes.Invalidar();
        return r;
    }

    private async Task<Domain.Associados.Associado> TitularAsync(CancellationToken ct)
    {
        var p = await acesso.ObterPortalAsync(ct);
        return p.Associado ?? throw new AcessoNegadoException("Somente o associado titular pode gerenciar dependentes.");
    }

    private async Task DependenteDoTitularAsync(Guid dependenteId, CancellationToken ct)
    {
        var titular = await TitularAsync(ct);
        if (!await db.Dependentes.AnyAsync(d => d.Id == dependenteId && d.AssociadoTitularId == titular.Id, ct))
            throw new NaoEncontradoException("Dependente");
    }
}
