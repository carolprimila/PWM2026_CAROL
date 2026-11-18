import { zodResolver } from '@hookform/resolvers/zod'
import { KeyRound, Lock, LogOut, MonitorSmartphone, Unlock } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Badge, Button, Checkbox, cn, EmptyState, ErrorState, Field, IconButton, Info, Input, LoadingState, Modal, QueryBoundary, StatusBadge, Tabs, useConfirm, useToast } from '@/components/ui'
import { TemporaryPasswordModal } from '@/features/associados/MemberDetail'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'

type User = Schema<'UserDto'>
type Role = Schema<'RoleDto'>
type Tab = 'dados' | 'funcoes' | 'permissoes' | 'sessoes'

const schema = z.object({
  name: z.string().trim().min(3, 'Informe o nome').max(150, 'Máximo de 150 caracteres'),
  email: z.string().trim().email('E-mail inválido'),
})
type Values = z.infer<typeof schema>

export function UserDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const { can, user: me } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [tab, setTab] = useState<Tab>('dados')
  const [access, setAccess] = useState<Schema<'TemporaryPasswordDto'> | null>(null)
  const manage = can('users.manage')

  const user = useQuery({ queryKey: ['usuario', id], queryFn: () => api.get<User>(`/api/usuarios/${id}`) })
  const roles = useQuery({ queryKey: ['/api/funcoes'], queryFn: () => api.get<Role[]>('/api/funcoes') })
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['usuario', id] })
    void queryClient.invalidateQueries({ queryKey: ['/api/usuarios'] })
  }
  const updated = (saved: User) => {
    queryClient.setQueryData(['usuario', id], saved)
    void queryClient.invalidateQueries({ queryKey: ['/api/usuarios'] })
    void queryClient.invalidateQueries({ queryKey: ['/api/funcoes'] })
    void queryClient.invalidateQueries({ queryKey: ['/api/funcionarios'] })
  }

  const block = useMutation({ mutationFn: (reason: string) => api.post(`/api/usuarios/${id}/bloquear`, { reason }), onSuccess: () => { toast.success('Usuário bloqueado. As sessões foram encerradas.'); refresh(); void queryClient.invalidateQueries({ queryKey: ['usuario-sessoes', id] }) }, onError: toast.error })
  const unblock = useMutation({ mutationFn: () => api.post(`/api/usuarios/${id}/desbloquear`), onSuccess: () => { toast.success('Usuário desbloqueado.'); refresh() }, onError: toast.error })
  const reset = useMutation({ mutationFn: () => api.post<Schema<'TemporaryPasswordDto'>>(`/api/usuarios/${id}/redefinir-senha`), onSuccess: (result) => { setAccess(result); refresh() }, onError: toast.error })

  const u = user.data
  return (
    <>
      <Modal open onClose={onClose} size="lg" title={u?.name ?? 'Usuário'}
        description={u && <span className="flex items-center gap-2">{u.email}<StatusBadge dict={labels.userKind} value={u.kind} />{u.isBlocked && <Badge tone="danger">Bloqueado</Badge>}</span>}>
        <QueryBoundary query={user}>
          {(u) => (
            <>
              {manage && (
                <div className="mb-4 flex flex-wrap items-center justify-end gap-2">
                  <Button size="sm" icon={KeyRound} loading={reset.isPending} onClick={async () => {
                    if (await confirm({ title: 'Redefinir a senha?', message: `Uma nova senha temporária será gerada para ${u.name}; a senha atual deixa de valer e a troca será obrigatória no próximo acesso.`, confirmLabel: 'Redefinir senha' })) reset.mutate()
                  }}>Redefinir senha</Button>
                  {u.isBlocked
                    ? <Button size="sm" icon={Unlock} loading={unblock.isPending} onClick={async () => { if (await confirm({ title: 'Desbloquear usuário?', message: `${u.name} poderá entrar novamente.`, confirmLabel: 'Desbloquear' })) unblock.mutate() }}>Desbloquear</Button>
                    : u.id !== me?.id && <Button size="sm" icon={Lock} loading={block.isPending} onClick={async () => {
                      const result = await confirm({ title: 'Bloquear usuário', message: 'O usuário não conseguirá entrar e todas as sessões abertas serão encerradas.', reason: 'required', danger: true, confirmLabel: 'Bloquear' })
                      if (result) block.mutate(result.reason.slice(0, 300))
                    }}>Bloquear</Button>}
                </div>
              )}
              {u.isBlocked && <Alert className="mb-4" tone="danger" title="Usuário bloqueado">{u.blockReason}</Alert>}

              <Tabs<Tab> value={tab} onChange={setTab} tabs={[
                { key: 'dados', label: 'Dados' },
                { key: 'funcoes', label: 'Funções', count: u.roles.length },
                { key: 'permissoes', label: 'Permissões individuais', count: u.overrides.length },
                { key: 'sessoes', label: 'Sessões' },
              ]} />

              {tab === 'dados' && <DataTab key={`${u.name}|${u.email}`} user={u} canEdit={manage} onSaved={updated} />}
              {tab === 'funcoes' && (u.kind !== 'Staff' ? <StaffOnly kind={u.kind} /> : (
                <QueryBoundary query={roles}>{(all) => <RolesTab key={u.roles.map((r) => r.id).join()} user={u} roles={all} canEdit={can('roles.manage')} onSaved={updated} />}</QueryBoundary>
              ))}
              {tab === 'permissoes' && (u.kind !== 'Staff' ? <StaffOnly kind={u.kind} /> : (
                <QueryBoundary query={roles}>{(all) => <OverridesTab key={JSON.stringify(u.overrides)} user={u} roles={all} canEdit={can('roles.manage')} onSaved={updated} />}</QueryBoundary>
              ))}
              {tab === 'sessoes' && <SessionsTab userId={u.id} canRevoke={manage} />}
            </>
          )}
        </QueryBoundary>
      </Modal>
      <TemporaryPasswordModal access={access} onClose={() => setAccess(null)} />
    </>
  )
}

function StaffOnly({ kind }: { kind: Schema<'UserKind'> }) {
  return (
    <Alert tone="info" title="Não se aplica a este tipo de usuário">
      {kind === 'ClubAdmin' || kind === 'SuperAdmin'
        ? 'Administradores têm acesso a todas as funcionalidades; funções e permissões individuais valem apenas para usuários funcionários.'
        : 'Associados e dependentes usam o aplicativo; funções e permissões individuais valem apenas para usuários funcionários.'}
    </Alert>
  )
}

function DataTab({ user, canEdit, onSaved }: { user: User; canEdit: boolean; onSaved: (user: User) => void }) {
  const toast = useToast()
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { name: user.name, email: user.email } })
  const { errors, isDirty } = form.formState
  const save = useMutation({
    mutationFn: (values: Values) => api.put<User>(`/api/usuarios/${user.id}`, values),
    onSuccess: (saved) => { toast.success('Usuário atualizado.'); onSaved(saved) },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
      else toast.error(error)
    },
  })
  const link = user.employeeId ? 'Funcionário' : user.memberId ? 'Associado' : user.dependentId ? 'Dependente' : null
  return (
    <div className="space-y-6">
      {canEdit ? (
        <form className="grid grid-cols-2 gap-4" noValidate onSubmit={form.handleSubmit((values) => save.mutate(values))}>
          <Field label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} {...form.register('name')} /></Field>
          <Field label="E-mail (login)" required error={errors.email?.message}><Input type="email" aria-invalid={!!errors.email} {...form.register('email')} /></Field>
          <div className="col-span-2 flex justify-end"><Button type="submit" variant="primary" size="sm" disabled={!isDirty} loading={save.isPending}>Salvar alterações</Button></div>
        </form>
      ) : (
        <dl className="grid grid-cols-2 gap-x-6 gap-y-4"><Info label="Nome">{user.name}</Info><Info label="E-mail (login)">{user.email}</Info></dl>
      )}
      <dl className="grid grid-cols-3 gap-x-6 gap-y-4 border-t border-line pt-5">
        <Info label="Tipo"><StatusBadge dict={labels.userKind} value={user.kind} /></Info>
        <Info label="Último acesso">{user.lastLoginAt ? fmt.dateTime(user.lastLoginAt) : 'Nunca acessou'}</Info>
        <Info label="Cadastro vinculado">{link}</Info>
      </dl>
    </div>
  )
}

function RolesTab({ user, roles, canEdit, onSaved }: { user: User; roles: Role[]; canEdit: boolean; onSaved: (user: User) => void }) {
  const toast = useToast()
  const [selected, setSelected] = useState(() => user.roles.map((r) => r.id))
  const dirty = selected.length !== user.roles.length || selected.some((id) => !user.roles.some((r) => r.id === id))
  const save = useMutation({
    mutationFn: () => api.put<User>(`/api/usuarios/${user.id}/funcoes`, { roleIds: selected }),
    onSuccess: (saved) => { toast.success('Funções atualizadas.'); onSaved(saved) }, onError: toast.error,
  })
  if (roles.length === 0) return <EmptyState title="Nenhuma função cadastrada" description="Cadastre funções em Administração → Funções e permissões." />
  return (
    <div>
      <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
        {roles.map((role) => (
          <li key={role.id} className="px-4 py-3">
            <Checkbox disabled={!canEdit} checked={selected.includes(role.id)} onChange={(e) => setSelected((list) => (e.target.checked ? [...list, role.id] : list.filter((r) => r !== role.id)))}
              label={<span><span className="font-medium text-ink">{role.name}</span><span className="block text-xs text-muted">{role.description || 'Sem descrição'} · {role.permissions.length} permissão(ões)</span></span>} />
          </li>
        ))}
      </ul>
      {canEdit && <div className="mt-4 flex justify-end"><Button variant="primary" size="sm" disabled={!dirty} loading={save.isPending} onClick={() => save.mutate()}>Salvar funções</Button></div>}
    </div>
  )
}

type Choice = 'inherit' | 'grant' | 'deny'
const choices: { key: Choice; label: string }[] = [{ key: 'inherit', label: 'Herdar' }, { key: 'grant', label: 'Conceder' }, { key: 'deny', label: 'Negar' }]

/** Exceções por usuário sobre as funções: herdar (sem exceção), conceder ou negar cada permissão. */
function OverridesTab({ user, roles, canEdit, onSaved }: { user: User; roles: Role[]; canEdit: boolean; onSaved: (user: User) => void }) {
  const toast = useToast()
  const catalog = useQuery({ queryKey: ['/api/funcoes/permissoes'], queryFn: () => api.get<Schema<'PermissionInfoDto'>[]>('/api/funcoes/permissoes'), staleTime: Infinity })
  const [overrides, setOverrides] = useState<Record<string, boolean>>(() => Object.fromEntries(user.overrides.map((o) => [o.permission, o.granted])))
  const inherited = new Set(roles.filter((role) => user.roles.some((r) => r.id === role.id)).flatMap((role) => role.permissions))
  const original = Object.fromEntries(user.overrides.map((o) => [o.permission, o.granted]))
  const dirty = JSON.stringify(Object.entries(overrides).sort()) !== JSON.stringify(Object.entries(original).sort())
  const save = useMutation({
    mutationFn: () => api.put<User>(`/api/usuarios/${user.id}/permissoes`, { overrides: Object.entries(overrides).map(([permission, granted]) => ({ permission, granted })) }),
    onSuccess: (saved) => { toast.success('Permissões individuais atualizadas.'); onSaved(saved) }, onError: toast.error,
  })
  const choose = (code: string, choice: Choice) => setOverrides((current) => {
    const next = { ...current }
    if (choice === 'inherit') delete next[code]; else next[code] = choice === 'grant'
    return next
  })

  if (catalog.isLoading) return <LoadingState />
  if (catalog.error) return <ErrorState error={catalog.error} onRetry={() => catalog.refetch()} />
  const groups = [...new Set(catalog.data!.map((p) => p.group))]
  return (
    <div>
      <Alert tone="info" className="mb-4">As funções definem as permissões herdadas. Use "Conceder" ou "Negar" apenas para exceções deste usuário — elas prevalecem sobre as funções.</Alert>
      <div className="space-y-5">
        {groups.map((group) => (
          <section key={group}>
            <h3 className="mb-2 text-xs font-semibold tracking-wider text-muted uppercase">{group}</h3>
            <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
              {catalog.data!.filter((p) => p.group === group).map((p) => {
                const value: Choice = p.code in overrides ? (overrides[p.code] ? 'grant' : 'deny') : 'inherit'
                const effective = value === 'inherit' ? inherited.has(p.code) : value === 'grant'
                return (
                  <li key={p.code} className="flex items-center gap-4 px-4 py-2.5">
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-ink">{p.description}</p>
                      <p className="text-xs text-muted">Pelas funções: {inherited.has(p.code) ? 'concedida' : 'não concedida'}</p>
                    </div>
                    <Badge tone={effective ? 'success' : 'neutral'}>{effective ? 'Permitido' : 'Sem acesso'}</Badge>
                    <div className={cn('inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5', !canEdit && 'opacity-60')}>
                      {choices.map((choice) => (
                        <button key={choice.key} type="button" disabled={!canEdit} onClick={() => choose(p.code, choice.key)}
                          className={cn('rounded-lg px-2.5 py-1 text-[13px] font-medium transition-colors', value === choice.key
                            ? cn('bg-surface shadow-sm', choice.key === 'grant' ? 'text-success' : choice.key === 'deny' ? 'text-danger' : 'text-ink') : 'text-muted hover:text-ink')}>
                          {choice.label}
                        </button>
                      ))}
                    </div>
                  </li>
                )
              })}
            </ul>
          </section>
        ))}
      </div>
      {canEdit && (
        <div className="sticky bottom-0 -mx-6 mt-4 -mb-5 flex items-center justify-between gap-3 border-t border-line bg-surface px-6 py-3">
          <span className="text-[13px] text-muted">{Object.keys(overrides).length} exceção(ões) definida(s)</span>
          <Button variant="primary" size="sm" disabled={!dirty} loading={save.isPending} onClick={() => save.mutate()}>Salvar permissões</Button>
        </div>
      )}
    </div>
  )
}

function SessionsTab({ userId, canRevoke }: { userId: string; canRevoke: boolean }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const sessions = useQuery({ queryKey: ['usuario-sessoes', userId], queryFn: () => api.get<Schema<'SessionDto'>[]>(`/api/usuarios/${userId}/sessoes`) })
  const revoke = useMutation({
    mutationFn: (sessionId: string) => api.delete(`/api/usuarios/${userId}/sessoes/${sessionId}`),
    onSuccess: () => { toast.success('Sessão encerrada.'); void queryClient.invalidateQueries({ queryKey: ['usuario-sessoes', userId] }) }, onError: toast.error,
  })
  if (sessions.isLoading) return <LoadingState />
  if (sessions.error) return <ErrorState error={sessions.error} onRetry={() => sessions.refetch()} />
  if (sessions.data!.length === 0) return <EmptyState icon={MonitorSmartphone} title="Nenhuma sessão ativa" description="O usuário não está conectado em nenhum dispositivo." />
  return (
    <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
      {sessions.data!.map((s) => (
        <li key={s.id} className="flex items-center gap-3 px-4 py-3">
          <MonitorSmartphone className="size-5 shrink-0 text-muted" />
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-ink">{s.device || 'Dispositivo não identificado'}{s.ip ? ` · ${s.ip}` : ''}</p>
            <p className="text-xs text-muted">Iniciada em {fmt.dateTime(s.createdAt)} · último uso {fmt.dateTime(s.lastUsedAt)} · expira em {fmt.dateTime(s.expiresAt)}</p>
          </div>
          {canRevoke && <IconButton icon={LogOut} label="Encerrar sessão" disabled={revoke.isPending} onClick={async () => {
            if (await confirm({ title: 'Encerrar esta sessão?', message: 'O dispositivo precisará entrar novamente.', danger: true, confirmLabel: 'Encerrar' })) revoke.mutate(s.id)
          }} />}
        </li>
      ))}
    </ul>
  )
}
