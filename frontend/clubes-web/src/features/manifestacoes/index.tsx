import { MessageSquareWarning } from 'lucide-react'
import { lazy } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Card, Checkbox, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Manifestation = Schema<'ManifestationDto'>

function ManifestationsPage() {
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const typeId = params.get('typeId') ?? ''
  const openOnly = params.get('abertas') !== '0'
  const setFilter = (key: string, value: string) => setParams((current) => { if (value) current.set(key, value); else current.delete(key); return current }, { replace: true })
  const types = useQuery({ queryKey: ['/api/manifestacoes/tipos', true], queryFn: () => api.get<Schema<'ManifestationTypeDto'>[]>('/api/manifestacoes/tipos', { includeInactive: true }) })
  const list = usePaged<Manifestation>('/api/manifestacoes', { status: status || undefined, typeId: typeId || undefined, openOnly: (!status && openOnly) || undefined })

  const columns: Column<Manifestation>[] = [
    { key: 'protocol', header: 'Protocolo', width: '120px', render: (m) => <span className="tabular font-medium text-ink">{m.protocol}</span> },
    { key: 'type', header: 'Tipo', render: (m) => <Badge tone={m.isSuggestion ? 'brand' : 'neutral'}>{m.typeName}</Badge> },
    { key: 'author', header: 'Autor', render: (m) => <div className="flex items-center gap-2"><span className="truncate text-ink">{m.authorName}</span><StatusBadge dict={labels.userKind} value={m.authorKind} /></div> },
    { key: 'summary', header: 'Resumo', render: (m) => <p className="line-clamp-1 max-w-md text-muted">{m.description}</p> },
    { key: 'status', header: 'Situação', render: (m) => <StatusBadge dict={labels.manifestationStatus} value={m.status} /> },
    { key: 'createdAt', header: 'Recebida em', render: (m) => <span className="tabular text-muted">{fmt.dateTime(m.createdAt)}</span> },
  ]

  return (
    <>
      <PageHeader title="Manifestações" subtitle="Reclamações, sugestões e demais manifestações enviadas por associados e dependentes." />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Protocolo, autor ou texto…" />
          <div className="w-56"><Select value={status} onChange={(e) => setFilter('status', e.target.value)}><option value="">Todas as situações</option>{options(labels.manifestationStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <div className="w-48"><Select value={typeId} onChange={(e) => setFilter('typeId', e.target.value)}><option value="">Todos os tipos</option>{(types.data ?? []).map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}</Select></div>
          <Checkbox label="Somente em aberto" checked={!status && openOnly} disabled={!!status} onChange={(e) => setFilter('abertas', e.target.checked ? '' : '0')} className="ml-1" />
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(m) => m.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(m) => navigate(`/manifestacoes/${m.id}`)}
          empty={{ title: 'Nenhuma manifestação encontrada', description: openOnly && !list.search && !status && !typeId ? 'Não há manifestações aguardando tratamento.' : 'Ajuste a pesquisa ou os filtros.' }} />
      </Card>
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/manifestacoes', label: 'Manifestações', icon: MessageSquareWarning, group: 'Comunicação', order: 3, permissions: ['manifestations.view'] }],
  routes: [
    { path: 'manifestacoes', component: ManifestationsPage, permissions: ['manifestations.view'] },
    { path: 'manifestacoes/:id', component: lazy(() => import('./ManifestationDetail')), permissions: ['manifestations.view'] },
  ],
}
export default feature
