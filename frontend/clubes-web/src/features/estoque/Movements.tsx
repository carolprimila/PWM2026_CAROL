import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Card, Select, StatusBadge } from '@/components/ui'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { useAllItems, type Movement } from './data'

export function Movements({ itemId, onItemChange }: { itemId: string; onItemChange: (itemId: string) => void }) {
  const items = useAllItems()
  const list = usePaged<Movement>('/api/estoque/movimentacoes', { itemId: itemId || undefined })
  const unitOf = (id: string) => items.data?.items.find((i) => i.id === id)?.unit ?? ''

  const columns: Column<Movement>[] = [
    { key: 'at', header: 'Data', render: (m) => <span className="tabular text-muted">{fmt.dateTime(m.at)}</span> },
    { key: 'item', header: 'Produto', render: (m) => <span className="font-medium text-ink">{m.itemName}</span> },
    { key: 'kind', header: 'Tipo', render: (m) => <StatusBadge dict={labels.stockMovement} value={m.kind} /> },
    { key: 'quantity', header: 'Quantidade', align: 'right', render: (m) => <span className="font-medium">{m.kind === 'In' ? '+' : m.kind === 'Out' ? '−' : '='}{fmt.number(m.quantity)} {unitOf(m.stockItemId)}</span> },
    { key: 'balance', header: 'Saldo após', align: 'right', render: (m) => `${fmt.number(m.balanceAfter)} ${unitOf(m.stockItemId)}` },
    { key: 'cost', header: 'Custo unit.', align: 'right', render: (m) => (m.unitCost != null ? fmt.money(m.unitCost) : <span className="text-muted">—</span>) },
    { key: 'reason', header: 'Motivo / fornecedor', render: (m) => <div className="max-w-xs"><p className="truncate text-ink/80" title={m.reason ?? undefined}>{m.reason || '—'}</p>{m.supplierName && <p className="truncate text-xs text-muted">{m.supplierName}</p>}</div> },
    { key: 'user', header: 'Usuário', render: (m) => <span className="text-muted">{m.userName}</span> },
  ]

  return (
    <Card padded={false}>
      <Toolbar>
        <div className="w-72">
          <Select value={itemId} onChange={(e) => onItemChange(e.target.value)}>
            <option value="">Todos os produtos</option>
            {(items.data?.items ?? []).map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
          </Select>
        </div>
      </Toolbar>
      <DataTable columns={columns} rows={list.rows} rowKey={(m) => m.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
        empty={{ title: 'Nenhuma movimentação registrada', description: itemId ? 'Este produto ainda não teve entradas, saídas ou ajustes.' : 'As entradas, saídas e ajustes aparecem aqui.' }} />
    </Card>
  )
}
