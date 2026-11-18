using Clubes.Mobile.Models;
using Clubes.Mobile.ViewModels;

namespace Clubes.Mobile.Views;

// Code-behind das páginas: apenas liga o view model e repassa toques de itens de lista aos comandos.

public partial class HomePage : BasePage
{
    private readonly HomeViewModel _vm;
    public HomePage(HomeViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnReservationTapped(object? sender, TappedEventArgs e) => _vm.OpenReservationCommand.Execute(Ctx<ReservationDto>(sender));
    private void OnAnnouncementTapped(object? sender, TappedEventArgs e) => _vm.OpenAnnouncementCommand.Execute(Ctx<AnnouncementDto>(sender));
}

public partial class CardPage : BasePage
{
    public CardPage(CardViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class ReservationsPage : BasePage
{
    private readonly ReservationsViewModel _vm;
    public ReservationsPage(ReservationsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<ReservationDto>(sender));
}

public partial class ReservationDetailPage : BasePage
{
    public ReservationDetailPage(ReservationDetailViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class SpacePickerPage : BasePage
{
    private readonly SpacePickerViewModel _vm;
    public SpacePickerPage(SpacePickerViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<SpaceDto>(sender));
}

public partial class NewReservationPage : BasePage
{
    private readonly NewReservationViewModel _vm;
    public NewReservationPage(NewReservationViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnSlotTapped(object? sender, TappedEventArgs e) => _vm.ToggleSlot(Ctx<SlotItem>(sender));
    private void OnDayTapped(object? sender, TappedEventArgs e) => _vm.SelectDay(Ctx<DayItem>(sender));
    private void OnRemoveGuest(object? sender, EventArgs e) => _vm.RemoveGuest(Ctx<GuestInput>(sender));
}

public partial class GuestsPage : BasePage
{
    private readonly GuestsViewModel _vm;
    public GuestsPage(GuestsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnCancel(object? sender, EventArgs e) => _vm.CancelCommand.Execute(Ctx<GuestDto>(sender));
}

public partial class GuestFormPage : BasePage
{
    public GuestFormPage(GuestFormViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class AnnouncementsPage : BasePage
{
    private readonly AnnouncementsViewModel _vm;
    public AnnouncementsPage(AnnouncementsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<AnnouncementDto>(sender));
    private async void OnCategoryTapped(object? sender, TappedEventArgs e) => await _vm.SelectCategoryAsync(Ctx<CategoryChip>(sender));
}

public partial class AnnouncementDetailPage : BasePage
{
    private readonly AnnouncementDetailViewModel _vm;
    public AnnouncementDetailPage(AnnouncementDetailViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnAttachmentTapped(object? sender, TappedEventArgs e) => _vm.OpenAttachmentCommand.Execute(Ctx<AttachmentDto>(sender));
}

public partial class EventsPage : BasePage
{
    private readonly EventsViewModel _vm;
    public EventsPage(EventsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<EventDto>(sender));
}

public partial class EventDetailPage : BasePage
{
    public EventDetailPage(EventDetailViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class FinancePage : BasePage
{
    private readonly FinanceViewModel _vm;
    public FinancePage(FinanceViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<ChargeDto>(sender));
}

public partial class ChargeDetailPage : BasePage
{
    private readonly ChargeDetailViewModel _vm;
    public ChargeDetailPage(ChargeDetailViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnReceipt(object? sender, EventArgs e) => _vm.ReceiptCommand.Execute(Ctx<PaymentDto>(sender));
}

public partial class PaymentsPage : BasePage
{
    private readonly PaymentsViewModel _vm;
    public PaymentsPage(PaymentsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnReceipt(object? sender, EventArgs e) => _vm.ReceiptCommand.Execute(Ctx<PaymentDto>(sender));
}

public partial class DependentsPage : BasePage
{
    private readonly DependentsViewModel _vm;
    public DependentsPage(DependentsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<DependentDto>(sender));
}

public partial class DependentFormPage : BasePage
{
    public DependentFormPage(DependentFormViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class DependentDetailPage : BasePage
{
    private readonly DependentDetailViewModel _vm;
    public DependentDetailPage(DependentDetailViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }

    // O interruptor dispara a alteração no servidor; o view model redesenha com o estado confirmado (ou o anterior, em falha).
    private async void OnPermissionToggled(object? sender, ToggledEventArgs e)
    {
        if (Ctx<PermissionItem>(sender) is { } item && item.EnabledByMember != e.Value) await _vm.SetPermissionAsync(item, e.Value);
    }
}

public partial class ManifestationsPage : BasePage
{
    private readonly ManifestationsViewModel _vm;
    public ManifestationsPage(ManifestationsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<ManifestationDto>(sender));
}

public partial class ManifestationFormPage : BasePage
{
    public ManifestationFormPage(ManifestationFormViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class ManifestationDetailPage : BasePage
{
    public ManifestationDetailPage(ManifestationDetailViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class AssembliesPage : BasePage
{
    private readonly AssembliesViewModel _vm;
    public AssembliesPage(AssembliesViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpenAssembly(object? sender, TappedEventArgs e) => _vm.OpenAssemblyCommand.Execute(Ctx<AssemblyDto>(sender));
    private void OnOpenPoll(object? sender, TappedEventArgs e) => _vm.OpenPollCommand.Execute(Ctx<PollDto>(sender));
}

public partial class AssemblyDetailPage : BasePage
{
    private readonly AssemblyDetailViewModel _vm;
    public AssemblyDetailPage(AssemblyDetailViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnAttachmentTapped(object? sender, TappedEventArgs e) => _vm.OpenAttachmentCommand.Execute(Ctx<AttachmentDto>(sender));
    private void OnOpenPoll(object? sender, TappedEventArgs e) => _vm.OpenPollCommand.Execute(Ctx<PollDto>(sender));
}

public partial class PollPage : BasePage
{
    private readonly PollViewModel _vm;
    public PollPage(PollViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOptionTapped(object? sender, TappedEventArgs e) => _vm.Select(Ctx<PollOptionItem>(sender));
}

public partial class NotificationsPage : BasePage
{
    private readonly NotificationsViewModel _vm;
    public NotificationsPage(NotificationsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnOpen(object? sender, TappedEventArgs e) => _vm.OpenCommand.Execute(Ctx<NotificationDto>(sender));
}

public partial class NotificationPreferencesPage : BasePage
{
    public NotificationPreferencesPage(NotificationPreferencesViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class ProfilePage : BasePage
{
    public ProfilePage(ProfileViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class EditProfilePage : BasePage
{
    public EditProfilePage(EditProfileViewModel vm) { InitializeComponent(); BindingContext = vm; }
}

public partial class SessionsPage : BasePage
{
    private readonly SessionsViewModel _vm;
    public SessionsPage(SessionsViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    private void OnRevoke(object? sender, EventArgs e) => _vm.RevokeCommand.Execute(Ctx<SessionDto>(sender));
}

public partial class PoliciesPage : BasePage
{
    public PoliciesPage(PoliciesViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
