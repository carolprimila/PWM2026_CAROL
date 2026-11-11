using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Application.Seguranca;
using ProjetoClubes.Domain.Clubes;

namespace ProjetoClubes.Application.Clubes;

public sealed record MembroDiretoriaDto(Guid Id, string Nome, string Cargo, string? Email, string? Telefone, DateOnly InicioMandato,
    DateOnly FimMandato, DateOnly? EncerradoEm, bool Atual, int? DiasParaTerminar);

/// <summary>Diretoria atual e anterior, com aviso quando o mandato do presidente está acabando ou não há presidente.</summary>
public sealed record DiretoriaDto(IReadOnlyList<MembroDiretoriaDto> Atual, IReadOnlyList<MembroDiretoriaDto> Anteriores, string? Aviso);

/// <summary>Sem fim informado, o mandato dura 2 anos a partir do início.</summary>
public sealed record SalvarMembroDiretoriaRequest(string Nome, string Cargo, string? Email, string? Telefone, DateOnly InicioMandato,
    DateOnly? FimMandato);

public sealed class SalvarMembroDiretoriaRequestValidator : AbstractValidator<SalvarMembroDiretoriaRequest>
{
    public SalvarMembroDiretoriaRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Informe o nome.").MaximumLength(150);
        RuleFor(x => x.Cargo).NotEmpty().WithMessage("Informe o cargo.").MaximumLength(60);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("E-mail inválido.").MaximumLength(254);
        RuleFor(x => x.Telefone).MaximumLength(30);
    }
}

public interface IDiretoriaService
{
    Task<DiretoriaDto> ObterAsync(CancellationToken ct);
    Task<DiretoriaDto> AdicionarAsync(SalvarMembroDiretoriaRequest r, CancellationToken ct);
    Task<DiretoriaDto> EncerrarAsync(Guid id, CancellationToken ct);
}

public sealed class DiretoriaService(
    IAppDbContext db,
    IUsuarioAtual atual,
    IPermissaoResolver permissoes,
    TimeProvider relogio,
    IValidator<SalvarMembroDiretoriaRequest> validator) : IDiretoriaService
{
    /// <summary>A partir de quantos dias do fim o sistema avisa sobre o mandato do presidente.</summary>
    public const int DiasAviso = 60;

    private Guid ClubeId => atual.ClubeIdObrigatorio;
    private DateOnly Hoje => DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);

    public async Task<DiretoriaDto> ObterAsync(CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Visualizar, ct);
        return await MontarAsync(ct);
    }

    public async Task<DiretoriaDto> AdicionarAsync(SalvarMembroDiretoriaRequest r, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Editar, ct);
        await validator.ValidarAsync(r, ct);
        var m = new MembroDiretoria(ClubeId, r.Nome, r.Cargo, r.InicioMandato, r.FimMandato)
        {
            Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim(),
            Telefone = string.IsNullOrWhiteSpace(r.Telefone) ? null : r.Telefone.Trim(),
        };
        if (m.Presidente)
        {
            // Um presidente por vez: o mandato novo não pode sobrepor o de outro presidente ainda em vigor.
            var presidentes = await db.MembrosDiretoria.Where(x => x.ClubeId == ClubeId && x.Cargo.ToLower() == "presidente").ToListAsync(ct);
            if (presidentes.FirstOrDefault(x => x.InicioMandato <= m.FimMandato && m.InicioMandato <= x.TerminoEfetivo) is { } outro)
                throw new ConflitoException("diretoria.presidente",
                    $"{outro.Nome} é presidente até {outro.TerminoEfetivo:dd/MM/yyyy}. Encerre esse mandato ou comece o novo depois dessa data.");
        }
        db.MembrosDiretoria.Add(m);
        await db.SaveChangesAsync(ct);
        return await MontarAsync(ct);
    }

    public async Task<DiretoriaDto> EncerrarAsync(Guid id, CancellationToken ct)
    {
        await permissoes.ExigirAsync(Permissoes.Clubes.Editar, ct);
        var m = await db.MembrosDiretoria.FirstOrDefaultAsync(x => x.Id == id && x.ClubeId == ClubeId, ct)
                ?? throw new NaoEncontradoException("Membro da diretoria");
        m.Encerrar(Hoje);
        await db.SaveChangesAsync(ct);
        return await MontarAsync(ct);
    }

    private async Task<DiretoriaDto> MontarAsync(CancellationToken ct)
    {
        var hoje = Hoje;
        var todos = await db.MembrosDiretoria.AsNoTracking().Where(x => x.ClubeId == ClubeId).ToListAsync(ct);
        MembroDiretoriaDto Dto(MembroDiretoria m) => new(m.Id, m.Nome, m.Cargo, m.Email, m.Telefone, m.InicioMandato, m.FimMandato,
            m.EncerradoEm, m.Atual(hoje), m.Atual(hoje) ? m.TerminoEfetivo.DayNumber - hoje.DayNumber : null);

        var atuais = todos.Where(m => m.Atual(hoje) || m.InicioMandato > hoje)
            .OrderByDescending(m => m.Presidente).ThenBy(m => m.InicioMandato > hoje).ThenBy(m => m.Cargo).Select(Dto).ToList();
        var anteriores = todos.Where(m => m.TerminoEfetivo < hoje).OrderByDescending(m => m.TerminoEfetivo).Select(Dto).ToList();

        var presidente = todos.FirstOrDefault(m => m.Presidente && m.Atual(hoje));
        var sucessor = todos.Any(m => m.Presidente && m.InicioMandato > hoje);
        string? aviso = presidente is null
            ? "O clube está sem presidente com mandato em vigor. Cadastre o presidente da gestão atual."
            : !sucessor && presidente.TerminoEfetivo.DayNumber - hoje.DayNumber <= DiasAviso
                ? $"O mandato de {presidente.Nome} termina em {presidente.TerminoEfetivo:dd/MM/yyyy}. Organize a eleição e cadastre o próximo presidente."
                : null;
        return new DiretoriaDto(atuais, anteriores, aviso);
    }
}
