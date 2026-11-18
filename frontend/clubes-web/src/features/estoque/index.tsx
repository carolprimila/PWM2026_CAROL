import { ArrowLeftRight, Boxes, MoreHorizontal, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Alert, Badge, Button, Card, Checkbox, cn, IconButton, Menu, PageHeader, SearchInput, Tabs, useConfirm, useToast } from '@/components/ui'
import { api, type Paged } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import type { StockItem } from './data'
import { Movements } from './Movements'
import { MoveModal, ProductForm } from './StockForms'
import { Suppliers } from './Suppliers'

type Tab = 'produtos' | 'movimentacoes' | 'fornecedores'

function StockPage() {
  const { can } = useAuth()
  const manage = can('stock.manage')
  const [tab, setTab] = useState<Tab>('produtos')
  const [form, setForm] = useState<{ item?: StockItem } | null>(null)
  const [moving, setMoving] = useState<{ item?: StockItem } | null>(null)
  const [historyOf, setHistoryOf] = useState('')
  const low = useQuery({ queryKey: ['/api/estoque/produtos', 'baixo'], queryFn: () => api.get<Paged<StockItem>>('/api/estoque/produtos', { lowOnly: true, pageSize: 1 }) })
  const lowCount = low.data?.total ?? 0

  return (
    <>
      <PageHeader title="Estoque" subtitle="Produtos, saldos, movimentações rastreáveis e fornecedores."
        actions={manage && <>
          <Button icon={ArrowLeftRight} onClick={() => setMoving({})}>Movimentar</Button>
          <Button variant="primary" icon={Plus} onClick={() => setForm({})}>Novo produto</Button>
        </>} />
      {lowCount > 0 && (
        <Alert tone="warning" className="mb-5" title={`${lowCount} produto(s) no estoque mínimo ou abaixo dele`}>
          Providencie a reposição. Use o filtro "Somente abaixo do mínimo" na aba Produtos para ver a lista.
        </Alert>
      )}
      <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'produtos', label: 'Produtos', count: lowCount }, { key: 'movimentacoes', label: 'Movimentações' }, { key: 'fornecedores', label: 'Fornecedores' }]} />
      {tab === 'produtos' && <Products onEdit={(item) => setForm({ item })} onMove={(item) => setMoving({ item })} onHistory={(item) => { setHistoryOf(item.id); setTab('movimentacoes') }} />}
      {tab === 'movimentacoes' && <Movements itemId={historyOf} onItemChange={setHistoryOf} />}
      {tab === 'fornecedores' && <Suppliers />}
      {form && <ProductForm item={form.item} onClose={() => setForm(null)} />}
      {moving && <MoveModal item={moving.item} onClose={() => setMoving(null)} />}
    </>
  )
}

function Products({ onEdit, onMove, onHistory }: { onEdit: (item: StockItem) => void; onMove: (item: StockItem) => void; onHistory: (item: StockItem) => void }) {
  const { can } = useAuth()
  const manage = can('stock.manage')
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [lowOnly, setLowOnly] = useState(false)
  const list = usePaged<StockItem>('/api/estoque/produtos', { lowOnly: lowOnly || undefined })
  const remove = useMutation({
    mutationFn: (id: string) => api.delete(`/api/estoque/produtos/${id}`),
    onSuccess: () => { toast.success('Produto excluído.'); void queryClient.invalidateQueries({ queryKey: ['/api/estoque/produtos'] }) },
    onError: toast.error,
  })

  const columns: Column<StockItem>[] = [
    { key: 'name', header: 'Produto', render: (i) => <div className="min-w-0"><p className="truncate font-medium text-ink">{i.name}</p><p className="truncate text-xs text-muted">{i.category || 'Sem categoria'}</p></div> },
    { key: 'supplier', header: 'Fornecedor', render: (i) => i.supplierName || <span className="text-muted">—</span> },
    { key: 'quantity', header: 'Saldo', align: 'right', render: (i) => <span className={cn('font-semibold', i.isLow ? 'text-danger' : 'text-ink')}>{fmt.number(i.quantity)} <span className="text-xs font-normal text-muted">{i.unit}</span></span> },
    { key: 'min', header: 'Mínimo', align: 'right', render: (i) => <span className="text-muted">{fmt.number(i.minQuantity)} {i.unit}</span> },
    { key: 'status', header: 'Situação', render: (i) => (i.isLow ? <Badge tone="danger">{i.quantity === 0 ? 'Sem estoque' : 'Abaixo do mínimo'}</Badge> : <Badge tone="success">Normal</Badge>) },
  ]

  return (
    <Card padded={false}>
      <Toolbar>
        <SearchInput value={list.search} onChange={list.setSearch} placeholder="Produto ou categoria…" />
        <Checkbox label="Somente abaixo do mínimo" checked={lowOnly} onChange={(e) => setLowOnly(e.target.checked)} className="ml-1" />
      </Toolbar>
      <DataTable columns={columns} rows={list.rows} rowKey={(i) => i.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={onHistory}
        rowActions={(item) => (
          <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
            manage && { label: 'Movimentar', icon: ArrowLeftRight, onClick: () => onMove(item) },
            { label: 'Ver movimentações', icon: Boxes, onClick: () => onHistory(item) },
            manage && { label: 'Editar', icon: Pencil, onClick: () => onEdit(item) },
            manage && {
              label: 'Excluir', icon: Trash2, danger: true, onClick: async () => {
                if (await confirm({ title: 'Excluir produto?', message: item.quantity !== 0 ? `"${item.name}" ainda tem saldo de ${fmt.number(item.quantity)} ${item.unit}. O servidor só permite excluir produtos com saldo zerado — faça antes um ajuste para zero.` : `"${item.name}" será removido do estoque.`, danger: true, confirmLabel: 'Excluir' })) remove.mutate(item.id)
              },
            },
          ]} />
        )}
        empty={{ title: 'Nenhum produto encontrado', description: lowOnly ? 'Nenhum produto está abaixo do estoque mínimo.' : list.search ? 'Ajuste a pesquisa.' : 'Cadastre os produtos controlados pelo clube.' }} />
    </Card>
  )
}

const feature: Feature = {
  nav: [{ to: '/estoque', label: 'Estoque', icon: Boxes, group: 'Operação', order: 2, permissions: ['stock.view'] }],
  routes: [{ path: 'estoque', component: StockPage, permissions: ['stock.view'] }],
}
export default feature
