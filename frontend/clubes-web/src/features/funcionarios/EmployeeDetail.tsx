import { KeyRound, Pencil, Power, RotateCcw } from 'lucide-react'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Attachments, HistoryList } from '@/components/shared'
import { Badge, Button, Info, Modal, QueryBoundary, StatusBadge, Tabs, useConfirm, useToast } from '@/components/ui'
import { TemporaryPasswordModal } from '@/features/associados/MemberDetail'
import { UserCreateModal } from '@/features/usuarios/UserCreateModal'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { EmployeeForm } from './EmployeeForm'

type Employee = Schema<'EmployeeDto'>
type Tab = 'dados' | 'documentos' | 'historico'

export function EmployeeDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const manage = can('employees.manage')
  const [tab, setTab] = useState<Tab>('dados')
  const [editing, setEditing] = useState(false)
  const [creatingUser, setCreatingUser] = useState(false)
  const [access, setAccess] = useState<Schema<'TemporaryPasswordDto'> | null>(null)

  const employee = useQuery({ queryKey: ['funcionario', id], queryFn: () => api.get<Employee>(`/api/funcionarios/${id}`) })
  const setStatus = useMutation({
    mutationFn: ({ path, reason }: { path: 'ativar' | 'desativar'; reason: string }) => api.post<Employee>(`/api/funcionarios/${id}/${path}`, { reason }),
    onSuccess: (saved, variables) => {
      toast.success(variables.path === 'ativar' ? 'Funcionário ativado.' : 'Funcionário desativado.')
      queryClient.setQueryData(['funcionario', id], saved)
      void queryClient.invalidateQueries({ queryKey: ['/api/funcionarios'] })
      void queryClient.invalidateQueries({ queryKey: ['historico', 'Employee', id] })
    },
    onError: toast.error,
  })
  const changeStatus = async (e: Employee) => {
    const activate = e.status === 'Inactive'
    const result = await confirm({
      title: activate ? 'Ativar funcionário' : 'Desativar funcionário', reason: 'required', danger: !activate, confirmLabel: activate ? 'Ativar' : 'Desativar',
      message: activate ? undefined : e.userId ? 'O login do usuário vinculado passa a ser recusado e as sessões abertas são encerradas.' : undefined,
    })
    if (result) setStatus.mutate({ path: activate ? 'ativar' : 'desativar', reason: result.reason })
  }

  const e = employee.data
  return (
    <>
      <Modal open onClose={onClose} size="lg" title={e?.fullName ?? 'Funcionário'}
        description={e && <span className="flex items-center gap-2">{[e.position, e.department].filter(Boolean).join(' · ') || 'Sem cargo informado'}<StatusBadge dict={labels.employeeStatus} value={e.status} /></span>}>
        <QueryBoundary query={employee}>
          {(e) => (
            <>
              {manage && (
                <div className="mb-4 flex flex-wrap items-center justify-end gap-2">
                  <Button size="sm" icon={Pencil} onClick={() => setEditing(true)}>Editar</Button>
                  <Button size="sm" icon={e.status === 'Active' ? Power : RotateCcw} loading={setStatus.isPending} onClick={() => changeStatus(e)}>{e.status === 'Active' ? 'Desativar' : 'Ativar'}</Button>
                </div>
              )}
              <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'dados', label: 'Dados' }, { key: 'documentos', label: 'Documentos' }, { key: 'historico', label: 'Histórico' }]} />

              {tab === 'dados' && (
                <div className="space-y-6">
                  <dl className="grid grid-cols-3 gap-x-6 gap-y-4">
                    <Info label="CPF">{fmt.cpf(e.cpf)}</Info>
                    <Info label="Cargo">{e.position}</Info>
                    <Info label="Setor">{e.department}</Info>
                    <Info label="Admissão">{fmt.day(e.hiredAt)}</Info>
                    <Info label="Telefone">{fmt.phone(e.phone)}</Info>
                    <Info label="Cadastrado em">{fmt.date(e.createdAt)}</Info>
                    <Info label="E-mail" className="col-span-3">{e.email}</Info>
                  </dl>
                  <div className="flex items-center justify-between gap-4 rounded-(--radius-control) border border-line px-4 py-3.5">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-ink">Usuário de acesso</p>
                      {e.userId ? (
                        <div className="mt-1 flex flex-wrap items-center gap-1.5 text-sm text-ink/80">
                          {e.userEmail}
                          {e.roles.length === 0 ? <Badge tone="warning">Sem função</Badge> : e.roles.map((role) => <Badge key={role}>{role}</Badge>)}
                        </div>
                      ) : <p className="mt-0.5 text-[13px] text-muted">Este funcionário ainda não possui login no painel.</p>}
                    </div>
                    {!e.userId && can('users.manage') && e.status === 'Active' && <Button size="sm" icon={KeyRound} onClick={() => setCreatingUser(true)}>Criar usuário de acesso</Button>}
                  </div>
                </div>
              )}
              {tab === 'documentos' && <Attachments ownerType="Employee" ownerId={e.id} canEdit={manage} />}
              {tab === 'historico' && <HistoryList entityType="Employee" id={e.id} labelOf={(s) => labels.employeeStatus[s as keyof typeof labels.employeeStatus]?.label ?? s} />}
            </>
          )}
        </QueryBoundary>
      </Modal>

      {editing && e && <EmployeeForm employee={e} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
      {creatingUser && e && <UserCreateModal employee={e} onClose={() => setCreatingUser(false)} onCreated={(result) => { setCreatingUser(false); setAccess(result) }} />}
      <TemporaryPasswordModal access={access} onClose={() => setAccess(null)} />
    </>
  )
}
