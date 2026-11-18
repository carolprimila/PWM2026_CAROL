using Clubes.Mobile.Models;

namespace Clubes.Mobile.Services;

/// <summary>Rotas da API usadas pelo aplicativo, tipadas. Nenhuma regra de negócio aqui: o servidor valida tudo.</summary>
public class ClubApi(ApiClient api)
{
    private static string Q(string value) => Uri.EscapeDataString(value);
    private static string Iso(DateTime utc) => Q(utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));

    // Configurações públicas
    public Task<PublicPoliciesDto> PoliciesAsync() => api.GetAsync<PublicPoliciesDto>("/api/configuracoes/politicas");

    // Cartão digital e QR Code
    public Task<CardDto> MyCardAsync() => api.GetAsync<CardDto>("/api/cartoes/meu");
    public Task<QrTokenDto> IssueQrAsync() => api.PostAsync<QrTokenDto>("/api/qr-code");

    // Financeiro
    public Task<FinancialStatusDto> MyFinancialStatusAsync() => api.GetAsync<FinancialStatusDto>("/api/financeiro/minha-situacao");
    public Task<PagedResult<ChargeDto>> ChargesAsync(ChargeStatus status) => api.GetAsync<PagedResult<ChargeDto>>($"/api/financeiro/cobrancas?status={status}&pageSize=100");
    public Task<ChargeDetailDto> ChargeAsync(Guid id) => api.GetAsync<ChargeDetailDto>($"/api/financeiro/cobrancas/{id}");
    public Task<AppPaymentDto> StartPaymentAsync(Guid chargeId, PaymentMethod method, string idempotencyKey) =>
        api.PostAsync<AppPaymentDto>("/api/pagamentos/app", new StartAppPaymentRequest(chargeId, method, idempotencyKey));
    public Task<PaymentDto> PaymentAsync(Guid id) => api.GetAsync<PaymentDto>($"/api/pagamentos/{id}");
    public Task<PagedResult<PaymentDto>> PaymentsAsync(int page) => api.GetAsync<PagedResult<PaymentDto>>($"/api/pagamentos?page={page}&pageSize=30");
    public Task<byte[]> ReceiptAsync(Guid paymentId) => api.GetBytesAsync($"/api/pagamentos/{paymentId}/comprovante");

    // Espaços e reservas
    public Task<List<SpaceDto>> SpacesAsync() => api.GetAsync<List<SpaceDto>>("/api/espacos");
    public Task<SpaceDto> SpaceAsync(Guid id) => api.GetAsync<SpaceDto>($"/api/espacos/{id}");
    public Task<DayAvailabilityDto> DayAvailabilityAsync(Guid spaceId, DateOnly date) =>
        api.GetAsync<DayAvailabilityDto>($"/api/espacos/{spaceId}/disponibilidade?date={Fmt.ApiDate(date)}");
    public Task<List<PeriodDayDto>> PeriodAvailabilityAsync(Guid spaceId, DateOnly from, DateOnly to) =>
        api.GetAsync<List<PeriodDayDto>>($"/api/espacos/{spaceId}/disponibilidade-periodo?from={Fmt.ApiDate(from)}&to={Fmt.ApiDate(to)}");
    public Task<PagedResult<ReservationDto>> ReservationsAsync(int pageSize = 100) => api.GetAsync<PagedResult<ReservationDto>>($"/api/reservas?pageSize={pageSize}&desc=true");
    public Task<PagedResult<ReservationDto>> UpcomingReservationsAsync(int pageSize) =>
        api.GetAsync<PagedResult<ReservationDto>>($"/api/reservas?from={Iso(DateTime.UtcNow)}&pageSize={pageSize}");
    public Task<ReservationDetailDto> ReservationAsync(Guid id) => api.GetAsync<ReservationDetailDto>($"/api/reservas/{id}");
    public Task<QuoteDto> QuoteReservationAsync(CreateReservationRequest req) => api.PostAsync<QuoteDto>("/api/reservas/simular", req);
    public Task<ReservationDetailDto> CreateReservationAsync(CreateReservationRequest req) => api.PostAsync<ReservationDetailDto>("/api/reservas", req);
    public Task<ReservationDetailDto> CancelReservationAsync(Guid id, string? reason) =>
        api.PostAsync<ReservationDetailDto>($"/api/reservas/{id}/cancelar", new DecisionRequest(reason));

    // Convidados
    public Task<PagedResult<GuestDto>> GuestsAsync() => api.GetAsync<PagedResult<GuestDto>>("/api/convidados?pageSize=100");
    public Task<PagedResult<GuestDto>> GuestsOfDayAsync(DateOnly date) => api.GetAsync<PagedResult<GuestDto>>($"/api/convidados?date={Fmt.ApiDate(date)}&pageSize=200");
    public Task<GuestDto> CreateGuestAsync(CreateGuestRequest req) => api.PostAsync<GuestDto>("/api/convidados", req);
    public Task CancelGuestAsync(Guid id) => api.PostAsync($"/api/convidados/{id}/cancelar");

    // Comunicados
    public Task<PagedResult<AnnouncementDto>> FeedAsync(int page, int pageSize, AnnouncementCategory? category = null) =>
        api.GetAsync<PagedResult<AnnouncementDto>>($"/api/comunicados/mural?page={page}&pageSize={pageSize}" + (category is { } c ? $"&category={c}" : ""));
    public Task<AnnouncementDto> AnnouncementAsync(Guid id) => api.GetAsync<AnnouncementDto>($"/api/comunicados/{id}");
    public Task MarkAnnouncementReadAsync(Guid id) => api.PostAsync($"/api/comunicados/{id}/lido");
    public Task<List<AttachmentDto>> AttachmentsAsync(string ownerType, Guid ownerId) =>
        api.GetAsync<List<AttachmentDto>>($"/api/anexos?ownerType={Q(ownerType)}&ownerId={ownerId}");

    // Eventos
    public Task<PagedResult<EventDto>> EventsAsync() => api.GetAsync<PagedResult<EventDto>>("/api/eventos?upcomingOnly=true&pageSize=100");
    public Task<EventDto> EventAsync(Guid id) => api.GetAsync<EventDto>($"/api/eventos/{id}");
    public Task<EventDto> RegisterEventAsync(Guid id) => api.PostAsync<EventDto>($"/api/eventos/{id}/inscricao");
    public Task<EventDto> CancelEventRegistrationAsync(Guid id) => api.DeleteAsync<EventDto>($"/api/eventos/{id}/inscricao");

    // Associado e dependentes
    public Task<MemberDto> MyMemberAsync() => api.GetAsync<MemberDto>("/api/associados/eu");
    public Task<MemberDto> UpdateMyMemberAsync(UpdateMyProfileRequest req) => api.PutAsync<MemberDto>("/api/associados/eu", req);
    public Task<DependentDto> MyDependentProfileAsync() => api.GetAsync<DependentDto>("/api/dependentes/eu");
    public Task<List<DependentDto>> MyDependentsAsync() => api.GetAsync<List<DependentDto>>("/api/dependentes/meus");
    public Task<DependentDto> DependentAsync(Guid id) => api.GetAsync<DependentDto>($"/api/dependentes/{id}");
    public Task<DependentDto> CreateDependentAsync(SaveDependentRequest req) => api.PostAsync<DependentDto>("/api/dependentes", req);
    public Task<DependentDto> UpdateDependentAsync(Guid id, SaveDependentRequest req) => api.PutAsync<DependentDto>($"/api/dependentes/{id}", req);
    public Task RemoveDependentAsync(Guid id, string reason) => api.PostAsync($"/api/dependentes/{id}/remover", new StatusChangeRequest(reason));
    public Task<DependentDto> SetDependentPermissionAsync(Guid id, string code, bool value) =>
        api.PutAsync<DependentDto>($"/api/dependentes/{id}/permissoes", new SetDependentPermissionRequest(code, value));
    public Task<TemporaryPasswordDto> CreateDependentAccessAsync(Guid id, string? email) =>
        api.PostAsync<TemporaryPasswordDto>($"/api/dependentes/{id}/acesso", new CreateAccessRequest(email));

    // Manifestações
    public Task<List<ManifestationTypeDto>> ManifestationTypesAsync() => api.GetAsync<List<ManifestationTypeDto>>("/api/manifestacoes/tipos");
    public Task<PagedResult<ManifestationDto>> ManifestationsAsync() => api.GetAsync<PagedResult<ManifestationDto>>("/api/manifestacoes?pageSize=100");
    public Task<ManifestationDetailDto> ManifestationAsync(Guid id) => api.GetAsync<ManifestationDetailDto>($"/api/manifestacoes/{id}");
    public Task<ManifestationDetailDto> CreateManifestationAsync(CreateManifestationRequest req) => api.PostAsync<ManifestationDetailDto>("/api/manifestacoes", req);

    // Assembleias e votações
    public Task<PagedResult<AssemblyDto>> AssembliesAsync() => api.GetAsync<PagedResult<AssemblyDto>>("/api/assembleias?pageSize=100");
    public Task<AssemblyDto> AssemblyAsync(Guid id) => api.GetAsync<AssemblyDto>($"/api/assembleias/{id}");
    public Task<List<PollDto>> PollsAsync(Guid? assemblyId = null) =>
        api.GetAsync<List<PollDto>>("/api/votacoes?openOnly=false" + (assemblyId is { } a ? $"&assemblyId={a}" : ""));
    public Task<PollDto> PollAsync(Guid id) => api.GetAsync<PollDto>($"/api/votacoes/{id}");
    public Task<PollDto> VoteAsync(Guid pollId, Guid optionId) => api.PostAsync<PollDto>($"/api/votacoes/{pollId}/votos", new VoteRequest(optionId));

    // Notificações
    public Task<PagedResult<NotificationDto>> NotificationsAsync(int page) => api.GetAsync<PagedResult<NotificationDto>>($"/api/notificacoes?page={page}&pageSize=30");
    public Task<UnreadCountDto> UnreadCountAsync() => api.GetAsync<UnreadCountDto>("/api/notificacoes/nao-lidas");
    public Task MarkNotificationReadAsync(Guid id) => api.PostAsync($"/api/notificacoes/{id}/lida");
    public Task MarkAllNotificationsReadAsync() => api.PostAsync("/api/notificacoes/marcar-todas-lidas");
    public Task<List<NotificationPreferenceDto>> NotificationPreferencesAsync() => api.GetAsync<List<NotificationPreferenceDto>>("/api/notificacoes/preferencias");
    public Task<List<NotificationPreferenceDto>> SaveNotificationPreferencesAsync(List<NotificationPreferenceDto> prefs) =>
        api.PutAsync<List<NotificationPreferenceDto>>("/api/notificacoes/preferencias", prefs);
    public Task RegisterDeviceAsync(string token, string platform) => api.PostAsync("/api/notificacoes/dispositivos", new RegisterDeviceRequest(token, platform));
    public Task UnregisterDeviceAsync(string token) => api.DeleteAsync($"/api/notificacoes/dispositivos?token={Q(token)}");

    // Portaria
    public Task<AccessResultDto> ValidateAccessAsync(string code) => api.PostAsync<AccessResultDto>("/api/portaria/validar", new ValidateRequest(code, AuthService.DeviceName));
    public Task<AccessResultDto> ManualAccessAsync(ManualAccessRequest req) => api.PostAsync<AccessResultDto>("/api/portaria/manual", req);
    public Task<AccessResultDto> GuestEntryAsync(Guid guestId) => api.PostAsync<AccessResultDto>($"/api/portaria/convidados/{guestId}/entrada");
    public Task<AccessResultDto> GuestExitAsync(Guid guestId) => api.PostAsync<AccessResultDto>($"/api/portaria/convidados/{guestId}/saida");
    public Task<List<GatePersonDto>> SearchPeopleAsync(string term) => api.GetAsync<List<GatePersonDto>>($"/api/portaria/pessoas?term={Q(term)}");
    public Task<List<PresenceDto>> PresentAsync() => api.GetAsync<List<PresenceDto>>("/api/portaria/presentes");
    public Task<OccupancyDto> OccupancyAsync() => api.GetAsync<OccupancyDto>("/api/portaria/ocupacao");

    // Arquivos
    public Task<byte[]> FileAsync(Guid id) => api.GetBytesAsync($"/api/arquivos/{id}");
    public Task<FileDto> UploadAsync(byte[] content, string fileName, string contentType) => api.UploadAsync(content, fileName, contentType, FileScope.Staff);
}
