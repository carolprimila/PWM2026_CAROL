using Clubes.Mobile.Services;
using Clubes.Mobile.Views;

namespace Clubes.Mobile;

/// <summary>Navegação principal: abas inferiores conforme o perfil informado pela API (associado, dependente ou portaria).</summary>
public class AppShell : Shell
{
    private static bool _routesRegistered;

    public AppShell(IServiceProvider services, AuthService auth)
    {
        RegisterRoutes();
        FlyoutBehavior = FlyoutBehavior.Disabled;

        var tabs = new TabBar();
        void Add<TPage>(string title, string icon, string route) where TPage : Page =>
            tabs.Items.Add(new ShellContent
            {
                Title = title, Icon = icon, Route = route,
                ContentTemplate = new DataTemplate(() => services.GetRequiredService<TPage>()),
            });

        if (auth.Profile == AppProfile.Gate)
        {
            Add<GateScannerPage>("Leitor", "ic_qr.png", "leitor");
            Add<GatePresentPage>("Presentes", "ic_group.png", "presentes");
            Add<ProfilePage>("Perfil", "ic_person.png", "perfil");
        }
        else
        {
            Add<HomePage>("Início", "ic_home.png", "inicio");
            Add<CardPage>("Carteirinha", "ic_card.png", "carteirinha");
            // O dependente só vê Reservas quando o responsável e a administração liberaram.
            if (auth.CanReserve) Add<ReservationsPage>("Reservas", "ic_calendar.png", "reservas");
            Add<AnnouncementsPage>("Comunicados", "ic_megaphone.png", "comunicados");
            Add<ProfilePage>("Perfil", "ic_person.png", "perfil");
        }
        Items.Add(tabs);
    }

    private static void RegisterRoutes()
    {
        if (_routesRegistered) return;
        _routesRegistered = true;
        Routing.RegisterRoute("espacos", typeof(SpacePickerPage));
        Routing.RegisterRoute("nova-reserva", typeof(NewReservationPage));
        Routing.RegisterRoute("reserva", typeof(ReservationDetailPage));
        Routing.RegisterRoute("convidados", typeof(GuestsPage));
        Routing.RegisterRoute("novo-convidado", typeof(GuestFormPage));
        Routing.RegisterRoute("comunicado", typeof(AnnouncementDetailPage));
        Routing.RegisterRoute("eventos", typeof(EventsPage));
        Routing.RegisterRoute("evento", typeof(EventDetailPage));
        Routing.RegisterRoute("financeiro", typeof(FinancePage));
        Routing.RegisterRoute("cobranca", typeof(ChargeDetailPage));
        Routing.RegisterRoute("pagamentos", typeof(PaymentsPage));
        Routing.RegisterRoute("dependentes", typeof(DependentsPage));
        Routing.RegisterRoute("dependente", typeof(DependentDetailPage));
        Routing.RegisterRoute("dependente-form", typeof(DependentFormPage));
        Routing.RegisterRoute("manifestacoes", typeof(ManifestationsPage));
        Routing.RegisterRoute("manifestacao", typeof(ManifestationDetailPage));
        Routing.RegisterRoute("nova-manifestacao", typeof(ManifestationFormPage));
        Routing.RegisterRoute("assembleias", typeof(AssembliesPage));
        Routing.RegisterRoute("assembleia", typeof(AssemblyDetailPage));
        Routing.RegisterRoute("votacao", typeof(PollPage));
        Routing.RegisterRoute("notificacoes", typeof(NotificationsPage));
        Routing.RegisterRoute("notificacoes-preferencias", typeof(NotificationPreferencesPage));
        Routing.RegisterRoute("editar-perfil", typeof(EditProfilePage));
        Routing.RegisterRoute("alterar-senha", typeof(ChangePasswordPage));
        Routing.RegisterRoute("sessoes", typeof(SessionsPage));
        Routing.RegisterRoute("politicas", typeof(PoliciesPage));
        Routing.RegisterRoute("portaria-convidados", typeof(GateGuestsPage));
        Routing.RegisterRoute("portaria-busca", typeof(GateSearchPage));
    }
}
