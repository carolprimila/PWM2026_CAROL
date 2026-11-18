import { AlertTriangle, ChevronLeft, ChevronRight } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Alert, Badge, Button, Card, cn, ErrorState, IconButton, Info, LoadingState, Modal, PageHeader, Select, Spinner } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels, options, type Tone } from '@/lib/labels'
import { useRealtime } from '@/lib/realtime'

type Item = Schema<'CalendarItemDto'>
type ItemType = Schema<'CalendarItemType'>
type View = 'day' | 'week' | 'month'

const HOUR = 44
const weekShort = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb']
const reservationTypes: ItemType[] = ['Reservation', 'HallRental', 'Sports']
const liveStatuses = ['Pending', 'Approved', 'Suspended', 'Completed'] as const

const toneClass: Record<Tone, string> = {
  neutral: 'border-l-muted bg-black/5', brand: 'border-l-brand-500 bg-brand-50', success: 'border-l-success bg-success-soft',
  warning: 'border-l-warning bg-warning-soft', danger: 'border-l-danger bg-danger-soft', info: 'border-l-info bg-info-soft',
}
const dotClass: Record<Tone, string> = { neutral: 'bg-muted', brand: 'bg-brand-500', success: 'bg-success', warning: 'bg-warning', danger: 'bg-danger', info: 'bg-info' }

const dayStart = (day: string) => new Date(`${day}T00:00:00`).getTime()
const weekday = (day: string) => new Date(`${day}T12:00:00`).getDay()
const typeOf = (item: Item) => labels.calendarType[item.type]

function statusText(item: Item) {
  if (!item.status) return null
  const dict = item.type === 'Event' ? labels.eventStatus : item.type === 'Assembly' ? labels.assemblyStatus : item.type === 'Maintenance' ? labels.maintenanceStatus
    : item.type === 'Block' ? labels.blockKind : item.type === 'Holiday' ? labels.holidayScope : labels.reservationStatus
  return (dict as Record<string, { label: string }>)[item.status]?.label ?? item.status
}

type Placed = { item: Item; start: number; end: number; col: number; cols: number }

/** Separa os itens de um dia em "dia inteiro" e com horário, distribuindo em colunas os que se sobrepõem. */
function layoutDay(items: Item[], day: string): { allDay: Item[]; timed: Placed[] } {
  const from = dayStart(day)
  const to = dayStart(dates.addDays(day, 1))
  const allDay: Item[] = []
  const timed: Placed[] = []
  for (const item of items) {
    const start = Math.max(new Date(item.startAt).getTime(), from)
    const end = Math.min(new Date(item.endAt).getTime(), to)
    if (end <= start) continue
    if (item.allDay || (start === from && end === to)) allDay.push(item)
    else timed.push({ item, start: (start - from) / 60_000, end: (end - from) / 60_000, col: 0, cols: 1 })
  }
  timed.sort((a, b) => a.start - b.start || b.end - a.end)
  let cluster: Placed[] = []
  let columnEnds: number[] = []
  let clusterEnd = -1
  const flush = () => { for (const placed of cluster) placed.cols = columnEnds.length; cluster = []; columnEnds = []; clusterEnd = -1 }
  for (const placed of timed) {
    if (cluster.length > 0 && placed.start >= clusterEnd) flush()
    let col = columnEnds.findIndex((end) => end <= placed.start)
    if (col < 0) { col = columnEnds.length; columnEnds.push(placed.end) } else columnEnds[col] = placed.end
    placed.col = col
    cluster.push(placed)
    clusterEnd = Math.max(clusterEnd, placed.end)
  }
  flush()
  return { allDay, timed }
}

function Chip({ item, onOpen, time }: { item: Item; onOpen: (item: Item) => void; time?: boolean }) {
  return (
    <button type="button" onClick={() => onOpen(item)} title={item.hasConflict ? item.conflictReason ?? 'Conflito' : `${item.title}${item.spaceName ? ` · ${item.spaceName}` : ''}`}
      className={cn('flex w-full items-center gap-1 truncate rounded border-l-[3px] px-1.5 py-0.5 text-left text-xs text-ink transition hover:brightness-95', toneClass[typeOf(item).tone], item.hasConflict && 'ring-1 ring-danger')}>
      {item.hasConflict && <AlertTriangle className="size-3 shrink-0 text-danger" />}
      {time && !item.allDay && <span className="tabular shrink-0 text-muted">{fmt.time(item.startAt)}</span>}
      <span className="truncate">{item.title}</span>
    </button>
  )
}

export default function CalendarPage() {
  const { can } = useAuth()
  const today = dates.today()
  const [view, setView] = useState<View>('week')
  const [anchor, setAnchor] = useState(today)
  const [spaceId, setSpaceId] = useState('')
  const [status, setStatus] = useState('')
  const [types, setTypes] = useState<ItemType[]>([])
  const [onlyConflicts, setOnlyConflicts] = useState(false)
  const [selected, setSelected] = useState<Item | null>(null)
  const scroller = useRef<HTMLDivElement>(null)

  const monthFirst = `${anchor.slice(0, 7)}-01`
  const start = view === 'day' ? anchor : view === 'week' ? dates.addDays(anchor, -weekday(anchor)) : dates.addDays(monthFirst, -weekday(monthFirst))
  const monthDays = new Date(Number(anchor.slice(0, 4)), Number(anchor.slice(5, 7)), 0).getDate()
  const count = view === 'day' ? 1 : view === 'week' ? 7 : Math.ceil((weekday(monthFirst) + monthDays) / 7) * 7
  const days = useMemo(() => Array.from({ length: count }, (_, i) => dates.addDays(start, i)), [start, count])

  const filters = { from: dates.startOfDay(start), to: dates.startOfDay(dates.addDays(start, count)), spaceId: spaceId || undefined, types, status: status || undefined }
  const calendar = useQuery({ queryKey: ['/api/calendario', filters], queryFn: () => api.get<Item[]>('/api/calendario', filters), placeholderData: keepPreviousData })
  const spaces = useQuery({ queryKey: ['/api/espacos', {}], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })
  useRealtime('availabilityChanged', () => void calendar.refetch())

  const all = calendar.data ?? []
  const conflicts = all.filter((item) => item.hasConflict).length
  const items = onlyConflicts ? all.filter((item) => item.hasConflict) : all
  const byDay = useMemo(() => new Map(days.map((day) => [day, layoutDay(items, day)])), [items, days])

  useEffect(() => { if (view !== 'month' && scroller.current) scroller.current.scrollTop = 7 * HOUR }, [view, calendar.isLoading])

  const move = (direction: number) => {
    if (view === 'month') setAnchor(dates.toDay(new Date(Number(anchor.slice(0, 4)), Number(anchor.slice(5, 7)) - 1 + direction, 1)))
    else setAnchor(dates.addDays(anchor, direction * (view === 'week' ? 7 : 1)))
  }
  const openDay = (day: string) => { setAnchor(day); setView('day') }
  const heading = view === 'day' ? fmt.longDate(new Date(`${anchor}T12:00:00`))
    : view === 'week' ? `${fmt.day(start)} – ${fmt.day(dates.addDays(start, 6))}`
    : new Date(`${monthFirst}T12:00:00`).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  const columns = { gridTemplateColumns: `56px repeat(${days.length}, minmax(0, 1fr))` }
  const now = new Date()
  const nowMinutes = now.getHours() * 60 + now.getMinutes()

  return (
    <>
      <PageHeader title="Calendário" subtitle="Reservas, locações, eventos, assembleias, manutenções, bloqueios e feriados em um só lugar." />
      <Card padded={false}>
        <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-3">
          <div className="inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5">
            {([['day', 'Dia'], ['week', 'Semana'], ['month', 'Mês']] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => setView(key)} className={cn('rounded-lg px-3 py-1.5 text-[13px] font-medium transition-colors', view === key ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink')}>{label}</button>
            ))}
          </div>
          <IconButton icon={ChevronLeft} label="Anterior" onClick={() => move(-1)} />
          <Button size="sm" onClick={() => setAnchor(today)}>Hoje</Button>
          <IconButton icon={ChevronRight} label="Próximo" onClick={() => move(1)} />
          <h2 className="ml-1 text-[15px] font-semibold text-ink first-letter:uppercase">{heading}</h2>
          {calendar.isFetching && <Spinner className="size-4" />}
          <div className="ml-auto flex flex-wrap items-center gap-2">
            <button type="button" onClick={() => setOnlyConflicts(!onlyConflicts)} disabled={conflicts === 0 && !onlyConflicts} title="Mostrar somente itens em conflito"
              className={cn('inline-flex h-8 items-center gap-1.5 rounded-full border px-3 text-[13px] font-medium transition-colors disabled:opacity-60',
                onlyConflicts ? 'border-danger bg-danger text-white' : conflicts > 0 ? 'border-danger/30 bg-danger-soft text-danger' : 'border-line text-muted')}>
              <AlertTriangle className="size-3.5" />Conflitos<span className="tabular">{conflicts}</span>
            </button>
            <div className="w-48"><Select className="h-8" value={spaceId} onChange={(e) => setSpaceId(e.target.value)}><option value="">Todos os espaços</option>{spaces.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select></div>
            <div className="w-52">
              <Select className="h-8" value={status} onChange={(e) => setStatus(e.target.value)} title="Filtra as reservas pela situação">
                <option value="">Reservas: todas as situações</option>
                {options(labels.reservationStatus).filter((o) => (liveStatuses as readonly string[]).includes(o.value)).map((o) => <option key={o.value} value={o.value}>Reservas: {o.label.toLowerCase()}</option>)}
              </Select>
            </div>
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-1.5 border-b border-line px-4 py-2.5">
          <span className="mr-1 text-xs text-muted">Atividades:</span>
          {options(labels.calendarType).map((o) => {
            const active = types.includes(o.value)
            return (
              <button key={o.value} type="button" aria-pressed={active} onClick={() => setTypes(active ? types.filter((t) => t !== o.value) : [...types, o.value])}
                className={cn('inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium transition-colors', active ? 'border-brand-500 bg-brand-50 text-brand-700' : 'border-line text-muted hover:text-ink')}>
                <span className={cn('size-2 rounded-full', dotClass[labels.calendarType[o.value].tone])} />{o.label}
              </button>
            )
          })}
          {types.length > 0 && <button type="button" className="ml-1 text-xs font-medium text-brand-600 hover:underline" onClick={() => setTypes([])}>Mostrar todas</button>}
        </div>

        {calendar.isLoading ? <LoadingState /> : calendar.error ? <ErrorState error={calendar.error} onRetry={() => calendar.refetch()} /> : view === 'month' ? (
          <div className="grid grid-cols-7">
            {weekShort.map((label) => <div key={label} className="border-b border-line px-2 py-2 text-center text-xs font-medium text-muted uppercase">{label}</div>)}
            {days.map((day, index) => {
              const layout = byDay.get(day)!
              const list = [...layout.allDay, ...layout.timed.map((p) => p.item)]
              const outside = day.slice(0, 7) !== anchor.slice(0, 7)
              return (
                <div key={day} className={cn('min-h-28 border-b border-line/70 p-1.5', index % 7 !== 0 && 'border-l', outside && 'bg-canvas/60')}>
                  <button type="button" onClick={() => openDay(day)} title="Ver o dia"
                    className={cn('tabular mb-1 flex size-6 items-center justify-center rounded-full text-xs font-medium hover:bg-black/5', day === today ? 'bg-brand-600 text-white hover:bg-brand-700' : outside ? 'text-muted/70' : 'text-ink')}>
                    {Number(day.slice(8))}
                  </button>
                  <div className="space-y-0.5">
                    {list.slice(0, 3).map((item) => <Chip key={item.id} item={item} onOpen={setSelected} time />)}
                    {list.length > 3 && <button type="button" onClick={() => openDay(day)} className="px-1.5 text-xs font-medium text-brand-600 hover:underline">+{list.length - 3} mais</button>}
                  </div>
                </div>
              )
            })}
          </div>
        ) : (
          <div ref={scroller} className="max-h-[680px] overflow-y-auto">
            <div className="sticky top-0 z-20 bg-surface">
              <div className="grid border-b border-line" style={columns}>
                <div />
                {days.map((day) => (
                  <button key={day} type="button" onClick={() => openDay(day)} className="border-l border-line/70 px-2 py-2 text-center hover:bg-canvas/70">
                    <span className="block text-xs text-muted uppercase">{weekShort[weekday(day)]}</span>
                    <span className={cn('tabular mx-auto mt-0.5 flex size-7 items-center justify-center rounded-full text-sm font-semibold', day === today ? 'bg-brand-600 text-white' : 'text-ink')}>{Number(day.slice(8))}</span>
                  </button>
                ))}
              </div>
              <div className="grid border-b border-line" style={columns}>
                <div className="px-1.5 py-1.5 text-right text-[11px] leading-tight text-muted">Dia inteiro</div>
                {days.map((day) => (
                  <div key={day} className="min-h-8 space-y-0.5 border-l border-line/70 p-1">
                    {byDay.get(day)!.allDay.map((item) => <Chip key={item.id} item={item} onOpen={setSelected} />)}
                  </div>
                ))}
              </div>
            </div>
            <div className="grid" style={{ ...columns, height: 24 * HOUR }}>
              <div className="relative">
                {Array.from({ length: 24 }, (_, h) => <span key={h} className="tabular absolute right-2 -translate-y-1/2 text-[11px] text-muted" style={{ top: h * HOUR }}>{h === 0 ? '' : `${String(h).padStart(2, '0')}:00`}</span>)}
              </div>
              {days.map((day) => (
                <div key={day} className={cn('relative border-l border-line/70', day === today && 'bg-brand-50/30')}>
                  {Array.from({ length: 24 }, (_, h) => <span key={h} className="absolute inset-x-0 border-t border-line/50" style={{ top: h * HOUR }} />)}
                  {day === today && <span className="absolute inset-x-0 z-10 border-t-2 border-danger" style={{ top: (nowMinutes / 60) * HOUR }} />}
                  {byDay.get(day)!.timed.map(({ item, start: from, end, col, cols }) => (
                    <div key={item.id} className="absolute p-px" style={{ top: (from / 60) * HOUR, height: Math.max(((end - from) / 60) * HOUR, 20), left: `${(col / cols) * 100}%`, width: `${100 / cols}%` }}>
                      <button type="button" onClick={() => setSelected(item)} title={item.hasConflict ? item.conflictReason ?? 'Conflito' : `${item.title}${item.spaceName ? ` · ${item.spaceName}` : ''}`}
                        className={cn('flex size-full flex-col overflow-hidden rounded border-l-[3px] px-1.5 py-0.5 text-left text-xs text-ink transition hover:brightness-95', toneClass[typeOf(item).tone], item.hasConflict && 'ring-1 ring-danger')}>
                        <span className="flex items-center gap-1 truncate font-medium">{item.hasConflict && <AlertTriangle className="size-3 shrink-0 text-danger" />}<span className="truncate">{item.title}</span></span>
                        <span className="tabular truncate text-[11px] text-muted">{fmt.time(item.startAt)}–{fmt.time(item.endAt)}{item.spaceName ? ` · ${item.spaceName}` : ''}</span>
                      </button>
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        )}
      </Card>

      {selected && (
        <Modal open onClose={() => setSelected(null)} size="sm" title={selected.title}
          footer={<>
            {reservationTypes.includes(selected.type) && selected.entityId && can('reservations.view') && <Link to={`/reservas/${selected.entityId}`}><Button variant="primary">Abrir reserva</Button></Link>}
            {selected.type === 'Block' && can('spaces.view') && <Link to="/espacos?aba=bloqueios"><Button>Ver bloqueios</Button></Link>}
            {selected.type === 'Holiday' && can('spaces.view') && <Link to="/espacos?aba=feriados"><Button>Ver feriados</Button></Link>}
            <Button onClick={() => setSelected(null)}>Fechar</Button>
          </>}>
          <div className="space-y-4">
            {selected.hasConflict && <Alert tone="danger" title="Conflito de agenda">{selected.conflictReason}</Alert>}
            <dl className="grid grid-cols-2 gap-4">
              <Info label="Atividade"><Badge tone={typeOf(selected).tone}>{typeOf(selected).label}</Badge></Info>
              <Info label="Situação">{statusText(selected)}</Info>
              <Info label="Início">{selected.allDay ? fmt.date(selected.startAt) : fmt.dateTime(selected.startAt)}</Info>
              <Info label="Término">{selected.allDay ? 'Dia inteiro' : fmt.dateTime(selected.endAt)}</Info>
              <Info label="Espaço" className="col-span-2">{selected.spaceName}</Info>
            </dl>
          </div>
        </Modal>
      )}
    </>
  )
}
