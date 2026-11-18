import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight } from 'lucide-react'
import { useCallback, useState, type ReactNode } from 'react'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, type Paged } from '@/lib/api'
import { fmt } from '@/lib/format'
import { Button, cn, EmptyState, ErrorState, LoadingState } from './ui'

export type Column<T> = {
  key: string
  header: string
  /** Nome do campo de ordenação aceito pela API (parâmetro `sort`). Sem ele, a coluna não é ordenável. */
  sort?: string
  align?: 'left' | 'right' | 'center'
  width?: string
  render: (row: T) => ReactNode
}

type PagedParams = Record<string, string | number | boolean | null | undefined>

/**
 * Consulta paginada padrão da API (page, pageSize, search, sort, desc + filtros).
 * `filters` são enviados como query string; mudar um filtro volta para a página 1.
 */
export function usePaged<T>(path: string, filters: PagedParams = {}, options: { pageSize?: number; enabled?: boolean; refetchInterval?: number } = {}) {
  const [page, setPage] = useState(1)
  const [search, setSearchState] = useState('')
  const [sort, setSortState] = useState<{ field: string; desc: boolean } | null>(null)
  const pageSize = options.pageSize ?? 20
  const filterKey = JSON.stringify(filters)
  const [lastFilters, setLastFilters] = useState(filterKey)
  if (lastFilters !== filterKey) { setLastFilters(filterKey); setPage(1) }

  const query = useQuery({
    queryKey: [path, filters, page, pageSize, search, sort],
    queryFn: ({ signal }) => api.get<Paged<T>>(path, { ...filters, page, pageSize, search, sort: sort?.field, desc: sort?.desc }, signal),
    placeholderData: keepPreviousData,
    enabled: options.enabled,
    refetchInterval: options.refetchInterval,
  })
  const setSearch = useCallback((value: string) => { setSearchState(value); setPage(1) }, [])
  const toggleSort = useCallback((field: string) => setSortState((current) => (current?.field === field ? (current.desc ? null : { field, desc: true }) : { field, desc: false })), [])
  return { ...query, rows: query.data?.items ?? [], total: query.data?.total ?? 0, page, pageSize, setPage, search, setSearch, sort, toggleSort }
}

type TableProps<T> = {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  isLoading?: boolean
  error?: unknown
  onRetry?: () => void
  onRowClick?: (row: T) => void
  empty?: { title: string; description?: ReactNode; action?: ReactNode }
  sort?: { field: string; desc: boolean } | null
  onSort?: (field: string) => void
  /** Paginação (omitir para listas simples). */
  page?: number
  pageSize?: number
  total?: number
  onPage?: (page: number) => void
  /** Ações contextuais por linha (ex.: <Menu/>). */
  rowActions?: (row: T) => ReactNode
}

export function DataTable<T>({ columns, rows, rowKey, isLoading, error, onRetry, onRowClick, empty, sort, onSort, page, pageSize, total, onPage, rowActions }: TableProps<T>) {
  if (isLoading && rows.length === 0) return <LoadingState />
  if (error) return <ErrorState error={error} onRetry={onRetry} />
  if (rows.length === 0) return <EmptyState title={empty?.title ?? 'Nenhum registro encontrado'} description={empty?.description} action={empty?.action} />

  const pages = total != null && pageSize ? Math.max(1, Math.ceil(total / pageSize)) : 1
  return (
    <div>
      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-b border-line text-left text-xs font-medium tracking-wide text-muted uppercase">
              {columns.map((column) => (
                <th key={column.key} style={{ width: column.width }} className={cn('px-4 py-3 font-medium whitespace-nowrap', column.align === 'right' && 'text-right', column.align === 'center' && 'text-center')}>
                  {column.sort && onSort ? (
                    <button className="inline-flex items-center gap-1 uppercase hover:text-ink" onClick={() => onSort(column.sort!)}>
                      {column.header}
                      {sort?.field === column.sort && (sort.desc ? <ArrowDown className="size-3" /> : <ArrowUp className="size-3" />)}
                    </button>
                  ) : column.header}
                </th>
              ))}
              {rowActions && <th className="w-12" />}
            </tr>
          </thead>
          <tbody className={cn(isLoading && 'opacity-60')}>
            {rows.map((row) => (
              <tr key={rowKey(row)} onClick={onRowClick ? () => onRowClick(row) : undefined}
                className={cn('border-b border-line/70 last:border-0', onRowClick && 'cursor-pointer transition-colors hover:bg-canvas/70')}>
                {columns.map((column) => (
                  <td key={column.key} className={cn('px-4 py-3 align-middle', column.align === 'right' && 'tabular text-right', column.align === 'center' && 'text-center')}>{column.render(row)}</td>
                ))}
                {rowActions && <td className="px-2 py-2 text-right" onClick={(e) => e.stopPropagation()}>{rowActions(row)}</td>}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {onPage && total != null && page != null && pageSize != null && (
        <footer className="flex items-center justify-between border-t border-line px-4 py-3 text-[13px] text-muted">
          <span>{fmt.number((page - 1) * pageSize + 1)}–{fmt.number(Math.min(page * pageSize, total))} de {fmt.number(total)}</span>
          <div className="flex items-center gap-2">
            <Button size="sm" icon={ChevronLeft} disabled={page <= 1} onClick={() => onPage(page - 1)}>Anterior</Button>
            <span className="tabular px-1">Página {page} de {pages}</span>
            <Button size="sm" disabled={page >= pages} onClick={() => onPage(page + 1)}>Próxima<ChevronRight className="size-4" /></Button>
          </div>
        </footer>
      )}
    </div>
  )
}

/** Barra de ferramentas acima de uma tabela: pesquisa e filtros à esquerda, ações à direita. */
export function Toolbar({ children, actions }: { children?: ReactNode; actions?: ReactNode }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line px-4 py-3">
      <div className="flex flex-1 flex-wrap items-center gap-2">{children}</div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  )
}
