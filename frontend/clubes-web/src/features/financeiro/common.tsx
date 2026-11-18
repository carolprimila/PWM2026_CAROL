/* Blocos compartilhados pelas telas do módulo financeiro. */
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { cn, Input, Select, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { dates, fmt } from '@/lib/format'
import { options, type Tone } from '@/lib/labels'

/** Cores dos gráficos — mesmos valores dos tokens de `src/index.css`. */
export const chartColors = { inflow: '#1c7460', outflow: '#c0392b', net: '#2b6cb0', grid: '#e3e7e3', axis: '#6b7770', ink: '#14201c' }

/** Invalida todas as consultas financeiras (cobranças, mensalidades, pagamentos, lançamentos, fluxo, inadimplência…). */
export function useInvalidateFinance() {
  const queryClient = useQueryClient()
  return () => {
    void queryClient.invalidateQueries({
      predicate: ({ queryKey: [root] }) => typeof root === 'string'
        && (root === 'financeiro' || root === 'dashboard' || root === 'associado' || root.startsWith('/api/financeiro') || root.startsWith('/api/mensalidades') || root.startsWith('/api/pagamentos')),
    })
  }
}

export function MemberLink({ id, name, number }: { id: string; name: string; number?: number }) {
  return (
    <Link to={`/associados/${id}`} onClick={(e) => e.stopPropagation()} className="group block min-w-0">
      <span className="block truncate font-medium text-ink group-hover:text-brand-700 group-hover:underline">{name || '—'}</span>
      {number != null && <span className="block text-xs text-muted">Matrícula {number}</span>}
    </Link>
  )
}

export function EnumSelect<K extends string>({ dict, value, onChange, all, className = 'w-48' }: {
  dict: Record<K, { label: string; tone: Tone }>; value: string; onChange: (value: string) => void; all: string; className?: string
}) {
  return (
    <div className={className}>
      <Select value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">{all}</option>
        {options(dict).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
      </Select>
    </div>
  )
}

/** Competência atual ("aaaa-mm") e deslocamentos em meses. */
export const competenceOf = (offset = 0) => { const d = new Date(); return dates.toDay(new Date(d.getFullYear(), d.getMonth() + offset, 1)).slice(0, 7) }

// ---------- Período ----------

export type Period = { from: string; to: string }
const presets: { label: string; range: () => Period }[] = [
  { label: 'Mês atual', range: () => ({ from: dates.firstOfMonth(), to: dates.lastOfMonth() }) },
  { label: 'Mês anterior', range: () => { const d = new Date(); const p = new Date(d.getFullYear(), d.getMonth() - 1, 1); return { from: dates.firstOfMonth(p), to: dates.lastOfMonth(p) } } },
  { label: 'Últimos 90 dias', range: () => ({ from: dates.addDays(dates.today(), -89), to: dates.today() }) },
  { label: 'Este ano', range: () => { const y = new Date().getFullYear(); return { from: `${y}-01-01`, to: `${y}-12-31` } } },
]
export const defaultPeriod = () => presets[0].range()

export function PeriodFilter({ value, onChange }: { value: Period; onChange: (period: Period) => void }) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5">
        {presets.map((preset) => {
          const range = preset.range()
          const active = range.from === value.from && range.to === value.to
          return (
            <button key={preset.label} type="button" onClick={() => onChange(range)}
              className={cn('rounded-lg px-3 py-1.5 text-[13px] font-medium transition-colors', active ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink')}>
              {preset.label}
            </button>
          )
        })}
      </div>
      <Input type="date" aria-label="De" className="w-40" value={value.from} max={value.to} onChange={(e) => e.target.value && onChange({ ...value, from: e.target.value })} />
      <span className="text-sm text-muted">até</span>
      <Input type="date" aria-label="Até" className="w-40" value={value.to} min={value.from} onChange={(e) => e.target.value && onChange({ ...value, to: e.target.value })} />
    </div>
  )
}

// ---------- Listas com barras proporcionais ----------

export function BarList({ items, color, format = fmt.money, empty = 'Sem movimentações no período.' }: {
  items: { name: string; value: number }[]; color: string; format?: (value: number) => string; empty?: string
}) {
  const max = Math.max(...items.map((item) => item.value), 0)
  if (items.length === 0) return <p className="py-6 text-center text-sm text-muted">{empty}</p>
  return (
    <ul className="space-y-3">
      {items.map((item) => (
        <li key={item.name}>
          <div className="flex items-baseline justify-between gap-3 text-sm">
            <span className="truncate text-ink">{item.name}</span>
            <span className="tabular shrink-0 font-medium text-ink">{format(item.value)}</span>
          </div>
          <div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-canvas">
            <div className="h-full rounded-full" style={{ width: `${max > 0 ? Math.max(2, (item.value / max) * 100) : 0}%`, backgroundColor: color }} />
          </div>
        </li>
      ))}
    </ul>
  )
}

/** Linha "rótulo … valor" usada em resumos de valores. */
export function AmountRow({ label, value, sign, strong, tone, children }: { label: ReactNode; value: number; sign?: '+' | '−'; strong?: boolean; tone?: 'danger' | 'success'; children?: ReactNode }) {
  return (
    <div className={cn('flex items-start justify-between gap-4 py-2 text-sm', strong && 'font-semibold')}>
      <div className="min-w-0"><span className={strong ? 'text-ink' : 'text-ink/80'}>{label}</span>{children}</div>
      <span className={cn('tabular shrink-0', tone === 'danger' ? 'text-danger' : tone === 'success' ? 'text-success' : 'text-ink')}>{sign ? `${sign} ` : ''}{fmt.money(value)}</span>
    </div>
  )
}

// ---------- Pagamentos: comprovante e estorno ----------

type Payment = Schema<'PaymentDto'>

export function useReceiptDownload() {
  const toast = useToast()
  const [busyId, setBusyId] = useState<string | null>(null)
  const download = async (payment: Payment) => {
    setBusyId(payment.id)
    try { await api.download(`/api/pagamentos/${payment.id}/comprovante`, undefined, `comprovante-${payment.receiptNumber}.pdf`) } catch (error) { toast.error(error) } finally { setBusyId(null) }
  }
  return { busyId, download }
}

/** Estorno com motivo obrigatório. O pagamento permanece no histórico e a cobrança volta a ficar em aberto. */
export function useRefund() {
  const toast = useToast()
  const confirm = useConfirm()
  const invalidate = useInvalidateFinance()
  const mutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => api.post<Payment>(`/api/pagamentos/${id}/estornar`, { reason }),
    onSuccess: (payment) => { toast.success(`Pagamento ${payment.receiptNumber} estornado.`); invalidate() },
    onError: toast.error,
  })
  const refund = async (payment: Payment) => {
    const result = await confirm({
      title: 'Estornar pagamento', danger: true, confirmLabel: 'Estornar', reason: 'required', reasonLabel: 'Motivo do estorno',
      message: <>O pagamento de <strong>{fmt.money(payment.amount)}</strong> (recibo nº {payment.receiptNumber}) será marcado como estornado e a cobrança voltará a ficar em aberto. O associado será notificado.</>,
    })
    if (result) mutation.mutate({ id: payment.id, reason: result.reason })
  }
  return { refund, isPending: mutation.isPending }
}

export function BackLink({ to, children }: { to: string; children: ReactNode }) {
  return <Link to={to} className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-muted hover:text-ink">{children}</Link>
}
