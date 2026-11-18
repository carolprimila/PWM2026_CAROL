using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.People;

public record CardDto(PersonType PersonType, Guid PersonId, string Name, Guid? PhotoFileId, string CardNumber, string ClubName, Guid? ClubLogoFileId,
    string Status, bool IsActive, string? ResponsibleName, bool IsInside);
public record QrTokenDto(string Token, string DisplayCode, AccessOperation Operation, DateTime ExpiresAt, int TtlSeconds);
public record ValidateRequest([Required, StringLength(300)] string Code, [StringLength(100)] string? Device);
public record ManualAccessRequest(PersonType PersonType, Guid PersonId, AccessOperation Operation, [Required, StringLength(300, MinimumLength = 5)] string Justification);
public record AccessResultDto(bool Authorized, string Message, AccessOperation? Operation, PersonType? PersonType, Guid? PersonId, string? PersonName,
    Guid? PhotoFileId, string? ResponsibleName, DateTime At);
public record PresenceDto(Guid Id, PersonType PersonType, Guid PersonId, string PersonName, Guid? ResponsibleMemberId, string? ResponsibleName, DateTime EnteredAt);
public record OccupancyDto(int Members, int Dependents, int Guests, int Total, IReadOnlyList<HourCount> PeakHours);
public record HourCount(int Hour, int Entries);
public record AccessLogDto(Guid Id, PersonType? PersonType, Guid? PersonId, string? PersonName, string? ResponsibleName, AccessOperation Operation,
    AccessResult Result, string? DenialReason, string? StaffName, string? Origin, DateTime OccurredAt);
public record GatePersonDto(PersonType PersonType, Guid PersonId, string Name, string? Document, Guid? PhotoFileId, string Status, string? ResponsibleName, bool IsInside);

public class AccessLogQuery : PageQuery
{
    public AccessResult? Result { get; set; }
    public AccessOperation? Operation { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>Cartão digital e emissão de QR Codes dinâmicos de uso único.</summary>
public class CardService(IAppDb db, RequestContext ctx, ITokenService tokens, SettingsService settings)
{
    public const string QrPrefix = "CLB1:";

    private (PersonType Type, Guid Id) Me() => ctx switch
    {
        { Kind: UserKind.Member, MemberId: { } m } => (PersonType.Member, m),
        { Kind: UserKind.Dependent, DependentId: { } d } => (PersonType.Dependent, d),
        _ => throw DomainException.Forbidden("O cartão digital está disponível para associados e dependentes."),
    };

    public Task<CardDto> MyCardAsync(CancellationToken ct = default) { var (t, id) = Me(); return GetAsync(t, id, ct); }

    public async Task<CardDto> GetAsync(PersonType type, Guid personId, CancellationToken ct = default)
    {
        string name, status; Guid? photo; string? responsible = null; bool active;
        if (type == PersonType.Member)
        {
            var m = await db.Set<Member>().Where(x => x.Id == personId).Select(x => new { x.FullName, x.PhotoFileId, x.Status }).FirstOr404Async("Associado", ct);
            (name, photo, status, active) = (m.FullName, m.PhotoFileId, m.Status.ToString(), m.Status == MemberStatus.Active);
        }
        else if (type == PersonType.Dependent)
        {
            var d = await db.Set<Dependent>().Where(x => x.Id == personId).Select(x => new { x.FullName, x.PhotoFileId, x.Status, Member = x.Member.FullName, MemberStatus = x.Member.Status }).FirstOr404Async("Dependente", ct);
            (name, photo, status, responsible) = (d.FullName, d.PhotoFileId, d.Status.ToString(), d.Member);
            active = d.Status == DependentStatus.Active && d.MemberStatus == MemberStatus.Active;
        }
        else throw new DomainException("Convidados não possuem cartão digital.", "tipo_invalido", 400);

        var card = await db.Set<DigitalCard>().FirstOrDefaultAsync(c => c.PersonType == type && c.PersonId == personId, ct);
        if (card is null)
        {
            card = new DigitalCard { PersonType = type, PersonId = personId, CardNumber = (await db.NextSequenceAsync("card", ct)).ToString("D8") };
            db.Set<DigitalCard>().Add(card);
            await db.SaveChangesAsync(ct);
        }
        var clubId = ctx.RequireClub();
        var club = await db.Set<Club>().Where(c => c.Id == clubId).Select(c => new { c.Name, c.LogoFileId }).FirstAsync(ct);
        var inside = await db.Set<Presence>().AnyAsync(p => p.PersonType == type && p.PersonId == personId, ct);
        return new CardDto(type, personId, name, photo, card.CardNumber, club.Name, club.LogoFileId, status, active && card.IsActive, responsible, inside);
    }

    /// <summary>
    /// Emite um novo token para a próxima operação da pessoa (entrada se está fora, saída se está dentro).
    /// Tokens anteriores ainda não usados são invalidados: existe no máximo um QR Code válido por pessoa.
    /// </summary>
    public async Task<QrTokenDto> IssueAsync(CancellationToken ct = default)
    {
        var (type, id) = Me();
        var card = await GetAsync(type, id, ct);
        if (!card.IsActive) throw new DomainException("Seu cadastro não está ativo. O QR Code não pode ser gerado.", "cadastro_inativo", 403);

        var now = DateTime.UtcNow;
        await db.Set<QrToken>().Where(t => t.PersonType == type && t.PersonId == id && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, now), ct);

        var ttl = (await settings.LoadAsync(ct)).QrTokenTtlSeconds;
        var raw = tokens.CreateOpaqueToken(24);
        var token = new QrToken
        {
            PersonType = type, PersonId = id, Operation = card.IsInside ? AccessOperation.Exit : AccessOperation.Entry,
            TokenHash = tokens.Hash(raw), DisplayCode = await NewDisplayCodeAsync(now, ct), ExpiresAt = now.AddSeconds(ttl), IssuedToUserId = ctx.RequireUser(),
        };
        db.Set<QrToken>().Add(token);
        await db.SaveChangesAsync(ct);
        return new QrTokenDto(QrPrefix + raw, token.DisplayCode, token.Operation, token.ExpiresAt, ttl);
    }

    private async Task<string> NewDisplayCodeAsync(DateTime now, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8");
            if (!await db.Set<QrToken>().AnyAsync(t => t.DisplayCode == code && t.ConsumedAt == null && t.ExpiresAt > now, ct)) return code;
        }
        throw new DomainException("Não foi possível gerar o código. Tente novamente.", "codigo_indisponivel", 503);
    }
}

/// <summary>Portaria: validação de QR Codes, entradas e saídas, ocupação e histórico.</summary>
public class GateService(IAppDb db, RequestContext ctx, ITokenService tokens, DelinquencyService delinquency, SettingsService settings,
    ClubClock clock, IRealtimeNotifier realtime)
{
    public async Task<AccessResultDto> ValidateAsync(ValidateRequest req, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var code = req.Code.Trim();
        QrToken? token;
        if (code.StartsWith(CardService.QrPrefix, StringComparison.Ordinal))
        {
            var hash = tokens.Hash(code[CardService.QrPrefix.Length..]);
            token = await db.Set<QrToken>().AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        }
        else
        {
            var digits = code.OnlyDigits();
            token = digits.Length == 8
                ? await db.Set<QrToken>().AsNoTracking().Where(t => t.DisplayCode == digits).OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync(ct)
                : null;
        }

        if (token is null) return await DenyAsync(null, null, null, null, AccessOperation.Entry, "QR Code inválido ou não reconhecido.", null, req.Device, ct);
        var (name, photo, responsibleId, responsibleName) = await PersonAsync(token.PersonType, token.PersonId, ct);
        Task<AccessResultDto> Deny(string reason) => DenyAsync(token.PersonType, token.PersonId, name, responsibleId, token.Operation, reason, token.Id, req.Device, ct, photo, responsibleName);

        if (token.ConsumedAt is not null) return await Deny("QR Code já utilizado. Gere um novo código no aplicativo.");
        if (token.ExpiresAt <= now) return await Deny("QR Code expirado. Gere um novo código no aplicativo.");

        // Consumo atômico: se duas leituras concorrerem, somente uma consegue invalidar o token.
        var staff = ctx.RequireUser();
        var consumed = await db.Set<QrToken>().Where(t => t.Id == token.Id && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now).SetProperty(t => t.ConsumedByUserId, staff), ct);
        if (consumed == 0) return await Deny("QR Code já utilizado. Gere um novo código no aplicativo.");

        var reason = await EligibilityAsync(token.PersonType, token.PersonId, token.Operation, ct);
        if (reason is not null) return await Deny(reason);
        return await RegisterAsync(token.PersonType, token.PersonId, name, photo, responsibleId, responsibleName, token.Operation, token.Id, req.Device ?? "qr", ct);
    }

    /// <summary>Registro manual (sem QR Code), sempre com justificativa — por exemplo, celular sem bateria.</summary>
    public async Task<AccessResultDto> ManualAsync(ManualAccessRequest req, CancellationToken ct = default)
    {
        if (req.PersonType == PersonType.Guest) throw new DomainException("Use o registro de convidados.", "tipo_invalido", 400);
        var (name, photo, responsibleId, responsibleName) = await PersonAsync(req.PersonType, req.PersonId, ct);
        if (name is null) throw DomainException.NotFound("Pessoa");
        ctx.Justification = req.Justification;
        var origin = "manual: " + req.Justification;
        var reason = await EligibilityAsync(req.PersonType, req.PersonId, req.Operation, ct);
        if (reason is not null) return await DenyAsync(req.PersonType, req.PersonId, name, responsibleId, req.Operation, reason, null, origin, ct, photo, responsibleName);
        return await RegisterAsync(req.PersonType, req.PersonId, name, photo, responsibleId, responsibleName, req.Operation, null, origin, ct);
    }

    public async Task<AccessResultDto> GuestAsync(Guid guestId, AccessOperation operation, CancellationToken ct = default)
    {
        var guest = await db.Set<Guest>().Include(g => g.Member).Where(g => g.Id == guestId).FirstOr404Async("Convidado", ct);
        Task<AccessResultDto> Deny(string reason) => DenyAsync(PersonType.Guest, guest.Id, guest.FullName, guest.MemberId, operation, reason, null, "convidado", ct, null, guest.Member.FullName);

        if (operation == AccessOperation.Entry)
        {
            var s = await settings.LoadAsync(ct);
            if (guest.IsCancelled) return await Deny("Convite cancelado.");
            if (guest.VisitDate != await clock.TodayAsync(ct)) return await Deny("O convite não é válido para a data de hoje.");
            if (guest.Authorization != GuestAuthorization.Authorized) return await Deny("Convidado sem autorização da administração.");
            if (guest.Member.Status != MemberStatus.Active) return await Deny("O associado responsável não está com o cadastro ativo.");
            if ((await delinquency.GetAsync(guest.MemberId, ct)).AccessBlocked) return await Deny("Associado responsável com pendências financeiras.");
            if (s.GuestDocumentRequired == true && string.IsNullOrWhiteSpace(guest.Document)) return await Deny("Documento do convidado não informado.");
            if (s.GuestEntryWithoutMemberAllowed == false && !await db.Set<Presence>().AnyAsync(p => p.PersonType == PersonType.Member && p.PersonId == guest.MemberId, ct))
                return await Deny("O associado responsável precisa estar presente no clube.");
        }
        var result = await RegisterAsync(PersonType.Guest, guest.Id, guest.FullName, null, guest.MemberId, guest.Member.FullName, operation, null, "convidado", ct);
        if (result.Authorized)
        {
            if (operation == AccessOperation.Entry) guest.EnteredAt = result.At; else guest.ExitedAt = result.At;
            await db.SaveChangesAsync(ct);
        }
        return result;
    }

    private async Task<string?> EligibilityAsync(PersonType type, Guid personId, AccessOperation operation, CancellationToken ct)
    {
        var inside = await db.Set<Presence>().AnyAsync(p => p.PersonType == type && p.PersonId == personId, ct);
        if (operation == AccessOperation.Exit) return inside ? null : "Não há entrada registrada para esta pessoa.";
        if (inside) return "Entrada já registrada. Registre a saída antes de uma nova entrada.";

        Guid memberId;
        if (type == PersonType.Member)
        {
            var status = await db.Set<Member>().Where(m => m.Id == personId).Select(m => (MemberStatus?)m.Status).FirstOrDefaultAsync(ct);
            if (status != MemberStatus.Active) return status == MemberStatus.Suspended ? "Associado suspenso." : "Cadastro de associado não está ativo.";
            memberId = personId;
        }
        else
        {
            var d = await db.Set<Dependent>().Where(x => x.Id == personId).Select(x => new { x.Status, x.MemberId, MemberStatus = x.Member.Status }).FirstOrDefaultAsync(ct);
            if (d is null || d.Status != DependentStatus.Active) return d?.Status == DependentStatus.Suspended ? "Dependente suspenso." : "Cadastro de dependente não está ativo.";
            if (d.MemberStatus != MemberStatus.Active) return "O associado responsável não está com o cadastro ativo.";
            memberId = d.MemberId;
        }
        return (await delinquency.GetAsync(memberId, ct)).AccessBlocked ? "Acesso bloqueado por pendências financeiras. Procure a secretaria." : null;
    }

    private async Task<AccessResultDto> RegisterAsync(PersonType type, Guid personId, string? name, Guid? photo, Guid? responsibleId, string? responsibleName,
        AccessOperation operation, Guid? tokenId, string? origin, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (operation == AccessOperation.Entry)
        {
            db.Set<Presence>().Add(new Presence { PersonType = type, PersonId = personId, PersonName = name ?? "", ResponsibleMemberId = responsibleId, EnteredAt = now });
        }
        else
        {
            var removed = await db.Set<Presence>().Where(p => p.PersonType == type && p.PersonId == personId).ExecuteDeleteAsync(ct);
            if (removed == 0) return await DenyAsync(type, personId, name, responsibleId, operation, "Não há entrada registrada para esta pessoa.", tokenId, origin, ct, photo, responsibleName);
        }
        db.Set<AccessLog>().Add(new AccessLog
        {
            PersonType = type, PersonId = personId, PersonName = name, ResponsibleMemberId = responsibleId, Operation = operation,
            Result = AccessResult.Authorized, StaffUserId = ctx.UserId, Origin = Trim(origin), QrTokenId = tokenId, OccurredAt = now,
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            // Índice único de presença: outra requisição registrou a mesma entrada no mesmo instante.
            foreach (var entry in db.Set<Presence>().Local.ToList()) db.Set<Presence>().Entry(entry).State = EntityState.Detached;
            foreach (var entry in db.Set<AccessLog>().Local.ToList()) db.Set<AccessLog>().Entry(entry).State = EntityState.Detached;
            return await DenyAsync(type, personId, name, responsibleId, operation, "Entrada já registrada. Registre a saída antes de uma nova entrada.", tokenId, origin, ct, photo, responsibleName);
        }
        await realtime.PublishAsync(ctx.RequireClub(), "occupancyChanged", new { operation, personType = type }, ct);
        var message = operation == AccessOperation.Entry ? "Entrada autorizada." : "Saída registrada.";
        return new AccessResultDto(true, message, operation, type, personId, name, photo, responsibleName, now);
    }

    private async Task<AccessResultDto> DenyAsync(PersonType? type, Guid? personId, string? name, Guid? responsibleId, AccessOperation operation, string reason,
        Guid? tokenId, string? origin, CancellationToken ct, Guid? photo = null, string? responsibleName = null)
    {
        var now = DateTime.UtcNow;
        db.Set<AccessLog>().Add(new AccessLog
        {
            PersonType = type, PersonId = personId, PersonName = name, ResponsibleMemberId = responsibleId, Operation = operation,
            Result = AccessResult.Denied, DenialReason = reason, StaffUserId = ctx.UserId, Origin = Trim(origin), QrTokenId = tokenId, OccurredAt = now,
        });
        await db.SaveChangesAsync(ct);
        return new AccessResultDto(false, reason, operation, type, personId, name, photo, responsibleName, now);
    }

    private async Task<(string? Name, Guid? Photo, Guid? ResponsibleId, string? ResponsibleName)> PersonAsync(PersonType type, Guid id, CancellationToken ct)
    {
        if (type == PersonType.Member)
        {
            var m = await db.Set<Member>().Where(x => x.Id == id).Select(x => new { x.FullName, x.PhotoFileId }).FirstOrDefaultAsync(ct);
            return (m?.FullName, m?.PhotoFileId, null, null);
        }
        var d = await db.Set<Dependent>().Where(x => x.Id == id).Select(x => new { x.FullName, x.PhotoFileId, x.MemberId, Member = x.Member.FullName }).FirstOrDefaultAsync(ct);
        return (d?.FullName, d?.PhotoFileId, d?.MemberId, d?.Member);
    }

    /// <summary>Busca de pessoas para a portaria: devolve apenas o necessário para identificar (LGPD).</summary>
    public async Task<List<GatePersonDto>> SearchAsync(string term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 3) return [];
        var like = term.Like();
        var inside = await db.Set<Presence>().Select(p => p.PersonId).ToListAsync(ct);
        var members = await db.Set<Member>().Where(m => EF.Functions.ILike(m.FullName, like)).OrderBy(m => m.FullName).Take(15)
            .Select(m => new { m.Id, m.FullName, m.Cpf, m.PhotoFileId, m.Status }).ToListAsync(ct);
        var dependents = await db.Set<Dependent>().Where(d => EF.Functions.ILike(d.FullName, like) && d.Status != DependentStatus.Removed).OrderBy(d => d.FullName).Take(15)
            .Select(d => new { d.Id, d.FullName, d.Cpf, d.PhotoFileId, d.Status, Member = d.Member.FullName }).ToListAsync(ct);
        return members.Select(m => new GatePersonDto(PersonType.Member, m.Id, m.FullName, Cpf.Mask(m.Cpf), m.PhotoFileId, m.Status.ToString(), null, inside.Contains(m.Id)))
            .Concat(dependents.Select(d => new GatePersonDto(PersonType.Dependent, d.Id, d.FullName, Cpf.Mask(d.Cpf), d.PhotoFileId, d.Status.ToString(), d.Member, inside.Contains(d.Id))))
            .OrderBy(p => p.Name).ToList();
    }

    public async Task<List<PresenceDto>> PresentAsync(CancellationToken ct = default)
        => await (from p in db.Set<Presence>()
                  join m in db.Set<Member>() on p.ResponsibleMemberId equals m.Id into responsible
                  from m in responsible.DefaultIfEmpty()
                  orderby p.EnteredAt descending
                  select new PresenceDto(p.Id, p.PersonType, p.PersonId, p.PersonName, p.ResponsibleMemberId, m.FullName, p.EnteredAt)).ToListAsync(ct);

    public async Task<OccupancyDto> OccupancyAsync(int days = 30, CancellationToken ct = default)
    {
        var counts = await db.Set<Presence>().GroupBy(p => p.PersonType).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        int Of(PersonType t) => counts.FirstOrDefault(c => c.Key == t)?.Count ?? 0;

        var since = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 365));
        var entries = await db.Set<AccessLog>().Where(l => l.Result == AccessResult.Authorized && l.Operation == AccessOperation.Entry && l.OccurredAt >= since)
            .Select(l => l.OccurredAt).ToListAsync(ct);
        var zone = await clock.ZoneAsync(ct);
        var peaks = entries.GroupBy(e => TimeZoneInfo.ConvertTimeFromUtc(e, zone).Hour).Select(g => new HourCount(g.Key, g.Count())).OrderBy(h => h.Hour).ToList();
        return new OccupancyDto(Of(PersonType.Member), Of(PersonType.Dependent), Of(PersonType.Guest), counts.Sum(c => c.Count), peaks);
    }

    public async Task<PagedResult<AccessLogDto>> HistoryAsync(AccessLogQuery query, CancellationToken ct = default)
    {
        var q = db.Set<AccessLog>().AsNoTracking();
        if (query.Result is not null) q = q.Where(l => l.Result == query.Result);
        if (query.Operation is not null) q = q.Where(l => l.Operation == query.Operation);
        if (query.From is not null) q = q.Where(l => l.OccurredAt >= query.From.Value.ToUniversalTime());
        if (query.To is not null) q = q.Where(l => l.OccurredAt <= query.To.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(query.Search)) { var like = query.Search.Like(); q = q.Where(l => EF.Functions.ILike(l.PersonName!, like)); }
        return await (from l in q
                      join m in db.Set<Member>() on l.ResponsibleMemberId equals m.Id into responsible
                      from m in responsible.DefaultIfEmpty()
                      join u in db.Set<User>() on l.StaffUserId equals u.Id into staff
                      from u in staff.DefaultIfEmpty()
                      orderby l.OccurredAt descending
                      select new AccessLogDto(l.Id, l.PersonType, l.PersonId, l.PersonName, m.FullName, l.Operation, l.Result, l.DenialReason, u.Name, l.Origin, l.OccurredAt))
            .ToPagedAsync(query, ct);
    }

    private static string? Trim(string? value) => value is { Length: > 300 } ? value[..300] : value;
}
