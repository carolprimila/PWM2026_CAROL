import { CalendarDays, Plus } from 'lucide-react'
import { useState } from 'react'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, Checkbox, FileImage, PageHeader, SearchInput, Select, StatusBadge } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { EventDetail } from './EventDetail'
import { EventForm } from './EventForm'
import { audienceLabel, type ClubEvent } from './audience'

function EventsPage() {
  const { can } = useAuth()
  const [status, setStatus] = useState<Schema<'EventStatus'> | ''>('')
  const [upcomingOnly, setUpcomingOnly] = useState(true)
  const [creating, setCreating] = useState(false)
  const [openId, setOpenId] = useState<string | null>(null)
  const list = usePaged<ClubEvent>('/api/eventos', { status: status || undefined, upcomingOnly: upcomingOnly || undefined })

  const columns: Column<ClubEvent>[] = [
    {
      key: 'title', header: 'Evento',
      render: (e) => (
        <div className="flex items-center gap-3">
          {e.imageFileId ? <FileImage fileId={e.imageFileId} alt="" className="h-11 w-16 shrink-0 rounded-lg object-cover" />
            : <span className="flex h-11 w-16 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600/50"><CalendarDays className="size-5" /></span>}
          <div className="min-w-0"><p className="truncate font-medium text-ink">{e.title}</p><p className="truncate text-xs text-muted">{e.location || 'Local não informado'}</p></div>
        </div>
      ),
    },
    { key: 'when', header: 'Data', render: (e) => <div><p className="tabular text-ink">{fmt.dateTime(e.startAt)}</p><p className="tabular text-xs text-muted">até {fmt.dateTime(e.endAt)}</p></div> },
    { key: 'audience', header: 'Público', render: (e) => <span className="text-muted">{audienceLabel(e)}</span> },
    {
      key: 'registered', header: 'Inscritos', align: 'center',
      render: (e) => (e.registrationRequired ? <span className="tabular">{e.registered}{e.capacity != null ? ` / ${e.capacity}` : ''}</span> : <span className="text-xs text-muted">Sem inscrição</span>),
    },
    { key: 'status', header: 'Situação', render: (e) => <div className="flex items-center gap-1.5"><StatusBadge dict={labels.eventStatus} value={e.status} />{e.capacity != null && e.registered >= e.capacity && e.status === 'Scheduled' && <Badge tone="warning">Lotado</Badge>}</div> },
  ]

  return (
    <>
      <PageHeader title="Eventos" subtitle="Agenda de eventos do clube, inscrições e controle de presença."
        actions={can('events.manage') && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo evento</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Pesquisar pelo título…" />
          <div className="w-52"><Select value={status} onChange={(e) => setStatus(e.target.value as Schema<'EventStatus'> | '')}><option value="">Todas as situações</option>{options(labels.eventStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <Checkbox label="Somente futuros" checked={upcomingOnly} onChange={(e) => setUpcomingOnly(e.target.checked)} className="ml-1" />
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(e) => e.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage} onRowClick={(e) => setOpenId(e.id)}
          empty={{ title: 'Nenhum evento encontrado', description: upcomingOnly ? 'Não há eventos futuros agendados. Desmarque "Somente futuros" para ver os anteriores.' : 'Ajuste a pesquisa ou os filtros.' }} />
      </Card>
      {creating && <EventForm onClose={() => setCreating(false)} onSaved={(saved) => { setCreating(false); setOpenId(saved.id) }} />}
      {openId && <EventDetail id={openId} onClose={() => setOpenId(null)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/eventos', label: 'Eventos', icon: CalendarDays, group: 'Comunicação', order: 2, permissions: ['events.view'] }],
  routes: [{ path: 'eventos', component: EventsPage, permissions: ['events.view'] }],
}
export default feature
