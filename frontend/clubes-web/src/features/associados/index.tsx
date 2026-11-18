import { Plus, Users } from 'lucide-react'
import { lazy, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Avatar, Badge, Button, Card, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { MemberForm } from './MemberForm'

type Member = Schema<'MemberListDto'>

function MembersPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const [params, setParams] = useSearchParams()
  const [creating, setCreating] = useState(false)
  const status = params.get('status') ?? ''
  const delinquent = params.get('delinquent') ?? ''
  const setFilter = (key: string, value: string) => setParams((current) => { if (value) current.set(key, value); else current.delete(key); return current }, { replace: true })
  const list = usePaged<Member>('/api/associados', { status: status || undefined, delinquent: delinquent || undefined })

  const columns: Column<Member>[] = [
    { key: 'number', header: 'Matrícula', sort: 'number', width: '110px', render: (m) => <span className="tabular text-muted">{m.number}</span> },
    {
      key: 'name', header: 'Associado', sort: 'name',
      render: (m) => (
        <div className="flex items-center gap-3">
          <Avatar fileId={m.photoFileId} name={m.fullName} size={34} />
          <div className="min-w-0"><p className="truncate font-medium text-ink">{m.fullName}</p><p className="truncate text-xs text-muted">{m.email ?? 'Sem e-mail'}</p></div>
        </div>
      ),
    },
    { key: 'cpf', header: 'CPF', render: (m) => <span className="tabular text-muted">{m.cpfMasked ?? '—'}</span> },
    { key: 'phone', header: 'Telefone', render: (m) => fmt.phone(m.phone) },
    { key: 'dependents', header: 'Dependentes', align: 'center', render: (m) => m.dependents },
    { key: 'joinedAt', header: 'Associação', sort: 'joinedAt', render: (m) => fmt.day(m.joinedAt) },
    {
      key: 'status', header: 'Situação', sort: 'status',
      render: (m) => <div className="flex items-center gap-1.5"><StatusBadge dict={labels.memberStatus} value={m.status} />{m.isDelinquent && <Badge tone="danger">Inadimplente</Badge>}</div>,
    },
  ]

  return (
    <>
      <PageHeader title="Associados" subtitle="Cadastro, situação e histórico dos associados do clube."
        actions={can('members.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo associado</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Nome, e-mail, CPF ou matrícula…" />
          <div className="w-52"><Select value={status} onChange={(e) => setFilter('status', e.target.value)}><option value="">Todas as situações</option>{options(labels.memberStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <div className="w-48"><Select value={delinquent} onChange={(e) => setFilter('delinquent', e.target.value)}><option value="">Situação financeira</option><option value="true">Inadimplentes</option><option value="false">Em dia</option></Select></div>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(m) => m.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          sort={list.sort} onSort={list.toggleSort} page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          onRowClick={(m) => navigate(`/associados/${m.id}`)}
          empty={{ title: 'Nenhum associado encontrado', description: list.search || status || delinquent ? 'Ajuste a pesquisa ou os filtros.' : 'Cadastre o primeiro associado do clube.' }} />
      </Card>
      {creating && <MemberForm onClose={() => setCreating(false)} onSaved={(m) => navigate(`/associados/${m.id}`)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/associados', label: 'Associados', icon: Users, group: 'Cadastros', order: 1, permissions: ['members.view'] }],
  routes: [
    { path: 'associados', component: MembersPage, permissions: ['members.view'] },
    { path: 'associados/:id', component: lazy(() => import('./MemberDetail')), permissions: ['members.view'] },
  ],
}
export default feature
