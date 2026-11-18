import { ClipboardCheck, Plus } from 'lucide-react'
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, Checkbox, PageHeader, Select, StatusBadge, Tabs } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { InspectionDetail } from './InspectionDetail'
import { InspectionForm } from './InspectionForm'
import { Keys } from './Keys'

type Inspection = Schema<'InspectionDto'>
type Tab = 'vistorias' | 'chaves'

function InspectionsPage() {
  const { can } = useAuth()
  const [tab, setTab] = useState<Tab>('vistorias')
  const [creating, setCreating] = useState(false)
  const [open, setOpen] = useState<{ id: string; justCreated?: boolean } | null>(null)
  const spaces = useQuery({ queryKey: ['/api/espacos'], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })

  return (
    <>
      <PageHeader title="Vistorias" subtitle="Checklists antes e depois do uso dos espaços e controle de entrega de chaves."
        actions={tab === 'vistorias' && can('inspections.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Nova vistoria</Button>} />
      <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'vistorias', label: 'Vistorias' }, { key: 'chaves', label: 'Entrega de chaves' }]} />
      {tab === 'vistorias' && <InspectionsList spaces={spaces.data ?? []} onOpen={(id) => setOpen({ id })} />}
      {tab === 'chaves' && <Keys />}
      {creating && <InspectionForm spaces={spaces.data ?? []} onClose={() => setCreating(false)} onSaved={(saved) => { setCreating(false); setOpen({ id: saved.id, justCreated: true }) }} />}
      {open && <InspectionDetail id={open.id} justCreated={open.justCreated} onClose={() => setOpen(null)} />}
    </>
  )
}

function InspectionsList({ spaces, onOpen }: { spaces: Schema<'SpaceDto'>[]; onOpen: (id: string) => void }) {
  const [spaceId, setSpaceId] = useState('')
  const [damageOnly, setDamageOnly] = useState(false)
  const list = usePaged<Inspection>('/api/vistorias', { spaceId: spaceId || undefined, damageOnly: damageOnly || undefined })

  const columns: Column<Inspection>[] = [
    { key: 'performedAt', header: 'Data', render: (i) => <span className="tabular text-ink">{fmt.dateTime(i.performedAt)}</span> },
    { key: 'space', header: 'Espaço', render: (i) => <span className="font-medium text-ink">{i.spaceName}</span> },
    { key: 'moment', header: 'Momento', render: (i) => <StatusBadge dict={labels.inspectionMoment} value={i.moment} /> },
    { key: 'reservation', header: 'Reserva', render: (i) => (i.reservationCode ? <div><p className="tabular text-ink">{i.reservationCode}</p><p className="text-xs text-muted">{i.memberName ?? '—'}</p></div> : <span className="text-muted">—</span>) },
    { key: 'inspector', header: 'Vistoriador', render: (i) => <span className="text-muted">{i.inspectorName}</span> },
    { key: 'items', header: 'Itens', align: 'center', render: (i) => i.items.length },
    { key: 'result', header: 'Resultado', render: (i) => (i.hasDamage ? <Badge tone="danger">Com danos</Badge> : i.items.some((item) => !item.isOk) ? <Badge tone="warning">Com ressalvas</Badge> : <Badge tone="success">Tudo em ordem</Badge>) },
  ]

  return (
    <Card padded={false}>
      <Toolbar>
        <div className="w-56"><Select value={spaceId} onChange={(e) => setSpaceId(e.target.value)}><option value="">Todos os espaços</option>{spaces.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select></div>
        <Checkbox label="Somente com danos" checked={damageOnly} onChange={(e) => setDamageOnly(e.target.checked)} className="ml-1" />
      </Toolbar>
      <DataTable columns={columns} rows={list.rows} rowKey={(i) => i.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(i) => onOpen(i.id)}
        empty={{ title: 'Nenhuma vistoria encontrada', description: spaceId || damageOnly ? 'Ajuste os filtros.' : 'Registre a vistoria dos espaços antes e depois de cada uso.' }} />
    </Card>
  )
}

const feature: Feature = {
  nav: [{ to: '/vistorias', label: 'Vistorias', icon: ClipboardCheck, group: 'Operação', order: 4, permissions: ['inspections.view'] }],
  routes: [{ path: 'vistorias', component: InspectionsPage, permissions: ['inspections.view'] }],
}
export default feature
