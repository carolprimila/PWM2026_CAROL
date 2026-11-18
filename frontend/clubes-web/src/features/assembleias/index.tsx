import { Landmark, Plus, Vote } from 'lucide-react'
import { lazy, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Button, Card, PageHeader, StatusBadge } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { AssemblyForm } from './AssemblyForm'

type Assembly = Schema<'AssemblyDto'>

function AssembliesPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const [creating, setCreating] = useState(false)
  const list = usePaged<Assembly>('/api/assembleias')

  const columns: Column<Assembly>[] = [
    { key: 'title', header: 'Assembleia', render: (a) => <div className="min-w-0"><p className="truncate font-medium text-ink">{a.title}</p><p className="truncate text-xs text-muted">{a.location || 'Local não informado'}</p></div> },
    { key: 'scheduledAt', header: 'Data e hora', render: (a) => <span className="tabular">{fmt.dateTime(a.scheduledAt)}</span> },
    { key: 'attendances', header: 'Presenças', align: 'center', render: (a) => a.attendances },
    { key: 'polls', header: 'Votações', align: 'center', render: (a) => a.polls },
    { key: 'minutes', header: 'Ata', render: (a) => (a.minutes || a.minutesFileId ? 'Registrada' : <span className="text-muted">—</span>) },
    { key: 'status', header: 'Situação', render: (a) => <StatusBadge dict={labels.assemblyStatus} value={a.status} /> },
  ]

  return (
    <>
      <PageHeader title="Assembleias" subtitle="Convocações, pauta, presença, votações e atas."
        actions={can('assemblies.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Nova assembleia</Button>} />
      <Card padded={false}>
        <Toolbar><p className="text-[13px] text-muted">Somente associados votam; dependentes nunca participam de assembleias ou votações.</p></Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(a) => a.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(a) => navigate(`/assembleias/${a.id}`)}
          empty={{ title: 'Nenhuma assembleia cadastrada', description: 'Ao criar uma assembleia, os associados são notificados da convocação.' }} />
      </Card>
      {creating && <AssemblyForm onClose={() => setCreating(false)} onSaved={(saved) => navigate(`/assembleias/${saved.id}`)} />}
    </>
  )
}

const feature: Feature = {
  nav: [
    { to: '/assembleias', label: 'Assembleias', icon: Landmark, group: 'Comunicação', order: 4, permissions: ['assemblies.view'] },
    { to: '/votacoes', label: 'Votações', icon: Vote, group: 'Comunicação', order: 5, permissions: ['assemblies.view'] },
  ],
  routes: [
    { path: 'assembleias', component: AssembliesPage, permissions: ['assemblies.view'] },
    { path: 'assembleias/:id', component: lazy(() => import('./AssemblyDetail')), permissions: ['assemblies.view'] },
    { path: 'votacoes', component: lazy(() => import('./PollsPage')), permissions: ['assemblies.view'] },
  ],
}
export default feature
