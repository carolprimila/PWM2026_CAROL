/* Disponibilidade de espaços: consulta, atualização em tempo real e seletor de horários/período (usado na nova reserva e no reagendamento). */
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, cn, ErrorState, IconButton, Input, LoadingState } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { dates, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { useRealtime } from '@/lib/realtime'

export type Space = Schema<'SpaceDto'>
export type DayAvailability = Schema<'DayAvailabilityDto'>
type Slot = Schema<'SlotDto'>
type SlotState = Schema<'SlotState'>
type Range = { startAt: string; endAt: string }

export const spaceGroups: { type: Schema<'SpaceType'>; title: string }[] = [
  { type: 'Court', title: 'Quadras' }, { type: 'Kiosk', title: 'Quiosques' }, { type: 'PartyHall', title: 'Salão de festas' },
  { type: 'Pool', title: 'Piscinas' }, { type: 'Other', title: 'Outros' },
]

/** Espaços ativos do clube. */
export const useSpaces = () => useQuery({ queryKey: ['/api/espacos', {}], queryFn: () => api.get<Space[]>('/api/espacos') })

export const dayKey = (spaceId: string, date: string) => ['disponibilidade', spaceId, 'dia', date]
export const fetchDay = (spaceId: string, date: string) => api.get<DayAvailability>(`/api/espacos/${spaceId}/disponibilidade`, { date })

/** Recarrega a disponibilidade quando o servidor avisa que ela mudou (reservas, bloqueios, feriados). */
export function useAvailabilityRealtime() {
  const queryClient = useQueryClient()
  useRealtime('availabilityChanged', (payload) => {
    const spaceId = (payload as { spaceId?: string | null } | null)?.spaceId
    void queryClient.invalidateQueries({ queryKey: spaceId ? ['disponibilidade', spaceId] : ['disponibilidade'] })
  })
}

/** "12/03/2026 08:00 – 10:00" ou "12/03/2026 00:00 – 16/03/2026 00:00" quando atravessa dias. */
export function period(startAt: string, endAt: string) {
  const sameDay = dates.toDay(new Date(startAt)) === dates.toDay(new Date(endAt))
  return sameDay ? `${fmt.dateTime(startAt)} – ${fmt.time(endAt)}` : `${fmt.dateTime(startAt)} – ${fmt.dateTime(endAt)}`
}

export const slotClass: Record<SlotState, string> = {
  Available: 'border-success/30 bg-success-soft text-success',
  Reserved: 'border-info/20 bg-info-soft text-info',
  Blocked: 'border-danger/20 bg-danger-soft text-danger',
  Past: 'border-transparent bg-black/5 text-muted',
  Closed: 'border-transparent bg-black/5 text-muted',
}

export function SlotLegend({ selected = true }: { selected?: boolean }) {
  return (
    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted">
      {selected && <span className="flex items-center gap-1.5"><span className="size-3 rounded border border-brand-600 bg-brand-600" />Selecionado</span>}
      {(Object.keys(slotClass) as SlotState[]).map((state) => (
        <span key={state} className="flex items-center gap-1.5"><span className={cn('size-3 rounded border', slotClass[state])} />{labels.slotState[state].label}</span>
      ))}
    </div>
  )
}

const noon = (day: string) => new Date(`${day}T12:00:00`)

/**
 * Seletor de disponibilidade. Espaços por horário: grade de intervalos do dia (seleção múltipla).
 * Espaços por período: calendário do mês para escolher a data de início do período fixo.
 * `own`: intervalos da própria reserva (reagendamento) — continuam selecionáveis mesmo aparecendo como reservados.
 */
export function AvailabilityPicker({ space, date, onDate, minutes, onMinutes, own }: {
  space: Space; date: string; onDate: (date: string) => void; minutes: number[]; onMinutes: (minutes: number[]) => void; own?: Range[]
}) {
  useAvailabilityRealtime()
  return space.bookingMode === 'Hourly'
    ? <HourlyPicker space={space} date={date} onDate={onDate} minutes={minutes} onMinutes={onMinutes} own={own} />
    : <PeriodPicker space={space} date={date} onDate={onDate} />
}

function HourlyPicker({ space, date, onDate, minutes, onMinutes, own }: {
  space: Space; date: string; onDate: (date: string) => void; minutes: number[]; onMinutes: (minutes: number[]) => void; own?: Range[]
}) {
  const today = dates.today()
  const day = useQuery({ queryKey: dayKey(space.id, date), queryFn: () => fetchDay(space.id, date), enabled: !!date })
  const isOwn = (slot: Slot) => !!own?.some((r) => new Date(r.startAt) <= new Date(slot.startAt) && new Date(r.endAt) >= new Date(slot.endAt))
  const selectable = (slot: Slot) => slot.state === 'Available' || (slot.state === 'Reserved' && isOwn(slot))

  // Se a disponibilidade mudou (tempo real ou conflito), descarta da seleção o que deixou de estar livre.
  useEffect(() => {
    if (!day.data) return
    const valid = minutes.filter((m) => day.data.slots.some((s) => s.startMinute === m && selectable(s)))
    if (valid.length !== minutes.length) onMinutes(valid)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [day.data])

  const change = (value: string) => { if (value && value !== date) { onMinutes([]); onDate(value) } }
  const toggle = (minute: number) => onMinutes(minutes.includes(minute) ? minutes.filter((m) => m !== minute) : [...minutes, minute].sort((a, b) => a - b))

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <IconButton icon={ChevronLeft} label="Dia anterior" disabled={!date || date <= today} onClick={() => change(dates.addDays(date, -1))} />
        <Input type="date" className="w-44" min={today} value={date} onChange={(e) => change(e.target.value)} />
        <IconButton icon={ChevronRight} label="Próximo dia" disabled={!date} onClick={() => change(dates.addDays(date, 1))} />
        {date && <span className="ml-1 text-sm text-muted first-letter:uppercase">{fmt.longDate(noon(date))}</span>}
      </div>
      {!date ? <Alert>Escolha a data para ver os horários.</Alert>
        : day.isLoading ? <LoadingState label="Consultando disponibilidade…" />
        : day.error || !day.data ? <ErrorState error={day.error} onRetry={() => day.refetch()} />
        : !day.data.isOpen ? <Alert tone="warning" title={`${space.name} não funciona nesta data`}>{day.data.closedReason}</Alert>
        : (
          <>
            <div className="grid grid-cols-4 gap-2 sm:grid-cols-6 xl:grid-cols-8">
              {day.data.slots.map((slot) => {
                const selected = minutes.includes(slot.startMinute)
                const enabled = selectable(slot)
                return (
                  <button key={slot.startMinute} type="button" disabled={!enabled} onClick={() => toggle(slot.startMinute)} aria-pressed={selected}
                    title={slot.reason ?? (slot.state === 'Reserved' && enabled ? 'Horário atual desta reserva' : labels.slotState[slot.state].label)}
                    className={cn('tabular rounded-(--radius-control) border px-2 py-2 text-center text-[13px] font-medium transition-colors',
                      selected ? 'border-brand-600 bg-brand-600 text-white' : slotClass[slot.state], enabled ? 'cursor-pointer hover:border-brand-400' : 'cursor-not-allowed opacity-70')}>
                    {fmt.minutes(slot.startMinute)}<span className="block text-[11px] font-normal opacity-80">até {fmt.minutes(slot.endMinute)}</span>
                  </button>
                )
              })}
            </div>
            <SlotLegend />
          </>
        )}
    </div>
  )
}

function PeriodPicker({ space, date, onDate }: { space: Space; date: string; onDate: (date: string) => void }) {
  const [month, setMonth] = useState(() => (date || dates.today()).slice(0, 7))
  const first = noon(`${month}-01`)
  const from = dates.firstOfMonth(first)
  const to = dates.lastOfMonth(first)
  const days = useQuery({
    queryKey: ['disponibilidade', space.id, 'periodo', from, to],
    queryFn: () => api.get<Schema<'PeriodDayDto'>[]>(`/api/espacos/${space.id}/disponibilidade-periodo`, { from, to }),
  })
  const shift = (amount: number) => setMonth(dates.toDay(new Date(first.getFullYear(), first.getMonth() + amount, 1)).slice(0, 7))
  const lastDay = date ? dates.addDays(date, space.fixedDays - 1) : ''

  return (
    <div className="space-y-4">
      <Alert title={`Período fixo de ${space.fixedDays} dia(s)`}>
        Escolha a data de início. A reserva ocupa {space.fixedDays} dia(s) consecutivos, que incluem montagem, preparação, evento, retirada da decoração e a limpeza realizada pelo clube.
      </Alert>
      <div className="flex items-center gap-2">
        <IconButton icon={ChevronLeft} label="Mês anterior" disabled={month <= dates.today().slice(0, 7)} onClick={() => shift(-1)} />
        <span className="w-44 text-center text-sm font-semibold text-ink first-letter:uppercase">{first.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })}</span>
        <IconButton icon={ChevronRight} label="Próximo mês" onClick={() => shift(1)} />
        {date && <span className="ml-2 text-sm text-ink">Período escolhido: <span className="font-medium">{fmt.day(date)} a {fmt.day(lastDay)}</span></span>}
      </div>
      {days.isLoading ? <LoadingState label="Consultando disponibilidade…" />
        : days.error || !days.data ? <ErrorState error={days.error} onRetry={() => days.refetch()} />
        : (
          <>
            <div className="grid max-w-xl grid-cols-7 gap-1.5">
              {['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'].map((d) => <span key={d} className="pb-1 text-center text-xs font-medium text-muted">{d}</span>)}
              {Array.from({ length: first.getDay() }, (_, i) => <span key={`blank-${i}`} />)}
              {days.data.map((d) => {
                const inRange = !!date && d.date >= date && d.date <= lastDay
                const enabled = d.state === 'Available'
                return (
                  <button key={d.date} type="button" disabled={!enabled} onClick={() => onDate(d.date)} aria-pressed={d.date === date}
                    title={d.reason ?? (d.state === 'Reserved' ? 'Período já reservado' : labels.slotState[d.state].label)}
                    className={cn('tabular h-11 rounded-(--radius-control) border text-sm font-medium transition-colors',
                      inRange ? (d.date === date ? 'border-brand-600 bg-brand-600 text-white' : 'border-brand-200 bg-brand-100 text-brand-700') : slotClass[d.state],
                      enabled ? 'cursor-pointer hover:border-brand-400' : 'cursor-not-allowed', !enabled && !inRange && 'opacity-70')}>
                    {Number(d.date.slice(8, 10))}
                  </button>
                )
              })}
            </div>
            <SlotLegend />
          </>
        )}
    </div>
  )
}
