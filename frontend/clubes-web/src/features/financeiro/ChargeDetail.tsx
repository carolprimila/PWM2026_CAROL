import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, Ban, Download, HandCoins, Receipt, RotateCcw, SlidersHorizontal } from 'lucide-react'
import { useRef, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Badge, Button, Card, CardTitle, EmptyState, Field, IconButton, Info, Input, Modal, MoneyInput, PageHeader, QueryBoundary, Select, StatusBadge, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, errorMessage, newIdempotencyKey, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, emptyToNull, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { AmountRow, BackLink, useInvalidateFinance, useReceiptDownload, useRefund } from './common'

type Detail = Schema<'ChargeDetailDto'>
type Charge = Schema<'ChargeDto'>

export default function ChargeDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { can } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const invalidate = useInvalidateFinance()
  const [dialog, setDialog] = useState<'payment' | 'adjustment' | null>(null)
  const detail = useQuery({ queryKey: ['financeiro', 'cobranca', id], queryFn: () => api.get<Detail>(`/api/financeiro/cobrancas/${id}`) })
  const receipt = useReceiptDownload()
  const { refund, isPending: refunding } = useRefund()

  const cancel = useMutation({
    mutationFn: (reason: string) => api.post<Detail>(`/api/financeiro/cobrancas/${id}/cancelar`, { reason }),
    onSuccess: () => { toast.success('Cobrança cancelada.'); invalidate() },
    onError: toast.error,
  })

  return (
    <QueryBoundary query={detail}>
      {({ charge: c, composition, adjustments, payments }) => {
        const open = c.status === 'Open'
        return (
          <>
            <PageHeader
              back={<BackLink to="/financeiro/cobrancas"><ArrowLeft className="size-3.5" />Cobranças</BackLink>}
              title={c.description}
              subtitle={<span className="flex flex-wrap items-center gap-2">
                <Link to={`/associados/${c.memberId}`} className="font-medium text-brand-700 hover:underline">{c.memberName}</Link>
                <span>· matrícula {c.memberNumber}</span>
                <StatusBadge dict={labels.chargeType} value={c.type} />
                <StatusBadge dict={labels.chargeStatus} value={c.status} />
                {c.isOverdue && <Badge tone="danger">Vencida</Badge>}
              </span>}
              actions={<>
                {open && c.paidAmount === 0 && can('finance.charges') && (
                  <Button icon={Ban} loading={cancel.isPending} onClick={async () => {
                    const result = await confirm({ title: 'Cancelar cobrança', message: <>A cobrança de <strong>{fmt.money(c.total)}</strong> será cancelada e deixará de ser exigida do associado. O registro e o motivo ficam preservados.</>, reason: 'required', danger: true, confirmLabel: 'Cancelar cobrança' })
                    if (result) cancel.mutate(result.reason)
                  }}>Cancelar cobrança</Button>
                )}
                {open && can('finance.adjust') && <Button icon={SlidersHorizontal} onClick={() => setDialog('adjustment')}>Lançar ajuste</Button>}
                {open && c.balance > 0 && can('finance.payments') && <Button variant="primary" icon={HandCoins} onClick={() => setDialog('payment')}>Registrar pagamento</Button>}
              </>}
            />

            {c.status === 'Cancelled' && <Alert tone="warning" className="mb-6" title="Cobrança cancelada">{c.cancelReason ?? 'Sem motivo registrado.'}</Alert>}

            <div className="grid gap-6 xl:grid-cols-3">
              <Card className="xl:col-span-2">
                <CardTitle icon={Receipt} title="Composição" description="Retrato dos valores no momento da geração. Mudanças posteriores na mensalidade base ou nos dependentes não alteram esta cobrança." />
                <div className="divide-y divide-line/70">
                  {c.type === 'Monthly' && (
                    <>
                      <AmountRow label="Mensalidade base" value={c.baseFee} />
                      <AmountRow label={`Acréscimo por dependentes (${c.dependentsCount} considerado${c.dependentsCount === 1 ? '' : 's'})`} value={c.dependentsAmount} sign="+">
                        {composition.length > 0 && <ul className="mt-1 space-y-0.5 text-xs text-muted">{composition.map((name, index) => <li key={index}>{index + 1}. {name}</li>)}</ul>}
                        {c.dependentsCount === 0 && <p className="mt-0.5 text-xs text-muted">Nenhum dependente na data da geração.</p>}
                      </AmountRow>
                    </>
                  )}
                  <AmountRow label="Valor original" value={c.originalAmount} strong={c.type === 'Monthly'} />
                  <AmountRow label="Acréscimos" value={c.additions} sign="+" />
                  <AmountRow label="Descontos" value={c.discounts} sign="−" />
                  <AmountRow label="Multa" value={c.fineAmount} sign="+" />
                  <AmountRow label="Juros" value={c.interestAmount} sign="+" />
                  <AmountRow label="Taxas" value={c.feesAmount} sign="+" />
                  <AmountRow label="Total" value={c.total} strong />
                  <AmountRow label="Pago" value={c.paidAmount} sign="−" tone="success" />
                  <AmountRow label="Saldo" value={c.balance} strong tone={c.isOverdue ? 'danger' : undefined} />
                </div>
              </Card>

              <Card className="self-start">
                <CardTitle title="Dados da cobrança" />
                <dl className="grid grid-cols-2 gap-4">
                  <Info label="Competência">{fmt.competence(c.competence)}</Info>
                  <Info label="Vencimento"><span className={c.isOverdue ? 'font-medium text-danger' : ''}>{fmt.day(c.dueDate)}</span></Info>
                  <Info label="Tipo">{labels.chargeType[c.type].label}</Info>
                  <Info label="Lançada em">{fmt.dateTime(c.createdAt)}</Info>
                  {c.reservationId && <Info label="Origem" className="col-span-2"><Link to={`/reservas/${c.reservationId}`} className="font-medium text-brand-700 hover:underline">Ver reserva vinculada</Link></Info>}
                </dl>
              </Card>
            </div>

            <div className="mt-6 grid gap-6 xl:grid-cols-2">
              <Card padded={false}>
                <div className="px-5 pt-5"><CardTitle title="Ajustes" description="Descontos, acréscimos, multas, juros e taxas lançados nesta cobrança." /></div>
                {adjustments.length === 0 ? <EmptyState title="Nenhum ajuste lançado" /> : (
                  <ul className="divide-y divide-line/70 border-t border-line">
                    {adjustments.map((a) => (
                      <li key={a.id} className="flex items-start gap-3 px-5 py-3 text-sm">
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-1.5"><StatusBadge dict={labels.adjustmentKind} value={a.kind} />{a.isAutomatic && <Badge tone="info">Automático</Badge>}</div>
                          <p className="mt-1 text-ink/80">{a.reason}</p>
                          <p className="mt-0.5 text-xs text-muted">{fmt.dateTime(a.at)}</p>
                        </div>
                        <span className="tabular shrink-0 font-medium text-ink">{a.kind === 'Discount' ? '− ' : '+ '}{fmt.money(a.amount)}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </Card>

              <Card padded={false}>
                <div className="px-5 pt-5"><CardTitle title="Pagamentos" description="Estornos preservam o registro e reabrem a cobrança." /></div>
                {payments.length === 0 ? <EmptyState title="Nenhum pagamento registrado" /> : (
                  <ul className="divide-y divide-line/70 border-t border-line">
                    {payments.map((p) => (
                      <li key={p.id} className="flex items-start gap-3 px-5 py-3 text-sm">
                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-center gap-1.5"><span className="tabular font-medium text-ink">Recibo {p.receiptNumber}</span><StatusBadge dict={labels.paymentStatus} value={p.status} /></div>
                          <p className="mt-1 text-xs text-muted">
                            {labels.paymentMethod[p.method].label} · {labels.paymentChannel[p.channel].label} · {fmt.dateTime(p.paidAt ?? p.createdAt)}{p.registeredBy ? ` · ${p.registeredBy}` : ''}
                          </p>
                          {p.notes && <p className="mt-0.5 text-xs text-ink/75">{p.notes}</p>}
                          {p.status === 'Refunded' && <p className="mt-0.5 text-xs text-danger">Estornado em {fmt.dateTime(p.refundedAt)}: {p.refundReason}</p>}
                        </div>
                        <span className={p.status === 'Confirmed' ? 'tabular shrink-0 font-medium text-ink' : 'tabular shrink-0 text-muted line-through'}>{fmt.money(p.amount)}</span>
                        <div className="flex shrink-0 items-center">
                          {p.status === 'Confirmed' && <IconButton icon={Download} label="Baixar comprovante (PDF)" disabled={receipt.busyId === p.id} onClick={() => receipt.download(p)} />}
                          {p.status === 'Confirmed' && can('finance.refund') && <IconButton icon={RotateCcw} label="Estornar pagamento" disabled={refunding} onClick={() => refund(p)} />}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </Card>
            </div>

            {dialog === 'payment' && <PaymentDialog charge={c} onClose={() => setDialog(null)} />}
            {dialog === 'adjustment' && <AdjustmentDialog charge={c} onClose={() => setDialog(null)} />}
          </>
        )
      }}
    </QueryBoundary>
  )
}

// ---------- Registrar pagamento (secretaria) ----------

function PaymentDialog({ charge, onClose }: { charge: Charge; onClose: () => void }) {
  const toast = useToast()
  const invalidate = useInvalidateFinance()
  // Uma chave por abertura do diálogo: repetir o envio após erro ou queda de conexão não duplica o pagamento.
  const idempotencyKey = useRef(newIdempotencyKey())
  const schema = z.object({
    amount: z.number().nullable()
      .refine((v) => v != null && v > 0, 'Informe um valor maior que zero')
      .refine((v) => v == null || v <= charge.balance, `O valor não pode ser maior que o saldo (${fmt.money(charge.balance)})`),
    method: z.string().min(1),
    paidAt: z.string().refine((v) => !v || new Date(v).getTime() <= Date.now() + 5 * 60_000, 'A data do pagamento não pode ser futura'),
    notes: z.string().max(500, 'Máximo de 500 caracteres'),
  })
  type Values = z.input<typeof schema>
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { amount: charge.balance, method: 'Pix', paidAt: '', notes: '' } })
  const { errors } = form.formState
  const amount = form.watch('amount')

  const pay = useMutation({
    mutationFn: (values: Values) => api.post<Schema<'PaymentDto'>>('/api/pagamentos', {
      chargeId: charge.id, amount: values.amount!, method: values.method as Schema<'PaymentMethod'>, paidAt: dates.fromInput(values.paidAt),
      notes: emptyToNull(values.notes), idempotencyKey: idempotencyKey.current,
    } satisfies Schema<'RegisterPaymentRequest'>),
    onSuccess: (payment) => { toast.success(`Pagamento de ${fmt.money(payment.amount)} confirmado. Recibo nº ${payment.receiptNumber}.`); invalidate(); onClose() },
    onError: () => invalidate(),
  })

  return (
    <Modal open onClose={onClose} title="Registrar pagamento" description={`${charge.description} · ${charge.memberName}`}
      footer={<>
        <Button onClick={onClose} disabled={pay.isPending}>Cancelar</Button>
        <Button variant="primary" loading={pay.isPending} onClick={form.handleSubmit((values) => pay.mutate(values))}>{pay.isError ? 'Tentar novamente' : 'Confirmar recebimento'}</Button>
      </>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        {pay.isError && (
          <Alert tone="danger" className="col-span-2" title="O pagamento não foi confirmado pelo servidor">
            {errorMessage(pay.error)} Você pode tentar novamente com segurança: a mesma tentativa nunca é registrada duas vezes.
          </Alert>
        )}
        <dl className="col-span-2 grid grid-cols-3 gap-4 rounded-(--radius-control) bg-canvas px-4 py-3">
          <Info label="Total da cobrança"><span className="tabular">{fmt.money(charge.total)}</span></Info>
          <Info label="Já pago"><span className="tabular">{fmt.money(charge.paidAmount)}</span></Info>
          <Info label="Saldo"><span className="tabular font-semibold">{fmt.money(charge.balance)}</span></Info>
        </dl>
        <Field label="Valor recebido" required error={errors.amount?.message}
          hint={amount != null && amount > 0 && amount < charge.balance ? `Pagamento parcial: restarão ${fmt.money(charge.balance - amount)} em aberto.` : 'Multa e juros por atraso são atualizados pelo servidor antes do registro.'}>
          <Controller control={form.control} name="amount" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} aria-invalid={!!errors.amount} />} />
        </Field>
        <Field label="Forma de pagamento" required>
          <Select {...form.register('method')}>{options(labels.paymentMethod).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
        </Field>
        <Field label="Data e hora do pagamento" error={errors.paidAt?.message} hint="Em branco, vale o momento do registro."><Input type="datetime-local" max={dates.toInput(new Date().toISOString())} aria-invalid={!!errors.paidAt} {...form.register('paidAt')} /></Field>
        <Field label="Canal"><Input disabled value="Secretaria" /></Field>
        <Field className="col-span-2" label="Observações" error={errors.notes?.message}><Textarea rows={2} {...form.register('notes')} /></Field>
      </form>
    </Modal>
  )
}

// ---------- Lançar ajuste ----------

function AdjustmentDialog({ charge, onClose }: { charge: Charge; onClose: () => void }) {
  const toast = useToast()
  const invalidate = useInvalidateFinance()
  const schema = z.object({
    kind: z.string().min(1),
    amount: z.number().nullable().refine((v) => v != null && v > 0, 'Informe um valor maior que zero'),
    reason: z.string().trim().min(3, 'Informe a justificativa (mínimo de 3 caracteres)').max(500, 'Máximo de 500 caracteres'),
  }).refine((v) => v.kind !== 'Discount' || v.amount == null || v.amount <= charge.balance, { path: ['amount'], message: `O desconto não pode ser maior que o saldo (${fmt.money(charge.balance)})` })
  type Values = z.input<typeof schema>
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { kind: 'Discount', amount: null, reason: '' } })
  const { errors } = form.formState
  const kind = form.watch('kind')
  const amount = form.watch('amount')

  const adjust = useMutation({
    mutationFn: (values: Values) => api.post<Detail>(`/api/financeiro/cobrancas/${charge.id}/ajustes`, {
      kind: values.kind as Schema<'AdjustmentKind'>, amount: values.amount!, reason: values.reason.trim(),
    } satisfies Schema<'AdjustChargeRequest'>),
    onSuccess: () => { toast.success('Ajuste lançado.'); invalidate(); onClose() },
    onError: toast.error,
  })
  const settles = kind === 'Discount' && amount != null && amount === charge.balance

  return (
    <Modal open onClose={onClose} title="Lançar ajuste" description={`${charge.description} · saldo atual ${fmt.money(charge.balance)}`}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={adjust.isPending} onClick={form.handleSubmit((values) => adjust.mutate(values))}>Lançar ajuste</Button>
      </>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field label="Tipo de ajuste" required hint={kind === 'Discount' ? 'Reduz o saldo da cobrança.' : 'Aumenta o saldo da cobrança.'}>
          <Select {...form.register('kind')}>{options(labels.adjustmentKind).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
        </Field>
        <Field label="Valor" required error={errors.amount?.message}>
          <Controller control={form.control} name="amount" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} aria-invalid={!!errors.amount} />} />
        </Field>
        <Field className="col-span-2" label="Justificativa" required error={errors.reason?.message} hint="Fica registrada na auditoria.">
          <Textarea rows={3} aria-invalid={!!errors.reason} {...form.register('reason')} />
        </Field>
        {settles && <Alert tone="warning" className="col-span-2">Um desconto igual ao saldo quita a cobrança.</Alert>}
        {kind !== 'Discount' && kind !== 'Interest' && <Alert tone="info" className="col-span-2">O associado será notificado sobre o acréscimo e o motivo informado.</Alert>}
      </form>
    </Modal>
  )
}
