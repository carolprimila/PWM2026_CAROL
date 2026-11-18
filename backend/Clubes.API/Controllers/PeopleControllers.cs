using Clubes.API.Infrastructure;
using Clubes.Application.Access;
using Clubes.Application.Common;
using Clubes.Application.Finance;
using Clubes.Application.Operations;
using Clubes.Application.People;
using Clubes.Application.Spaces;
using Clubes.Domain;
using Microsoft.AspNetCore.Mvc;
using P = Clubes.Application.Common.Permissions;

namespace Clubes.API.Controllers;

[Route("api/associados")]
public class MembersController(MemberService members, DelinquencyService delinquency) : ApiController
{
    [HttpGet, Permission(P.MembersView)]
    public Task<PagedResult<MemberListDto>> List([FromQuery] MemberQuery query, CancellationToken ct) => members.ListAsync(query, ct);

    [HttpGet("{id:guid}"), Permission(P.MembersView)]
    public Task<MemberDto> Get(Guid id, CancellationToken ct) => members.GetAsync(id, ct);

    [HttpPost, Permission(P.MembersManage)]
    public Task<MemberDto> Create(SaveMemberRequest req, CancellationToken ct) => members.CreateAsync(req, ct);

    [HttpPut("{id:guid}"), Permission(P.MembersManage)]
    public Task<MemberDto> Update(Guid id, SaveMemberRequest req, CancellationToken ct) => members.UpdateAsync(id, req, ct);

    [HttpPost("{id:guid}/aprovar"), Permission(P.MembersApprove)]
    public Task<MemberDto> Approve(Guid id, CancellationToken ct) => members.ApproveAsync(id, ct);

    [HttpPost("{id:guid}/suspender"), Permission(P.MembersApprove)]
    public Task<MemberDto> Suspend(Guid id, StatusChangeRequest req, CancellationToken ct) => members.SuspendAsync(id, req, ct);

    [HttpPost("{id:guid}/reativar"), Permission(P.MembersApprove)]
    public Task<MemberDto> Reactivate(Guid id, StatusChangeRequest req, CancellationToken ct) => members.ReactivateAsync(id, req, ct);

    [HttpPost("{id:guid}/cancelar"), Permission(P.MembersApprove)]
    public Task<MemberDto> Cancel(Guid id, StatusChangeRequest req, CancellationToken ct) => members.CancelAsync(id, req, ct);

    /// <summary>Cria o login do associado no aplicativo, com senha temporária de troca obrigatória.</summary>
    [HttpPost("{id:guid}/acesso"), Permission(P.MembersManage)]
    public Task<TemporaryPasswordDto> CreateAccess(Guid id, CreateAccessRequest req, CancellationToken ct) => members.CreateAccessAsync(id, req, ct);

    [HttpGet("{id:guid}/situacao-financeira"), Permission(P.MembersView, P.FinanceView)]
    public Task<FinancialStatusDto> Financial(Guid id, CancellationToken ct) => delinquency.GetAsync(id, ct);

    /// <summary>LGPD: anonimiza os dados pessoais de uma associação cancelada, preservando o histórico financeiro sem identificação.</summary>
    [HttpPost("{id:guid}/anonimizar"), Permission(P.MembersApprove)]
    public async Task<IActionResult> Anonymize(Guid id, StatusChangeRequest req, CancellationToken ct) { await members.AnonymizeAsync(id, req, ct); return NoContent(); }

    /// <summary>LGPD: cópia dos dados pessoais do associado autenticado (direito de acesso do titular).</summary>
    [HttpGet("eu/meus-dados"), Kinds(UserKind.Member)]
    public Task<object> ExportMine(CancellationToken ct) => members.ExportMineAsync(ct);

    /// <summary>Cadastro do associado autenticado.</summary>
    [HttpGet("eu"), Kinds(UserKind.Member)]
    public Task<MemberDto> Mine(CancellationToken ct) => members.GetMineAsync(ct);

    [HttpPut("eu"), Kinds(UserKind.Member)]
    public Task<MemberDto> UpdateMine(UpdateMyProfileRequest req, CancellationToken ct) => members.UpdateMineAsync(req, ct);
}

[Route("api/dependentes")]
public class DependentsController(DependentService dependents, RequestContext ctx) : ApiController
{
    [HttpGet, Permission(P.DependentsView)]
    public Task<PagedResult<DependentDto>> List([FromQuery] DependentQuery query, CancellationToken ct) => dependents.ListAsync(query, ct);

    /// <summary>Dependentes do associado autenticado.</summary>
    [HttpGet("meus"), Kinds(UserKind.Member)]
    public Task<List<DependentDto>> Mine(CancellationToken ct) => dependents.ListMineAsync(ct);

    /// <summary>Perfil do dependente autenticado.</summary>
    [HttpGet("eu"), Kinds(UserKind.Dependent)]
    public Task<DependentDto> Me(CancellationToken ct) => dependents.GetAsync(ctx.DependentId!.Value, ct);

    [HttpGet("{id:guid}"), Permission(P.DependentsView, Self = true)]
    public Task<DependentDto> Get(Guid id, CancellationToken ct) => dependents.GetAsync(id, ct);

    /// <summary>Cadastra um dependente. Pela equipe, já entra ativo; pelo associado, aguarda aprovação. Respeita o limite por associado.</summary>
    [HttpPost, Permission(P.DependentsManage, Self = true), Kinds(UserKind.Member, UserKind.Staff, UserKind.ClubAdmin, UserKind.SuperAdmin)]
    public Task<DependentDto> Create([FromQuery] Guid? memberId, SaveDependentRequest req, CancellationToken ct) => dependents.CreateAsync(memberId, req, ct);

    [HttpPut("{id:guid}"), Permission(P.DependentsManage, Self = true)]
    public Task<DependentDto> Update(Guid id, SaveDependentRequest req, CancellationToken ct) => dependents.UpdateAsync(id, req, ct);

    [HttpPost("{id:guid}/aprovar"), Permission(P.DependentsManage)]
    public Task<DependentDto> Approve(Guid id, CancellationToken ct) => dependents.ApproveAsync(id, ct);

    [HttpPost("{id:guid}/rejeitar"), Permission(P.DependentsManage)]
    public Task<DependentDto> Reject(Guid id, StatusChangeRequest req, CancellationToken ct) => dependents.RejectAsync(id, req, ct);

    [HttpPost("{id:guid}/suspender"), Permission(P.DependentsManage)]
    public Task<DependentDto> Suspend(Guid id, StatusChangeRequest req, CancellationToken ct) => dependents.SuspendAsync(id, req, ct);

    [HttpPost("{id:guid}/reativar"), Permission(P.DependentsManage)]
    public Task<DependentDto> Reactivate(Guid id, StatusChangeRequest req, CancellationToken ct) => dependents.ReactivateAsync(id, req, ct);

    [HttpPost("{id:guid}/remover"), Permission(P.DependentsManage, Self = true)]
    public async Task<IActionResult> Remove(Guid id, StatusChangeRequest req, CancellationToken ct) { await dependents.RemoveAsync(id, req, ct); return NoContent(); }

    /// <summary>Administração libera a permissão; o associado responsável a habilita. Só vale com as duas marcações.</summary>
    [HttpPut("{id:guid}/permissoes"), Permission(P.DependentsManage, Self = true)]
    public Task<DependentDto> SetPermission(Guid id, SetDependentPermissionRequest req, CancellationToken ct) => dependents.SetPermissionAsync(id, req, ct);

    [HttpPost("{id:guid}/acesso"), Permission(P.DependentsManage, Self = true)]
    public Task<TemporaryPasswordDto> CreateAccess(Guid id, CreateAccessRequest req, CancellationToken ct) => dependents.CreateAccessAsync(id, req, ct);
}

[Route("api/cartoes")]
public class CardsController(CardService cards) : ApiController
{
    /// <summary>Cartão digital do associado ou dependente autenticado.</summary>
    [HttpGet("meu"), Kinds(UserKind.Member, UserKind.Dependent)]
    public Task<CardDto> Mine(CancellationToken ct) => cards.MyCardAsync(ct);

    [HttpGet("{personType}/{personId:guid}"), Permission(P.MembersView, P.DependentsView, P.GateOperate)]
    public Task<CardDto> Get(PersonType personType, Guid personId, CancellationToken ct) => cards.GetAsync(personType, personId, ct);
}

[Route("api/qr-code")]
public class QrCodeController(CardService cards) : ApiController
{
    /// <summary>
    /// Gera um novo QR Code de uso único para a próxima operação (entrada ou saída). Tokens anteriores
    /// não utilizados são invalidados. O código expira conforme a validade configurada pelo clube.
    /// </summary>
    [HttpPost, Kinds(UserKind.Member, UserKind.Dependent)]
    public Task<QrTokenDto> Issue(CancellationToken ct) => cards.IssueAsync(ct);
}

[Route("api/portaria")]
public class GateController(GateService gate) : ApiController
{
    /// <summary>Valida o QR Code (ou o código numérico) e registra a entrada/saída. O token é consumido na validação.</summary>
    [HttpPost("validar"), Permission(P.GateOperate)]
    public Task<AccessResultDto> Validate(ValidateRequest req, CancellationToken ct) => gate.ValidateAsync(req, ct);

    /// <summary>Registro manual, com justificativa obrigatória, para situações excepcionais.</summary>
    [HttpPost("manual"), Permission(P.GateOperate)]
    public Task<AccessResultDto> Manual(ManualAccessRequest req, CancellationToken ct) => gate.ManualAsync(req, ct);

    [HttpPost("convidados/{guestId:guid}/entrada"), Permission(P.GateOperate)]
    public Task<AccessResultDto> GuestEntry(Guid guestId, CancellationToken ct) => gate.GuestAsync(guestId, AccessOperation.Entry, ct);

    [HttpPost("convidados/{guestId:guid}/saida"), Permission(P.GateOperate)]
    public Task<AccessResultDto> GuestExit(Guid guestId, CancellationToken ct) => gate.GuestAsync(guestId, AccessOperation.Exit, ct);

    [HttpGet("pessoas"), Permission(P.GateOperate)]
    public Task<List<GatePersonDto>> Search([FromQuery] string term, CancellationToken ct) => gate.SearchAsync(term, ct);

    [HttpGet("presentes"), Permission(P.GateOperate, P.GateHistory)]
    public Task<List<PresenceDto>> Present(CancellationToken ct) => gate.PresentAsync(ct);

    [HttpGet("ocupacao"), Permission(P.GateOperate, P.GateHistory, P.DashboardView)]
    public Task<OccupancyDto> Occupancy([FromQuery] int days = 30, CancellationToken ct = default) => gate.OccupancyAsync(days, ct);
}

[Route("api/acessos")]
public class AccessLogsController(GateService gate) : ApiController
{
    /// <summary>Histórico de entradas, saídas e acessos negados.</summary>
    [HttpGet, Permission(P.GateHistory)]
    public Task<PagedResult<AccessLogDto>> History([FromQuery] AccessLogQuery query, CancellationToken ct) => gate.HistoryAsync(query, ct);
}

[Route("api/convidados")]
public class GuestsController(GuestService guests) : ApiController
{
    [HttpGet, Permission(P.GuestsView, P.GateOperate, Self = true)]
    public Task<PagedResult<GuestDto>> List([FromQuery] GuestQuery query, CancellationToken ct) => guests.ListAsync(query, ct);

    /// <summary>Cadastro antecipado de convidado. Respeita o limite por associado/dia e as regras financeiras do responsável.</summary>
    [HttpPost, Permission(P.GuestsManage, Self = true)]
    public Task<GuestDto> Create(CreateGuestRequest req, CancellationToken ct) => guests.CreateAsync(req, ct);

    [HttpPost("{id:guid}/autorizacao"), Permission(P.GuestsManage)]
    public Task<GuestDto> Authorize(Guid id, AuthorizeGuestRequest req, CancellationToken ct) => guests.AuthorizeAsync(id, req, ct);

    [HttpPost("{id:guid}/cancelar"), Permission(P.GuestsManage, Self = true)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) { await guests.CancelAsync(id, ct); return NoContent(); }
}

[Route("api/funcionarios")]
public class EmployeesController(EmployeeService employees) : ApiController
{
    [HttpGet, Permission(P.EmployeesView)]
    public Task<PagedResult<EmployeeDto>> List([FromQuery] PageQuery page, [FromQuery] EmployeeStatus? status, CancellationToken ct) => employees.ListAsync(page, status, ct);

    [HttpGet("{id:guid}"), Permission(P.EmployeesView)]
    public Task<EmployeeDto> Get(Guid id, CancellationToken ct) => employees.GetAsync(id, ct);

    [HttpPost, Permission(P.EmployeesManage)]
    public Task<EmployeeDto> Create(SaveEmployeeRequest req, CancellationToken ct) => employees.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.EmployeesManage)]
    public Task<EmployeeDto> Update(Guid id, SaveEmployeeRequest req, CancellationToken ct) => employees.SaveAsync(id, req, ct);

    [HttpPost("{id:guid}/ativar"), Permission(P.EmployeesManage)]
    public Task<EmployeeDto> Activate(Guid id, StatusChangeRequest req, CancellationToken ct) => employees.SetStatusAsync(id, EmployeeStatus.Active, req, ct);

    [HttpPost("{id:guid}/desativar"), Permission(P.EmployeesManage)]
    public Task<EmployeeDto> Deactivate(Guid id, StatusChangeRequest req, CancellationToken ct) => employees.SetStatusAsync(id, EmployeeStatus.Inactive, req, ct);
}
