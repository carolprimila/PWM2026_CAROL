import { AlertTriangle, ArrowRight, CalendarClock, CalendarDays, DoorOpen, LayoutDashboard, Megaphone, PiggyBank, Settings2, TrendingDown, UserCheck, Users, Wallet } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { Alert, Badge, Card, CardTitle, EmptyState, PageHeader, QueryBoundary, StatCard, StatusBadge } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { useRealtime } from '@/lib/realtime'

function Pending({ to, count, label }: { to: string; count: number; label: string }) {
  if (count === 0) return null
  return (
    <Link to={to} className="flex items-center justify-between rounded-(--radius-control) px-3 py-2.5 text-sm transition-colors hover:bg-canvas">
      <span className="flex items-center gap-3"><span className="tabular flex h-6 min-w-6 items-center justify-center rounded-full bg-warning-soft px-1.5 text-xs font-semibold text-warning">{count}</span>{label}</span>
      <ArrowRight className="size-4 text-muted" />
    </Link>
  )
}

function ListCard({ title, icon, to, children }: { title: string; icon: typeof Users; to: string; children: ReactNode }) {
  return (
    <Card padded={false}>
      <div className="px-5 pt-5"><CardTitle icon={icon} title={title} actions={<Link to={to} className="text-[13px] font-medium text-brand-600 hover:underline">Ver tudo</Link>} /></div>
      {children}
    </Card>
  )
}

function DashboardPage() {
  const { user, can } = useAuth()
  const dashboard = useQuery({ queryKey: ['dashboard'], queryFn: () => api.get<Schema<'DashboardDto'>>('/api/relatorios/dashboard'), refetchInterval: 120_000 })
  useRealtime('occupancyChanged', () => void dashboard.refetch())

  return (
    <>
      <PageHeader title={`Olá, ${user?.name.split(' ')[0]}`} subtitle={fmt.longDate(new Date())} />
      <QueryBoundary query={dashboard}>
        {(d) => {
          const pendings = d.pendingMembers + d.pendingDependents + d.pendingReservations + d.openManifestations + d.openOccurrences + d.pendingGuests + d.lowStockItems
          return (
            <div className="space-y-6">
              {d.pendingSettings > 0 && can('settings.manage') && (
                <Alert tone="warning" title={`${d.pendingSettings} política(s) do clube ainda não definida(s)`}>
                  Regras como vencimento, tolerância e cobrança por dependente dependem de decisão da administração. <Link to="/configuracoes" className="font-medium underline">Abrir configurações</Link>
                </Alert>
              )}

              <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
                <StatCard label="Associados" value={fmt.number(d.members)} hint={`${fmt.number(d.activeMembers)} ativos`} icon={Users} />
                <StatCard label="Dependentes" value={fmt.number(d.dependents)} icon={UserCheck} tone="info" />
                <StatCard label="Pessoas no clube agora" value={fmt.number(d.presentNow)} icon={DoorOpen} tone="success" />
                <StatCard label="Reservas hoje" value={fmt.number(d.reservationsToday.length)} hint={d.pendingReservations > 0 ? `${d.pendingReservations} aguardando aprovação` : undefined} icon={CalendarClock} tone="warning" />
              </div>

              {d.finance && (
                <div className="grid grid-cols-2 gap-4 xl:grid-cols-4">
                  <StatCard label="Recebido no mês" value={fmt.money(d.finance.receivedThisMonth)} icon={PiggyBank} tone="success" />
                  <StatCard label="Despesas no mês" value={fmt.money(d.finance.expensesThisMonth)} icon={TrendingDown} tone="neutral" />
                  <StatCard label="A receber" value={fmt.money(d.finance.openAmount)} icon={Wallet} tone="info" />
                  <StatCard label="Em atraso" value={fmt.money(d.finance.overdueAmount)} hint={`${fmt.number(d.finance.delinquentMembers)} associado(s) inadimplente(s)`} icon={AlertTriangle} tone="danger" />
                </div>
              )}

              <div className="grid gap-6 xl:grid-cols-3">
                <div className="space-y-6 xl:col-span-2">
                  <ListCard title="Reservas de hoje" icon={CalendarClock} to="/reservas">
                    {d.reservationsToday.length === 0 ? <EmptyState title="Nenhuma reserva para hoje" /> : (
                      <ul className="divide-y divide-line/70 border-t border-line">
                        {d.reservationsToday.map((r) => (
                          <li key={r.id}>
                            <Link to={`/reservas/${r.id}`} className="flex items-center gap-4 px-5 py-3 text-sm transition-colors hover:bg-canvas/70">
                              <span className="tabular w-28 shrink-0 font-medium text-ink">{fmt.time(r.startAt)} – {fmt.time(r.endAt)}</span>
                              <span className="min-w-0 flex-1"><span className="block truncate text-ink">{r.spaceName}</span><span className="block truncate text-xs text-muted">{r.who ?? '—'} · {r.code}</span></span>
                              <StatusBadge dict={labels.reservationStatus} value={r.status} />
                            </Link>
                          </li>
                        ))}
                      </ul>
                    )}
                  </ListCard>

                  <ListCard title="Comunicados recentes" icon={Megaphone} to="/comunicados">
                    {d.recentAnnouncements.length === 0 ? <EmptyState title="Nenhum comunicado publicado" /> : (
                      <ul className="divide-y divide-line/70 border-t border-line">
                        {d.recentAnnouncements.map((a) => (
                          <li key={a.id} className="flex items-center gap-3 px-5 py-3 text-sm">
                            <StatusBadge dict={labels.announcementCategory} value={a.category} />
                            <span className="min-w-0 flex-1 truncate text-ink">{a.title}</span>
                            {a.priority !== 'Normal' && <Badge tone={labels.priority[a.priority].tone}>{labels.priority[a.priority].label}</Badge>}
                            <span className="text-xs text-muted">{fmt.date(a.publishedAt)}</span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </ListCard>
                </div>

                <div className="space-y-6">
                  <Card>
                    <CardTitle icon={Settings2} title="Pendências" description={pendings === 0 ? 'Tudo em dia.' : 'Itens que aguardam ação da equipe.'} />
                    <div className="-mx-2 space-y-0.5">
                      <Pending to="/associados?status=PendingApproval" count={d.pendingMembers} label="Associados aguardando aprovação" />
                      <Pending to="/dependentes?status=PendingApproval" count={d.pendingDependents} label="Dependentes aguardando aprovação" />
                      <Pending to="/reservas?status=Pending" count={d.pendingReservations} label="Reservas aguardando aprovação" />
                      <Pending to="/convidados" count={d.pendingGuests} label="Convidados aguardando autorização" />
                      <Pending to="/manifestacoes" count={d.openManifestations} label="Manifestações em aberto" />
                      <Pending to="/ocorrencias" count={d.openOccurrences} label="Ocorrências em aberto" />
                      <Pending to="/manutencoes" count={d.openMaintenances} label="Manutenções em aberto" />
                      <Pending to="/estoque" count={d.lowStockItems} label="Produtos no estoque mínimo" />
                    </div>
                  </Card>

                  <ListCard title="Próximos eventos" icon={CalendarDays} to="/eventos">
                    {d.upcomingEvents.length === 0 ? <EmptyState title="Nenhum evento agendado" /> : (
                      <ul className="divide-y divide-line/70 border-t border-line">
                        {d.upcomingEvents.map((e) => (
                          <li key={e.id} className="px-5 py-3 text-sm">
                            <p className="truncate font-medium text-ink">{e.title}</p>
                            <p className="mt-0.5 text-xs text-muted">{fmt.dateTime(e.startAt)}{e.location ? ` · ${e.location}` : ''} · {e.registered} inscrito(s)</p>
                          </li>
                        ))}
                      </ul>
                    )}
                  </ListCard>
                </div>
              </div>
            </div>
          )
        }}
      </QueryBoundary>
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/', label: 'Painel', icon: LayoutDashboard, group: 'Visão geral', order: 1, permissions: ['dashboard.view'] }],
  routes: [{ path: '', component: DashboardPage, permissions: ['dashboard.view'] }],
}
export default feature
