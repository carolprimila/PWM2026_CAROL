import { ArrowLeft, Ban, CheckCircle2, Copy, KeyRound, Pencil, RotateCcw, UserX } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { DataTable, usePaged, type Column } from '@/components/table'
import { Attachments, HistoryList } from '@/components/shared'
import { Alert, Avatar, Badge, Button, Card, CardTitle, Info, Menu, Modal, PageHeader, QueryBoundary, StatusBadge, Tabs, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { MemberForm } from './MemberForm'

type Member = Schema<'MemberDto'>
type Tab = 'resumo' | 'dependentes' | 'financeiro' | 'documentos' | 'historico'

export default function MemberDetail() {
  const { id } = useParams<{ id: string }>()
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [tab, setTab] = useState<Tab>('resumo')
  const [editing, setEditing] = useState(false)
  const [access, setAccess] = useState<Schema<'TemporaryPasswordDto'> | null>(null)

  const member = useQuery({ queryKey: ['associado', id], queryFn: () => api.get<Member>(`/api/associados/${id}`) })
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['associado', id] })
    void queryClient.invalidateQueries({ queryKey: ['/api/associados'] })
    void queryClient.invalidateQueries({ queryKey: ['historico', 'Member', id] })
  }
  const action = useMutation({
    mutationFn: ({ path, body }: { path: string; body?: unknown; message: string }) => api.post<Member>(`/api/associados/${id}/${path}`, body),
    onSuccess: (_, variables) => { toast.success(variables.message); refresh() },
    onError: toast.error,
  })
  const createAccess = useMutation({
    mutationFn: () => api.post<Schema<'TemporaryPasswordDto'>>(`/api/associados/${id}/acesso`, {}),
    onSuccess: (result) => { setAccess(result); refresh() },
    onError: toast.error,
  })

  // Ações de situação exigem motivo: o servidor grava na auditoria e no histórico.
  const withReason = async (title: string, path: string, message: string, danger = false) => {
    const result = await confirm({ title, reason: 'required', danger, confirmLabel: 'Confirmar' })
    if (result) action.mutate({ path, body: { reason: result.reason }, message })
  }

  return (
    <QueryBoundary query={member}>
      {(m) => (
        <>
          <PageHeader
            back={<Link to="/associados" className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-muted hover:text-ink"><ArrowLeft className="size-3.5" />Associados</Link>}
            title={m.fullName}
            subtitle={<span className="flex items-center gap-2">Matrícula {m.number}<StatusBadge dict={labels.memberStatus} value={m.status} />{m.financial.isDelinquent && <Badge tone="danger">Inadimplente</Badge>}</span>}
            actions={<>
              {can('members.approve') && m.status === 'PendingApproval' && (
                <Button variant="primary" icon={CheckCircle2} loading={action.isPending} onClick={async () => {
                  if (await confirm({ title: 'Aprovar cadastro?', message: `${m.fullName} passará a ser associado(a) ativo(a).`, confirmLabel: 'Aprovar' })) action.mutate({ path: 'aprovar', message: 'Cadastro aprovado.' })
                }}>Aprovar cadastro</Button>
              )}
              {can('members.manage') && <Button icon={Pencil} onClick={() => setEditing(true)}>Editar</Button>}
              <Menu trigger={<Button>Mais ações</Button>} items={[
                can('members.manage') && !m.hasAccess && (m.status === 'Active' || m.status === 'Suspended') && { label: 'Criar acesso ao aplicativo', icon: KeyRound, onClick: () => createAccess.mutate() },
                can('members.approve') && m.status === 'Active' && { label: 'Suspender', icon: Ban, onClick: () => withReason('Suspender associado', 'suspender', 'Associado suspenso.', true) },
                can('members.approve') && (m.status === 'Suspended' || m.status === 'Cancelled') && { label: 'Reativar', icon: RotateCcw, onClick: () => withReason('Reativar associado', 'reativar', 'Associado reativado.') },
                can('members.approve') && m.status !== 'Cancelled' && { label: 'Cancelar associação', icon: UserX, danger: true, onClick: () => withReason('Cancelar associação', 'cancelar', 'Associação cancelada.', true) },
              ]} />
            </>}
          />

          <Tabs<Tab> value={tab} onChange={setTab} tabs={[
            { key: 'resumo', label: 'Resumo' },
            { key: 'dependentes', label: 'Dependentes', count: m.dependents },
            ...(can('finance.view') ? [{ key: 'financeiro' as const, label: 'Financeiro' }] : []),
            ...(can('documents.view') ? [{ key: 'documentos' as const, label: 'Documentos' }] : []),
            { key: 'historico', label: 'Histórico' },
          ]} />

          {tab === 'resumo' && (
            <div className="grid gap-6 xl:grid-cols-3">
              <Card className="xl:col-span-2">
                <div className="mb-5 flex items-center gap-4">
                  <Avatar fileId={m.photoFileId} name={m.fullName} size={72} />
                  <div><p className="text-lg font-semibold text-ink">{m.fullName}</p><p className="text-sm text-muted">Associado(a) desde {fmt.day(m.joinedAt)}</p></div>
                </div>
                <dl className="grid grid-cols-3 gap-x-6 gap-y-4">
                  <Info label="CPF">{fmt.cpf(m.cpf)}</Info>
                  <Info label="RG">{m.rg}</Info>
                  <Info label="Nascimento">{fmt.day(m.birthDate)}</Info>
                  <Info label="Gênero">{m.gender}</Info>
                  <Info label="Estado civil">{m.maritalStatus}</Info>
                  <Info label="Telefone">{fmt.phone(m.phone)}</Info>
                  <Info label="E-mail" className="col-span-3">{m.email}</Info>
                  <Info label="Endereço" className="col-span-3">
                    {[m.street, m.addressNumber, m.complement, m.district, m.city && `${m.city}${m.state ? `/${m.state}` : ''}`, m.zipCode].filter(Boolean).join(', ') || '—'}
                  </Info>
                  {m.notes && <Info label="Informações complementares" className="col-span-3"><span className="whitespace-pre-wrap">{m.notes}</span></Info>}
                </dl>
              </Card>
              <div className="space-y-6">
                <Card>
                  <CardTitle title="Situação financeira" />
                  {m.financial.isDelinquent ? (
                    <Alert tone="danger" title={`${m.financial.overdueCharges} cobrança(s) em atraso`}>Total em atraso: {fmt.money(m.financial.overdueAmount)} · mais antiga de {fmt.day(m.financial.oldestDueDate)}</Alert>
                  ) : <Alert tone="success" title="Em dia">Sem cobranças em atraso além da tolerância.</Alert>}
                  <dl className="mt-4 grid grid-cols-2 gap-4">
                    <Info label="Em aberto">{fmt.money(m.financial.openAmount)}</Info>
                    <Info label="Liberação excepcional">{m.financial.hasActiveRelease ? `Até ${fmt.day(m.financial.releaseValidUntil)}` : 'Não'}</Info>
                    <Info label="Acesso ao clube">{m.financial.accessBlocked ? <Badge tone="danger">Bloqueado</Badge> : <Badge tone="success">Liberado</Badge>}</Info>
                    <Info label="Reservas">{m.financial.reservationsBlocked ? <Badge tone="danger">Bloqueadas</Badge> : <Badge tone="success">Liberadas</Badge>}</Info>
                  </dl>
                </Card>
                <Card>
                  <CardTitle title="Acesso ao aplicativo" />
                  {m.hasAccess ? <p className="text-sm text-ink">Login: <span className="font-medium">{m.accessEmail}</span></p>
                    : <p className="text-sm text-muted">Este associado ainda não possui login no aplicativo.</p>}
                </Card>
              </div>
            </div>
          )}
          {tab === 'dependentes' && <DependentsTab memberId={m.id} />}
          {tab === 'financeiro' && <ChargesTab memberId={m.id} />}
          {tab === 'documentos' && <Card><Attachments ownerType="Member" ownerId={m.id} ownerMemberId={m.id} canEdit={can('members.manage')} /></Card>}
          {tab === 'historico' && <Card><HistoryList entityType="Member" id={m.id} labelOf={(s) => labels.memberStatus[s as keyof typeof labels.memberStatus]?.label ?? s} /></Card>}

          {editing && <MemberForm member={m} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
          <TemporaryPasswordModal access={access} onClose={() => setAccess(null)} />
        </>
      )}
    </QueryBoundary>
  )
}

/** A senha temporária é exibida uma única vez; o usuário é obrigado a trocá-la no primeiro acesso. */
export function TemporaryPasswordModal({ access, onClose, title = 'Acesso criado' }: { access: Schema<'TemporaryPasswordDto'> | null; onClose: () => void; title?: string }) {
  const toast = useToast()
  if (!access) return null
  return (
    <Modal open onClose={onClose} size="sm" title={title} footer={<Button variant="primary" onClick={onClose}>Concluir</Button>}>
      <div className="space-y-4">
        <Alert tone={access.emailSent ? 'success' : 'warning'}>
          {access.emailSent ? 'As credenciais foram enviadas para o e-mail do usuário.' : 'O envio de e-mail não está configurado. Entregue as credenciais abaixo ao usuário — a senha não será exibida novamente.'}
        </Alert>
        <dl className="space-y-3">
          <Info label="Login">{access.email}</Info>
          <Info label="Senha temporária">
            <span className="flex items-center gap-2">
              <code className="rounded-lg bg-canvas px-3 py-1.5 font-mono text-base tracking-wider">{access.temporaryPassword}</code>
              <Button size="sm" icon={Copy} onClick={() => navigator.clipboard.writeText(access.temporaryPassword).then(() => toast.success('Senha copiada.'))}>Copiar</Button>
            </span>
          </Info>
        </dl>
      </div>
    </Modal>
  )
}

function DependentsTab({ memberId }: { memberId: string }) {
  const list = usePaged<Schema<'DependentDto'>>('/api/dependentes', { memberId }, { pageSize: 50 })
  const columns: Column<Schema<'DependentDto'>>[] = [
    { key: 'name', header: 'Dependente', render: (d) => <div className="flex items-center gap-3"><Avatar fileId={d.photoFileId} name={d.fullName} size={32} /><span className="font-medium text-ink">{d.fullName}</span></div> },
    { key: 'relationship', header: 'Parentesco', render: (d) => d.relationship },
    { key: 'birth', header: 'Nascimento', render: (d) => fmt.day(d.birthDate) },
    { key: 'access', header: 'Aplicativo', render: (d) => (d.hasAccess ? <Badge tone="success">Com acesso</Badge> : <Badge>Sem acesso</Badge>) },
    { key: 'status', header: 'Situação', render: (d) => <StatusBadge dict={labels.dependentStatus} value={d.status} /> },
  ]
  return (
    <Card padded={false}>
      <div className="flex items-center justify-between border-b border-line px-5 py-3.5">
        <p className="text-sm text-muted">Cada associado pode ter dependentes até o limite configurado pelo clube.</p>
        <Link to={`/dependentes?memberId=${memberId}`}><Button size="sm">Gerenciar dependentes</Button></Link>
      </div>
      <DataTable columns={columns} rows={list.rows} rowKey={(d) => d.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()} empty={{ title: 'Nenhum dependente cadastrado' }} />
    </Card>
  )
}

function ChargesTab({ memberId }: { memberId: string }) {
  const list = usePaged<Schema<'ChargeDto'>>('/api/financeiro/cobrancas', { memberId }, { pageSize: 12 })
  const columns: Column<Schema<'ChargeDto'>>[] = [
    { key: 'description', header: 'Cobrança', render: (c) => <div><p className="font-medium text-ink">{c.description}</p><p className="text-xs text-muted">{labels.chargeType[c.type].label} · competência {fmt.competence(c.competence)}</p></div> },
    { key: 'due', header: 'Vencimento', render: (c) => <span className={c.isOverdue ? 'font-medium text-danger' : ''}>{fmt.day(c.dueDate)}</span> },
    { key: 'total', header: 'Total', align: 'right', render: (c) => fmt.money(c.total) },
    { key: 'paid', header: 'Pago', align: 'right', render: (c) => fmt.money(c.paidAmount) },
    { key: 'balance', header: 'Saldo', align: 'right', render: (c) => <span className="font-medium">{fmt.money(c.balance)}</span> },
    { key: 'status', header: 'Situação', render: (c) => <div className="flex gap-1.5"><StatusBadge dict={labels.chargeStatus} value={c.status} />{c.isOverdue && <Badge tone="danger">Vencida</Badge>}</div> },
  ]
  return (
    <Card padded={false}>
      <DataTable columns={columns} rows={list.rows} rowKey={(c) => c.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} empty={{ title: 'Nenhuma cobrança para este associado' }} />
    </Card>
  )
}
