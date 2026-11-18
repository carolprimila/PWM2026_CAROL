using System.ComponentModel.DataAnnotations;
using Clubes.Application.Common;
using Clubes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Clubes.Application.Clubs;

public record PricingRuleDto([Range(1, 50)] int FromCount, [Range(1, 50)] int ToCount, [Range(0, 1_000_000)] decimal Amount);

public class SettingsDto
{
    // Financeiro
    [Range(0, 1_000_000)] public decimal? MonthlyBaseFee { get; set; }
    [Range(1, 28)] public int? DueDay { get; set; }
    [Range(0, 365)] public int? GraceDays { get; set; }
    [Range(0, 100)] public decimal? LateFinePercent { get; set; }
    [Range(0, 100)] public decimal? DailyInterestPercent { get; set; }
    public DependentPricingMode DependentPricingMode { get; set; }
    [Range(0, 1_000_000)] public decimal? DependentFixedAmount { get; set; }
    public List<PricingRuleDto> DependentPricingRules { get; set; } = [];
    public bool? BlockAccessWhenDelinquent { get; set; }
    public bool? BlockReservationsWhenDelinquent { get; set; }
    public DelinquentReservationPolicy? DelinquentReservationPolicy { get; set; }
    public bool? AllowAdvancePayment { get; set; }
    [StringLength(2000)] public string? FamilyCompositionChangeRule { get; set; }
    public bool AutoGenerateMonthlyCharges { get; set; }
    [Range(0, 30)] public int DueReminderDays { get; set; }

    // Cadastros
    [Range(0, 50)] public int MaxDependentsPerMember { get; set; }
    [Range(0, 50)] public int MaxGuestsPerMember { get; set; }
    public bool MemberCanToggleDependentPermissions { get; set; }

    // Reservas
    [Range(0, 3650)] public int? MaxAdvanceDays { get; set; }
    [Range(1, 1000)] public int? MaxConcurrentReservations { get; set; }
    [Range(0, 8760)] public int? FreeCancellationHours { get; set; }
    [Range(0, 1_000_000)] public decimal? LateCancellationFee { get; set; }
    [Range(0, 1_000_000)] public decimal? NoShowFee { get; set; }
    public bool? RequireAdvancePayment { get; set; }
    public ApprovalMode? ReservationApprovalMode { get; set; }

    // Convidados
    [Range(0, 1_000_000)] public decimal? GuestFee { get; set; }
    public bool? GuestAdvanceRegistrationRequired { get; set; }
    public bool? GuestDocumentRequired { get; set; }
    public bool? GuestEntryWithoutMemberAllowed { get; set; }

    // Acesso
    [Range(15, 600)] public int QrTokenTtlSeconds { get; set; }

    // Funcionamento
    [StringLength(4000)] public string? HolidayRules { get; set; }
    [StringLength(4000)] public string? MinorsRules { get; set; }
    [StringLength(4000)] public string? MinorsAccompanimentRules { get; set; }
    [StringLength(4000)] public string? PoolPolicy { get; set; }
    [StringLength(4000)] public string? EventProcedures { get; set; }
    [StringLength(4000)] public string? SpaceUsagePolicy { get; set; }
    [StringLength(20000)] public string? PrivacyPolicy { get; set; }
    [Required, StringLength(20)] public string PrivacyPolicyVersion { get; set; } = "1";

    /// <summary>Políticas ainda não definidas pela administração (somente leitura).</summary>
    public List<string> Pending { get; set; } = [];
}

public class SettingsService(IAppDb db, RequestContext ctx)
{
    /// <summary>Configurações do clube corrente (cria o registro em branco se ainda não existir).</summary>
    public async Task<ClubSettings> LoadAsync(CancellationToken ct = default)
    {
        var settings = await db.Set<ClubSettings>().FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new ClubSettings { ClubId = ctx.RequireClub() };
            db.Set<ClubSettings>().Add(settings);
            await db.SaveChangesAsync(ct);
        }
        return settings;
    }

    public async Task<SettingsDto> GetAsync(CancellationToken ct = default)
    {
        var s = await LoadAsync(ct);
        var rules = await db.Set<DependentPricingRule>().OrderBy(r => r.FromCount).ToListAsync(ct);
        return Map(s, rules);
    }

    public async Task<SettingsDto> UpdateAsync(SettingsDto dto, string? justification, CancellationToken ct = default)
    {
        Validate(dto);
        var s = await LoadAsync(ct);
        ctx.Justification = justification;

        s.MonthlyBaseFee = dto.MonthlyBaseFee;
        s.DueDay = dto.DueDay;
        s.GraceDays = dto.GraceDays;
        s.LateFinePercent = dto.LateFinePercent;
        s.DailyInterestPercent = dto.DailyInterestPercent;
        s.DependentPricingMode = dto.DependentPricingMode;
        s.DependentFixedAmount = dto.DependentFixedAmount;
        s.BlockAccessWhenDelinquent = dto.BlockAccessWhenDelinquent;
        s.BlockReservationsWhenDelinquent = dto.BlockReservationsWhenDelinquent;
        s.DelinquentReservationPolicy = dto.DelinquentReservationPolicy;
        s.AllowAdvancePayment = dto.AllowAdvancePayment;
        s.FamilyCompositionChangeRule = dto.FamilyCompositionChangeRule;
        s.AutoGenerateMonthlyCharges = dto.AutoGenerateMonthlyCharges;
        s.DueReminderDays = dto.DueReminderDays;
        s.MaxDependentsPerMember = dto.MaxDependentsPerMember;
        s.MaxGuestsPerMember = dto.MaxGuestsPerMember;
        s.MemberCanToggleDependentPermissions = dto.MemberCanToggleDependentPermissions;
        s.MaxAdvanceDays = dto.MaxAdvanceDays;
        s.MaxConcurrentReservations = dto.MaxConcurrentReservations;
        s.FreeCancellationHours = dto.FreeCancellationHours;
        s.LateCancellationFee = dto.LateCancellationFee;
        s.NoShowFee = dto.NoShowFee;
        s.RequireAdvancePayment = dto.RequireAdvancePayment;
        s.ReservationApprovalMode = dto.ReservationApprovalMode;
        s.GuestFee = dto.GuestFee;
        s.GuestAdvanceRegistrationRequired = dto.GuestAdvanceRegistrationRequired;
        s.GuestDocumentRequired = dto.GuestDocumentRequired;
        s.GuestEntryWithoutMemberAllowed = dto.GuestEntryWithoutMemberAllowed;
        s.QrTokenTtlSeconds = dto.QrTokenTtlSeconds;
        s.HolidayRules = dto.HolidayRules;
        s.MinorsRules = dto.MinorsRules;
        s.MinorsAccompanimentRules = dto.MinorsAccompanimentRules;
        s.PoolPolicy = dto.PoolPolicy;
        s.EventProcedures = dto.EventProcedures;
        s.SpaceUsagePolicy = dto.SpaceUsagePolicy;
        s.PrivacyPolicy = dto.PrivacyPolicy;
        s.PrivacyPolicyVersion = dto.PrivacyPolicyVersion;

        var existing = await db.Set<DependentPricingRule>().ToListAsync(ct);
        var same = existing.Count == dto.DependentPricingRules.Count && existing.OrderBy(r => r.FromCount)
            .Zip(dto.DependentPricingRules.OrderBy(r => r.FromCount)).All(p => p.First.FromCount == p.Second.FromCount && p.First.ToCount == p.Second.ToCount && p.First.Amount == p.Second.Amount);
        if (!same)
        {
            db.Set<DependentPricingRule>().RemoveRange(existing);
            db.Set<DependentPricingRule>().AddRange(dto.DependentPricingRules.Select(r => new DependentPricingRule { FromCount = r.FromCount, ToCount = r.ToCount, Amount = r.Amount }));
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }

    private static void Validate(SettingsDto dto)
    {
        static DomainException Invalid(string message) => new(message, "configuracao_invalida", 400);
        var rules = dto.DependentPricingRules.OrderBy(r => r.FromCount).ToList();
        switch (dto.DependentPricingMode)
        {
            case DependentPricingMode.FixedPerDependent when dto.DependentFixedAmount is null:
                throw Invalid("Informe o valor fixo por dependente.");
            case DependentPricingMode.Progressive:
                if (rules.Count == 0) throw Invalid("Informe o valor de cada dependente (1º, 2º, 3º...).");
                if (rules.Any(r => r.FromCount != r.ToCount) || rules.Select((r, i) => r.FromCount == i + 1).Any(ok => !ok))
                    throw Invalid("Na cobrança progressiva, informe os valores em sequência a partir do 1º dependente, sem lacunas.");
                break;
            case DependentPricingMode.Tiers:
                if (rules.Count == 0) throw Invalid("Informe ao menos uma faixa de dependentes.");
                if (rules.Any(r => r.ToCount < r.FromCount)) throw Invalid("Em cada faixa, a quantidade final deve ser maior ou igual à inicial.");
                for (var i = 1; i < rules.Count; i++)
                    if (rules[i].FromCount <= rules[i - 1].ToCount) throw Invalid("As faixas de dependentes não podem se sobrepor.");
                break;
        }
        if (dto.DependentPricingMode is DependentPricingMode.Progressive or DependentPricingMode.Tiers
            && rules.Max(r => r.ToCount) < dto.MaxDependentsPerMember)
            throw Invalid($"As regras de cobrança precisam cobrir até {dto.MaxDependentsPerMember} dependentes (limite por associado).");
    }

    private static SettingsDto Map(ClubSettings s, List<DependentPricingRule> rules)
    {
        var dto = new SettingsDto
        {
            MonthlyBaseFee = s.MonthlyBaseFee, DueDay = s.DueDay, GraceDays = s.GraceDays, LateFinePercent = s.LateFinePercent,
            DailyInterestPercent = s.DailyInterestPercent, DependentPricingMode = s.DependentPricingMode, DependentFixedAmount = s.DependentFixedAmount,
            DependentPricingRules = rules.Select(r => new PricingRuleDto(r.FromCount, r.ToCount, r.Amount)).ToList(),
            BlockAccessWhenDelinquent = s.BlockAccessWhenDelinquent, BlockReservationsWhenDelinquent = s.BlockReservationsWhenDelinquent,
            DelinquentReservationPolicy = s.DelinquentReservationPolicy, AllowAdvancePayment = s.AllowAdvancePayment,
            FamilyCompositionChangeRule = s.FamilyCompositionChangeRule, AutoGenerateMonthlyCharges = s.AutoGenerateMonthlyCharges,
            DueReminderDays = s.DueReminderDays, MaxDependentsPerMember = s.MaxDependentsPerMember,
            MaxGuestsPerMember = s.MaxGuestsPerMember, MemberCanToggleDependentPermissions = s.MemberCanToggleDependentPermissions,
            MaxAdvanceDays = s.MaxAdvanceDays, MaxConcurrentReservations = s.MaxConcurrentReservations, FreeCancellationHours = s.FreeCancellationHours,
            LateCancellationFee = s.LateCancellationFee, NoShowFee = s.NoShowFee, RequireAdvancePayment = s.RequireAdvancePayment,
            ReservationApprovalMode = s.ReservationApprovalMode, GuestFee = s.GuestFee,
            GuestAdvanceRegistrationRequired = s.GuestAdvanceRegistrationRequired, GuestDocumentRequired = s.GuestDocumentRequired,
            GuestEntryWithoutMemberAllowed = s.GuestEntryWithoutMemberAllowed, QrTokenTtlSeconds = s.QrTokenTtlSeconds,
            HolidayRules = s.HolidayRules, MinorsRules = s.MinorsRules, MinorsAccompanimentRules = s.MinorsAccompanimentRules,
            PoolPolicy = s.PoolPolicy, EventProcedures = s.EventProcedures, SpaceUsagePolicy = s.SpaceUsagePolicy,
            PrivacyPolicy = s.PrivacyPolicy, PrivacyPolicyVersion = s.PrivacyPolicyVersion,
        };

        void Pending(bool missing, string label) { if (missing) dto.Pending.Add(label); }
        Pending(s.MonthlyBaseFee is null, "Financeiro: valor da mensalidade base");
        Pending(s.DueDay is null, "Financeiro: dia de vencimento");
        Pending(s.GraceDays is null, "Financeiro: prazo de tolerância");
        Pending(s.DependentPricingMode == DependentPricingMode.NotConfigured, "Financeiro: política de cobrança por dependente");
        Pending(s.LateFinePercent is null || s.DailyInterestPercent is null, "Financeiro: multa e juros por atraso");
        Pending(s.BlockAccessWhenDelinquent is null || s.BlockReservationsWhenDelinquent is null, "Financeiro: bloqueios por inadimplência");
        Pending(s.DelinquentReservationPolicy is null, "Financeiro: tratamento de reservas aprovadas de inadimplentes");
        Pending(s.AllowAdvancePayment is null, "Financeiro: regras de pagamento antecipado");
        Pending(string.IsNullOrWhiteSpace(s.FamilyCompositionChangeRule), "Financeiro: critérios para mudança de composição familiar");
        Pending(s.MaxAdvanceDays is null, "Reservas: antecedência máxima");
        Pending(s.MaxConcurrentReservations is null, "Reservas: limite de reservas simultâneas");
        Pending(s.FreeCancellationHours is null, "Reservas: prazo de cancelamento sem multa");
        Pending(s.NoShowFee is null, "Reservas: política de não comparecimento");
        Pending(s.RequireAdvancePayment is null, "Reservas: pagamento antecipado ou cobrança posterior");
        Pending(s.ReservationApprovalMode is null, "Reservas: aprovação automática ou manual");
        Pending(s.GuestFee is null, "Convidados: taxa por pessoa");
        Pending(s.GuestAdvanceRegistrationRequired is null, "Convidados: cadastro antecipado obrigatório");
        Pending(s.GuestDocumentRequired is null, "Convidados: documentos exigidos");
        Pending(s.GuestEntryWithoutMemberAllowed is null, "Convidados: entrada sem o associado responsável");
        Pending(string.IsNullOrWhiteSpace(s.HolidayRules), "Funcionamento: regras para feriados");
        Pending(string.IsNullOrWhiteSpace(s.MinorsRules), "Funcionamento: regras para menores");
        Pending(string.IsNullOrWhiteSpace(s.MinorsAccompanimentRules), "Funcionamento: acompanhamento de menores");
        Pending(string.IsNullOrWhiteSpace(s.PoolPolicy), "Funcionamento: política de utilização das piscinas");
        Pending(string.IsNullOrWhiteSpace(s.EventProcedures), "Funcionamento: procedimentos para eventos");
        Pending(string.IsNullOrWhiteSpace(s.PrivacyPolicy), "LGPD: texto da política de privacidade");
        return dto;
    }
}
