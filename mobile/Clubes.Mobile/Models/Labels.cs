using System.Globalization;

namespace Clubes.Mobile.Models;

/// <summary>Formatação de valores para exibição (pt-BR).</summary>
public static class Fmt
{
    public static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static string Money(decimal value) => value.ToString("C2", Br);
    public static string Money(decimal? value) => value is null ? "—" : Money(value.Value);
    public static string DateTime(DateTime utc) => Local(utc).ToString("dd/MM/yyyy HH:mm", Br);
    public static string DateTime(DateTime? utc) => utc is null ? "—" : DateTime(utc.Value);
    public static string Date(DateTime utc) => Local(utc).ToString("dd/MM/yyyy", Br);
    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Br);
    public static string Date(DateOnly? date) => date is null ? "—" : Date(date.Value);
    public static string Time(DateTime utc) => Local(utc).ToString("HH:mm", Br);
    public static string LongDate(DateTime utc) => Local(utc).ToString("ddd, dd 'de' MMM", Br);
    public static string LongDate(DateOnly date) => date.ToString("dddd, dd 'de' MMMM", Br);
    public static string Minutes(int minutes) => $"{minutes / 60:00}:{minutes % 60:00}";
    public static string Period(DateTime startUtc, DateTime endUtc)
    {
        var (s, e) = (Local(startUtc), Local(endUtc));
        return s.Date == e.Date
            ? $"{s.ToString("ddd, dd/MM", Br)} · {s:HH:mm} às {e:HH:mm}"
            : $"{s.ToString("dd/MM HH:mm", Br)} até {e.ToString("dd/MM HH:mm", Br)}";
    }
    public static DateTime Local(DateTime value) => value.Kind == DateTimeKind.Local ? value : System.DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();
    public static string ApiDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string ApiDate(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string FileSize(long bytes) => bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";
}

/// <summary>Rótulos em português para os valores devolvidos pela API.</summary>
public static class Labels
{
    public static string Of(ReservationStatus s) => s switch
    {
        ReservationStatus.Pending => "Aguardando aprovação", ReservationStatus.Approved => "Aprovada", ReservationStatus.Rejected => "Recusada",
        ReservationStatus.Cancelled => "Cancelada", ReservationStatus.Suspended => "Suspensa", ReservationStatus.Completed => "Concluída", _ => s.ToString(),
    };
    public static ChipKind Kind(ReservationStatus s) => s switch
    {
        ReservationStatus.Pending => ChipKind.Warning, ReservationStatus.Approved => ChipKind.Success, ReservationStatus.Rejected => ChipKind.Danger,
        ReservationStatus.Cancelled => ChipKind.Neutral, ReservationStatus.Suspended => ChipKind.Danger, ReservationStatus.Completed => ChipKind.Info, _ => ChipKind.Neutral,
    };

    public static string Of(SpaceType t) => t switch
    {
        SpaceType.Court => "Quadras", SpaceType.Kiosk => "Quiosques", SpaceType.PartyHall => "Salão de festas", SpaceType.Pool => "Piscinas", _ => "Outros espaços",
    };

    public static string Of(SpaceChargeMode? m) => m switch
    {
        SpaceChargeMode.NextMonthlyBill => "Cobrança na próxima mensalidade", SpaceChargeMode.Advance => "Pagamento antecipado", _ => "",
    };

    public static string Of(GuestAuthorization a) => a switch
    {
        GuestAuthorization.Pending => "Aguardando autorização", GuestAuthorization.Authorized => "Autorizado", GuestAuthorization.Denied => "Não autorizado", _ => a.ToString(),
    };
    public static ChipKind Kind(GuestAuthorization a) => a switch
    {
        GuestAuthorization.Pending => ChipKind.Warning, GuestAuthorization.Authorized => ChipKind.Success, GuestAuthorization.Denied => ChipKind.Danger, _ => ChipKind.Neutral,
    };

    public static string Of(AnnouncementCategory c) => c switch
    {
        AnnouncementCategory.Communication => "Comunicado", AnnouncementCategory.Notice => "Aviso", AnnouncementCategory.News => "Notícia",
        AnnouncementCategory.Important => "Importante", AnnouncementCategory.Operation => "Funcionamento", AnnouncementCategory.Event => "Evento",
        AnnouncementCategory.Financial => "Financeiro", _ => c.ToString(),
    };
    public static string Of(Priority p) => p switch { Priority.High => "Alta prioridade", Priority.Urgent => "Urgente", _ => "" };

    public static string Of(EventStatus s) => s switch { EventStatus.Scheduled => "Programado", EventStatus.Cancelled => "Cancelado", EventStatus.Finished => "Encerrado", _ => s.ToString() };
    public static ChipKind Kind(EventStatus s) => s switch { EventStatus.Scheduled => ChipKind.Info, EventStatus.Cancelled => ChipKind.Danger, _ => ChipKind.Neutral };

    public static string Of(ChargeStatus s) => s switch { ChargeStatus.Open => "Em aberto", ChargeStatus.Paid => "Paga", ChargeStatus.Cancelled => "Cancelada", _ => s.ToString() };
    public static string Of(ChargeType t) => t switch
    {
        ChargeType.Monthly => "Mensalidade", ChargeType.Fine => "Multa", ChargeType.Fee => "Taxa", ChargeType.Reservation => "Reserva",
        ChargeType.Guest => "Convidado", ChargeType.Damage => "Dano", ChargeType.Penalty => "Penalidade", _ => "Outros",
    };
    public static string Of(AdjustmentKind k) => k switch
    {
        AdjustmentKind.Discount => "Desconto", AdjustmentKind.Addition => "Acréscimo", AdjustmentKind.Fine => "Multa", AdjustmentKind.Interest => "Juros", AdjustmentKind.Fee => "Taxa", _ => k.ToString(),
    };
    public static string Of(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "Dinheiro", PaymentMethod.Pix => "Pix", PaymentMethod.CreditCard => "Cartão de crédito", PaymentMethod.DebitCard => "Cartão de débito",
        PaymentMethod.BankSlip => "Boleto", PaymentMethod.Transfer => "Transferência", _ => m.ToString(),
    };
    public static string Of(PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "Aguardando confirmação", PaymentStatus.Confirmed => "Confirmado", PaymentStatus.Failed => "Não concluído", PaymentStatus.Refunded => "Estornado", _ => s.ToString(),
    };
    public static ChipKind Kind(PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => ChipKind.Warning, PaymentStatus.Confirmed => ChipKind.Success, PaymentStatus.Failed => ChipKind.Danger, _ => ChipKind.Neutral,
    };
    public static string Of(PaymentChannel c) => c == PaymentChannel.App ? "Aplicativo" : "Secretaria";

    public static string Of(DependentStatus s) => s switch
    {
        DependentStatus.PendingApproval => "Aguardando aprovação", DependentStatus.Active => "Ativo", DependentStatus.Suspended => "Suspenso",
        DependentStatus.Rejected => "Recusado", DependentStatus.Removed => "Removido", _ => s.ToString(),
    };
    public static ChipKind Kind(DependentStatus s) => s switch
    {
        DependentStatus.PendingApproval => ChipKind.Warning, DependentStatus.Active => ChipKind.Success, DependentStatus.Suspended => ChipKind.Danger,
        DependentStatus.Rejected => ChipKind.Danger, _ => ChipKind.Neutral,
    };
    public static string Of(MemberStatus s) => s switch
    {
        MemberStatus.PendingApproval => "Aguardando aprovação", MemberStatus.Active => "Ativo", MemberStatus.Suspended => "Suspenso", MemberStatus.Cancelled => "Cancelado", _ => s.ToString(),
    };

    /// <summary>Situação cadastral devolvida como texto (cartão e busca da portaria).</summary>
    public static string PersonStatus(string? status) => status switch
    {
        "PendingApproval" => "Aguardando aprovação", "Active" => "Ativo", "Suspended" => "Suspenso", "Cancelled" => "Cancelado",
        "Rejected" => "Recusado", "Removed" => "Removido", null or "" => "—", _ => status,
    };
    public static ChipKind PersonStatusKind(string? status) => status switch
    {
        "Active" => ChipKind.Success, "PendingApproval" => ChipKind.Warning, "Suspended" or "Cancelled" or "Rejected" => ChipKind.Danger, _ => ChipKind.Neutral,
    };

    public static string Of(ManifestationStatus s) => s switch
    {
        ManifestationStatus.Received => "Recebida", ManifestationStatus.UnderReview => "Em análise", ManifestationStatus.InProgress => "Em andamento",
        ManifestationStatus.Accepted => "Aceita", ManifestationStatus.ForwardedToAssembly => "Encaminhada à assembleia", ManifestationStatus.Resolved => "Resolvida",
        ManifestationStatus.Refused => "Recusada", _ => s.ToString(),
    };
    public static ChipKind Kind(ManifestationStatus s) => s switch
    {
        ManifestationStatus.Received => ChipKind.Neutral, ManifestationStatus.UnderReview or ManifestationStatus.InProgress => ChipKind.Warning,
        ManifestationStatus.Accepted or ManifestationStatus.Resolved => ChipKind.Success, ManifestationStatus.ForwardedToAssembly => ChipKind.Info,
        ManifestationStatus.Refused => ChipKind.Danger, _ => ChipKind.Neutral,
    };

    public static string Of(AssemblyStatus s) => s switch
    {
        AssemblyStatus.Scheduled => "Agendada", AssemblyStatus.InProgress => "Em andamento", AssemblyStatus.Finished => "Encerrada", AssemblyStatus.Cancelled => "Cancelada", _ => s.ToString(),
    };
    public static ChipKind Kind(AssemblyStatus s) => s switch
    {
        AssemblyStatus.Scheduled => ChipKind.Info, AssemblyStatus.InProgress => ChipKind.Success, AssemblyStatus.Cancelled => ChipKind.Danger, _ => ChipKind.Neutral,
    };

    public static string Of(NotificationCategory c) => c switch
    {
        NotificationCategory.General => "Geral", NotificationCategory.Finance => "Financeiro", NotificationCategory.Reservation => "Reservas",
        NotificationCategory.Dependent => "Dependentes", NotificationCategory.Support => "Manifestações", NotificationCategory.Assembly => "Assembleias",
        NotificationCategory.Event => "Eventos", _ => c.ToString(),
    };

    public static string Of(PersonType t) => t switch { PersonType.Member => "Associado", PersonType.Dependent => "Dependente", PersonType.Guest => "Convidado", _ => t.ToString() };
    public static string Of(AccessOperation o) => o == AccessOperation.Entry ? "Entrada" : "Saída";

    public static string DependentPermission(string code) => code switch
    {
        "reservations.request" => "Solicitar reservas", "events.register" => "Inscrever-se em eventos", "guests.register" => "Cadastrar convidados", _ => code,
    };
}
