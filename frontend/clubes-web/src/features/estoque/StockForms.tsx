import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, cn, Field, Input, Modal, MoneyInput, Select, Textarea, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { emptyToNull, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { parseQuantity, useAllItems, useSuppliers, type Movement, type StockItem } from './data'

const quantity = (message: string, allowZero: boolean) => z.string().refine((v) => { const n = parseQuantity(v); return n != null && n <= 100_000_000 && (allowZero ? n >= 0 : n > 0) }, message)

const productSchema = z.object({
  name: z.string().trim().min(2, 'Informe o nome do produto').max(150, 'Máximo de 150 caracteres'),
  category: z.string().max(80, 'Máximo de 80 caracteres'),
  unit: z.string().trim().min(1, 'Informe a unidade').max(10, 'Máximo de 10 caracteres'),
  minQuantity: quantity('Informe um número igual ou maior que zero', true),
  supplierId: z.string(),
})
type ProductValues = z.infer<typeof productSchema>

export function ProductForm({ item, onClose }: { item?: StockItem; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const suppliers = useSuppliers()
  const all = useAllItems()
  const categories = [...new Set((all.data?.items ?? []).map((i) => i.category).filter((c): c is string => !!c))].sort((a, b) => a.localeCompare(b, 'pt-BR'))
  const form = useForm<ProductValues>({
    resolver: zodResolver(productSchema),
    defaultValues: { name: item?.name ?? '', category: item?.category ?? '', unit: item?.unit ?? 'un', minQuantity: String(item?.minQuantity ?? 0).replace('.', ','), supplierId: item?.supplierId ?? '' },
  })
  const { errors } = form.formState
  const save = useMutation({
    mutationFn: (values: ProductValues) => {
      const body: Schema<'SaveStockItemRequest'> = { name: values.name, category: emptyToNull(values.category), unit: values.unit, minQuantity: parseQuantity(values.minQuantity) ?? 0, supplierId: values.supplierId || null }
      return item ? api.put<StockItem>(`/api/estoque/produtos/${item.id}`, body) : api.post<StockItem>('/api/estoque/produtos', body)
    },
    onSuccess: () => {
      toast.success(item ? 'Produto atualizado.' : 'Produto cadastrado. Registre uma entrada para lançar o saldo inicial.')
      void queryClient.invalidateQueries({ queryKey: ['/api/estoque/produtos'] })
      onClose()
    },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} title={item ? 'Editar produto' : 'Novo produto'} description="O saldo só é alterado por movimentações (entrada, saída ou ajuste)."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{item ? 'Salvar alterações' : 'Cadastrar'}</Button>
      </>}>
      <form className="grid grid-cols-6 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-6" label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} maxLength={150} {...form.register('name')} /></Field>
        <Field className="col-span-3" label="Categoria" error={errors.category?.message}>
          <Input list="stock-categories" maxLength={80} placeholder="Ex.: Limpeza, Piscina" {...form.register('category')} />
          <datalist id="stock-categories">{categories.map((c) => <option key={c} value={c} />)}</datalist>
        </Field>
        <Field className="col-span-3" label="Fornecedor habitual">
          <Select {...form.register('supplierId')} value={form.watch('supplierId')}><option value="">Nenhum</option>{(suppliers.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
        </Field>
        <Field className="col-span-3" label="Unidade" required error={errors.unit?.message} hint="Ex.: un, kg, L, cx."><Input aria-invalid={!!errors.unit} maxLength={10} {...form.register('unit')} /></Field>
        <Field className="col-span-3" label="Estoque mínimo" required error={errors.minQuantity?.message} hint="A equipe é avisada quando o saldo atinge este valor.">
          <Input inputMode="decimal" className="tabular" aria-invalid={!!errors.minQuantity} {...form.register('minQuantity')} />
        </Field>
      </form>
    </Modal>
  )
}

const moveSchema = z.object({
  stockItemId: z.string().min(1, 'Selecione o produto'),
  kind: z.string(),
  quantity: z.string(),
  unitCost: z.number().min(0).nullable(),
  reason: z.string().max(300, 'Máximo de 300 caracteres'),
  supplierId: z.string(),
}).superRefine((v, ctx) => {
  const n = parseQuantity(v.quantity)
  if (n == null || n < 0 || n > 100_000_000 || (v.kind !== 'Adjustment' && n <= 0)) ctx.addIssue({ code: 'custom', path: ['quantity'], message: v.kind === 'Adjustment' ? 'Informe o saldo contado (zero ou mais)' : 'Informe uma quantidade maior que zero' })
  if (v.kind !== 'In' && !v.reason.trim()) ctx.addIssue({ code: 'custom', path: ['reason'], message: 'Informe o motivo da saída ou do ajuste' })
})
type MoveValues = z.infer<typeof moveSchema>
type Kind = Schema<'StockMovementKind'>

export function MoveModal({ item, onClose }: { item?: StockItem; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const suppliers = useSuppliers()
  const all = useAllItems()
  const form = useForm<MoveValues>({
    resolver: zodResolver(moveSchema),
    defaultValues: { stockItemId: item?.id ?? '', kind: 'In', quantity: '', unitCost: null, reason: '', supplierId: item?.supplierId ?? '' },
  })
  const { errors } = form.formState
  const kind = form.watch('kind') as Kind
  const selected = item ?? all.data?.items.find((i) => i.id === form.watch('stockItemId'))
  const typed = parseQuantity(form.watch('quantity'))
  const after = selected && typed != null ? (kind === 'In' ? selected.quantity + typed : kind === 'Out' ? selected.quantity - typed : typed) : null

  const move = useMutation({
    mutationFn: (values: MoveValues) => api.post<Movement>('/api/estoque/movimentacoes', {
      stockItemId: values.stockItemId, kind: values.kind as Kind, quantity: parseQuantity(values.quantity) ?? 0, unitCost: values.kind === 'In' ? values.unitCost : null,
      reason: emptyToNull(values.reason), supplierId: values.kind === 'In' && values.supplierId ? values.supplierId : null,
    } satisfies Schema<'MoveStockRequest'>),
    onSuccess: (movement) => {
      toast.success(`${labels.stockMovement[movement.kind].label} registrada. Saldo de ${movement.itemName}: ${fmt.number(movement.balanceAfter)}.`)
      void queryClient.invalidateQueries({ queryKey: ['/api/estoque/produtos'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/estoque/movimentacoes'] })
      onClose()
    },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} title="Movimentar estoque" description="Toda movimentação fica registrada com usuário, data, motivo e saldo resultante."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={move.isPending} onClick={form.handleSubmit((values) => move.mutate(values))}>Registrar {labels.stockMovement[kind].label.toLowerCase()}</Button>
      </>}>
      <form className="space-y-4" noValidate onSubmit={(e) => e.preventDefault()}>
        {item ? (
          <div className="flex items-center justify-between rounded-(--radius-control) border border-line bg-canvas px-3 py-2.5 text-sm">
            <span className="font-medium text-ink">{item.name}</span>
            <span className="tabular text-muted">Saldo atual: {fmt.number(item.quantity)} {item.unit}</span>
          </div>
        ) : (
          <Field label="Produto" required error={errors.stockItemId?.message} hint={selected ? `Saldo atual: ${fmt.number(selected.quantity)} ${selected.unit}` : undefined}>
            <Select aria-invalid={!!errors.stockItemId} {...form.register('stockItemId')} value={form.watch('stockItemId')}>
              <option value="">Selecione…</option>
              {(all.data?.items ?? []).map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
            </Select>
          </Field>
        )}
        <div className="inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5">
          {options(labels.stockMovement).map((o) => (
            <button key={o.value} type="button" onClick={() => form.setValue('kind', o.value, { shouldValidate: form.formState.isSubmitted })}
              className={cn('rounded-lg px-4 py-1.5 text-[13px] font-medium transition-colors', kind === o.value ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink')}>{o.label}</button>
          ))}
        </div>
        {kind === 'Adjustment' && <Alert tone="warning" title="Ajuste de inventário">A quantidade informada <strong>passa a ser o novo saldo</strong> do produto (não é somada nem subtraída). Use após a contagem física.</Alert>}
        <div className="grid grid-cols-2 gap-4">
          <Field label={kind === 'Adjustment' ? 'Novo saldo (contagem)' : 'Quantidade'} required error={errors.quantity?.message}
            hint={after != null ? (after < 0 ? 'Saldo insuficiente para esta saída.' : `Saldo após a movimentação: ${fmt.number(after)} ${selected?.unit ?? ''}`) : undefined}>
            <Input inputMode="decimal" className="tabular" autoFocus aria-invalid={!!errors.quantity} placeholder="0" {...form.register('quantity')} />
          </Field>
          {kind === 'In' && (
            <Field label="Custo unitário" hint="Opcional.">
              <Controller control={form.control} name="unitCost" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} />} />
            </Field>
          )}
        </div>
        {kind === 'In' && (
          <Field label="Fornecedor" hint="Opcional.">
            <Select {...form.register('supplierId')} value={form.watch('supplierId')}><option value="">Não informado</option>{(suppliers.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
          </Field>
        )}
        <Field label={kind === 'In' ? 'Observação' : 'Motivo'} required={kind !== 'In'} error={errors.reason?.message}>
          <Textarea rows={2} maxLength={300} aria-invalid={!!errors.reason} placeholder={kind === 'Out' ? 'Ex.: uso na limpeza da piscina' : kind === 'Adjustment' ? 'Ex.: inventário mensal' : 'Ex.: nota fiscal 1234'} {...form.register('reason')} />
        </Field>
      </form>
    </Modal>
  )
}
