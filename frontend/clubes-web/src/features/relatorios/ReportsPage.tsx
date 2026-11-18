import {
  AlertTriangle, Ban, CalendarCheck, CalendarClock, CalendarX, CircleDollarSign, DoorOpen, FileSpreadsheet, FileText, Gauge, Hammer, Percent, PiggyBank, Scale,
  ShieldAlert, TrendingDown, UserCheck, UserMinus, UserPlus, UserX, Users, Wallet, Wrench,
} from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Button, Card, CardTitle, cn, Input, PageHeader, QueryBoundary, StatCard, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { dates, fmt } from '@/lib/format'

/** Cores dos gráficos — mesmos valores dos tokens de `src/index.css`. */
const colors = { brand: '#1c7460', brandSoft: '#74b39f', info: '#2b6cb0', gold: '#c9a227', grid: '#e3e7e3', axis: '#6b7770', ink: '#14201c' }
const tooltipStyle = { borderRadius: 10, border: `1px solid ${colors.grid}`, fontSize: 13, color: colors.ink }
const compact = new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 })

type Period = { from: string; to: string }
type Named = Schema<'NamedCount'>
const presets: { label: string; range: () => Period }[] = [
  { label: 'Mês atual', range: () => ({ from: dates.firstOfMonth(), to: dates.lastOfMonth() }) },
  { label: 'Mês anterior', range: () => { const d = new Date(); const p = new Date(d.getFullYear(), d.getMonth() - 1, 1); return { from: dates.firstOfMonth(p), to: dates.lastOfMonth(p) } } },
  { label: 'Últimos 90 dias', range: () => ({ from: dates.addDays(dates.today(), -89), to: dates.today() }) },
  { label: 'Este ano', range: () => { const y = new Date().getFullYear(); return { from: `${y}-01-01`, to: `${y}-12-31` } } },
]

function Section({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <section>
      <div className="mb-3"><h2 className="text-base font-semibold text-ink">{title}</h2>{description && <p className="text-[13px] text-muted">{description}</p>}</div>
      <div className="space-y-4">{children}</div>
    </section>
  )
}

const Empty = ({ children }: { children: ReactNode }) => <p className="py-10 text-center text-sm text-muted">{children}</p>

/** Barras horizontais (ranking por nome). */
function RankChart({ data, color, format, empty }: { data: Named[]; color: string; format: (value: number) => string; empty: string }) {
  if (data.length === 0 || data.every((item) => item.value === 0)) return <Empty>{empty}</Empty>
  return (
    <div style={{ height: Math.max(120, data.length * 38 + 16) }}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} layout="vertical" margin={{ top: 0, right: 16, bottom: 0, left: 0 }}>
          <CartesianGrid horizontal={false} stroke={colors.grid} />
          <XAxis type="number" tickLine={false} axisLine={false} allowDecimals={false} tick={{ fill: colors.axis, fontSize: 12 }} tickFormatter={(value: number) => compact.format(value)} />
          <YAxis type="category" dataKey="name" width={150} tickLine={false} axisLine={false} tick={{ fill: colors.ink, fontSize: 12 }} />
          <Tooltip cursor={{ fill: 'rgb(20 32 28 / 0.04)' }} formatter={(value) => format(Number(value))} contentStyle={tooltipStyle} />
          <Bar dataKey="value" name="Total" fill={color} radius={[0, 4, 4, 0]} maxBarSize={18} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

export default function ReportsPage() {
  const toast = useToast()
  const [period, setPeriod] = useState<Period>(() => presets[0].range())
  const [exporting, setExporting] = useState<'pdf' | 'excel' | null>(null)
  const indicators = useQuery({
    queryKey: ['/api/relatorios/indicadores', period],
    queryFn: () => api.get<Schema<'IndicatorsDto'>>('/api/relatorios/indicadores', period),
    placeholderData: keepPreviousData,
  })
  const exportAs = async (format: 'pdf' | 'excel') => {
    setExporting(format)
    try { await api.download(`/api/relatorios/indicadores/${format}`, period, format === 'pdf' ? 'indicadores.pdf' : 'indicadores.xlsx') } catch (error) { toast.error(error) } finally { setExporting(null) }
  }

  return (
    <>
      <PageHeader title="Relatórios" subtitle="Indicadores de associados, financeiro, reservas e operação no período selecionado."
        actions={<>
          <Button icon={FileText} loading={exporting === 'pdf'} disabled={exporting !== null} onClick={() => exportAs('pdf')}>Exportar PDF</Button>
          <Button icon={FileSpreadsheet} loading={exporting === 'excel'} disabled={exporting !== null} onClick={() => exportAs('excel')}>Exportar Excel</Button>
        </>} />

      <div className="mb-6 flex flex-wrap items-center gap-2">
        <div className="inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5">
          {presets.map((preset) => {
            const range = preset.range()
            return (
              <button key={preset.label} type="button" onClick={() => setPeriod(range)}
                className={cn('rounded-lg px-3 py-1.5 text-[13px] font-medium transition-colors', range.from === period.from && range.to === period.to ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink')}>
                {preset.label}
              </button>
            )
          })}
        </div>
        <Input type="date" aria-label="De" className="w-40" value={period.from} max={period.to} onChange={(e) => e.target.value && setPeriod({ ...period, from: e.target.value })} />
        <span className="text-sm text-muted">até</span>
        <Input type="date" aria-label="Até" className="w-40" value={period.to} min={period.from} onChange={(e) => e.target.value && setPeriod({ ...period, to: e.target.value })} />
      </div>

      <QueryBoundary query={indicators}>
        {({ members: m, finance: f, reservations: r, operational: o }) => {
          const leastUsed = r.bySpace.length > 1 ? r.bySpace[r.bySpace.length - 1] : null
          const peak = r.byHour.reduce<Named | null>((best, hour) => (!best || hour.value > best.value ? hour : best), null)
          return (
            <div className={cn('space-y-10', indicators.isPlaceholderData && 'opacity-60')}>
              <Section title="Associados" description="Quadro atual por situação e movimentação no período.">
                <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
                  <StatCard label="Associados" value={fmt.number(m.total)} hint={`${fmt.number(m.dependents)} dependente(s)`} icon={Users} />
                  <StatCard label="Ativos" value={fmt.number(m.active)} icon={UserCheck} tone="success" />
                  <StatCard label="Suspensos" value={fmt.number(m.suspended)} icon={Ban} tone="danger" />
                  <StatCard label="Aguardando aprovação" value={fmt.number(m.pending)} icon={CalendarClock} tone="warning" />
                  <StatCard label="Cancelados" value={fmt.number(m.cancelled)} icon={UserX} tone="neutral" />
                  <StatCard label="Novos no período" value={fmt.number(m.newInPeriod)} icon={UserPlus} tone="info" />
                  <StatCard label="Cancelamentos no período" value={fmt.number(m.cancelledInPeriod)} icon={UserMinus} tone="neutral" />
                </div>
                <Card>
                  <CardTitle title="Associados por situação" />
                  <RankChart color={colors.brand} format={fmt.number} empty="Nenhum associado cadastrado."
                    data={[{ name: 'Ativos', value: m.active }, { name: 'Aguardando aprovação', value: m.pending }, { name: 'Suspensos', value: m.suspended }, { name: 'Cancelados', value: m.cancelled }]} />
                </Card>
              </Section>

              {f && (
                <Section title="Financeiro" description="Valores efetivamente recebidos e pagos no período; inadimplência na posição atual.">
                  <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
                    <StatCard label="Receitas" value={fmt.money(f.revenue)} icon={PiggyBank} tone="success" />
                    <StatCard label="Despesas" value={fmt.money(f.expenses)} icon={TrendingDown} tone="neutral" />
                    <StatCard label="Resultado" value={<span className={f.net < 0 ? 'text-danger' : undefined}>{fmt.money(f.net)}</span>} icon={Scale} tone={f.net < 0 ? 'danger' : 'brand'} />
                    <StatCard label="Mensalidades recebidas" value={fmt.money(f.monthlyReceived)} icon={Wallet} tone="success" />
                    <StatCard label="Mensalidades pendentes" value={fmt.money(f.monthlyPending)} hint="Com vencimento no período" icon={CalendarClock} tone="warning" />
                    <StatCard label="Multas e juros" value={fmt.money(f.fines)} hint="Lançados em cobranças do período" icon={ShieldAlert} tone="danger" />
                    <StatCard label="Taxas" value={fmt.money(f.fees)} hint="Lançadas em cobranças do período" icon={CircleDollarSign} tone="info" />
                    <StatCard label="Inadimplência" value={fmt.money(f.overdueAmount)} hint={`${fmt.number(f.delinquentMembers)} associado(s) · taxa de ${fmt.percent(f.delinquencyRate)}`} icon={AlertTriangle} tone="danger" />
                  </div>
                  <Card>
                    <CardTitle title="Receitas por categoria" />
                    <RankChart data={f.revenueByCategory} color={colors.brand} format={fmt.money} empty="Nenhuma receita recebida no período." />
                  </Card>
                </Section>
              )}

              <Section title="Reservas" description="Reservas com início no período.">
                <div className="grid grid-cols-2 gap-4 xl:grid-cols-5">
                  <StatCard label="Reservas" value={fmt.number(r.total)} icon={CalendarClock} />
                  <StatCard label="Aprovadas" value={fmt.number(r.approved)} hint="Inclui concluídas" icon={CalendarCheck} tone="success" />
                  <StatCard label="Cancelamentos" value={fmt.number(r.cancelled)} icon={CalendarX} tone="neutral" />
                  <StatCard label="Não comparecimentos" value={fmt.number(r.noShows)} icon={UserX} tone="danger" />
                  <StatCard label="Taxa de ocupação" value={fmt.percent(r.occupancyRate)} hint="Horas reservadas ÷ horas disponíveis" icon={Percent} tone="info" />
                </div>
                <div className="grid gap-4 xl:grid-cols-2">
                  <Card>
                    <CardTitle title="Uso por espaço" description={leastUsed && r.bySpace[0].value > 0 ? `Mais usado: ${r.bySpace[0].name} · menos usado: ${leastUsed.name}` : 'Do mais ao menos utilizado.'} />
                    <RankChart data={r.bySpace} color={colors.brand} format={(value) => `${fmt.number(value)} reserva(s)`} empty="Nenhuma reserva aprovada no período." />
                  </Card>
                  <Card>
                    <CardTitle title="Horários mais procurados" description={peak ? `Pico às ${peak.name}` : undefined} />
                    {r.byHour.length === 0 ? <Empty>Nenhuma reserva aprovada no período.</Empty> : (
                      <div className="h-64">
                        <ResponsiveContainer width="100%" height="100%">
                          <BarChart data={r.byHour} margin={{ top: 8, right: 8, bottom: 0, left: 0 }}>
                            <CartesianGrid vertical={false} stroke={colors.grid} />
                            <XAxis dataKey="name" tickLine={false} axisLine={{ stroke: colors.grid }} tick={{ fill: colors.axis, fontSize: 12 }} />
                            <YAxis width={36} allowDecimals={false} tickLine={false} axisLine={false} tick={{ fill: colors.axis, fontSize: 12 }} />
                            <Tooltip cursor={{ fill: 'rgb(20 32 28 / 0.04)' }} formatter={(value) => `${fmt.number(Number(value))} reserva(s)`} contentStyle={tooltipStyle} />
                            <Bar dataKey="value" name="Reservas" fill={colors.info} radius={[4, 4, 0, 0]} maxBarSize={28} />
                          </BarChart>
                        </ResponsiveContainer>
                      </div>
                    )}
                  </Card>
                  <Card className="xl:col-span-2">
                    <CardTitle title="Receita de reservas por espaço" />
                    <RankChart data={r.revenueBySpace} color={colors.gold} format={fmt.money} empty="Nenhuma reserva com valor no período." />
                  </Card>
                </div>
              </Section>

              <Section title="Operacional" description="Manutenção, ocorrências e portaria no período.">
                <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
                  <StatCard label="Manutenções em aberto" value={fmt.number(o.openMaintenances)} hint="Posição atual" icon={Wrench} tone="warning" />
                  <StatCard label="Manutenções abertas no período" value={fmt.number(o.maintenancesInPeriod)} icon={Hammer} tone="info" />
                  <StatCard label="Custo de manutenção" value={fmt.money(o.maintenanceCost)} hint="Ordens concluídas no período" icon={CircleDollarSign} tone="neutral" />
                  <StatCard label="Ocorrências" value={fmt.number(o.occurrences)} hint={`${fmt.number(o.damages)} com dano`} icon={AlertTriangle} tone="danger" />
                  <StatCard label="Danos registrados" value={fmt.number(o.damages)} icon={ShieldAlert} tone="danger" />
                  <StatCard label="Convidados" value={fmt.number(o.guests)} icon={UserPlus} tone="info" />
                  <StatCard label="Entradas no clube" value={fmt.number(o.entries)} icon={DoorOpen} tone="success" />
                  <StatCard label="Acessos negados" value={fmt.number(o.deniedAccesses)} icon={Gauge} tone="danger" />
                </div>
              </Section>
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}
