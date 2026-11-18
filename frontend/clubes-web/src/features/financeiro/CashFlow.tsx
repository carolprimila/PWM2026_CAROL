import { AlertTriangle, ArrowDownCircle, ArrowUpCircle, BarChart3, CalendarClock, Scale, Wallet } from 'lucide-react'
import { useState } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Card, CardTitle, EmptyState, PageHeader, QueryBoundary, StatCard } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { fmt } from '@/lib/format'
import { BarList, chartColors, defaultPeriod, PeriodFilter, type Period } from './common'

/** "aaaa-mm-dd" → dd/mm; "aaaa-mm" → mm/aaaa (a API agrupa por dia até ~2 meses e por mês acima disso). */
const periodLabel = (period: string) => (period.length > 7 ? `${period.slice(8, 10)}/${period.slice(5, 7)}` : fmt.competence(period))
const compactMoney = new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 })

export default function CashFlowPage() {
  const [period, setPeriod] = useState<Period>(defaultPeriod)
  const flow = useQuery({
    queryKey: ['/api/financeiro/fluxo-de-caixa', period],
    queryFn: () => api.get<Schema<'CashFlowDto'>>('/api/financeiro/fluxo-de-caixa', period),
    placeholderData: keepPreviousData,
  })

  return (
    <>
      <PageHeader title="Fluxo de caixa" subtitle="Entradas e saídas efetivadas no período, com a posição atual de contas a receber e a pagar." />
      <div className="mb-6"><PeriodFilter value={period} onChange={setPeriod} /></div>
      <QueryBoundary query={flow}>
        {(d) => (
          <div className={flow.isPlaceholderData ? 'space-y-6 opacity-60' : 'space-y-6'}>
            <div className="grid grid-cols-2 gap-4 xl:grid-cols-3">
              <StatCard label="Entradas no período" value={fmt.money(d.inflow)} icon={ArrowDownCircle} tone="success" />
              <StatCard label="Saídas no período" value={fmt.money(d.outflow)} icon={ArrowUpCircle} tone="danger" />
              <StatCard label="Resultado do período" value={<span className={d.net < 0 ? 'text-danger' : undefined}>{fmt.money(d.net)}</span>} icon={Scale} tone={d.net < 0 ? 'danger' : 'brand'} />
              <StatCard label="A receber" value={fmt.money(d.receivable)} hint="Cobranças em aberto e receitas pendentes (posição atual)" icon={Wallet} tone="info" />
              <StatCard label="Em atraso" value={fmt.money(d.overdueReceivable)} hint="Cobranças vencidas e não pagas (posição atual)" icon={AlertTriangle} tone="danger" />
              <StatCard label="A pagar" value={fmt.money(d.payable)} hint="Despesas pendentes (posição atual)" icon={CalendarClock} tone="warning" />
            </div>

            <Card>
              <CardTitle icon={BarChart3} title="Entradas e saídas" description={`De ${fmt.day(d.from)} a ${fmt.day(d.to)}, por ${d.series.some((p) => p.period.length > 7) ? 'dia' : 'mês'}.`} />
              {d.series.length === 0 ? <EmptyState title="Sem movimentações no período" description="Pagamentos confirmados e lançamentos pagos ou recebidos aparecem aqui." /> : (
                <div className="h-80">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={d.series.map((p) => ({ ...p, label: periodLabel(p.period) }))} margin={{ top: 8, right: 8, bottom: 0, left: 0 }} barGap={2}>
                      <CartesianGrid vertical={false} stroke={chartColors.grid} />
                      <XAxis dataKey="label" tickLine={false} axisLine={{ stroke: chartColors.grid }} tick={{ fill: chartColors.axis, fontSize: 12 }} minTickGap={12} />
                      <YAxis tickLine={false} axisLine={false} width={56} tick={{ fill: chartColors.axis, fontSize: 12 }} tickFormatter={(value: number) => compactMoney.format(value)} />
                      <Tooltip cursor={{ fill: 'rgb(20 32 28 / 0.04)' }} formatter={(value) => fmt.money(Number(value))}
                        contentStyle={{ borderRadius: 10, border: `1px solid ${chartColors.grid}`, fontSize: 13, color: chartColors.ink }} />
                      <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 13 }} />
                      <Bar dataKey="inflow" name="Entradas" fill={chartColors.inflow} radius={[4, 4, 0, 0]} maxBarSize={28} />
                      <Bar dataKey="outflow" name="Saídas" fill={chartColors.outflow} radius={[4, 4, 0, 0]} maxBarSize={28} />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              )}
            </Card>

            <div className="grid gap-6 xl:grid-cols-2">
              <Card>
                <CardTitle title="Entradas por categoria" description="Pagamentos de cobranças e outras receitas recebidas." />
                <BarList color={chartColors.inflow} items={d.inflowByCategory.map((c) => ({ name: c.category, value: c.amount }))} empty="Nenhuma entrada no período." />
              </Card>
              <Card>
                <CardTitle title="Saídas por categoria" description="Despesas pagas no período." />
                <BarList color={chartColors.outflow} items={d.outflowByCategory.map((c) => ({ name: c.category, value: c.amount }))} empty="Nenhuma saída no período." />
              </Card>
            </div>
          </div>
        )}
      </QueryBoundary>
    </>
  )
}
