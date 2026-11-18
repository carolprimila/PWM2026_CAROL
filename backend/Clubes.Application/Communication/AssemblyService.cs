using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Communication;

public record AssemblyDto(Guid Id, string Title, DateTime ScheduledAt, string? Location, string? Agenda, AssemblyStatus Status, string? Minutes, Guid? MinutesFileId,
    int Attendances, int Polls, DateTime CreatedAt);
public record SaveAssemblyRequest([Required, StringLength(150, MinimumLength = 3)] string Title, DateTime ScheduledAt, [StringLength(200)] string? Location, [StringLength(8000)] string? Agenda);
public record SetAssemblyStatusRequest(AssemblyStatus Status);
public record SaveMinutesRequest([StringLength(50000)] string? Minutes, Guid? MinutesFileId);
public record AttendanceDto(Guid Id, Guid MemberId, int MemberNumber, string MemberName, DateTime At);
public record PollOptionDto(Guid Id, string Text, int SortOrder, int? Votes);
public record PollDto(Guid Id, Guid? AssemblyId, string? AssemblyTitle, string Title, string? Description, DateTime? OpensAt, DateTime? ClosesAt, PollStatus Status,
    PollCriterion Criterion, bool ResultsPublished, IReadOnlyList<PollOptionDto> Options, int? TotalVotes, bool HasVoted, bool IsOpenNow);

public class SavePollRequest
{
    public Guid? AssemblyId { get; set; }
    [Required, StringLength(200, MinimumLength = 3)] public string Title { get; set; } = "";
    [StringLength(4000)] public string? Description { get; set; }
    public DateTime? OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public PollCriterion Criterion { get; set; }
    [Required, MinLength(2)] public List<string> Options { get; set; } = [];
}

public record VoteRequest(Guid OptionId);

public class AssemblyService(IAppDb db, RequestContext ctx, NotificationService notifications, DelinquencyService delinquency)
{
    private void EnsureNotDependent()
    {
        if (ctx.Kind == UserKind.Dependent) throw DomainException.Forbidden("Assembleias e votações são exclusivas dos associados.");
    }

    // ---------- Assembleias ----------

    public async Task<PagedResult<AssemblyDto>> ListAsync(PageQuery page, CancellationToken ct = default)
    {
        EnsureNotDependent();
        return await Project(db.Set<Assembly>().AsNoTracking().OrderByDescending(a => a.ScheduledAt)).ToPagedAsync(page, ct);
    }

    public async Task<AssemblyDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        EnsureNotDependent();
        return await Project(db.Set<Assembly>().AsNoTracking().Where(a => a.Id == id)).FirstOr404Async("Assembleia", ct);
    }

    private static IQueryable<AssemblyDto> Project(IQueryable<Assembly> q)
        => q.Select(a => new AssemblyDto(a.Id, a.Title, a.ScheduledAt, a.Location, a.Agenda, a.Status, a.Minutes, a.MinutesFileId, a.Attendances.Count, a.Polls.Count, a.CreatedAt));

    public async Task<AssemblyDto> SaveAsync(Guid? id, SaveAssemblyRequest req, CancellationToken ct = default)
    {
        Assembly a;
        if (id is null)
        {
            a = new Assembly { CreatedByUserId = ctx.RequireUser() };
            db.Set<Assembly>().Add(a);
        }
        else
        {
            a = await db.Set<Assembly>().Where(x => x.Id == id).FirstOr404Async("Assembleia", ct);
            if (a.Status is AssemblyStatus.Finished or AssemblyStatus.Cancelled) throw DomainException.Conflict("Assembleia encerrada não pode ser alterada.");
        }
        (a.Title, a.ScheduledAt, a.Location, a.Agenda) = (req.Title.Trim(), req.ScheduledAt.ToUniversalTime(), req.Location, req.Agenda);
        if (id is null)
            await notifications.ToAudienceAsync(true, false, false, NotificationCategory.Assembly, "assembly.scheduled", "Assembleia convocada",
                $"{a.Title}" + (a.Location is null ? "" : $" — {a.Location}") + ". Consulte a pauta no aplicativo.", nameof(Assembly), a.Id, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(a.Id, ct);
    }

    public async Task<AssemblyDto> SetStatusAsync(Guid id, SetAssemblyStatusRequest req, CancellationToken ct = default)
    {
        var a = await db.Set<Assembly>().Where(x => x.Id == id).FirstOr404Async("Assembleia", ct);
        var allowed = (a.Status, req.Status) switch
        {
            (AssemblyStatus.Scheduled, AssemblyStatus.InProgress or AssemblyStatus.Cancelled) => true,
            (AssemblyStatus.InProgress, AssemblyStatus.Finished) => true,
            _ => false,
        };
        if (!allowed) throw DomainException.Conflict("Mudança de situação não permitida.");
        if (req.Status == AssemblyStatus.Finished && await db.Set<Poll>().AnyAsync(p => p.AssemblyId == id && p.Status == PollStatus.Open, ct))
            throw DomainException.Conflict("Encerre as votações abertas antes de finalizar a assembleia.");
        db.AddHistory(ctx, nameof(Assembly), id, a.Status, req.Status);
        a.Status = req.Status;
        if (req.Status == AssemblyStatus.Cancelled)
            await notifications.ToAudienceAsync(true, false, false, NotificationCategory.Assembly, "assembly.cancelled", "Assembleia cancelada", $"A assembleia \"{a.Title}\" foi cancelada.", nameof(Assembly), id, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<AssemblyDto> SaveMinutesAsync(Guid id, SaveMinutesRequest req, CancellationToken ct = default)
    {
        var a = await db.Set<Assembly>().Where(x => x.Id == id).FirstOr404Async("Assembleia", ct);
        (a.Minutes, a.MinutesFileId) = (req.Minutes, req.MinutesFileId);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<List<AttendanceDto>> AttendancesAsync(Guid id, CancellationToken ct = default)
        => await db.Set<AssemblyAttendance>().Where(x => x.AssemblyId == id).OrderBy(x => x.Member.FullName)
            .Select(x => new AttendanceDto(x.Id, x.MemberId, x.Member.Number, x.Member.FullName, x.CreatedAt)).ToListAsync(ct);

    public async Task<List<AttendanceDto>> AddAttendanceAsync(Guid id, Guid memberId, CancellationToken ct = default)
    {
        var a = await db.Set<Assembly>().Where(x => x.Id == id).FirstOr404Async("Assembleia", ct);
        if (a.Status is AssemblyStatus.Finished or AssemblyStatus.Cancelled) throw DomainException.Conflict("Assembleia encerrada.");
        var status = await db.Set<Member>().Where(m => m.Id == memberId).Select(m => (MemberStatus?)m.Status).FirstOrDefaultAsync(ct) ?? throw DomainException.NotFound("Associado");
        if (status != MemberStatus.Active) throw DomainException.Conflict("Somente associados ativos participam de assembleias.");
        if (!await db.Set<AssemblyAttendance>().AnyAsync(x => x.AssemblyId == id && x.MemberId == memberId, ct))
        {
            db.Set<AssemblyAttendance>().Add(new AssemblyAttendance { AssemblyId = id, MemberId = memberId, RegisteredByUserId = ctx.RequireUser() });
            await db.SaveChangesAsync(ct);
        }
        return await AttendancesAsync(id, ct);
    }

    public async Task RemoveAttendanceAsync(Guid attendanceId, CancellationToken ct = default)
    {
        var attendance = await db.Set<AssemblyAttendance>().Where(x => x.Id == attendanceId).FirstOr404Async("Presença", ct);
        if (await db.Set<Vote>().AnyAsync(v => v.MemberId == attendance.MemberId && v.Poll.AssemblyId == attendance.AssemblyId, ct))
            throw DomainException.Conflict("O associado já votou nesta assembleia; a presença não pode ser removida.");
        db.Set<AssemblyAttendance>().Remove(attendance);
        await db.SaveChangesAsync(ct);
    }

    // ---------- Votações ----------

    public async Task<List<PollDto>> PollsAsync(Guid? assemblyId, bool openOnly, CancellationToken ct = default)
    {
        EnsureNotDependent();
        var q = db.Set<Poll>().AsNoTracking().Include(p => p.Options).Include(p => p.Assembly).AsQueryable();
        if (assemblyId is not null) q = q.Where(p => p.AssemblyId == assemblyId);
        if (!ctx.IsStaff) q = q.Where(p => p.Status != PollStatus.Draft);
        if (openOnly) q = q.Where(p => p.Status == PollStatus.Open);
        return await MapAsync(await q.OrderByDescending(p => p.CreatedAt).Take(200).ToListAsync(ct), ct);
    }

    public async Task<PollDto> GetPollAsync(Guid id, CancellationToken ct = default)
    {
        EnsureNotDependent();
        var poll = await db.Set<Poll>().AsNoTracking().Include(p => p.Options).Include(p => p.Assembly).Where(p => p.Id == id && (ctx.IsStaff || p.Status != PollStatus.Draft))
            .FirstOr404Async("Votação", ct);
        return (await MapAsync([poll], ct))[0];
    }

    public async Task<PollDto> SavePollAsync(Guid? id, SavePollRequest req, CancellationToken ct = default)
    {
        var options = req.Options.Select(o => o.Trim()).Where(o => o.Length > 0).Distinct().ToList();
        if (options.Count < 2) throw Invalid("Informe ao menos duas opções.");
        if (req.OpensAt is not null && req.ClosesAt is not null && req.ClosesAt <= req.OpensAt) throw Invalid("O encerramento deve ser posterior à abertura.");
        if (req.Criterion == PollCriterion.PresentAtAssembly && req.AssemblyId is null) throw Invalid("O critério de presença exige uma assembleia vinculada.");
        if (req.AssemblyId is { } assemblyId) await db.Set<Assembly>().Where(a => a.Id == assemblyId).FirstOr404Async("Assembleia", ct);

        Poll poll;
        if (id is null) { poll = new Poll { CreatedByUserId = ctx.RequireUser() }; db.Set<Poll>().Add(poll); }
        else
        {
            poll = await db.Set<Poll>().Include(p => p.Options).Where(p => p.Id == id).FirstOr404Async("Votação", ct);
            if (poll.Status != PollStatus.Draft) throw DomainException.Conflict("Somente votações em rascunho podem ser editadas.");
            db.Set<PollOption>().RemoveRange(poll.Options);
        }
        (poll.AssemblyId, poll.Title, poll.Description, poll.Criterion) = (req.AssemblyId, req.Title.Trim(), req.Description, req.Criterion);
        (poll.OpensAt, poll.ClosesAt) = (req.OpensAt?.ToUniversalTime(), req.ClosesAt?.ToUniversalTime());
        for (var i = 0; i < options.Count; i++) db.Set<PollOption>().Add(new PollOption { Poll = poll, Text = options[i], SortOrder = i });
        await db.SaveChangesAsync(ct);
        return await GetPollAsync(poll.Id, ct);
    }

    public async Task<PollDto> OpenPollAsync(Guid id, CancellationToken ct = default)
    {
        var poll = await db.Set<Poll>().Where(p => p.Id == id).FirstOr404Async("Votação", ct);
        if (poll.Status != PollStatus.Draft) throw DomainException.Conflict("Esta votação já foi aberta.");
        if (poll.ClosesAt <= DateTime.UtcNow) throw DomainException.Conflict("O prazo de encerramento já passou.");
        poll.Status = PollStatus.Open;
        poll.OpensAt ??= DateTime.UtcNow;
        db.AddHistory(ctx, nameof(Poll), id, PollStatus.Draft, PollStatus.Open);
        await notifications.ToAudienceAsync(true, false, false, NotificationCategory.Assembly, "poll.opened", "Votação aberta", $"A votação \"{poll.Title}\" está aberta para os associados.", nameof(Poll), id, ct);
        await db.SaveChangesAsync(ct);
        return await GetPollAsync(id, ct);
    }

    public async Task<PollDto> ClosePollAsync(Guid id, bool publishResults, CancellationToken ct = default)
    {
        var poll = await db.Set<Poll>().Where(p => p.Id == id).FirstOr404Async("Votação", ct);
        if (poll.Status == PollStatus.Draft) throw DomainException.Conflict("A votação ainda não foi aberta.");
        if (poll.Status == PollStatus.Open)
        {
            poll.Status = PollStatus.Closed;
            poll.ClosesAt = DateTime.UtcNow;
            db.AddHistory(ctx, nameof(Poll), id, PollStatus.Open, PollStatus.Closed);
        }
        if (publishResults && !poll.ResultsPublished)
        {
            poll.ResultsPublished = true;
            await notifications.ToAudienceAsync(true, false, false, NotificationCategory.Assembly, "poll.results", "Resultado de votação", $"O resultado da votação \"{poll.Title}\" foi divulgado.", nameof(Poll), id, ct);
        }
        await db.SaveChangesAsync(ct);
        return await GetPollAsync(id, ct);
    }

    /// <summary>
    /// Registro de voto. Regras validadas no servidor: somente associados (nunca dependentes ou equipe),
    /// cadastro ativo, votação aberta e dentro do prazo, critério de participação e um único voto por
    /// associado — este último garantido também por índice único no banco.
    /// </summary>
    public async Task<PollDto> VoteAsync(Guid pollId, VoteRequest req, CancellationToken ct = default)
    {
        if (ctx.Kind != UserKind.Member || ctx.MemberId is not { } memberId)
            throw DomainException.Forbidden("Somente associados podem votar.");
        var status = await db.Set<Member>().Where(m => m.Id == memberId).Select(m => (MemberStatus?)m.Status).FirstOrDefaultAsync(ct);
        if (status != MemberStatus.Active) throw DomainException.Forbidden("Somente associados com cadastro ativo podem votar.");

        var poll = await db.Set<Poll>().Include(p => p.Options).Where(p => p.Id == pollId).FirstOr404Async("Votação", ct);
        var now = DateTime.UtcNow;
        if (poll.Status != PollStatus.Open || poll.OpensAt > now || poll.ClosesAt <= now) throw DomainException.Conflict("Esta votação não está aberta.");
        if (poll.Options.All(o => o.Id != req.OptionId)) throw Invalid("Opção inválida para esta votação.");
        switch (poll.Criterion)
        {
            case PollCriterion.NonDelinquentMembers when (await delinquency.GetAsync(memberId, ct)).IsDelinquent:
                throw DomainException.Forbidden("Esta votação é restrita a associados em dia com o clube.");
            case PollCriterion.PresentAtAssembly when !await db.Set<AssemblyAttendance>().AnyAsync(a => a.AssemblyId == poll.AssemblyId && a.MemberId == memberId, ct):
                throw DomainException.Forbidden("Esta votação é restrita aos associados presentes na assembleia.");
        }
        if (await db.Set<Vote>().AnyAsync(v => v.PollId == pollId && v.MemberId == memberId, ct)) throw DomainException.Conflict("Você já votou nesta votação.");

        db.Set<Vote>().Add(new Vote { PollId = pollId, PollOptionId = req.OptionId, MemberId = memberId, UserId = ctx.RequireUser() });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw DomainException.Conflict("Você já votou nesta votação."); }
        return await GetPollAsync(pollId, ct);
    }

    private async Task<List<PollDto>> MapAsync(List<Poll> polls, CancellationToken ct)
    {
        var ids = polls.Select(p => p.Id).ToList();
        var counts = await db.Set<Vote>().Where(v => ids.Contains(v.PollId)).GroupBy(v => new { v.PollId, v.PollOptionId })
            .Select(g => new { g.Key.PollId, g.Key.PollOptionId, Count = g.Count() }).ToListAsync(ct);
        var voted = ctx.MemberId is { } memberId
            ? (await db.Set<Vote>().Where(v => ids.Contains(v.PollId) && v.MemberId == memberId).Select(v => v.PollId).ToListAsync(ct)).ToHashSet()
            : [];
        var now = DateTime.UtcNow;
        return polls.Select(p =>
        {
            // O resultado parcial nunca é exposto aos associados: só após a divulgação pela administração.
            var showResults = ctx.IsStaff || p.ResultsPublished;
            return new PollDto(p.Id, p.AssemblyId, p.Assembly?.Title, p.Title, p.Description, p.OpensAt, p.ClosesAt, p.Status, p.Criterion, p.ResultsPublished,
                p.Options.OrderBy(o => o.SortOrder).Select(o => new PollOptionDto(o.Id, o.Text, o.SortOrder,
                    showResults ? counts.FirstOrDefault(c => c.PollId == p.Id && c.PollOptionId == o.Id)?.Count ?? 0 : null)).ToList(),
                showResults ? counts.Where(c => c.PollId == p.Id).Sum(c => c.Count) : null, voted.Contains(p.Id),
                p.Status == PollStatus.Open && !(p.OpensAt > now) && !(p.ClosesAt <= now));
        }).ToList();
    }

    private static DomainException Invalid(string message) => new(message, "votacao_invalida", 400);
}
