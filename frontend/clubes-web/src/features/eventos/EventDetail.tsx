import { Ban, CheckCircle2, Pencil, Users } from 'lucide-react'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Badge, Button, EmptyState, ErrorState, FileImage, Info, LoadingState, Modal, QueryBoundary, StatusBadge, TriState, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { EventForm } from './EventForm'
import { audienceLabel, type ClubEvent } from './audience'

type Registration = Schema<'EventRegistrationDto'>

export function EventDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [editing, setEditing] = useState(false)
  const event = useQuery({ queryKey: ['evento', id], queryFn: () => api.get<ClubEvent>(`/api/eventos/${id}`) })
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['evento', id] })
    void queryClient.invalidateQueries({ queryKey: ['/api/eventos'] })
  }
  const action = useMutation({
    mutationFn: ({ path, body }: { path: string; body?: unknown; message: string }) => api.post<ClubEvent>(`/api/eventos/${id}/${path}`, body),
    onSuccess: (_, variables) => { toast.success(variables.message); refresh() },
    onError: toast.error,
  })
  const e = event.data
  const manageable = can('events.manage') && e?.status === 'Scheduled'

  if (editing && e) return <EventForm event={e} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />
  return (
    <Modal open onClose={onClose} size="xl" title={e?.title ?? 'Evento'}
      footer={<>
        {manageable && e && <>
          <Button variant="ghost" icon={Ban} className="mr-auto text-danger hover:text-danger" disabled={action.isPending} onClick={async () => {
            const result = await confirm({ title: 'Cancelar evento', message: e.registered > 0 ? `Os ${e.registered} inscrito(s) serão notificados do cancelamento e do motivo.` : 'O evento será marcado como cancelado.', reason: 'required', danger: true, confirmLabel: 'Cancelar evento' })
            if (result) action.mutate({ path: 'cancelar', body: { reason: result.reason }, message: 'Evento cancelado.' })
          }}>Cancelar evento</Button>
          <Button icon={CheckCircle2} disabled={action.isPending} onClick={async () => {
            if (await confirm({ title: 'Encerrar evento?', message: 'O evento será marcado como encerrado e não poderá mais ser alterado. A presença dos inscritos continua editável.', confirmLabel: 'Encerrar' })) action.mutate({ path: 'encerrar', message: 'Evento encerrado.' })
          }}>Encerrar</Button>
          <Button icon={Pencil} onClick={() => setEditing(true)}>Editar</Button>
        </>}
        <Button variant="primary" onClick={onClose}>Fechar</Button>
      </>}>
      <QueryBoundary query={event}>
        {(item) => (
          <div className="grid gap-6 lg:grid-cols-5">
            <div className="space-y-4 lg:col-span-2">
              {item.imageFileId && <FileImage fileId={item.imageFileId} alt={item.title} className="h-44 w-full rounded-(--radius-control) object-cover" />}
              <div className="flex flex-wrap items-center gap-1.5">
                <StatusBadge dict={labels.eventStatus} value={item.status} />
                <Badge>{audienceLabel(item)}</Badge>
                {item.registrationRequired ? <Badge tone="info">Com inscrição</Badge> : <Badge>Sem inscrição</Badge>}
              </div>
              {item.status === 'Cancelled' && <Alert tone="danger" title="Evento cancelado">{item.cancelReason}</Alert>}
              {item.description && <p className="text-sm leading-relaxed whitespace-pre-wrap text-ink/85">{item.description}</p>}
              <dl className="grid grid-cols-2 gap-4 border-t border-line pt-4">
                <Info label="Início">{fmt.dateTime(item.startAt)}</Info>
                <Info label="Término">{fmt.dateTime(item.endAt)}</Info>
                <Info label="Local" className="col-span-2">{item.location || '—'}</Info>
                <Info label="Capacidade">{item.capacity != null ? fmt.number(item.capacity) : 'Sem limite'}</Info>
                <Info label="Criado em">{fmt.date(item.createdAt)}</Info>
                {item.registrationRequired && <>
                  <Info label="Prazo de inscrição">{item.registrationDeadline ? fmt.dateTime(item.registrationDeadline) : 'Até o início'}</Info>
                  <Info label="Prazo de cancelamento">{item.cancellationDeadline ? fmt.dateTime(item.cancellationDeadline) : 'Até o início'}</Info>
                </>}
              </dl>
            </div>
            <div className="lg:col-span-3"><Registrations event={item} /></div>
          </div>
        )}
      </QueryBoundary>
    </Modal>
  )
}

function Registrations({ event }: { event: ClubEvent }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const key = ['evento', event.id, 'inscricoes']
  const list = useQuery({ queryKey: key, queryFn: () => api.get<Registration[]>(`/api/eventos/${event.id}/inscricoes`) })
  const attendance = useMutation({
    mutationFn: ({ registrationId, attended }: { registrationId: string; attended: boolean | null }) => api.put(`/api/eventos/inscricoes/${registrationId}/presenca`, { attended }),
    onSuccess: () => { toast.success('Presença atualizada.'); void queryClient.invalidateQueries({ queryKey: key }) },
    onError: toast.error,
  })
  if (list.isLoading) return <LoadingState />
  if (list.error) return <ErrorState error={list.error} onRetry={() => list.refetch()} />
  const rows = list.data!
  const active = rows.filter((r) => r.status === 'Registered')
  const present = active.filter((r) => r.attended === true).length
  const absent = active.filter((r) => r.attended === false).length
  const counters = [
    { label: 'Inscritos', value: `${active.length}${event.capacity != null ? ` / ${event.capacity}` : ''}` },
    { label: 'Presentes', value: String(present) },
    { label: 'Ausentes', value: String(absent) },
    { label: 'Cancelaram', value: String(rows.length - active.length) },
  ]
  return (
    <div>
      <div className="mb-4 grid grid-cols-4 gap-3">
        {counters.map((counter) => (
          <div key={counter.label} className="rounded-(--radius-control) border border-line px-3 py-2.5">
            <p className="text-xs text-muted">{counter.label}</p>
            <p className="tabular mt-0.5 text-lg font-semibold text-ink">{counter.value}</p>
          </div>
        ))}
      </div>
      {rows.length === 0 ? <EmptyState icon={Users} title="Nenhum inscrito" description={event.registrationRequired ? 'As inscrições feitas pelo aplicativo aparecem aqui.' : 'Este evento não exige inscrição.'} /> : (
        <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
          {rows.map((r) => (
            <li key={r.id} className="flex flex-wrap items-center gap-3 px-4 py-2.5 text-sm">
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-ink">{r.personName}</p>
                <p className="text-xs text-muted">Inscrição em {fmt.dateTime(r.registeredAt)}</p>
              </div>
              <StatusBadge dict={labels.userKind} value={r.personKind} />
              <StatusBadge dict={labels.registrationStatus} value={r.status} />
              {r.status === 'Registered' && (can('events.manage')
                ? <TriState value={r.attended} yes="Presente" no="Ausente" onChange={(attended) => attendance.mutate({ registrationId: r.id, attended })} />
                : <Badge tone={r.attended === true ? 'success' : r.attended === false ? 'danger' : 'neutral'}>{r.attended === true ? 'Presente' : r.attended === false ? 'Ausente' : 'Presença não registrada'}</Badge>)}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
