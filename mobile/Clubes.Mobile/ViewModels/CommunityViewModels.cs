using System.Collections.ObjectModel;
using Clubes.Mobile.Models;
using Clubes.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Clubes.Mobile.ViewModels;

// ---------- Convidados ----------
public partial class GuestsViewModel(ClubApi api, AuthService auth) : BaseViewModel
{
    public ObservableCollection<GuestDto> Items { get; } = [];
    public bool CanRegister => auth.CanRegisterGuests;

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Items.ReplaceWith((await api.GuestsAsync()).Items));

    [RelayCommand] private Task New() => Nav.GoToAsync("novo-convidado");

    [RelayCommand]
    private async Task Cancel(GuestDto? guest)
    {
        if (guest is null) return;
        if (!await Dialogs.ConfirmAsync("Cancelar convidado", $"Cancelar o cadastro de {guest.FullName} para {Fmt.Date(guest.VisitDate)}?", "Cancelar convidado", "Manter")) return;
        if (await RunAsync(() => api.CancelGuestAsync(guest.Id))) await Load();
    }
}

public class SpaceOption(Guid? id, string name)
{
    public Guid? Id { get; } = id;
    public string Name { get; } = name;
    public override string ToString() => Name;
}

public partial class GuestFormViewModel(ClubApi api) : BaseViewModel
{
    [ObservableProperty] public partial string FullName { get; set; } = "";
    [ObservableProperty] public partial string Document { get; set; } = "";
    [ObservableProperty] public partial string Phone { get; set; } = "";
    [ObservableProperty] public partial DateTime? VisitDate { get; set; } = DateTime.Today;
    [ObservableProperty] public partial SpaceOption? Space { get; set; }
    [ObservableProperty] public partial string? NameError { get; set; }
    [ObservableProperty] public partial string PolicyText { get; set; } = "";
    public DateTime MinDate { get; } = DateTime.Today;
    public ObservableCollection<SpaceOption> Spaces { get; } = [];

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var spacesTask = api.SpacesAsync();
        var policiesTask = api.PoliciesAsync();
        await Task.WhenAll(spacesTask, policiesTask);
        Spaces.ReplaceWith(new[] { new SpaceOption(null, "Nenhum espaço específico") }
            .Concat(spacesTask.Result.Where(s => s.IsActive).OrderBy(s => s.Name).Select(s => new SpaceOption(s.Id, s.Name))));
        Space = Spaces[0];
        var p = policiesTask.Result;
        PolicyText = $"Limite de {p.MaxGuestsPerMember} convidado(s) por associado por dia."
            + (p.GuestFee is > 0 ? $" Taxa por convidado: {Fmt.Money(p.GuestFee)}." : "");
    });

    [RelayCommand]
    private async Task Save()
    {
        NameError = FullName.Trim().Length < 3 ? "Informe o nome completo do convidado." : null;
        if (NameError is not null) return;
        var request = new CreateGuestRequest
        {
            FullName = FullName.Trim(),
            Document = string.IsNullOrWhiteSpace(Document) ? null : Document.Trim(),
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            VisitDate = DateOnly.FromDateTime(VisitDate ?? DateTime.Today),
            SpaceId = Space?.Id,
        };
        GuestDto? created = null;
        if (!await RunAsync(async () => created = await api.CreateGuestAsync(request))) return;
        await Dialogs.AlertAsync("Convidado cadastrado", $"{created!.FullName} — {created.StatusText}. {created.FeeText}.");
        await Nav.BackAsync();
    }
}

// ---------- Comunicados ----------
public record AnnouncementReadMessage(Guid Id);

public partial class CategoryChip(string label, AnnouncementCategory? category) : ObservableObject
{
    public string Label { get; } = label;
    public AnnouncementCategory? Category { get; } = category;
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

public partial class AnnouncementsViewModel : BaseViewModel, IRecipient<AnnouncementReadMessage>
{
    private const int PageSize = 15;
    private readonly ClubApi _api;
    private int _page;
    private int _total;
    private bool _loadingMore;

    public AnnouncementsViewModel(ClubApi api)
    {
        _api = api;
        Categories.Add(new CategoryChip("Todos", null) { IsSelected = true });
        foreach (var c in Enum.GetValues<AnnouncementCategory>()) Categories.Add(new CategoryChip(Labels.Of(c), c));
        WeakReferenceMessenger.Default.Register(this);
    }

    public ObservableCollection<CategoryChip> Categories { get; } = [];
    public ObservableCollection<AnnouncementDto> Items { get; } = [];
    private AnnouncementCategory? Selected => Categories.FirstOrDefault(c => c.IsSelected)?.Category;

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var result = await _api.FeedAsync(1, PageSize, Selected);
        _page = 1;
        _total = result.Total;
        Items.ReplaceWith(result.Items);
    });

    [RelayCommand]
    private async Task LoadMore()
    {
        if (_loadingMore || !HasLoaded || Items.Count >= _total) return;
        _loadingMore = true;
        try
        {
            var result = await _api.FeedAsync(_page + 1, PageSize, Selected);
            _page++;
            _total = result.Total;
            foreach (var item in result.Items.Where(i => Items.All(x => x.Id != i.Id))) Items.Add(item);
        }
        catch (ApiException) { /* tenta de novo na próxima rolagem */ }
        finally { _loadingMore = false; }
    }

    public async Task SelectCategoryAsync(CategoryChip? chip)
    {
        if (chip is null || chip.IsSelected) return;
        foreach (var c in Categories) c.IsSelected = ReferenceEquals(c, chip);
        HasLoaded = false;
        Items.Clear();
        await Load();
    }

    [RelayCommand] private Task Open(AnnouncementDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("comunicado", item.Id);

    /// <summary>O servidor confirmou a leitura: atualiza o indicador do item na lista.</summary>
    public void Receive(AnnouncementReadMessage message)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id != message.Id || Items[i].IsRead) continue;
            var item = Items[i];
            item.IsRead = true;
            Items[i] = item;
        }
    }
}

public partial class AnnouncementDetailViewModel(ClubApi api, FileService files) : BaseViewModel
{
    private Guid _id;

    [ObservableProperty] public partial AnnouncementDto? Item { get; set; }
    [ObservableProperty] public partial bool HasAttachments { get; set; }
    [ObservableProperty] public partial bool HasEvent { get; set; }
    public ObservableCollection<AttachmentDto> Attachments { get; } = [];

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var item = await api.AnnouncementAsync(_id);
        Item = item;
        HasEvent = item.EventId is not null;
        if (item.Attachments > 0) Attachments.ReplaceWith(await api.AttachmentsAsync("Announcement", _id));
        HasAttachments = Attachments.Count > 0;
        if (!item.IsRead)
        {
            try
            {
                await api.MarkAnnouncementReadAsync(_id);
                WeakReferenceMessenger.Default.Send(new AnnouncementReadMessage(_id));
            }
            catch (ApiException) { /* a leitura será registrada na próxima abertura */ }
        }
    });

    [RelayCommand]
    private Task OpenAttachment(AttachmentDto? attachment) =>
        attachment is null ? Task.CompletedTask : RunAsync(() => files.OpenFileAsync(attachment.FileId, attachment.FileName), "Não foi possível abrir o anexo");

    [RelayCommand]
    private Task OpenEvent() => Item?.EventId is { } id ? Nav.GoToAsync("evento", id) : Task.CompletedTask;
}

// ---------- Eventos ----------
public partial class EventsViewModel(ClubApi api) : BaseViewModel
{
    public ObservableCollection<EventDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Items.ReplaceWith((await api.EventsAsync()).Items));

    [RelayCommand] private Task Open(EventDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("evento", item.Id);
}

public partial class EventDetailViewModel(ClubApi api, AuthService auth) : BaseViewModel
{
    private Guid _id;

    [ObservableProperty] public partial EventDto? Item { get; set; }
    [ObservableProperty] public partial bool HasDescription { get; set; }
    [ObservableProperty] public partial bool ShowRegister { get; set; }
    [ObservableProperty] public partial bool ShowCancel { get; set; }
    [ObservableProperty] public partial string RegistrationInfo { get; set; } = "";
    [ObservableProperty] public partial bool HasCancelReason { get; set; }

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Apply(await api.EventAsync(_id)));

    private void Apply(EventDto e)
    {
        Item = e;
        HasDescription = !string.IsNullOrWhiteSpace(e.Description);
        HasCancelReason = e.Status == EventStatus.Cancelled && !string.IsNullOrWhiteSpace(e.CancelReason);
        var scheduled = e.Status == EventStatus.Scheduled;
        // Dependente só vê o botão quando possui a permissão "events.register"; o servidor valida prazos e vagas.
        ShowRegister = scheduled && e.RegistrationRequired && !e.IsRegistered && auth.CanRegisterEvents;
        ShowCancel = scheduled && e.IsRegistered && auth.CanRegisterEvents;
        RegistrationInfo = !e.RegistrationRequired ? "Este evento não exige inscrição."
            : e.IsRegistered ? "Você está inscrito neste evento."
            : !auth.CanRegisterEvents ? "A inscrição em eventos não está liberada para o seu cadastro. Fale com o associado responsável."
            : e.RegistrationDeadline is { } d ? $"Inscrições até {Fmt.DateTime(d)}." : "Inscrições abertas.";
    }

    [RelayCommand]
    private async Task Register()
    {
        EventDto? updated = null;
        if (!await RunAsync(async () => updated = await api.RegisterEventAsync(_id))) return;
        Apply(updated!);
        await Dialogs.AlertAsync("Inscrição confirmada", "Sua inscrição no evento foi registrada.");
    }

    [RelayCommand]
    private async Task CancelRegistration()
    {
        if (!await Dialogs.ConfirmAsync("Cancelar inscrição", "Deseja cancelar a sua inscrição neste evento?", "Cancelar inscrição", "Manter")) return;
        EventDto? updated = null;
        if (await RunAsync(async () => updated = await api.CancelEventRegistrationAsync(_id))) Apply(updated!);
    }
}

// ---------- Manifestações ----------
public partial class ManifestationsViewModel(ClubApi api) : BaseViewModel
{
    public ObservableCollection<ManifestationDto> Items { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Items.ReplaceWith((await api.ManifestationsAsync()).Items));

    [RelayCommand] private Task New() => Nav.GoToAsync("nova-manifestacao");
    [RelayCommand] private Task Open(ManifestationDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("manifestacao", item.Id);
}

public partial class ManifestationFormViewModel(ClubApi api, FileService files) : BaseViewModel
{
    [ObservableProperty] public partial ManifestationTypeDto? Type { get; set; }
    [ObservableProperty] public partial string Description { get; set; } = "";
    [ObservableProperty] public partial string? TypeError { get; set; }
    [ObservableProperty] public partial string? DescriptionError { get; set; }
    [ObservableProperty] public partial Guid? PhotoFileId { get; set; }
    [ObservableProperty] public partial bool HasPhoto { get; set; }
    [ObservableProperty] public partial bool NoPhoto { get; set; } = true;
    public ObservableCollection<ManifestationTypeDto> Types { get; } = [];

    protected override Task OnAppearingAsync() => HasLoaded ? Task.CompletedTask : Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Types.ReplaceWith((await api.ManifestationTypesAsync()).Where(t => t.IsActive)));

    [RelayCommand]
    private async Task AddPhoto()
    {
        FileDto? file = null;
        if (!await RunAsync(async () => file = await files.PickAndUploadPhotoAsync(), "Não foi possível enviar a foto") || file is null) return;
        PhotoFileId = file.Id;
        HasPhoto = true;
        NoPhoto = false;
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        PhotoFileId = null;
        HasPhoto = false;
        NoPhoto = true;
    }

    [RelayCommand]
    private async Task Save()
    {
        TypeError = Type is null ? "Escolha o tipo da manifestação." : null;
        DescriptionError = Description.Trim().Length < 10 ? "Descreva com ao menos 10 caracteres." : null;
        if (TypeError is not null || DescriptionError is not null) return;
        ManifestationDetailDto? created = null;
        if (!await RunAsync(async () => created = await api.CreateManifestationAsync(new CreateManifestationRequest(Type!.Id, Description.Trim(), PhotoFileId)))) return;
        await Dialogs.AlertAsync("Manifestação enviada", $"Protocolo {created!.Manifestation.Protocol}. Você pode acompanhar o andamento por aqui.");
        await Nav.BackAsync();
    }
}

public partial class ManifestationDetailViewModel(ClubApi api) : BaseViewModel
{
    private Guid _id;

    [ObservableProperty] public partial ManifestationDto? Item { get; set; }
    [ObservableProperty] public partial bool HasUpdates { get; set; }
    public ObservableCollection<ManifestationUpdateDto> Updates { get; } = [];

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var detail = await api.ManifestationAsync(_id);
        Item = detail.Manifestation;
        Updates.ReplaceWith(detail.Updates.OrderByDescending(u => u.At));
        HasUpdates = Updates.Count > 0;
    });
}

// ---------- Assembleias e votações (somente associados) ----------
public partial class AssembliesViewModel(ClubApi api) : BaseViewModel
{
    [ObservableProperty] public partial bool ShowPolls { get; set; } = true;
    [ObservableProperty] public partial bool ShowAssemblies { get; set; }
    public ObservableCollection<AssemblyDto> Assemblies { get; } = [];
    public ObservableCollection<PollDto> Polls { get; } = [];

    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var assembliesTask = api.AssembliesAsync();
        var pollsTask = api.PollsAsync();
        await Task.WhenAll(assembliesTask, pollsTask);
        Assemblies.ReplaceWith(assembliesTask.Result.Items.OrderByDescending(a => a.ScheduledAt));
        Polls.ReplaceWith(pollsTask.Result.OrderByDescending(p => p.IsOpenNow).ThenByDescending(p => p.ClosesAt ?? p.OpensAt));
    });

    [RelayCommand] private void TabPolls() => (ShowPolls, ShowAssemblies) = (true, false);
    [RelayCommand] private void TabAssemblies() => (ShowPolls, ShowAssemblies) = (false, true);
    [RelayCommand] private Task OpenAssembly(AssemblyDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("assembleia", item.Id);
    [RelayCommand] private Task OpenPoll(PollDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("votacao", item.Id);
}

public partial class AssemblyDetailViewModel(ClubApi api, FileService files) : BaseViewModel
{
    private Guid _id;

    [ObservableProperty] public partial AssemblyDto? Item { get; set; }
    [ObservableProperty] public partial bool HasAgenda { get; set; }
    [ObservableProperty] public partial bool HasMinutes { get; set; }
    [ObservableProperty] public partial bool HasMinutesFile { get; set; }
    [ObservableProperty] public partial bool HasAttachments { get; set; }
    [ObservableProperty] public partial bool HasPolls { get; set; }
    public ObservableCollection<AttachmentDto> Attachments { get; } = [];
    public ObservableCollection<PollDto> Polls { get; } = [];

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () =>
    {
        var itemTask = api.AssemblyAsync(_id);
        var attachmentsTask = api.AttachmentsAsync("Assembly", _id);
        var pollsTask = api.PollsAsync(_id);
        await Task.WhenAll(itemTask, attachmentsTask, pollsTask);
        Item = itemTask.Result;
        HasAgenda = !string.IsNullOrWhiteSpace(Item.Agenda);
        HasMinutes = !string.IsNullOrWhiteSpace(Item.Minutes);
        HasMinutesFile = Item.MinutesFileId is not null;
        Attachments.ReplaceWith(attachmentsTask.Result);
        HasAttachments = Attachments.Count > 0;
        Polls.ReplaceWith(pollsTask.Result);
        HasPolls = Polls.Count > 0;
    });

    [RelayCommand]
    private Task OpenMinutesFile() =>
        Item?.MinutesFileId is { } id ? RunAsync(() => files.OpenFileAsync(id, "ata-assembleia.pdf"), "Não foi possível abrir a ata") : Task.CompletedTask;

    [RelayCommand]
    private Task OpenAttachment(AttachmentDto? attachment) =>
        attachment is null ? Task.CompletedTask : RunAsync(() => files.OpenFileAsync(attachment.FileId, attachment.FileName), "Não foi possível abrir o documento");

    [RelayCommand] private Task OpenPoll(PollDto? item) => item is null ? Task.CompletedTask : Nav.GoToAsync("votacao", item.Id);
}

public partial class PollOptionItem(PollOptionDto dto, int? total) : ObservableObject
{
    public Guid Id => dto.Id;
    public string Text => dto.Text;
    public bool ShowVotes => dto.Votes is not null;
    public string VotesText => dto.Votes is { } v ? (total is > 0 ? $"{v} voto(s) · {v * 100.0 / total.Value:0.#}%" : $"{v} voto(s)") : "";
    public double Share => dto.Votes is { } v && total is > 0 ? (double)v / total.Value : 0;
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

public partial class PollViewModel(ClubApi api) : BaseViewModel
{
    private Guid _id;

    [ObservableProperty] public partial PollDto? Poll { get; set; }
    [ObservableProperty] public partial bool HasDescription { get; set; }
    [ObservableProperty] public partial bool CanVote { get; set; }
    [ObservableProperty] public partial bool HasVoted { get; set; }
    [ObservableProperty] public partial string InfoText { get; set; } = "";
    [ObservableProperty] public partial string ResultsText { get; set; } = "";
    public ObservableCollection<PollOptionItem> Options { get; } = [];

    protected override void OnParameters(IDictionary<string, object> query) => _id = IdFrom(query);
    protected override Task OnAppearingAsync() => Load();

    [RelayCommand]
    private Task Load() => LoadAsync(async () => Apply(await api.PollAsync(_id)));

    private void Apply(PollDto poll)
    {
        Poll = poll;
        HasDescription = !string.IsNullOrWhiteSpace(poll.Description);
        HasVoted = poll.HasVoted;
        CanVote = poll.IsOpenNow && !poll.HasVoted;
        // Os resultados só aparecem quando publicados (o servidor só envia a contagem nesse caso).
        var showResults = poll.ResultsPublished;
        Options.ReplaceWith(poll.Options.OrderBy(o => o.SortOrder).Select(o => new PollOptionItem(showResults ? o : new PollOptionDto { Id = o.Id, Text = o.Text, SortOrder = o.SortOrder }, poll.TotalVotes)));
        ResultsText = showResults && poll.TotalVotes is { } t ? $"Resultado publicado · {t} voto(s) no total" : "";
        InfoText = poll.HasVoted ? "Seu voto foi registrado. O voto não pode ser alterado."
            : poll.IsOpenNow ? "Escolha uma opção e confirme. Cada associado vota uma única vez."
            : poll.Status == PollStatus.Closed ? (showResults ? "Votação encerrada." : "Votação encerrada. O resultado ainda não foi publicado.")
            : "Esta votação ainda não está aberta.";
    }

    public void Select(PollOptionItem? option)
    {
        if (option is null || !CanVote || IsBusy) return;
        foreach (var o in Options) o.IsSelected = ReferenceEquals(o, option);
    }

    [RelayCommand]
    private async Task Vote()
    {
        var option = Options.FirstOrDefault(o => o.IsSelected);
        if (option is null)
        {
            await Dialogs.AlertAsync("Escolha uma opção", "Selecione a opção em que deseja votar.");
            return;
        }
        if (!await Dialogs.ConfirmAsync("Confirmar voto", $"Você está votando em:\n\n{option.Text}\n\nO voto não poderá ser alterado depois.", "Votar")) return;
        PollDto? updated = null;
        if (!await RunAsync(async () => updated = await api.VoteAsync(_id, option.Id), "Não foi possível registrar o voto")) return;
        Apply(updated!);
        await Dialogs.AlertAsync("Voto registrado", "Seu voto foi registrado com sucesso.");
    }
}
