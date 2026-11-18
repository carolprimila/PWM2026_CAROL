import { useQuery } from '@tanstack/react-query'
import { api, type Paged, type Schema } from '@/lib/api'

export type StockItem = Schema<'StockItemDto'>
export type Supplier = Schema<'SupplierDto'>
export type Movement = Schema<'StockMovementDto'>

export const useSuppliers = () => useQuery({ queryKey: ['/api/estoque/fornecedores'], queryFn: () => api.get<Supplier[]>('/api/estoque/fornecedores') })

/** Todos os produtos (até 200) para seletores e para sugerir categorias já usadas. */
export const useAllItems = () => useQuery({ queryKey: ['/api/estoque/produtos', 'opcoes'], queryFn: () => api.get<Paged<StockItem>>('/api/estoque/produtos', { pageSize: 200 }) })

/** "1.234,5" → 1234.5; texto inválido → null. */
export function parseQuantity(text: string): number | null {
  const cleaned = text.trim().replace(/\./g, '').replace(',', '.')
  if (cleaned === '') return null
  const value = Number(cleaned)
  return Number.isFinite(value) ? value : null
}
