import { zodResolver } from '@hookform/resolvers/zod'
import { MoreHorizontal, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { DataTable, Toolbar, type Column } from '@/components/table'
import { Button, Card, Field, IconButton, Input, MaskedInput, Menu, Modal, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt, masks } from '@/lib/format'
import { useSuppliers, type Supplier } from './data'

const formatDocument = (value: string) => (value.replace(/\D/g, '').length > 11 ? masks.cnpj(value) : masks.cpf(value))

export function Suppliers() {
  const { can } = useAuth()
  const manage = can('stock.manage')
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [form, setForm] = useState<{ supplier?: Supplier } | null>(null)
  const list = useSuppliers()
  const remove = useMutation({
    mutationFn: (id: string) => api.delete(`/api/estoque/fornecedores/${id}`),
    onSuccess: () => {
      toast.success('Fornecedor excluído.')
      void queryClient.invalidateQueries({ queryKey: ['/api/estoque/fornecedores'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/estoque/produtos'] })
    },
    onError: toast.error,
  })

  const columns: Column<Supplier>[] = [
    { key: 'name', header: 'Fornecedor', render: (s) => <span className="font-medium text-ink">{s.name}</span> },
    { key: 'document', header: 'CPF/CNPJ', render: (s) => <span className="tabular text-muted">{s.document ? formatDocument(s.document) : '—'}</span> },
    { key: 'phone', header: 'Telefone', render: (s) => fmt.phone(s.phone) },
    { key: 'email', header: 'E-mail', render: (s) => s.email || <span className="text-muted">—</span> },
    { key: 'notes', header: 'Observações', render: (s) => <p className="max-w-xs truncate text-muted" title={s.notes ?? undefined}>{s.notes || '—'}</p> },
  ]

  return (
    <Card padded={false}>
      <Toolbar actions={manage && <Button variant="primary" size="sm" icon={Plus} onClick={() => setForm({})}>Novo fornecedor</Button>}>
        <p className="text-[13px] text-muted">Fornecedores podem ser vinculados aos produtos e às entradas de estoque.</p>
      </Toolbar>
      <DataTable columns={columns} rows={list.data ?? []} rowKey={(s) => s.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        rowActions={manage ? (supplier) => (
          <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
            { label: 'Editar', icon: Pencil, onClick: () => setForm({ supplier }) },
            {
              label: 'Excluir', icon: Trash2, danger: true, onClick: async () => {
                if (await confirm({ title: 'Excluir fornecedor?', message: `"${supplier.name}" será removido do cadastro. O histórico de movimentações é preservado.`, danger: true, confirmLabel: 'Excluir' })) remove.mutate(supplier.id)
              },
            },
          ]} />
        ) : undefined}
        empty={{ title: 'Nenhum fornecedor cadastrado' }} />
      {form && <SupplierForm supplier={form.supplier} onClose={() => setForm(null)} />}
    </Card>
  )
}

const schema = z.object({
  name: z.string().trim().min(2, 'Informe o nome do fornecedor').max(150, 'Máximo de 150 caracteres'),
  document: z.string().refine((v) => [0, 11, 14].includes(v.replace(/\D/g, '').length), 'Informe um CPF (11 dígitos) ou CNPJ (14 dígitos)'),
  phone: z.string(),
  email: z.union([z.literal(''), z.string().email('E-mail inválido')]),
  notes: z.string().max(1000, 'Máximo de 1.000 caracteres'),
})
type Values = z.infer<typeof schema>

function SupplierForm({ supplier, onClose }: { supplier?: Supplier; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { name: supplier?.name ?? '', document: formatDocument(supplier?.document ?? ''), phone: masks.phone(supplier?.phone ?? ''), email: supplier?.email ?? '', notes: supplier?.notes ?? '' },
  })
  const { errors } = form.formState
  const save = useMutation({
    mutationFn: (values: Values) => {
      const body: Schema<'SaveSupplierRequest'> = { name: values.name, document: emptyToNull(values.document), phone: emptyToNull(values.phone), email: emptyToNull(values.email), notes: emptyToNull(values.notes) }
      return supplier ? api.put<Supplier>(`/api/estoque/fornecedores/${supplier.id}`, body) : api.post<Supplier>('/api/estoque/fornecedores', body)
    },
    onSuccess: () => {
      toast.success(supplier ? 'Fornecedor atualizado.' : 'Fornecedor cadastrado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/estoque/fornecedores'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/estoque/produtos'] })
      onClose()
    },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} title={supplier ? 'Editar fornecedor' : 'Novo fornecedor'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{supplier ? 'Salvar alterações' : 'Cadastrar'}</Button>
      </>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-2" label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} maxLength={150} {...form.register('name')} /></Field>
        <Field label="CPF/CNPJ" error={errors.document?.message}>
          <Controller control={form.control} name="document" render={({ field }) => <Input inputMode="numeric" aria-invalid={!!errors.document} value={field.value} onChange={(e) => field.onChange(formatDocument(e.target.value))} />} />
        </Field>
        <Field label="Telefone">
          <Controller control={form.control} name="phone" render={({ field }) => <MaskedInput mask="phone" value={field.value} onChange={field.onChange} placeholder="(00) 00000-0000" />} />
        </Field>
        <Field className="col-span-2" label="E-mail" error={errors.email?.message}><Input type="email" aria-invalid={!!errors.email} {...form.register('email')} /></Field>
        <Field className="col-span-2" label="Observações" error={errors.notes?.message}><Textarea rows={3} {...form.register('notes')} /></Field>
      </form>
    </Modal>
  )
}
