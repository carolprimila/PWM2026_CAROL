using System.Text.Json;
using Clubes.API.Infrastructure;
using Clubes.Application.Admin;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Application.Spaces;
using Clubes.Domain;
using Clubes.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using P = Clubes.Application.Common.Permissions;

namespace Clubes.API.Controllers;

[Route("api/financeiro")]
public class FinanceController(ChargeService charges, FinanceAdminService finance, DelinquencyService delinquency, RequestContext ctx) : ApiController
{
    /// <summary>Cobranças. A equipe vê todas (com filtros); o associado vê apenas as próprias.</summary>
    [HttpGet("cobrancas"), Permission(P.FinanceView, Self = true), Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
    public Task<PagedResult<ChargeDto>> Charges([FromQuery] ChargeQuery query, CancellationToken ct) => charges.ListAsync(query, ct);

    [HttpGet("cobrancas/{id:guid}"), Permission(P.FinanceView, Self = true), Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
    public Task<ChargeDetailDto> Charge(Guid id, CancellationToken ct) => charges.GetAsync(id, ct);

    [HttpPost("cobrancas"), Permission(P.FinanceCharges)]
    public Task<ChargeDetailDto> CreateCharge(CreateChargeRequest req, CancellationToken ct) => charges.CreateAsync(req, ct);

    /// <summary>Lança desconto, acréscimo, multa, juros ou taxa em uma cobrança em aberto, com justificativa auditada.</summary>
    [HttpPost("cobrancas/{id:guid}/ajustes"), Permission(P.FinanceAdjust)]
    public Task<ChargeDetailDto> Adjust(Guid id, AdjustChargeRequest req, CancellationToken ct) => charges.AdjustAsync(id, req, ct);

    [HttpPost("cobrancas/{id:guid}/cancelar"), Permission(P.FinanceCharges)]
    public Task<ChargeDetailDto> CancelCharge(Guid id, CancelRequest req, CancellationToken ct) => charges.CancelAsync(id, req, ct);

    /// <summary>Situação financeira do associado autenticado (pendências e bloqueios).</summary>
    [HttpGet("minha-situacao"), Kinds(UserKind.Member)]
    public Task<FinancialStatusDto> MyStatus(CancellationToken ct) => delinquency.GetAsync(ctx.RequireMember(), ct);

    [HttpGet("inadimplentes"), Permission(P.FinanceView)]
    public Task<List<DelinquentDto>> Delinquents(CancellationToken ct) => finance.DelinquentsAsync(ct);

    [HttpGet("liberacoes"), Permission(P.FinanceView)]
    public Task<List<ReleaseDto>> Releases([FromQuery] Guid? memberId, CancellationToken ct) => finance.ReleasesAsync(memberId, ct);

    /// <summary>Liberação excepcional de associado inadimplente, com justificativa e validade.</summary>
    [HttpPost("liberacoes"), Permission(P.FinanceRelease)]
    public Task<ReleaseDto> GrantRelease(GrantReleaseRequest req, CancellationToken ct) => finance.GrantReleaseAsync(req, ct);

    [HttpDelete("liberacoes/{id:guid}"), Permission(P.FinanceRelease)]
    public async Task<IActionResult> RevokeRelease(Guid id, CancellationToken ct) { await finance.RevokeReleaseAsync(id, ct); return NoContent(); }

    [HttpGet("lancamentos"), Permission(P.FinanceView)]
    public Task<PagedResult<FinancialEntryDto>> Entries([FromQuery] EntryQuery query, CancellationToken ct) => finance.EntriesAsync(query, ct);

    [HttpPost("lancamentos"), Permission(P.FinanceEntries)]
    public Task<FinancialEntryDto> CreateEntry(SaveEntryRequest req, CancellationToken ct) => finance.SaveEntryAsync(null, req, ct);

    [HttpPut("lancamentos/{id:guid}"), Permission(P.FinanceEntries)]
    public Task<FinancialEntryDto> UpdateEntry(Guid id, SaveEntryRequest req, CancellationToken ct) => finance.SaveEntryAsync(id, req, ct);

    [HttpPost("lancamentos/{id:guid}/pagar"), Permission(P.FinanceEntries)]
    public Task<FinancialEntryDto> PayEntry(Guid id, PayEntryRequest req, CancellationToken ct) => finance.PayEntryAsync(id, req, ct);

    [HttpPost("lancamentos/{id:guid}/cancelar"), Permission(P.FinanceEntries)]
    public Task<FinancialEntryDto> CancelEntry(Guid id, CancelRequest req, CancellationToken ct) => finance.CancelEntryAsync(id, req, ct);

    [HttpGet("fluxo-de-caixa"), Permission(P.FinanceView)]
    public Task<CashFlowDto> CashFlow([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct) => finance.CashFlowAsync(from, to, ct);

    [HttpGet("fechamentos"), Permission(P.FinanceView)]
    public Task<List<ClosingDto>> Closings(CancellationToken ct) => finance.ClosingsAsync(ct);

    [HttpPost("fechamentos"), Permission(P.FinanceClose)]
    public Task<ClosingDto> Close(CloseMonthRequest req, CancellationToken ct) => finance.CloseMonthAsync(req, ct);

    [HttpPost("fechamentos/{id:guid}/reabrir"), Permission(P.FinanceClose)]
    public async Task<IActionResult> Reopen(Guid id, CancelRequest req, CancellationToken ct) { await finance.ReopenMonthAsync(id, req, ct); return NoContent(); }
}

[Route("api/mensalidades")]
public class MonthlyFeesController(ChargeService charges) : ApiController
{
    /// <summary>
    /// Gera as mensalidades da competência para os associados ativos, conforme a mensalidade base e a
    /// política de dependentes configuradas. Idempotente: não duplica mensalidades já existentes.
    /// </summary>
    [HttpPost("gerar"), Permission(P.FinanceCharges)]
    public Task<GenerateMonthlyResult> Generate(GenerateMonthlyRequest req, CancellationToken ct) => charges.GenerateMonthlyAsync(req, ct);

    [HttpGet, Permission(P.FinanceView, Self = true), Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
    public Task<PagedResult<ChargeDto>> List([FromQuery] ChargeQuery query, CancellationToken ct)
    {
        query.Type = ChargeType.Monthly;
        return charges.ListAsync(query, ct);
    }

    /// <summary>Recalcula multa e juros das cobranças vencidas (também executado automaticamente).</summary>
    [HttpPost("atualizar-encargos"), Permission(P.FinanceAdjust)]
    public async Task<object> ApplyLateFees(CancellationToken ct) => new { updated = await charges.ApplyLateFeesAsync(null, ct) };
}

public record SandboxConfirmRequest(bool Paid = true);

[Route("api/pagamentos")]
public class PaymentsController(PaymentService payments, ReportService reports, IPaymentGateway gateway) : ApiController
{
    [HttpGet, Permission(P.FinanceView, Self = true), Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
    public Task<PagedResult<PaymentDto>> List([FromQuery] PaymentQuery query, CancellationToken ct) => payments.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.FinanceView, Self = true), Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
    public Task<PaymentDto> Get(Guid id, CancellationToken ct) => payments.GetAsync(id, ct);

    /// <summary>Registra pagamento recebido na secretaria. Exige chave de idempotência: repetir a requisição não duplica o pagamento.</summary>
    [HttpPost, Permission(P.FinancePayments)]
    public Task<PaymentDto> Register(RegisterPaymentRequest req, CancellationToken ct) => payments.RegisterAsync(req, ct);

    /// <summary>Inicia o pagamento pelo aplicativo no gateway. A confirmação ocorre pela notificação do provedor.</summary>
    [HttpPost("app"), Kinds(UserKind.Member)]
    public Task<AppPaymentDto> StartApp(StartAppPaymentRequest req, CancellationToken ct) => payments.StartAppPaymentAsync(req, ct);

    [HttpPost("{id:guid}/estornar"), Permission(P.FinanceRefund)]
    public Task<PaymentDto> Refund(Guid id, RefundRequest req, CancellationToken ct) => payments.RefundAsync(id, req, ct);

    /// <summary>Comprovante de pagamento em PDF.</summary>
    [HttpGet("{id:guid}/comprovante"), Permission(P.FinanceView, Self = true), Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin), Produces("application/pdf")]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken ct) => File(await reports.ReceiptPdfAsync(id, ct), "application/pdf", $"comprovante-{id:N}.pdf");

    /// <summary>Notificação (webhook) do gateway de pagamento. A autenticidade é verificada pela assinatura do provedor.</summary>
    [HttpPost("webhook"), AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        return await payments.HandleGatewayNotificationAsync(body, headers, ct) ? Ok() : BadRequest(new ApiError("notificacao_invalida", "Notificação rejeitada."));
    }

    /// <summary>Somente em homologação (gateway Sandbox): simula a confirmação do provedor para um pagamento pendente.</summary>
    [HttpPost("{id:guid}/sandbox"), Permission(P.FinancePayments)]
    public async Task<IActionResult> SandboxConfirm(Guid id, SandboxConfirmRequest req, [FromServices] IOptions<PaymentOptions> options, CancellationToken ct)
    {
        if (gateway is not SandboxPaymentGateway) return NotFound(new ApiError("nao_encontrado", "Recurso disponível apenas com o gateway de homologação."));
        await payments.GetAsync(id, ct);
        var body = JsonSerializer.Serialize(new { reference = "sbx_" + id.ToString("N"), paid = req.Paid });
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [SandboxPaymentGateway.SignatureHeader] = SandboxPaymentGateway.Sign(body, options.Value.SandboxSecret ?? "") };
        return await payments.HandleGatewayNotificationAsync(body, headers, ct) ? Ok(await payments.GetAsync(id, ct)) : BadRequest(new ApiError("notificacao_invalida", "Notificação rejeitada."));
    }
}

[Route("api/espacos")]
public class SpacesController(SpaceService spaces) : ApiController
{
    /// <summary>Espaços do clube (visível a todos os usuários autenticados do clube).</summary>
    [HttpGet]
    public Task<List<SpaceDto>> List([FromQuery] bool includeInactive, [FromServices] RequestContext ctx, CancellationToken ct) => spaces.ListAsync(includeInactive && ctx.IsStaff, ct);

    [HttpGet("{id:guid}")]
    public Task<SpaceDto> Get(Guid id, CancellationToken ct) => spaces.GetAsync(id, ct);

    [HttpPost, Permission(P.SpacesManage)]
    public Task<SpaceDto> Create(SaveSpaceRequest req, CancellationToken ct) => spaces.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.SpacesManage)]
    public Task<SpaceDto> Update(Guid id, SaveSpaceRequest req, CancellationToken ct) => spaces.SaveAsync(id, req, ct);

    [HttpDelete("{id:guid}"), Permission(P.SpacesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await spaces.DeleteAsync(id, ct); return NoContent(); }

    /// <summary>Disponibilidade por horário em uma data (quadras e quiosques).</summary>
    [HttpGet("{id:guid}/disponibilidade")]
    public Task<DayAvailabilityDto> Day(Guid id, [FromQuery] DateOnly date, CancellationToken ct) => spaces.DayAsync(id, date, ct);

    /// <summary>Disponibilidade por data de início (espaços reservados por período, como o salão de festas).</summary>
    [HttpGet("{id:guid}/disponibilidade-periodo")]
    public Task<List<PeriodDayDto>> Period(Guid id, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct) => spaces.PeriodAsync(id, from, to, ct);

    [HttpGet("bloqueios"), Permission(P.SpacesView, P.SpacesManage, P.CalendarView)]
    public Task<List<BlockDto>> Blocks([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? spaceId, CancellationToken ct) => spaces.BlocksAsync(from, to, spaceId, ct);

    [HttpPost("bloqueios"), Permission(P.SpacesManage)]
    public Task<BlockResult> CreateBlock(CreateBlockRequest req, CancellationToken ct) => spaces.CreateBlockAsync(req, ct);

    [HttpDelete("bloqueios/{id:guid}"), Permission(P.SpacesManage)]
    public async Task<IActionResult> CancelBlock(Guid id, CancellationToken ct) { await spaces.CancelBlockAsync(id, ct); return NoContent(); }

    [HttpGet("feriados")]
    public Task<List<HolidayDto>> Holidays([FromQuery] int? year, CancellationToken ct) => spaces.HolidaysAsync(year, ct);

    [HttpPost("feriados"), Permission(P.SpacesManage, P.SettingsManage)]
    public Task<HolidayDto> CreateHoliday(SaveHolidayRequest req, CancellationToken ct) => spaces.SaveHolidayAsync(null, req, ct);

    [HttpPut("feriados/{id:guid}"), Permission(P.SpacesManage, P.SettingsManage)]
    public Task<HolidayDto> UpdateHoliday(Guid id, SaveHolidayRequest req, CancellationToken ct) => spaces.SaveHolidayAsync(id, req, ct);

    [HttpDelete("feriados/{id:guid}"), Permission(P.SpacesManage, P.SettingsManage)]
    public async Task<IActionResult> DeleteHoliday(Guid id, CancellationToken ct) { await spaces.DeleteHolidayAsync(id, ct); return NoContent(); }
}

[Route("api/reservas")]
public class ReservationsController(ReservationService reservations) : ApiController
{
    /// <summary>Reservas. A equipe vê todas; associado vê as suas; dependente vê as que solicitou.</summary>
    [HttpGet, Permission(P.ReservationsView, Self = true)]
    public Task<PagedResult<ReservationDto>> List([FromQuery] ReservationQuery query, CancellationToken ct) => reservations.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.ReservationsView, Self = true)]
    public Task<ReservationDetailDto> Get(Guid id, CancellationToken ct) => reservations.GetAsync(id, ct);

    /// <summary>Simula a reserva: valida horários, capacidade e bloqueios e devolve o valor total com seus componentes.</summary>
    [HttpPost("simular"), Permission(P.ReservationsManage, Self = true)]
    public Task<QuoteDto> Quote(CreateReservationRequest req, CancellationToken ct) => reservations.QuoteAsync(req, ct);

    /// <summary>
    /// Solicita uma reserva. Valida situação financeira, permissões do dependente, funcionamento, bloqueios,
    /// capacidade e convidados. Conflitos de horário são impedidos por restrição no banco de dados.
    /// </summary>
    [HttpPost, Permission(P.ReservationsManage, Self = true)]
    public Task<ReservationDetailDto> Create(CreateReservationRequest req, CancellationToken ct) => reservations.CreateAsync(req, ct);

    [HttpPost("{id:guid}/aprovar"), Permission(P.ReservationsManage)]
    public Task<ReservationDetailDto> Approve(Guid id, DecisionRequest req, CancellationToken ct) => reservations.ApproveAsync(id, req, ct);

    [HttpPost("{id:guid}/recusar"), Permission(P.ReservationsManage)]
    public Task<ReservationDetailDto> Reject(Guid id, DecisionRequest req, CancellationToken ct) => reservations.RejectAsync(id, req, ct);

    [HttpPost("{id:guid}/cancelar"), Permission(P.ReservationsManage, Self = true)]
    public Task<ReservationDetailDto> Cancel(Guid id, DecisionRequest req, CancellationToken ct) => reservations.CancelAsync(id, req, ct);

    [HttpPost("{id:guid}/reagendar"), Permission(P.ReservationsManage)]
    public Task<ReservationDetailDto> Reschedule(Guid id, RescheduleRequest req, CancellationToken ct) => reservations.RescheduleAsync(id, req, ct);

    /// <summary>Registra comparecimento ou falta. Penalidade só é lançada se configurada pela administração.</summary>
    [HttpPost("{id:guid}/comparecimento"), Permission(P.ReservationsManage)]
    public Task<ReservationDetailDto> Attendance(Guid id, AttendanceRequest req, CancellationToken ct) => reservations.SetAttendanceAsync(id, req, ct);
}
