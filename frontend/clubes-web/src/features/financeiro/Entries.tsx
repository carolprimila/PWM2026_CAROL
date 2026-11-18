import { zodResolver } from '@hookform/resolvers/zod'
import { Ban, CheckCircle2, Download, FileText, MoreHorizontal, Pencil, Plus, X } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, Field, FileUpload, IconButton, Input, Menu, Modal, MoneyInput, PageHeader, SearchInput, Select, StatusBadge, Tabs, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { EnumSelect, useInvalidateFinance } from './common'

type Entry = Schema<'FinancialEntryDto'>
type Kind = Schema<'FinancialEntryKind'>
type KindTab = 'all' | Kind
const PATH = '/api/financeiro/lancamentos'
const settledLabel = (kind: Kind) => (kind === 'Revenue' ? 'recebido' : 'pago')

/** Resumo das contas pendentes de um tipo (até 200 lançamentos; acima disso o valor é indicado como parcial). */
function PendingChip({ kind, label }: { kind: Kind; label: string }) {
  const pending = useQuery({ queryKey: [PATH, 'pendentes', kind], queryFn: () => api.get<Paged<Entry>>(PATH, { kind, status: 'Pending', pageSize: 200 }) })
  const items = pending.data?.items ?? []
  const overdue = items.filter((e) => e.isOverdue)
  const partial = (pending.data?.total ?? 0) > items.length
  return (
    <Card className="flex flex-1 items-center justify-between gap-4 py-4!">
      <div>
        <p className="text-[13px] text-muted">{label}</p>
        <p className="tabular mt-0.5 text-xl font-semibold tracking-tight text-ink">{pending.isLoading ? '…' : pending.error ? '—' : `${partial ? '≥ ' : ''}${fmt.money(items.reduce((total, e) => total + e.amount, 0))}`}</p>
      </div>
      <div className="flex flex-wrap justify-end gap-1.5">
        <Badge tone="warning">{fmt.number(pending.data?.total ?? 0)} pendente(s)</Badge>
        {overdue.length > 0 && <Badge tone="danger">{fmt.number(overdue.length)} vencido(s) · {fmt.money(overdue.reduce((total, e) => total + e.amount, 0))}</Badge>}
      </div>
    </Card>
  )
}

export default function EntriesPage() {
  const { can } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const invalidate = useInvalidateFinance()
  const canEdit = can('finance.entries')
  const [kind, setKind] = useState<KindTab>('all')
  const [status, setStatus] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [form, setForm] = useState<{ entry?: Entry } | null>(null)
  const [paying, setPaying] = useState<Entry | null>(null)
  const list = usePaged<Entry>(PATH, { kind: kind === 'all' ? undefined : kind, status: status || undefined, from: from || undefined, to: to || undefined })

  const cancel = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => api.post<Entry>(`${PATH}/${id}/cancelar`, { reason }),
    onSuccess: () => { toast.success('Lançamento cancelado.'); invalidate() },
    onError: toast.error,
  })
  const askCancel = async (entry: Entry) => {
    const result = await confirm({
      title: 'Cancelar lançamento', danger: true, confirmLabel: 'Cancelar lançamento', reason: 'required',
      message: <>{labels.entryKind[entry.kind].label} "{entry.description}" de <strong>{fmt.money(entry.amount)}</strong>. Lançamentos nunca são apagados: o registro permanece com o motivo do cancelamento.</>,
    })
    if (result) cancel.mutate({ id: entry.id, reason: result.reason })
  }
  const downloadReceipt = (entry: Entry) => api.download(`/api/arquivos/${entry.receiptFileId}`, undefined, 'comprovante').catch(toast.error)

  const columns: Column<Entry>[] = [
    {
      key: 'description', header: 'Lançamento',
      render: (e) => (
        <div className="min-w-0">
          <p className="flex items-center gap-1.5 truncate font-medium text-ink">{e.description}{e.receiptFileId && <span title="Comprovante anexado"><FileText className="size-3.5 text-muted" /></span>}</p>
          <p className="truncate text-xs text-muted">{[e.category, e.counterparty].filter(Boolean).join(' · ')}</p>
          {e.status === 'Cancelled' && e.cancelReason && <p className="truncate text-xs text-muted">Motivo: {e.cancelReason}</p>}
        </div>
      ),
    },
    { key: 'kind', header: 'Tipo', render: (e) => <StatusBadge dict={labels.entryKind} value={e.kind} /> },
    { key: 'due', header: 'Vencimento', render: (e) => <span className={e.isOverdue ? 'tabular font-medium text-danger' : 'tabular'}>{fmt.day(e.dueDate)}</span> },
    { key: 'paidOn', header: 'Pago/recebido em', render: (e) => <span className="tabular text-muted">{fmt.day(e.paidOn)}</span> },
    { key: 'amount', header: 'Valor', align: 'right', render: (e) => <span className={e.status === 'Cancelled' ? 'text-muted line-through' : 'font-medium'}>{fmt.money(e.amount)}</span> },
    {
      key: 'status', header: 'Situação',
      render: (e) => (
        <div className="flex items-center gap-1.5">
          {e.status === 'Paid' ? <Badge tone="success">{e.kind === 'Revenue' ? 'Recebido' : 'Pago'}</Badge> : <StatusBadge dict={labels.entryStatus} value={e.status} />}
          {e.isOverdue && <Badge tone="danger">Vencido</Badge>}
        </div>
      ),
    },
  ]
  const categories = [...new Set(list.rows.map((e) => e.category))].sort((a, b) => a.localeCompare(b, 'pt-BR'))

  return (
    <>
      <PageHeader title="Receitas e despesas" subtitle="Contas a pagar e a receber do clube, além das cobranças dos associados."
        actions={canEdit && <Button variant="primary" icon={Plus} onClick={() => setForm({})}>Novo lançamento</Button>} />

      <div className="mb-6 flex flex-wrap gap-4">
        <PendingChip kind="Revenue" label="Contas a receber" />
        <PendingChip kind="Expense" label="Contas a pagar" />
      </div>

      <Card padded={false}>
        <Tabs<KindTab> className="mb-0! px-4 pt-1" value={kind} onChange={setKind} tabs={[{ key: 'all', label: 'Todos' }, { key: 'Revenue', label: 'Receitas' }, { key: 'Expense', label: 'Despesas' }]} />
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Descrição, categoria ou favorecido…" />
          <EnumSelect dict={labels.entryStatus} value={status} onChange={setStatus} all="Todas as situações" className="w-44" />
          <Input type="date" aria-label="Vencimento a partir de" title="Vencimento a partir de" className="w-40" value={from} max={to || undefined} onChange={(e) => setFrom(e.target.value)} />
          <span className="text-sm text-muted">até</span>
          <Input type="date" aria-label="Vencimento até" title="Vencimento até" className="w-40" value={to} min={from || undefined} onChange={(e) => setTo(e.target.value)} />
          {(from || to) && <IconButton icon={X} label="Limpar período" onClick={() => { setFrom(''); setTo('') }} />}
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(e) => e.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          rowActions={(e) => (
            <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
              canEdit && e.status === 'Pending' && { label: `Marcar como ${settledLabel(e.kind)}`, icon: CheckCircle2, onClick: () => setPaying(e) },
              canEdit && e.status === 'Pending' && { label: 'Editar', icon: Pencil, onClick: () => setForm({ entry: e }) },
              !!e.receiptFileId && { label: 'Baixar comprovante', icon: Download, onClick: () => void downloadReceipt(e) },
              canEdit && e.status !== 'Cancelled' && { label: 'Cancelar', icon: Ban, danger: true, onClick: () => void askCancel(e) },
            ]} />
          )}
          empty={{ title: 'Nenhum lançamento encontrado', description: list.search || status || from || to || kind !== 'all' ? 'Ajuste a pesquisa ou os filtros.' : 'Registre as receitas e despesas do clube para acompanhar o fluxo de caixa.' }} />
      </Card>

      {form && <EntryForm entry={form.entry} defaultKind={kind === 'all' ? 'Expense' : kind} categories={categories} onClose={() => setForm(null)} />}
      {paying && <PayDialog entry={paying} onClose={() => setPaying(null)} />}
    </>
  )
}

// ---------- Comprovante (arquivo restrito) ----------

function ReceiptField({ fileId, onChange }: { fileId: string | null; onChange: (fileId: string | null) => void }) {
  const toast = useToast()
  return (
    <div className="flex items-center gap-3">
      {fileId ? (
        <>
          <span className="inline-flex items-center gap-1.5 text-sm text-ink"><FileText className="size-4 text-muted" />Comprovante anexado</span>
          <Button size="sm" icon={Download} onClick={() => api.download(`/api/arquivos/${fileId}`, undefined, 'comprovante').catch(toast.error)}>Baixar</Button>
          <Button size="sm" variant="ghost" icon={X} onClick={() => onChange(null)}>Remover</Button>
        </>
      ) : <FileUpload size="sm" scope="Restricted" label="Anexar comprovante" onUploaded={(file) => onChange(file.id)} />}
    </div>
  )
}

// ---------- Criar / editar ----------

const schema = z.object({
  kind: z.string().min(1),
  category: z.string().trim().min(1, 'Informe a categoria').max(80, 'Máximo de 80 caracteres'),
  description: z.string().trim().min(3, 'Descreva o lançamento (mínimo de 3 caracteres)').max(200, 'Máximo de 200 caracteres'),
  counterparty: z.string().max(150, 'Máximo de 150 caracteres'),
  amount: z.number().nullable().refine((v) => v != null && v > 0, 'Informe um valor maior que zero'),
  dueDate: z.string().min(1, 'Informe o vencimento'),
  notes: z.string().max(1000, 'Máximo de 1000 caracteres'),
  receiptFileId: z.string().nullable(),
})
type Values = z.input<typeof schema>

function EntryForm({ entry, defaultKind, categories, onClose }: { entry?: Entry; defaultKind: Kind; categories: string[]; onClose: () => void }) {
  const toast = useToast()
  const invalidate = useInvalidateFinance()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      kind: entry?.kind ?? defaultKind, category: entry?.category ?? '', description: entry?.description ?? '', counterparty: entry?.counterparty ?? '',
      amount: entry?.amount ?? null, dueDate: entry?.dueDate ?? dates.today(), notes: entry?.notes ?? '', receiptFileId: entry?.receiptFileId ?? null,
    },
  })
  const { errors } = form.formState
  const kind = form.watch('kind') as Kind
  const save = useMutation({
    mutationFn: (values: Values) => {
      const body = {
        kind: values.kind as Kind, category: values.category.trim(), description: values.description.trim(), counterparty: emptyToNull(values.counterparty), amount: values.amount!,
        dueDate: values.dueDate, notes: emptyToNull(values.notes), maintenanceOrderId: entry?.maintenanceOrderId ?? null, receiptFileId: values.receiptFileId,
      } satisfies Schema<'SaveEntryRequest'>
      return entry ? api.put<Entry>(`${PATH}/${entry.id}`, body) : api.post<Entry>(PATH, body)
    },
    onSuccess: () => { toast.success(entry ? 'Lançamento atualizado.' : 'Lançamento registrado.'); invalidate(); onClose() },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} title={entry ? 'Editar lançamento' : 'Novo lançamento'} description="O lançamento entra como pendente até ser marcado como pago ou recebido."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{entry ? 'Salvar alterações' : 'Registrar'}</Button>
      </>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field label="Tipo" required><Select {...form.register('kind')}><option value="Expense">Despesa (conta a pagar)</option><option value="Revenue">Receita (conta a receber)</option></Select></Field>
        <Field label="Categoria" required error={errors.category?.message}>
          <Input list="financeiro-categorias" aria-invalid={!!errors.category} placeholder={kind === 'Revenue' ? 'Ex.: Patrocínio' : 'Ex.: Energia elétrica'} {...form.register('category')} />
          <datalist id="financeiro-categorias">{categories.map((category) => <option key={category} value={category} />)}</datalist>
        </Field>
        <Field className="col-span-2" label="Descrição" required error={errors.description?.message}><Input aria-invalid={!!errors.description} {...form.register('description')} /></Field>
        <Field className="col-span-2" label={kind === 'Revenue' ? 'Pagador' : 'Fornecedor / favorecido'} error={errors.counterparty?.message}><Input {...form.register('counterparty')} /></Field>
        <Field label="Valor" required error={errors.amount?.message}>
          <Controller control={form.control} name="amount" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} aria-invalid={!!errors.amount} />} />
        </Field>
        <Field label="Vencimento" required error={errors.dueDate?.message}><Input type="date" aria-invalid={!!errors.dueDate} {...form.register('dueDate')} /></Field>
        <Field className="col-span-2" label="Observações" error={errors.notes?.message}><Textarea rows={2} {...form.register('notes')} /></Field>
        <div className="col-span-2">
          <span className="mb-1.5 block text-[13px] font-medium text-ink">Comprovante ou nota fiscal</span>
          <Controller control={form.control} name="receiptFileId" render={({ field }) => <ReceiptField fileId={field.value} onChange={field.onChange} />} />
        </div>
      </form>
    </Modal>
  )
}

// ---------- Marcar como pago / recebido ----------

function PayDialog({ entry, onClose }: { entry: Entry; onClose: () => void }) {
  const toast = useToast()
  const invalidate = useInvalidateFinance()
  const [paidOn, setPaidOn] = useState(dates.today())
  const [receiptFileId, setReceiptFileId] = useState<string | null>(null)
  const invalid = !paidOn || paidOn > dates.today()
  const pay = useMutation({
    mutationFn: () => api.post<Entry>(`${PATH}/${entry.id}/pagar`, { paidOn, receiptFileId } satisfies Schema<'PayEntryRequest'>),
    onSuccess: () => { toast.success(`Lançamento marcado como ${settledLabel(entry.kind)}.`); invalidate(); onClose() },
    onError: toast.error,
  })
  return (
    <Modal open onClose={onClose} size="sm" title={`Marcar como ${settledLabel(entry.kind)}`} description={`${entry.description} · ${fmt.money(entry.amount)}`}
      footer={<>
        <Button onClick={onClose}>Voltar</Button>
        <Button variant="primary" loading={pay.isPending} disabled={invalid} onClick={() => pay.mutate()}>Confirmar</Button>
      </>}>
      <div className="space-y-4">
        <Field label={entry.kind === 'Revenue' ? 'Data do recebimento' : 'Data do pagamento'} required error={invalid ? 'Informe uma data que não seja futura' : undefined}
          hint="O valor entra no fluxo de caixa nesta data.">
          <Input type="date" max={dates.today()} value={paidOn} aria-invalid={invalid} onChange={(e) => setPaidOn(e.target.value)} />
        </Field>
        <div>
          <span className="mb-1.5 block text-[13px] font-medium text-ink">Comprovante{entry.receiptFileId ? ' (substitui o anexo atual)' : ''}</span>
          <ReceiptField fileId={receiptFileId} onChange={setReceiptFileId} />
        </div>
      </div>
    </Modal>
  )
}
