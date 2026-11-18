using System.Collections.ObjectModel;
using Clubes.Mobile.Models;
using Clubes.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Clubes.Mobile.ViewModels;

// ---------- Dependentes (somente associados) ----------
public partial class DependentsViewModel(ClubApi api) : BaseViewModel
{
    [ObservableProperty] public partial string LimitText { get; set; } = "";
    public ObservableCollection<DependentDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var itemsTask = api.MyDependentsAsync();
        var policiesTask = api.PoliciesAsync();
        await Task.WhenAll(itemsTask, policiesTask);
        Items.ReplaceWith(itemsTask.Result);
        LimitText = $"{Items.Count} de {policiesTask.Result.MaxDependentsPerMember} dependentes permitidos. Novos cadastros aguardam a aprovação do clube.";
    });

    [RelayCommand] private Task New() => Nav.GoToAsync("dependente-form");
    [RelayCommand] private Task Open(DependentDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("dependente", item.Id);
}

public partial class DependentFormViewModel(ClubApi api) : BaseViewModel
{
    private Guid _id;
    private Guid? _photoFileId;

    public static IReadOnlyList<string> Genders { get; } = ["Feminino", "Masculino", "Outro", "Prefiro não informar"];
    public static IReadOnlyList<string> Relationships { get; } = ["Cônjuge", "Companheiro(a)", "Filho(a)", "Enteado(a)", "Pai/Mãe", "Neto(a)", "Outro"];

    [ObservableProperty] public partial string Title { get; set; } = "Novo dependente";
    [ObservableProperty] public partial string FullName { get; set; } = "";
    [ObservableProperty] public partial string Cpf { get; set; } = "";
    [ObservableProperty] public partial string Rg { get; set; } = "";
    [ObservableProperty] public partial DateTime? BirthDate { get; set; } = DateTime.Today.AddYears(-10);
    [ObservableProperty] public partial string? Gender { get; set; }
    [ObservableProperty] public partial string? Relationship { get; set; }
    [ObservableProperty] public partial string Email { get; set; } = "";
    [ObservableProperty] public partial string Phone { get; set; } = "";
    [ObservableProperty] public partial string? NameError { get; set; }
    [ObservableProperty] public partial string? RelationshipError { get; set; }
    [ObservableProperty] public partial string? EmailError { get; set; }
    public DateTime MaxDate { get; } = DateTime.Today;
    public bool IsEdit => _id != Guid.Empty;

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        if (!IsEdit) return;
        var d = await api.DependentAsync(_id);
        Title = "Editar dependente";
        FullName = d.FullName;
        Cpf = d.Cpf ?? "";
        Rg = d.Rg ?? "";
        if (d.BirthDate is { } b) BirthDate = b.ToDateTime(TimeOnly.MinValue);
        Gender = d.Gender;
        Relationship = d.Relationship;
        Email = d.Email ?? "";
        Phone = d.Phone ?? "";
        _photoFileId = d.PhotoFileId;
    });

    [RelayCommand]
    private async Task Save()
    {
        NameError = FullName.Trim().Length < 3 ? "Informe o nome completo." : null;
        RelationshipError = string.IsNullOrWhiteSpace(Relationship) ? "Informe o parentesco." : null;
        EmailError = !string.IsNullOrWhiteSpace(Email) && !Email.Contains('@') ? "Informe um e-mail válido." : null;
        if (NameError is not null || RelationshipError is not null || EmailError is not null) return;
        var request = new SaveDependentRequest
        {
            FullName = FullName.Trim(),
            Cpf = string.IsNullOrWhiteSpace(Cpf) ? null : Cpf,
            Rg = string.IsNullOrWhiteSpace(Rg) ? null : Rg.Trim(),
            BirthDate = BirthDate is { } birth ? DateOnly.FromDateTime(birth) : null,
            Gender = string.IsNullOrWhiteSpace(Gender) ? null : Gender,
            Relationship = Relationship!.Trim(),
            PhotoFileId = _photoFileId,
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone,
        };
        DependentDto? saved = null;
        if (!await RunAsync(async () => saved = IsEdit ? await api.UpdateDependentAsync(_id, request) : await api.CreateDependentAsync(request))) return;
        await Dialogs.AlertAsync(IsEdit ? "Dependente atualizado" : "Dependente cadastrado", $"{saved!.FullName} — {saved.StatusText}.");
        await Nav.BackAsync();
    }
}

public class PermissionItem(DependentPermissionDto dto, bool memberCanToggle)
{
    public string Code => dto.Code;
    public string Title => dto.Title;
    public bool EnabledByMember => dto.EnabledByMember;
    public string EffectiveText => dto.EffectiveText;
    public ChipKind EffectiveKind => dto.EffectiveKind;
    public bool CanToggle => dto.AllowedByAdmin && memberCanToggle;
    public string Hint => !dto.AllowedByAdmin ? "A administração do clube ainda não liberou esta permissão."
        : !memberCanToggle ? "O clube não permite que o associado altere as permissões dos dependentes."
        : dto.EnabledByMember ? "Habilitada por você." : "Liberada pelo clube. Habilite para valer.";
}

public partial class DependentDetailViewModel(ClubApi api) : BaseViewModel
{
    private Guid _id;
    private bool _memberCanToggle;

    [ObservableProperty] public partial DependentDto? Item { get; set; }
    [ObservableProperty] public partial string InfoText { get; set; } = "";
    [ObservableProperty] public partial bool HasStatusReason { get; set; }
    [ObservableProperty] public partial string AccessText { get; set; } = "";
    [ObservableProperty] public partial bool CanCreateAccess { get; set; }
    [ObservableProperty] public partial bool CanManage { get; set; }
    public ObservableCollection<PermissionItem> Permissions { get; } = [];

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var itemTask = api.DependentAsync(_id);
        var policiesTask = api.PoliciesAsync();
        await Task.WhenAll(itemTask, policiesTask);
        _memberCanToggle = policiesTask.Result.MemberCanToggleDependentPermissions;
        Apply(itemTask.Result);
    });

    private void Apply(DependentDto d)
    {
        Item = d;
        var parts = new List<string> { d.Relationship };
        if (d.BirthDate is { } b) parts.Add($"Nascimento: {Fmt.Date(b)}");
        if (!string.IsNullOrWhiteSpace(d.Cpf)) parts.Add($"CPF: {d.Cpf}");
        if (!string.IsNullOrWhiteSpace(d.Phone)) parts.Add($"Telefone: {d.Phone}");
        if (!string.IsNullOrWhiteSpace(d.Email)) parts.Add($"E-mail: {d.Email}");
        InfoText = string.Join("\n", parts);
        HasStatusReason = !string.IsNullOrWhiteSpace(d.StatusReason);
        AccessText = d.HasAccess ? $"Acesso ao aplicativo criado ({d.AccessEmail})." : "Este dependente ainda não tem acesso ao aplicativo.";
        CanManage = d.Status != DependentStatus.Removed;
        CanCreateAccess = CanManage && !d.HasAccess;
        Permissions.ReplaceWith(d.Permissions.Select(p => new PermissionItem(p, _memberCanToggle)));
    }

    /// <summary>Altera a permissão no servidor. O interruptor só permanece na nova posição se o servidor confirmar.</summary>
    public async Task SetPermissionAsync(PermissionItem? permission, bool value)
    {
        if (permission is null || Item is null || permission.EnabledByMember == value) return;
        DependentDto? updated = null;
        var ok = permission.CanToggle && await RunAsync(async () => updated = await api.SetDependentPermissionAsync(_id, permission.Code, value));
        Apply(ok ? updated! : Item); // em falha, redesenha com o estado confirmado anterior
    }

    [RelayCommand] private Task Edit() => Nav.GoToAsync("dependente-form", _id);

    [RelayCommand]
    private async Task CreateAccess()
    {
        if (Item is null) return;
        var email = await Dialogs.PromptAsync("Criar acesso ao aplicativo", $"Informe o e-mail que {Item.FullName} usará para entrar.", "E-mail", 150, Keyboard.Email, Item.Email ?? "");
        if (email is null) return;
        TemporaryPasswordDto? result = null;
        if (!await RunAsync(async () => result = await api.CreateDependentAccessAsync(_id, string.IsNullOrWhiteSpace(email) ? null : email.Trim()))) return;
        // A senha temporária é exibida uma única vez e não fica armazenada no aparelho.
        var copy = await Dialogs.ConfirmAsync("Acesso criado",
            $"E-mail: {result!.Email}\nSenha temporária: {result.TemporaryPassword}\n\nAnote ou copie agora: a senha não será exibida novamente. No primeiro acesso será obrigatório trocá-la."
            + (result.EmailSent ? "\n\nA senha também foi enviada para o e-mail informado." : ""),
            "Copiar senha", "Fechar");
        if (copy) await Clipboard.Default.SetTextAsync(result.TemporaryPassword);
        await Load();
    }

    [RelayCommand]
    private async Task Remove()
    {
        if (Item is null) return;
        if (!await Dialogs.ConfirmAsync("Remover dependente", $"Remover {Item.FullName} dos seus dependentes? O acesso ao clube e ao aplicativo será encerrado.", "Remover", "Manter")) return;
        var reason = await Dialogs.PromptAsync("Motivo da remoção", "Informe o motivo (obrigatório).", "Motivo", 500);
        if (reason is null) return;
        if (reason.Trim().Length < 3)
        {
            await Dialogs.AlertAsync("Motivo obrigatório", "Informe o motivo da remoção com ao menos 3 caracteres.");
            return;
        }
        if (!await RunAsync(() => api.RemoveDependentAsync(_id, reason.Trim()))) return;
        await Dialogs.AlertAsync("Dependente removido", "A remoção foi registrada.");
        await Nav.BackAsync();
    }
}

// ---------- Notificações ----------
public partial class NotificationsViewModel(ClubApi api, NotificationState center, AuthService auth) : BaseViewModel
{
    private int _page;
    private int _total;
    private bool _loadingMore;
    public ObservableCollection<NotificationDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var result = await api.NotificationsAsync(1);
        _page = 1;
        _total = result.Total;
        Items.ReplaceWith(result.Items);
        await center.RefreshAsync();
    });

    [RelayCommand]
    private async Task LoadMore()
    {
        if (_loadingMore || !HasLoaded || Items.Count >= _total) return;
        _loadingMore = true;
        try
        {
            var result = await api.NotificationsAsync(_page + 1);
            _page++;
            _total = result.Total;
            foreach (var item in result.Items.Where(i => Items.All(x => x.Id != i.Id))) Items.Add(item);
        }
        catch (ApiException) { /* tenta de novo na próxima rolagem */ }
        finally { _loadingMore = false; }
    }

    [RelayCommand]
    private async Task MarkAll()
    {
        if (await RunAsync(api.MarkAllNotificationsReadAsync)) await Load();
    }

    [RelayCommand] private Task Preferences() => Nav.GoToAsync("notificacoes-preferencias");

    [RelayCommand]
    private async Task Open(NotificationDto? item)
    {
        if (item is null) return;
        if (item.IsUnread)
        {
            if (!await RunAsync(() => api.MarkNotificationReadAsync(item.Id))) return;
            var index = Items.IndexOf(item);
            item.ReadAt = DateTime.UtcNow;
            if (index >= 0) Items[index] = item;
            _ = center.RefreshAsync();
        }
        if (item.EntityId is not { } id) return;
        var route = item.EntityType switch
        {
            "Reservation" => "reserva",
            "Announcement" => "comunicado",
            "Manifestation" => "manifestacao",
            "ClubEvent" => "evento",
            "Charge" when auth.IsMember => "cobranca",
            "Assembly" when auth.IsMember => "assembleia",
            "Poll" when auth.IsMember => "votacao",
            "Dependent" when auth.IsMember => "dependente",
            _ => null,
        };
        if (route is not null) await Nav.GoToAsync(route, id);
    }
}

public partial class NotificationPreferencesViewModel(ClubApi api) : BaseViewModel
{
    public ObservableCollection<NotificationPreferenceDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Items.ReplaceWith(await api.NotificationPreferencesAsync()));

    [RelayCommand]
    private async Task Save()
    {
        List<NotificationPreferenceDto>? saved = null;
        if (!await RunAsync(async () => saved = await api.SaveNotificationPreferencesAsync(Items.ToList()))) return;
        Items.ReplaceWith(saved!);
        await Dialogs.AlertAsync("Preferências salvas", "Suas preferências de notificação foram atualizadas.");
    }
}

// ---------- Perfil ----------
public partial class ProfileViewModel(ClubApi api, AuthService auth, AppRouter router) : BaseViewModel
{
    public bool IsMember => auth.IsMember;
    public bool IsDependent => auth.IsDependent;
    public bool IsClubUser => auth.IsMember || auth.IsDependent;
    public string ClubName => auth.ClubName;

    [ObservableProperty] public partial string Name { get; set; } = "";
    [ObservableProperty] public partial string Subtitle { get; set; } = "";
    [ObservableProperty] public partial Guid? PhotoFileId { get; set; }
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    [ObservableProperty] public partial ChipKind StatusKind { get; set; }
    [ObservableProperty] public partial string InfoText { get; set; } = "";
    public ObservableCollection<DependentPermissionDto> Permissions { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var user = auth.User;
        Name = user?.Name ?? "";
        if (IsMember)
        {
            var m = await api.MyMemberAsync();
            Name = m.FullName;
            Subtitle = $"Associado nº {m.Number}";
            PhotoFileId = m.PhotoFileId;
            StatusText = Labels.Of(m.Status);
            StatusKind = m.Status == MemberStatus.Active ? ChipKind.Success : m.Status == MemberStatus.PendingApproval ? ChipKind.Warning : ChipKind.Danger;
            var address = string.Join(", ", new[] { m.Street, m.AddressNumber, m.Complement, m.District, m.City, m.State }.Where(v => !string.IsNullOrWhiteSpace(v)));
            InfoText = string.Join("\n", new[]
            {
                $"CPF: {m.Cpf}",
                $"E-mail: {m.Email ?? "—"}",
                $"Telefone: {m.Phone ?? "—"}",
                $"Endereço: {(address.Length > 0 ? address : "—")}" + (string.IsNullOrWhiteSpace(m.ZipCode) ? "" : $" · CEP {m.ZipCode}"),
                $"Associado desde: {Fmt.Date(m.JoinedAt)}",
            });
        }
        else if (IsDependent)
        {
            var d = await api.MyDependentProfileAsync();
            Name = d.FullName;
            Subtitle = $"Dependente de {d.MemberName}";
            PhotoFileId = d.PhotoFileId;
            StatusText = d.StatusText;
            StatusKind = d.StatusKind;
            InfoText = string.Join("\n", new[]
            {
                $"Parentesco: {d.Relationship}",
                $"Nascimento: {Fmt.Date(d.BirthDate)}",
                $"E-mail: {d.Email ?? "—"}",
                $"Telefone: {d.Phone ?? "—"}",
            });
            Permissions.ReplaceWith(d.Permissions);
        }
        else
        {
            Subtitle = "Equipe · Portaria";
            InfoText = $"E-mail: {user?.Email}";
        }
    });

    [RelayCommand] private Task EditProfile() => Nav.GoToAsync("editar-perfil");
    [RelayCommand] private Task Dependents() => Nav.GoToAsync("dependentes");
    [RelayCommand] private Task ChangePassword() => Nav.GoToAsync("alterar-senha");
    [RelayCommand] private Task Sessions() => Nav.GoToAsync("sessoes");
    [RelayCommand] private Task Policies() => Nav.GoToAsync("politicas");
    [RelayCommand] private Task NotificationPreferences() => Nav.GoToAsync("notificacoes-preferencias");

    [RelayCommand]
    private async Task Logout()
    {
        if (!await Dialogs.ConfirmAsync("Sair", "Deseja sair da sua conta neste aparelho?", "Sair")) return;
        if (IsBusy) return;
        IsBusy = true;
        try { await router.LogoutAsync(); }
        finally { IsBusy = false; }
    }
}

public partial class EditProfileViewModel(ClubApi api, FileService files) : BaseViewModel
{
    [ObservableProperty] public partial Guid? PhotoFileId { get; set; }
    [ObservableProperty] public partial string Phone { get; set; } = "";
    [ObservableProperty] public partial string ZipCode { get; set; } = "";
    [ObservableProperty] public partial string Street { get; set; } = "";
    [ObservableProperty] public partial string AddressNumber { get; set; } = "";
    [ObservableProperty] public partial string Complement { get; set; } = "";
    [ObservableProperty] public partial string District { get; set; } = "";
    [ObservableProperty] public partial string City { get; set; } = "";
    [ObservableProperty] public partial string State { get; set; } = "";
    [ObservableProperty] public partial string? StateError { get; set; }

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var m = await api.MyMemberAsync();
        PhotoFileId = m.PhotoFileId;
        Phone = m.Phone ?? "";
        ZipCode = m.ZipCode ?? "";
        Street = m.Street ?? "";
        AddressNumber = m.AddressNumber ?? "";
        Complement = m.Complement ?? "";
        District = m.District ?? "";
        City = m.City ?? "";
        State = m.State ?? "";
    });

    /// <summary>Envia a nova foto; ela só passa a valer no cadastro ao salvar.</summary>
    [RelayCommand]
    private async Task ChangePhoto()
    {
        FileDto? file = null;
        if (await RunAsync(async () => file = await files.PickAndUploadPhotoAsync(), "Não foi possível enviar a foto") && file is not null)
            PhotoFileId = file.Id;
    }

    [RelayCommand]
    private async Task Save()
    {
        StateError = State.Length is 0 or 2 ? null : "Use a sigla com 2 letras.";
        if (StateError is not null) return;
        static string? N(string v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        var request = new UpdateMyProfileRequest
        {
            Phone = N(Phone), ZipCode = N(ZipCode), Street = N(Street), AddressNumber = N(AddressNumber), Complement = N(Complement),
            District = N(District), City = N(City), State = N(State), PhotoFileId = PhotoFileId,
        };
        if (!await RunAsync(() => api.UpdateMyMemberAsync(request))) return;
        await Dialogs.AlertAsync("Cadastro atualizado", "Seus dados foram salvos.");
        await Nav.BackAsync();
    }
}

public partial class SessionsViewModel(AuthService auth) : BaseViewModel
{
    public ObservableCollection<SessionDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Items.ReplaceWith(await auth.SessionsAsync()));

    [RelayCommand]
    private async Task Revoke(SessionDto? session)
    {
        if (session is null || session.IsCurrent) return;
        if (!await Dialogs.ConfirmAsync("Encerrar sessão", $"Encerrar a sessão em \"{session.DeviceText}\"? Será preciso entrar novamente naquele aparelho.", "Encerrar")) return;
        if (await RunAsync(() => auth.RevokeSessionAsync(session.Id))) await Load();
    }
}

public record PolicySection(string Title, string Text);

public partial class PoliciesViewModel(ClubApi api, AuthService auth) : BaseViewModel
{
    public string ClubName => auth.ClubName;
    public ObservableCollection<PolicySection> Sections { get; } = [];

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var p = await api.PoliciesAsync();
        var sections = new List<PolicySection>
        {
            new("Limites", $"Até {p.MaxDependentsPerMember} dependentes por associado.\nAté {p.MaxGuestsPerMember} convidado(s) por associado."
                + (p.GuestFee is > 0 ? $"\nTaxa por convidado: {Fmt.Money(p.GuestFee)}." : "")),
        };
        void Add(string title, string? text) { if (!string.IsNullOrWhiteSpace(text)) sections.Add(new(title, text.Trim())); }
        Add("Uso dos espaços", p.SpaceUsagePolicy);
        Add("Piscina", p.PoolPolicy);
        Add("Menores de idade", p.MinorsRules);
        Add("Acompanhamento de menores", p.MinorsAccompanimentRules);
        Add("Feriados", p.HolidayRules);
        Add("Eventos", p.EventProcedures);
        Add($"Política de privacidade" + (string.IsNullOrWhiteSpace(p.PrivacyPolicyVersion) ? "" : $" (versão {p.PrivacyPolicyVersion})"), p.PrivacyPolicy);
        Sections.ReplaceWith(sections);
    });
}
