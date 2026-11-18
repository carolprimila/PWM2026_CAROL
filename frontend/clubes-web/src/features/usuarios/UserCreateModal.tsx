import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, Checkbox, Field, Input, Modal, Select } from '@/components/ui'
import { api, ApiError, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'

type Role = Schema<'RoleDto'>
type TemporaryPassword = Schema<'TemporaryPasswordDto'>

const schema = z.object({
  name: z.string().trim().min(3, 'Informe o nome').max(150, 'Máximo de 150 caracteres'),
  email: z.string().trim().email('E-mail inválido'),
  kind: z.enum(['Staff', 'ClubAdmin']),
  employeeId: z.string(),
  roleIds: z.array(z.string()),
})
type Values = z.infer<typeof schema>

/**
 * Criação de usuário da equipe (funcionário ou administrador). Com `employee`, o usuário nasce vinculado ao funcionário.
 * O servidor devolve a senha temporária uma única vez — quem chama exibe o `TemporaryPasswordModal`.
 */
export function UserCreateModal({ employee, onClose, onCreated }: {
  employee?: { id: string; fullName: string; email?: string | null }; onClose: () => void; onCreated: (result: TemporaryPassword) => void
}) {
  const { can, isAdmin } = useAuth()
  const queryClient = useQueryClient()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: employee?.fullName ?? '', email: employee?.email ?? '', kind: 'Staff', employeeId: employee?.id ?? '', roleIds: [] },
  })
  const { errors } = form.formState
  const kind = form.watch('kind')
  const roleIds = form.watch('roleIds')

  const roles = useQuery({ queryKey: ['/api/funcoes'], queryFn: () => api.get<Role[]>('/api/funcoes'), enabled: can('users.view', 'roles.manage') })
  // Funcionários ativos ainda sem usuário, para vincular o acesso ao cadastro.
  const employees = useQuery({
    queryKey: ['/api/funcionarios', 'sem-usuario'],
    queryFn: () => api.get<Paged<Schema<'EmployeeDto'>>>('/api/funcionarios', { status: 'Active', pageSize: 200 }),
    enabled: !employee && can('employees.view'),
  })
  const available = employees.data?.items.filter((e) => !e.userId) ?? []

  const create = useMutation({
    mutationFn: (values: Values) => api.post<TemporaryPassword>('/api/usuarios', {
      name: values.name, email: values.email, kind: values.kind,
      employeeId: values.kind === 'Staff' && values.employeeId ? values.employeeId : null,
      roleIds: values.kind === 'Staff' ? values.roleIds : [],
    }),
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ['/api/usuarios'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/funcionarios'] })
      void queryClient.invalidateQueries({ queryKey: ['funcionario'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/funcoes'] })
      onCreated(result)
    },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
    },
  })

  return (
    <Modal open onClose={onClose} title={employee ? 'Criar usuário de acesso' : 'Novo usuário'}
      description={employee ? `Acesso ao painel para ${employee.fullName}.` : 'Usuários da equipe acessam o painel administrativo conforme as funções atribuídas.'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={create.isPending} onClick={form.handleSubmit((values) => create.mutate(values))}>Criar usuário</Button>
      </>}>
      <form className="space-y-5" noValidate onSubmit={(e) => e.preventDefault()}>
        {create.error && !(create.error instanceof ApiError && create.error.errors) ? <Alert tone="danger">{(create.error as Error).message}</Alert> : null}
        <div className="grid grid-cols-2 gap-4">
          <Field className="col-span-2" label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} {...form.register('name')} /></Field>
          <Field className="col-span-2" label="E-mail (login)" required error={errors.email?.message} hint="A senha temporária é gerada pelo sistema e deve ser trocada no primeiro acesso.">
            <Input type="email" aria-invalid={!!errors.email} {...form.register('email')} />
          </Field>
          {!employee && (
            <Field label="Tipo de usuário" required>
              <Select {...form.register('kind')}>
                <option value="Staff">Funcionário</option>
                {isAdmin && <option value="ClubAdmin">Administrador</option>}
              </Select>
            </Field>
          )}
          {!employee && kind === 'Staff' && can('employees.view') && (
            <Field label="Funcionário vinculado" hint="Opcional. Funcionários ativos sem usuário.">
              <Select {...form.register('employeeId')}>
                <option value="">Sem vínculo</option>
                {available.map((e) => <option key={e.id} value={e.id}>{e.fullName}</option>)}
              </Select>
            </Field>
          )}
        </div>

        {kind === 'ClubAdmin' ? <Alert tone="warning">Administradores têm acesso a todas as funcionalidades do clube, sem restrição por função.</Alert> : (
          <fieldset>
            <legend className="mb-2 text-[13px] font-medium text-ink">Funções</legend>
            {roles.isLoading ? <p className="text-sm text-muted">Carregando funções…</p>
              : !roles.data?.length ? <p className="text-sm text-muted">Nenhuma função disponível. O usuário será criado sem permissões até que uma função seja atribuída.</p> : (
                <div className="grid grid-cols-2 gap-x-4 gap-y-2.5 rounded-(--radius-control) border border-line p-4">
                  {roles.data.map((role) => (
                    <Checkbox key={role.id} label={role.name} checked={roleIds.includes(role.id)}
                      onChange={(e) => form.setValue('roleIds', e.target.checked ? [...roleIds, role.id] : roleIds.filter((r) => r !== role.id), { shouldDirty: true })} />
                  ))}
                </div>
              )}
          </fieldset>
        )}
      </form>
    </Modal>
  )
}
