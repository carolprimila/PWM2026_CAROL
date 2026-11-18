import { Bell, Building2, CheckCheck, ChevronsUpDown, KeyRound, LogOut, MonitorSmartphone } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Avatar, Badge, Button, cn, EmptyState, Menu, Select, Spinner, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { useRealtime } from '@/lib/realtime'
import { allNav, navGroups } from './features'

function Sidebar() {
  const { user, can } = useAuth()
  const groups = useMemo(() => {
    const visible = allNav.filter((item) => (!item.kinds || (user && item.kinds.includes(user.kind))) && (!item.permissions || can(...item.permissions)))
    return navGroups
      .map((group) => ({ group, items: visible.filter((item) => item.group === group).sort((a, b) => (a.order ?? 50) - (b.order ?? 50)) }))
      .filter((entry) => entry.items.length > 0)
  }, [user, can])

  return (
    <aside className="flex w-64 shrink-0 flex-col border-r border-line bg-surface">
      <div className="flex h-16 items-center gap-3 px-5">
        <span className="flex size-9 items-center justify-center rounded-xl bg-brand-600 text-sm font-bold text-white">{(user?.clubName ?? 'Daros').slice(0, 1).toUpperCase()}</span>
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold text-ink">{user?.clubName ?? 'Daros'}</p>
          <p className="text-xs text-muted">Gestão do clube</p>
        </div>
      </div>
      <nav className="flex-1 overflow-y-auto px-3 pb-6">
        {groups.map(({ group, items }) => (
          <div key={group} className="mt-5 first:mt-2">
            <p className="mb-1.5 px-3 text-[11px] font-semibold tracking-wider text-muted/80 uppercase">{group}</p>
            {items.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.to === '/' || allNav.some((other) => other.to.startsWith(item.to + '/'))}
                className={({ isActive }) => cn('flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors', isActive ? 'bg-brand-50 font-medium text-brand-700' : 'text-ink/75 hover:bg-canvas hover:text-ink')}>
                <item.icon className="size-4 shrink-0" />
                <span className="truncate">{item.label}</span>
              </NavLink>
            ))}
          </div>
        ))}
      </nav>
    </aside>
  )
}

function Notifications() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const toast = useToast()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const unread = useQuery({ queryKey: ['notificacoes', 'nao-lidas'], queryFn: () => api.get<{ count: number }>('/api/notificacoes/nao-lidas'), refetchInterval: 60_000 })
  const list = useQuery({ queryKey: ['notificacoes', 'lista'], queryFn: () => api.get<Paged<Schema<'NotificationDto'>>>('/api/notificacoes', { pageSize: 15 }), enabled: open })
  const refresh = () => queryClient.invalidateQueries({ queryKey: ['notificacoes'] })
  const markAll = useMutation({ mutationFn: () => api.post('/api/notificacoes/marcar-todas-lidas'), onSuccess: refresh, onError: toast.error })
  const markOne = useMutation({ mutationFn: (id: string) => api.post(`/api/notificacoes/${id}/lida`), onSuccess: refresh })

  useRealtime('notification', (payload) => {
    void refresh()
    const title = (payload as { title?: string } | null)?.title
    if (title) toast.info(title)
  })
  useEffect(() => {
    if (!open) return
    const onDown = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', onDown)
    return () => document.removeEventListener('mousedown', onDown)
  }, [open])

  // Leva o usuário ao registro relacionado quando existe uma tela para ele.
  const targets: Record<string, (id: string) => string> = {
    Reservation: (id) => `/reservas/${id}`, Member: (id) => `/associados/${id}`, Dependent: () => '/dependentes', Manifestation: (id) => `/manifestacoes/${id}`,
    Charge: (id) => `/financeiro/cobrancas/${id}`, Occurrence: () => '/ocorrencias', MaintenanceOrder: () => '/manutencoes', Guest: () => '/convidados', StockItem: () => '/estoque',
  }
  const count = unread.data?.count ?? 0
  return (
    <div className="relative" ref={ref}>
      <button onClick={() => setOpen((o) => !o)} aria-label="Notificações" className="relative inline-flex size-9 items-center justify-center rounded-lg text-muted transition-colors hover:bg-black/5 hover:text-ink">
        <Bell className="size-[18px]" />
        {count > 0 && <span className="absolute top-1 right-1 flex min-w-4 items-center justify-center rounded-full bg-danger px-1 text-[10px] font-semibold text-white">{count > 99 ? '99+' : count}</span>}
      </button>
      {open && (
        <div className="animate-pop absolute right-0 z-40 mt-2 w-96 rounded-2xl border border-line bg-surface shadow-(--shadow-pop)">
          <header className="flex items-center justify-between border-b border-line px-4 py-3">
            <p className="text-sm font-semibold">Notificações</p>
            {count > 0 && <Button size="sm" variant="ghost" icon={CheckCheck} loading={markAll.isPending} onClick={() => markAll.mutate()}>Marcar todas como lidas</Button>}
          </header>
          <div className="max-h-[420px] overflow-y-auto">
            {list.isLoading ? <div className="flex justify-center py-10"><Spinner /></div>
              : (list.data?.items.length ?? 0) === 0 ? <EmptyState title="Nenhuma notificação" description="As novidades do clube aparecerão aqui." icon={Bell} />
              : list.data!.items.map((n) => (
                <button key={n.id} onClick={() => {
                  if (!n.readAt) markOne.mutate(n.id)
                  const target = n.entityType && n.entityId ? targets[n.entityType]?.(n.entityId) : undefined
                  if (target) { setOpen(false); navigate(target) }
                }} className={cn('flex w-full gap-3 border-b border-line/70 px-4 py-3 text-left transition-colors last:border-0 hover:bg-canvas/70', !n.readAt && 'bg-brand-50/40')}>
                  <span className={cn('mt-1.5 size-2 shrink-0 rounded-full', n.readAt ? 'bg-transparent' : 'bg-brand-500')} />
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center justify-between gap-2">
                      <span className="truncate text-sm font-medium text-ink">{n.title}</span>
                      <Badge tone={labels.notificationCategory[n.category].tone}>{labels.notificationCategory[n.category].label}</Badge>
                    </span>
                    <span className="mt-0.5 line-clamp-2 block text-[13px] text-muted">{n.body}</span>
                    <span className="mt-1 block text-xs text-muted/80">{fmt.dateTime(n.createdAt)}</span>
                  </span>
                </button>
              ))}
          </div>
        </div>
      )}
    </div>
  )
}

/** Superadministrador: escolhe o clube em que vai operar. O servidor valida a escolha a cada requisição. */
function ClubSwitcher() {
  const { clubId, selectClub } = useAuth()
  const clubs = useQuery({ queryKey: ['plataforma', 'clubes'], queryFn: () => api.get<Schema<'ClubDto'>[]>('/api/clubes') })
  return (
    <div className="flex items-center gap-2">
      <Building2 className="size-4 text-muted" />
      <div className="w-64">
        <Select value={clubId ?? ''} onChange={(e) => selectClub(e.target.value || null)}>
          <option value="">Daros (nenhuma sede selecionada)</option>
          {clubs.data?.map((club) => <option key={club.id} value={club.id}>{club.name}{club.isActive ? '' : ' (inativo)'}</option>)}
        </Select>
      </div>
    </div>
  )
}

export function Layout() {
  const { user, logout, isSuperAdmin, clubId } = useAuth()
  const navigate = useNavigate()
  if (!user) return null
  return (
    <div className="flex h-full">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-16 shrink-0 items-center justify-between gap-4 border-b border-line bg-surface/80 px-8 backdrop-blur">
          <div>{isSuperAdmin && <ClubSwitcher />}</div>
          <div className="flex items-center gap-2">
            {clubId && <Notifications />}
            <Menu
              trigger={
                <button className="flex items-center gap-2.5 rounded-xl py-1.5 pr-2 pl-1.5 transition-colors hover:bg-black/5">
                  <Avatar name={user.name} size={32} />
                  <span className="text-left leading-tight">
                    <span className="block max-w-40 truncate text-sm font-medium text-ink">{user.name}</span>
                    <span className="block text-xs text-muted">{labels.userKind[user.kind].label}</span>
                  </span>
                  <ChevronsUpDown className="size-4 text-muted" />
                </button>
              }
              items={[
                { label: 'Alterar senha', icon: KeyRound, onClick: () => navigate('/conta/senha') },
                { label: 'Sessões ativas', icon: MonitorSmartphone, onClick: () => navigate('/conta/sessoes') },
                { label: 'Sair', icon: LogOut, danger: true, onClick: () => void logout() },
              ]}
            />
          </div>
        </header>
        <main className="flex-1 overflow-y-auto">
          <div className="mx-auto w-full max-w-[1400px] px-8 py-8">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}

export function Forbidden() {
  return <EmptyState title="Acesso não autorizado" description="Você não tem permissão para acessar esta área. Se precisar, solicite à administração do clube." icon={KeyRound} />
}

export function NotFound() {
  const navigate = useNavigate()
  return <EmptyState title="Página não encontrada" description="O endereço acessado não existe." action={<Button onClick={() => navigate('/')}>Voltar ao início</Button>} />
}
