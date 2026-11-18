using Clubes.API.Infrastructure;
using Clubes.Application.Common;
using Clubes.Application.Communication;
using Clubes.Application.Operations;
using Clubes.Application.People;
using Clubes.Application.Spaces;
using Clubes.Domain;
using Microsoft.AspNetCore.Mvc;
using P = Clubes.Application.Common.Permissions;

namespace Clubes.API.Controllers;

[Route("api/eventos")]
public class EventsController(EventService events) : ApiController
{
    /// <summary>Eventos visíveis ao usuário conforme o público de cada evento.</summary>
    [HttpGet, Permission(P.EventsView, Self = true)]
    public Task<PagedResult<EventDto>> List([FromQuery] PageQuery page, [FromQuery] bool upcomingOnly, [FromQuery] EventStatus? status, CancellationToken ct)
        => events.ListAsync(page, upcomingOnly, status, ct);

    [HttpGet("{id:guid}"), Permission(P.EventsView, Self = true)]
    public Task<EventDto> Get(Guid id, CancellationToken ct) => events.GetAsync(id, ct);

    [HttpPost, Permission(P.EventsManage)]
    public Task<EventDto> Create(SaveEventRequest req, CancellationToken ct) => events.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.EventsManage)]
    public Task<EventDto> Update(Guid id, SaveEventRequest req, CancellationToken ct) => events.SaveAsync(id, req, ct);

    [HttpPost("{id:guid}/cancelar"), Permission(P.EventsManage)]
    public Task<EventDto> Cancel(Guid id, StatusChangeRequest req, CancellationToken ct) => events.CancelAsync(id, req, ct);

    [HttpPost("{id:guid}/encerrar"), Permission(P.EventsManage)]
    public Task<EventDto> Finish(Guid id, CancellationToken ct) => events.FinishAsync(id, ct);

    [HttpPost("{id:guid}/inscricao"), Kinds(UserKind.Member, UserKind.Dependent)]
    public Task<EventDto> Register(Guid id, CancellationToken ct) => events.RegisterAsync(id, ct);

    [HttpDelete("{id:guid}/inscricao"), Kinds(UserKind.Member, UserKind.Dependent)]
    public Task<EventDto> CancelRegistration(Guid id, CancellationToken ct) => events.CancelRegistrationAsync(id, ct);

    [HttpGet("{id:guid}/inscricoes"), Permission(P.EventsView)]
    public Task<List<EventRegistrationDto>> Registrations(Guid id, CancellationToken ct) => events.RegistrationsAsync(id, ct);

    [HttpPut("inscricoes/{registrationId:guid}/presenca"), Permission(P.EventsManage)]
    public async Task<IActionResult> SetAttendance(Guid registrationId, SetEventAttendanceRequest req, CancellationToken ct) { await events.SetAttendanceAsync(registrationId, req, ct); return NoContent(); }
}

[Route("api/comunicados")]
public class AnnouncementsController(AnnouncementService announcements) : ApiController
{
    /// <summary>Mural do usuário: somente comunicados publicados e destinados ao seu público.</summary>
    [HttpGet("mural")]
    public Task<PagedResult<AnnouncementDto>> Feed([FromQuery] PageQuery page, [FromQuery] AnnouncementCategory? category, CancellationToken ct) => announcements.FeedAsync(page, category, ct);

    [HttpGet, Permission(P.AnnouncementsManage)]
    public Task<PagedResult<AnnouncementDto>> List([FromQuery] PageQuery page, [FromQuery] AnnouncementCategory? category, CancellationToken ct) => announcements.ListAsync(page, category, ct);

    [HttpGet("{id:guid}")]
    public Task<AnnouncementDto> Get(Guid id, CancellationToken ct) => announcements.GetAsync(id, ct);

    [HttpPost, Permission(P.AnnouncementsManage)]
    public Task<AnnouncementDto> Create(SaveAnnouncementRequest req, CancellationToken ct) => announcements.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.AnnouncementsManage)]
    public Task<AnnouncementDto> Update(Guid id, SaveAnnouncementRequest req, CancellationToken ct) => announcements.SaveAsync(id, req, ct);

    [HttpDelete("{id:guid}"), Permission(P.AnnouncementsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await announcements.DeleteAsync(id, ct); return NoContent(); }

    [HttpPost("{id:guid}/lido")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct) { await announcements.MarkReadAsync(id, ct); return NoContent(); }

    [HttpGet("{id:guid}/leituras"), Permission(P.AnnouncementsManage)]
    public Task<List<AnnouncementReaderDto>> Readers(Guid id, CancellationToken ct) => announcements.ReadersAsync(id, ct);
}

[Route("api/manifestacoes")]
public class ManifestationsController(ManifestationService manifestations) : ApiController
{
    [HttpGet("tipos")]
    public Task<List<ManifestationTypeDto>> Types([FromQuery] bool includeInactive, CancellationToken ct) => manifestations.TypesAsync(includeInactive, ct);

    [HttpPost("tipos"), Permission(P.ManifestationsManage, P.SettingsManage)]
    public Task<ManifestationTypeDto> CreateType(SaveManifestationTypeRequest req, CancellationToken ct) => manifestations.SaveTypeAsync(null, req, ct);

    [HttpPut("tipos/{id:guid}"), Permission(P.ManifestationsManage, P.SettingsManage)]
    public Task<ManifestationTypeDto> UpdateType(Guid id, SaveManifestationTypeRequest req, CancellationToken ct) => manifestations.SaveTypeAsync(id, req, ct);

    /// <summary>Manifestações. A equipe vê todas; associados e dependentes veem apenas as que enviaram.</summary>
    [HttpGet, Permission(P.ManifestationsView, Self = true)]
    public Task<PagedResult<ManifestationDto>> List([FromQuery] ManifestationQuery query, CancellationToken ct) => manifestations.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.ManifestationsView, Self = true)]
    public Task<ManifestationDetailDto> Get(Guid id, CancellationToken ct) => manifestations.GetAsync(id, ct);

    /// <summary>Envia reclamação, sugestão ou outro tipo configurado. Gera protocolo e notifica a administração.</summary>
    [HttpPost, Kinds(UserKind.Member, UserKind.Dependent)]
    public Task<ManifestationDetailDto> Create(CreateManifestationRequest req, CancellationToken ct) => manifestations.CreateAsync(req, ct);

    [HttpPost("{id:guid}/andamento"), Permission(P.ManifestationsManage)]
    public Task<ManifestationDetailDto> UpdateStatus(Guid id, UpdateManifestationRequest req, CancellationToken ct) => manifestations.UpdateStatusAsync(id, req, ct);

    [HttpPost("{id:guid}/respostas"), Permission(P.ManifestationsManage)]
    public Task<ManifestationDetailDto> Respond(Guid id, RespondManifestationRequest req, CancellationToken ct) => manifestations.RespondAsync(id, req, ct);
}

public record AddAttendanceRequest(Guid MemberId);
public record ClosePollRequest(bool PublishResults);

[Route("api/assembleias")]
[Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
public class AssembliesController(AssemblyService assemblies) : ApiController
{
    [HttpGet, Permission(P.AssembliesView, Self = true)]
    public Task<PagedResult<AssemblyDto>> List([FromQuery] PageQuery page, CancellationToken ct) => assemblies.ListAsync(page, ct);

    [HttpGet("{id:guid}"), Permission(P.AssembliesView, Self = true)]
    public Task<AssemblyDto> Get(Guid id, CancellationToken ct) => assemblies.GetAsync(id, ct);

    [HttpPost, Permission(P.AssembliesManage)]
    public Task<AssemblyDto> Create(SaveAssemblyRequest req, CancellationToken ct) => assemblies.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.AssembliesManage)]
    public Task<AssemblyDto> Update(Guid id, SaveAssemblyRequest req, CancellationToken ct) => assemblies.SaveAsync(id, req, ct);

    [HttpPost("{id:guid}/situacao"), Permission(P.AssembliesManage)]
    public Task<AssemblyDto> SetStatus(Guid id, SetAssemblyStatusRequest req, CancellationToken ct) => assemblies.SetStatusAsync(id, req, ct);

    [HttpPut("{id:guid}/ata"), Permission(P.AssembliesManage)]
    public Task<AssemblyDto> Minutes(Guid id, SaveMinutesRequest req, CancellationToken ct) => assemblies.SaveMinutesAsync(id, req, ct);

    [HttpGet("{id:guid}/presencas"), Permission(P.AssembliesView)]
    public Task<List<AttendanceDto>> Attendances(Guid id, CancellationToken ct) => assemblies.AttendancesAsync(id, ct);

    [HttpPost("{id:guid}/presencas"), Permission(P.AssembliesManage)]
    public Task<List<AttendanceDto>> AddAttendance(Guid id, AddAttendanceRequest req, CancellationToken ct) => assemblies.AddAttendanceAsync(id, req.MemberId, ct);

    [HttpDelete("presencas/{attendanceId:guid}"), Permission(P.AssembliesManage)]
    public async Task<IActionResult> RemoveAttendance(Guid attendanceId, CancellationToken ct) { await assemblies.RemoveAttendanceAsync(attendanceId, ct); return NoContent(); }
}

[Route("api/votacoes")]
[Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
public class PollsController(AssemblyService assemblies) : ApiController
{
    [HttpGet, Permission(P.AssembliesView, Self = true)]
    public Task<List<PollDto>> List([FromQuery] Guid? assemblyId, [FromQuery] bool openOnly, CancellationToken ct) => assemblies.PollsAsync(assemblyId, openOnly, ct);

    [HttpGet("{id:guid}"), Permission(P.AssembliesView, Self = true)]
    public Task<PollDto> Get(Guid id, CancellationToken ct) => assemblies.GetPollAsync(id, ct);

    [HttpPost, Permission(P.AssembliesManage)]
    public Task<PollDto> Create(SavePollRequest req, CancellationToken ct) => assemblies.SavePollAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.AssembliesManage)]
    public Task<PollDto> Update(Guid id, SavePollRequest req, CancellationToken ct) => assemblies.SavePollAsync(id, req, ct);

    [HttpPost("{id:guid}/abrir"), Permission(P.AssembliesManage)]
    public Task<PollDto> Open(Guid id, CancellationToken ct) => assemblies.OpenPollAsync(id, ct);

    [HttpPost("{id:guid}/encerrar"), Permission(P.AssembliesManage)]
    public Task<PollDto> Close(Guid id, ClosePollRequest req, CancellationToken ct) => assemblies.ClosePollAsync(id, req.PublishResults, ct);

    /// <summary>Registra o voto. Somente associados votam — dependentes e funcionários são recusados pelo servidor. Um voto por associado.</summary>
    [HttpPost("{id:guid}/votos"), Kinds(UserKind.Member)]
    public Task<PollDto> Vote(Guid id, VoteRequest req, CancellationToken ct) => assemblies.VoteAsync(id, req, ct);
}

[Route("api/patrimonio")]
public class AssetsController(AssetService assets) : ApiController
{
    [HttpGet, Permission(P.AssetsView)]
    public Task<PagedResult<AssetDto>> List([FromQuery] AssetQuery query, CancellationToken ct) => assets.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.AssetsView)]
    public Task<AssetDto> Get(Guid id, CancellationToken ct) => assets.GetAsync(id, ct);

    [HttpPost, Permission(P.AssetsManage)]
    public Task<AssetDto> Create(SaveAssetRequest req, CancellationToken ct) => assets.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.AssetsManage)]
    public Task<AssetDto> Update(Guid id, SaveAssetRequest req, CancellationToken ct) => assets.SaveAsync(id, req, ct);

    /// <summary>Baixa do bem (exclusão lógica com justificativa).</summary>
    [HttpPost("{id:guid}/baixar"), Permission(P.AssetsManage)]
    public async Task<IActionResult> Delete(Guid id, StatusChangeRequest req, CancellationToken ct) { await assets.DeleteAsync(id, req, ct); return NoContent(); }
}

[Route("api/estoque")]
public class StockController(StockService stock) : ApiController
{
    [HttpGet("produtos"), Permission(P.StockView)]
    public Task<PagedResult<StockItemDto>> Items([FromQuery] StockQuery query, CancellationToken ct) => stock.ItemsAsync(query, ct);

    [HttpPost("produtos"), Permission(P.StockManage)]
    public Task<StockItemDto> CreateItem(SaveStockItemRequest req, CancellationToken ct) => stock.SaveItemAsync(null, req, ct);

    [HttpPut("produtos/{id:guid}"), Permission(P.StockManage)]
    public Task<StockItemDto> UpdateItem(Guid id, SaveStockItemRequest req, CancellationToken ct) => stock.SaveItemAsync(id, req, ct);

    [HttpDelete("produtos/{id:guid}"), Permission(P.StockManage)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct) { await stock.DeleteItemAsync(id, ct); return NoContent(); }

    /// <summary>Entrada, saída ou ajuste de inventário. Toda movimentação é rastreável.</summary>
    [HttpPost("movimentacoes"), Permission(P.StockManage)]
    public Task<StockMovementDto> Move(MoveStockRequest req, CancellationToken ct) => stock.MoveAsync(req, ct);

    [HttpGet("movimentacoes"), Permission(P.StockView)]
    public Task<PagedResult<StockMovementDto>> Movements([FromQuery] PageQuery page, [FromQuery] Guid? itemId, CancellationToken ct) => stock.MovementsAsync(page, itemId, ct);

    [HttpGet("fornecedores"), Permission(P.StockView)]
    public Task<List<SupplierDto>> Suppliers(CancellationToken ct) => stock.SuppliersAsync(ct);

    [HttpPost("fornecedores"), Permission(P.StockManage)]
    public Task<SupplierDto> CreateSupplier(SaveSupplierRequest req, CancellationToken ct) => stock.SaveSupplierAsync(null, req, ct);

    [HttpPut("fornecedores/{id:guid}"), Permission(P.StockManage)]
    public Task<SupplierDto> UpdateSupplier(Guid id, SaveSupplierRequest req, CancellationToken ct) => stock.SaveSupplierAsync(id, req, ct);

    [HttpDelete("fornecedores/{id:guid}"), Permission(P.StockManage)]
    public async Task<IActionResult> DeleteSupplier(Guid id, CancellationToken ct) { await stock.DeleteSupplierAsync(id, ct); return NoContent(); }
}

[Route("api/manutencoes")]
public class MaintenanceController(MaintenanceService maintenance) : ApiController
{
    [HttpGet, Permission(P.MaintenanceView)]
    public Task<PagedResult<MaintenanceDto>> List([FromQuery] MaintenanceQuery query, CancellationToken ct) => maintenance.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.MaintenanceView)]
    public Task<MaintenanceDto> Get(Guid id, CancellationToken ct) => maintenance.GetAsync(id, ct);

    [HttpPost, Permission(P.MaintenanceManage)]
    public Task<MaintenanceDto> Create(SaveMaintenanceRequest req, CancellationToken ct) => maintenance.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.MaintenanceManage)]
    public Task<MaintenanceDto> Update(Guid id, SaveMaintenanceRequest req, CancellationToken ct) => maintenance.SaveAsync(id, req, ct);

    [HttpPost("{id:guid}/situacao"), Permission(P.MaintenanceManage)]
    public Task<MaintenanceDto> SetStatus(Guid id, MaintenanceStatusRequest req, CancellationToken ct) => maintenance.SetStatusAsync(id, req, ct);

    /// <summary>Bloqueia o espaço da manutenção; o bloqueio reflete imediatamente na disponibilidade das reservas.</summary>
    [HttpPost("{id:guid}/bloqueio"), Permission(P.MaintenanceManage)]
    public Task<BlockResult> Block(Guid id, MaintenanceBlockRequest req, CancellationToken ct) => maintenance.BlockSpaceAsync(id, req, ct);
}

[Route("api/vistorias")]
public class InspectionsController(InspectionService inspections) : ApiController
{
    [HttpGet, Permission(P.InspectionsView)]
    public Task<PagedResult<InspectionDto>> List([FromQuery] InspectionQuery query, CancellationToken ct) => inspections.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.InspectionsView)]
    public Task<InspectionDto> Get(Guid id, CancellationToken ct) => inspections.GetAsync(id, ct);

    /// <summary>Registra o checklist de vistoria (antes ou depois do uso). Havendo dano, gera ocorrência vinculada à reserva.</summary>
    [HttpPost, Permission(P.InspectionsManage)]
    public Task<InspectionDto> Create(CreateInspectionRequest req, CancellationToken ct) => inspections.CreateAsync(req, ct);
}

[Route("api/ocorrencias")]
public class OccurrencesController(OccurrenceService occurrences) : ApiController
{
    [HttpGet, Permission(P.OccurrencesView)]
    public Task<PagedResult<OccurrenceDto>> List([FromQuery] OccurrenceQuery query, CancellationToken ct) => occurrences.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.OccurrencesView)]
    public Task<OccurrenceDto> Get(Guid id, CancellationToken ct) => occurrences.GetAsync(id, ct);

    [HttpPost, Permission(P.OccurrencesManage)]
    public Task<OccurrenceDto> Create(CreateOccurrenceRequest req, CancellationToken ct) => occurrences.CreateAsync(req, ct);

    [HttpPut("{id:guid}"), Permission(P.OccurrencesManage)]
    public Task<OccurrenceDto> Update(Guid id, UpdateOccurrenceRequest req, CancellationToken ct) => occurrences.UpdateAsync(id, req, ct);

    [HttpPost("{id:guid}/cobranca"), Permission(P.OccurrencesManage)]
    public Task<OccurrenceDto> ProposeCharge(Guid id, ProposeChargeRequest req, CancellationToken ct) => occurrences.ProposeChargeAsync(id, req, ct);

    /// <summary>Aprova ou rejeita a cobrança por dano. Somente após a aprovação o valor é lançado no financeiro.</summary>
    [HttpPost("{id:guid}/cobranca/decisao"), Permission(P.OccurrencesApproveCharge)]
    public Task<OccurrenceDto> DecideCharge(Guid id, DecideChargeRequest req, CancellationToken ct) => occurrences.DecideChargeAsync(id, req, ct);
}

[Route("api/chaves")]
public class KeysController(KeyHandoverService keys) : ApiController
{
    [HttpGet, Permission(P.InspectionsView)]
    public Task<PagedResult<KeyHandoverDto>> List([FromQuery] KeyHandoverQuery query, CancellationToken ct) => keys.ListAsync(query, ct);

    [HttpPost("entrega"), Permission(P.InspectionsManage)]
    public Task<KeyHandoverDto> Deliver(DeliverKeyRequest req, CancellationToken ct) => keys.DeliverAsync(req, ct);

    [HttpPost("{id:guid}/devolucao"), Permission(P.InspectionsManage)]
    public Task<KeyHandoverDto> Return(Guid id, ReturnKeyRequest req, CancellationToken ct) => keys.ReturnAsync(id, req, ct);

    [HttpPost("{id:guid}/pendencias-resolvidas"), Permission(P.InspectionsManage)]
    public Task<KeyHandoverDto> ResolvePendencies(Guid id, CancellationToken ct) => keys.ResolvePendenciesAsync(id, ct);
}
