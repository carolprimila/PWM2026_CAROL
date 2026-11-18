import { CheckCircle2, KeyRound, Undo2 } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Alert, Badge, Button, Card, Checkbox, Field, Info, Input, Modal, Select, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'

type Handover = Schema<'KeyHandoverDto'>

const late = (minutes: number) => (minutes >= 1440 ? `${Math.floor(minutes / 1440)}d ${Math.floor((minutes % 1440) / 60)}h` : minutes >= 60 ? `${Math.floor(minutes / 60)}h ${minutes % 60}min` : `${minutes}min`)

export function Keys() {
  const { can } = useAuth()
  const manage = can('inspections.manage')
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [outstandingOnly, setOutstandingOnly] = useState(true)
  const [delivering, setDelivering] = useState(false)
  const [returning, setReturning] = useState<Handover | null>(null)
  const [viewing, setViewing] = useState<Handover | null>(null)
  const list = usePaged<Handover>('/api/chaves', { outstandingOnly: outstandingOnly || undefined }, { refetchInterval: 60_000 })
  const refresh = () => void queryClient.invalidateQueries({ queryKey: ['/api/chaves'] })
  const resolve = useMutation({
    mutationFn: (id: string) => api.post<Handover>(`/api/chaves/${id}/pendencias-resolvidas`),
    onSuccess: () => { toast.success('Pendências marcadas como resolvidas.'); refresh() },
    onError: toast.error,
  })

  const columns: Column<Handover>[] = [
    { key: 'reservation', header: 'Reserva', render: (k) => <div><p className="tabular font-medium text-ink">{k.reservationCode}</p><p className="text-xs text-muted">{k.spaceName}</p></div> },
    { key: 'receiver', header: 'Retirada por', render: (k) => <div className="min-w-0"><p className="truncate text-ink">{k.receiverName}</p>{k.memberName && k.memberName !== k.receiverName && <p className="truncate text-xs text-muted">Associado: {k.memberName}</p>}</div> },
    { key: 'pickedUpAt', header: 'Entrega', render: (k) => <div><p className="tabular text-ink">{fmt.dateTime(k.pickedUpAt)}</p><p className="text-xs text-muted">por {k.deliveredBy}</p></div> },
    { key: 'expected', header: 'Devolução prevista', render: (k) => <span className="tabular text-muted">{fmt.dateTime(k.expectedReturnAt)}</span> },
    { key: 'returned', header: 'Devolução', render: (k) => (k.returnedAt ? <div><p className="tabular text-ink">{fmt.dateTime(k.returnedAt)}</p><p className="text-xs text-muted">recebida por {k.receivedBy ?? '—'}</p></div> : <span className="text-muted">Não devolvidas</span>) },
    {
      key: 'status', header: 'Situação',
      render: (k) => (
        <div className="flex flex-wrap items-center gap-1.5">
          {k.returnedAt ? <Badge tone="success">Devolvidas</Badge> : <Badge tone="warning">Em posse</Badge>}
          {k.isLate && <Badge tone="danger">{k.returnedAt ? 'Devolvidas com atraso de' : 'Atraso de'} {late(k.lateMinutes)}</Badge>}
          {k.pendencies && (k.pendenciesResolved ? <Badge>Pendências resolvidas</Badge> : <Badge tone="danger">Com pendências</Badge>)}
        </div>
      ),
    },
  ]

  return (
    <Card padded={false}>
      <Toolbar actions={manage && <Button variant="primary" size="sm" icon={KeyRound} onClick={() => setDelivering(true)}>Entregar chaves</Button>}>
        <Checkbox label="Somente pendentes (não devolvidas ou com pendências)" checked={outstandingOnly} onChange={(e) => setOutstandingOnly(e.target.checked)} />
      </Toolbar>
      <DataTable columns={columns} rows={list.rows} rowKey={(k) => k.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={setViewing}
        rowActions={manage ? (k) => (
          !k.returnedAt ? <Button size="sm" icon={Undo2} onClick={() => setReturning(k)}>Registrar devolução</Button>
            : k.pendencies && !k.pendenciesResolved ? (
              <Button size="sm" icon={CheckCircle2} disabled={resolve.isPending} onClick={async () => {
                if (await confirm({ title: 'Pendências resolvidas?', message: <>Reserva {k.reservationCode}: <em>{k.pendencies}</em></>, confirmLabel: 'Marcar como resolvidas' })) resolve.mutate(k.id)
              }}>Pendências resolvidas</Button>
            ) : null
        ) : undefined}
        empty={{ title: outstandingOnly ? 'Nenhuma chave pendente' : 'Nenhuma entrega de chaves registrada', description: outstandingOnly ? 'Todas as chaves foram devolvidas e não há pendências em aberto.' : undefined }} />
      {delivering && <DeliverModal onClose={() => setDelivering(false)} onDone={() => { setDelivering(false); refresh() }} />}
      {returning && <ReturnModal handover={returning} onClose={() => setReturning(null)} onDone={() => { setReturning(null); refresh() }} />}
      {viewing && (
        <Modal open onClose={() => setViewing(null)} title={`Chaves — reserva ${viewing.reservationCode}`} footer={<Button variant="primary" onClick={() => setViewing(null)}>Fechar</Button>}>
          <dl className="grid grid-cols-2 gap-4">
            <Info label="Espaço">{viewing.spaceName}</Info>
            <Info label="Associado">{viewing.memberName || '—'}</Info>
            <Info label="Retirada por">{viewing.receiverName}</Info>
            <Info label="Entregue por">{viewing.deliveredBy} em {fmt.dateTime(viewing.pickedUpAt)}</Info>
            <Info label="Devolução prevista">{fmt.dateTime(viewing.expectedReturnAt)}</Info>
            <Info label="Devolução">{viewing.returnedAt ? `${fmt.dateTime(viewing.returnedAt)} · recebida por ${viewing.receivedBy ?? '—'}` : 'Não devolvidas'}</Info>
            <Info label="Condição na entrega" className="col-span-2"><span className="whitespace-pre-wrap">{viewing.conditionOnDelivery || '—'}</span></Info>
            <Info label="Condição na devolução" className="col-span-2"><span className="whitespace-pre-wrap">{viewing.conditionOnReturn || '—'}</span></Info>
            <Info label="Pendências" className="col-span-2"><span className="whitespace-pre-wrap">{viewing.pendencies ? `${viewing.pendencies}${viewing.pendenciesResolved ? ' (resolvidas)' : ''}` : 'Nenhuma'}</span></Info>
            {viewing.isLate && <Info label="Atraso" className="col-span-2"><Badge tone="danger">{late(viewing.lateMinutes)}</Badge></Info>}
          </dl>
        </Modal>
      )}
    </Card>
  )
}

function DeliverModal({ onClose, onDone }: { onClose: () => void; onDone: () => void }) {
  const { can } = useAuth()
  const toast = useToast()
  const [reservationId, setReservationId] = useState('')
  const [receiverName, setReceiverName] = useState('')
  const [condition, setCondition] = useState('')
  // Reservas aprovadas que ainda não terminaram há mais de um dia.
  const from = useMemo(() => new Date(Date.now() - 86_400_000).toISOString(), [])
  const reservations = useQuery({
    queryKey: ['/api/reservas', 'chaves'], enabled: can('reservations.view'),
    queryFn: () => api.get<Paged<Schema<'ReservationDto'>>>('/api/reservas', { status: 'Approved', from, pageSize: 100 }),
  })
  const items = [...(reservations.data?.items ?? [])].sort((a, b) => a.startAt.localeCompare(b.startAt))
  const deliver = useMutation({
    mutationFn: () => api.post<Handover>('/api/chaves/entrega', { reservationId, receiverName: receiverName.trim(), conditionOnDelivery: emptyToNull(condition) } satisfies Schema<'DeliverKeyRequest'>),
    onSuccess: (saved) => { toast.success(`Chaves entregues. Devolução prevista para ${fmt.dateTime(saved.expectedReturnAt)}.`); onDone() },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} title="Entregar chaves" description="Somente reservas aprovadas. A devolução é esperada ao término da reserva."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={deliver.isPending} disabled={!reservationId || receiverName.trim().length < 3} onClick={() => deliver.mutate()}>Registrar entrega</Button>
      </>}>
      <div className="space-y-4">
        {!can('reservations.view') ? <Alert tone="warning">Para selecionar a reserva é necessária a permissão de visualizar reservas.</Alert> : (
          <Field label="Reserva aprovada" required hint={reservations.error ? 'Não foi possível carregar as reservas.' : !reservations.isLoading && items.length === 0 ? 'Nenhuma reserva aprovada em andamento ou futura.' : undefined}>
            <Select value={reservationId} onChange={(e) => {
              setReservationId(e.target.value)
              const reservation = items.find((r) => r.id === e.target.value)
              if (reservation?.memberName && !receiverName.trim()) setReceiverName(reservation.memberName)
            }}>
              <option value="">{reservations.isLoading ? 'Carregando…' : 'Selecione…'}</option>
              {items.map((r) => <option key={r.id} value={r.id}>{r.code} — {r.spaceName} — {r.memberName ?? r.title ?? labels.reservationKind[r.kind].label} — {fmt.dateTime(r.startAt)}</option>)}
            </Select>
          </Field>
        )}
        <Field label="Quem está retirando" required hint="Nome de quem recebe as chaves (mínimo de 3 caracteres)."><Input maxLength={150} value={receiverName} onChange={(e) => setReceiverName(e.target.value)} /></Field>
        <Field label="Condição na entrega" hint="Opcional. Ex.: 2 chaves e 1 controle, espaço limpo."><Textarea rows={3} maxLength={1000} value={condition} onChange={(e) => setCondition(e.target.value)} /></Field>
      </div>
    </Modal>
  )
}

function ReturnModal({ handover, onClose, onDone }: { handover: Handover; onClose: () => void; onDone: () => void }) {
  const toast = useToast()
  const [condition, setCondition] = useState('')
  const [pendencies, setPendencies] = useState('')
  const giveBack = useMutation({
    mutationFn: () => api.post<Handover>(`/api/chaves/${handover.id}/devolucao`, { conditionOnReturn: emptyToNull(condition), pendencies: emptyToNull(pendencies) } satisfies Schema<'ReturnKeyRequest'>),
    onSuccess: (saved) => { toast.success(saved.pendencies ? 'Devolução registrada com pendências.' : 'Devolução registrada.'); onDone() },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} title="Registrar devolução das chaves" description={`Reserva ${handover.reservationCode} · ${handover.spaceName} · retiradas por ${handover.receiverName}`}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={giveBack.isPending} onClick={() => giveBack.mutate()}>Registrar devolução</Button>
      </>}>
      <div className="space-y-4">
        {handover.isLate && <Alert tone="warning" title={`Devolução com atraso de ${late(handover.lateMinutes)}`}>Prevista para {fmt.dateTime(handover.expectedReturnAt)}.</Alert>}
        <Field label="Condição na devolução" hint="Opcional. Estado das chaves e do espaço."><Textarea rows={3} maxLength={1000} value={condition} onChange={(e) => setCondition(e.target.value)} /></Field>
        <Field label="Pendências" hint="Deixe em branco se não houver. Com pendências, o registro continua na lista de pendentes até ser resolvido.">
          <Textarea rows={3} maxLength={1000} placeholder="Ex.: faltou devolver o controle do portão" value={pendencies} onChange={(e) => setPendencies(e.target.value)} />
        </Field>
      </div>
    </Modal>
  )
}
