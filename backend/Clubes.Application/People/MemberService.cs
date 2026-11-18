using System.ComponentModel.DataAnnotations;
using Clubes.Application.Access;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.People;

public record MemberListDto(Guid Id, int Number, string FullName, string? CpfMasked, string? Email, string? Phone, MemberStatus Status,
    int Dependents, bool IsDelinquent, Guid? PhotoFileId, DateOnly? JoinedAt, bool HasAccess);
public record MemberDto(Guid Id, int Number, string FullName, string Cpf, string? Rg, DateOnly? BirthDate, string? Gender, string? MaritalStatus,
    string? Email, string? Phone, string? ZipCode, string? Street, string? AddressNumber, string? Complement, string? District, string? City,
    string? State, Guid? PhotoFileId, DateOnly? JoinedAt, MemberStatus Status, string? Notes, DateTime CreatedAt, bool HasAccess, string? AccessEmail,
    FinancialStatusDto Financial, int Dependents);

public class SaveMemberRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string FullName { get; set; } = "";
    [Required] public string Cpf { get; set; } = "";
    [StringLength(20)] public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
    [StringLength(30)] public string? Gender { get; set; }
    [StringLength(30)] public string? MaritalStatus { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(9)] public string? ZipCode { get; set; }
    [StringLength(200)] public string? Street { get; set; }
    [StringLength(20)] public string? AddressNumber { get; set; }
    [StringLength(100)] public string? Complement { get; set; }
    [StringLength(100)] public string? District { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(2)] public string? State { get; set; }
    public Guid? PhotoFileId { get; set; }
    public DateOnly? JoinedAt { get; set; }
    [StringLength(4000)] public string? Notes { get; set; }
}

/// <summary>Dados que o próprio associado pode atualizar pelo aplicativo.</summary>
public class UpdateMyProfileRequest
{
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(9)] public string? ZipCode { get; set; }
    [StringLength(200)] public string? Street { get; set; }
    [StringLength(20)] public string? AddressNumber { get; set; }
    [StringLength(100)] public string? Complement { get; set; }
    [StringLength(100)] public string? District { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(2)] public string? State { get; set; }
    public Guid? PhotoFileId { get; set; }
}

public record StatusChangeRequest([Required, StringLength(500, MinimumLength = 3)] string Reason);
public record CreateAccessRequest([EmailAddress] string? Email);

public class MemberQuery : PageQuery
{
    public MemberStatus? Status { get; set; }
    public bool? Delinquent { get; set; }
}

public class MemberService(IAppDb db, RequestContext ctx, DelinquencyService delinquency, UserService users, NotificationService notifications, ClubClock clock)
{
    public async Task<PagedResult<MemberListDto>> ListAsync(MemberQuery query, CancellationToken ct = default)
    {
        var delinquentIds = await delinquency.DelinquentMemberIdsAsync(ct);
        var q = db.Set<Member>().AsNoTracking();
        if (query.Status is not null) q = q.Where(m => m.Status == query.Status);
        if (query.Delinquent is true) q = q.Where(m => delinquentIds.Contains(m.Id));
        if (query.Delinquent is false) q = q.Where(m => !delinquentIds.Contains(m.Id));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var like = query.Search.Like();
            var digits = query.Search.OnlyDigits();
            q = q.Where(m => EF.Functions.ILike(m.FullName, like) || EF.Functions.ILike(m.Email!, like)
                || (digits != "" && (m.Cpf.Contains(digits) || m.Number.ToString() == digits)));
        }
        q = query.Sort switch
        {
            "number" => query.Desc ? q.OrderByDescending(m => m.Number) : q.OrderBy(m => m.Number),
            "joinedAt" => query.Desc ? q.OrderByDescending(m => m.JoinedAt) : q.OrderBy(m => m.JoinedAt),
            "status" => query.Desc ? q.OrderByDescending(m => m.Status) : q.OrderBy(m => m.Status),
            _ => query.Desc ? q.OrderByDescending(m => m.FullName) : q.OrderBy(m => m.FullName),
        };
        var page = await q.Select(m => new
        {
            m.Id, m.Number, m.FullName, m.Cpf, m.Email, m.Phone, m.Status, m.PhotoFileId, m.JoinedAt,
            Dependents = m.Dependents.Count(d => d.Status == DependentStatus.Active || d.Status == DependentStatus.PendingApproval || d.Status == DependentStatus.Suspended),
            IsDelinquent = delinquentIds.Contains(m.Id),
            HasAccess = db.Set<User>().Any(u => u.MemberId == m.Id && u.Kind == UserKind.Member),
        }).ToPagedAsync(query, ct);
        return new PagedResult<MemberListDto>(page.Items.Select(m => new MemberListDto(m.Id, m.Number, m.FullName, Cpf.Mask(m.Cpf), m.Email, m.Phone,
            m.Status, m.Dependents, m.IsDelinquent, m.PhotoFileId, m.JoinedAt, m.HasAccess)).ToList(), page.Total, page.Page, page.PageSize);
    }

    public async Task<MemberDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var member = await db.Set<Member>().AsNoTracking().Where(m => m.Id == id).FirstOr404Async("Associado", ct);
        return await MapAsync(member, ct);
    }

    public Task<MemberDto> GetMineAsync(CancellationToken ct = default) => GetAsync(ctx.RequireMember(), ct);

    public async Task<MemberDto> CreateAsync(SaveMemberRequest req, CancellationToken ct = default)
    {
        var member = new Member { Number = (int)await db.NextSequenceAsync("member", ct) };
        await ApplyAsync(member, req, ct);
        db.Set<Member>().Add(member);
        db.AddHistory(ctx, nameof(Member), member.Id, null, member.Status, "Cadastro recebido");
        await db.SaveChangesAsync(ct);
        return await MapAsync(member, ct);
    }

    public async Task<MemberDto> UpdateAsync(Guid id, SaveMemberRequest req, CancellationToken ct = default)
    {
        var member = await db.Set<Member>().Where(m => m.Id == id).FirstOr404Async("Associado", ct);
        await ApplyAsync(member, req, ct);
        await db.SaveChangesAsync(ct);
        return await MapAsync(member, ct);
    }

    public async Task<MemberDto> UpdateMineAsync(UpdateMyProfileRequest req, CancellationToken ct = default)
    {
        var id = ctx.RequireMember();
        var member = await db.Set<Member>().Where(m => m.Id == id).FirstOr404Async("Associado", ct);
        (member.Phone, member.ZipCode, member.Street, member.AddressNumber) = (req.Phone, req.ZipCode, req.Street, req.AddressNumber);
        (member.Complement, member.District, member.City, member.State) = (req.Complement, req.District, req.City, req.State?.ToUpperInvariant());
        if (req.PhotoFileId is not null) member.PhotoFileId = req.PhotoFileId;
        await db.SaveChangesAsync(ct);
        return await MapAsync(member, ct);
    }

    public Task<MemberDto> ApproveAsync(Guid id, CancellationToken ct = default)
        => ChangeStatusAsync(id, MemberStatus.Active, "Cadastro aprovado", [MemberStatus.PendingApproval],
            "Cadastro aprovado", "Seu cadastro de associado foi aprovado. Bem-vindo(a) ao clube!", ct);

    public Task<MemberDto> SuspendAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
        => ChangeStatusAsync(id, MemberStatus.Suspended, req.Reason, [MemberStatus.Active],
            "Cadastro suspenso", "Seu cadastro foi suspenso pela administração. Procure a secretaria para mais informações.", ct);

    public Task<MemberDto> ReactivateAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
        => ChangeStatusAsync(id, MemberStatus.Active, req.Reason, [MemberStatus.Suspended, MemberStatus.Cancelled],
            "Cadastro reativado", "Seu cadastro de associado está ativo novamente.", ct);

    public Task<MemberDto> CancelAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
        => ChangeStatusAsync(id, MemberStatus.Cancelled, req.Reason, [MemberStatus.PendingApproval, MemberStatus.Active, MemberStatus.Suspended], null, null, ct);

    private async Task<MemberDto> ChangeStatusAsync(Guid id, MemberStatus to, string reason, MemberStatus[] allowedFrom, string? title, string? body, CancellationToken ct)
    {
        var member = await db.Set<Member>().Where(m => m.Id == id).FirstOr404Async("Associado", ct);
        if (!allowedFrom.Contains(member.Status))
            throw DomainException.Conflict("A situação atual do associado não permite esta operação.");
        var from = member.Status;
        member.Status = to;
        if (to == MemberStatus.Active) member.JoinedAt ??= await clock.TodayAsync(ct);
        ctx.Justification = reason;
        db.AddHistory(ctx, nameof(Member), member.Id, from, to, reason);
        if (title is not null) await notifications.ToMemberAsync(id, NotificationCategory.General, "member.status", title, body!, nameof(Member), id, true, ct);
        await db.SaveChangesAsync(ct);

        if (to == MemberStatus.Cancelled)
        {
            // Associado cancelado perde o acesso: sessões do titular e dos dependentes são encerradas.
            var userIds = await db.Set<User>().Where(u => u.MemberId == id || db.Set<Dependent>().Any(d => d.Id == u.DependentId && d.MemberId == id)).Select(u => u.Id).ToListAsync(ct);
            await db.Set<RefreshToken>().Where(t => userIds.Contains(t.UserId) && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow).SetProperty(t => t.RevokedReason, "associado_cancelado"), ct);
        }
        return await MapAsync(member, ct);
    }

    public async Task<TemporaryPasswordDto> CreateAccessAsync(Guid id, CreateAccessRequest req, CancellationToken ct = default)
    {
        var member = await db.Set<Member>().Where(m => m.Id == id).FirstOr404Async("Associado", ct);
        if (member.Status is MemberStatus.Cancelled or MemberStatus.PendingApproval)
            throw DomainException.Conflict("O acesso só pode ser criado para associados aprovados.");
        if (await db.Set<User>().AnyAsync(u => u.MemberId == id && u.Kind == UserKind.Member, ct))
            throw DomainException.Conflict("Este associado já possui acesso. Use a redefinição de senha.");
        var emailAddress = req.Email ?? member.Email ?? throw new DomainException("Informe um e-mail para criar o acesso.", "email_obrigatorio", 400);
        if (member.Email is null) member.Email = emailAddress.Trim().ToLowerInvariant();
        return await users.CreateAccessAsync(UserKind.Member, member.FullName, emailAddress, member.Id, null, null, ct);
    }

    /// <summary>
    /// LGPD — anonimização a pedido do titular ou ao fim da retenção. Só é permitida para associações canceladas
    /// e sem cobranças em aberto. Remove os dados pessoais do associado e de seus dependentes e encerra os logins;
    /// o histórico financeiro e de acessos é preservado sem identificação (obrigação legal e contábil).
    /// </summary>
    public async Task AnonymizeAsync(Guid id, StatusChangeRequest req, CancellationToken ct = default)
    {
        var member = await db.Set<Member>().Include(m => m.Dependents).Where(m => m.Id == id).FirstOr404Async("Associado", ct);
        if (member.Status != MemberStatus.Cancelled) throw DomainException.Conflict("Somente associações canceladas podem ser anonimizadas.");
        if (await db.Set<Charge>().AnyAsync(c => c.MemberId == id && c.Status == ChargeStatus.Open, ct))
            throw DomainException.Conflict("Há cobranças em aberto. Quite ou cancele as cobranças antes de anonimizar.");
        if (await db.Set<Presence>().AnyAsync(p => p.PersonId == id || p.ResponsibleMemberId == id, ct))
            throw DomainException.Conflict("Há registro de presença em aberto para este associado ou seus acompanhantes.");

        ctx.Justification = req.Reason;
        (member.FullName, member.Cpf, member.Rg, member.BirthDate, member.Gender, member.MaritalStatus) = ($"Titular anonimizado {member.Number}", $"ANON{member.Number:D7}", null, null, null, null);
        (member.Email, member.Phone, member.ZipCode, member.Street, member.AddressNumber, member.Complement) = (null, null, null, null, null, null);
        (member.District, member.City, member.State, member.PhotoFileId, member.Notes) = (null, null, null, null, null);
        var index = 0;
        foreach (var d in member.Dependents)
        {
            (d.FullName, d.Cpf, d.Rg, d.BirthDate, d.Gender, d.PhotoFileId, d.Email, d.Phone) = ($"Dependente anonimizado {member.Number}-{++index}", null, null, null, null, null, null, null);
            if (d.Status != DependentStatus.Removed) d.Status = DependentStatus.Removed;
        }
        var dependentIds = member.Dependents.Select(d => d.Id).ToList();
        var logins = await db.Set<User>().Where(u => u.MemberId == id || (u.DependentId != null && dependentIds.Contains(u.DependentId.Value))).ToListAsync(ct);
        foreach (var user in logins)
        {
            (user.Name, user.Email, user.IsBlocked, user.BlockReason) = ("Usuário anonimizado", $"anonimizado-{user.Id:N}@invalido.local", true, "Dados anonimizados (LGPD)");
            db.Set<User>().Remove(user);
        }
        var guests = await db.Set<Guest>().Where(g => g.MemberId == id).ToListAsync(ct);
        foreach (var g in guests) (g.FullName, g.Document, g.Phone) = ("Convidado anonimizado", null, null);
        db.AddHistory(ctx, nameof(Member), id, member.Status, "Anonimizado", req.Reason);
        await db.SaveChangesAsync(ct);

        var userIds = logins.Select(u => u.Id).ToList();
        await db.Set<RefreshToken>().Where(t => userIds.Contains(t.UserId)).ExecuteDeleteAsync(ct);
        await db.Set<DeviceToken>().Where(t => userIds.Contains(t.UserId)).ExecuteDeleteAsync(ct);
        await db.Set<AccessLog>().Where(l => l.PersonId == id || (l.PersonId != null && dependentIds.Contains(l.PersonId.Value)) || l.ResponsibleMemberId == id)
            .ExecuteUpdateAsync(u => u.SetProperty(l => l.PersonName, "Anonimizado"), ct);
    }

    /// <summary>LGPD — acesso do titular: cópia dos dados pessoais mantidos pelo clube sobre o associado autenticado.</summary>
    public async Task<object> ExportMineAsync(CancellationToken ct = default)
    {
        var id = ctx.RequireMember();
        var member = await GetAsync(id, ct);
        return new
        {
            GeradoEm = DateTime.UtcNow,
            Associado = member,
            Dependentes = await db.Set<Dependent>().Where(d => d.MemberId == id).Select(d => new { d.FullName, d.Cpf, d.Rg, d.BirthDate, d.Gender, d.Relationship, d.Email, d.Phone, d.Status }).ToListAsync(ct),
            Convidados = await db.Set<Guest>().Where(g => g.MemberId == id).Select(g => new { g.FullName, g.Document, g.Phone, g.VisitDate }).ToListAsync(ct),
            Cobrancas = await db.Set<Charge>().Where(c => c.MemberId == id).Select(c => new { c.Description, c.Competence, c.DueDate, c.OriginalAmount, c.PaidAmount, c.Status }).ToListAsync(ct),
            Pagamentos = await db.Set<Payment>().Where(p => p.MemberId == id).Select(p => new { p.ReceiptNumber, p.Amount, p.Method, p.PaidAt, p.Status }).ToListAsync(ct),
            Reservas = await db.Set<Reservation>().Where(r => r.MemberId == id).Select(r => new { r.Code, Espaco = r.Space.Name, r.StartAt, r.EndAt, r.Status }).ToListAsync(ct),
            Acessos = await db.Set<AccessLog>().Where(l => l.PersonType == PersonType.Member && l.PersonId == id).OrderByDescending(l => l.OccurredAt).Take(500).Select(l => new { l.Operation, l.Result, l.OccurredAt }).ToListAsync(ct),
            Consentimento = await db.Set<User>().Where(u => u.MemberId == id).Select(u => new { u.PrivacyAcceptedAt, u.PrivacyAcceptedVersion }).FirstOrDefaultAsync(ct),
        };
    }

    private async Task ApplyAsync(Member member, SaveMemberRequest req, CancellationToken ct)
    {
        var cpf = req.Cpf.OnlyDigits();
        if (!Cpf.IsValid(cpf)) throw new DomainException("CPF inválido.", "cpf_invalido", 400);
        if (await db.Set<Member>().AnyAsync(m => m.Cpf == cpf && m.Id != member.Id, ct)) throw DomainException.Conflict("Já existe um associado com este CPF.");
        if (req.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow)) throw new DomainException("Data de nascimento inválida.", "data_invalida", 400);

        member.FullName = req.FullName.Trim();
        member.Cpf = cpf;
        member.Rg = req.Rg;
        member.BirthDate = req.BirthDate;
        member.Gender = req.Gender;
        member.MaritalStatus = req.MaritalStatus;
        member.Email = req.Email?.Trim().ToLowerInvariant();
        member.Phone = req.Phone;
        member.ZipCode = req.ZipCode;
        member.Street = req.Street;
        member.AddressNumber = req.AddressNumber;
        member.Complement = req.Complement;
        member.District = req.District;
        member.City = req.City;
        member.State = req.State?.ToUpperInvariant();
        member.PhotoFileId = req.PhotoFileId;
        member.JoinedAt = req.JoinedAt ?? member.JoinedAt;
        member.Notes = req.Notes;
    }

    private async Task<MemberDto> MapAsync(Member m, CancellationToken ct)
    {
        var accessEmail = await db.Set<User>().Where(u => u.MemberId == m.Id && u.Kind == UserKind.Member).Select(u => u.Email).FirstOrDefaultAsync(ct);
        var dependents = await db.Set<Dependent>().CountAsync(d => d.MemberId == m.Id && d.Status != DependentStatus.Rejected && d.Status != DependentStatus.Removed, ct);
        // Observações internas não são expostas ao próprio associado.
        return new MemberDto(m.Id, m.Number, m.FullName, m.Cpf, m.Rg, m.BirthDate, m.Gender, m.MaritalStatus, m.Email, m.Phone, m.ZipCode, m.Street,
            m.AddressNumber, m.Complement, m.District, m.City, m.State, m.PhotoFileId, m.JoinedAt, m.Status, ctx.IsStaff ? m.Notes : null, m.CreatedAt,
            accessEmail is not null, accessEmail, await delinquency.GetAsync(m.Id, ct), dependents);
    }
}
