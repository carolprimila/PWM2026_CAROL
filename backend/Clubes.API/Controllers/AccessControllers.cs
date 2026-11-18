using Clubes.API.Infrastructure;
using Clubes.Application.Access;
using Clubes.Application.Admin;
using Clubes.Application.Clubs;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using P = Clubes.Application.Common.Permissions;

namespace Clubes.API.Controllers;

[Route("api/auth")]
public class AuthController(AuthService auth) : ApiController
{
    /// <summary>Autentica por e-mail e senha. Devolve token de acesso (curta duração) e token de renovação.</summary>
    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public Task<AuthResponse> Login(LoginRequest req, CancellationToken ct) => auth.LoginAsync(req, Ip, UserAgent, ct);

    /// <summary>Renova a sessão. O token de renovação é de uso único (rotação).</summary>
    [HttpPost("refresh"), AllowAnonymous, EnableRateLimiting("auth")]
    public Task<AuthResponse> Refresh(RefreshRequest req, CancellationToken ct) => auth.RefreshAsync(req, Ip, ct);

    [HttpPost("logout"), AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest req, CancellationToken ct) { await auth.LogoutAsync(req, ct); return NoContent(); }

    [HttpPost("esqueci-senha"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Forgot(ForgotPasswordRequest req, CancellationToken ct) { await auth.ForgotPasswordAsync(req, ct); return NoContent(); }

    [HttpPost("redefinir-senha"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<IActionResult> Reset(ResetPasswordRequest req, CancellationToken ct) { await auth.ResetPasswordAsync(req, ct); return NoContent(); }

    [HttpPost("alterar-senha")]
    public async Task<IActionResult> Change(ChangePasswordRequest req, CancellationToken ct) { await auth.ChangePasswordAsync(req, ct); return NoContent(); }

    [HttpGet("me")]
    public Task<MeDto> Me(CancellationToken ct) => auth.MeAsync(ct);

    /// <summary>Registra o consentimento do usuário com a política de privacidade vigente (LGPD).</summary>
    [HttpPost("aceitar-privacidade")]
    public async Task<IActionResult> AcceptPrivacy(CancellationToken ct) { await auth.AcceptPrivacyAsync(ct); return NoContent(); }

    [HttpGet("sessoes")]
    public Task<List<SessionDto>> Sessions([FromHeader(Name = "X-Refresh-Token")] string? current, CancellationToken ct) => auth.SessionsAsync(null, current, ct);

    [HttpDelete("sessoes/{id:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct) { await auth.RevokeSessionAsync(id, null, ct); return NoContent(); }
}

[Route("api/usuarios")]
public class UsersController(UserService users, AuthService auth) : ApiController
{
    [HttpGet, Permission(P.UsersView)]
    public Task<PagedResult<UserDto>> List([FromQuery] PageQuery page, [FromQuery] UserKind? kind, CancellationToken ct) => users.ListAsync(page, kind, ct);

    [HttpGet("{id:guid}"), Permission(P.UsersView)]
    public Task<UserDto> Get(Guid id, CancellationToken ct) => users.GetAsync(id, ct);

    [HttpPost, Permission(P.UsersManage)]
    public Task<TemporaryPasswordDto> Create(CreateUserRequest req, CancellationToken ct) => users.CreateStaffAsync(req, ct);

    [HttpPut("{id:guid}"), Permission(P.UsersManage)]
    public Task<UserDto> Update(Guid id, UpdateUserRequest req, CancellationToken ct) => users.UpdateAsync(id, req, ct);

    [HttpPut("{id:guid}/funcoes"), Permission(P.RolesManage)]
    public Task<UserDto> SetRoles(Guid id, SetRolesRequest req, CancellationToken ct) => users.SetRolesAsync(id, req, ct);

    [HttpPut("{id:guid}/permissoes"), Permission(P.RolesManage)]
    public Task<UserDto> SetOverrides(Guid id, SetOverridesRequest req, CancellationToken ct) => users.SetOverridesAsync(id, req, ct);

    [HttpPost("{id:guid}/bloquear"), Permission(P.UsersManage)]
    public async Task<IActionResult> Block(Guid id, BlockRequest req, CancellationToken ct) { await users.BlockAsync(id, req, ct); return NoContent(); }

    [HttpPost("{id:guid}/desbloquear"), Permission(P.UsersManage)]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken ct) { await users.UnblockAsync(id, ct); return NoContent(); }

    [HttpPost("{id:guid}/redefinir-senha"), Permission(P.UsersManage)]
    public Task<TemporaryPasswordDto> ResetPassword(Guid id, CancellationToken ct) => users.ResetPasswordAsync(id, ct);

    [HttpGet("{id:guid}/sessoes"), Permission(P.UsersView)]
    public async Task<List<SessionDto>> Sessions(Guid id, CancellationToken ct) { await users.GetAsync(id, ct); return await auth.SessionsAsync(id, null, ct); }

    [HttpDelete("{id:guid}/sessoes/{sessionId:guid}"), Permission(P.UsersManage)]
    public async Task<IActionResult> RevokeSession(Guid id, Guid sessionId, CancellationToken ct) { await users.GetAsync(id, ct); await auth.RevokeSessionAsync(sessionId, id, ct); return NoContent(); }

    [HttpGet("historico-acessos"), Permission(P.UsersView)]
    public Task<PagedResult<LoginHistoryDto>> LoginHistory([FromQuery] PageQuery page, [FromQuery] Guid? userId, [FromQuery] bool? success, CancellationToken ct)
        => users.LoginHistoryAsync(page, userId, success, ct);
}

[Route("api/funcoes")]
public class RolesController(RoleService roles) : ApiController
{
    [HttpGet("permissoes"), Permission(P.UsersView, P.RolesManage)]
    public IReadOnlyList<PermissionInfoDto> Catalog() => roles.Catalog();

    [HttpGet, Permission(P.UsersView, P.RolesManage)]
    public Task<List<RoleDto>> List(CancellationToken ct) => roles.ListAsync(ct);

    [HttpPost, Permission(P.RolesManage)]
    public Task<RoleDto> Create(SaveRoleRequest req, CancellationToken ct) => roles.SaveAsync(null, req, ct);

    [HttpPut("{id:guid}"), Permission(P.RolesManage)]
    public Task<RoleDto> Update(Guid id, SaveRoleRequest req, CancellationToken ct) => roles.SaveAsync(id, req, ct);

    [HttpDelete("{id:guid}"), Permission(P.RolesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await roles.DeleteAsync(id, ct); return NoContent(); }
}

[Route("api/clubes")]
public class ClubsController(ClubService clubs) : ApiController
{
    /// <summary>Lista todos os clubes da plataforma (somente superadministrador).</summary>
    [HttpGet, Kinds(UserKind.SuperAdmin)]
    public Task<List<ClubDto>> List(CancellationToken ct) => clubs.ListAsync(ct);

    [HttpPost, Kinds(UserKind.SuperAdmin)]
    public Task<CreateClubResult> Create(CreateClubRequest req, CancellationToken ct) => clubs.CreateAsync(req, ct);

    /// <summary>Dados do clube do usuário autenticado.</summary>
    [HttpGet("atual")]
    public Task<ClubDto> Current(CancellationToken ct) => clubs.GetCurrentAsync(ct);

    [HttpPut("{id:guid}"), Permission(P.ClubManage)]
    public Task<ClubDto> Update(Guid id, SaveClubRequest req, CancellationToken ct) => clubs.UpdateAsync(id, req, ct);

    [HttpPost("{id:guid}/ativar"), Kinds(UserKind.SuperAdmin)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct) { await clubs.SetActiveAsync(id, true, ct); return NoContent(); }

    [HttpPost("{id:guid}/desativar"), Kinds(UserKind.SuperAdmin)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct) { await clubs.SetActiveAsync(id, false, ct); return NoContent(); }

    [HttpGet("atual/horarios")]
    public Task<List<OpeningHourDto>> OpeningHours(CancellationToken ct) => clubs.GetOpeningHoursAsync(ct);

    [HttpPut("atual/horarios"), Permission(P.SettingsManage)]
    public Task<List<OpeningHourDto>> SetOpeningHours(List<OpeningHourDto> hours, CancellationToken ct) => clubs.SetOpeningHoursAsync(hours, ct);
}

public record UpdateSettingsRequest(SettingsDto Settings, string? Justification);
public record PublicPoliciesDto(string? HolidayRules, string? MinorsRules, string? MinorsAccompanimentRules, string? PoolPolicy, string? EventProcedures,
    string? SpaceUsagePolicy, string? PrivacyPolicy, string PrivacyPolicyVersion, int MaxGuestsPerMember, int MaxDependentsPerMember, decimal? GuestFee,
    bool MemberCanToggleDependentPermissions);

[Route("api/configuracoes")]
public class SettingsController(SettingsService settings) : ApiController
{
    [HttpGet, Permission(P.SettingsView, P.SettingsManage)]
    public Task<SettingsDto> Get(CancellationToken ct) => settings.GetAsync(ct);

    /// <summary>Altera as políticas do clube. Toda alteração fica registrada na auditoria com os valores anteriores e novos.</summary>
    [HttpPut, Permission(P.SettingsManage)]
    public Task<SettingsDto> Update(UpdateSettingsRequest req, CancellationToken ct) => settings.UpdateAsync(req.Settings, req.Justification, ct);

    /// <summary>Políticas de uso exibidas a associados e dependentes (sem parâmetros financeiros internos).</summary>
    [HttpGet("politicas")]
    public async Task<PublicPoliciesDto> Policies(CancellationToken ct)
    {
        var s = await settings.GetAsync(ct);
        return new PublicPoliciesDto(s.HolidayRules, s.MinorsRules, s.MinorsAccompanimentRules, s.PoolPolicy, s.EventProcedures, s.SpaceUsagePolicy, s.PrivacyPolicy,
            s.PrivacyPolicyVersion, s.MaxGuestsPerMember, s.MaxDependentsPerMember, s.GuestFee, s.MemberCanToggleDependentPermissions);
    }
}

[Route("api/auditoria")]
public class AuditController(AuditService audit) : ApiController
{
    [HttpGet, Permission(P.AuditView)]
    public Task<PagedResult<AuditLogDto>> List([FromQuery] AuditQuery query, CancellationToken ct) => audit.ListAsync(query, ct);

    [HttpGet("entidades"), Permission(P.AuditView)]
    public Task<List<string>> EntityTypes(CancellationToken ct) => audit.EntityTypesAsync(ct);
}

[Route("api/calendario")]
public class CalendarController(CalendarService calendar) : ApiController
{
    [HttpGet, Permission(P.CalendarView)]
    public Task<List<CalendarItemDto>> Get([FromQuery] CalendarQuery query, CancellationToken ct) => calendar.GetAsync(query, ct);
}

[Route("api/relatorios")]
public class ReportsController(ReportService reports) : ApiController
{
    [HttpGet("dashboard"), Permission(P.DashboardView)]
    public Task<DashboardDto> Dashboard(CancellationToken ct) => reports.DashboardAsync(ct);

    [HttpGet("indicadores"), Permission(P.ReportsView)]
    public Task<IndicatorsDto> Indicators([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct) => reports.IndicatorsAsync(from, to, ct);

    [HttpGet("indicadores/excel"), Permission(P.ReportsView)]
    public async Task<IActionResult> Excel([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => File(await reports.ExcelAsync(from, to, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"indicadores-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx");

    [HttpGet("indicadores/pdf"), Permission(P.ReportsView)]
    public async Task<IActionResult> Pdf([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
        => File(await reports.PdfAsync(from, to, ct), "application/pdf", $"indicadores-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
}

public record UploadForm(IFormFile File, FileScope Scope = FileScope.Restricted, Guid? OwnerMemberId = null);

[Route("api/arquivos")]
public class FilesController(FileService files) : ApiController
{
    /// <summary>Envia um arquivo (imagem ou PDF, até 10 MB). O acesso posterior é controlado pelo escopo informado.</summary>
    [HttpPost, RequestSizeLimit(11 * 1024 * 1024), Consumes("multipart/form-data")]
    public async Task<FileDto> Upload([FromForm] UploadForm form, CancellationToken ct)
    {
        await using var stream = form.File.OpenReadStream();
        return await files.UploadAsync(stream, form.File.FileName, form.File.ContentType, form.File.Length, form.Scope, form.OwnerMemberId, ct);
    }

    /// <summary>Baixa um arquivo, validando clube e permissão do usuário a cada acesso.</summary>
    [HttpGet("{id:guid}"), Produces("application/octet-stream")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (file, content) = await files.OpenAsync(id, ct);
        Response.Headers.CacheControl = "private, max-age=300";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(content, file.ContentType, file.ContentType == "application/pdf" ? file.FileName : null);
    }
}

[Route("api/anexos")]
public class AttachmentsController(AttachmentService attachments) : ApiController
{
    [HttpGet]
    public Task<List<AttachmentDto>> List([FromQuery] string ownerType, [FromQuery] Guid ownerId, CancellationToken ct) => attachments.ListAsync(ownerType, ownerId, ct);

    [HttpPost]
    public Task<AttachmentDto> Add(AddAttachmentRequest req, CancellationToken ct) => attachments.AddAsync(req, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) { await attachments.RemoveAsync(id, ct); return NoContent(); }
}

[Route("api/historico")]
public class HistoryController(HistoryService history) : ApiController
{
    private static readonly Dictionary<string, string> Required = new()
    {
        ["Member"] = P.MembersView, ["Dependent"] = P.DependentsView, ["Employee"] = P.EmployeesView, ["Reservation"] = P.ReservationsView,
        ["MaintenanceOrder"] = P.MaintenanceView, ["Occurrence"] = P.OccurrencesView, ["Asset"] = P.AssetsView, ["Assembly"] = P.AssembliesView, ["Poll"] = P.AssembliesView,
    };

    /// <summary>Histórico de mudanças de situação de um registro.</summary>
    [HttpGet("{entityType}/{id:guid}")]
    public async Task<ActionResult<List<HistoryDto>>> Get(string entityType, Guid id, [FromServices] PermissionService permissions, CancellationToken ct)
    {
        if (!Required.TryGetValue(entityType, out var permission)) return NotFound(new ApiError("nao_encontrado", "Tipo de registro desconhecido."));
        await permissions.EnsureAsync(permission, ct);
        return await history.ListAsync(entityType, id, ct);
    }
}

[Route("api/notificacoes")]
public class NotificationsController(NotificationService notifications) : ApiController
{
    [HttpGet]
    public Task<PagedResult<NotificationDto>> Mine([FromQuery] PageQuery page, [FromQuery] bool unreadOnly, CancellationToken ct) => notifications.MineAsync(page, unreadOnly, ct);

    [HttpGet("nao-lidas")]
    public async Task<object> Unread(CancellationToken ct) => new { count = await notifications.UnreadCountAsync(ct) };

    [HttpPost("{id:guid}/lida")]
    public async Task<IActionResult> Read(Guid id, CancellationToken ct) { await notifications.MarkReadAsync(id, ct); return NoContent(); }

    [HttpPost("marcar-todas-lidas")]
    public async Task<IActionResult> ReadAll(CancellationToken ct) { await notifications.MarkReadAsync(null, ct); return NoContent(); }

    [HttpPost("dispositivos")]
    public async Task<IActionResult> RegisterDevice(RegisterDeviceRequest req, CancellationToken ct) { await notifications.RegisterDeviceAsync(req, ct); return NoContent(); }

    [HttpDelete("dispositivos")]
    public async Task<IActionResult> UnregisterDevice([FromQuery] string token, CancellationToken ct) { await notifications.UnregisterDeviceAsync(token, ct); return NoContent(); }

    [HttpGet("preferencias")]
    public Task<List<NotificationPreferenceDto>> Preferences(CancellationToken ct) => notifications.PreferencesAsync(ct);

    [HttpPut("preferencias")]
    public Task<List<NotificationPreferenceDto>> SetPreferences(List<NotificationPreferenceDto> prefs, CancellationToken ct) => notifications.SetPreferencesAsync(prefs, ct);

    /// <summary>Registro de envio, entrega e leitura das notificações do clube.</summary>
    [HttpGet("entregas"), Permission(P.AuditView)]
    public Task<PagedResult<NotificationDeliveryDto>> Deliveries([FromQuery] PageQuery page, CancellationToken ct) => notifications.DeliveriesAsync(page, ct);
}
