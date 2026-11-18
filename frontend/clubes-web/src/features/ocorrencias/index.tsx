import { Plus, ShieldAlert } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Button, Card, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { OccurrenceDetail } from './OccurrenceDetail'
import { OccurrenceForm } from './OccurrenceForm'

type Occurrence = Schema<'OccurrenceDto'>

function OccurrencesPage() {
  const { can } = useAuth()
  const [params, setParams] = useSearchParams()
  const [creating, setCreating] = useState(false)
  const kind = params.get('kind') ?? ''
  const status = params.get('status') ?? ''
  const chargeDecision = params.get('chargeDecision') ?? ''
  const openId = params.get('id')
  const setFilter = (key: string, value: string | null) => setParams((current) => { if (value) current.set(key, value); else current.delete(key); return current }, { replace: true })
  const list = usePaged<Occurrence>('/api/ocorrencias', { kind: kind || undefined, status: status || undefined, chargeDecision: chargeDecision || undefined })

  const columns: Column<Occurrence>[] = [
    { key: 'code', header: 'Código', width: '110px', render: (o) => <span className="tabular font-medium text-ink">{o.code}</span> },
    { key: 'kind', header: 'Tipo', render: (o) => <StatusBadge dict={labels.occurrenceKind} value={o.kind} /> },
    { key: 'description', header: 'Descrição', render: (o) => <p className="line-clamp-1 max-w-md text-ink/80">{o.description}</p> },
    { key: 'links', header: 'Vínculos', render: (o) => <div className="min-w-0"><p className="truncate text-ink">{o.memberName ?? '—'}</p><p className="truncate text-xs text-muted">{[o.spaceName, o.reservationCode && `Reserva ${o.reservationCode}`].filter(Boolean).join(' · ') || 'Sem espaço ou reserva'}</p></div> },
    { key: 'occurredAt', header: 'Data', render: (o) => <span className="tabular text-muted">{fmt.dateTime(o.occurredAt)}</span> },
    { key: 'charge', header: 'Cobrança', render: (o) => (o.chargeDecision === 'None' ? <span className="text-muted">—</span> : <div><StatusBadge dict={labels.chargeDecision} value={o.chargeDecision} /><p className="tabular mt-0.5 text-xs text-muted">{fmt.money(o.proposedChargeAmount)}</p></div>) },
    { key: 'status', header: 'Situação', render: (o) => <StatusBadge dict={labels.occurrenceStatus} value={o.status} /> },
  ]

  return (
    <>
      <PageHeader title="Ocorrências" subtitle="Danos, registros da portaria e demais ocorrências, com análise e cobrança sujeita a aprovação."
        actions={can('occurrences.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Nova ocorrência</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Código ou descrição…" />
          <div className="w-40"><Select value={kind} onChange={(e) => setFilter('kind', e.target.value)}><option value="">Todos os tipos</option>{options(labels.occurrenceKind).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <div className="w-48"><Select value={status} onChange={(e) => setFilter('status', e.target.value)}><option value="">Todas as situações</option>{options(labels.occurrenceStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <div className="w-52"><Select value={chargeDecision} onChange={(e) => setFilter('chargeDecision', e.target.value)}><option value="">Cobrança: todas</option>{options(labels.chargeDecision).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(o) => o.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(o) => setFilter('id', o.id)}
          empty={{ title: 'Nenhuma ocorrência encontrada', description: list.search || kind || status || chargeDecision ? 'Ajuste a pesquisa ou os filtros.' : 'Ocorrências registradas pela equipe ou geradas por vistorias aparecem aqui.' }} />
      </Card>
      {creating && <OccurrenceForm onClose={() => setCreating(false)} onSaved={(saved) => { setCreating(false); setFilter('id', saved.id) }} />}
      {openId && <OccurrenceDetail id={openId} onClose={() => setFilter('id', null)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/ocorrencias', label: 'Ocorrências', icon: ShieldAlert, group: 'Operação', order: 5, permissions: ['occurrences.view'] }],
  routes: [{ path: 'ocorrencias', component: OccurrencesPage, permissions: ['occurrences.view'] }],
}
export default feature
