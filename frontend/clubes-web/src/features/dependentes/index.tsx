import { Plus, UserCheck } from 'lucide-react'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { MemberPicker } from '@/components/shared'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Avatar, Badge, Button, Card, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { DependentDetail } from './DependentDetail'
import { DependentForm } from './DependentForm'

type Dependent = Schema<'DependentDto'>

function DependentsPage() {
  const { can } = useAuth()
  const [params, setParams] = useSearchParams()
  const [creating, setCreating] = useState(false)
  const [openId, setOpenId] = useState<string | null>(null)
  const status = params.get('status') ?? ''
  const memberId = params.get('memberId') ?? ''
  const setFilter = (key: string, value: string) => setParams((current) => { if (value) current.set(key, value); else current.delete(key); return current }, { replace: true })
  const list = usePaged<Dependent>('/api/dependentes', { status: status || undefined, memberId: memberId || undefined })
  // Nome do responsável filtrado pela URL (?memberId=), quando o usuário pode consultar associados.
  const member = useQuery({ queryKey: ['associado', memberId], queryFn: () => api.get<Schema<'MemberDto'>>(`/api/associados/${memberId}`), enabled: !!memberId && can('members.view') })
  const filterMember = memberId ? { id: memberId, name: member.data?.fullName ?? list.rows.find((d) => d.memberId === memberId)?.memberName ?? 'Associado selecionado' } : null

  const columns: Column<Dependent>[] = [
    {
      key: 'name', header: 'Dependente',
      render: (d) => (
        <div className="flex items-center gap-3">
          <Avatar fileId={d.photoFileId} name={d.fullName} size={34} />
          <div className="min-w-0"><p className="truncate font-medium text-ink">{d.fullName}</p><p className="truncate text-xs text-muted">{d.email ?? 'Sem e-mail'}</p></div>
        </div>
      ),
    },
    {
      key: 'member', header: 'Responsável',
      render: (d) => can('members.view')
        ? <Link to={`/associados/${d.memberId}`} className="font-medium text-brand-600 hover:underline" onClick={(e) => e.stopPropagation()}>{d.memberName}</Link>
        : d.memberName,
    },
    { key: 'relationship', header: 'Parentesco', render: (d) => d.relationship },
    { key: 'birth', header: 'Nascimento', render: (d) => fmt.day(d.birthDate) },
    { key: 'access', header: 'Acesso ao app', render: (d) => (d.hasAccess && d.status !== 'Removed' ? <Badge tone="success">Com acesso</Badge> : <Badge>{d.hasAccess ? 'Acesso revogado' : 'Sem acesso'}</Badge>) },
    { key: 'status', header: 'Situação', render: (d) => <StatusBadge dict={labels.dependentStatus} value={d.status} /> },
  ]

  return (
    <>
      <PageHeader title="Dependentes" subtitle="Dependentes dos associados, suas permissões e o acesso ao aplicativo."
        actions={can('dependents.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo dependente</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Nome do dependente ou do responsável…" />
          <div className="w-52"><Select value={status} onChange={(e) => setFilter('status', e.target.value)}><option value="">Todas as situações</option>{options(labels.dependentStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          {can('members.view') && <div className="w-80"><MemberPicker value={filterMember} onChange={(m) => setFilter('memberId', m?.id ?? '')} onlyActive={false} placeholder="Filtrar por associado responsável…" /></div>}
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(d) => d.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(d) => setOpenId(d.id)}
          empty={{ title: 'Nenhum dependente encontrado', description: list.search || status || memberId ? 'Ajuste a pesquisa ou os filtros.' : 'Os dependentes cadastrados pela equipe ou pelos associados aparecem aqui.' }} />
      </Card>
      {creating && <DependentForm member={filterMember} onClose={() => setCreating(false)} onSaved={(d) => { setCreating(false); setOpenId(d.id) }} />}
      {openId && <DependentDetail key={openId} id={openId} onClose={() => setOpenId(null)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/dependentes', label: 'Dependentes', icon: UserCheck, group: 'Cadastros', order: 2, permissions: ['dependents.view'] }],
  routes: [{ path: 'dependentes', component: DependentsPage, permissions: ['dependents.view'] }],
}
export default feature
