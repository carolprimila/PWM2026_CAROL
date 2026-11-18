/* Abas do painel da portaria: presentes, convidados do dia, busca manual e ocorrências. */
import { zodResolver } from '@hookform/resolvers/zod'
import { ClipboardList, LogIn, LogOut, Search, UserPlus, Users } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { MemberPicker } from '@/components/shared'
import { usePaged } from '@/components/table'
import { Avatar, Badge, Button, EmptyState, ErrorState, Field, Input, LoadingState, SearchInput, StatusBadge, Textarea, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'

type PersonType = Schema<'PersonType'>
type Operation = Schema<'AccessOperation'>

/** Ações de registro oferecidas pelo painel; o resultado devolvido pelo servidor é exibido no cartão de validação. */
export type GateActions = {
  busy: boolean
  guest: (guestId: string, operation: 'entrada' | 'saida') => void
  manual: (personType: PersonType, personId: string, name: string, operation: Operation) => Promise<void>
}

const row = 'flex items-center gap-3 px-5 py-3 text-sm'

export function PresentTab({ actions }: { actions: GateActions }) {
  const [filter, setFilter] = useState('')
  const present = useQuery({ queryKey: ['/api/portaria/presentes'], queryFn: () => api.get<Schema<'PresenceDto'>[]>('/api/portaria/presentes') })
  if (present.isLoading) return <LoadingState />
  if (present.error || !present.data) return <ErrorState error={present.error} onRetry={() => present.refetch()} />
  const term = filter.trim().toLowerCase()
  const list = term ? present.data.filter((p) => p.personName.toLowerCase().includes(term) || p.responsibleName?.toLowerCase().includes(term)) : present.data
  return (
    <>
      <div className="border-b border-line px-4 py-3"><Input className="max-w-xs" placeholder="Filtrar por nome…" value={filter} onChange={(e) => setFilter(e.target.value)} /></div>
      {list.length === 0 ? <EmptyState icon={Users} title={present.data.length === 0 ? 'Ninguém no clube no momento' : 'Nenhuma pessoa encontrada'} /> : (
        <ul className="max-h-[420px] divide-y divide-line/70 overflow-y-auto">
          {list.map((p) => (
            <li key={p.id} className={row}>
              <Avatar name={p.personName} size={34} />
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-ink">{p.personName}</p>
                <p className="truncate text-xs text-muted">{p.responsibleName ? `Responsável: ${p.responsibleName}` : 'Titular'}</p>
              </div>
              <StatusBadge dict={labels.personType} value={p.personType} />
              <span className="tabular w-28 shrink-0 text-right text-xs text-muted">Entrada {fmt.dateTime(p.enteredAt)}</span>
              <Button size="sm" icon={LogOut} disabled={actions.busy}
                onClick={() => (p.personType === 'Guest' ? actions.guest(p.personId, 'saida') : void actions.manual(p.personType, p.personId, p.personName, 'Exit'))}>Saída</Button>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}

export function GuestsTodayTab({ actions }: { actions: GateActions }) {
  const guests = usePaged<Schema<'GuestDto'>>('/api/convidados', { date: dates.today() }, { pageSize: 100 })
  return (
    <>
      <div className="flex items-center justify-between gap-3 border-b border-line px-4 py-3">
        <SearchInput value={guests.search} onChange={guests.setSearch} placeholder="Convidado, associado ou documento…" />
        <span className="text-xs text-muted">{guests.total} convidado(s) para hoje</span>
      </div>
      {guests.isLoading ? <LoadingState /> : guests.error ? <ErrorState error={guests.error} onRetry={() => guests.refetch()} />
        : guests.rows.length === 0 ? <EmptyState icon={UserPlus} title="Nenhum convidado para hoje" description="Os convidados cadastrados para a data de hoje aparecem aqui." /> : (
          <ul className="max-h-[420px] divide-y divide-line/70 overflow-y-auto">
            {guests.rows.map((g) => (
              <li key={g.id} className={row}>
                <Avatar name={g.fullName} size={34} />
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium text-ink">{g.fullName}{g.document ? <span className="ml-2 text-xs font-normal text-muted">{g.document}</span> : null}</p>
                  <p className="truncate text-xs text-muted">Responsável: {g.memberName}{g.spaceName ? ` · ${g.spaceName}` : ''}</p>
                </div>
                {g.isCancelled ? <Badge>Cancelado</Badge> : g.exitedAt ? <Badge>Saiu às {fmt.time(g.exitedAt)}</Badge> : g.enteredAt ? <Badge tone="success">No clube desde {fmt.time(g.enteredAt)}</Badge>
                  : <StatusBadge dict={labels.guestAuthorization} value={g.authorization} />}
                {!g.isCancelled && !g.enteredAt && <Button size="sm" variant="primary" icon={LogIn} disabled={actions.busy} onClick={() => actions.guest(g.id, 'entrada')}>Entrada</Button>}
                {g.enteredAt && !g.exitedAt && <Button size="sm" icon={LogOut} disabled={actions.busy} onClick={() => actions.guest(g.id, 'saida')}>Saída</Button>}
              </li>
            ))}
          </ul>
        )}
    </>
  )
}

export function SearchTab({ actions }: { actions: GateActions }) {
  const [term, setTerm] = useState('')
  const [debounced, setDebounced] = useState('')
  useEffect(() => { const timer = setTimeout(() => setDebounced(term.trim()), 300); return () => clearTimeout(timer) }, [term])
  const enabled = debounced.length >= 3
  const people = useQuery({ queryKey: ['/api/portaria/pessoas', debounced], queryFn: () => api.get<Schema<'GatePersonDto'>[]>('/api/portaria/pessoas', { term: debounced }), enabled })
  // Depois de um registro, a situação "no clube" muda: a lista de presentes é invalidada pelo painel e a busca acompanha.
  const present = useQuery({ queryKey: ['/api/portaria/presentes'], queryFn: () => api.get<Schema<'PresenceDto'>[]>('/api/portaria/presentes') })
  const inside = (personId: string, fallback: boolean) => (present.data ? present.data.some((p) => p.personId === personId) : fallback)
  const statusLabel = (type: PersonType, status: string) =>
    ((type === 'Dependent' ? labels.dependentStatus : labels.memberStatus) as Record<string, { label: string; tone: 'neutral' | 'brand' | 'success' | 'warning' | 'danger' | 'info' }>)[status]

  return (
    <>
      <div className="border-b border-line px-4 py-3">
        <span className="relative block max-w-md">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted" />
          <Input className="pl-9" placeholder="Nome do associado ou dependente (mínimo de 3 letras)…" value={term} onChange={(e) => setTerm(e.target.value)} />
        </span>
        <p className="mt-2 text-xs text-muted">Para quem está sem o celular ou sem o QR Code. O registro manual exige justificativa.</p>
      </div>
      {!enabled ? <EmptyState icon={Search} title="Busque uma pessoa" description="Digite ao menos 3 letras do nome." />
        : people.isLoading ? <LoadingState /> : people.error || !people.data ? <ErrorState error={people.error} onRetry={() => people.refetch()} />
        : people.data.length === 0 ? <EmptyState icon={Search} title="Nenhuma pessoa encontrada" /> : (
          <ul className="max-h-[420px] divide-y divide-line/70 overflow-y-auto">
            {people.data.map((p) => {
              const isInside = inside(p.personId, p.isInside)
              const status = statusLabel(p.personType, p.status)
              return (
                <li key={`${p.personType}-${p.personId}`} className={row}>
                  <Avatar fileId={p.photoFileId} name={p.name} size={40} />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium text-ink">{p.name}</p>
                    <p className="truncate text-xs text-muted">{p.document ?? 'Sem documento'}{p.responsibleName ? ` · Responsável: ${p.responsibleName}` : ''}</p>
                  </div>
                  <StatusBadge dict={labels.personType} value={p.personType} />
                  {status ? <Badge tone={status.tone}>{status.label}</Badge> : <Badge>{p.status}</Badge>}
                  {isInside && <Badge tone="success">No clube</Badge>}
                  {isInside
                    ? <Button size="sm" icon={LogOut} disabled={actions.busy} onClick={() => void actions.manual(p.personType, p.personId, p.name, 'Exit')}>Saída</Button>
                    : <Button size="sm" variant="primary" icon={LogIn} disabled={actions.busy} onClick={() => void actions.manual(p.personType, p.personId, p.name, 'Entry')}>Entrada</Button>}
                </li>
              )
            })}
          </ul>
        )}
    </>
  )
}

const occurrenceSchema = z.object({ description: z.string().trim().min(5, 'Descreva a ocorrência (mínimo de 5 caracteres)').max(4000, 'Máximo de 4000 caracteres') })
type OccurrenceValues = z.infer<typeof occurrenceSchema>

export function OccurrencesTab() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [member, setMember] = useState<{ id: string; name: string } | null>(null)
  const form = useForm<OccurrenceValues>({ resolver: zodResolver(occurrenceSchema), defaultValues: { description: '' } })
  const canView = can('occurrences.view')
  const list = usePaged<Schema<'OccurrenceDto'>>('/api/ocorrencias', { kind: 'Gate' }, { pageSize: 8, enabled: canView })
  const create = useMutation({
    mutationFn: (values: OccurrenceValues) => api.post<Schema<'OccurrenceDto'>>('/api/ocorrencias', { kind: 'Gate', description: values.description.trim(), memberId: member?.id ?? null }),
    onSuccess: (occurrence) => {
      toast.success(`Ocorrência ${occurrence.code} registrada.`)
      form.reset()
      setMember(null)
      void queryClient.invalidateQueries({ queryKey: ['/api/ocorrencias'] })
    },
    onError: toast.error,
  })

  return (
    <>
      {can('occurrences.manage') && (
        <form className="space-y-3 border-b border-line px-5 py-4" noValidate onSubmit={form.handleSubmit((values) => create.mutate(values))}>
          <Field label="Nova ocorrência da portaria" required error={form.formState.errors.description?.message}>
            <Textarea rows={3} aria-invalid={!!form.formState.errors.description} placeholder="Descreva o que aconteceu (tentativa de entrada indevida, desentendimento, objeto esquecido…)" {...form.register('description')} />
          </Field>
          <div className="flex flex-wrap items-end gap-3">
            <div className="min-w-64 flex-1">
              <p className="mb-1.5 text-[13px] font-medium text-ink">Associado envolvido (opcional)</p>
              <MemberPicker value={member} onChange={setMember} onlyActive={false} />
            </div>
            <Button type="submit" variant="primary" icon={ClipboardList} loading={create.isPending}>Registrar ocorrência</Button>
          </div>
        </form>
      )}
      {canView && (
        list.isLoading ? <LoadingState /> : list.error ? <ErrorState error={list.error} onRetry={() => list.refetch()} />
          : list.rows.length === 0 ? <EmptyState icon={ClipboardList} title="Nenhuma ocorrência de portaria registrada" /> : (
            <ul className="max-h-[360px] divide-y divide-line/70 overflow-y-auto">
              {list.rows.map((o) => (
                <li key={o.id} className="px-5 py-3 text-sm">
                  <div className="flex items-center gap-2">
                    <span className="tabular font-medium text-ink">{o.code}</span>
                    <StatusBadge dict={labels.occurrenceStatus} value={o.status} />
                    <span className="ml-auto text-xs text-muted">{fmt.dateTime(o.occurredAt)} · {o.reportedBy}</span>
                  </div>
                  <p className="mt-1 line-clamp-2 text-[13px] text-ink/80">{o.description}</p>
                  {o.memberName && <p className="mt-0.5 text-xs text-muted">Associado: {o.memberName}</p>}
                </li>
              ))}
            </ul>
          )
      )}
    </>
  )
}
