import { Banknote, CheckCircle2, XCircle } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Attachments, HistoryList, MemberPicker } from '@/components/shared'
import { Alert, Button, Field, Info, Modal, MoneyInput, QueryBoundary, Select, StatusBadge, Tabs, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Occurrence = Schema<'OccurrenceDto'>
type Status = Schema<'OccurrenceStatus'>
type Tab = 'dados' | 'anexos' | 'historico'

export function OccurrenceDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const { can } = useAuth()
  const [tab, setTab] = useState<Tab>('dados')
  const [proposing, setProposing] = useState(false)
  const occurrence = useQuery({ queryKey: ['ocorrencia', id], queryFn: () => api.get<Occurrence>(`/api/ocorrencias/${id}`) })
  const o = occurrence.data

  if (proposing && o) return <ProposeModal occurrence={o} onClose={() => setProposing(false)} />
  return (
    <Modal open onClose={onClose} size="xl" title={o ? `Ocorrência ${o.code}` : 'Ocorrência'} description={o ? `Registrada por ${o.reportedBy} · ${fmt.dateTime(o.occurredAt)}` : undefined}
      footer={<Button variant="primary" onClick={onClose}>Fechar</Button>}>
      <QueryBoundary query={occurrence}>
        {(item) => (
          <>
            <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'dados', label: 'Dados e tratamento' }, { key: 'anexos', label: 'Anexos' }, { key: 'historico', label: 'Histórico' }]} />
            {tab === 'dados' && (
              <div className="grid gap-6 lg:grid-cols-2">
                <div className="space-y-4">
                  <div className="flex flex-wrap items-center gap-1.5">
                    <StatusBadge dict={labels.occurrenceKind} value={item.kind} />
                    <StatusBadge dict={labels.occurrenceStatus} value={item.status} />
                    {item.chargeDecision !== 'None' && <StatusBadge dict={labels.chargeDecision} value={item.chargeDecision} />}
                  </div>
                  <p className="text-sm leading-relaxed whitespace-pre-wrap text-ink">{item.description}</p>
                  <dl className="grid grid-cols-2 gap-4 border-t border-line pt-4">
                    <Info label="Associado">
                      {item.memberId ? (can('members.view') ? <Link to={`/associados/${item.memberId}`} className="font-medium text-brand-600 hover:underline">{item.memberName}</Link> : item.memberName) : '—'}
                    </Info>
                    <Info label="Espaço">{item.spaceName || '—'}</Info>
                    <Info label="Reserva">
                      {item.reservationId ? (can('reservations.view') ? <Link to={`/reservas/${item.reservationId}`} className="font-medium text-brand-600 hover:underline">{item.reservationCode}</Link> : item.reservationCode) : '—'}
                    </Info>
                    <Info label="Origem">{item.inspectionId ? 'Gerada automaticamente por vistoria' : 'Registro manual'}</Info>
                    {item.resolution && <Info label={item.status === 'Dismissed' ? 'Motivo do arquivamento' : 'Resolução'} className="col-span-2"><span className="whitespace-pre-wrap">{item.resolution}</span></Info>}
                  </dl>
                </div>
                <div className="space-y-5">
                  {can('occurrences.manage') && <StatusPanel occurrence={item} />}
                  <ChargePanel occurrence={item} onPropose={() => setProposing(true)} />
                </div>
              </div>
            )}
            {tab === 'anexos' && <Attachments ownerType="Occurrence" ownerId={item.id} canEdit={can('occurrences.manage')} />}
            {tab === 'historico' && <HistoryList entityType="Occurrence" id={item.id} labelOf={(s) => labels.occurrenceStatus[s as Status]?.label ?? s} />}
          </>
        )}
      </QueryBoundary>
    </Modal>
  )
}

function useOccurrenceUpdated(id: string) {
  const queryClient = useQueryClient()
  return (saved: Occurrence) => {
    queryClient.setQueryData(['ocorrencia', id], saved)
    void queryClient.invalidateQueries({ queryKey: ['/api/ocorrencias'] })
    void queryClient.invalidateQueries({ queryKey: ['historico', 'Occurrence', id] })
  }
}

const panel = 'rounded-(--radius-control) border border-line p-4'
const panelTitle = 'mb-3 text-xs font-semibold tracking-wider text-muted uppercase'

function StatusPanel({ occurrence: o }: { occurrence: Occurrence }) {
  const toast = useToast()
  const updated = useOccurrenceUpdated(o.id)
  const [status, setStatus] = useState<Status>(o.status)
  const [resolution, setResolution] = useState('')
  const closing = status === 'Resolved' || status === 'Dismissed'
  const blocked = closing && o.chargeDecision === 'Pending'
  const save = useMutation({
    mutationFn: () => api.put<Occurrence>(`/api/ocorrencias/${o.id}`, { status, resolution: emptyToNull(resolution) } satisfies Schema<'UpdateOccurrenceRequest'>),
    onSuccess: (saved) => { toast.success(`Situação alterada para "${labels.occurrenceStatus[saved.status].label}".`); setResolution(''); updated(saved) },
    onError: toast.error,
  })

  return (
    <section className={panel}>
      <h3 className={panelTitle}>Situação</h3>
      <div className="space-y-3">
        <Field label="Nova situação">
          <Select value={status} onChange={(e) => setStatus(e.target.value as Status)}>{options(labels.occurrenceStatus).map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}</Select>
        </Field>
        <Field label={status === 'Dismissed' ? 'Motivo do arquivamento' : status === 'Resolved' ? 'Resolução' : 'Observação'} required={closing} hint={closing ? undefined : 'Opcional. Fica registrada no histórico.'}>
          <Textarea rows={3} maxLength={2000} value={resolution} onChange={(e) => setResolution(e.target.value)} />
        </Field>
        {blocked && <p className="text-xs text-danger">Há uma cobrança proposta aguardando decisão. Aprove ou rejeite a cobrança antes de encerrar a ocorrência.</p>}
        <Button className="w-full" loading={save.isPending} disabled={blocked || (closing && !resolution.trim()) || (status === o.status && !resolution.trim())} onClick={() => save.mutate()}>Atualizar situação</Button>
      </div>
    </section>
  )
}

function ChargePanel({ occurrence: o, onPropose }: { occurrence: Occurrence; onPropose: () => void }) {
  const { can } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const updated = useOccurrenceUpdated(o.id)
  const decide = useMutation({
    mutationFn: (body: Schema<'DecideChargeRequest'>) => api.post<Occurrence>(`/api/ocorrencias/${o.id}/cobranca/decisao`, body),
    onSuccess: (saved, body) => { toast.success(body.approved ? 'Cobrança aprovada e lançada no financeiro.' : 'Cobrança rejeitada.'); updated(saved) },
    onError: toast.error,
  })
  const ask = async (approved: boolean) => {
    const result = await confirm({
      title: approved ? 'Aprovar cobrança por dano' : 'Rejeitar cobrança por dano', reason: 'required', reasonLabel: approved ? 'Justificativa da aprovação' : 'Motivo da rejeição', danger: !approved,
      confirmLabel: approved ? 'Aprovar e lançar' : 'Rejeitar',
      message: approved
        ? <>Será lançada no financeiro uma cobrança de <strong>{fmt.money(o.proposedChargeAmount)}</strong> para <strong>{o.memberName}</strong>, com vencimento em 30 dias. O associado será notificado.</>
        : <>A proposta de {fmt.money(o.proposedChargeAmount)} será rejeitada e nada será lançado no financeiro.</>,
    })
    if (result) decide.mutate({ approved, reason: result.reason })
  }
  const canPropose = can('occurrences.manage') && o.chargeDecision !== 'Approved'

  return (
    <section className={panel}>
      <h3 className={panelTitle}>Cobrança por dano</h3>
      <div className="space-y-3">
        {o.chargeDecision === 'None' && <p className="text-sm text-muted">Nenhuma cobrança proposta. A cobrança só chega ao financeiro depois de aprovada pela administração.</p>}
        {o.chargeDecision === 'Pending' && (
          <Alert tone="warning" title={`Proposta de ${fmt.money(o.proposedChargeAmount)} aguardando aprovação`}>
            Responsável: {o.memberName ?? '—'}. Nada é lançado no financeiro antes da aprovação administrativa.
          </Alert>
        )}
        {o.chargeDecision === 'Approved' && (
          <Alert tone="success" title={`Cobrança de ${fmt.money(o.proposedChargeAmount)} aprovada`}>
            {o.chargeDecisionReason}
            {o.chargeId && can('finance.view') && <> · <Link to={`/financeiro/cobrancas/${o.chargeId}`} className="font-medium underline">Abrir cobrança no financeiro</Link></>}
          </Alert>
        )}
        {o.chargeDecision === 'Rejected' && <Alert tone="danger" title={`Proposta de ${fmt.money(o.proposedChargeAmount)} rejeitada`}>{o.chargeDecisionReason} Nada foi lançado no financeiro.</Alert>}

        {o.chargeDecision === 'Pending' && can('occurrences.approve-charge') && (
          <div className="grid grid-cols-2 gap-2">
            <Button icon={XCircle} disabled={decide.isPending} onClick={() => ask(false)}>Rejeitar cobrança</Button>
            <Button variant="primary" icon={CheckCircle2} loading={decide.isPending} onClick={() => ask(true)}>Aprovar cobrança</Button>
          </div>
        )}
        {o.chargeDecision === 'Pending' && !can('occurrences.approve-charge') && <p className="text-xs text-muted">A decisão cabe a um usuário com permissão para aprovar cobranças de ocorrências.</p>}
        {canPropose && <Button icon={Banknote} className="w-full" onClick={onPropose}>{o.chargeDecision === 'None' ? 'Propor cobrança' : o.chargeDecision === 'Pending' ? 'Alterar proposta' : 'Propor novamente'}</Button>}
      </div>
    </section>
  )
}

function ProposeModal({ occurrence: o, onClose }: { occurrence: Occurrence; onClose: () => void }) {
  const toast = useToast()
  const updated = useOccurrenceUpdated(o.id)
  const [amount, setAmount] = useState<number | null>(o.chargeDecision === 'Pending' ? o.proposedChargeAmount ?? null : null)
  const [justification, setJustification] = useState('')
  const [member, setMember] = useState<{ id: string; name: string } | null>(o.memberId ? { id: o.memberId, name: o.memberName ?? 'Associado' } : null)
  const propose = useMutation({
    mutationFn: () => api.post<Occurrence>(`/api/ocorrencias/${o.id}/cobranca`, { amount: amount ?? 0, justification: justification.trim(), memberId: member?.id ?? null } satisfies Schema<'ProposeChargeRequest'>),
    onSuccess: (saved) => { toast.success('Cobrança proposta. Aguardando aprovação administrativa.'); updated(saved); onClose() },
    onError: toast.error,
  })
  const invalid = !(amount != null && amount > 0) || justification.trim().length < 5 || !member

  return (
    <Modal open onClose={onClose} title={`Propor cobrança — ${o.code}`}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={propose.isPending} disabled={invalid} onClick={() => propose.mutate()}>Enviar para aprovação</Button>
      </>}>
      <div className="space-y-4">
        <Alert tone="info">A proposta <strong>não gera cobrança imediatamente</strong>: ela só chega ao financeiro depois de aprovada por um usuário com permissão para aprovar cobranças.</Alert>
        <Field label="Associado responsável" required><MemberPicker value={member} onChange={setMember} onlyActive={false} /></Field>
        <Field label="Valor" required><MoneyInput value={amount} onChange={setAmount} /></Field>
        <Field label="Justificativa" required hint="Mínimo de 5 caracteres. Fica registrada no histórico e na auditoria.">
          <Textarea rows={3} maxLength={1000} placeholder="Ex.: reposição de 2 cadeiras quebradas conforme orçamento" value={justification} onChange={(e) => setJustification(e.target.value)} />
        </Field>
      </div>
    </Modal>
  )
}
