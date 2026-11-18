/* Blocos locais do painel de políticas: linha de configuração e campo numérico que aceita "não definido" (vazio → null). */
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Badge, cn, Input } from '@/components/ui'

/** Uma política: rótulo e explicação à esquerda, controle à direita. `undefinedValue` sinaliza política ainda não definida. */
export function Row({ label, hint, error, undefinedValue, stacked, children }: { label: string; hint?: ReactNode; error?: string; undefinedValue?: boolean; stacked?: boolean; children: ReactNode }) {
  return (
    <div className={cn('py-4', !stacked && 'flex items-start justify-between gap-8')}>
      <div className={cn('min-w-0', !stacked && 'max-w-xl')}>
        <p className="flex flex-wrap items-center gap-2 text-sm font-medium text-ink">{label}{undefinedValue && <Badge tone="warning">Não definido</Badge>}</p>
        {hint && <p className="mt-0.5 text-[13px] text-muted">{hint}</p>}
        {error && <p className="mt-1 text-xs font-medium text-danger">{error}</p>}
      </div>
      <div className={cn(stacked ? 'mt-3' : 'shrink-0')}>{children}</div>
    </div>
  )
}

const show = (value: number | null) => (value == null || Number.isNaN(value) ? '' : String(value).replace('.', ','))

/** Número inteiro ou decimal controlado por `number | null`. Campo vazio = null (não definido); texto inválido = NaN (barrado na validação). */
export function NumberInput({ value, onChange, suffix, decimal, invalid, className, placeholder = 'Não definido', label }: {
  value: number | null; onChange: (value: number | null) => void; suffix?: string; decimal?: boolean; invalid?: boolean; className?: string; placeholder?: string; label?: string
}) {
  const [text, setText] = useState(show(value))
  const last = useRef(value)
  useEffect(() => {
    if (!Object.is(value, last.current)) { setText(show(value)); last.current = value }
  }, [value])
  return (
    <span className={cn('relative block w-40', className)}>
      <Input inputMode={decimal ? 'decimal' : 'numeric'} className={cn('tabular', suffix && 'pr-20')} placeholder={placeholder} aria-label={label} aria-invalid={invalid} value={text}
        onChange={(e) => {
          const cleaned = decimal ? e.target.value.replace(/[^\d,.]/g, '') : e.target.value.replace(/\D/g, '')
          const parsed = cleaned === '' ? null : Number(cleaned.replace(',', '.'))
          setText(cleaned); last.current = parsed; onChange(parsed)
        }} />
      {suffix && <span className="pointer-events-none absolute top-1/2 right-3 -translate-y-1/2 text-[13px] text-muted">{suffix}</span>}
    </span>
  )
}
