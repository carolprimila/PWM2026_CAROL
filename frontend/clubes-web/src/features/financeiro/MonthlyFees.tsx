import { CalendarPlus, CircleDollarSign, Clock, RefreshCw, Wallet } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { DataTable, Toolbar, usePaged } from '@/components/table'
import { Alert, Button, Card, Info, Input, Modal, PageHeader, SearchInput, StatCard, useConfirm, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { chargeColumns } from './Charges'
import { competenceOf, EnumSelect, useInvalidateFinance } from './common'

type Charge = Schema<'ChargeDto'>
type Result = Schema<'GenerateMonthlyResult'>
const columns = chargeColumns.filter((column) => column.key !== 'competence')

export default function MonthlyFeesPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const invalidate = useInvalidateFinance()
  const [competence, setCompetence] = useState(competenceOf())
  const [status, setStatus] = useState('')
  const [result, setResult] = useState<Result | null>(null)
  const [pendingSettings, setPendingSettings] = useState<string | null>(null)
  const list = usePaged<Charge>('/api/mensalidades', { competence, status: status || undefined }, { pageSize: 100 })

  const generate = useMutation({
    mutationFn: () => api.post<Result>('/api/mensalidades/gerar', { competence } satisfies Schema<'GenerateMonthlyRequest'>),
    onSuccess: (data) => { setPendingSettings(null); setResult(data); invalidate() },
    onError: (error) => {
      // Parâmetros financeiros ainda não definidos pela administração: orienta para Configurações.
      if (error instanceof ApiError && error.code === 'configuracao_pendente') setPendingSettings(error.message)
      else toast.error(error)
    },
  })
  const lateFees = useMutation({
    mutationFn: () => api.post<{ updated: number }>('/api/mensalidades/atualizar-encargos'),
    onSuccess: ({ updated }) => { toast.success(updated === 0 ? 'Nenhuma cobrança precisou de atualização.' : `${fmt.number(updated)} cobrança(s) com multa e juros atualizados.`); invalidate() },
    onError: toast.error,
  })

  const active = list.rows.filter((c) => c.status !== 'Cancelled')
  const sum = (pick: (c: Charge) => number) => active.reduce((total, c) => total + pick(c), 0)
  const partial = list.total > list.rows.length
  const scope = partial ? `Soma das ${list.rows.length} mensalidades desta página` : status ? 'Soma das mensalidades filtradas' : 'Soma das mensalidades da competência'

  return (
    <>
      <PageHeader title="Mensalidades" subtitle="Geração e acompanhamento das mensalidades por competência."
        actions={<>
          {can('finance.adjust') && (
            <Button icon={RefreshCw} loading={lateFees.isPending} onClick={async () => {
              if (await confirm({ title: 'Atualizar multas e juros?', message: 'Recalcula multa e juros de todas as cobranças vencidas além da tolerância, com os percentuais registrados em cada cobrança. Esta rotina também é executada automaticamente.', confirmLabel: 'Atualizar' })) lateFees.mutate()
            }}>Atualizar multas e juros</Button>
          )}
          {can('finance.charges') && (
            <Button variant="primary" icon={CalendarPlus} loading={generate.isPending} disabled={!competence} onClick={async () => {
              const ok = await confirm({
                title: `Gerar mensalidades de ${fmt.competence(competence)}?`, confirmLabel: 'Gerar mensalidades',
                message: <>Será criada uma mensalidade para cada associado ativo, com a <strong>mensalidade base e a política de dependentes configuradas neste momento</strong>. A operação é idempotente: associados que já possuem mensalidade nesta competência são ignorados, então é seguro executá-la novamente. Os associados são notificados.</>,
              })
              if (ok) generate.mutate()
            }}>Gerar mensalidades</Button>
          )}
        </>} />

      {pendingSettings && (
        <Alert tone="warning" className="mb-6" title="Não foi possível gerar as mensalidades">
          {pendingSettings} <Link to="/configuracoes" className="font-medium underline">Abrir Configurações</Link>
        </Alert>
      )}

      <div className="mb-6 grid grid-cols-2 gap-4 xl:grid-cols-4">
        <StatCard label="Mensalidades" value={fmt.number(list.total)} hint={`Competência ${fmt.competence(competence)}`} icon={CalendarPlus} />
        <StatCard label="Emitido" value={fmt.money(sum((c) => c.total))} hint={scope} icon={CircleDollarSign} tone="info" />
        <StatCard label="Recebido" value={fmt.money(sum((c) => c.paidAmount))} hint={scope} icon={Wallet} tone="success" />
        <StatCard label="Em aberto" value={fmt.money(sum((c) => (c.status === 'Open' ? c.balance : 0)))} hint={`${fmt.number(active.filter((c) => c.isOverdue).length)} vencida(s)`} icon={Clock} tone="warning" />
      </div>

      <Card padded={false}>
        <Toolbar>
          <Input type="month" aria-label="Competência" title="Competência" className="w-44" value={competence} onChange={(e) => { if (e.target.value) { setCompetence(e.target.value); setPendingSettings(null) } }} />
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Associado…" />
          <EnumSelect dict={labels.chargeStatus} value={status} onChange={setStatus} all="Todas as situações" className="w-44" />
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(c) => c.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          onRowClick={(c) => navigate(`/financeiro/cobrancas/${c.id}`)}
          empty={{ title: 'Nenhuma mensalidade nesta competência', description: list.search || status ? 'Ajuste a pesquisa ou os filtros.' : can('finance.charges') ? 'Use "Gerar mensalidades" para criar as cobranças dos associados ativos.' : undefined }} />
      </Card>

      {result && (
        <Modal open onClose={() => setResult(null)} size="sm" title="Mensalidades geradas" footer={<Button variant="primary" onClick={() => setResult(null)}>Concluir</Button>}>
          <Alert tone={result.created > 0 ? 'success' : 'info'} title={result.created > 0 ? `${fmt.number(result.created)} mensalidade(s) criada(s)` : 'Nenhuma mensalidade nova'}>
            {result.created > 0 ? 'Os associados foram notificados.' : result.alreadyExisting > 0 ? 'Todos os associados ativos já possuíam mensalidade nesta competência.' : 'Não há associados ativos para gerar mensalidades.'}
          </Alert>
          <dl className="mt-4 grid grid-cols-2 gap-4">
            <Info label="Competência">{fmt.competence(result.competence)}</Info>
            <Info label="Valor total gerado"><span className="tabular font-medium">{fmt.money(result.totalAmount)}</span></Info>
            <Info label="Criadas agora">{fmt.number(result.created)}</Info>
            <Info label="Já existentes (ignoradas)">{fmt.number(result.alreadyExisting)}</Info>
          </dl>
        </Modal>
      )}
    </>
  )
}
