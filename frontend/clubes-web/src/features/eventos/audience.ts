import type { Schema } from '@/lib/api'
import { labels } from '@/lib/labels'

export type ClubEvent = Schema<'EventDto'>

/** Texto do público do evento; para "Específico", lista os grupos marcados. */
export function audienceLabel(e: Pick<ClubEvent, 'audience' | 'allowMembers' | 'allowDependents' | 'allowStaff'>) {
  if (e.audience !== 'Specific') return labels.eventAudience[e.audience].label
  return [e.allowMembers && 'Associados', e.allowDependents && 'Dependentes', e.allowStaff && 'Funcionários'].filter(Boolean).join(', ') || '—'
}
