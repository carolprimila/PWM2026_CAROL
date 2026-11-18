import { ScrollText } from 'lucide-react'
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Alert, Badge, Card, Info, Input, Modal, PageHeader, SearchInput, Select, StatusBadge, Tabs } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { dates, fmt } from '@/lib/format'
import { labels, type Tone } from '@/lib/labels'

type AuditLog = Schema<'AuditLogDto'>
type Delivery = Schema<'NotificationDeliveryDto'>
type Tab = 'registros' | 'entregas'

/** Nomes das entidades registradas pela auditoria (nome técnico → rótulo). Entidades fora da lista aparecem com o nome técnico. */
const entityLabels: Record<string, string> = {
  Club: 'Clube', ClubSettings: 'Configurações do clube', ClubOpeningHour: 'Horário de funcionamento', DependentPricingRule: 'Regra de cobrança por dependente',
  User: 'Usuário', Role: 'Função', RolePermission: 'Permissão da função', UserRole: 'Função do usuário', UserPermission: 'Permissão individual',
  Member: 'Associado', Dependent: 'Dependente', DependentPermission: 'Permissão do dependente', Employee: 'Funcionário', Guest: 'Convidado',
  Charge: 'Cobrança', ChargeAdjustment: 'Ajuste de cobrança', Payment: 'Pagamento', FinancialEntry: 'Lançamento financeiro', MonthlyClosing: 'Fechamento mensal', DelinquencyRelease: 'Liberação de inadimplente',
  Space: 'Espaço', SpaceSchedule: 'Horário do espaço', SpaceBlock: 'Bloqueio de espaço', Holiday: 'Feriado', Reservation: 'Reserva',
  Event: 'Evento', EventRegistration: 'Inscrição em evento', Announcement: 'Comunicado', Manifestation: 'Manifestação', ManifestationType: 'Tipo de manifestação',
  Assembly: 'Assembleia', Poll: 'Votação', Asset: 'Patrimônio', Product: 'Produto', Supplier: 'Fornecedor', StockMovement: 'Movimentação de estoque',
  MaintenanceOrder: 'Manutenção', Inspection: 'Vistoria', Occurrence: 'Ocorrência', KeyHandover: 'Entrega de chaves', Attachment: 'Anexo', StoredFile: 'Arquivo',
}
const entityLabel = (type: string) => entityLabels[type] ?? type
const operationTone: Record<string, Tone> = { 'Criação': 'success', 'Alteração': 'info', 'Exclusão': 'danger' }

function AuditPage() {
  const [tab, setTab] = useState<Tab>('registros')
  return (
    <>
      <PageHeader title="Auditoria" subtitle="Registro imutável de quem alterou o quê, e o acompanhamento da entrega das notificações." />
      <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'registros', label: 'Registros' }, { key: 'entregas', label: 'Entrega de notificações' }]} />
      {tab === 'registros' ? <LogsTab /> : <DeliveriesTab />}
    </>
  )
}

function LogsTab() {
  const [entityType, setEntityType] = useState('')
  const [operation, setOperation] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [open, setOpen] = useState<AuditLog | null>(null)
  const entities = useQuery({ queryKey: ['/api/auditoria/entidades'], queryFn: () => api.get<string[]>('/api/auditoria/entidades'), staleTime: 60_000 })
  const list = usePaged<AuditLog>('/api/auditoria', {
    entityType: entityType || undefined, operation: operation || undefined,
    from: from ? dates.startOfDay(from) : undefined, to: to ? dates.endOfDay(to) : undefined,
  })
  const filtered = !!(list.search || entityType || operation || from || to)

  const columns: Column<AuditLog>[] = [
    { key: 'at', header: 'Data', width: '150px', render: (a) => <span className="tabular">{fmt.dateTime(a.at)}</span> },
    { key: 'user', header: 'Usuário', render: (a) => a.userName ?? <span className="text-muted">Sistema</span> },
    { key: 'operation', header: 'Operação', render: (a) => <Badge tone={operationTone[a.operation] ?? 'neutral'}>{a.operation}</Badge> },
    { key: 'entity', header: 'Registro', render: (a) => <span className="font-medium text-ink">{entityLabel(a.entityType)}</span> },
    { key: 'fields', header: 'Campos', render: (a) => { const keys = changedKeys(a); return <span className="text-muted">{keys.length === 0 ? '—' : keys.length <= 3 ? keys.join(', ') : `${keys.slice(0, 3).join(', ')} +${keys.length - 3}`}</span> } },
    { key: 'justification', header: 'Justificativa', render: (a) => (a.justification ? <span className="line-clamp-1 max-w-64" title={a.justification}>{a.justification}</span> : <span className="text-muted">—</span>) },
  ]

  return (
    <>
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Usuário ou justificativa…" />
          <div className="w-56"><Select value={entityType} onChange={(e) => setEntityType(e.target.value)}><option value="">Todos os registros</option>{(entities.data ?? []).map((type) => <option key={type} value={type}>{entityLabel(type)}</option>)}</Select></div>
          <div className="w-44"><Select value={operation} onChange={(e) => setOperation(e.target.value)}><option value="">Todas as operações</option>{Object.keys(operationTone).map((o) => <option key={o}>{o}</option>)}</Select></div>
          <div className="flex items-center gap-2 text-sm text-muted">
            <Input type="date" className="w-40" aria-label="Período — de" value={from} max={to || undefined} onChange={(e) => setFrom(e.target.value)} />
            até
            <Input type="date" className="w-40" aria-label="Período — até" value={to} min={from || undefined} onChange={(e) => setTo(e.target.value)} />
          </div>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(a) => a.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={setOpen}
          empty={{ title: 'Nenhum registro de auditoria', description: filtered ? 'Ajuste a pesquisa, os filtros ou o período.' : undefined }} />
      </Card>
      {open && <LogModal log={open} onClose={() => setOpen(null)} />}
    </>
  )
}

function parse(json: string | null | undefined): Record<string, unknown> {
  if (!json) return {}
  try { const value: unknown = JSON.parse(json); return value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : {} } catch { return {} }
}
const changedKeys = (log: AuditLog) => [...new Set([...Object.keys(parse(log.beforeJson)), ...Object.keys(parse(log.afterJson))])]

function Value({ value }: { value: unknown }) {
  if (value === undefined || value === null || value === '') return <span className="text-muted">—</span>
  if (typeof value === 'boolean') return <>{value ? 'Sim' : 'Não'}</>
  if (typeof value === 'number') return <span className="tabular">{fmt.number(value)}</span>
  if (typeof value === 'string') return <span className="break-words whitespace-pre-wrap">{/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(value) ? fmt.dateTime(value) : /^\d{4}-\d{2}-\d{2}$/.test(value) ? fmt.day(value) : value}</span>
  return <code className="font-mono text-xs break-all">{JSON.stringify(value)}</code>
}

function LogModal({ log, onClose }: { log: AuditLog; onClose: () => void }) {
  const before = parse(log.beforeJson)
  const after = parse(log.afterJson)
  const keys = changedKeys(log)
  return (
    <Modal open onClose={onClose} size="lg" title={`${log.operation} — ${entityLabel(log.entityType)}`} description={fmt.dateTime(log.at)}>
      <dl className="grid grid-cols-3 gap-x-6 gap-y-4">
        <Info label="Quem">{log.userName ?? 'Sistema (rotina automática)'}</Info>
        <Info label="Quando">{fmt.dateTime(log.at)}</Info>
        <Info label="Operação"><Badge tone={operationTone[log.operation] ?? 'neutral'}>{log.operation}</Badge></Info>
        <Info label="Registro" className="col-span-3">{entityLabel(log.entityType)}{log.entityId && <span className="ml-2 font-mono text-xs text-muted">{log.entityId}</span>}</Info>
        <Info label="Origem" className="col-span-3">{log.origin}</Info>
      </dl>
      {log.justification && <Alert className="mt-4" tone="info" title="Justificativa">{log.justification}</Alert>}

      <h3 className="mt-6 mb-2 text-xs font-semibold tracking-wider text-muted uppercase">Campos alterados</h3>
      {keys.length === 0 ? <p className="text-sm text-muted">Este registro não possui detalhes de campos.</p> : (
        <div className="overflow-x-auto rounded-(--radius-control) border border-line">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-line text-left text-xs font-medium tracking-wide text-muted uppercase">
                <th className="w-1/4 px-4 py-2.5 font-medium">Campo</th><th className="px-4 py-2.5 font-medium">Antes</th><th className="px-4 py-2.5 font-medium">Depois</th>
              </tr>
            </thead>
            <tbody>
              {keys.map((key) => (
                <tr key={key} className="border-b border-line/70 align-top last:border-0">
                  <td className="px-4 py-2.5 font-mono text-xs text-ink">{key}</td>
                  <td className="px-4 py-2.5 text-ink/75"><Value value={before[key]} /></td>
                  <td className="px-4 py-2.5 text-ink"><Value value={after[key]} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <p className="mt-3 text-xs text-muted">Dados sensíveis aparecem mascarados (***). Os registros de auditoria não podem ser alterados nem excluídos.</p>
    </Modal>
  )
}

function DeliveriesTab() {
  const list = usePaged<Delivery>('/api/notificacoes/entregas')
  const columns: Column<Delivery>[] = [
    { key: 'user', header: 'Usuário', render: (n) => <span className="font-medium text-ink">{n.userName}</span> },
    { key: 'title', header: 'Título', render: (n) => <span className="line-clamp-1 max-w-80" title={n.title}>{n.title}</span> },
    { key: 'category', header: 'Categoria', render: (n) => <StatusBadge dict={labels.notificationCategory} value={n.category} /> },
    { key: 'created', header: 'Criada', render: (n) => <span className="tabular">{fmt.dateTime(n.createdAt)}</span> },
    { key: 'read', header: 'Lida', render: (n) => (n.readAt ? <span className="tabular">{fmt.dateTime(n.readAt)}</span> : <Badge>Não lida</Badge>) },
    { key: 'push', header: 'Push', render: (n) => <span title={n.pushSentAt ? `Enviado em ${fmt.dateTime(n.pushSentAt)}` : undefined}><StatusBadge dict={labels.deliveryStatus} value={n.pushStatus} /></span> },
    { key: 'email', header: 'E-mail', render: (n) => <StatusBadge dict={labels.deliveryStatus} value={n.emailStatus} /> },
    { key: 'error', header: 'Erro', render: (n) => (n.deliveryError ? <span className="line-clamp-1 max-w-56 text-danger" title={n.deliveryError}>{n.deliveryError}</span> : <span className="text-muted">—</span>) },
  ]
  return (
    <Card padded={false}>
      <DataTable columns={columns} rows={list.rows} rowKey={(n) => n.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
        page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
        empty={{ title: 'Nenhuma notificação enviada', description: 'As notificações geradas pelo clube aparecem aqui com a situação de cada canal.' }} />
    </Card>
  )
}

const feature: Feature = {
  nav: [{ to: '/auditoria', label: 'Auditoria', icon: ScrollText, group: 'Administração', order: 5, permissions: ['audit.view'] }],
  routes: [{ path: 'auditoria', component: AuditPage, permissions: ['audit.view'] }],
}
export default feature
