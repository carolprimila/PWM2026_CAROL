import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, Pencil, Plus, ShieldCheck, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import type { Feature } from '@/app/features'
import { DataTable, type Column } from '@/components/table'
import { Alert, Badge, Button, Card, Checkbox, Field, IconButton, Input, Modal, PageHeader, QueryBoundary, useConfirm, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'

type Role = Schema<'RoleDto'>
type PermissionInfo = Schema<'PermissionInfoDto'>

function RolesPage() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const manage = can('roles.manage')
  const [editing, setEditing] = useState<Role | 'new' | null>(null)
  const roles = useQuery({ queryKey: ['/api/funcoes'], queryFn: () => api.get<Role[]>('/api/funcoes') })
  const remove = useMutation({
    mutationFn: (id: string) => api.delete(`/api/funcoes/${id}`),
    onSuccess: () => { toast.success('Função excluída.'); void queryClient.invalidateQueries({ queryKey: ['/api/funcoes'] }) }, onError: toast.error,
  })

  const columns: Column<Role>[] = [
    {
      key: 'name', header: 'Função',
      render: (r) => <div className="min-w-0"><p className="flex items-center gap-2 font-medium text-ink">{r.name}{r.isSystem && <Badge tone="brand">Padrão</Badge>}</p><p className="truncate text-xs text-muted">{r.description || 'Sem descrição'}</p></div>,
    },
    { key: 'permissions', header: 'Permissões', align: 'center', width: '140px', render: (r) => r.permissions.length },
    { key: 'users', header: 'Usuários', align: 'center', width: '140px', render: (r) => r.users },
  ]

  return (
    <>
      <PageHeader title="Funções e permissões" subtitle="Perfis de acesso da equipe. Cada função reúne as permissões que os usuários vinculados recebem."
        actions={manage && <Button variant="primary" icon={Plus} onClick={() => setEditing('new')}>Nova função</Button>} />
      <Card padded={false}>
        <DataTable columns={columns} rows={roles.data ?? []} rowKey={(r) => r.id} isLoading={roles.isLoading} error={roles.error} onRetry={() => roles.refetch()}
          onRowClick={(r) => setEditing(r)}
          empty={{ title: 'Nenhuma função cadastrada', description: manage ? 'Crie a primeira função para distribuir permissões à equipe.' : undefined }}
          rowActions={(r) => (
            <div className="flex justify-end">
              <IconButton icon={manage ? Pencil : Eye} label={manage ? 'Editar' : 'Ver permissões'} onClick={() => setEditing(r)} />
              {manage && <IconButton icon={Trash2} label="Excluir" disabled={remove.isPending} onClick={async () => {
                if (await confirm({ title: 'Excluir função?', danger: true, confirmLabel: 'Excluir', message: r.users > 0 ? `Há ${r.users} usuário(s) vinculado(s) a "${r.name}". O servidor só permite excluir funções sem usuários.` : `A função "${r.name}" será excluída definitivamente.` })) remove.mutate(r.id)
              }} />}
            </div>
          )} />
      </Card>
      {editing && <RoleForm role={editing === 'new' ? undefined : editing} readOnly={!manage} onClose={() => setEditing(null)} />}
    </>
  )
}

const schema = z.object({
  name: z.string().trim().min(2, 'Informe o nome da função').max(80, 'Máximo de 80 caracteres'),
  description: z.string().max(300, 'Máximo de 300 caracteres'),
  permissions: z.array(z.string()),
})
type Values = z.infer<typeof schema>

function RoleForm({ role, readOnly, onClose }: { role?: Role; readOnly: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const catalog = useQuery({ queryKey: ['/api/funcoes/permissoes'], queryFn: () => api.get<PermissionInfo[]>('/api/funcoes/permissoes'), staleTime: Infinity })
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { name: role?.name ?? '', description: role?.description ?? '', permissions: role?.permissions ?? [] } })
  const { errors } = form.formState
  const selected = form.watch('permissions')
  const setSelected = (codes: string[]) => form.setValue('permissions', codes, { shouldDirty: true })

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body = { name: values.name, description: values.description.trim() || null, permissions: values.permissions }
      return role ? api.put<Role>(`/api/funcoes/${role.id}`, body) : api.post<Role>('/api/funcoes', body)
    },
    onSuccess: () => {
      toast.success(role ? 'Função atualizada.' : 'Função criada.')
      void queryClient.invalidateQueries({ queryKey: ['/api/funcoes'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/usuarios'] })
      onClose()
    },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
    },
  })

  return (
    <Modal open onClose={onClose} size="xl" title={role ? (readOnly ? role.name : 'Editar função') : 'Nova função'}
      description={role && role.users > 0 ? `${role.users} usuário(s) vinculado(s) — as alterações valem para todos eles.` : undefined}
      footer={readOnly ? <Button onClick={onClose}>Fechar</Button> : <>
        <span className="mr-auto text-[13px] text-muted">{selected.length} permissão(ões) selecionada(s)</span>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{role ? 'Salvar alterações' : 'Criar função'}</Button>
      </>}>
      <form noValidate onSubmit={(e) => e.preventDefault()}>
        <fieldset disabled={readOnly} className="space-y-6">
          {save.error && !(save.error instanceof ApiError && save.error.errors) ? <Alert tone="danger">{(save.error as Error).message}</Alert> : null}
          <div className="grid grid-cols-3 gap-4">
            <Field label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} {...form.register('name')} /></Field>
            <Field className="col-span-2" label="Descrição" error={errors.description?.message}><Input {...form.register('description')} /></Field>
          </div>

          <QueryBoundary query={catalog}>
            {(permissions) => {
              const groups = [...new Set(permissions.map((p) => p.group))]
              return (
                <div>
                  <div className="mb-3 flex items-center justify-between">
                    <p className="text-xs font-semibold tracking-wider text-muted uppercase">Permissões</p>
                    {!readOnly && (
                      <div className="flex gap-2">
                        <Button size="sm" variant="ghost" onClick={() => setSelected(permissions.map((p) => p.code))}>Marcar todas</Button>
                        <Button size="sm" variant="ghost" onClick={() => setSelected([])}>Limpar</Button>
                      </div>
                    )}
                  </div>
                  <div className="grid gap-4 lg:grid-cols-2">
                    {groups.map((group) => {
                      const items = permissions.filter((p) => p.group === group)
                      const codes = items.map((p) => p.code)
                      const count = codes.filter((code) => selected.includes(code)).length
                      return (
                        <section key={group} className="rounded-(--radius-control) border border-line">
                          <header className="flex items-center justify-between border-b border-line bg-canvas/60 px-4 py-2.5">
                            <Checkbox checked={count === codes.length} title="Marcar grupo"
                              onChange={(e) => setSelected(e.target.checked ? [...new Set([...selected, ...codes])] : selected.filter((code) => !codes.includes(code)))}
                              label={<span className="font-semibold text-ink">{group}</span>} />
                            <span className="text-xs text-muted">{count} de {codes.length}</span>
                          </header>
                          <ul className="space-y-2.5 px-4 py-3">
                            {items.map((p) => (
                              <li key={p.code}>
                                <Checkbox checked={selected.includes(p.code)} onChange={(e) => setSelected(e.target.checked ? [...selected, p.code] : selected.filter((code) => code !== p.code))}
                                  label={<span className="text-ink">{p.description}</span>} />
                              </li>
                            ))}
                          </ul>
                        </section>
                      )
                    })}
                  </div>
                </div>
              )
            }}
          </QueryBoundary>
        </fieldset>
      </form>
    </Modal>
  )
}

const feature: Feature = {
  nav: [{ to: '/funcoes', label: 'Funções e permissões', icon: ShieldCheck, group: 'Administração', order: 2, permissions: ['roles.manage', 'users.view'] }],
  routes: [{ path: 'funcoes', component: RolesPage, permissions: ['roles.manage', 'users.view'] }],
}
export default feature
