import type { Schema } from '@/lib/api'

export type Announcement = Schema<'AnnouncementDto'>

/** Públicos de um comunicado, na ordem em que aparecem nos cartões. */
export function audienceOf(a: Pick<Announcement, 'isSpecific' | 'audienceMembers' | 'audienceDependents' | 'audienceStaff' | 'targetUserIds'>) {
  if (a.isSpecific) return [`Específico (${a.targetUserIds.length})`]
  return [a.audienceMembers && 'Associados', a.audienceDependents && 'Dependentes', a.audienceStaff && 'Funcionários'].filter((x): x is string => !!x)
}
