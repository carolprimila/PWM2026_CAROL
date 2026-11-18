import { Plus, UserCog } from 'lucide-react'
import { useState } from 'react'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, PageHeader, SearchInput, Select, StatusBadge, Tabs } from '@/components/ui'
import { TemporaryPasswordModal } from '@/features/associados/MemberDetail'
import type { Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { UserCreateModal } from './UserCreateModal'
import { UserDetail } from './UserDetail'

type User = Schema<'UserDto'>
type Login = Schema<'LoginHistoryDto'>
type Tab = 'usuarios' | 'acessos'

function UsersPage() {
  const { can } = useAuth()
  const [tab, setTab] = useState<Tab>('usuarios')
  const [creating, setCreating] = useState(false)
  const [access, setAccess] = useState<Schema<'TemporaryPasswordDto'> | null>(null)
  return (
    <>
      <PageHeader title="Usuários e acessos" subtitle="Quem acessa o sistema, com quais funções, e o histórico de tentativas de login."
        actions={can('users.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo usuário</Button>} />
      <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'usuarios', label: 'Usuários' }, { key: 'acessos', label: 'Histórico de acessos' }]} />
      {tab === 'usuarios' ? <UsersTab /> : <LoginHistoryTab />}
      {creating && <UserCreateModal onClose={() => setCreating(false)} onCreated={(result) => { setCreating(false); setAccess(result) }} />}
      <TemporaryPasswordModal access={access} onClose={() => setAccess(null)} />
    </>
  )
}

function UsersTab() {
  const [kind, setKind] = useState('')
  const [openId, setOpenId] = useState<string | null>(null)
  const list = usePaged<User>('/api/usuarios', { kind: kind || undefined })
  const columns: Column<User>[] = [
    { key: 'name', header: 'Usuário', render: (u) => <div className="min-w-0"><p className="truncate font-medium text-ink">{u.name}</p><p className="truncate text-xs text-muted">{u.email}</p></div> },
    { key: 'kind', header: 'Tipo', render: (u) => <StatusBadge dict={labels.userKind} value={u.kind} /> },
    {
      key: 'roles', header: 'Funções',
      render: (u) => u.kind !== 'Staff' ? <span className="text-muted">—</span>
        : u.roles.length === 0 ? <span className="text-muted">Sem função</span>
        : <div className="flex flex-wrap items-center gap-1">{u.roles.map((r) => <Badge key={r.id}>{r.name}</Badge>)}{u.overrides.length > 0 && <Badge tone="info">{u.overrides.length} exceção(ões)</Badge>}</div>,
    },
    { key: 'lastLogin', header: 'Último acesso', render: (u) => (u.lastLoginAt ? fmt.dateTime(u.lastLoginAt) : <span className="text-muted">Nunca acessou</span>) },
    { key: 'status', header: 'Situação', render: (u) => (u.isBlocked ? <Badge tone="danger">Bloqueado</Badge> : <Badge tone="success">Ativo</Badge>) },
  ]
  return (
    <>
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Nome ou e-mail…" />
          <div className="w-52"><Select value={kind} onChange={(e) => setKind(e.target.value)}><option value="">Todos os tipos</option>{options(labels.userKind).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(u) => u.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(u) => setOpenId(u.id)}
          empty={{ title: 'Nenhum usuário encontrado', description: list.search || kind ? 'Ajuste a pesquisa ou o filtro.' : undefined }} />
      </Card>
      {openId && <UserDetail key={openId} id={openId} onClose={() => setOpenId(null)} />}
    </>
  )
}

const loginReasons: Record<string, string> = {
  usuario_inexistente: 'Usuário inexistente', senha_invalida: 'Senha inválida', bloqueio_temporario: 'Bloqueio temporário por tentativas',
  usuario_bloqueado: 'Usuário bloqueado', clube_inativo: 'Clube inativo', associado_sem_acesso: 'Cadastro de associado não ativo',
  dependente_sem_acesso: 'Cadastro de dependente não ativo', funcionario_inativo: 'Funcionário inativo',
}

/** Nome curto do navegador/aplicativo a partir do User-Agent (o texto completo fica na dica do campo). */
function browserName(userAgent: string) {
  const os = /Windows/.test(userAgent) ? 'Windows' : /Android/.test(userAgent) ? 'Android' : /iPhone|iPad|iOS/.test(userAgent) ? 'iOS' : /Mac OS X|Macintosh/.test(userAgent) ? 'macOS' : /Linux/.test(userAgent) ? 'Linux' : ''
  const name = /Edg\//.test(userAgent) ? 'Edge' : /OPR\//.test(userAgent) ? 'Opera' : /Firefox\//.test(userAgent) ? 'Firefox' : /Chrome\//.test(userAgent) ? 'Chrome'
    : /Safari\//.test(userAgent) ? 'Safari' : /Dart|okhttp|CFNetwork|Expo/i.test(userAgent) ? 'Aplicativo' : ''
  return [name, os].filter(Boolean).join(' · ') || userAgent.slice(0, 40)
}

function LoginHistoryTab() {
  const [success, setSuccess] = useState('')
  const list = usePaged<Login>('/api/usuarios/historico-acessos', { success: success || undefined })
  const columns: Column<Login>[] = [
    { key: 'at', header: 'Data', width: '150px', render: (h) => <span className="tabular">{fmt.dateTime(h.at)}</span> },
    { key: 'email', header: 'E-mail', render: (h) => <span className="font-medium text-ink">{h.email}</span> },
    { key: 'result', header: 'Resultado', render: (h) => (h.success ? <Badge tone="success">Sucesso</Badge> : <Badge tone="danger">Falha</Badge>) },
    { key: 'reason', header: 'Motivo', render: (h) => (h.reason ? loginReasons[h.reason] ?? h.reason : <span className="text-muted">—</span>) },
    { key: 'ip', header: 'IP', render: (h) => <span className="tabular text-muted">{h.ip ?? '—'}</span> },
    { key: 'agent', header: 'Navegador', render: (h) => (h.userAgent ? <span title={h.userAgent} className="text-muted">{browserName(h.userAgent)}</span> : <span className="text-muted">—</span>) },
  ]
  return (
    <Card padded={false}>
      <Toolbar>
        <SearchInput value={list.search} onChange={list.setSearch} placeholder="E-mail…" />
        <div className="w-52"><Select value={success} onChange={(e) => setSuccess(e.target.value)}><option value="">Todos os resultados</option><option value="true">Somente sucessos</option><option value="false">Somente falhas</option></Select></div>
      </Toolbar>
      <DataTable columns={columns} rows={list.rows} rowKey={(h) => h.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
        empty={{ title: 'Nenhum acesso registrado', description: list.search || success ? 'Ajuste a pesquisa ou o filtro.' : undefined }} />
    </Card>
  )
}

const feature: Feature = {
  nav: [{ to: '/usuarios', label: 'Usuários e acessos', icon: UserCog, group: 'Administração', order: 1, permissions: ['users.view'] }],
  routes: [{ path: 'usuarios', component: UsersPage, permissions: ['users.view'] }],
}
export default feature
