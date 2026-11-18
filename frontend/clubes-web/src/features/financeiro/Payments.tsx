import { Download, FlaskConical, MoreHorizontal, Receipt, RotateCcw } from 'lucide-react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Button, Card, IconButton, Input, Menu, PageHeader, SearchInput, StatusBadge, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { EnumSelect, MemberLink, useInvalidateFinance, useReceiptDownload, useRefund } from './common'

type Payment = Schema<'PaymentDto'>

export default function PaymentsPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const invalidate = useInvalidateFinance()
  const [params, setParams] = useSearchParams()
  const filter = (key: string) => params.get(key) ?? ''
  const setFilter = (key: string, value: string) => setParams((current) => { if (value) current.set(key, value); else current.delete(key); return current }, { replace: true })
  const [status, method, from, to, memberId] = ['status', 'method', 'from', 'to', 'memberId'].map(filter)
  const list = usePaged<Payment>('/api/pagamentos', {
    status: status || undefined, method: method || undefined, memberId: memberId || undefined,
    from: from ? dates.startOfDay(from) : undefined, to: to ? dates.endOfDay(to) : undefined,
  })
  const receipt = useReceiptDownload()
  const { refund } = useRefund()

  const sandbox = useMutation({
    mutationFn: (id: string) => api.post<Payment>(`/api/pagamentos/${id}/sandbox`, { paid: true }),
    onSuccess: (payment) => { toast.success(`Pagamento ${payment.receiptNumber} confirmado no ambiente de homologação.`); invalidate() },
    onError: toast.error,
  })
  const confirmSandbox = async (payment: Payment) => {
    const ok = await confirm({
      title: 'Confirmar pagamento (homologação)', confirmLabel: 'Simular confirmação',
      message: <>Recurso <strong>exclusivo de homologação</strong>: simula a notificação do provedor para o pagamento pendente de {fmt.money(payment.amount)} iniciado pelo aplicativo. Só funciona com o gateway de testes (Sandbox) ativo; em produção a confirmação chega automaticamente do provedor.</>,
    })
    if (ok) sandbox.mutate(payment.id)
  }

  const columns: Column<Payment>[] = [
    { key: 'receipt', header: 'Recibo nº', width: '120px', render: (p) => <span className="tabular font-medium text-ink">{p.receiptNumber}</span> },
    { key: 'member', header: 'Associado', render: (p) => <MemberLink id={p.memberId} name={p.memberName} /> },
    { key: 'charge', header: 'Referente a', render: (p) => <span className="text-ink/80">{p.chargeDescription}</span> },
    { key: 'amount', header: 'Valor', align: 'right', render: (p) => <span className={p.status === 'Refunded' || p.status === 'Failed' ? 'text-muted line-through' : 'font-medium'}>{fmt.money(p.amount)}</span> },
    { key: 'method', header: 'Forma', render: (p) => labels.paymentMethod[p.method].label },
    { key: 'channel', header: 'Canal', render: (p) => <StatusBadge dict={labels.paymentChannel} value={p.channel} /> },
    { key: 'status', header: 'Situação', render: (p) => <span title={p.status === 'Refunded' ? `Estornado em ${fmt.dateTime(p.refundedAt)}: ${p.refundReason ?? ''}` : undefined}><StatusBadge dict={labels.paymentStatus} value={p.status} /></span> },
    { key: 'date', header: 'Data', render: (p) => <span className="tabular">{fmt.dateTime(p.paidAt ?? p.createdAt)}</span> },
    { key: 'by', header: 'Registrado por', render: (p) => <span className="text-muted">{p.registeredBy ?? (p.channel === 'App' ? 'Aplicativo' : '—')}</span> },
  ]

  return (
    <>
      <PageHeader title="Pagamentos" subtitle="Recebimentos registrados na secretaria e pelo aplicativo. Novos pagamentos são registrados a partir da cobrança." />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Associado ou nº do recibo…" />
          <EnumSelect dict={labels.paymentStatus} value={status} onChange={(v) => setFilter('status', v)} all="Todas as situações" className="w-44" />
          <EnumSelect dict={labels.paymentMethod} value={method} onChange={(v) => setFilter('method', v)} all="Todas as formas" className="w-44" />
          <Input type="date" aria-label="Pagos a partir de" title="Pagos a partir de" className="w-40" value={from} max={to || undefined} onChange={(e) => setFilter('from', e.target.value)} />
          <span className="text-sm text-muted">até</span>
          <Input type="date" aria-label="Pagos até" title="Pagos até" className="w-40" value={to} min={from || undefined} onChange={(e) => setFilter('to', e.target.value)} />
          {memberId && <Button size="sm" variant="ghost" onClick={() => setFilter('memberId', '')}>Limpar filtro por associado</Button>}
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(p) => p.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          onRowClick={(p) => navigate(`/financeiro/cobrancas/${p.chargeId}`)}
          rowActions={(p) => (
            <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
              { label: 'Ver cobrança', icon: Receipt, onClick: () => navigate(`/financeiro/cobrancas/${p.chargeId}`) },
              p.status === 'Confirmed' && { label: 'Baixar comprovante (PDF)', icon: Download, disabled: receipt.busyId === p.id, onClick: () => void receipt.download(p) },
              p.status === 'Confirmed' && can('finance.refund') && { label: 'Estornar', icon: RotateCcw, danger: true, onClick: () => void refund(p) },
              p.status === 'Pending' && p.channel === 'App' && can('finance.payments') && { label: 'Confirmar (homologação)', icon: FlaskConical, disabled: sandbox.isPending, onClick: () => void confirmSandbox(p) },
            ]} />
          )}
          empty={{ title: 'Nenhum pagamento encontrado', description: list.search || status || method || from || to || memberId ? 'Ajuste a pesquisa ou os filtros.' : 'Os pagamentos aparecem aqui assim que forem registrados.' }} />
      </Card>
    </>
  )
}
