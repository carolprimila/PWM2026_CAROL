import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, X } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { z } from 'zod'
import { MemberPicker } from '@/components/shared'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, Field, Input, Modal, MoneyInput, PageHeader, SearchInput, Select, StatusBadge, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { EnumSelect, MemberLink, useInvalidateFinance } from './common'

type Charge = Schema<'ChargeDto'>

export const chargeColumns: Column<Charge>[] = [
  { key: 'member', header: 'Associado', render: (c) => <MemberLink id={c.memberId} name={c.memberName} number={c.memberNumber} /> },
  { key: 'description', header: 'Cobrança', render: (c) => <div className="min-w-0"><p className="truncate font-medium text-ink">{c.description}</p><p className="text-xs text-muted">{labels.chargeType[c.type].label}</p></div> },
  { key: 'competence', header: 'Competência', render: (c) => <span className="tabular text-muted">{fmt.competence(c.competence)}</span> },
  { key: 'due', header: 'Vencimento', render: (c) => <span className={c.isOverdue ? 'tabular font-medium text-danger' : 'tabular'}>{fmt.day(c.dueDate)}</span> },
  { key: 'total', header: 'Total', align: 'right', render: (c) => fmt.money(c.total) },
  { key: 'paid', header: 'Pago', align: 'right', render: (c) => <span className="text-muted">{fmt.money(c.paidAmount)}</span> },
  { key: 'balance', header: 'Saldo', align: 'right', render: (c) => <span className="font-medium">{fmt.money(c.balance)}</span> },
  { key: 'status', header: 'Situação', render: (c) => <div className="flex items-center gap-1.5"><StatusBadge dict={labels.chargeStatus} value={c.status} />{c.isOverdue && <Badge tone="danger">Vencida</Badge>}</div> },
]

export default function ChargesPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const [params, setParams] = useSearchParams()
  const [creating, setCreating] = useState(false)
  const filter = (key: string) => params.get(key) ?? ''
  const setFilter = (key: string, value: string) => setParams((current) => { if (value) current.set(key, value); else current.delete(key); return current }, { replace: true })
  const [status, type, competence, overdue, memberId] = ['status', 'type', 'competence', 'overdue', 'memberId'].map(filter)
  const list = usePaged<Charge>('/api/financeiro/cobrancas', { status: status || undefined, type: type || undefined, competence: competence || undefined, overdue: overdue || undefined, memberId: memberId || undefined })
  const filtered = !!(list.search || status || type || competence || overdue || memberId)

  return (
    <>
      <PageHeader title="Cobranças" subtitle="Mensalidades, taxas, multas e demais cobranças lançadas aos associados."
        actions={can('finance.charges') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Nova cobrança avulsa</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Associado ou descrição…" />
          <EnumSelect dict={labels.chargeStatus} value={status} onChange={(v) => setFilter('status', v)} all="Todas as situações" className="w-44" />
          <EnumSelect dict={labels.chargeType} value={type} onChange={(v) => setFilter('type', v)} all="Todos os tipos" className="w-40" />
          <div className="w-40"><Select value={overdue} onChange={(e) => setFilter('overdue', e.target.value)}><option value="">Vencidas ou não</option><option value="true">Somente vencidas</option><option value="false">Não vencidas</option></Select></div>
          <Input type="month" aria-label="Competência" title="Competência" className="w-40" value={competence} onChange={(e) => setFilter('competence', e.target.value)} />
          {memberId && <Button size="sm" variant="ghost" icon={X} onClick={() => setFilter('memberId', '')}>Filtro por associado</Button>}
        </Toolbar>
        <DataTable columns={chargeColumns} rows={list.rows} rowKey={(c) => c.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          onRowClick={(c) => navigate(`/financeiro/cobrancas/${c.id}`)}
          empty={{ title: 'Nenhuma cobrança encontrada', description: filtered ? 'Ajuste a pesquisa ou os filtros.' : 'As mensalidades são geradas em Financeiro › Mensalidades; cobranças avulsas podem ser lançadas aqui.' }} />
      </Card>
      {creating && <ChargeForm onClose={() => setCreating(false)} onSaved={(id) => navigate(`/financeiro/cobrancas/${id}`)} />}
    </>
  )
}

const schema = z.object({
  member: z.object({ id: z.string(), name: z.string() }).nullable().refine((v) => v != null, 'Selecione o associado'),
  type: z.string().min(1),
  description: z.string().trim().min(3, 'Descreva a cobrança (mínimo de 3 caracteres)').max(200, 'Máximo de 200 caracteres'),
  amount: z.number().nullable().refine((v) => v != null && v > 0, 'Informe um valor maior que zero'),
  dueDate: z.string().min(1, 'Informe o vencimento'),
})
type Values = z.input<typeof schema>

function ChargeForm({ onClose, onSaved }: { onClose: () => void; onSaved: (id: string) => void }) {
  const toast = useToast()
  const invalidate = useInvalidateFinance()
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { member: null, type: 'Fee', description: '', amount: null, dueDate: dates.addDays(dates.today(), 7) } })
  const { errors } = form.formState
  const save = useMutation({
    mutationFn: (values: Values) => api.post<Schema<'ChargeDetailDto'>>('/api/financeiro/cobrancas', {
      memberId: values.member!.id, type: values.type as Schema<'ChargeType'>, description: values.description.trim(), amount: values.amount!, dueDate: values.dueDate,
    } satisfies Schema<'CreateChargeRequest'>),
    onSuccess: (detail) => { toast.success('Cobrança lançada. O associado foi notificado.'); invalidate(); onSaved(detail.charge.id) },
    onError: toast.error,
  })
  return (
    <Modal open onClose={onClose} title="Nova cobrança avulsa" description="Mensalidades são geradas pela rotina de competência; aqui são lançadas taxas, multas e outras cobranças."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>Lançar cobrança</Button>
      </>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-2" label="Associado" required error={errors.member?.message}>
          <Controller control={form.control} name="member" render={({ field }) => <MemberPicker value={field.value} onChange={field.onChange} onlyActive={false} />} />
        </Field>
        <Field label="Tipo" required>
          <Select {...form.register('type')}>{options(labels.chargeType).filter((o) => o.value !== 'Monthly').map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
        </Field>
        <Field label="Vencimento" required error={errors.dueDate?.message} hint="A competência é o mês do vencimento."><Input type="date" aria-invalid={!!errors.dueDate} {...form.register('dueDate')} /></Field>
        <Field className="col-span-2" label="Descrição" required error={errors.description?.message}><Input aria-invalid={!!errors.description} placeholder="Ex.: Taxa de segunda via da carteirinha" {...form.register('description')} /></Field>
        <Field label="Valor" required error={errors.amount?.message}>
          <Controller control={form.control} name="amount" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} aria-invalid={!!errors.amount} />} />
        </Field>
      </form>
    </Modal>
  )
}
