/* Design system do Web. Um único conjunto de componentes para toda a aplicação: nunca crie variações locais de botão, campo, card ou tabela. */
import { clsx } from 'clsx'
import { AlertCircle, CheckCircle2, ChevronDown, Inbox, Loader2, Search, Upload, User, X } from 'lucide-react'
import {
  createContext, useCallback, useContext, useEffect, useId, useRef, useState,
  type ButtonHTMLAttributes, type ComponentType, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes,
} from 'react'
import { useQuery } from '@tanstack/react-query'
import { api, errorMessage, type Schema } from '@/lib/api'
import { masks, parseMoney } from '@/lib/format'
import type { Tone } from '@/lib/labels'

export const cn = clsx
type Icon = ComponentType<{ className?: string }>

// ---------- Botão ----------

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md'
  loading?: boolean
  icon?: Icon
}

export function Button({ variant = 'secondary', size = 'md', loading, icon: IconComponent, className, children, disabled, type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={cn(
        'inline-flex shrink-0 items-center justify-center gap-2 rounded-(--radius-control) font-medium whitespace-nowrap transition-colors disabled:cursor-not-allowed disabled:opacity-50',
        size === 'sm' ? 'h-8 px-3 text-[13px]' : 'h-10 px-4 text-sm',
        variant === 'primary' && 'bg-brand-600 text-white shadow-sm hover:bg-brand-700',
        variant === 'secondary' && 'border border-line bg-surface text-ink hover:bg-canvas',
        variant === 'ghost' && 'text-muted hover:bg-black/5 hover:text-ink',
        variant === 'danger' && 'bg-danger text-white shadow-sm hover:brightness-95',
        className,
      )}
      {...props}
    >
      {loading ? <Loader2 className="size-4 animate-spin" /> : IconComponent ? <IconComponent className="size-4" /> : null}
      {children}
    </button>
  )
}

export function IconButton({ icon: IconComponent, label, className, ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { icon: Icon; label: string }) {
  return (
    <button type="button" aria-label={label} title={label} className={cn('inline-flex size-8 items-center justify-center rounded-lg text-muted transition-colors hover:bg-black/5 hover:text-ink disabled:opacity-40', className)} {...props}>
      <IconComponent className="size-4" />
    </button>
  )
}

// ---------- Campos ----------

const control = 'w-full rounded-(--radius-control) border border-line bg-surface px-3 text-sm text-ink placeholder:text-muted/70 transition-colors focus:border-brand-400 focus:outline-none focus:ring-3 focus:ring-brand-100 disabled:bg-canvas disabled:text-muted aria-invalid:border-danger aria-invalid:ring-danger-soft'

export function Field({ label, error, hint, required, children, className }: { label?: string; error?: string; hint?: string; required?: boolean; children: ReactNode; className?: string }) {
  return (
    <label className={cn('block', className)}>
      {label && <span className="mb-1.5 block text-[13px] font-medium text-ink">{label}{required && <span className="text-danger"> *</span>}</span>}
      {children}
      {error ? <span className="mt-1 flex items-center gap-1 text-xs text-danger"><AlertCircle className="size-3.5" />{error}</span>
        : hint ? <span className="mt-1 block text-xs text-muted">{hint}</span> : null}
    </label>
  )
}

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn(control, 'h-10', className)} {...props} />
}

export function Textarea({ className, rows = 4, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea rows={rows} className={cn(control, 'py-2 leading-relaxed', className)} {...props} />
}

export function Select({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <span className="relative block">
      <select className={cn(control, 'h-10 appearance-none pr-9', className)} {...props}>{children}</select>
      <ChevronDown className="pointer-events-none absolute top-1/2 right-3 size-4 -translate-y-1/2 text-muted" />
    </span>
  )
}

/** Campo com máscara (CPF, CNPJ, telefone, CEP). Controlado: recebe e devolve o texto já mascarado. */
export function MaskedInput({ mask, value, onChange, ...props }: Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange' | 'value'> & { mask: keyof typeof masks; value: string; onChange: (value: string) => void }) {
  return <Input inputMode="numeric" value={masks[mask](value ?? '')} onChange={(e) => onChange(masks[mask](e.target.value))} {...props} />
}

/** Valor monetário em reais. Controlado por número (ou null quando vazio). */
export function MoneyInput({ value, onChange, placeholder = '0,00', ...props }: Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange' | 'value'> & { value: number | null | undefined; onChange: (value: number | null) => void }) {
  const [text, setText] = useState(value == null ? '' : value.toFixed(2).replace('.', ','))
  const last = useRef(value)
  useEffect(() => {
    if (value !== last.current) { setText(value == null ? '' : value.toFixed(2).replace('.', ',')); last.current = value }
  }, [value])
  return (
    <span className="relative block">
      <span className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-sm text-muted">R$</span>
      <Input
        inputMode="decimal" className="tabular pl-9" placeholder={placeholder} value={text}
        onChange={(e) => { setText(e.target.value); const parsed = parseMoney(e.target.value); last.current = parsed; onChange(parsed) }}
        onBlur={() => { const parsed = parseMoney(text); setText(parsed == null ? '' : parsed.toFixed(2).replace('.', ',')) }}
        {...props}
      />
    </span>
  )
}

export function Checkbox({ label, className, ...props }: InputHTMLAttributes<HTMLInputElement> & { label?: ReactNode }) {
  return (
    <label className={cn('inline-flex cursor-pointer items-center gap-2 text-sm', className)}>
      <input type="checkbox" className="size-4 rounded border-line accent-brand-600" {...props} />
      {label}
    </label>
  )
}

export function Switch({ checked, onChange, label, description, disabled }: { checked: boolean; onChange: (checked: boolean) => void; label?: ReactNode; description?: ReactNode; disabled?: boolean }) {
  return (
    <label className={cn('flex items-start gap-3', disabled ? 'cursor-not-allowed opacity-60' : 'cursor-pointer')}>
      <button
        type="button" role="switch" aria-checked={checked} disabled={disabled} onClick={() => onChange(!checked)}
        className={cn('relative mt-0.5 h-5 w-9 shrink-0 rounded-full transition-colors', checked ? 'bg-brand-600' : 'bg-line')}
      >
        <span className={cn('absolute top-0.5 left-0.5 size-4 rounded-full bg-white shadow transition-transform', checked && 'translate-x-4')} />
      </button>
      {(label || description) && <span className="text-sm"><span className="block font-medium text-ink">{label}</span>{description && <span className="block text-xs text-muted">{description}</span>}</span>}
    </label>
  )
}

/** Escolha de três estados para políticas ainda não definidas: Sim / Não / Não definido (null). */
export function TriState({ value, onChange, yes = 'Sim', no = 'Não', unset = 'Não definido', disabled }: { value: boolean | null | undefined; onChange: (value: boolean | null) => void; yes?: string; no?: string; unset?: string; disabled?: boolean }) {
  const items: { key: string; value: boolean | null; label: string }[] = [{ key: 'null', value: null, label: unset }, { key: 'true', value: true, label: yes }, { key: 'false', value: false, label: no }]
  return (
    <div className="inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5">
      {items.map((item) => (
        <button key={item.key} type="button" disabled={disabled} onClick={() => onChange(item.value)}
          className={cn('rounded-lg px-3 py-1.5 text-[13px] font-medium transition-colors', (value ?? null) === item.value ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink')}>
          {item.label}
        </button>
      ))}
    </div>
  )
}

export function SearchInput({ value, onChange, placeholder = 'Pesquisar…', className }: { value: string; onChange: (value: string) => void; placeholder?: string; className?: string }) {
  const [text, setText] = useState(value)
  useEffect(() => setText(value), [value])
  useEffect(() => {
    const timer = setTimeout(() => { if (text !== value) onChange(text) }, 300)
    return () => clearTimeout(timer)
  }, [text, value, onChange])
  return (
    <span className={cn('relative block w-full max-w-xs', className)}>
      <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted" />
      <Input className="pl-9" placeholder={placeholder} value={text} onChange={(e) => setText(e.target.value)} />
    </span>
  )
}

// ---------- Superfícies ----------

export function Card({ className, children, padded = true }: { className?: string; children: ReactNode; padded?: boolean }) {
  return <section className={cn('rounded-(--radius-card) border border-line bg-surface shadow-(--shadow-card)', padded && 'p-5', className)}>{children}</section>
}

export function CardTitle({ title, description, actions, icon: IconComponent }: { title: string; description?: ReactNode; actions?: ReactNode; icon?: Icon }) {
  return (
    <header className="mb-4 flex items-start justify-between gap-4">
      <div className="flex items-start gap-3">
        {IconComponent && <span className="mt-0.5 flex size-8 items-center justify-center rounded-lg bg-brand-50 text-brand-600"><IconComponent className="size-4" /></span>}
        <div>
          <h2 className="text-[15px] font-semibold text-ink">{title}</h2>
          {description && <p className="mt-0.5 text-[13px] text-muted">{description}</p>}
        </div>
      </div>
      {actions && <div className="flex shrink-0 items-center gap-2">{actions}</div>}
    </header>
  )
}

const tones: Record<Tone, string> = {
  neutral: 'bg-black/5 text-muted', brand: 'bg-brand-50 text-brand-700', success: 'bg-success-soft text-success',
  warning: 'bg-warning-soft text-warning', danger: 'bg-danger-soft text-danger', info: 'bg-info-soft text-info',
}

export function Badge({ tone = 'neutral', children, className }: { tone?: Tone; children: ReactNode; className?: string }) {
  return <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap', tones[tone], className)}>{children}</span>
}

/** Badge a partir de um dicionário de `labels` (ex.: <StatusBadge dict={labels.memberStatus} value={m.status} />). */
export function StatusBadge<K extends string>({ dict, value }: { dict: Record<K, { label: string; tone: Tone }>; value: K | null | undefined }) {
  if (!value || !dict[value]) return <span className="text-muted">—</span>
  return <Badge tone={dict[value].tone}>{dict[value].label}</Badge>
}

export function Alert({ tone = 'info', title, children, className }: { tone?: 'info' | 'warning' | 'danger' | 'success'; title?: string; children?: ReactNode; className?: string }) {
  return (
    <div className={cn('flex gap-3 rounded-(--radius-control) px-4 py-3 text-sm', tones[tone], className)}>
      {tone === 'success' ? <CheckCircle2 className="mt-0.5 size-4 shrink-0" /> : <AlertCircle className="mt-0.5 size-4 shrink-0" />}
      <div className="text-ink">{title && <p className="font-medium">{title}</p>}{children && <div className={cn(title && 'mt-0.5', 'text-[13px] text-ink/80')}>{children}</div>}</div>
    </div>
  )
}

export function PageHeader({ title, subtitle, actions, back }: { title: string; subtitle?: ReactNode; actions?: ReactNode; back?: ReactNode }) {
  return (
    <header className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        {back}
        <h1 className="text-2xl font-semibold tracking-tight text-ink">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-muted">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </header>
  )
}

export function StatCard({ label, value, hint, icon: IconComponent, tone = 'brand' }: { label: string; value: ReactNode; hint?: ReactNode; icon?: Icon; tone?: Tone }) {
  return (
    <Card className="flex items-start justify-between gap-3">
      <div className="min-w-0">
        <p className="text-[13px] text-muted">{label}</p>
        <p className="tabular mt-1 truncate text-2xl font-semibold tracking-tight text-ink">{value}</p>
        {hint && <p className="mt-1 text-xs text-muted">{hint}</p>}
      </div>
      {IconComponent && <span className={cn('flex size-9 shrink-0 items-center justify-center rounded-xl', tones[tone])}><IconComponent className="size-[18px]" /></span>}
    </Card>
  )
}

export function Tabs<K extends string>({ tabs, value, onChange, className }: { tabs: { key: K; label: string; count?: number }[]; value: K; onChange: (key: K) => void; className?: string }) {
  return (
    <div className={cn('mb-5 flex gap-1 border-b border-line', className)} role="tablist">
      {tabs.map((tab) => (
        <button key={tab.key} role="tab" aria-selected={tab.key === value} onClick={() => onChange(tab.key)}
          className={cn('-mb-px flex items-center gap-2 border-b-2 px-3 py-2.5 text-sm font-medium transition-colors', tab.key === value ? 'border-brand-600 text-brand-700' : 'border-transparent text-muted hover:text-ink')}>
          {tab.label}
          {tab.count != null && tab.count > 0 && <span className="rounded-full bg-black/5 px-1.5 text-xs">{tab.count}</span>}
        </button>
      ))}
    </div>
  )
}

/** Par rótulo/valor para telas de detalhe. */
export function Info({ label, children, className }: { label: string; children: ReactNode; className?: string }) {
  return (
    <div className={className}>
      <dt className="text-xs text-muted">{label}</dt>
      <dd className="mt-0.5 text-sm text-ink">{children ?? '—'}</dd>
    </div>
  )
}

// ---------- Estados ----------

export function Spinner({ className }: { className?: string }) {
  return <Loader2 className={cn('size-5 animate-spin text-brand-600', className)} />
}

export function LoadingState({ label = 'Carregando…' }: { label?: string }) {
  return <div className="flex flex-col items-center justify-center gap-3 py-16 text-sm text-muted"><Spinner />{label}</div>
}

export function EmptyState({ title, description, action, icon: IconComponent = Inbox }: { title: string; description?: ReactNode; action?: ReactNode; icon?: Icon }) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-14 text-center">
      <span className="flex size-11 items-center justify-center rounded-full bg-canvas text-muted"><IconComponent className="size-5" /></span>
      <p className="mt-3 text-sm font-medium text-ink">{title}</p>
      {description && <p className="mt-1 max-w-sm text-[13px] text-muted">{description}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  )
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-14 text-center">
      <span className="flex size-11 items-center justify-center rounded-full bg-danger-soft text-danger"><AlertCircle className="size-5" /></span>
      <p className="mt-3 text-sm font-medium text-ink">Não foi possível carregar</p>
      <p className="mt-1 max-w-sm text-[13px] text-muted">{errorMessage(error)}</p>
      {onRetry && <Button className="mt-4" size="sm" onClick={onRetry}>Tentar novamente</Button>}
    </div>
  )
}

/** Envolve o conteúdo de uma consulta: carregando → erro (com repetir) → conteúdo. */
export function QueryBoundary<T>({ query, children }: { query: { data: T | undefined; isLoading: boolean; error: unknown; refetch: () => unknown }; children: (data: T) => ReactNode }) {
  if (query.isLoading) return <LoadingState />
  if (query.error || query.data === undefined) return <ErrorState error={query.error} onRetry={() => query.refetch()} />
  return <>{children(query.data)}</>
}

// ---------- Modal ----------

export function Modal({ open, onClose, title, description, children, footer, size = 'md' }: {
  open: boolean; onClose: () => void; title: string; description?: ReactNode; children: ReactNode; footer?: ReactNode; size?: 'sm' | 'md' | 'lg' | 'xl'
}) {
  useEffect(() => {
    if (!open) return
    const onKey = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose() }
    document.addEventListener('keydown', onKey)
    document.body.style.overflow = 'hidden'
    return () => { document.removeEventListener('keydown', onKey); document.body.style.overflow = '' }
  }, [open, onClose])
  if (!open) return null
  return (
    <div className="animate-fade fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-ink/40 p-6 backdrop-blur-[2px]" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose() }}>
      <div role="dialog" aria-modal="true" aria-label={title}
        className={cn('animate-pop my-auto w-full rounded-2xl bg-surface shadow-(--shadow-pop)', { sm: 'max-w-md', md: 'max-w-xl', lg: 'max-w-3xl', xl: 'max-w-5xl' }[size])}>
        <header className="flex items-start justify-between gap-4 border-b border-line px-6 py-4">
          <div>
            <h2 className="text-base font-semibold text-ink">{title}</h2>
            {description && <p className="mt-0.5 text-[13px] text-muted">{description}</p>}
          </div>
          <IconButton icon={X} label="Fechar" onClick={onClose} />
        </header>
        <div className="px-6 py-5">{children}</div>
        {footer && <footer className="flex items-center justify-end gap-2 border-t border-line bg-canvas/60 px-6 py-3.5">{footer}</footer>}
      </div>
    </div>
  )
}

// ---------- Confirmação (com justificativa opcional/obrigatória) ----------

type ConfirmOptions = {
  title: string
  message?: ReactNode
  confirmLabel?: string
  danger?: boolean
  /** Solicita um texto (motivo/justificativa). 'required' impede confirmar sem preencher. */
  reason?: 'required' | 'optional'
  reasonLabel?: string
  /** Tamanho mínimo do motivo quando obrigatório (padrão 3; use o mínimo exigido pela API). */
  reasonMinLength?: number
}
type ConfirmResult = false | { reason: string }

const ConfirmContext = createContext<((options: ConfirmOptions) => Promise<ConfirmResult>) | null>(null)

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<{ options: ConfirmOptions; resolve: (result: ConfirmResult) => void } | null>(null)
  const [reason, setReason] = useState('')
  const confirm = useCallback((options: ConfirmOptions) => new Promise<ConfirmResult>((resolve) => { setReason(''); setState({ options, resolve }) }), [])
  const close = (result: ConfirmResult) => { state?.resolve(result); setState(null) }
  const options = state?.options
  const missing = options?.reason === 'required' && reason.trim().length < (options.reasonMinLength ?? 3)
  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      <Modal open={!!state} onClose={() => close(false)} title={options?.title ?? ''} size="sm"
        footer={<>
          <Button onClick={() => close(false)}>Voltar</Button>
          <Button variant={options?.danger ? 'danger' : 'primary'} disabled={missing} onClick={() => close({ reason: reason.trim() })}>{options?.confirmLabel ?? 'Confirmar'}</Button>
        </>}>
        {options?.message && <div className="text-sm text-ink/80">{options.message}</div>}
        {options?.reason && (
          <Field className={options.message ? 'mt-4' : ''} label={options.reasonLabel ?? 'Motivo'} required={options.reason === 'required'}>
            <Textarea autoFocus rows={3} value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Descreva o motivo…" />
          </Field>
        )}
      </Modal>
    </ConfirmContext.Provider>
  )
}

/** `const confirm = useConfirm(); const ok = await confirm({ title, message, reason: 'required' }); if (!ok) return; ok.reason` */
export function useConfirm() {
  const context = useContext(ConfirmContext)
  if (!context) throw new Error('useConfirm deve ser usado dentro de ConfirmProvider')
  return context
}

// ---------- Toasts ----------

type Toast = { id: number; tone: 'success' | 'danger' | 'info'; message: string }
type ToastApi = { success: (message: string) => void; error: (error: unknown) => void; info: (message: string) => void }
const ToastContext = createContext<ToastApi | null>(null)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const push = useCallback((tone: Toast['tone'], message: string) => {
    const id = Date.now() + Math.random()
    setToasts((list) => [...list.slice(-3), { id, tone, message }])
    setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), tone === 'danger' ? 7000 : 4000)
  }, [])
  const apiRef = useRef<ToastApi>({
    success: (message) => push('success', message),
    info: (message) => push('info', message),
    error: (error) => push('danger', typeof error === 'string' ? error : errorMessage(error)),
  })
  return (
    <ToastContext.Provider value={apiRef.current}>
      {children}
      <div className="pointer-events-none fixed right-5 bottom-5 z-[60] flex w-[360px] max-w-[calc(100vw-2.5rem)] flex-col gap-2" aria-live="polite">
        {toasts.map((toast) => (
          <div key={toast.id} className="animate-pop pointer-events-auto flex items-start gap-3 rounded-xl border border-line bg-surface px-4 py-3 text-sm shadow-(--shadow-pop)">
            {toast.tone === 'danger' ? <AlertCircle className="mt-0.5 size-4 shrink-0 text-danger" /> : <CheckCircle2 className={cn('mt-0.5 size-4 shrink-0', toast.tone === 'success' ? 'text-success' : 'text-info')} />}
            <p className="flex-1 text-ink">{toast.message}</p>
            <button className="text-muted hover:text-ink" aria-label="Dispensar" onClick={() => setToasts((list) => list.filter((t) => t.id !== toast.id))}><X className="size-4" /></button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const context = useContext(ToastContext)
  if (!context) throw new Error('useToast deve ser usado dentro de ToastProvider')
  return context
}

// ---------- Menu suspenso ----------

export function Menu({ trigger, items, align = 'right' }: { trigger: ReactNode; items: ({ label: string; icon?: Icon; onClick: () => void; danger?: boolean; disabled?: boolean } | null | false | undefined)[]; align?: 'left' | 'right' }) {
  const [open, setOpen] = useState(false)
  const [position, setPosition] = useState<{ top?: number; bottom?: number; left?: number; right?: number }>({})
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    const close = () => setOpen(false)
    const onDown = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) close() }
    document.addEventListener('mousedown', onDown)
    window.addEventListener('scroll', close, true)
    window.addEventListener('resize', close)
    return () => { document.removeEventListener('mousedown', onDown); window.removeEventListener('scroll', close, true); window.removeEventListener('resize', close) }
  }, [open])
  // Posição fixa (calculada a partir do gatilho) para não ser cortado por tabelas com rolagem; abre para cima perto do rodapé.
  const toggle = () => {
    const rect = ref.current?.getBoundingClientRect()
    if (rect) {
      const upward = window.innerHeight - rect.bottom < 260 && rect.top > 260
      setPosition({
        ...(upward ? { bottom: window.innerHeight - rect.top + 4 } : { top: rect.bottom + 4 }),
        ...(align === 'right' ? { right: window.innerWidth - rect.right } : { left: rect.left }),
      })
    }
    setOpen((o) => !o)
  }
  const visible = items.filter((item): item is Exclude<typeof item, null | false | undefined> => !!item)
  if (visible.length === 0) return null
  return (
    <div className="relative inline-block" ref={ref}>
      <span onClick={(e) => { e.stopPropagation(); toggle() }}>{trigger}</span>
      {open && (
        <div style={position} className="animate-pop fixed z-40 min-w-48 rounded-xl border border-line bg-surface p-1 shadow-(--shadow-pop)">
          {visible.map((item) => (
            <button key={item.label} disabled={item.disabled} onClick={(e) => { e.stopPropagation(); setOpen(false); item.onClick() }}
              className={cn('flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-left text-sm transition-colors disabled:opacity-40', item.danger ? 'text-danger hover:bg-danger-soft' : 'text-ink hover:bg-canvas')}>
              {item.icon && <item.icon className="size-4 opacity-70" />}{item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

// ---------- Arquivos autenticados ----------

/** URL de objeto para um arquivo protegido (GET /api/arquivos/{id} exige o token; não funciona em <img src> direto). */
export function useFileUrl(fileId: string | null | undefined) {
  const query = useQuery({
    queryKey: ['arquivo', fileId],
    queryFn: async () => URL.createObjectURL((await api.blob(`/api/arquivos/${fileId}`)).blob),
    enabled: !!fileId, staleTime: Infinity, gcTime: 10 * 60_000, retry: false,
  })
  return fileId ? query.data : undefined
}

export function Avatar({ fileId, name, size = 36, className }: { fileId?: string | null; name?: string | null; size?: number; className?: string }) {
  const url = useFileUrl(fileId)
  const initials = (name ?? '').split(' ').filter(Boolean).slice(0, 2).map((part) => part[0]?.toUpperCase()).join('')
  return (
    <span style={{ width: size, height: size, fontSize: size * 0.36 }} className={cn('inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-brand-50 font-semibold text-brand-700', className)}>
      {url ? <img src={url} alt={name ?? ''} className="size-full object-cover" /> : initials || <User className="size-1/2" />}
    </span>
  )
}

export function FileImage({ fileId, alt, className }: { fileId: string | null | undefined; alt?: string; className?: string }) {
  const url = useFileUrl(fileId)
  if (!fileId) return null
  return url ? <img src={url} alt={alt ?? ''} className={className} /> : <div className={cn('animate-pulse bg-canvas', className)} />
}

/** Botão de envio de arquivo. Devolve o FileDto após a confirmação do servidor. */
export function FileUpload({ onUploaded, scope = 'Restricted', ownerMemberId, accept = 'image/jpeg,image/png,image/webp,application/pdf', label = 'Enviar arquivo', size = 'md' }: {
  onUploaded: (file: Schema<'FileDto'>) => void; scope?: Schema<'FileScope'>; ownerMemberId?: string | null; accept?: string; label?: string; size?: 'sm' | 'md'
}) {
  const toast = useToast()
  const [busy, setBusy] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const id = useId()
  return (
    <>
      <input ref={inputRef} id={id} type="file" accept={accept} className="hidden" onChange={async (event) => {
        const file = event.target.files?.[0]
        event.target.value = ''
        if (!file) return
        setBusy(true)
        try { onUploaded(await api.upload(file, scope, ownerMemberId)) } catch (error) { toast.error(error) } finally { setBusy(false) }
      }} />
      <Button size={size} icon={Upload} loading={busy} onClick={() => inputRef.current?.click()}>{label}</Button>
    </>
  )
}
