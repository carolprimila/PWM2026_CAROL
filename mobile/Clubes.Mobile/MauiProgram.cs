using Clubes.Mobile.Services;
using Clubes.Mobile.ViewModels;
using Clubes.Mobile.Views;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using ZXing.Net.Maui.Controls;

namespace Clubes.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;

        // Serviços (uma instância por aplicativo)
        s.AddSingleton<AppConfig>();
        s.AddSingleton<SessionStore>();
        s.AddSingleton<ApiClient>();
        s.AddSingleton<AuthService>();
        s.AddSingleton<ClubApi>();
        s.AddSingleton<FileService>();
        s.AddSingleton<RealtimeService>();
        s.AddSingleton<NotificationState>();
        s.AddSingleton<IPushTokenProvider, NotConfiguredPushTokenProvider>();
        s.AddSingleton<IPushRegistration, PushRegistration>();
        s.AddSingleton<AppRouter>();

        // Acesso
        s.AddTransient<LoginViewModel>().AddTransient<LoginPage>();
        s.AddTransient<ForgotPasswordViewModel>().AddTransient<ForgotPasswordPage>();
        s.AddTransient<ResetPasswordViewModel>().AddTransient<ResetPasswordPage>();
        s.AddTransient<ChangePasswordViewModel>().AddTransient<ChangePasswordPage>();
        s.AddTransient<PrivacyViewModel>().AddTransient<PrivacyPage>();
        s.AddTransient<UnsupportedViewModel>().AddTransient<UnsupportedPage>();

        // Associado e dependente
        s.AddTransient<HomeViewModel>().AddTransient<HomePage>();
        s.AddTransient<CardViewModel>().AddTransient<CardPage>();
        s.AddTransient<ReservationsViewModel>().AddTransient<ReservationsPage>();
        s.AddTransient<ReservationDetailViewModel>().AddTransient<ReservationDetailPage>();
        s.AddTransient<SpacePickerViewModel>().AddTransient<SpacePickerPage>();
        s.AddTransient<NewReservationViewModel>().AddTransient<NewReservationPage>();
        s.AddTransient<GuestsViewModel>().AddTransient<GuestsPage>();
        s.AddTransient<GuestFormViewModel>().AddTransient<GuestFormPage>();
        s.AddTransient<AnnouncementsViewModel>().AddTransient<AnnouncementsPage>();
        s.AddTransient<AnnouncementDetailViewModel>().AddTransient<AnnouncementDetailPage>();
        s.AddTransient<EventsViewModel>().AddTransient<EventsPage>();
        s.AddTransient<EventDetailViewModel>().AddTransient<EventDetailPage>();
        s.AddTransient<FinanceViewModel>().AddTransient<FinancePage>();
        s.AddTransient<ChargeDetailViewModel>().AddTransient<ChargeDetailPage>();
        s.AddTransient<PaymentsViewModel>().AddTransient<PaymentsPage>();
        s.AddTransient<DependentsViewModel>().AddTransient<DependentsPage>();
        s.AddTransient<DependentFormViewModel>().AddTransient<DependentFormPage>();
        s.AddTransient<DependentDetailViewModel>().AddTransient<DependentDetailPage>();
        s.AddTransient<ManifestationsViewModel>().AddTransient<ManifestationsPage>();
        s.AddTransient<ManifestationFormViewModel>().AddTransient<ManifestationFormPage>();
        s.AddTransient<ManifestationDetailViewModel>().AddTransient<ManifestationDetailPage>();
        s.AddTransient<AssembliesViewModel>().AddTransient<AssembliesPage>();
        s.AddTransient<AssemblyDetailViewModel>().AddTransient<AssemblyDetailPage>();
        s.AddTransient<PollViewModel>().AddTransient<PollPage>();
        s.AddTransient<NotificationsViewModel>().AddTransient<NotificationsPage>();
        s.AddTransient<NotificationPreferencesViewModel>().AddTransient<NotificationPreferencesPage>();
        s.AddTransient<ProfileViewModel>().AddTransient<ProfilePage>();
        s.AddTransient<EditProfileViewModel>().AddTransient<EditProfilePage>();
        s.AddTransient<SessionsViewModel>().AddTransient<SessionsPage>();
        s.AddTransient<PoliciesViewModel>().AddTransient<PoliciesPage>();

        // Portaria
        s.AddTransient<GateScannerViewModel>().AddTransient<GateScannerPage>();
        s.AddTransient<GatePresentViewModel>().AddTransient<GatePresentPage>();
        s.AddTransient<GateGuestsViewModel>().AddTransient<GateGuestsPage>();
        s.AddTransient<GateSearchViewModel>().AddTransient<GateSearchPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
