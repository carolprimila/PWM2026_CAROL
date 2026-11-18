import { ChevronLeft, ChevronRight, LayoutGrid, RefreshCw } from 'lucide-react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQueries, useQueryClient } from '@tanstack/react-query'
import { Button, Card, cn, EmptyState, IconButton, Input, PageHeader, QueryBoundary, Select, Spinner } from '@/components/ui'
import { errorMessage } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { dayKey, fetchDay, SlotLegend, slotClass, spaceGroups, useAvailabilityRealtime, useSpaces } from './availability'

/** Quadro do dia: uma linha por espaço reservado por horário, colunas por hora, atualizado em tempo real. */
export default function Board() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const [params, setParams] = useSearchParams()
  const date = params.get('date') || dates.today()
  const type = params.get('tipo') ?? ''
  const setParam = (key: string, value: string) => setParams((current) => { if (value) current.set(key, value); else current.delete(key); return current }, { replace: true })
  const manage = can('reservations.manage')

  const spaces = useSpaces()
  const hourly = (spaces.data ?? []).filter((s) => s.isReservable && s.isActive && s.bookingMode === 'Hourly')
  const rows = hourly.filter((s) => !type || s.type === type)
  const days = useQueries({ queries: rows.map((s) => ({ queryKey: dayKey(s.id, date), queryFn: () => fetchDay(s.id, date) })) })
  useAvailabilityRealtime()

  const open = days.flatMap((d) => (d.data?.isOpen && d.data.openMinute != null && d.data.closeMinute != null ? [{ from: d.data.openMinute, to: d.data.closeMinute }] : []))
  const min = open.length > 0 ? Math.floor(Math.min(...open.map((o) => o.from)) / 60) * 60 : 480
  const max = open.length > 0 ? Math.ceil(Math.max(...open.map((o) => o.to)) / 60) * 60 : 1320
  const span = max - min
  const hours = Array.from({ length: span / 60 }, (_, i) => min + i * 60)
  const percent = (minutes: number) => `${(minutes / span) * 100}%`
  const fetching = days.some((d) => d.isFetching)

  return (
    <>
      <PageHeader title="Disponibilidade" subtitle="Situação de cada horário dos espaços no dia. Clique em um horário disponível para iniciar uma reserva."
        actions={<Button icon={RefreshCw} loading={fetching} onClick={() => void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })}>Atualizar</Button>} />
      <Card padded={false}>
        <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-3">
          <IconButton icon={ChevronLeft} label="Dia anterior" onClick={() => setParam('date', dates.addDays(date, -1))} />
          <Input type="date" className="w-44" value={date} onChange={(e) => setParam('date', e.target.value)} />
          <IconButton icon={ChevronRight} label="Próximo dia" onClick={() => setParam('date', dates.addDays(date, 1))} />
          <Button size="sm" onClick={() => setParam('date', '')}>Hoje</Button>
          <span className="ml-1 text-sm text-muted first-letter:uppercase">{fmt.longDate(new Date(`${date}T12:00:00`))}</span>
          <div className="ml-auto w-52">
            <Select value={type} onChange={(e) => setParam('tipo', e.target.value)}>
              <option value="">Todos os tipos</option>
              {spaceGroups.filter((g) => hourly.some((s) => s.type === g.type)).map((g) => <option key={g.type} value={g.type}>{g.title}</option>)}
            </Select>
          </div>
        </div>

        <QueryBoundary query={spaces}>
          {() => rows.length === 0 ? <EmptyState icon={LayoutGrid} title="Nenhum espaço reservado por horário" description="Cadastre espaços reserváveis por horário em Espaços." /> : (
            <div className="overflow-x-auto">
              <div style={{ minWidth: 220 + hours.length * 64 }}>
                <div className="flex border-b border-line text-xs font-medium text-muted">
                  <div className="w-[220px] shrink-0 px-4 py-2.5 uppercase tracking-wide">Espaço</div>
                  <div className="flex flex-1">{hours.map((h) => <div key={h} className="tabular border-l border-line/70 py-2.5 pl-1.5" style={{ width: percent(60) }}>{fmt.minutes(h)}</div>)}</div>
                </div>
                {rows.map((space, index) => {
                  const day = days[index]
                  return (
                    <div key={space.id} className="flex items-stretch border-b border-line/70 last:border-0">
                      <div className="w-[220px] shrink-0 px-4 py-2.5">
                        <p className="truncate text-sm font-medium text-ink">{space.name}</p>
                        <p className="truncate text-xs text-muted">{space.subtype || labels.spaceType[space.type].label}{space.capacity != null ? ` · ${space.capacity} pessoas` : ''}</p>
                      </div>
                      <div className="relative min-h-12 flex-1">
                        {hours.map((h) => <span key={h} className="absolute inset-y-0 border-l border-line/50" style={{ left: percent(h - min) }} />)}
                        {day.isLoading ? <div className="absolute inset-0 flex items-center justify-center"><Spinner className="size-4" /></div>
                          : day.error || !day.data ? (
                            <div className="absolute inset-0 flex items-center gap-2 px-3 text-[13px] text-danger">
                              {errorMessage(day.error)}<button type="button" className="font-medium underline" onClick={() => void day.refetch()}>Tentar novamente</button>
                            </div>
                          )
                          : !day.data.isOpen ? <div className="absolute inset-1 flex items-center justify-center rounded-lg bg-black/5 text-[13px] text-muted">{day.data.closedReason ?? 'Fechado'}</div>
                          : day.data.slots.map((slot) => {
                            const clickable = manage && slot.state === 'Available'
                            return (
                              <div key={slot.startMinute} className="absolute inset-y-1 px-0.5" style={{ left: percent(slot.startMinute - min), width: percent(slot.endMinute - slot.startMinute) }}>
                                <button type="button" disabled={!clickable}
                                  title={`${fmt.minutes(slot.startMinute)}–${fmt.minutes(slot.endMinute)} · ${labels.slotState[slot.state].label}${slot.reason ? ` · ${slot.reason}` : ''}`}
                                  onClick={() => navigate(`/reservas/nova?spaceId=${space.id}&date=${date}&start=${slot.startMinute}`)}
                                  className={cn('tabular size-full overflow-hidden rounded-md border text-[11px] font-medium transition-colors', slotClass[slot.state], clickable ? 'cursor-pointer hover:border-success hover:brightness-95' : 'cursor-default')}>
                                  {slot.state === 'Available' ? fmt.minutes(slot.startMinute) : slot.state === 'Reserved' ? 'Reservado' : slot.state === 'Blocked' ? 'Bloqueado' : ''}
                                </button>
                              </div>
                            )
                          })}
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </QueryBoundary>
        <div className="border-t border-line px-4 py-3"><SlotLegend selected={false} /></div>
      </Card>
    </>
  )
}
