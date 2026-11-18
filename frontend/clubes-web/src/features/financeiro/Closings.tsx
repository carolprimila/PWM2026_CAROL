import { zodResolver } from '@hookform/resolvers/zod'
import { Lock, LockOpen } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { DataTable, type Column } from '@/components/table'
import { Alert, Button, Card, Field, Input, Modal, PageHeader, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { competenceOf, useInvalidateFinance } from './common'

type Closing = Schema<'ClosingDto'>
const PATH = '/api/financeiro/fechamentos'

export default function ClosingsPage() {
  const { can } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const invalidate = useInvalidateFinance()
  const canClose = can('finance.close')
  const [closing, setClosing] = useState(false)
  const closings = useQuery({ queryKey: [PATH], queryFn: () => api.get<Closing[]>(PATH) })

  const reopen = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => api.post(`${PATH}/${id}/reabrir`, { reason }),
    onSuccess: () => { toast.success('Competência reaberta.'); invalidate() },
    onError: toast.error,
  })

  const columns: Column<Closing>[] = [
    { key: 'competence', header: 'Competência', render: (c) => <div><p className="tabular font-medium text-ink">{fmt.competence(c.competence)}</p>{c.notes && <p className="max-w-56 truncate text-xs text-muted" title={c.notes}>{c.notes}</p>}</div> },
    { key: 'issued', header: 'Cobranças emitidas', align: 'right', render: (c) => fmt.money(c.chargesIssued) },
    { key: 'received', header: 'Pagamentos recebidos', align: 'right', render: (c) => fmt.money(c.paymentsReceived) },
    { key: 'other', header: 'Outras receitas', align: 'right', render: (c) => fmt.money(c.otherRevenue) },
    { key: 'expenses', header: 'Despesas', align: 'right', render: (c) => fmt.money(c.expenses) },
    {
      key: 'net', header: 'Resultado', align: 'right',
      render: (c) => { const net = c.paymentsReceived + c.otherRevenue - c.expenses; return <span className={net < 0 ? 'font-medium text-danger' : 'font-medium'}>{fmt.money(net)}</span> },
    },
    { key: 'open', header: 'Saldo em aberto', align: 'right', render: (c) => <span className={c.openBalance > 0 ? 'text-warning' : 'text-muted'}>{fmt.money(c.openBalance)}</span> },
    { key: 'closed', header: 'Fechado', render: (c) => <div><p className="tabular">{fmt.dateTime(c.closedAt)}</p><p className="text-xs text-muted">{c.closedBy ?? '—'}</p></div> },
  ]

  return (
    <>
      <PageHeader title="Fechamento mensal" subtitle="Consolida os totais de cada competência e bloqueia novas movimentações nela."
        actions={canClose && <Button variant="primary" icon={Lock} onClick={() => setClosing(true)}>Fechar competência</Button>} />
      <Card padded={false}>
        <DataTable columns={columns} rows={closings.data ?? []} rowKey={(c) => c.id} isLoading={closings.isLoading} error={closings.error} onRetry={() => closings.refetch()}
          rowActions={canClose ? (c) => (
            <Button size="sm" icon={LockOpen} disabled={reopen.isPending} onClick={async () => {
              const result = await confirm({
                title: `Reabrir ${fmt.competence(c.competence)}`, reason: 'required', danger: true, confirmLabel: 'Reabrir competência',
                message: 'O fechamento é removido e a competência volta a aceitar lançamentos, pagamentos, estornos e cancelamentos. Os totais consolidados serão recalculados quando ela for fechada novamente.',
              })
              if (result) reopen.mutate({ id: c.id, reason: result.reason })
            }}>Reabrir</Button>
          ) : undefined}
          empty={{ title: 'Nenhuma competência fechada', description: 'Feche o mês depois de conferir pagamentos, receitas e despesas.' }} />
      </Card>
      {closing && <CloseForm closed={(closings.data ?? []).map((c) => c.competence)} onClose={() => setClosing(false)} />}
    </>
  )
}

function CloseForm({ closed, onClose }: { closed: string[]; onClose: () => void }) {
  const toast = useToast()
  const invalidate = useInvalidateFinance()
  const last = competenceOf(-1)
  const schema = z.object({
    competence: z.string().regex(/^\d{4}-(0[1-9]|1[0-2])$/, 'Informe a competência')
      .refine((v) => v <= last, 'Só é possível fechar competências já encerradas')
      .refine((v) => !closed.includes(v), 'Esta competência já está fechada'),
    notes: z.string().max(1000, 'Máximo de 1000 caracteres'),
  })
  type Values = z.input<typeof schema>
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { competence: last, notes: '' } })
  const { errors } = form.formState
  const close = useMutation({
    mutationFn: (values: Values) => api.post<Closing>(PATH, { competence: values.competence, notes: emptyToNull(values.notes) } satisfies Schema<'CloseMonthRequest'>),
    onSuccess: (closing) => { toast.success(`Competência ${fmt.competence(closing.competence)} fechada.`); invalidate(); onClose() },
    onError: toast.error,
  })
  return (
    <Modal open onClose={onClose} title="Fechar competência"
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" icon={Lock} loading={close.isPending} onClick={form.handleSubmit((values) => close.mutate(values))}>Fechar competência</Button>
      </>}>
      <form className="space-y-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Alert tone="warning" title="Após o fechamento, a competência fica bloqueada">
          Não será possível gerar ou cancelar cobranças, estornar pagamentos nem marcar ou cancelar lançamentos pagos nessa competência. Para corrigir algo depois, será preciso reabri-la com justificativa.
        </Alert>
        <Field label="Competência" required error={errors.competence?.message} hint="Somente meses já encerrados.">
          <Input type="month" max={last} className="w-48" aria-invalid={!!errors.competence} {...form.register('competence')} />
        </Field>
        <Field label="Observações" error={errors.notes?.message}><Textarea rows={3} {...form.register('notes')} /></Field>
      </form>
    </Modal>
  )
}
