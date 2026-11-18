import { CalendarCheck, CalendarClock, Check, Eye, LayoutGrid, MoreHorizontal, Plus, X } from 'lucide-react'
import { lazy, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, IconButton, Input, Menu, PageHeader, SearchInput, Select, StatusBadge, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { period, useSpaces } from './availability'

type Reservation = Schema<'ReservationDto'>

function ReservationsPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const [spaceId, setSpaceId] = useState('')
  const [attendance, setAttendance] = useState('')
  // Com situação vinda por link (ex.: pendentes do painel), não restringe por data.
  const [from, setFrom] = useState(status ? '' : dates.today())
  const [to, setTo] = useState('')
  const manage = can('reservations.manage')
  const spaces = useSpaces()

  const list = usePaged<Reservation>('/api/reservas', {
    status: status || undefined, spaceId: spaceId || undefined, attendance: attendance || undefined,
    from: from ? dates.startOfDay(from) : undefined, to: to ? dates.endOfDay(to) : undefined,
  })
  const decide = useMutation({
    mutationFn: ({ id, path, reason }: { id: string; path: 'aprovar' | 'recusar'; reason: string | null; message: string }) => api.post(`/api/reservas/${id}/${path}`, { reason }),
    onSuccess: (_, variables) => {
      toast.success(variables.message)
      void queryClient.invalidateQueries({ queryKey: ['/api/reservas'] })
      void queryClient.invalidateQueries({ queryKey: ['reserva', variables.id] })
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
    },
    onError: toast.error,
  })
  const approve = async (r: Reservation) => {
    if (await confirm({ title: `Aprovar a reserva ${r.code}?`, message: `${r.spaceName} · ${period(r.startAt, r.endAt)}${r.totalAmount > 0 ? ` · ${fmt.money(r.totalAmount)}` : ''}`, confirmLabel: 'Aprovar' }))
      decide.mutate({ id: r.id, path: 'aprovar', reason: null, message: 'Reserva aprovada.' })
  }
  const reject = async (r: Reservation) => {
    const result = await confirm({ title: `Recusar a reserva ${r.code}?`, message: 'O solicitante será notificado com o motivo informado.', reason: 'required', reasonLabel: 'Motivo da recusa', danger: true, confirmLabel: 'Recusar' })
    if (result) decide.mutate({ id: r.id, path: 'recusar', reason: result.reason, message: 'Reserva recusada.' })
  }

  const columns: Column<Reservation>[] = [
    { key: 'code', header: 'Código', width: '110px', render: (r) => <span className="tabular font-medium text-ink">{r.code}</span> },
    { key: 'space', header: 'Espaço', render: (r) => <div><p className="font-medium text-ink">{r.spaceName}</p><p className="text-xs text-muted">{labels.spaceType[r.spaceType].label}</p></div> },
    {
      key: 'who', header: 'Associado / título',
      render: (r) => r.kind === 'Member'
        ? <div><p className="text-ink">{r.memberName ?? '—'}</p>{r.requestedByDependent && <p className="text-xs text-muted">Solicitada por dependente: {r.requestedBy}</p>}</div>
        : <div><p className="text-ink">{r.title ?? '—'}</p><Badge tone={labels.reservationKind[r.kind].tone}>{labels.reservationKind[r.kind].label}</Badge></div>,
    },
    { key: 'period', header: 'Período', sort: 'start', render: (r) => <span className="tabular whitespace-nowrap">{period(r.startAt, r.endAt)}</span> },
    { key: 'people', header: 'Pessoas', align: 'center', render: (r) => <span title={r.guests > 0 ? `${r.guests} convidado(s)` : undefined}>{r.peopleCount}</span> },
    { key: 'total', header: 'Valor', align: 'right', render: (r) => (r.totalAmount > 0 ? fmt.money(r.totalAmount) : <span className="text-muted">—</span>) },
    {
      key: 'status', header: 'Situação',
      render: (r) => <div className="flex items-center gap-1.5"><StatusBadge dict={labels.reservationStatus} value={r.status} />{r.attendance !== 'Unknown' && <StatusBadge dict={labels.attendance} value={r.attendance} />}</div>,
    },
  ]

  return (
    <>
      <PageHeader title="Reservas" subtitle="Solicitações, aprovações e histórico das reservas de espaços."
        actions={<>
          <Link to="/reservas/disponibilidade"><Button icon={LayoutGrid}>Disponibilidade</Button></Link>
          {manage && <Link to="/reservas/nova"><Button variant="primary" icon={Plus}>Nova reserva</Button></Link>}
        </>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Código, associado, espaço ou título…" />
          <div className="w-44">
            <Select value={status} onChange={(e) => setParams((current) => { if (e.target.value) current.set('status', e.target.value); else current.delete('status'); return current }, { replace: true })}>
              <option value="">Todas as situações</option>{options(labels.reservationStatus).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </Select>
          </div>
          <div className="w-48"><Select value={spaceId} onChange={(e) => setSpaceId(e.target.value)}><option value="">Todos os espaços</option>{spaces.data?.filter((s) => s.isReservable).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select></div>
          <div className="w-44"><Select value={attendance} onChange={(e) => setAttendance(e.target.value)}><option value="">Comparecimento</option>{options(labels.attendance).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
          <label className="flex items-center gap-2 text-[13px] text-muted">De<Input type="date" className="w-40" value={from} onChange={(e) => setFrom(e.target.value)} /></label>
          <label className="flex items-center gap-2 text-[13px] text-muted">até<Input type="date" className="w-40" value={to} onChange={(e) => setTo(e.target.value)} /></label>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(r) => r.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          sort={list.sort} onSort={list.toggleSort} page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          onRowClick={(r) => navigate(`/reservas/${r.id}`)}
          rowActions={(r) => (
            <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
              { label: 'Abrir', icon: Eye, onClick: () => navigate(`/reservas/${r.id}`) },
              manage && r.status === 'Pending' && { label: 'Aprovar', icon: Check, onClick: () => void approve(r) },
              manage && r.status === 'Pending' && { label: 'Recusar', icon: X, danger: true, onClick: () => void reject(r) },
            ]} />
          )}
          empty={{ title: 'Nenhuma reserva encontrada', description: list.search || status || spaceId || attendance || from || to ? 'Ajuste a pesquisa, o período ou os filtros.' : 'As reservas solicitadas pelos associados e pela equipe aparecem aqui.' }} />
      </Card>
    </>
  )
}

const feature: Feature = {
  nav: [
    { to: '/reservas', label: 'Reservas', icon: CalendarCheck, group: 'Espaços e reservas', order: 1, permissions: ['reservations.view'] },
    { to: '/reservas/disponibilidade', label: 'Disponibilidade', icon: CalendarClock, group: 'Espaços e reservas', order: 2, permissions: ['reservations.view'] },
  ],
  routes: [
    { path: 'reservas', component: ReservationsPage, permissions: ['reservations.view'] },
    { path: 'reservas/nova', component: lazy(() => import('./NewReservation')), permissions: ['reservations.manage'] },
    { path: 'reservas/disponibilidade', component: lazy(() => import('./Board')), permissions: ['reservations.view'] },
    { path: 'reservas/:id', component: lazy(() => import('./ReservationDetail')), permissions: ['reservations.view'] },
  ],
}
export default feature
