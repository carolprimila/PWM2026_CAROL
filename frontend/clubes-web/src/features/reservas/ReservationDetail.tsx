import { ArrowLeft, CalendarClock, Check, ClipboardCheck, History, Users, X, XCircle } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { DataTable, type Column } from '@/components/table'
import { Timeline } from '@/components/shared'
import { Alert, Badge, Button, Card, CardTitle, cn, EmptyState, Field, Info, Menu, Modal, PageHeader, QueryBoundary, StatusBadge, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { AvailabilityPicker, period, useSpaces } from './availability'

type Detail = Schema<'ReservationDetailDto'>
type Reservation = Schema<'ReservationDto'>
type Guest = Schema<'GuestDto'>

const statusLabel = (status: string) => labels.reservationStatus[status as keyof typeof labels.reservationStatus]?.label ?? status

export default function ReservationDetail() {
  const { id } = useParams<{ id: string }>()
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [rescheduling, setRescheduling] = useState(false)
  const [attending, setAttending] = useState(false)
  const manage = can('reservations.manage')

  const detail = useQuery({ queryKey: ['reserva', id], queryFn: () => api.get<Detail>(`/api/reservas/${id}`) })
  const refresh = (data?: Detail) => {
    if (data) queryClient.setQueryData(['reserva', id], data)
    else void queryClient.invalidateQueries({ queryKey: ['reserva', id] })
    void queryClient.invalidateQueries({ queryKey: ['/api/reservas'] })
    void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
  }
  const action = useMutation({
    mutationFn: ({ path, reason }: { path: 'aprovar' | 'recusar' | 'cancelar'; reason: string | null; message: string }) => api.post<Detail>(`/api/reservas/${id}/${path}`, { reason }),
    onSuccess: (data, variables) => { toast.success(variables.message); refresh(data) },
    onError: (error) => { toast.error(error); refresh() },
  })

  const approve = async (r: Reservation) => {
    const result = await confirm({
      title: `Aprovar a reserva ${r.code}?`, reason: 'optional', reasonLabel: 'Observação (opcional)', confirmLabel: 'Aprovar',
      message: r.totalAmount > 0 && r.kind === 'Member' ? `Ao aprovar, a cobrança de ${fmt.money(r.totalAmount)} é gerada conforme a forma de cobrança da reserva.` : 'O solicitante será notificado.',
    })
    if (result) action.mutate({ path: 'aprovar', reason: emptyToNull(result.reason), message: 'Reserva aprovada.' })
  }
  const withReason = async (title: string, path: 'recusar' | 'cancelar', message: string, text: string, reasonLabel: string) => {
    const result = await confirm({ title, message: text, reason: 'required', reasonLabel, danger: true, confirmLabel: 'Confirmar' })
    if (result) action.mutate({ path, reason: result.reason, message })
  }

  const guestColumns: Column<Guest>[] = [
    { key: 'name', header: 'Convidado', render: (g) => <span className={cn('font-medium text-ink', g.isCancelled && 'line-through opacity-60')}>{g.fullName}</span> },
    { key: 'document', header: 'Documento', render: (g) => g.document ?? '—' },
    { key: 'phone', header: 'Telefone', render: (g) => fmt.phone(g.phone) },
    { key: 'fee', header: 'Taxa', render: (g) => (g.feeAmount > 0 ? `${fmt.money(g.feeAmount)} · ${g.paymentStatus}` : g.paymentStatus) },
    { key: 'authorization', header: 'Autorização', render: (g) => (g.isCancelled ? <Badge>Cancelado</Badge> : <StatusBadge dict={labels.guestAuthorization} value={g.authorization} />) },
    { key: 'access', header: 'Entrada / saída', render: (g) => <span className="tabular text-muted">{g.enteredAt ? `${fmt.time(g.enteredAt)} – ${g.exitedAt ? fmt.time(g.exitedAt) : 'no clube'}` : '—'}</span> },
  ]

  return (
    <QueryBoundary query={detail}>
      {({ reservation: r, guests, history }) => {
        const started = new Date(r.startAt) <= new Date()
        const canApprove = manage && (r.status === 'Pending' || r.status === 'Suspended')
        const canCancel = manage && (r.status === 'Pending' || r.status === 'Approved' || r.status === 'Suspended')
        const canReschedule = manage && (r.status === 'Pending' || r.status === 'Approved')
        const canAttend = manage && (r.status === 'Approved' || r.status === 'Completed') && started
        return (
          <>
            <PageHeader
              back={<Link to="/reservas" className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-muted hover:text-ink"><ArrowLeft className="size-3.5" />Reservas</Link>}
              title={`Reserva ${r.code}`}
              subtitle={<span className="flex flex-wrap items-center gap-2">{r.spaceName}<StatusBadge dict={labels.reservationStatus} value={r.status} />{r.kind !== 'Member' && <Badge tone={labels.reservationKind[r.kind].tone}>{labels.reservationKind[r.kind].label}</Badge>}</span>}
              actions={<>
                {canApprove && <Button variant="primary" icon={Check} loading={action.isPending} onClick={() => void approve(r)}>Aprovar</Button>}
                {manage && r.status === 'Pending' && <Button icon={X} onClick={() => void withReason(`Recusar a reserva ${r.code}?`, 'recusar', 'Reserva recusada.', 'O solicitante será notificado com o motivo informado e os horários serão liberados.', 'Motivo da recusa')}>Recusar</Button>}
                {canAttend && <Button icon={ClipboardCheck} onClick={() => setAttending(true)}>Registrar comparecimento</Button>}
                <Menu trigger={<Button>Mais ações</Button>} items={[
                  canReschedule && { label: 'Reagendar', icon: CalendarClock, onClick: () => setRescheduling(true) },
                  canCancel && { label: 'Cancelar reserva', icon: XCircle, danger: true, onClick: () => void withReason(`Cancelar a reserva ${r.code}?`, 'cancelar', 'Reserva cancelada.', 'Os horários serão liberados, os convites vinculados cancelados e a cobrança ainda não paga será cancelada. Cancelamentos feitos pela administração não geram multa.', 'Motivo do cancelamento') },
                ]} />
              </>}
            />

            <div className="grid gap-6 xl:grid-cols-3">
              <div className="space-y-6 xl:col-span-2">
                <Card>
                  <CardTitle title="Resumo" />
                  <dl className="grid grid-cols-3 gap-x-6 gap-y-4">
                    <Info label="Espaço">{r.spaceName} <span className="text-muted">· {labels.spaceType[r.spaceType].label}</span></Info>
                    <Info label="Período" className="col-span-2"><span className="tabular">{period(r.startAt, r.endAt)}</span></Info>
                    <Info label={r.slots.length > 1 ? `Intervalos (${r.slots.length})` : 'Intervalo'} className="col-span-3">
                      {r.slots.length === 0 ? '—' : <span className="flex flex-wrap gap-1.5">{r.slots.map((s, index) => <span key={`${s.startAt}-${index}`} className="tabular rounded-lg bg-canvas px-2 py-1 text-[13px]">{period(s.startAt, s.endAt)}</span>)}</span>}
                    </Info>
                    {r.kind === 'Member'
                      ? <Info label="Associado">{r.memberId && can('members.view') ? <Link to={`/associados/${r.memberId}`} className="font-medium text-brand-600 hover:underline">{r.memberName}</Link> : r.memberName}</Info>
                      : <Info label="Título">{r.title}</Info>}
                    <Info label="Solicitante">{r.requestedBy || '—'}{r.requestedByDependent && <Badge tone="info" className="ml-2">Solicitada por dependente</Badge>}</Info>
                    <Info label="Solicitada em">{fmt.dateTime(r.createdAt)}</Info>
                    <Info label="Pessoas">{r.peopleCount}</Info>
                    <Info label="Convidados">{r.guests}</Info>
                    <Info label="Tipo">{labels.reservationKind[r.kind].label}</Info>
                    {r.notes && <Info label="Observações" className="col-span-3"><span className="whitespace-pre-wrap">{r.notes}</span></Info>}
                    {r.decisionReason && <Info label="Decisão da administração" className="col-span-3">{r.decisionReason}</Info>}
                    {r.cancellationReason && <Info label="Motivo do cancelamento" className="col-span-3">{r.cancellationReason}</Info>}
                  </dl>
                </Card>

                <Card padded={false}>
                  <div className="px-5 pt-5"><CardTitle icon={Users} title="Convidados" description={guests.length > 0 ? `${guests.length} convidado(s) vinculados a esta reserva.` : undefined} /></div>
                  {guests.length === 0 ? <EmptyState icon={Users} title="Nenhum convidado nesta reserva" /> : <div className="border-t border-line"><DataTable columns={guestColumns} rows={guests} rowKey={(g) => g.id} /></div>}
                </Card>
              </div>

              <div className="space-y-6">
                <Card>
                  <CardTitle title="Valores" />
                  <dl className="space-y-2.5 text-sm">
                    <div className="flex justify-between"><dt className="text-muted">Locação</dt><dd className="tabular text-ink">{fmt.money(r.rentalAmount)}</dd></div>
                    <div className="flex justify-between"><dt className="text-muted">Taxa de limpeza</dt><dd className="tabular text-ink">{fmt.money(r.cleaningFee)}</dd></div>
                    <div className="flex justify-between border-t border-line pt-2.5"><dt className="font-semibold text-ink">Total</dt><dd className="tabular text-lg font-semibold text-ink">{fmt.money(r.totalAmount)}</dd></div>
                    <div className="flex justify-between pt-1"><dt className="text-muted">Forma de cobrança</dt><dd className="text-ink">{r.chargeMode ? labels.chargeMode[r.chargeMode].label : 'Não definida'}</dd></div>
                  </dl>
                  {r.chargeId ? <Link to={`/financeiro/cobrancas/${r.chargeId}`} className="mt-4 inline-block text-[13px] font-medium text-brand-600 hover:underline">Ver cobrança da reserva</Link>
                    : r.totalAmount > 0 && <p className="mt-4 text-xs text-muted">A cobrança é gerada na aprovação da reserva.</p>}
                </Card>

                <Card>
                  <CardTitle icon={ClipboardCheck} title="Comparecimento" />
                  <div className="flex items-center gap-2"><StatusBadge dict={labels.attendance} value={r.attendance} /></div>
                  {r.attendanceNote && <p className="mt-2 text-[13px] text-ink/80">{r.attendanceNote}</p>}
                  {r.penaltyChargeId && <Link to={`/financeiro/cobrancas/${r.penaltyChargeId}`} className="mt-3 inline-block text-[13px] font-medium text-brand-600 hover:underline">Ver cobrança da penalidade</Link>}
                  {r.attendance === 'Unknown' && !started && <p className="mt-2 text-xs text-muted">O comparecimento pode ser registrado após o início da reserva.</p>}
                </Card>

                <Card>
                  <CardTitle icon={History} title="Histórico" />
                  {history.length === 0 ? <EmptyState icon={History} title="Sem histórico" />
                    : <Timeline items={history.map((h) => ({ id: h.id, title: h.fromStatus === h.toStatus ? 'Alteração registrada' : statusLabel(h.toStatus), note: h.note, by: h.userName, at: h.at }))} />}
                </Card>
              </div>
            </div>

            {rescheduling && <RescheduleModal reservation={r} onClose={() => setRescheduling(false)} onDone={(data) => { setRescheduling(false); refresh(data) }} />}
            {attending && <AttendanceModal reservation={r} onClose={() => setAttending(false)} onDone={(data) => { setAttending(false); refresh(data) }} />}
          </>
        )
      }}
    </QueryBoundary>
  )
}

function RescheduleModal({ reservation, onClose, onDone }: { reservation: Reservation; onClose: () => void; onDone: (detail: Detail) => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const spaces = useSpaces()
  const space = spaces.data?.find((s) => s.id === reservation.spaceId)
  const hourly = space?.bookingMode === 'Hourly'
  const currentDay = dates.toDay(new Date(reservation.startAt))
  const [date, setDate] = useState(currentDay < dates.today() ? dates.today() : currentDay)
  const [minutes, setMinutes] = useState<number[]>([])
  const [reason, setReason] = useState('')
  const ready = !!space && !!date && (hourly ? minutes.length > 0 : date !== currentDay)

  const reschedule = useMutation({
    mutationFn: () => api.post<Detail>(`/api/reservas/${reservation.id}/reagendar`, { date, startMinutes: hourly ? minutes : [], reason: emptyToNull(reason) }),
    onSuccess: (data) => { toast.success('Reserva reagendada. O solicitante foi notificado.'); onDone(data) },
    onError: (error) => {
      toast.error(error)
      if (error instanceof ApiError && error.status === 409) void queryClient.invalidateQueries({ queryKey: ['disponibilidade', reservation.spaceId] })
    },
  })

  return (
    <Modal open onClose={onClose} size="lg" title={`Reagendar ${reservation.code}`} description={`${reservation.spaceName} · atual: ${period(reservation.startAt, reservation.endAt)}`}
      footer={<><Button onClick={onClose}>Cancelar</Button><Button variant="primary" disabled={!ready} loading={reschedule.isPending} onClick={() => reschedule.mutate()}>Confirmar novo horário</Button></>}>
      <QueryBoundary query={spaces}>
        {() => !space ? <Alert tone="warning">O espaço desta reserva não está mais ativo. Não é possível reagendar.</Alert> : (
          <div className="space-y-5">
            <AvailabilityPicker space={space} date={date} onDate={setDate} minutes={minutes} onMinutes={setMinutes} own={reservation.slots} />
            <Field label="Motivo da alteração" hint="Registrado no histórico da reserva."><Textarea rows={2} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} /></Field>
          </div>
        )}
      </QueryBoundary>
    </Modal>
  )
}

function AttendanceModal({ reservation, onClose, onDone }: { reservation: Reservation; onClose: () => void; onDone: (detail: Detail) => void }) {
  const toast = useToast()
  const [attendance, setAttendance] = useState<'Attended' | 'NoShow'>(reservation.attendance === 'NoShow' ? 'NoShow' : 'Attended')
  const [note, setNote] = useState(reservation.attendanceNote ?? '')
  const missing = attendance === 'NoShow' && note.trim().length < 3
  const save = useMutation({
    mutationFn: () => api.post<Detail>(`/api/reservas/${reservation.id}/comparecimento`, { attendance, note: emptyToNull(note) }),
    onSuccess: (data) => { toast.success(attendance === 'Attended' ? 'Comparecimento registrado.' : 'Não comparecimento registrado.'); onDone(data) },
    onError: toast.error,
  })
  return (
    <Modal open onClose={onClose} size="sm" title="Registrar comparecimento" description={`${reservation.code} · ${reservation.spaceName}`}
      footer={<><Button onClick={onClose}>Cancelar</Button><Button variant={attendance === 'NoShow' ? 'danger' : 'primary'} disabled={missing} loading={save.isPending} onClick={() => save.mutate()}>Registrar</Button></>}>
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-2">
          {(['Attended', 'NoShow'] as const).map((value) => (
            <button key={value} type="button" aria-pressed={attendance === value} onClick={() => setAttendance(value)}
              className={cn('rounded-(--radius-control) border px-3 py-2.5 text-sm font-medium transition-colors', attendance === value ? (value === 'Attended' ? 'border-success bg-success-soft text-success' : 'border-danger bg-danger-soft text-danger') : 'border-line text-ink hover:bg-canvas')}>
              {labels.attendance[value].label}
            </button>
          ))}
        </div>
        <Field label={attendance === 'NoShow' ? 'Descrição da ocorrência' : 'Observação'} required={attendance === 'NoShow'}>
          <Textarea rows={3} maxLength={500} value={note} onChange={(e) => setNote(e.target.value)} placeholder={attendance === 'NoShow' ? 'Descreva o não comparecimento…' : 'Opcional'} />
        </Field>
        {attendance === 'NoShow' && <Alert tone="warning">A penalidade por não comparecimento só é cobrada se o clube tiver configurado um valor. Sem essa configuração, apenas o registro fica no histórico.</Alert>}
      </div>
    </Modal>
  )
}
