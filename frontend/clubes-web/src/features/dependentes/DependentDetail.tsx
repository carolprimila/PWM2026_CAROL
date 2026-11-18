import { Ban, CheckCircle2, KeyRound, Pencil, RotateCcw, Trash2, XCircle } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Attachments, HistoryList } from '@/components/shared'
import { Alert, Avatar, Badge, Button, Field, Info, Input, Menu, Modal, QueryBoundary, StatusBadge, Switch, Tabs, useConfirm, useToast } from '@/components/ui'
import { TemporaryPasswordModal } from '@/features/associados/MemberDetail'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { DependentForm } from './DependentForm'

type Dependent = Schema<'DependentDto'>
type Tab = 'dados' | 'permissoes' | 'documentos' | 'historico'

/** Detalhe do dependente em modal: dados, permissões individuais, documentos e histórico de situação. */
export function DependentDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const manage = can('dependents.manage')
  const [tab, setTab] = useState<Tab>('dados')
  const [editing, setEditing] = useState(false)
  const [askingAccess, setAskingAccess] = useState(false)
  const [access, setAccess] = useState<Schema<'TemporaryPasswordDto'> | null>(null)

  const dependent = useQuery({ queryKey: ['dependente', id], queryFn: () => api.get<Dependent>(`/api/dependentes/${id}`) })
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['dependente', id] })
    void queryClient.invalidateQueries({ queryKey: ['/api/dependentes'] })
    void queryClient.invalidateQueries({ queryKey: ['historico', 'Dependent', id] })
    void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    if (dependent.data) void queryClient.invalidateQueries({ queryKey: ['associado', dependent.data.memberId] })
  }
  const action = useMutation({
    mutationFn: ({ path, body }: { path: string; body?: unknown; message: string }) => api.post(`/api/dependentes/${id}/${path}`, body),
    onSuccess: (_, variables) => { toast.success(variables.message); refresh() },
    onError: toast.error,
  })
  const setPermission = useMutation({
    mutationFn: (body: { code: string; value: boolean }) => api.put<Dependent>(`/api/dependentes/${id}/permissoes`, body),
    onSuccess: (updated) => { queryClient.setQueryData(['dependente', id], updated); void queryClient.invalidateQueries({ queryKey: ['/api/dependentes'] }); toast.success('Permissão atualizada.') },
    onError: toast.error,
  })
  const createAccess = useMutation({
    mutationFn: (email: string | null) => api.post<Schema<'TemporaryPasswordDto'>>(`/api/dependentes/${id}/acesso`, { email }),
    onSuccess: (result) => { setAskingAccess(false); setAccess(result); refresh() },
    onError: toast.error,
  })

  // Mudanças de situação com motivo: o servidor grava na auditoria e no histórico e notifica o associado.
  const withReason = async (title: string, path: string, message: string, danger = false, text?: string) => {
    const result = await confirm({ title, message: text, reason: 'required', danger, confirmLabel: 'Confirmar' })
    if (result) action.mutate({ path, body: { reason: result.reason }, message })
  }

  const d = dependent.data
  return (
    <>
      <Modal open onClose={onClose} size="lg" title={d?.fullName ?? 'Dependente'}
        description={d && <span className="flex items-center gap-2">{d.relationship} de {can('members.view') ? <Link to={`/associados/${d.memberId}`} className="font-medium text-brand-600 hover:underline">{d.memberName}</Link> : d.memberName}<StatusBadge dict={labels.dependentStatus} value={d.status} /></span>}>
        <QueryBoundary query={dependent}>
          {(d) => (
            <>
              {manage && d.status !== 'Removed' && (
                <div className="mb-4 flex flex-wrap items-center justify-end gap-2">
                  {d.status === 'PendingApproval' && (
                    <Button variant="primary" size="sm" icon={CheckCircle2} loading={action.isPending} onClick={async () => {
                      if (await confirm({ title: 'Aprovar dependente?', message: `${d.fullName} passará a ser dependente ativo(a) de ${d.memberName}.`, confirmLabel: 'Aprovar' })) action.mutate({ path: 'aprovar', message: 'Dependente aprovado.' })
                    }}>Aprovar</Button>
                  )}
                  <Button size="sm" icon={Pencil} onClick={() => setEditing(true)}>Editar</Button>
                  <Menu trigger={<Button size="sm">Mais ações</Button>} items={[
                    d.status === 'Active' && !d.hasAccess && { label: 'Criar acesso ao aplicativo', icon: KeyRound, onClick: () => setAskingAccess(true) },
                    d.status === 'PendingApproval' && { label: 'Rejeitar cadastro', icon: XCircle, danger: true, onClick: () => withReason('Rejeitar dependente', 'rejeitar', 'Cadastro não aprovado.', true, 'O motivo é enviado ao associado responsável.') },
                    d.status === 'Active' && { label: 'Suspender', icon: Ban, onClick: () => withReason('Suspender dependente', 'suspender', 'Dependente suspenso.', true) },
                    d.status === 'Suspended' && { label: 'Reativar', icon: RotateCcw, onClick: () => withReason('Reativar dependente', 'reativar', 'Dependente reativado.') },
                    { label: 'Remover dependente', icon: Trash2, danger: true, onClick: () => withReason('Remover dependente', 'remover', 'Dependente removido.', true, 'O vínculo é encerrado, o acesso ao aplicativo é revogado e as cobranças futuras deixam de considerá-lo.') },
                  ]} />
                </div>
              )}
              {d.statusReason && d.status !== 'Active' && <Alert className="mb-4" tone={d.status === 'PendingApproval' ? 'info' : 'warning'} title="Motivo da situação atual">{d.statusReason}</Alert>}

              <Tabs<Tab> value={tab} onChange={setTab} tabs={[
                { key: 'dados', label: 'Dados' },
                { key: 'permissoes', label: 'Permissões' },
                ...(can('documents.view') ? [{ key: 'documentos' as const, label: 'Documentos' }] : []),
                { key: 'historico', label: 'Histórico' },
              ]} />

              {tab === 'dados' && (
                <div>
                  <div className="mb-5 flex items-center gap-4">
                    <Avatar fileId={d.photoFileId} name={d.fullName} size={64} />
                    <div><p className="text-base font-semibold text-ink">{d.fullName}</p><p className="text-sm text-muted">Cadastrado em {fmt.date(d.createdAt)}</p></div>
                  </div>
                  <dl className="grid grid-cols-3 gap-x-6 gap-y-4">
                    <Info label="Parentesco">{d.relationship}</Info>
                    <Info label="Nascimento">{fmt.day(d.birthDate)}</Info>
                    <Info label="Gênero">{d.gender}</Info>
                    <Info label="CPF">{fmt.cpf(d.cpf)}</Info>
                    <Info label="RG">{d.rg}</Info>
                    <Info label="Telefone">{fmt.phone(d.phone)}</Info>
                    <Info label="E-mail" className="col-span-2">{d.email}</Info>
                    <Info label="Acesso ao aplicativo">{d.hasAccess ? <span className="flex flex-col items-start gap-1">{d.status === 'Removed' ? <Badge>Acesso revogado</Badge> : <Badge tone="success">Com acesso</Badge>}<span className="text-xs text-muted">{d.accessEmail}</span></span> : <Badge>Sem acesso</Badge>}</Info>
                  </dl>
                </div>
              )}

              {tab === 'permissoes' && (
                <div className="space-y-4">
                  <Alert tone="info">A permissão só vale quando é liberada pela administração e habilitada pelo associado responsável. Se o clube não permite que o associado gerencie as permissões, a liberação da administração já a torna efetiva.</Alert>
                  <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
                    {d.permissions.map((p) => (
                      <li key={p.code} className="flex flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3.5">
                        <p className="min-w-0 flex-1 basis-64 text-sm font-medium text-ink">{p.description}</p>
                        <Switch checked={p.allowedByAdmin} disabled={!manage || d.status === 'Removed' || setPermission.isPending} label="Liberada pela administração"
                          onChange={(value) => setPermission.mutate({ code: p.code, value })} />
                        <div className="flex items-center gap-2">
                          <Badge tone={p.enabledByMember ? 'info' : 'neutral'}>{p.enabledByMember ? 'Habilitada pelo associado' : 'Não habilitada pelo associado'}</Badge>
                          <Badge tone={p.effective ? 'success' : 'neutral'}>{p.effective ? 'Efetiva' : 'Não efetiva'}</Badge>
                        </div>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {tab === 'documentos' && <Attachments ownerType="Dependent" ownerId={d.id} ownerMemberId={d.memberId} canEdit={manage && d.status !== 'Removed'} />}
              {tab === 'historico' && <HistoryList entityType="Dependent" id={d.id} labelOf={(s) => labels.dependentStatus[s as keyof typeof labels.dependentStatus]?.label ?? s} />}
            </>
          )}
        </QueryBoundary>
      </Modal>

      {editing && d && <DependentForm dependent={d} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
      {askingAccess && d && <AccessModal dependent={d} loading={createAccess.isPending} onClose={() => setAskingAccess(false)} onConfirm={(email) => createAccess.mutate(email)} />}
      <TemporaryPasswordModal access={access} onClose={() => setAccess(null)} />
    </>
  )
}

/** O login usa o e-mail informado aqui ou, se vazio, o e-mail do cadastro do dependente. */
function AccessModal({ dependent, loading, onClose, onConfirm }: { dependent: Dependent; loading: boolean; onClose: () => void; onConfirm: (email: string | null) => void }) {
  const [email, setEmail] = useState(dependent.email ?? '')
  const [error, setError] = useState('')
  const submit = () => {
    const value = emptyToNull(email)
    if (!value && !dependent.email) { setError('Informe um e-mail para o acesso'); return }
    if (value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) { setError('E-mail inválido'); return }
    onConfirm(value)
  }
  return (
    <Modal open onClose={onClose} size="sm" title="Criar acesso ao aplicativo"
      footer={<><Button onClick={onClose}>Cancelar</Button><Button variant="primary" loading={loading} onClick={submit}>Criar acesso</Button></>}>
      <p className="mb-4 text-sm text-ink/80">{dependent.fullName} receberá um login com senha temporária, de troca obrigatória no primeiro acesso.</p>
      <Field label="E-mail de acesso" required error={error}>
        <Input type="email" autoFocus value={email} aria-invalid={!!error} onChange={(e) => { setEmail(e.target.value); setError('') }} onKeyDown={(e) => { if (e.key === 'Enter') submit() }} />
      </Field>
    </Modal>
  )
}
