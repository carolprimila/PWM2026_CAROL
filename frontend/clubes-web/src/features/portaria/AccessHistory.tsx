import { ArrowLeft, ShieldX } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Button, Card, Input, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Log = Schema<'AccessLogDto'>

/** A origem gravada pelo servidor: dispositivo do leitor, "manual: justificativa" ou "convidado". */
function origin(value: string | null | undefined) {
  if (!value) return '—'
  if (value.startsWith('manual:')) return `Manual — ${value.slice(7).trim()}`
  if (value === 'convidado') return 'Lista de convidados'
  if (value === 'qr') return 'QR Code'
  return `QR Code (${value})`
}

export default function AccessHistory() {
  const { can } = useAuth()
  const [result, setResult] = useState('')
  const [operation, setOperation] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const list = usePaged<Log>('/api/acessos', {
    result: result || undefined, operation: operation || undefined, from: from ? dates.startOfDay(from) : undefined, to: to ? dates.endOfDay(to) : undefined,
  }, { pageSize: 25 })
  const onlyDenied = result === 'Denied'

  const columns: Column<Log>[] = [
    { key: 'at', header: 'Data e hora', width: '150px', render: (l) => <span className="tabular whitespace-nowrap">{fmt.dateTime(l.occurredAt)}</span> },
    { key: 'person', header: 'Pessoa', render: (l) => <span className="font-medium text-ink">{l.personName ?? 'Não identificada'}</span> },
    { key: 'type', header: 'Tipo', render: (l) => <StatusBadge dict={labels.personType} value={l.personType} /> },
    { key: 'responsible', header: 'Responsável', render: (l) => l.responsibleName ?? <span className="text-muted">—</span> },
    { key: 'operation', header: 'Operação', render: (l) => <StatusBadge dict={labels.accessOperation} value={l.operation} /> },
    { key: 'result', header: 'Resultado', render: (l) => <StatusBadge dict={labels.accessResult} value={l.result} /> },
    { key: 'denial', header: 'Motivo da negação', render: (l) => (l.denialReason ? <span className="text-danger">{l.denialReason}</span> : <span className="text-muted">—</span>) },
    { key: 'staff', header: 'Funcionário', render: (l) => l.staffName ?? <span className="text-muted">—</span> },
    { key: 'origin', header: 'Origem', render: (l) => <span className="text-[13px] text-ink/80">{origin(l.origin)}</span> },
  ]

  return (
    <>
      <PageHeader title="Histórico de acessos" subtitle="Entradas, saídas e tentativas negadas registradas na portaria."
        back={can('gate.operate') ? <Link to="/portaria" className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-muted hover:text-ink"><ArrowLeft className="size-3.5" />Portaria</Link> : undefined} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Nome da pessoa…" />
          <Button variant={onlyDenied ? 'danger' : 'secondary'} icon={ShieldX} aria-pressed={onlyDenied} onClick={() => setResult(onlyDenied ? '' : 'Denied')}>Somente negados</Button>
          <div className="w-44"><Select value={result} onChange={(e) => setResult(e.target.value)}><option value="">Todos os resultados</option>{options(labels.accessResult).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <div className="w-44"><Select value={operation} onChange={(e) => setOperation(e.target.value)}><option value="">Entradas e saídas</option>{options(labels.accessOperation).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <label className="flex items-center gap-2 text-[13px] text-muted">De<Input type="date" className="w-40" value={from} onChange={(e) => setFrom(e.target.value)} /></label>
          <label className="flex items-center gap-2 text-[13px] text-muted">até<Input type="date" className="w-40" value={to} onChange={(e) => setTo(e.target.value)} /></label>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(l) => l.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          empty={{ title: 'Nenhum acesso encontrado', description: list.search || result || operation || from || to ? 'Ajuste a pesquisa, o período ou os filtros.' : 'Os registros da portaria aparecem aqui.' }} />
      </Card>
    </>
  )
}
