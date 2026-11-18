using System.Globalization;
using Clubes.Application.Access;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Domain;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Clubes.Application.Admin;

public record DashboardFinanceDto(decimal ReceivedThisMonth, decimal ExpensesThisMonth, decimal OpenAmount, decimal OverdueAmount, int DelinquentMembers);
public record DashboardReservationDto(Guid Id, string Code, string SpaceName, string? Who, DateTime StartAt, DateTime EndAt, ReservationStatus Status);
public record DashboardEventDto(Guid Id, string Title, DateTime StartAt, string? Location, int Registered);
public record DashboardAnnouncementDto(Guid Id, string Title, AnnouncementCategory Category, Priority Priority, DateTime? PublishedAt);
public record DashboardDto(int Members, int ActiveMembers, int PendingMembers, int Dependents, int PendingDependents, int PresentNow, DashboardFinanceDto? Finance,
    IReadOnlyList<DashboardReservationDto> ReservationsToday, int PendingReservations, IReadOnlyList<DashboardEventDto> UpcomingEvents,
    IReadOnlyList<DashboardAnnouncementDto> RecentAnnouncements, int OpenManifestations, int OpenOccurrences, int OpenMaintenances, int PendingGuests,
    int LowStockItems, int PendingSettings);

public record NamedCount(string Name, decimal Value);
public record MembersIndicators(int Total, int Active, int Suspended, int Pending, int Cancelled, int Dependents, int NewInPeriod, int CancelledInPeriod);
public record FinanceIndicators(decimal Revenue, decimal Expenses, decimal Net, decimal MonthlyReceived, decimal MonthlyPending, decimal Fines, decimal Fees,
    decimal OverdueAmount, int DelinquentMembers, decimal DelinquencyRate, IReadOnlyList<NamedCount> RevenueByCategory);
public record ReservationIndicators(int Total, int Approved, int Cancelled, int NoShows, decimal OccupancyRate, IReadOnlyList<NamedCount> BySpace,
    IReadOnlyList<NamedCount> ByHour, IReadOnlyList<NamedCount> RevenueBySpace);
public record OperationalIndicators(int OpenMaintenances, int MaintenancesInPeriod, decimal MaintenanceCost, int Occurrences, int Damages, int Guests, int Entries, int DeniedAccesses);
public record IndicatorsDto(DateOnly From, DateOnly To, MembersIndicators Members, FinanceIndicators? Finance, ReservationIndicators Reservations, OperationalIndicators Operational);

public class ReportService(IAppDb db, RequestContext ctx, ClubClock clock, PermissionService permissions, SettingsService settings, DelinquencyService delinquency)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<DashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var today = await clock.TodayAsync(ct);
        var dayStart = await clock.ToUtcAsync(today, 0, ct);
        var dayEnd = dayStart.AddDays(1);
        var monthStart = await clock.ToUtcAsync(new DateOnly(today.Year, today.Month, 1), 0, ct);
        var now = DateTime.UtcNow;

        DashboardFinanceDto? finance = null;
        if (await permissions.HasAsync(Permissions.FinanceView, ct))
        {
            var open = db.Set<Charge>().Where(c => c.Status == ChargeStatus.Open);
            finance = new DashboardFinanceDto(
                await db.Set<Payment>().Where(p => p.Status == PaymentStatus.Confirmed && p.PaidAt >= monthStart).SumAsync(p => p.Amount, ct),
                await db.Set<FinancialEntry>().Where(e => e.Kind == FinancialEntryKind.Expense && e.Status == FinancialEntryStatus.Paid && e.PaidOn >= new DateOnly(today.Year, today.Month, 1)).SumAsync(e => e.Amount, ct),
                await open.SumAsync(ChargeService.BalanceExpr, ct),
                await open.Where(c => c.DueDate < today).SumAsync(ChargeService.BalanceExpr, ct),
                await (await delinquency.DelinquentMemberIdsAsync(ct)).CountAsync(ct));
        }

        var members = await db.Set<Member>().GroupBy(m => m.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var dependents = await db.Set<Dependent>().GroupBy(d => d.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var reservationsToday = await db.Set<Reservation>().Where(r => r.StartAt < dayEnd && r.EndAt > dayStart && (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Pending))
            .OrderBy(r => r.StartAt).Take(12)
            .Select(r => new DashboardReservationDto(r.Id, r.Code, r.Space.Name, r.Member!.FullName ?? r.Title, r.StartAt, r.EndAt, r.Status)).ToListAsync(ct);
        var events = await db.Set<ClubEvent>().Where(e => e.Status == EventStatus.Scheduled && e.EndAt >= now).OrderBy(e => e.StartAt).Take(5)
            .Select(e => new DashboardEventDto(e.Id, e.Title, e.StartAt, e.Location, e.Registrations.Count(r => r.Status == RegistrationStatus.Registered))).ToListAsync(ct);
        var announcements = await db.Set<Announcement>().Where(a => a.PublishedAt != null).OrderByDescending(a => a.PublishedAt).Take(5)
            .Select(a => new DashboardAnnouncementDto(a.Id, a.Title, a.Category, a.Priority, a.PublishedAt)).ToListAsync(ct);

        return new DashboardDto(
            members.Where(m => m.Key != MemberStatus.Cancelled).Sum(m => m.Count),
            members.FirstOrDefault(m => m.Key == MemberStatus.Active)?.Count ?? 0,
            members.FirstOrDefault(m => m.Key == MemberStatus.PendingApproval)?.Count ?? 0,
            dependents.Where(d => d.Key is DependentStatus.Active or DependentStatus.Suspended).Sum(d => d.Count),
            dependents.FirstOrDefault(d => d.Key == DependentStatus.PendingApproval)?.Count ?? 0,
            await db.Set<Presence>().CountAsync(ct), finance, reservationsToday,
            await db.Set<Reservation>().CountAsync(r => r.Status == ReservationStatus.Pending && r.EndAt > now, ct), events, announcements,
            await db.Set<Manifestation>().CountAsync(m => m.Status != ManifestationStatus.Resolved && m.Status != ManifestationStatus.Refused, ct),
            await db.Set<Occurrence>().CountAsync(o => o.Status == OccurrenceStatus.Open || o.Status == OccurrenceStatus.UnderReview, ct),
            await db.Set<MaintenanceOrder>().CountAsync(m => m.Status != MaintenanceStatus.Completed && m.Status != MaintenanceStatus.Cancelled, ct),
            await db.Set<Guest>().CountAsync(g => g.Authorization == GuestAuthorization.Pending && !g.IsCancelled && g.VisitDate >= today, ct),
            await db.Set<StockItem>().CountAsync(i => i.Quantity <= i.MinQuantity, ct),
            (await settings.GetAsync(ct)).Pending.Count);
    }

    public async Task<IndicatorsDto> IndicatorsAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from || to.DayNumber - from.DayNumber > 800) throw new DomainException("Período inválido.", "periodo_invalido", 400);
        var fromUtc = await clock.ToUtcAsync(from, 0, ct);
        var toUtc = await clock.ToUtcAsync(to.AddDays(1), 0, ct);
        var today = await clock.TodayAsync(ct);
        var zone = await clock.ZoneAsync(ct);

        // Associados
        var byStatus = await db.Set<Member>().GroupBy(m => m.Status).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var membersIndicators = new MembersIndicators(byStatus.Values.Sum(), byStatus.GetValueOrDefault(MemberStatus.Active), byStatus.GetValueOrDefault(MemberStatus.Suspended),
            byStatus.GetValueOrDefault(MemberStatus.PendingApproval), byStatus.GetValueOrDefault(MemberStatus.Cancelled),
            await db.Set<Dependent>().CountAsync(d => d.Status == DependentStatus.Active || d.Status == DependentStatus.Suspended, ct),
            await db.Set<Member>().CountAsync(m => m.CreatedAt >= fromUtc && m.CreatedAt < toUtc, ct),
            await db.Set<StatusHistory>().CountAsync(h => h.EntityType == nameof(Member) && h.ToStatus == nameof(MemberStatus.Cancelled) && h.CreatedAt >= fromUtc && h.CreatedAt < toUtc, ct));

        // Financeiro (somente para quem pode consultar o financeiro)
        FinanceIndicators? finance = null;
        if (await permissions.HasAsync(Permissions.FinanceView, ct))
        {
            var payments = await db.Set<Payment>().Where(p => p.Status == PaymentStatus.Confirmed && p.PaidAt >= fromUtc && p.PaidAt < toUtc)
                .Select(p => new { p.Amount, p.Charge.Type }).ToListAsync(ct);
            var otherRevenue = await db.Set<FinancialEntry>().Where(e => e.Kind == FinancialEntryKind.Revenue && e.Status == FinancialEntryStatus.Paid && e.PaidOn >= from && e.PaidOn <= to)
                .GroupBy(e => e.Category).Select(g => new NamedCount(g.Key, g.Sum(e => e.Amount))).ToListAsync(ct);
            var expenses = await db.Set<FinancialEntry>().Where(e => e.Kind == FinancialEntryKind.Expense && e.Status == FinancialEntryStatus.Paid && e.PaidOn >= from && e.PaidOn <= to).SumAsync(e => e.Amount, ct);
            var revenue = payments.Sum(p => p.Amount) + otherRevenue.Sum(r => r.Value);
            var periodCharges = db.Set<Charge>().Where(c => c.Status != ChargeStatus.Cancelled && c.DueDate >= from && c.DueDate <= to);
            var delinquentCount = await (await delinquency.DelinquentMemberIdsAsync(ct)).CountAsync(ct);
            var active = byStatus.GetValueOrDefault(MemberStatus.Active) + byStatus.GetValueOrDefault(MemberStatus.Suspended);
            finance = new FinanceIndicators(revenue, expenses, revenue - expenses,
                payments.Where(p => p.Type == ChargeType.Monthly).Sum(p => p.Amount),
                await periodCharges.Where(c => c.Type == ChargeType.Monthly && c.Status == ChargeStatus.Open).SumAsync(ChargeService.BalanceExpr, ct),
                await periodCharges.SumAsync(c => c.FineAmount + c.InterestAmount, ct) + await periodCharges.Where(c => c.Type == ChargeType.Fine || c.Type == ChargeType.Penalty).SumAsync(c => c.OriginalAmount, ct),
                await periodCharges.SumAsync(c => c.FeesAmount, ct) + await periodCharges.Where(c => c.Type == ChargeType.Fee || c.Type == ChargeType.Guest).SumAsync(c => c.OriginalAmount, ct),
                await db.Set<Charge>().Where(c => c.Status == ChargeStatus.Open && c.DueDate < today).SumAsync(ChargeService.BalanceExpr, ct),
                delinquentCount, active == 0 ? 0 : decimal.Round(delinquentCount * 100m / active, 1),
                payments.GroupBy(p => FinanceAdminService.ChargeTypeLabel(p.Type)).Select(g => new NamedCount(g.Key, g.Sum(p => p.Amount))).Concat(otherRevenue).OrderByDescending(c => c.Value).ToList());
        }

        // Reservas
        var reservations = await db.Set<Reservation>().Where(r => r.StartAt >= fromUtc && r.StartAt < toUtc)
            .Select(r => new { r.SpaceId, SpaceName = r.Space.Name, r.Status, r.Attendance, r.TotalAmount, Slots = r.Slots.Select(s => new { s.StartAt, s.EndAt, s.IsActive }).ToList() }).ToListAsync(ct);
        var used = reservations.Where(r => r.Status is ReservationStatus.Approved or ReservationStatus.Completed).ToList();
        var spaces = await db.Set<Space>().Include(s => s.Schedules).Where(s => s.IsReservable && s.IsActive).ToListAsync(ct);
        decimal availableHours = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
            foreach (var space in spaces)
                availableHours += space.BookingMode == BookingMode.MultiDay ? 24 : (space.Schedules.FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek) is { } sc ? (sc.CloseMinute - sc.OpenMinute) / 60m : 0);
        var usedHours = (decimal)used.SelectMany(r => r.Slots).Sum(s => (s.EndAt - s.StartAt).TotalHours);
        var bySpace = spaces.Select(s => new NamedCount(s.Name, used.Count(r => r.SpaceId == s.Id))).OrderByDescending(c => c.Value).ToList();
        var byHour = used.SelectMany(r => r.Slots).SelectMany(s => Enumerable.Range(0, (int)Math.Ceiling((s.EndAt - s.StartAt).TotalHours)).Select(h => TimeZoneInfo.ConvertTimeFromUtc(s.StartAt.AddHours(h), zone).Hour))
            .GroupBy(h => h).OrderBy(g => g.Key).Select(g => new NamedCount($"{g.Key:00}h", g.Count())).ToList();
        var reservationIndicators = new ReservationIndicators(reservations.Count, used.Count, reservations.Count(r => r.Status == ReservationStatus.Cancelled),
            reservations.Count(r => r.Attendance == AttendanceStatus.NoShow), availableHours == 0 ? 0 : decimal.Round(usedHours * 100 / availableHours, 1), bySpace, byHour,
            used.GroupBy(r => r.SpaceName).Select(g => new NamedCount(g.Key, g.Sum(r => r.TotalAmount))).Where(c => c.Value > 0).OrderByDescending(c => c.Value).ToList());

        // Operacional
        var operational = new OperationalIndicators(
            await db.Set<MaintenanceOrder>().CountAsync(m => m.Status != MaintenanceStatus.Completed && m.Status != MaintenanceStatus.Cancelled, ct),
            await db.Set<MaintenanceOrder>().CountAsync(m => m.OpenedAt >= fromUtc && m.OpenedAt < toUtc, ct),
            await db.Set<MaintenanceOrder>().Where(m => m.CompletedAt >= fromUtc && m.CompletedAt < toUtc).SumAsync(m => m.ActualCost ?? 0, ct),
            await db.Set<Occurrence>().CountAsync(o => o.OccurredAt >= fromUtc && o.OccurredAt < toUtc, ct),
            await db.Set<Occurrence>().CountAsync(o => o.Kind == OccurrenceKind.Damage && o.OccurredAt >= fromUtc && o.OccurredAt < toUtc, ct),
            await db.Set<Guest>().CountAsync(g => g.VisitDate >= from && g.VisitDate <= to && !g.IsCancelled, ct),
            await db.Set<AccessLog>().CountAsync(l => l.Result == AccessResult.Authorized && l.Operation == AccessOperation.Entry && l.OccurredAt >= fromUtc && l.OccurredAt < toUtc, ct),
            await db.Set<AccessLog>().CountAsync(l => l.Result == AccessResult.Denied && l.OccurredAt >= fromUtc && l.OccurredAt < toUtc, ct));

        return new IndicatorsDto(from, to, membersIndicators, finance, reservationIndicators, operational);
    }

    // ---------- Exportação ----------

    private record Section(string Title, List<(string Label, string Value)> Rows);

    private async Task<(string Club, List<Section> Sections, IndicatorsDto Data)> BuildAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var data = await IndicatorsAsync(from, to, ct);
        var clubId = ctx.RequireClub();
        var club = await db.Set<Club>().Where(c => c.Id == clubId).Select(c => c.Name).FirstAsync(ct);
        static string Money(decimal v) => v.ToString("C", PtBr);
        static string Number(decimal v) => v.ToString("N0", PtBr);
        var sections = new List<Section>
        {
            new("Associados",
            [
                ("Total de associados", Number(data.Members.Total)), ("Ativos", Number(data.Members.Active)), ("Suspensos", Number(data.Members.Suspended)),
                ("Aguardando aprovação", Number(data.Members.Pending)), ("Cancelados", Number(data.Members.Cancelled)), ("Dependentes", Number(data.Members.Dependents)),
                ("Novos cadastros no período", Number(data.Members.NewInPeriod)), ("Cancelamentos no período", Number(data.Members.CancelledInPeriod)),
            ]),
        };
        if (data.Finance is { } f)
        {
            var rows = new List<(string, string)>
            {
                ("Receitas", Money(f.Revenue)), ("Despesas", Money(f.Expenses)), ("Resultado", Money(f.Net)), ("Mensalidades recebidas", Money(f.MonthlyReceived)),
                ("Mensalidades pendentes", Money(f.MonthlyPending)), ("Multas e juros", Money(f.Fines)), ("Taxas", Money(f.Fees)), ("Valor em atraso", Money(f.OverdueAmount)),
                ("Associados inadimplentes", Number(f.DelinquentMembers)), ("Taxa de inadimplência", $"{f.DelinquencyRate.ToString("N1", PtBr)}%"),
            };
            rows.AddRange(f.RevenueByCategory.Select(c => ($"Receita — {c.Name}", Money(c.Value))));
            sections.Add(new Section("Financeiro", rows));
        }
        var r = data.Reservations;
        var reservationRows = new List<(string, string)>
        {
            ("Reservas no período", Number(r.Total)), ("Aprovadas/realizadas", Number(r.Approved)), ("Cancelamentos", Number(r.Cancelled)),
            ("Não comparecimentos", Number(r.NoShows)), ("Taxa de ocupação", $"{r.OccupancyRate.ToString("N1", PtBr)}%"),
        };
        reservationRows.AddRange(r.BySpace.Select(s => ($"Reservas — {s.Name}", Number(s.Value))));
        reservationRows.AddRange(r.ByHour.Select(h => ($"Horário {h.Name}", Number(h.Value))));
        reservationRows.AddRange(r.RevenueBySpace.Select(s => ($"Receita — {s.Name}", Money(s.Value))));
        sections.Add(new Section("Reservas", reservationRows));
        var o = data.Operational;
        sections.Add(new Section("Operacional",
        [
            ("Manutenções abertas", Number(o.OpenMaintenances)), ("Manutenções abertas no período", Number(o.MaintenancesInPeriod)), ("Custo de manutenção", Money(o.MaintenanceCost)),
            ("Ocorrências", Number(o.Occurrences)), ("Danos", Number(o.Damages)), ("Convidados", Number(o.Guests)), ("Entradas na portaria", Number(o.Entries)),
            ("Acessos negados", Number(o.DeniedAccesses)),
        ]));
        return (club, sections, data);
    }

    public async Task<byte[]> ExcelAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var (club, sections, _) = await BuildAsync(from, to, ct);
        using var workbook = new XLWorkbook();
        foreach (var section in sections)
        {
            var sheet = workbook.Worksheets.Add(section.Title);
            sheet.Cell(1, 1).Value = $"{club} — {section.Title}";
            sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
            sheet.Cell(2, 1).Value = $"Período: {from:dd/MM/yyyy} a {to:dd/MM/yyyy}";
            sheet.Cell(4, 1).Value = "Indicador";
            sheet.Cell(4, 2).Value = "Valor";
            sheet.Range(4, 1, 4, 2).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8EEF5"));
            for (var i = 0; i < section.Rows.Count; i++)
            {
                sheet.Cell(5 + i, 1).Value = section.Rows[i].Label;
                sheet.Cell(5 + i, 2).Value = section.Rows[i].Value;
                sheet.Cell(5 + i, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }
            sheet.Columns().AdjustToContents();
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> PdfAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var (club, sections, _) = await BuildAsync(from, to, ct);
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(10));
            page.Header().Column(col =>
            {
                col.Item().Text(club).FontSize(18).SemiBold();
                col.Item().Text($"Relatório de indicadores — {from:dd/MM/yyyy} a {to:dd/MM/yyyy}").FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
            page.Content().PaddingTop(12).Column(col =>
            {
                foreach (var section in sections)
                {
                    col.Item().PaddingTop(10).Text(section.Title).FontSize(13).SemiBold();
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); });
                        foreach (var (label, value) in section.Rows)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).Text(label);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingVertical(3).AlignRight().Text(value);
                        }
                    });
                }
            });
            page.Footer().AlignCenter().Text(t => { t.Span($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} · página "); t.CurrentPageNumber(); t.Span(" de "); t.TotalPages(); });
        })).GeneratePdf();
    }

    /// <summary>Comprovante de pagamento em PDF (acessível ao próprio associado e à equipe financeira).</summary>
    public async Task<byte[]> ReceiptPdfAsync(Guid paymentId, CancellationToken ct = default)
    {
        var p = await db.Set<Payment>().Where(x => x.Id == paymentId)
            .Select(x => new { x.ReceiptNumber, x.Amount, x.Method, x.Channel, x.PaidAt, x.Status, x.MemberId, Member = x.Charge.Member.FullName, x.Charge.Member.Number, x.Charge.Description, x.Charge.Competence })
            .FirstOr404Async("Pagamento", ct);
        if (!ctx.IsStaff && p.MemberId != ctx.MemberId) throw DomainException.NotFound("Pagamento");
        if (p.Status is not (PaymentStatus.Confirmed or PaymentStatus.Refunded)) throw DomainException.Conflict("O pagamento ainda não foi confirmado.");
        var clubId = ctx.RequireClub();
        var club = await db.Set<Club>().Where(c => c.Id == clubId).Select(c => new { c.Name, c.Cnpj }).FirstAsync(ct);
        var paidAt = p.PaidAt is { } at ? await clock.ToLocalAsync(at, ct) : (DateTime?)null;
        var method = p.Method switch
        {
            PaymentMethod.Cash => "Dinheiro", PaymentMethod.Pix => "Pix", PaymentMethod.CreditCard => "Cartão de crédito", PaymentMethod.DebitCard => "Cartão de débito",
            PaymentMethod.BankSlip => "Boleto", _ => "Transferência",
        };
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(32);
            page.DefaultTextStyle(t => t.FontSize(11));
            page.Content().Column(col =>
            {
                col.Spacing(6);
                col.Item().Text(club.Name).FontSize(18).SemiBold();
                if (club.Cnpj is not null) col.Item().Text($"CNPJ {club.Cnpj}").FontColor(Colors.Grey.Darken1);
                col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().Text($"Comprovante de pagamento nº {p.ReceiptNumber}").FontSize(14).SemiBold();
                if (p.Status == PaymentStatus.Refunded) col.Item().Text("PAGAMENTO ESTORNADO").FontColor(Colors.Red.Medium).SemiBold();
                col.Item().PaddingTop(8).Text($"Associado: {p.Member} (matrícula {p.Number})");
                col.Item().Text($"Referente a: {p.Description}");
                col.Item().Text($"Competência: {p.Competence}");
                col.Item().Text($"Forma de pagamento: {method} ({(p.Channel == PaymentChannel.App ? "aplicativo" : "secretaria")})");
                col.Item().Text($"Data: {paidAt:dd/MM/yyyy HH:mm}");
                col.Item().PaddingTop(10).Text($"Valor pago: {p.Amount.ToString("C", PtBr)}").FontSize(16).SemiBold();
            });
        })).GeneratePdf();
    }
}
