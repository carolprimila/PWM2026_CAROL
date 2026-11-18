import { Plus, Wrench } from 'lucide-react'
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, Checkbox, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { MaintenanceDetail } from './MaintenanceDetail'
import { MaintenanceForm } from './MaintenanceForm'

type Maintenance = Schema<'MaintenanceDto'>

function MaintenancePage() {
  const { can } = useAuth()
  const [status, setStatus] = useState('')
  const [spaceId, setSpaceId] = useState('')
  const [openOnly, setOpenOnly] = useState(true)
  const [creating, setCreating] = useState(false)
  const [openId, setOpenId] = useState<string | null>(null)
  const spaces = useQuery({ queryKey: ['/api/espacos'], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })
  const list = usePaged<Maintenance>('/api/manutencoes', { status: status || undefined, spaceId: spaceId || undefined, openOnly: (!status && openOnly) || undefined })

  const columns: Column<Maintenance>[] = [
    { key: 'code', header: 'Código', width: '110px', render: (m) => <span className="tabular font-medium text-ink">{m.code}</span> },
    { key: 'title', header: 'Problema', render: (m) => <div className="min-w-0"><p className="truncate font-medium text-ink">{m.title}</p><p className="truncate text-xs text-muted">{[m.spaceName, m.assetName].filter(Boolean).join(' · ') || '—'}</p></div> },
    { key: 'kind', header: 'Tipo', render: (m) => <StatusBadge dict={labels.maintenanceKind} value={m.kind} /> },
    { key: 'responsible', header: 'Responsável', render: (m) => m.responsibleName || <span className="text-muted">—</span> },
    { key: 'openedAt', header: 'Abertura', render: (m) => <span className="tabular text-muted">{fmt.date(m.openedAt)}</span> },
    { key: 'dueDate', header: 'Prazo', render: (m) => <span className={m.isLate ? 'tabular font-medium text-danger' : 'tabular text-muted'}>{fmt.day(m.dueDate)}</span> },
    { key: 'cost', header: 'Custo', align: 'right', render: (m) => (m.actualCost != null ? fmt.money(m.actualCost) : m.estimatedCost != null ? <span className="text-muted" title="Custo estimado">≈ {fmt.money(m.estimatedCost)}</span> : <span className="text-muted">—</span>) },
    {
      key: 'status', header: 'Situação',
      render: (m) => <div className="flex flex-wrap items-center gap-1.5"><StatusBadge dict={labels.maintenanceStatus} value={m.status} />{m.isLate && <Badge tone="danger">Atrasada</Badge>}{m.hasActiveBlock && <Badge tone="warning">Espaço bloqueado</Badge>}</div>,
    },
  ]

  return (
    <>
      <PageHeader title="Manutenções" subtitle="Solicitações, ordens de serviço, prazos, custos e bloqueio de espaços."
        actions={can('maintenance.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Nova manutenção</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Problema ou código…" />
          <div className="w-48"><Select value={status} onChange={(e) => setStatus(e.target.value)}><option value="">Todas as situações</option>{options(labels.maintenanceStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <div className="w-48"><Select value={spaceId} onChange={(e) => setSpaceId(e.target.value)}><option value="">Todos os espaços</option>{(spaces.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select></div>
          <Checkbox label="Somente abertas" checked={!status && openOnly} disabled={!!status} onChange={(e) => setOpenOnly(e.target.checked)} className="ml-1" />
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(m) => m.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(m) => setOpenId(m.id)}
          empty={{ title: 'Nenhuma manutenção encontrada', description: !status && openOnly && !list.search && !spaceId ? 'Não há manutenções em aberto.' : 'Ajuste a pesquisa ou os filtros.' }} />
      </Card>
      {creating && <MaintenanceForm onClose={() => setCreating(false)} onSaved={(saved) => { setCreating(false); setOpenId(saved.id) }} />}
      {openId && <MaintenanceDetail id={openId} onClose={() => setOpenId(null)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/manutencoes', label: 'Manutenções', icon: Wrench, group: 'Operação', order: 3, permissions: ['maintenance.view'] }],
  routes: [{ path: 'manutencoes', component: MaintenancePage, permissions: ['maintenance.view'] }],
}
export default feature
