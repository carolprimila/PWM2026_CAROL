using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Carteirinhas;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Acesso;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Dependentes;

namespace ProjetoClubes.Application.ControleAcesso;

public sealed record ValidarCodigoRequest(string Codigo);

public sealed record RegistrarAcessoRequest(string Codigo, TipoMovimentoAcesso Tipo, string? Observacao);

public sealed record ResultadoValidacaoDto(
    bool Autorizado,
    string Mensagem,
    Guid? PessoaId,
    string? Nome,
    string? Tipo,
    string? NumeroIdentificacao,
    SituacaoCadastral? Situacao,
    string? Categoria,
    string? TitularNome,
    SituacaoCadastral? TitularSituacao,
    Parentesco? Parentesco,
    bool TemFoto,
    Guid? RegistroId);

public sealed record AcessoDto(Guid Id, Guid? PessoaId, string? Nome, TipoMovimentoAcesso Tipo, bool Autorizado, string? Motivo,
    DateTimeOffset OcorridoEm, Guid? RegistradoPor, string? RegistradoPorNome, string? Observacao);

public sealed record PresenteDto(Guid PessoaId, string Nome, DateTimeOffset EntradaEm);

public sealed class ConsultaAcessos : ConsultaPaginada
{
    public DateTimeOffset? De { get; set; }
    public DateTimeOffset? Ate { get; set; }
    public Guid? PessoaId { get; set; }
    public bool? Autorizado { get; set; }
    public TipoMovimentoAcesso? Tipo { get; set; }
}

public sealed class RegistrarAcessoRequestValidator : AbstractValidator<RegistrarAcessoRequest>
{
    public RegistrarAcessoRequestValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().WithMessage("Leia ou digite o código da carteirinha.").MaximumLength(150);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Observacao).MaximumLength(300);
    }
}

public interface IControleAcessoService
{
    Task<ResultadoValidacaoDto> ValidarAsync(ValidarCodigoRequest request, CancellationToken ct);
    Task<ResultadoValidacaoDto> RegistrarAsync(RegistrarAcessoRequest request, CancellationToken ct);
    Task<Pagina<AcessoDto>> ListarAsync(ConsultaAcessos consulta, CancellationToken ct);
    Task<IReadOnlyList<PresenteDto>> PresentesAsync(CancellationToken ct);
}

/// <summary>
/// Portaria: valida a carteirinha no servidor (situação, titular, permissões) e registra entradas e saídas
/// com o funcionário responsável. Carteirinhas de outro clube não são reconhecidas (filtro de tenant).
/// </summary>
public sealed class ControleAcessoService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    TimeProvider relogio,
    IValidator<RegistrarAcessoRequest> validator) : IControleAcessoService
{
    private sealed record Avaliacao(bool Autorizado, string Mensagem, Guid? PessoaId, Guid? AssociadoId, Guid? DependenteId,
        ResultadoValidacaoDto Resultado);

    private Guid ClubeId => atual.ClubeIdObrigatorio;

    public async Task<ResultadoValidacaoDto> ValidarAsync(ValidarCodigoRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.ControleAcesso.Validar, ct);
        return (await AvaliarAsync(request.Codigo, ct)).Resultado;
    }

    public async Task<ResultadoValidacaoDto> RegistrarAsync(RegistrarAcessoRequest request, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.ControleAcesso.Validar, ct);
        await validator.ValidarAsync(request, ct);
        var a = await AvaliarAsync(request.Codigo, ct);

        // A saída de uma pessoa reconhecida é sempre registrada como autorizada; a entrada depende da validação.
        var autorizado = request.Tipo == TipoMovimentoAcesso.Saida ? a.PessoaId is not null : a.Autorizado;
        var registro = new AcessoClube(ClubeId, a.PessoaId, a.AssociadoId, a.DependenteId, request.Tipo, autorizado,
            autorizado ? null : a.Mensagem, atual.UsuarioId, relogio.GetUtcNow(), request.Observacao?.Trim());
        db.AcessosClube.Add(registro);
        await db.SaveChangesAsync(ct);

        var mensagem = request.Tipo == TipoMovimentoAcesso.Saida && autorizado ? "Saída registrada." :
            autorizado ? "Entrada autorizada e registrada." : a.Mensagem;
        return a.Resultado with { Autorizado = autorizado, Mensagem = mensagem, RegistroId = registro.Id };
    }

    public async Task<Pagina<AcessoDto>> ListarAsync(ConsultaAcessos c, CancellationToken ct)
    {
        var minhas = await permissoes.ObterDoUsuarioAtualAsync(ct);
        if (!minhas.Contains(Permissoes.ControleAcesso.Validar) && !minhas.Contains(Permissoes.Relatorios.Visualizar))
            throw new AcessoNegadoException();

        var q = db.AcessosClube.AsNoTracking().Where(x => x.ClubeId == ClubeId);
        if (c.De is { } de) q = q.Where(x => x.OcorridoEm >= de);
        if (c.Ate is { } ate) q = q.Where(x => x.OcorridoEm <= ate);
        if (c.PessoaId is { } p) q = q.Where(x => x.PessoaId == p);
        if (c.Autorizado is { } aut) q = q.Where(x => x.Autorizado == aut);
        if (c.Tipo is { } t) q = q.Where(x => x.Tipo == t);

        return await q.OrderByDescending(x => x.OcorridoEm)
            .Select(x => new AcessoDto(x.Id, x.PessoaId,
                db.Pessoas.Where(pe => pe.Id == x.PessoaId).Select(pe => pe.NomeCompleto).FirstOrDefault(),
                x.Tipo, x.Autorizado, x.Motivo, x.OcorridoEm, x.RegistradoPor,
                db.Usuarios.IgnoreQueryFilters().Where(u => u.Id == x.RegistradoPor).Select(u => u.Nome).FirstOrDefault(),
                x.Observacao))
            .PaginarAsync(c, ct);
    }

    public async Task<IReadOnlyList<PresenteDto>> PresentesAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.ControleAcesso.Validar, ct);
        var desde = relogio.GetUtcNow().AddHours(-24);
        var ultimos = await db.AcessosClube.AsNoTracking()
            .Where(x => x.ClubeId == ClubeId && x.Autorizado && x.PessoaId != null && x.OcorridoEm >= desde)
            .GroupBy(x => x.PessoaId)
            .Select(g => g.OrderByDescending(x => x.OcorridoEm).First())
            .ToListAsync(ct);
        var dentro = ultimos.Where(x => x.Tipo == TipoMovimentoAcesso.Entrada).ToList();
        var ids = dentro.Select(x => x.PessoaId!.Value).ToList();
        var nomes = await db.Pessoas.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.NomeCompleto, ct);
        return dentro.Select(x => new PresenteDto(x.PessoaId!.Value, nomes.GetValueOrDefault(x.PessoaId!.Value, "—"), x.OcorridoEm))
            .OrderBy(p => p.Nome).ToList();
    }

    private async Task<Avaliacao> AvaliarAsync(string codigo, CancellationToken ct)
    {
        var token = CodigoCarteirinha.Extrair(codigo);
        var pessoa = token is null ? null
            : await db.Pessoas.AsNoTracking().FirstOrDefaultAsync(p => p.TokenCarteirinha == token && p.ClubeId == ClubeId, ct);
        if (pessoa is null)
            return Negado("Carteirinha não reconhecida neste clube.", null, null, null, Vazio("Carteirinha não reconhecida neste clube."));

        var associado = await db.Associados.AsNoTracking().Include(a => a.Categoria)
            .Where(a => a.PessoaId == pessoa.Id)
            .OrderBy(a => a.Situacao == SituacaoCadastral.Ativo ? 0 : 1).ThenByDescending(a => a.CriadoEm)
            .FirstOrDefaultAsync(ct);
        var temFoto = pessoa.FotoArquivoId is not null;

        if (associado is not null)
        {
            var (ok, msg) = associado.Situacao switch
            {
                SituacaoCadastral.Ativo => (true, "Acesso autorizado."),
                SituacaoCadastral.Pendente => (false, "Cadastro aguardando aprovação."),
                SituacaoCadastral.Suspenso => (false, "Associado suspenso. Encaminhe à secretaria."),
                _ => (false, "Associado sem cadastro ativo."),
            };
            var r = new ResultadoValidacaoDto(ok, msg, pessoa.Id, pessoa.NomeCompleto, "Associado", associado.Matricula,
                associado.Situacao, associado.Categoria.Nome, null, null, null, temFoto, null);
            return new Avaliacao(ok, msg, pessoa.Id, associado.Id, null, r);
        }

        var dependentes = await db.Dependentes.AsNoTracking().Include(d => d.AssociadoTitular).ThenInclude(t => t.Pessoa)
            .Where(d => d.PessoaId == pessoa.Id).ToListAsync(ct);
        if (dependentes.Count == 0)
            return Negado("Pessoa sem cadastro de associado ou dependente.", pessoa.Id, null, null,
                Vazio("Pessoa sem cadastro de associado ou dependente.") with { PessoaId = pessoa.Id, Nome = pessoa.NomeCompleto, TemFoto = temFoto });

        // Com exceção de múltiplos titulares, basta um vínculo que autorize; senão, mostra o motivo do vínculo mais relevante.
        var avaliados = dependentes.Select(d => (Dep: d, Motivo: MotivoDependente(d)))
            .OrderBy(x => x.Motivo is null ? 0 : 1).ThenBy(x => x.Dep.Situacao == SituacaoCadastral.Ativo ? 0 : 1).ToList();
        var (dep, motivo) = avaliados[0];
        var autorizado = motivo is null;
        var mensagem = motivo ?? "Acesso autorizado.";
        var res = new ResultadoValidacaoDto(autorizado, mensagem, pessoa.Id, pessoa.NomeCompleto, "Dependente", dep.NumeroIdentificacao,
            dep.Situacao, null, dep.AssociadoTitular.Pessoa.NomeCompleto, dep.AssociadoTitular.Situacao, dep.Parentesco, temFoto, null);
        return new Avaliacao(autorizado, mensagem, pessoa.Id, null, dep.Id, res);
    }

    private static string? MotivoDependente(Dependente d) => d switch
    {
        { Situacao: SituacaoCadastral.Pendente } => "Cadastro do dependente aguardando aprovação.",
        { Situacao: SituacaoCadastral.Suspenso } => "Dependente suspenso. Encaminhe à secretaria.",
        { Situacao: not SituacaoCadastral.Ativo } => "Dependente sem cadastro ativo.",
        { AssociadoTitular.Situacao: SituacaoCadastral.Suspenso } => "O titular está suspenso.",
        { AssociadoTitular.Situacao: not SituacaoCadastral.Ativo } => "O titular não está ativo.",
        { Permissoes.AcessoClube: false } => "Dependente sem permissão de acesso ao clube.",
        _ => null,
    };

    private static Avaliacao Negado(string msg, Guid? pessoaId, Guid? associadoId, Guid? dependenteId, ResultadoValidacaoDto r) =>
        new(false, msg, pessoaId, associadoId, dependenteId, r);

    private static ResultadoValidacaoDto Vazio(string mensagem) =>
        new(false, mensagem, null, null, null, null, null, null, null, null, null, false, null);
}
