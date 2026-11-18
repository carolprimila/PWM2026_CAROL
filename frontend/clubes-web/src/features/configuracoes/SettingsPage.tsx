import { Banknote, CalendarClock, Clock, DoorOpen, FileLock2, Plus, QrCode, ShieldAlert, Trash2, UserPlus, Users, Wallet } from 'lucide-react'
import { useEffect, useState, type ComponentType, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, Card, CardTitle, cn, IconButton, Input, MoneyInput, PageHeader, QueryBoundary, Select, Switch, Textarea, TriState, useConfirm, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { NumberInput, Row } from './fields'
import { ManifestationTypes } from './ManifestationTypes'

type Settings = Schema<'SettingsDto'>
type Rule = { fromCount: number | null; toCount: number | null; amount: number | null }
type LooseKeys = 'pending' | 'dependentPricingRules' | 'dueReminderDays' | 'maxDependentsPerMember' | 'maxGuestsPerMember' | 'qrTokenTtlSeconds' | 'delinquentReservationPolicy' | 'reservationApprovalMode'
/** Estado de edição: os campos obrigatórios da API podem ficar vazios enquanto o usuário digita (barrados na validação). */
type TextKeys = 'familyCompositionChangeRule' | 'holidayRules' | 'minorsRules' | 'minorsAccompanimentRules' | 'poolPolicy' | 'eventProcedures' | 'spaceUsagePolicy' | 'privacyPolicy'
type Draft = Required<Omit<Settings, LooseKeys | TextKeys>> & Record<TextKeys, string> & {
  dependentPricingRules: Rule[]
  dueReminderDays: number | null; maxDependentsPerMember: number | null; maxGuestsPerMember: number | null; qrTokenTtlSeconds: number | null
  delinquentReservationPolicy: Schema<'DelinquentReservationPolicy'> | null
  reservationApprovalMode: Schema<'ApprovalMode'> | null
}

// Nulo = política ainda não definida pela administração. Nenhum valor é presumido na interface.
const toDraft = (s: Settings): Draft => ({
  monthlyBaseFee: s.monthlyBaseFee ?? null, dueDay: s.dueDay ?? null, graceDays: s.graceDays ?? null, lateFinePercent: s.lateFinePercent ?? null,
  dailyInterestPercent: s.dailyInterestPercent ?? null, dependentPricingMode: s.dependentPricingMode, dependentFixedAmount: s.dependentFixedAmount ?? null,
  dependentPricingRules: s.dependentPricingRules.map((r) => ({ ...r })), blockAccessWhenDelinquent: s.blockAccessWhenDelinquent ?? null,
  blockReservationsWhenDelinquent: s.blockReservationsWhenDelinquent ?? null, delinquentReservationPolicy: s.delinquentReservationPolicy ?? null,
  allowAdvancePayment: s.allowAdvancePayment ?? null, familyCompositionChangeRule: s.familyCompositionChangeRule ?? '',
  autoGenerateMonthlyCharges: s.autoGenerateMonthlyCharges, dueReminderDays: s.dueReminderDays, maxDependentsPerMember: s.maxDependentsPerMember,
  maxGuestsPerMember: s.maxGuestsPerMember, memberCanToggleDependentPermissions: s.memberCanToggleDependentPermissions, maxAdvanceDays: s.maxAdvanceDays ?? null,
  maxConcurrentReservations: s.maxConcurrentReservations ?? null, freeCancellationHours: s.freeCancellationHours ?? null, lateCancellationFee: s.lateCancellationFee ?? null,
  noShowFee: s.noShowFee ?? null, requireAdvancePayment: s.requireAdvancePayment ?? null, reservationApprovalMode: s.reservationApprovalMode ?? null,
  guestFee: s.guestFee ?? null, guestAdvanceRegistrationRequired: s.guestAdvanceRegistrationRequired ?? null, guestDocumentRequired: s.guestDocumentRequired ?? null,
  guestEntryWithoutMemberAllowed: s.guestEntryWithoutMemberAllowed ?? null, qrTokenTtlSeconds: s.qrTokenTtlSeconds, holidayRules: s.holidayRules ?? '',
  minorsRules: s.minorsRules ?? '', minorsAccompanimentRules: s.minorsAccompanimentRules ?? '', poolPolicy: s.poolPolicy ?? '', eventProcedures: s.eventProcedures ?? '',
  spaceUsagePolicy: s.spaceUsagePolicy ?? '', privacyPolicy: s.privacyPolicy ?? '', privacyPolicyVersion: s.privacyPolicyVersion,
})

const usesRules = (mode: Draft['dependentPricingMode']) => mode === 'Progressive' || mode === 'Tiers'

/** Corpo de PUT /api/configuracoes: textos vazios voltam a null; regras só são exigidas completas no modo que as utiliza. */
const toBody = (d: Draft) => ({
  ...d,
  familyCompositionChangeRule: emptyToNull(d.familyCompositionChangeRule), holidayRules: emptyToNull(d.holidayRules), minorsRules: emptyToNull(d.minorsRules),
  minorsAccompanimentRules: emptyToNull(d.minorsAccompanimentRules), poolPolicy: emptyToNull(d.poolPolicy), eventProcedures: emptyToNull(d.eventProcedures),
  spaceUsagePolicy: emptyToNull(d.spaceUsagePolicy), privacyPolicy: emptyToNull(d.privacyPolicy), privacyPolicyVersion: d.privacyPolicyVersion.trim(),
  dependentPricingRules: usesRules(d.dependentPricingMode) ? d.dependentPricingRules : d.dependentPricingRules.filter((r) => r.fromCount != null && r.toCount != null && r.amount != null),
  pending: [],
})

const required = 'Informe um valor'
const int = (min: number, max: number) => z.number({ error: required }).int('Use um número inteiro').min(min, `O mínimo é ${min}`).max(max, `O máximo é ${max}`)
const money = z.number({ error: 'Valor inválido' }).min(0, 'O valor não pode ser negativo').max(1_000_000, 'O máximo é 1.000.000,00')
const percent = z.number({ error: 'Valor inválido' }).min(0, 'O mínimo é 0').max(100, 'O máximo é 100')
const text = (max: number) => z.string().max(max, `Máximo de ${max.toLocaleString('pt-BR')} caracteres`)
// Mesmos limites validados pelo servidor (SettingsDto).
const schema = z.object({
  monthlyBaseFee: money.nullable(), dueDay: int(1, 28).nullable(), graceDays: int(0, 365).nullable(), lateFinePercent: percent.nullable(), dailyInterestPercent: percent.nullable(),
  dependentFixedAmount: money.nullable(), dueReminderDays: int(0, 30), maxDependentsPerMember: int(0, 50), maxGuestsPerMember: int(0, 50),
  maxAdvanceDays: int(0, 3650).nullable(), maxConcurrentReservations: int(1, 1000).nullable(), freeCancellationHours: int(0, 8760).nullable(),
  lateCancellationFee: money.nullable(), noShowFee: money.nullable(), guestFee: money.nullable(), qrTokenTtlSeconds: int(15, 600),
  familyCompositionChangeRule: text(2000), holidayRules: text(4000), minorsRules: text(4000), minorsAccompanimentRules: text(4000), poolPolicy: text(4000),
  eventProcedures: text(4000), spaceUsagePolicy: text(4000), privacyPolicy: text(20000),
  privacyPolicyVersion: z.string().trim().min(1, 'Informe a versão').max(20, 'Máximo de 20 caracteres'),
})

type Errors = Record<string, string>

function validate(d: Draft): Errors {
  const errors: Errors = {}
  const result = schema.safeParse(d)
  if (!result.success) for (const issue of result.error.issues) errors[issue.path.join('.')] ??= issue.message
  // Regras de cobrança por dependente (as mesmas do servidor, que valida novamente).
  const rules = d.dependentPricingRules
  if (d.dependentPricingMode === 'FixedPerDependent' && d.dependentFixedAmount == null) errors.dependentFixedAmount ??= 'Informe o valor fixo por dependente'
  if (usesRules(d.dependentPricingMode)) {
    if (rules.length === 0) errors.dependentPricingRules = d.dependentPricingMode === 'Progressive' ? 'Informe o valor de cada dependente (1º, 2º, 3º…)' : 'Informe ao menos uma faixa de dependentes'
    rules.forEach((rule, index) => {
      const invalid = (n: number | null, min: number, max: number) => n == null || !Number.isInteger(n) || n < min || n > max
      if (rule.amount == null || Number.isNaN(rule.amount) || rule.amount < 0) errors[`dependentPricingRules.${index}`] = 'Informe o valor'
      else if (invalid(rule.fromCount, 1, 50) || invalid(rule.toCount, 1, 50)) errors[`dependentPricingRules.${index}`] = 'Informe quantidades entre 1 e 50'
      else if (rule.toCount! < rule.fromCount!) errors[`dependentPricingRules.${index}`] = 'A quantidade final deve ser maior ou igual à inicial'
      else if (index > 0 && rules[index - 1].toCount != null && rule.fromCount! <= rules[index - 1].toCount!) errors[`dependentPricingRules.${index}`] = 'As faixas não podem se sobrepor'
    })
    const covered = Math.max(0, ...rules.map((r) => r.toCount ?? 0))
    if (rules.length > 0 && d.maxDependentsPerMember != null && covered < d.maxDependentsPerMember)
      errors.dependentPricingRules ??= `As regras precisam cobrir até ${d.maxDependentsPerMember} dependentes (limite por associado)`
  }
  return errors
}

type SectionKey = 'financeiro' | 'cadastros' | 'reservas' | 'convidados' | 'acesso' | 'funcionamento' | 'lgpd' | 'manifestacoes'
const sections: { key: SectionKey; label: string; icon: ComponentType<{ className?: string }>; pendingPrefix?: string; fields: string[] }[] = [
  { key: 'financeiro', label: 'Financeiro', icon: Wallet, pendingPrefix: 'Financeiro', fields: ['monthlyBaseFee', 'dueDay', 'graceDays', 'lateFinePercent', 'dailyInterestPercent', 'dueReminderDays', 'dependentFixedAmount', 'dependentPricingRules', 'familyCompositionChangeRule'] },
  { key: 'cadastros', label: 'Cadastros', icon: Users, fields: ['maxDependentsPerMember', 'maxGuestsPerMember'] },
  { key: 'reservas', label: 'Reservas', icon: CalendarClock, pendingPrefix: 'Reservas', fields: ['maxAdvanceDays', 'maxConcurrentReservations', 'freeCancellationHours', 'lateCancellationFee', 'noShowFee'] },
  { key: 'convidados', label: 'Convidados', icon: UserPlus, pendingPrefix: 'Convidados', fields: ['guestFee'] },
  { key: 'acesso', label: 'Acesso', icon: QrCode, fields: ['qrTokenTtlSeconds'] },
  { key: 'funcionamento', label: 'Funcionamento', icon: Clock, pendingPrefix: 'Funcionamento', fields: ['holidayRules', 'minorsRules', 'minorsAccompanimentRules', 'poolPolicy', 'eventProcedures', 'spaceUsagePolicy'] },
  { key: 'lgpd', label: 'LGPD', icon: FileLock2, pendingPrefix: 'LGPD', fields: ['privacyPolicy', 'privacyPolicyVersion'] },
  { key: 'manifestacoes', label: 'Tipos de manifestação', icon: ShieldAlert, fields: [] },
]
const sectionHasError = (section: (typeof sections)[number], errors: Errors) => Object.keys(errors).some((key) => section.fields.includes(key.split('.')[0]))

type Ctx = { d: Draft; set: <K extends keyof Draft>(key: K, value: Draft[K]) => void; errors: Errors }

export default function SettingsPage() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const manage = can('settings.manage')
  const [section, setSection] = useState<SectionKey>('financeiro')
  const [draft, setDraft] = useState<Draft | null>(null)
  const [errors, setErrors] = useState<Errors>({})

  const settings = useQuery({ queryKey: ['/api/configuracoes'], queryFn: () => api.get<Settings>('/api/configuracoes') })
  useEffect(() => { if (settings.data) { setDraft(toDraft(settings.data)); setErrors({}) } }, [settings.data])
  const dirty = !!draft && !!settings.data && JSON.stringify(draft) !== JSON.stringify(toDraft(settings.data))
  const set: Ctx['set'] = (key, value) => {
    setDraft((current) => current && { ...current, [key]: value })
    setErrors((current) => Object.fromEntries(Object.entries(current).filter(([k]) => k.split('.')[0] !== key)))
  }

  const save = useMutation({
    mutationFn: ({ body, justification }: { body: ReturnType<typeof toBody>; justification: string | null }) => api.put<Settings>('/api/configuracoes', { settings: body, justification }),
    onSuccess: (saved) => {
      toast.success('Políticas do clube atualizadas.')
      queryClient.setQueryData(['/api/configuracoes'], saved)
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
    onError: (error) => {
      // Validação do servidor: chaves como "Settings.DueDay" voltam para o campo correspondente.
      if (error instanceof ApiError && error.errors) {
        const mapped: Errors = {}
        for (const [field, messages] of Object.entries(error.errors)) {
          const parts = field.replace(/^(\$\.)?settings\./i, '').replace(/\[(\d+)\]/g, '.$1').split('.').map((part) => part.charAt(0).toLowerCase() + part.slice(1))
          mapped[(parts[0] === 'dependentPricingRules' ? parts.slice(0, 2) : parts).join('.')] ??= messages[0]
        }
        setErrors(mapped)
        const first = sections.find((s) => sectionHasError(s, mapped))
        if (first) setSection(first.key)
      }
      toast.error(error)
    },
  })

  const submit = async () => {
    if (!draft) return
    const problems = validate(draft)
    setErrors(problems)
    if (Object.keys(problems).length > 0) {
      const first = sections.find((s) => sectionHasError(s, problems))
      if (first) setSection(first.key)
      toast.error('Revise os campos destacados antes de salvar.')
      return
    }
    const result = await confirm({
      title: 'Salvar alterações nas políticas?', confirmLabel: 'Salvar alterações', reason: 'optional', reasonLabel: 'Justificativa (opcional)',
      message: 'As novas regras valem daqui em diante: cobranças já geradas e reservas já registradas não são alteradas retroativamente. A alteração fica registrada na auditoria, com os valores anteriores e novos.',
    })
    if (result) save.mutate({ body: toBody(draft), justification: result.reason || null })
  }

  return (
    <>
      <PageHeader title="Configurações" subtitle="Políticas do clube: regras financeiras, de cadastro, reservas, convidados, acesso e privacidade." />
      <QueryBoundary query={settings}>
        {(server) => !draft ? null : (
          <div className="space-y-6 pb-4">
            {!manage && <Alert tone="info">Você pode consultar as políticas, mas não tem permissão para alterá-las.</Alert>}
            {server.pending.length > 0 && (
              <Alert tone="warning" title={`Políticas pendentes de definição (${server.pending.length})`}>
                <p>Estas regras ainda não foram decididas pela administração. Enquanto não forem definidas, o sistema não aplica nenhum valor presumido.</p>
                <ul className="mt-2 grid list-disc gap-x-8 gap-y-0.5 pl-5 md:grid-cols-2">{server.pending.map((item) => <li key={item}>{item}</li>)}</ul>
              </Alert>
            )}

            <div className="grid items-start gap-6 lg:grid-cols-[220px_minmax(0,1fr)]">
              <nav className="sticky top-4 space-y-0.5" aria-label="Seções das configurações">
                {sections.map((item) => {
                  const pending = item.pendingPrefix ? server.pending.filter((p) => p.startsWith(`${item.pendingPrefix}:`)).length : 0
                  return (
                    <button key={item.key} type="button" onClick={() => setSection(item.key)} aria-current={section === item.key}
                      className={cn('flex w-full items-center gap-2.5 rounded-(--radius-control) px-3 py-2 text-left text-sm font-medium transition-colors', section === item.key ? 'bg-brand-50 text-brand-700' : 'text-muted hover:bg-black/5 hover:text-ink')}>
                      <item.icon className="size-4 shrink-0" />
                      <span className="flex-1 truncate">{item.label}</span>
                      {sectionHasError(item, errors) ? <span className="size-2 rounded-full bg-danger" title="Há campos a revisar" />
                        : pending > 0 && <span className="tabular rounded-full bg-warning-soft px-1.5 text-xs font-semibold text-warning" title={`${pending} política(s) pendente(s)`}>{pending}</span>}
                    </button>
                  )
                })}
              </nav>

              <div className="min-w-0">
                {section === 'manifestacoes' ? <ManifestationTypes /> : (
                  <fieldset disabled={!manage || save.isPending} className="min-w-0 space-y-6">
                    {section === 'financeiro' && <Finance d={draft} set={set} errors={errors} />}
                    {section === 'cadastros' && <Registrations d={draft} set={set} errors={errors} />}
                    {section === 'reservas' && <Reservations d={draft} set={set} errors={errors} />}
                    {section === 'convidados' && <Guests d={draft} set={set} errors={errors} />}
                    {section === 'acesso' && <Access d={draft} set={set} errors={errors} />}
                    {section === 'funcionamento' && <Operation d={draft} set={set} errors={errors} />}
                    {section === 'lgpd' && <Privacy d={draft} set={set} errors={errors} changed={draft.privacyPolicyVersion.trim() !== server.privacyPolicyVersion} />}
                  </fieldset>
                )}
              </div>
            </div>

            {manage && dirty && (
              <div className="animate-pop sticky bottom-4 z-20 flex flex-wrap items-center justify-between gap-3 rounded-(--radius-card) border border-line bg-surface px-5 py-3 shadow-(--shadow-pop)">
                <div>
                  <p className="text-sm font-medium text-ink">Há alterações não salvas</p>
                  <p className="text-xs text-muted">As mudanças não alteram retroativamente cobranças e reservas já existentes.</p>
                </div>
                <div className="flex items-center gap-2">
                  <Button disabled={save.isPending} onClick={() => { setDraft(toDraft(server)); setErrors({}) }}>Descartar</Button>
                  <Button variant="primary" loading={save.isPending} onClick={submit}>Salvar alterações</Button>
                </div>
              </div>
            )}
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function Group({ icon, title, description, children }: { icon: ComponentType<{ className?: string }>; title: string; description?: string; children: ReactNode }) {
  return (
    <Card>
      <CardTitle icon={icon} title={title} description={description} />
      <div className="divide-y divide-line/70 [&>*:first-child]:pt-0 [&>*:last-child]:pb-0">{children}</div>
    </Card>
  )
}

const yesNo = (ctx: Ctx, key: 'blockAccessWhenDelinquent' | 'blockReservationsWhenDelinquent' | 'allowAdvancePayment' | 'requireAdvancePayment' | 'guestAdvanceRegistrationRequired' | 'guestDocumentRequired' | 'guestEntryWithoutMemberAllowed', yes?: string, no?: string) =>
  <TriState value={ctx.d[key]} onChange={(value) => ctx.set(key, value)} yes={yes} no={no} />

function Finance(ctx: Ctx) {
  const { d, set, errors } = ctx
  return (
    <>
      <Group icon={Banknote} title="Mensalidade" description="Base para a geração das mensalidades e para os encargos por atraso.">
        <Row label="Mensalidade base" hint="Valor mensal cobrado de cada associado ativo, antes dos acréscimos por dependente." error={errors.monthlyBaseFee} undefinedValue={d.monthlyBaseFee == null}>
          <div className="w-40"><MoneyInput value={d.monthlyBaseFee} onChange={(v) => set('monthlyBaseFee', v)} placeholder="Não definido" aria-invalid={!!errors.monthlyBaseFee} /></div>
        </Row>
        <Row label="Dia de vencimento" hint="Dia do mês em que as mensalidades vencem (1 a 28)." error={errors.dueDay} undefinedValue={d.dueDay == null}>
          <NumberInput label="Dia de vencimento" value={d.dueDay} onChange={(v) => set('dueDay', v)} suffix="do mês" invalid={!!errors.dueDay} />
        </Row>
        <Row label="Tolerância" hint="Dias após o vencimento antes de o associado ser considerado inadimplente." error={errors.graceDays} undefinedValue={d.graceDays == null}>
          <NumberInput label="Tolerância" value={d.graceDays} onChange={(v) => set('graceDays', v)} suffix="dias" invalid={!!errors.graceDays} />
        </Row>
        <Row label="Multa por atraso" hint="Percentual aplicado uma vez sobre a cobrança vencida." error={errors.lateFinePercent} undefinedValue={d.lateFinePercent == null}>
          <NumberInput label="Multa por atraso" decimal value={d.lateFinePercent} onChange={(v) => set('lateFinePercent', v)} suffix="%" invalid={!!errors.lateFinePercent} />
        </Row>
        <Row label="Juros por atraso" hint="Percentual aplicado por dia de atraso." error={errors.dailyInterestPercent} undefinedValue={d.dailyInterestPercent == null}>
          <NumberInput label="Juros por atraso" decimal value={d.dailyInterestPercent} onChange={(v) => set('dailyInterestPercent', v)} suffix="% ao dia" invalid={!!errors.dailyInterestPercent} />
        </Row>
        <Row label="Geração automática de mensalidades" hint="Quando ligada, o sistema gera as mensalidades de cada competência sem ação manual.">
          <Switch checked={d.autoGenerateMonthlyCharges} onChange={(v) => set('autoGenerateMonthlyCharges', v)} />
        </Row>
        <Row label="Aviso de vencimento" hint="Antecedência com que o associado é notificado do vencimento. Use 0 para não enviar o aviso." error={errors.dueReminderDays}>
          <NumberInput label="Aviso de vencimento" value={d.dueReminderDays} onChange={(v) => set('dueReminderDays', v)} suffix="dias antes" placeholder="" invalid={!!errors.dueReminderDays} />
        </Row>
      </Group>

      <Group icon={Users} title="Cobrança por dependente" description="Como os dependentes ativos entram no valor da mensalidade do associado.">
        <Row label="Política de cobrança" undefinedValue={d.dependentPricingMode === 'NotConfigured'}>
          <div className="w-72"><Select value={d.dependentPricingMode} onChange={(e) => set('dependentPricingMode', e.target.value as Draft['dependentPricingMode'])}>{options(labels.pricingMode).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
        </Row>
        {d.dependentPricingMode === 'FixedPerDependent' && (
          <Row label="Valor por dependente" hint="Somado à mensalidade para cada dependente." error={errors.dependentFixedAmount}>
            <div className="w-40"><MoneyInput value={d.dependentFixedAmount} onChange={(v) => set('dependentFixedAmount', v)} aria-invalid={!!errors.dependentFixedAmount} /></div>
          </Row>
        )}
        {d.dependentPricingMode === 'Progressive' && <ProgressiveRules {...ctx} />}
        {d.dependentPricingMode === 'Tiers' && <TierRules {...ctx} />}
      </Group>

      <Group icon={ShieldAlert} title="Inadimplência e pagamentos" description="O que acontece quando há cobranças vencidas além da tolerância.">
        <Row label="Bloquear o acesso ao clube" hint="Impede a entrada de associados inadimplentes (e de seus dependentes) na portaria." undefinedValue={d.blockAccessWhenDelinquent == null}>{yesNo(ctx, 'blockAccessWhenDelinquent', 'Bloquear', 'Não bloquear')}</Row>
        <Row label="Bloquear novas reservas" hint="Impede que associados inadimplentes solicitem reservas." undefinedValue={d.blockReservationsWhenDelinquent == null}>{yesNo(ctx, 'blockReservationsWhenDelinquent', 'Bloquear', 'Não bloquear')}</Row>
        <Row label="Reservas já aprovadas de inadimplentes" hint="Tratamento das reservas futuras quando o associado se torna inadimplente." undefinedValue={d.delinquentReservationPolicy == null}>
          <div className="w-56"><Select value={d.delinquentReservationPolicy ?? ''} onChange={(e) => set('delinquentReservationPolicy', (e.target.value || null) as Draft['delinquentReservationPolicy'])}><option value="">Não definido</option>{options(labels.delinquentPolicy).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
        </Row>
        <Row label="Pagamento antecipado" hint="Permite pagar cobranças com vencimento distante (mais de um mês à frente)." undefinedValue={d.allowAdvancePayment == null}>{yesNo(ctx, 'allowAdvancePayment', 'Permitir', 'Não permitir')}</Row>
        <Row stacked label="Critérios para mudança de composição familiar" hint="Como entradas e saídas de dependentes afetam a cobrança (texto de referência para a equipe)." error={errors.familyCompositionChangeRule} undefinedValue={!d.familyCompositionChangeRule.trim()}>
          <Textarea rows={3} value={d.familyCompositionChangeRule} onChange={(e) => set('familyCompositionChangeRule', e.target.value)} />
        </Row>
      </Group>
    </>
  )
}

/** Progressiva: um valor por posição (1º, 2º, 3º… dependente), em sequência e sem lacunas — regras com fromCount = toCount = N. */
function ProgressiveRules({ d, set, errors }: Ctx) {
  const rules = d.dependentPricingRules
  const update = (next: Rule[]) => set('dependentPricingRules', next.map((rule, index) => ({ fromCount: index + 1, toCount: index + 1, amount: rule.amount })))
  const sequential = rules.every((rule, index) => rule.fromCount === index + 1 && rule.toCount === index + 1)
  return (
    <Row stacked label="Valor de cada dependente" hint={`Informe o valor do 1º, do 2º, do 3º dependente e assim por diante, até o limite de dependentes por associado${d.maxDependentsPerMember != null ? ` (${d.maxDependentsPerMember})` : ''}.`} error={errors.dependentPricingRules}>
      {!sequential && rules.length > 0 && (
        <Alert tone="warning" className="mb-3">As regras atuais estão em faixas e não em sequência. <button type="button" className="font-medium underline" onClick={() => update(rules)}>Converter para valores por posição</button></Alert>
      )}
      <ul className="max-w-md space-y-2">
        {rules.map((rule, index) => (
          <li key={index}>
            <div className="flex items-center gap-3">
              <span className="w-32 shrink-0 text-sm text-ink">{sequential ? `${index + 1}º dependente` : `${rule.fromCount ?? '?'} a ${rule.toCount ?? '?'}`}</span>
              <div className="w-40"><MoneyInput value={rule.amount} onChange={(amount) => set('dependentPricingRules', rules.map((r, i) => (i === index ? { ...r, amount } : r)))} aria-invalid={!!errors[`dependentPricingRules.${index}`]} aria-label={`Valor do ${index + 1}º dependente`} /></div>
              {index === rules.length - 1 && <IconButton icon={Trash2} label="Remover" onClick={() => update(rules.slice(0, -1))} />}
            </div>
            {errors[`dependentPricingRules.${index}`] && <p className="mt-1 text-xs text-danger">{errors[`dependentPricingRules.${index}`]}</p>}
          </li>
        ))}
      </ul>
      <Button size="sm" icon={Plus} className="mt-3" disabled={rules.length >= 50} onClick={() => update([...rules, { fromCount: null, toCount: null, amount: null }])}>Adicionar {rules.length + 1}º dependente</Button>
    </Row>
  )
}

/** Por faixas: valor por dependente conforme a quantidade (de/até), sem sobreposição. */
function TierRules({ d, set, errors }: Ctx) {
  const rules = d.dependentPricingRules
  const patch = (index: number, value: Partial<Rule>) => set('dependentPricingRules', rules.map((r, i) => (i === index ? { ...r, ...value } : r)))
  return (
    <Row stacked label="Faixas de quantidade" hint={`Valor cobrado conforme a quantidade de dependentes. As faixas não podem se sobrepor e precisam cobrir até o limite de dependentes por associado${d.maxDependentsPerMember != null ? ` (${d.maxDependentsPerMember})` : ''}.`} error={errors.dependentPricingRules}>
      {rules.length > 0 && (
        <table className="text-sm">
          <thead><tr className="text-left text-xs font-medium tracking-wide text-muted uppercase"><th className="pr-3 pb-2 font-medium">De (dependentes)</th><th className="pr-3 pb-2 font-medium">Até</th><th className="pr-3 pb-2 font-medium">Valor por dependente</th><th /></tr></thead>
          <tbody>
            {rules.map((rule, index) => (
              <tr key={index} className="align-top">
                <td className="pr-3 pb-2"><NumberInput className="w-28" label={`Faixa ${index + 1} — de`} placeholder="" value={rule.fromCount} onChange={(v) => patch(index, { fromCount: v })} invalid={!!errors[`dependentPricingRules.${index}`]} /></td>
                <td className="pr-3 pb-2"><NumberInput className="w-28" label={`Faixa ${index + 1} — até`} placeholder="" value={rule.toCount} onChange={(v) => patch(index, { toCount: v })} invalid={!!errors[`dependentPricingRules.${index}`]} /></td>
                <td className="pr-3 pb-2">
                  <div className="w-40"><MoneyInput value={rule.amount} onChange={(amount) => patch(index, { amount })} aria-invalid={!!errors[`dependentPricingRules.${index}`]} aria-label={`Faixa ${index + 1} — valor`} /></div>
                  {errors[`dependentPricingRules.${index}`] && <p className="mt-1 text-xs text-danger">{errors[`dependentPricingRules.${index}`]}</p>}
                </td>
                <td className="pb-2"><IconButton icon={Trash2} label="Remover faixa" className="mt-1" onClick={() => set('dependentPricingRules', rules.filter((_, i) => i !== index))} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <Button size="sm" icon={Plus} className="mt-1" disabled={rules.length >= 50} onClick={() => set('dependentPricingRules', [...rules, { fromCount: null, toCount: null, amount: null }])}>Adicionar faixa</Button>
    </Row>
  )
}

function Registrations({ d, set, errors }: Ctx) {
  return (
    <Group icon={Users} title="Cadastros" description="Limites e autonomia do associado sobre seus dependentes e convidados.">
      <Row label="Máximo de dependentes por associado" hint="O servidor recusa novos dependentes acima deste limite." error={errors.maxDependentsPerMember}>
        <NumberInput label="Máximo de dependentes por associado" value={d.maxDependentsPerMember} onChange={(v) => set('maxDependentsPerMember', v)} suffix="dependentes" placeholder="" className="w-44" invalid={!!errors.maxDependentsPerMember} />
      </Row>
      <Row label="Máximo de convidados por associado" hint="Limite de convidados por associado em um mesmo dia e por reserva." error={errors.maxGuestsPerMember}>
        <NumberInput label="Máximo de convidados por associado" value={d.maxGuestsPerMember} onChange={(v) => set('maxGuestsPerMember', v)} suffix="convidados" placeholder="" className="w-44" invalid={!!errors.maxGuestsPerMember} />
      </Row>
      <Row label="Associado pode habilitar permissões dos dependentes" hint="Ligado: a administração libera e o associado habilita cada permissão. Desligado: a liberação da administração já torna a permissão efetiva.">
        <Switch checked={d.memberCanToggleDependentPermissions} onChange={(v) => set('memberCanToggleDependentPermissions', v)} />
      </Row>
    </Group>
  )
}

function Reservations(ctx: Ctx) {
  const { d, set, errors } = ctx
  return (
    <Group icon={CalendarClock} title="Reservas" description="Regras gerais para as reservas de espaços. Cada espaço pode ter regras próprias no seu cadastro.">
      <Row label="Antecedência máxima" hint="Com quantos dias de antecedência uma reserva pode ser solicitada." error={errors.maxAdvanceDays} undefinedValue={d.maxAdvanceDays == null}>
        <NumberInput label="Antecedência máxima" value={d.maxAdvanceDays} onChange={(v) => set('maxAdvanceDays', v)} suffix="dias" invalid={!!errors.maxAdvanceDays} />
      </Row>
      <Row label="Limite de reservas simultâneas" hint="Quantas reservas em aberto cada associado pode manter ao mesmo tempo." error={errors.maxConcurrentReservations} undefinedValue={d.maxConcurrentReservations == null}>
        <NumberInput label="Limite de reservas simultâneas" value={d.maxConcurrentReservations} onChange={(v) => set('maxConcurrentReservations', v)} suffix="reservas" invalid={!!errors.maxConcurrentReservations} />
      </Row>
      <Row label="Cancelamento sem multa" hint="Antecedência mínima, em horas, para cancelar sem multa." error={errors.freeCancellationHours} undefinedValue={d.freeCancellationHours == null}>
        <NumberInput label="Cancelamento sem multa" value={d.freeCancellationHours} onChange={(v) => set('freeCancellationHours', v)} suffix="horas" invalid={!!errors.freeCancellationHours} />
      </Row>
      <Row label="Multa por cancelamento tardio" hint="Lançada quando o cancelamento ocorre fora do prazo acima." error={errors.lateCancellationFee} undefinedValue={d.lateCancellationFee == null}>
        <div className="w-40"><MoneyInput value={d.lateCancellationFee} onChange={(v) => set('lateCancellationFee', v)} placeholder="Não definido" aria-invalid={!!errors.lateCancellationFee} /></div>
      </Row>
      <Row label="Penalidade por não comparecimento" hint="Lançada quando a equipe registra a falta em uma reserva aprovada. Use 0,00 para não cobrar." error={errors.noShowFee} undefinedValue={d.noShowFee == null}>
        <div className="w-40"><MoneyInput value={d.noShowFee} onChange={(v) => set('noShowFee', v)} placeholder="Não definido" aria-invalid={!!errors.noShowFee} /></div>
      </Row>
      <Row label="Pagamento das reservas" hint="Padrão para espaços sem forma de cobrança própria: pagamento antecipado ou cobrança na mensalidade seguinte." undefinedValue={d.requireAdvancePayment == null}>{yesNo(ctx, 'requireAdvancePayment', 'Antecipado', 'Na mensalidade')}</Row>
      <Row label="Modo de aprovação" hint="Se as solicitações são aprovadas automaticamente ou passam pela equipe." undefinedValue={d.reservationApprovalMode == null}>
        <div className="w-56"><Select value={d.reservationApprovalMode ?? ''} onChange={(e) => set('reservationApprovalMode', (e.target.value || null) as Draft['reservationApprovalMode'])}><option value="">Não definido</option>{options(labels.approvalMode).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
      </Row>
    </Group>
  )
}

function Guests(ctx: Ctx) {
  const { d, set, errors } = ctx
  return (
    <Group icon={UserPlus} title="Convidados" description="Regras para a entrada de convidados dos associados.">
      <Row label="Taxa por convidado" hint="Valor cobrado do associado por convidado. Use 0,00 para não cobrar." error={errors.guestFee} undefinedValue={d.guestFee == null}>
        <div className="w-40"><MoneyInput value={d.guestFee} onChange={(v) => set('guestFee', v)} placeholder="Não definido" aria-invalid={!!errors.guestFee} /></div>
      </Row>
      <Row label="Cadastro antecipado obrigatório" hint="Exige que o associado cadastre o convidado antes do dia da visita." undefinedValue={d.guestAdvanceRegistrationRequired == null}>{yesNo(ctx, 'guestAdvanceRegistrationRequired', 'Obrigatório', 'Opcional')}</Row>
      <Row label="Documento obrigatório" hint="Exige o documento do convidado no cadastro." undefinedValue={d.guestDocumentRequired == null}>{yesNo(ctx, 'guestDocumentRequired', 'Obrigatório', 'Opcional')}</Row>
      <Row label="Entrada sem o associado" hint="Permite que o convidado entre quando o associado responsável não está no clube." undefinedValue={d.guestEntryWithoutMemberAllowed == null}>{yesNo(ctx, 'guestEntryWithoutMemberAllowed', 'Permitir', 'Não permitir')}</Row>
    </Group>
  )
}

function Access({ d, set, errors }: Ctx) {
  return (
    <Group icon={DoorOpen} title="Acesso" description="Controle de entrada pela portaria com o cartão digital.">
      <Row label="Validade do QR Code" hint="Tempo, em segundos, durante o qual o código gerado no aplicativo pode ser usado (15 a 600). Códigos são de uso único." error={errors.qrTokenTtlSeconds}>
        <NumberInput label="Validade do QR Code" value={d.qrTokenTtlSeconds} onChange={(v) => set('qrTokenTtlSeconds', v)} suffix="segundos" placeholder="" className="w-44" invalid={!!errors.qrTokenTtlSeconds} />
      </Row>
    </Group>
  )
}

const operationTexts: { key: 'holidayRules' | 'minorsRules' | 'minorsAccompanimentRules' | 'poolPolicy' | 'eventProcedures' | 'spaceUsagePolicy'; label: string; hint: string }[] = [
  { key: 'holidayRules', label: 'Regras para feriados', hint: 'Funcionamento do clube em feriados e datas especiais.' },
  { key: 'minorsRules', label: 'Regras para menores', hint: 'Condições de acesso e permanência de menores de idade.' },
  { key: 'minorsAccompanimentRules', label: 'Acompanhamento de menores', hint: 'Quando e por quem os menores devem estar acompanhados.' },
  { key: 'poolPolicy', label: 'Política de utilização das piscinas', hint: 'Horários, exame médico, trajes e demais regras das piscinas.' },
  { key: 'eventProcedures', label: 'Procedimentos para eventos', hint: 'Como solicitar, organizar e encerrar eventos no clube.' },
  { key: 'spaceUsagePolicy', label: 'Política de uso dos espaços', hint: 'Regras gerais de conservação, limpeza e devolução dos espaços.' },
]

function Operation({ d, set, errors }: Ctx) {
  return (
    <Group icon={Clock} title="Funcionamento" description="Textos das políticas de uso. São exibidos aos associados e dependentes no aplicativo.">
      {operationTexts.map((item) => (
        <Row key={item.key} stacked label={item.label} hint={item.hint} error={errors[item.key]} undefinedValue={!d[item.key].trim()}>
          <Textarea rows={5} value={d[item.key]} aria-invalid={!!errors[item.key]} onChange={(e) => set(item.key, e.target.value)} />
        </Row>
      ))}
    </Group>
  )
}

function Privacy({ d, set, errors, changed }: Ctx & { changed: boolean }) {
  return (
    <Group icon={FileLock2} title="LGPD — política de privacidade" description="Texto apresentado aos usuários para consentimento no primeiro acesso.">
      <Row stacked label="Política de privacidade" hint="Informe como o clube trata os dados pessoais de associados, dependentes e convidados." error={errors.privacyPolicy} undefinedValue={!d.privacyPolicy.trim()}>
        <Textarea rows={16} value={d.privacyPolicy} aria-invalid={!!errors.privacyPolicy} onChange={(e) => set('privacyPolicy', e.target.value)} />
      </Row>
      <Row label="Versão da política" hint="Identificador da versão vigente (ex.: 1, 2, 2026-01). Ao mudar a versão, todos os usuários precisam aceitar a política novamente." error={errors.privacyPolicyVersion}>
        <Input className="w-40" maxLength={20} value={d.privacyPolicyVersion} aria-invalid={!!errors.privacyPolicyVersion} onChange={(e) => set('privacyPolicyVersion', e.target.value)} />
      </Row>
      {changed && <div className="py-4"><Alert tone="warning" title="A versão foi alterada">Ao salvar, o aceite anterior deixa de valer: cada usuário verá a política e precisará aceitá-la de novo no próximo acesso. Altere a versão somente quando o texto mudar de forma relevante.</Alert></div>}
    </Group>
  )
}
