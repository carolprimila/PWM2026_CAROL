import { BarChart3, CheckCircle2, DoorOpen, History, KeyRound, ScanLine, UserCheck, UserPlus, Users, XCircle } from 'lucide-react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Avatar, Button, Card, CardTitle, cn, EmptyState, ErrorState, Input, PageHeader, StatCard, StatusBadge, Tabs, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { useRealtime } from '@/lib/realtime'
import { GuestsTodayTab, OccurrencesTab, PresentTab, SearchTab, type GateActions } from './GateTabs'
import { QrScanner } from './Scanner'

type Result = Schema<'AccessResultDto'>
type Tab = 'presentes' | 'convidados' | 'busca' | 'ocorrencias'

const resultTitle = (r: Result) => (r.authorized ? (r.operation === 'Exit' ? 'Saída registrada' : 'Entrada autorizada') : 'Acesso negado')

function ResultCard({ result }: { result: Result | null }) {
  if (!result) {
    return (
      <div className="flex flex-col items-center justify-center rounded-(--radius-card) border border-dashed border-line px-6 py-10 text-center">
        <ScanLine className="size-8 text-muted" />
        <p className="mt-3 text-sm font-medium text-ink">Aguardando leitura</p>
        <p className="mt-1 text-[13px] text-muted">Leia o QR Code do cartão digital ou digite o código de 8 dígitos exibido no aplicativo.</p>
      </div>
    )
  }
  const ok = result.authorized
  return (
    <div role="status" aria-live="assertive" className={cn('animate-pop rounded-(--radius-card) border-2 p-5', ok ? 'border-success bg-success-soft' : 'border-danger bg-danger-soft')}>
      <div className={cn('flex items-center gap-3', ok ? 'text-success' : 'text-danger')}>
        {ok ? <CheckCircle2 className="size-9 shrink-0" /> : <XCircle className="size-9 shrink-0" />}
        <div>
          <p className="text-2xl leading-tight font-semibold tracking-tight">{resultTitle(result)}</p>
          {!ok && <p className="mt-0.5 text-sm font-medium">{result.message}</p>}
        </div>
      </div>
      <div className="mt-4 flex items-center gap-4 rounded-xl bg-surface/80 p-3">
        <Avatar fileId={result.photoFileId} name={result.personName} size={72} />
        <div className="min-w-0 flex-1">
          <p className="truncate text-lg font-semibold text-ink">{result.personName ?? 'Pessoa não identificada'}</p>
          <div className="mt-1 flex flex-wrap items-center gap-2 text-[13px] text-muted">
            {result.personType && <StatusBadge dict={labels.personType} value={result.personType} />}
            {result.operation && <StatusBadge dict={labels.accessOperation} value={result.operation} />}
            {result.responsibleName && <span>Responsável: <span className="text-ink">{result.responsibleName}</span></span>}
          </div>
        </div>
        <p className="tabular shrink-0 text-right text-sm text-muted">{fmt.time(result.at)}</p>
      </div>
    </div>
  )
}

export default function Gate() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [result, setResult] = useState<Result | null>(null)
  const [recent, setRecent] = useState<Result[]>([])
  const [code, setCode] = useState('')
  const [tab, setTab] = useState<Tab>('presentes')
  // Trava de leitura: ativa durante a requisição e por ~2,5 s após o resultado, para o mesmo código não ser enviado duas vezes.
  const [paused, setPaused] = useState(false)
  const lock = useRef(false)
  const lastScan = useRef<{ code: string; at: number } | null>(null)
  const release = useRef<number | undefined>(undefined)
  useEffect(() => () => window.clearTimeout(release.current), [])

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['/api/portaria/ocupacao'] })
    void queryClient.invalidateQueries({ queryKey: ['/api/portaria/presentes'] })
    void queryClient.invalidateQueries({ queryKey: ['/api/convidados'] })
  }
  useRealtime('occupancyChanged', refresh)
  const occupancy = useQuery({ queryKey: ['/api/portaria/ocupacao'], queryFn: () => api.get<Schema<'OccupancyDto'>>('/api/portaria/ocupacao'), refetchInterval: 60_000 })

  const show = (value: Result) => { setResult(value); setRecent((list) => [value, ...list].slice(0, 8)); refresh() }
  const announce = (value: Result) => {
    show(value)
    if (value.authorized) toast.success(`${value.personName ?? 'Pessoa'}: ${value.message}`); else toast.error(value.message)
  }

  const validate = useMutation({
    mutationFn: (value: string) => api.post<Result>('/api/portaria/validar', { code: value, device: 'Web' }),
    onSuccess: show,
    onError: toast.error,
    onSettled: () => { release.current = window.setTimeout(() => { lock.current = false; setPaused(false) }, 2500) },
  })
  const submit = (value: string) => {
    window.clearTimeout(release.current)
    lock.current = true
    setPaused(true)
    validate.mutate(value)
  }
  const onScan = (value: string) => {
    const last = lastScan.current
    if (lock.current || (last && last.code === value && Date.now() - last.at < 15_000)) return
    lastScan.current = { code: value, at: Date.now() }
    submit(value)
  }
  const onManualCode = (event: FormEvent) => {
    event.preventDefault()
    const digits = code.replace(/\D/g, '')
    if (digits.length !== 8) { toast.error('Informe os 8 dígitos do código exibido no aplicativo.'); return }
    if (validate.isPending) return
    setCode('')
    submit(digits)
  }

  const manual = useMutation({ mutationFn: (body: Schema<'ManualAccessRequest'>) => api.post<Result>('/api/portaria/manual', body), onSuccess: announce, onError: toast.error })
  const guestAccess = useMutation({
    mutationFn: ({ guestId, operation }: { guestId: string; operation: 'entrada' | 'saida' }) => api.post<Result>(`/api/portaria/convidados/${guestId}/${operation}`),
    onSuccess: announce, onError: toast.error,
  })
  const actions: GateActions = {
    busy: manual.isPending || guestAccess.isPending,
    guest: (guestId, operation) => guestAccess.mutate({ guestId, operation }),
    manual: async (personType, personId, name, operation) => {
      const answer = await confirm({
        title: `Registrar ${operation === 'Entry' ? 'entrada' : 'saída'} manual de ${name}?`, reason: 'required', reasonLabel: 'Justificativa', confirmLabel: 'Registrar',
        message: 'Use o registro manual somente em situações excepcionais (por exemplo, celular sem bateria). A justificativa fica gravada no histórico de acessos e na auditoria.',
      })
      if (!answer) return
      if (answer.reason.length < 5) { toast.error('A justificativa deve ter pelo menos 5 caracteres.'); return }
      manual.mutate({ personType, personId, operation, justification: answer.reason })
    },
  }

  const peaks = occupancy.data?.peakHours ?? []
  const firstHour = peaks.length > 0 ? Math.min(...peaks.map((p) => p.hour)) : 0
  const lastHour = peaks.length > 0 ? Math.max(...peaks.map((p) => p.hour)) : 0
  const chart = peaks.length === 0 ? [] : Array.from({ length: lastHour - firstHour + 1 }, (_, i) => ({ hour: `${firstHour + i}h`, entries: peaks.find((p) => p.hour === firstHour + i)?.entries ?? 0 }))
  const showOccurrences = can('occurrences.manage', 'occurrences.view')

  return (
    <>
      <PageHeader title="Portaria" subtitle="Validação de acesso, ocupação do clube e registros excepcionais."
        actions={can('gate.history') && <Link to="/portaria/historico"><Button icon={History}>Histórico de acessos</Button></Link>} />
      <div className="grid items-start gap-6 xl:grid-cols-5">
        <div className="space-y-6 xl:col-span-2">
          <Card>
            <CardTitle icon={ScanLine} title="Validar acesso" description="Cada QR Code vale para uma única entrada ou saída." />
            <div className="space-y-4">
              <ResultCard result={result} />
              <QrScanner onCode={onScan} paused={paused} />
              <form className="flex items-center gap-2" onSubmit={onManualCode}>
                <span className="relative block flex-1">
                  <KeyRound className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted" />
                  <Input autoFocus inputMode="numeric" maxLength={9} placeholder="Código de 8 dígitos" aria-label="Código numérico de 8 dígitos" className="tabular h-11 pl-9 text-base tracking-[0.2em]"
                    value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 8))} />
                </span>
                <Button type="submit" variant="primary" className="h-11" loading={validate.isPending}>Validar</Button>
              </form>
            </div>
          </Card>

          <Card padded={false}>
            <div className="px-5 pt-5"><CardTitle title="Últimas validações" description="Resultados desta sessão, conforme devolvidos pelo servidor." /></div>
            {recent.length === 0 ? <EmptyState icon={ScanLine} title="Nenhuma validação nesta sessão" /> : (
              <ul className="divide-y divide-line/70 border-t border-line">
                {recent.map((r, index) => (
                  <li key={`${r.at}-${index}`} className="flex items-center gap-3 px-5 py-2.5 text-sm">
                    {r.authorized ? <CheckCircle2 className="size-4 shrink-0 text-success" /> : <XCircle className="size-4 shrink-0 text-danger" />}
                    <span className="min-w-0 flex-1">
                      <span className="block truncate font-medium text-ink">{r.personName ?? 'Não identificado'}</span>
                      <span className={cn('block truncate text-xs', r.authorized ? 'text-muted' : 'text-danger')}>{r.authorized ? resultTitle(r) : r.message}</span>
                    </span>
                    <span className="tabular shrink-0 text-xs text-muted">{fmt.time(r.at)}</span>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>

        <div className="space-y-6 xl:col-span-3">
          {occupancy.error ? <Card><ErrorState error={occupancy.error} onRetry={() => occupancy.refetch()} /></Card> : (
            <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
              <StatCard label="No clube agora" value={occupancy.data ? fmt.number(occupancy.data.total) : '…'} icon={DoorOpen} tone="success" />
              <StatCard label="Associados" value={occupancy.data ? fmt.number(occupancy.data.members) : '…'} icon={Users} />
              <StatCard label="Dependentes" value={occupancy.data ? fmt.number(occupancy.data.dependents) : '…'} icon={UserCheck} tone="info" />
              <StatCard label="Convidados" value={occupancy.data ? fmt.number(occupancy.data.guests) : '…'} icon={UserPlus} tone="warning" />
            </div>
          )}

          <div>
            <Tabs<Tab> value={tab} onChange={setTab} tabs={[
              { key: 'presentes', label: 'Presentes', count: occupancy.data?.total },
              { key: 'convidados', label: 'Convidados de hoje' },
              { key: 'busca', label: 'Busca manual' },
              ...(showOccurrences ? [{ key: 'ocorrencias' as const, label: 'Ocorrências' }] : []),
            ]} />
            <Card padded={false}>
              {tab === 'presentes' && <PresentTab actions={actions} />}
              {tab === 'convidados' && <GuestsTodayTab actions={actions} />}
              {tab === 'busca' && <SearchTab actions={actions} />}
              {tab === 'ocorrencias' && showOccurrences && <OccurrencesTab />}
            </Card>
          </div>

          <Card>
            <CardTitle icon={BarChart3} title="Horários de maior movimentação" description="Entradas autorizadas por hora nos últimos 30 dias." />
            {chart.length === 0 ? <p className="py-6 text-center text-[13px] text-muted">Ainda não há entradas registradas no período.</p> : (
              <div className="h-48">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={chart} margin={{ top: 4, right: 4, bottom: 0, left: -20 }}>
                    <CartesianGrid vertical={false} stroke="var(--color-line)" />
                    <XAxis dataKey="hour" tickLine={false} axisLine={false} tick={{ fontSize: 11, fill: 'var(--color-muted)' }} />
                    <YAxis allowDecimals={false} tickLine={false} axisLine={false} tick={{ fontSize: 11, fill: 'var(--color-muted)' }} />
                    <Tooltip cursor={{ fill: 'rgb(0 0 0 / 0.04)' }} formatter={(value) => [`${value} entrada(s)`, 'Movimento']}
                      contentStyle={{ borderRadius: 10, border: '1px solid var(--color-line)', fontSize: 12 }} />
                    <Bar dataKey="entries" fill="var(--color-brand-500)" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            )}
          </Card>
        </div>
      </div>
    </>
  )
}
