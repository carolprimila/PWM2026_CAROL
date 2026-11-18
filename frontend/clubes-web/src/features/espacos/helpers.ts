import type { Schema } from '@/lib/api'
import { fmt } from '@/lib/format'
import { dayOfWeek, daysOfWeek } from '@/lib/labels'

export type Space = Schema<'SpaceDto'>

export const spaceGroups: { type: Schema<'SpaceType'>; title: string }[] = [
  { type: 'Court', title: 'Quadras' }, { type: 'Kiosk', title: 'Quiosques' }, { type: 'PartyHall', title: 'Salão de festas' },
  { type: 'Pool', title: 'Piscinas' }, { type: 'Other', title: 'Outros' },
]

/** Minutos desde 00:00 → "HH:MM" para <input type="time"> (1440 → "00:00"). */
export const minutesToTime = (minutes: number | null | undefined) => (minutes == null ? '' : fmt.minutes(minutes))

/** "HH:MM" → minutos. No fechamento, "00:00" representa o fim do dia (1440). */
export function timeToMinutes(value: string, closing = false): number | null {
  const match = /^(\d{1,2}):(\d{2})$/.exec(value)
  if (!match) return null
  const minutes = Number(match[1]) * 60 + Number(match[2])
  return closing && minutes === 0 ? 1440 : minutes
}

/** Resumo do funcionamento semanal: dias consecutivos com o mesmo horário são agrupados ("Seg–Sex 08:00–22:00"). */
export function scheduleSummary(schedules: Schema<'ScheduleDto'>[]): string {
  if (schedules.length === 0) return 'Sem horários definidos'
  const byDay = new Map(schedules.map((s) => [s.dayOfWeek, s]))
  const groups: { from: string; to: string; hours: string }[] = []
  let previousIndex = -2
  for (const day of daysOfWeek) {
    const schedule = byDay.get(day)
    if (!schedule) continue
    const hours = `${fmt.minutes(schedule.openMinute)}–${fmt.minutes(schedule.closeMinute)}`
    const last = groups[groups.length - 1]
    if (last && last.hours === hours && previousIndex === dayOfWeek[day].index - 1) last.to = dayOfWeek[day].short
    else groups.push({ from: dayOfWeek[day].short, to: dayOfWeek[day].short, hours })
    previousIndex = dayOfWeek[day].index
  }
  return groups.map((g) => `${g.from === g.to ? g.from : `${g.from}–${g.to}`} ${g.hours}`).join(' · ')
}
