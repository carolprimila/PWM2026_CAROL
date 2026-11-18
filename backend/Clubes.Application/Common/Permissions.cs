namespace Clubes.Application.Common;

/// <summary>
/// Catálogo de permissões do sistema. As funções (perfis) de cada clube são combinações livres
/// destas permissões, configuradas pela administração sem alteração de código.
/// </summary>
public static class Permissions
{
    public const string DashboardView = "dashboard.view";
    public const string ClubManage = "club.manage";
    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string MembersView = "members.view";
    public const string MembersManage = "members.manage";
    public const string MembersApprove = "members.approve";
    public const string DependentsView = "dependents.view";
    public const string DependentsManage = "dependents.manage";
    public const string DocumentsView = "documents.view";
    public const string EmployeesView = "employees.view";
    public const string EmployeesManage = "employees.manage";
    public const string FinanceView = "finance.view";
    public const string FinanceCharges = "finance.charges";
    public const string FinancePayments = "finance.payments";
    public const string FinanceRefund = "finance.refund";
    public const string FinanceAdjust = "finance.adjust";
    public const string FinanceEntries = "finance.entries";
    public const string FinanceClose = "finance.close";
    public const string FinanceRelease = "finance.release";
    public const string SpacesView = "spaces.view";
    public const string SpacesManage = "spaces.manage";
    public const string ReservationsView = "reservations.view";
    public const string ReservationsManage = "reservations.manage";
    public const string GuestsView = "guests.view";
    public const string GuestsManage = "guests.manage";
    public const string GateOperate = "gate.operate";
    public const string GateHistory = "gate.history";
    public const string EventsView = "events.view";
    public const string EventsManage = "events.manage";
    public const string AnnouncementsManage = "announcements.manage";
    public const string ManifestationsView = "manifestations.view";
    public const string ManifestationsManage = "manifestations.manage";
    public const string AssembliesView = "assemblies.view";
    public const string AssembliesManage = "assemblies.manage";
    public const string AssetsView = "assets.view";
    public const string AssetsManage = "assets.manage";
    public const string StockView = "stock.view";
    public const string StockManage = "stock.manage";
    public const string MaintenanceView = "maintenance.view";
    public const string MaintenanceManage = "maintenance.manage";
    public const string InspectionsView = "inspections.view";
    public const string InspectionsManage = "inspections.manage";
    public const string OccurrencesView = "occurrences.view";
    public const string OccurrencesManage = "occurrences.manage";
    public const string OccurrencesApproveCharge = "occurrences.approve-charge";
    public const string CalendarView = "calendar.view";
    public const string ReportsView = "reports.view";
    public const string AuditView = "audit.view";

    public record Info(string Code, string Group, string Description);

    public static readonly IReadOnlyList<Info> All =
    [
        new(DashboardView, "Geral", "Visualizar o painel inicial"),
        new(CalendarView, "Geral", "Visualizar o calendário administrativo"),
        new(ReportsView, "Geral", "Visualizar e exportar relatórios"),
        new(AuditView, "Geral", "Consultar a auditoria"),
        new(ClubManage, "Clube", "Editar dados institucionais do clube"),
        new(SettingsView, "Clube", "Visualizar configurações"),
        new(SettingsManage, "Clube", "Alterar configurações e políticas"),
        new(UsersView, "Acesso", "Visualizar usuários e sessões"),
        new(UsersManage, "Acesso", "Gerenciar usuários, bloqueios e senhas"),
        new(RolesManage, "Acesso", "Gerenciar funções e permissões"),
        new(MembersView, "Associados", "Consultar associados"),
        new(MembersManage, "Associados", "Cadastrar e editar associados"),
        new(MembersApprove, "Associados", "Aprovar, suspender e cancelar associados"),
        new(DependentsView, "Associados", "Consultar dependentes"),
        new(DependentsManage, "Associados", "Gerenciar dependentes e suas permissões"),
        new(DocumentsView, "Associados", "Visualizar documentos pessoais"),
        new(EmployeesView, "Equipe", "Consultar funcionários"),
        new(EmployeesManage, "Equipe", "Gerenciar funcionários"),
        new(FinanceView, "Financeiro", "Consultar cobranças, pagamentos e fluxo de caixa"),
        new(FinanceCharges, "Financeiro", "Gerar e cancelar cobranças"),
        new(FinancePayments, "Financeiro", "Registrar pagamentos na secretaria"),
        new(FinanceRefund, "Financeiro", "Estornar pagamentos"),
        new(FinanceAdjust, "Financeiro", "Aplicar descontos, multas, juros e taxas"),
        new(FinanceEntries, "Financeiro", "Gerenciar receitas e despesas"),
        new(FinanceClose, "Financeiro", "Realizar o fechamento mensal"),
        new(FinanceRelease, "Financeiro", "Liberar associados inadimplentes excepcionalmente"),
        new(SpacesView, "Espaços", "Consultar espaços"),
        new(SpacesManage, "Espaços", "Gerenciar espaços, horários, bloqueios e feriados"),
        new(ReservationsView, "Espaços", "Consultar reservas"),
        new(ReservationsManage, "Espaços", "Aprovar, recusar, cancelar e criar reservas"),
        new(GuestsView, "Portaria", "Consultar convidados"),
        new(GuestsManage, "Portaria", "Autorizar e gerenciar convidados"),
        new(GateOperate, "Portaria", "Validar QR Codes e registrar entradas e saídas"),
        new(GateHistory, "Portaria", "Consultar histórico de acessos e ocupação"),
        new(EventsView, "Comunicação", "Consultar eventos e inscrições"),
        new(EventsManage, "Comunicação", "Gerenciar eventos e presença"),
        new(AnnouncementsManage, "Comunicação", "Publicar e gerenciar comunicados"),
        new(ManifestationsView, "Comunicação", "Consultar reclamações e sugestões"),
        new(ManifestationsManage, "Comunicação", "Tratar reclamações e sugestões"),
        new(AssembliesView, "Comunicação", "Consultar assembleias e votações"),
        new(AssembliesManage, "Comunicação", "Gerenciar assembleias e votações"),
        new(AssetsView, "Operação", "Consultar patrimônio"),
        new(AssetsManage, "Operação", "Gerenciar patrimônio"),
        new(StockView, "Operação", "Consultar estoque"),
        new(StockManage, "Operação", "Movimentar e gerenciar estoque"),
        new(MaintenanceView, "Operação", "Consultar manutenções"),
        new(MaintenanceManage, "Operação", "Gerenciar manutenções"),
        new(InspectionsView, "Operação", "Consultar vistorias e entregas de chaves"),
        new(InspectionsManage, "Operação", "Realizar vistorias e entregas de chaves"),
        new(OccurrencesView, "Operação", "Consultar ocorrências"),
        new(OccurrencesManage, "Operação", "Registrar e tratar ocorrências"),
        new(OccurrencesApproveCharge, "Operação", "Aprovar cobranças por danos"),
    ];

    public static readonly IReadOnlySet<string> Codes = All.Select(p => p.Code).ToHashSet();

    /// <summary>Funções iniciais criadas com cada clube. São editáveis pela administração.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> DefaultRoles = new Dictionary<string, string[]>
    {
        ["Secretaria"] =
        [
            DashboardView, CalendarView, MembersView, MembersManage, DependentsView, DependentsManage, DocumentsView,
            FinanceView, FinancePayments, SpacesView, ReservationsView, ReservationsManage, GuestsView, GuestsManage,
            EventsView, EventsManage, AnnouncementsManage, ManifestationsView, ManifestationsManage, AssembliesView,
            InspectionsView, OccurrencesView, OccurrencesManage,
        ],
        ["Financeiro"] =
        [
            DashboardView, MembersView, DependentsView, FinanceView, FinanceCharges, FinancePayments, FinanceRefund,
            FinanceAdjust, FinanceEntries, FinanceClose, FinanceRelease, ReportsView,
        ],
        ["Portaria"] = [GateOperate, GateHistory, GuestsView, OccurrencesView, OccurrencesManage],
    };
}

/// <summary>Permissões individuais que podem ser concedidas a um dependente.</summary>
public static class DependentPermissions
{
    public const string RequestReservations = "reservations.request";
    public const string RegisterEvents = "events.register";
    public const string RegisterGuests = "guests.register";

    public static readonly IReadOnlyList<(string Code, string Description)> All =
    [
        (RequestReservations, "Solicitar reservas de espaços (sempre sujeitas à aprovação da administração)"),
        (RegisterEvents, "Inscrever-se em eventos"),
        (RegisterGuests, "Cadastrar convidados em nome do associado responsável"),
    ];
}
