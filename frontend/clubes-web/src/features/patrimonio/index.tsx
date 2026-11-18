import { Armchair, Plus } from 'lucide-react'
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Button, Card, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { AssetDetail } from './AssetDetail'
import { AssetForm } from './AssetForm'

type Asset = Schema<'AssetDto'>

/** Categorias já usadas nos bens (o filtro da API é por categoria exata). */
export function useAssetCategories() {
  const query = useQuery({ queryKey: ['/api/patrimonio', 'categorias'], queryFn: () => api.get<Paged<Asset>>('/api/patrimonio', { pageSize: 200 }) })
  return [...new Set((query.data?.items ?? []).map((a) => a.category).filter((c): c is string => !!c))].sort((a, b) => a.localeCompare(b, 'pt-BR'))
}

function AssetsPage() {
  const { can } = useAuth()
  const [category, setCategory] = useState('')
  const [condition, setCondition] = useState('')
  const [creating, setCreating] = useState(false)
  const [openId, setOpenId] = useState<string | null>(null)
  const categories = useAssetCategories()
  const list = usePaged<Asset>('/api/patrimonio', { category: category || undefined, condition: condition || undefined })

  const columns: Column<Asset>[] = [
    { key: 'code', header: 'Identificação', width: '130px', render: (a) => <span className="tabular font-medium text-ink">{a.code}</span> },
    { key: 'name', header: 'Bem', render: (a) => <div className="min-w-0"><p className="truncate font-medium text-ink">{a.name}</p><p className="truncate text-xs text-muted">{a.category || 'Sem categoria'}</p></div> },
    { key: 'location', header: 'Localização', render: (a) => [a.spaceName, a.location].filter(Boolean).join(' · ') || <span className="text-muted">—</span> },
    { key: 'responsible', header: 'Responsável', render: (a) => a.responsibleName || <span className="text-muted">—</span> },
    { key: 'acquiredOn', header: 'Aquisição', render: (a) => <span className="tabular text-muted">{fmt.day(a.acquiredOn)}</span> },
    { key: 'value', header: 'Valor', align: 'right', render: (a) => fmt.money(a.value) },
    { key: 'condition', header: 'Estado', render: (a) => <StatusBadge dict={labels.assetCondition} value={a.condition} /> },
  ]

  return (
    <>
      <PageHeader title="Patrimônio" subtitle="Bens do clube, estado de conservação, localização e responsáveis."
        actions={can('assets.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo bem</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Nome, identificação ou localização…" />
          <div className="w-52"><Select value={category} onChange={(e) => setCategory(e.target.value)}><option value="">Todas as categorias</option>{categories.map((c) => <option key={c} value={c}>{c}</option>)}</Select></div>
          <div className="w-44"><Select value={condition} onChange={(e) => setCondition(e.target.value)}><option value="">Todos os estados</option>{options(labels.assetCondition).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(a) => a.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(a) => setOpenId(a.id)}
          empty={{ title: 'Nenhum bem encontrado', description: list.search || category || condition ? 'Ajuste a pesquisa ou os filtros.' : 'Cadastre os bens patrimoniais do clube.' }} />
      </Card>
      {creating && <AssetForm categories={categories} onClose={() => setCreating(false)} onSaved={(saved) => { setCreating(false); setOpenId(saved.id) }} />}
      {openId && <AssetDetail id={openId} categories={categories} onClose={() => setOpenId(null)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/patrimonio', label: 'Patrimônio', icon: Armchair, group: 'Operação', order: 1, permissions: ['assets.view'] }],
  routes: [{ path: 'patrimonio', component: AssetsPage, permissions: ['assets.view'] }],
}
export default feature
