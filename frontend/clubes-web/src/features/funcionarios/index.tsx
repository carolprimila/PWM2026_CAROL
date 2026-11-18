import { IdCard, Plus } from 'lucide-react'
import { useState } from 'react'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { EmployeeDetail } from './EmployeeDetail'
import { EmployeeForm } from './EmployeeForm'

type Employee = Schema<'EmployeeDto'>

function EmployeesPage() {
  const { can } = useAuth()
  const [status, setStatus] = useState('')
  const [creating, setCreating] = useState(false)
  const [openId, setOpenId] = useState<string | null>(null)
  const list = usePaged<Employee>('/api/funcionarios', { status: status || undefined })

  const columns: Column<Employee>[] = [
    { key: 'name', header: 'Funcionário', render: (e) => <div className="min-w-0"><p className="truncate font-medium text-ink">{e.fullName}</p><p className="truncate text-xs text-muted">{e.position ?? 'Sem cargo informado'}</p></div> },
    { key: 'cpf', header: 'CPF', render: (e) => <span className="tabular text-muted">{fmt.cpf(e.cpf)}</span> },
    { key: 'department', header: 'Setor', render: (e) => e.department ?? <span className="text-muted">—</span> },
    { key: 'phone', header: 'Telefone', render: (e) => fmt.phone(e.phone) },
    {
      key: 'user', header: 'Usuário de acesso',
      render: (e) => e.userId ? (
        <div className="min-w-0">
          <p className="truncate text-ink">{e.userEmail}</p>
          <div className="mt-0.5 flex flex-wrap gap-1">{e.roles.length === 0 ? <Badge tone="warning">Sem função</Badge> : e.roles.map((role) => <Badge key={role}>{role}</Badge>)}</div>
        </div>
      ) : <span className="text-muted">Sem acesso</span>,
    },
    { key: 'status', header: 'Situação', render: (e) => <StatusBadge dict={labels.employeeStatus} value={e.status} /> },
  ]

  return (
    <>
      <PageHeader title="Funcionários" subtitle="Equipe do clube, com o usuário de acesso e as funções de cada pessoa."
        actions={can('employees.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo funcionário</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Nome, cargo ou setor…" />
          <div className="w-52"><Select value={status} onChange={(e) => setStatus(e.target.value)}><option value="">Todas as situações</option>{options(labels.employeeStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(e) => e.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(e) => setOpenId(e.id)}
          empty={{ title: 'Nenhum funcionário encontrado', description: list.search || status ? 'Ajuste a pesquisa ou o filtro.' : 'Cadastre a equipe do clube para liberar o acesso ao painel.' }} />
      </Card>
      {creating && <EmployeeForm onClose={() => setCreating(false)} onSaved={(e) => { setCreating(false); setOpenId(e.id) }} />}
      {openId && <EmployeeDetail key={openId} id={openId} onClose={() => setOpenId(null)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/funcionarios', label: 'Funcionários', icon: IdCard, group: 'Cadastros', order: 3, permissions: ['employees.view'] }],
  routes: [{ path: 'funcionarios', component: EmployeesPage, permissions: ['employees.view'] }],
}
export default feature
