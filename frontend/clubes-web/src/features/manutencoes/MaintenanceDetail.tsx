import { CalendarOff, Pencil, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Attachments, HistoryList } from '@/components/shared'
import { Alert, Badge, Button, Field, Info, Input, Modal, MoneyInput, QueryBoundary, Select, StatusBadge, Switch, Tabs, Textarea, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { MaintenanceForm } from './MaintenanceForm'

type Maintenance = Schema<'MaintenanceDto'>
type Status = Schema<'MaintenanceStatus'>
type Tab = 'dados' | 'historico' | 'anexos'

/** Fluxo: Solicitada → Em análise → Aprovada → Em andamento → Concluída; o cancelamento é possível em qualquer etapa aberta. */
const NEXT: Record<Status, Status[]> = {
  Requested: ['UnderReview', 'Cancelled'], UnderReview: ['Approved', 'Cancelled'], Approved: ['InProgress', 'Cancelled'], InProgress: ['Completed', 'Cancelled'], Completed: [], Cancelled: [],
}

export function MaintenanceDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const { can } = useAuth()
  const [tab, setTab] = useState<Tab>('dados')
  const [modal, setModal] = useState<'edit' | 'status' | 'block' | null>(null)
  const maintenance = useQuery({ queryKey: ['manutencao', id], queryFn: () => api.get<Maintenance>(`/api/manutencoes/${id}`) })
  const m = maintenance.data
  const open = !!m && NEXT[m.status].length > 0
  const manage = can('maintenance.manage')

  if (modal === 'edit' && m) return <MaintenanceForm maintenance={m} onClose={() => setModal(null)} onSaved={() => setModal(null)} />
  if (modal === 'status' && m) return <StatusModal maintenance={m} onClose={() => setModal(null)} />
  if (modal === 'block' && m) return <BlockModal maintenance={m} onClose={() => setModal(null)} />
  return (
    <Modal open onClose={onClose} size="lg" title={m ? `${m.code} — ${m.title}` : 'Manutenção'}
      footer={<>
        {manage && open && <>
          <Button icon={Pencil} onClick={() => setModal('edit')}>Editar</Button>
          {m.spaceId && <Button icon={CalendarOff} onClick={() => setModal('block')}>Bloquear espaço</Button>}
          <Button variant="primary" icon={RefreshCw} onClick={() => setModal('status')}>Alterar situação</Button>
        </>}
        <Button variant={manage && open ? 'secondary' : 'primary'} onClick={onClose}>Fechar</Button>
      </>}>
      <QueryBoundary query={maintenance}>
        {(item) => (
          <>
            <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'dados', label: 'Dados' }, { key: 'historico', label: 'Histórico' }, { key: 'anexos', label: 'Fotos e anexos' }]} />
            {tab === 'dados' && (
              <div className="space-y-4">
                <div className="flex flex-wrap items-center gap-1.5">
                  <StatusBadge dict={labels.maintenanceStatus} value={item.status} />
                  <StatusBadge dict={labels.maintenanceKind} value={item.kind} />
                  {item.isLate && <Badge tone="danger">Atrasada</Badge>}
                  {item.hasActiveBlock && <Badge tone="warning">Espaço bloqueado para reservas</Badge>}
                </div>
                {item.description && <p className="text-sm leading-relaxed whitespace-pre-wrap text-ink/85">{item.description}</p>}
                <dl className="grid grid-cols-3 gap-x-6 gap-y-4 border-t border-line pt-4">
                  <Info label="Espaço">{item.spaceName || '—'}</Info>
                  <Info label="Bem patrimonial">{item.assetName || '—'}</Info>
                  <Info label="Responsável">{item.responsibleName || '—'}</Info>
                  <Info label="Abertura">{fmt.dateTime(item.openedAt)}</Info>
                  <Info label="Prazo"><span className={item.isLate ? 'font-medium text-danger' : ''}>{fmt.day(item.dueDate)}</span></Info>
                  <Info label="Conclusão">{item.completedAt ? fmt.dateTime(item.completedAt) : '—'}</Info>
                  <Info label="Custo estimado">{fmt.money(item.estimatedCost)}</Info>
                  <Info label="Custo real">{fmt.money(item.actualCost)}</Info>
                </dl>
              </div>
            )}
            {tab === 'historico' && <HistoryList entityType="MaintenanceOrder" id={item.id} labelOf={(s) => labels.maintenanceStatus[s as Status]?.label ?? s} />}
            {tab === 'anexos' && <Attachments ownerType="MaintenanceOrder" ownerId={item.id} canEdit={manage} />}
          </>
        )}
      </QueryBoundary>
    </Modal>
  )
}

function StatusModal({ maintenance: m, onClose }: { maintenance: Maintenance; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const [status, setStatus] = useState<Status>(NEXT[m.status][0])
  const [note, setNote] = useState('')
  const [actualCost, setActualCost] = useState<number | null>(m.actualCost ?? null)
  const [createExpense, setCreateExpense] = useState(false)
  const withCost = status === 'InProgress' || status === 'Completed'
  const noteRequired = status === 'Cancelled'
  const expense = status === 'Completed' && createExpense

  const save = useMutation({
    mutationFn: () => api.post<Maintenance>(`/api/manutencoes/${m.id}/situacao`, { status, note: emptyToNull(note), actualCost: withCost ? actualCost : null, createExpense: expense } satisfies Schema<'MaintenanceStatusRequest'>),
    onSuccess: (saved) => {
      toast.success(expense ? 'Manutenção concluída e despesa lançada no financeiro.' : `Situação alterada para "${labels.maintenanceStatus[saved.status].label}".`)
      queryClient.setQueryData(['manutencao', m.id], saved)
      void queryClient.invalidateQueries({ queryKey: ['/api/manutencoes'] })
      void queryClient.invalidateQueries({ queryKey: ['historico', 'MaintenanceOrder', m.id] })
      onClose()
    },
    onError: toast.error,
  })
  const invalid = (noteRequired && !note.trim()) || (expense && !(actualCost != null && actualCost > 0))

  return (
    <Modal open onClose={onClose} title={`Alterar situação — ${m.code}`} description={`Situação atual: ${labels.maintenanceStatus[m.status].label}`}
      footer={<>
        <Button onClick={onClose}>Voltar</Button>
        <Button variant={status === 'Cancelled' ? 'danger' : 'primary'} loading={save.isPending} disabled={invalid} onClick={() => save.mutate()}>Confirmar</Button>
      </>}>
      <div className="space-y-4">
        <Field label="Nova situação">
          <Select value={status} onChange={(e) => setStatus(e.target.value as Status)}>{NEXT[m.status].map((s) => <option key={s} value={s}>{labels.maintenanceStatus[s].label}</option>)}</Select>
        </Field>
        {(status === 'Completed' || status === 'Cancelled') && m.hasActiveBlock && <Alert tone="info">O bloqueio do espaço vinculado a esta manutenção será encerrado automaticamente e o espaço volta a ficar disponível para reservas.</Alert>}
        <Field label={noteRequired ? 'Motivo do cancelamento' : 'Observação'} required={noteRequired} hint={noteRequired ? undefined : 'Opcional. Fica registrada no histórico.'}>
          <Textarea rows={3} maxLength={1000} value={note} onChange={(e) => setNote(e.target.value)} />
        </Field>
        {withCost && <Field label="Custo real" hint={status === 'Completed' ? undefined : 'Opcional. Pode ser informado na conclusão.'}><MoneyInput value={actualCost} onChange={setActualCost} /></Field>}
        {status === 'Completed' && (
          <>
            <Switch checked={createExpense} onChange={setCreateExpense} label="Lançar despesa no financeiro"
              description='Cria uma despesa pendente na categoria "Manutenção" com o custo real informado, vencendo hoje.' />
            {expense && !(actualCost != null && actualCost > 0) && <p className="text-xs text-danger">Informe o custo real (maior que zero) para lançar a despesa.</p>}
          </>
        )}
      </div>
    </Modal>
  )
}

function BlockModal({ maintenance: m, onClose }: { maintenance: Maintenance; onClose: () => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [startAt, setStartAt] = useState(dates.toInput(new Date().toISOString()))
  const [endAt, setEndAt] = useState('')
  const [result, setResult] = useState<Schema<'BlockResult'> | null>(null)
  const invalidRange = !!startAt && !!endAt && new Date(endAt) <= new Date(startAt)
  const block = useMutation({
    mutationFn: () => api.post<Schema<'BlockResult'>>(`/api/manutencoes/${m.id}/bloqueio`, { startAt: dates.fromInput(startAt)!, endAt: dates.fromInput(endAt)! } satisfies Schema<'MaintenanceBlockRequest'>),
    onSuccess: (data) => {
      toast.success('Espaço bloqueado para reservas no período.')
      setResult(data)
      void queryClient.invalidateQueries({ queryKey: ['manutencao', m.id] })
      void queryClient.invalidateQueries({ queryKey: ['/api/manutencoes'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/espacos/bloqueios'] })
    },
    onError: toast.error,
  })

  if (result) {
    return (
      <Modal open onClose={onClose} size="lg" title="Espaço bloqueado" footer={<Button variant="primary" onClick={onClose}>Concluir</Button>}>
        <div className="space-y-4">
          <Alert tone="success" title={`${result.block.spaceName ?? m.spaceName ?? 'Espaço'} bloqueado`}>De {fmt.dateTime(result.block.startAt)} até {fmt.dateTime(result.block.endAt)}. A disponibilidade das reservas já reflete o bloqueio.</Alert>
          {result.conflicts.length === 0 ? <p className="text-sm text-muted">Nenhuma reserva existente conflita com o período bloqueado.</p> : (
            <>
              <Alert tone="warning" title={`${result.conflicts.length} reserva(s) existente(s) no período`}>O bloqueio não cancela reservas automaticamente. Avalie cada uma e reagende ou cancele pela tela de reservas.</Alert>
              <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
                {result.conflicts.map((c) => (
                  <li key={c.reservationId} className="flex flex-wrap items-center gap-3 px-4 py-2.5 text-sm">
                    {can('reservations.view') ? <Link to={`/reservas/${c.reservationId}`} className="tabular font-medium text-brand-600 hover:underline">{c.code}</Link> : <span className="tabular font-medium text-ink">{c.code}</span>}
                    <span className="min-w-0 flex-1 truncate text-ink">{c.memberName ?? 'Reserva administrativa'}</span>
                    <span className="tabular text-xs text-muted">{fmt.dateTime(c.startAt)} – {fmt.dateTime(c.endAt)}</span>
                    <StatusBadge dict={labels.reservationStatus} value={c.status} />
                  </li>
                ))}
              </ul>
            </>
          )}
        </div>
      </Modal>
    )
  }
  return (
    <Modal open onClose={onClose} title={`Bloquear ${m.spaceName ?? 'espaço'}`} description={`Manutenção ${m.code}. O bloqueio vale imediatamente para novas reservas.`}
      footer={<>
        <Button onClick={onClose}>Voltar</Button>
        <Button variant="primary" loading={block.isPending} disabled={!startAt || !endAt || invalidRange} onClick={() => block.mutate()}>Bloquear espaço</Button>
      </>}>
      <div className="space-y-4">
        {m.hasActiveBlock && <Alert tone="warning">Já existe um bloqueio vigente vinculado a esta manutenção. Um novo bloqueio será criado além do existente.</Alert>}
        <div className="grid grid-cols-2 gap-4">
          <Field label="Início" required><Input type="datetime-local" value={startAt} onChange={(e) => setStartAt(e.target.value)} /></Field>
          <Field label="Término" required error={invalidRange ? 'Deve ser posterior ao início' : undefined}><Input type="datetime-local" aria-invalid={invalidRange} value={endAt} onChange={(e) => setEndAt(e.target.value)} /></Field>
        </div>
        <p className="text-xs text-muted">Ao concluir ou cancelar a manutenção, o bloqueio é encerrado automaticamente. Reservas já existentes no período serão listadas após a confirmação.</p>
      </div>
    </Modal>
  )
}
