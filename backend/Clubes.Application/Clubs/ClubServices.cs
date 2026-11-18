using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Clubes.Application.Access;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Clubs;

public record ClubDto(Guid Id, string Name, string? LegalName, string? Cnpj, string Slug, string? Email, string? Phone, string? ZipCode,
    string? Street, string? Number, string? Complement, string? District, string? City, string? State, Guid? LogoFileId, string? About,
    string TimeZoneId, bool IsActive, DateTime CreatedAt);
public record SaveClubRequest([Required, StringLength(150)] string Name, [StringLength(200)] string? LegalName, [StringLength(18)] string? Cnpj,
    [EmailAddress] string? Email, [StringLength(30)] string? Phone, [StringLength(9)] string? ZipCode, [StringLength(200)] string? Street,
    [StringLength(20)] string? Number, [StringLength(100)] string? Complement, [StringLength(100)] string? District, [StringLength(100)] string? City,
    [StringLength(2)] string? State, Guid? LogoFileId, [StringLength(4000)] string? About, string? TimeZoneId);
public record CreateClubRequest([Required] SaveClubRequest Club, [Required, StringLength(150)] string AdminName, [Required, EmailAddress] string AdminEmail, bool ApplyStandardStructure);
public record CreateClubResult(ClubDto Club, TemporaryPasswordDto Admin);
public record OpeningHourDto(DayOfWeek DayOfWeek, bool IsClosed, [Range(0, 1440)] int OpenMinute, [Range(0, 1440)] int CloseMinute);

/// <summary>Converte horários entre UTC (persistência) e o fuso do clube (regras de funcionamento).</summary>
public class ClubClock(IAppDb db, RequestContext ctx)
{
    private TimeZoneInfo? _tz;

    public async Task<TimeZoneInfo> ZoneAsync(CancellationToken ct = default)
    {
        if (_tz is not null) return _tz;
        var clubId = ctx.RequireClub();
        var id = await db.Set<Club>().Where(c => c.Id == clubId).Select(c => c.TimeZoneId).FirstOrDefaultAsync(ct) ?? "America/Sao_Paulo";
        return _tz = TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    public async Task<DateTime> NowLocalAsync(CancellationToken ct = default) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, await ZoneAsync(ct));
    public async Task<DateOnly> TodayAsync(CancellationToken ct = default) => DateOnly.FromDateTime(await NowLocalAsync(ct));
    public async Task<DateTime> ToLocalAsync(DateTime utc, CancellationToken ct = default) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), await ZoneAsync(ct));
    public async Task<DateTime> ToUtcAsync(DateTime local, CancellationToken ct = default) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), await ZoneAsync(ct));
    public async Task<DateTime> ToUtcAsync(DateOnly date, int minuteOfDay, CancellationToken ct = default) => await ToUtcAsync(date.ToDateTime(TimeOnly.MinValue).AddMinutes(minuteOfDay), ct);
}

public partial class ClubService(IAppDb db, RequestContext ctx, IPasswordHasher<User> hasher, IEmailSender email)
{
    public async Task<List<ClubDto>> ListAsync(CancellationToken ct = default)
    {
        if (ctx.Kind != UserKind.SuperAdmin) throw DomainException.Forbidden();
        return (await db.Set<Club>().AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<ClubDto> GetCurrentAsync(CancellationToken ct = default)
    {
        var id = ctx.RequireClub();
        return Map(await db.Set<Club>().AsNoTracking().Where(c => c.Id == id).FirstOr404Async("Clube", ct));
    }

    public async Task<CreateClubResult> CreateAsync(CreateClubRequest req, CancellationToken ct = default)
    {
        if (ctx.Kind != UserKind.SuperAdmin) throw DomainException.Forbidden("Somente o superadministrador da plataforma cadastra clubes.");
        var adminEmail = req.AdminEmail.Trim().ToLowerInvariant();
        if (await db.Set<User>().IgnoreQueryFilters().AnyAsync(u => u.Email == adminEmail && !u.IsDeleted, ct))
            throw DomainException.Conflict("Já existe um usuário com o e-mail informado para o administrador.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var club = new Club();
        await ApplyAsync(club, req.Club, ct);
        club.Slug = await UniqueSlugAsync(club.Name, ct);
        db.Set<Club>().Add(club);
        ClubProvisioning.Provision(db, club, req.ApplyStandardStructure);

        var password = PasswordPolicy.GenerateTemporary();
        var admin = new User { ClubId = club.Id, Name = req.AdminName.Trim(), Email = adminEmail, Kind = UserKind.ClubAdmin, MustChangePassword = true };
        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.Set<User>().Add(admin);

        // O superadministrador opera fora de um clube: o contexto assume o novo clube durante o provisionamento.
        var previous = ctx.ClubId;
        ctx.ClubId = club.Id;
        try { await db.SaveChangesAsync(ct); }
        finally { ctx.ClubId = previous; }
        await tx.CommitAsync(ct);

        var sent = false;
        if (email.IsConfigured)
        {
            try
            {
                await email.SendAsync(new EmailMessage(admin.Email, $"Acesso de administrador — {club.Name}",
                    $"Olá, {admin.Name}.\n\nO clube {club.Name} foi cadastrado na plataforma.\nLogin: {admin.Email}\nSenha temporária: {password}"), ct);
                sent = true;
            }
            catch { /* a senha temporária é devolvida na resposta */ }
        }
        return new CreateClubResult(Map(club), new TemporaryPasswordDto(admin.Id, admin.Email, password, sent));
    }

    public async Task<ClubDto> UpdateAsync(Guid id, SaveClubRequest req, CancellationToken ct = default)
    {
        if (ctx.Kind != UserKind.SuperAdmin && ctx.ClubId != id) throw DomainException.Forbidden();
        var club = await db.Set<Club>().Where(c => c.Id == id).FirstOr404Async("Clube", ct);
        await ApplyAsync(club, req, ct);
        await db.SaveChangesAsync(ct);
        return Map(club);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        if (ctx.Kind != UserKind.SuperAdmin) throw DomainException.Forbidden();
        var club = await db.Set<Club>().Where(c => c.Id == id).FirstOr404Async("Clube", ct);
        club.IsActive = active;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<OpeningHourDto>> GetOpeningHoursAsync(CancellationToken ct = default)
        => await db.Set<ClubOpeningHour>().OrderBy(h => h.DayOfWeek).Select(h => new OpeningHourDto(h.DayOfWeek, h.IsClosed, h.OpenMinute, h.CloseMinute)).ToListAsync(ct);

    public async Task<List<OpeningHourDto>> SetOpeningHoursAsync(List<OpeningHourDto> hours, CancellationToken ct = default)
    {
        if (hours.Any(h => !h.IsClosed && h.CloseMinute <= h.OpenMinute))
            throw new DomainException("O horário de fechamento deve ser posterior ao de abertura.", "horario_invalido", 400);
        if (hours.GroupBy(h => h.DayOfWeek).Any(g => g.Count() > 1))
            throw new DomainException("Informe um único horário por dia da semana.", "horario_invalido", 400);
        var existing = await db.Set<ClubOpeningHour>().ToListAsync(ct);
        db.Set<ClubOpeningHour>().RemoveRange(existing.Where(e => hours.All(h => h.DayOfWeek != e.DayOfWeek)));
        foreach (var h in hours)
        {
            var row = existing.FirstOrDefault(e => e.DayOfWeek == h.DayOfWeek);
            if (row is null) db.Set<ClubOpeningHour>().Add(row = new ClubOpeningHour { DayOfWeek = h.DayOfWeek });
            (row.IsClosed, row.OpenMinute, row.CloseMinute) = (h.IsClosed, h.OpenMinute, h.CloseMinute);
        }
        await db.SaveChangesAsync(ct);
        return await GetOpeningHoursAsync(ct);
    }

    private async Task ApplyAsync(Club club, SaveClubRequest req, CancellationToken ct)
    {
        var cnpj = req.Cnpj.OnlyDigits();
        if (cnpj.Length is not (0 or 14)) throw new DomainException("CNPJ inválido.", "cnpj_invalido", 400);
        if (cnpj.Length == 14 && await db.Set<Club>().AnyAsync(c => c.Cnpj == cnpj && c.Id != club.Id, ct))
            throw DomainException.Conflict("Já existe um clube com este CNPJ.");
        if (req.TimeZoneId is { Length: > 0 } tz && !TimeZoneInfo.TryFindSystemTimeZoneById(tz, out _))
            throw new DomainException("Fuso horário inválido.", "fuso_invalido", 400);

        club.Name = req.Name.Trim();
        club.LegalName = req.LegalName;
        club.Cnpj = cnpj.Length == 14 ? cnpj : null;
        club.Email = req.Email;
        club.Phone = req.Phone;
        club.ZipCode = req.ZipCode;
        club.Street = req.Street;
        club.Number = req.Number;
        club.Complement = req.Complement;
        club.District = req.District;
        club.City = req.City;
        club.State = req.State?.ToUpperInvariant();
        club.LogoFileId = req.LogoFileId;
        club.About = req.About;
        if (req.TimeZoneId is { Length: > 0 }) club.TimeZoneId = req.TimeZoneId;
    }

    private async Task<string> UniqueSlugAsync(string name, CancellationToken ct)
    {
        var normalized = name.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var ascii = new string(normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
        var baseSlug = SlugRegex().Replace(ascii, "-").Trim('-');
        if (baseSlug.Length == 0) baseSlug = "clube";
        var slug = baseSlug;
        for (var i = 2; await db.Set<Club>().IgnoreQueryFilters().AnyAsync(c => c.Slug == slug, ct); i++) slug = $"{baseSlug}-{i}";
        return slug;
    }

    internal static ClubDto Map(Club c) => new(c.Id, c.Name, c.LegalName, c.Cnpj, c.Slug, c.Email, c.Phone, c.ZipCode, c.Street, c.Number, c.Complement,
        c.District, c.City, c.State, c.LogoFileId, c.About, c.TimeZoneId, c.IsActive, c.CreatedAt);

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}

/// <summary>Dados iniciais de um clube novo: configurações em branco, funções padrão, tipos de manifestação e estrutura física.</summary>
public static class ClubProvisioning
{
    public static void Provision(IAppDb db, Club club, bool standardStructure)
    {
        db.Set<ClubSettings>().Add(new ClubSettings { ClubId = club.Id });

        foreach (var (name, codes) in Permissions.DefaultRoles)
        {
            var role = new Role { ClubId = club.Id, Name = name, IsSystem = true, Description = $"Função padrão: {name}" };
            role.Permissions.AddRange(codes.Select(c => new RolePermission { ClubId = club.Id, Permission = c }));
            db.Set<Role>().Add(role);
        }

        db.Set<ManifestationType>().AddRange(
            new ManifestationType { ClubId = club.Id, Name = "Reclamação" },
            new ManifestationType { ClubId = club.Id, Name = "Sugestão", IsSuggestion = true },
            new ManifestationType { ClubId = club.Id, Name = "Solicitação" });

        if (standardStructure) AddStandardStructure(db, club.Id);
    }

    /// <summary>Estrutura física inicial: 4 quadras, 10 quiosques (5 fechados e 5 abertos), salão de festas e piscinas.</summary>
    public static void AddStandardStructure(IAppDb db, Guid clubId)
    {
        var order = 0;
        var allDays = Enum.GetValues<DayOfWeek>();

        Space Add(string name, SpaceType type, int? capacity, string? subtype = null, bool reservable = true)
        {
            var space = new Space { ClubId = clubId, Name = name, Type = type, Subtype = subtype, Capacity = capacity, IsReservable = reservable, SortOrder = ++order };
            db.Set<Space>().Add(space);
            return space;
        }

        foreach (var name in new[] { "Quadra de Tênis", "Quadra de Vôlei", "Quadra de Futsal", "Quadra de Futcampo" })
        {
            var court = Add(name, SpaceType.Court, 30);
            court.Schedules.AddRange(allDays.Select(d => new SpaceSchedule { ClubId = clubId, DayOfWeek = d, OpenMinute = 8 * 60, CloseMinute = 22 * 60 }));
        }

        foreach (var (label, subtype) in new[] { ("Quiosque Fechado", "Fechado"), ("Quiosque Aberto", "Aberto") })
        {
            for (var i = 1; i <= 5; i++)
            {
                var kiosk = Add($"{label} {i}", SpaceType.Kiosk, 20, subtype);
                kiosk.Description = "Quiosque com churrasqueira";
                kiosk.Schedules.AddRange(allDays.Select(d => new SpaceSchedule
                {
                    ClubId = clubId, DayOfWeek = d, OpenMinute = 12 * 60,
                    CloseMinute = d is DayOfWeek.Friday or DayOfWeek.Saturday ? 24 * 60 : 22 * 60,
                }));
            }
        }

        var hall = Add("Salão de Festas", SpaceType.PartyHall, 250);
        hall.BookingMode = BookingMode.MultiDay;
        hall.FixedDays = 2;
        hall.Description = "Locação por período de 2 dias: montagem, preparação, evento, retirada da decoração e limpeza (realizada pelo clube).";

        Add("Piscinas", SpaceType.Pool, null, reservable: false).Description = "Uso livre conforme as políticas do clube. Não reservável.";
    }
}
