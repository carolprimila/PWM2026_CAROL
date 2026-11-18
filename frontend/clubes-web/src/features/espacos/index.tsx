import { MapPin, MoreHorizontal, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { DataTable, type Column } from '@/components/table'
import { Badge, Button, Card, EmptyState, FileImage, IconButton, Menu, PageHeader, QueryBoundary, Tabs, useConfirm, useToast } from '@/components/ui'
import { api } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { BlocksTab } from './BlocksTab'
import { scheduleSummary, spaceGroups, type Space } from './helpers'
import { HolidaysTab } from './HolidaysTab'
import { SpaceForm } from './SpaceForm'

type Tab = 'espacos' | 'bloqueios' | 'feriados'

function SpacesTab() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [form, setForm] = useState<{ space?: Space } | null>(null)
  const spaces = useQuery({ queryKey: ['/api/espacos', { includeInactive: true }], queryFn: () => api.get<Space[]>('/api/espacos', { includeInactive: true }) })
  const remove = useMutation({
    mutationFn: (id: string) => api.delete(`/api/espacos/${id}`),
    onSuccess: () => { toast.success('Espaço excluído.'); void queryClient.invalidateQueries({ queryKey: ['/api/espacos'] }) },
    onError: toast.error,
  })
  const manage = can('spaces.manage')

  const price = (label: string, value: number | null | undefined) => (
    <p className="whitespace-nowrap"><span className="text-muted">{label}: </span>{value == null ? <span className="text-warning">não definido</span> : <span className="tabular">{fmt.money(value)}</span>}</p>
  )
  const columns: Column<Space>[] = [
    {
      key: 'name', header: 'Espaço',
      render: (s) => (
        <div className="flex items-center gap-3">
          {s.imageFileId ? <FileImage fileId={s.imageFileId} alt={s.name} className="size-10 shrink-0 rounded-lg object-cover" />
            : <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-canvas text-muted"><MapPin className="size-4" /></span>}
          <div className="min-w-0"><p className="truncate font-medium text-ink">{s.name}</p><p className="truncate text-xs text-muted">{s.subtype || labels.spaceType[s.type].label}</p></div>
        </div>
      ),
    },
    { key: 'capacity', header: 'Capacidade', align: 'center', width: '110px', render: (s) => (s.capacity == null ? <span className="text-muted">—</span> : `${fmt.number(s.capacity)} pessoas`) },
    {
      key: 'booking', header: 'Reserva', width: '190px',
      render: (s) => !s.isReservable ? <Badge>Não reservável</Badge> : (
        <div><Badge tone="brand">{labels.bookingMode[s.bookingMode].label}</Badge>
          <p className="mt-1 text-xs text-muted">{s.bookingMode === 'Hourly' ? `Intervalos de ${s.slotMinutes} min` : `Período fixo de ${s.fixedDays} dia(s)`}</p></div>
      ),
    },
    {
      key: 'prices', header: 'Preços', width: '210px',
      render: (s) => !s.isReservable ? <span className="text-muted">—</span> : (
        <div className="text-[13px]">
          {s.bookingMode === 'Hourly' ? price('Hora', s.hourlyPrice) : price('Período', s.periodPrice)}
          {price('Limpeza', s.cleaningFee)}
        </div>
      ),
    },
    { key: 'schedule', header: 'Funcionamento', render: (s) => <span className="text-[13px] text-ink/80">{scheduleSummary(s.schedules)}</span> },
    { key: 'active', header: 'Situação', width: '100px', render: (s) => (s.isActive ? <Badge tone="success">Ativo</Badge> : <Badge>Inativo</Badge>) },
  ]

  return (
    <>
      {manage && <div className="mb-4 flex justify-end"><Button variant="primary" icon={Plus} onClick={() => setForm({})}>Novo espaço</Button></div>}
      <QueryBoundary query={spaces}>
        {(list) => list.length === 0 ? <Card><EmptyState icon={MapPin} title="Nenhum espaço cadastrado" description="Cadastre quadras, quiosques, salões e demais áreas do clube." /></Card> : (
          <div className="space-y-6">
            {spaceGroups.map((group) => {
              const rows = list.filter((s) => s.type === group.type)
              if (rows.length === 0) return null
              return (
                <Card key={group.type} padded={false}>
                  <header className="flex items-center gap-2 border-b border-line px-5 py-3.5">
                    <h2 className="text-[15px] font-semibold text-ink">{group.title}</h2><span className="rounded-full bg-black/5 px-2 text-xs text-muted">{rows.length}</span>
                  </header>
                  <DataTable columns={columns} rows={rows} rowKey={(s) => s.id} onRowClick={manage ? (s) => setForm({ space: s }) : undefined}
                    rowActions={manage ? (s) => (
                      <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
                        { label: 'Editar', icon: Pencil, onClick: () => setForm({ space: s }) },
                        {
                          label: 'Excluir', icon: Trash2, danger: true, onClick: async () => {
                            if (await confirm({ title: `Excluir ${s.name}?`, message: 'O espaço será removido. Se houver reservas futuras, a exclusão será recusada — nesse caso, desative o espaço.', danger: true, confirmLabel: 'Excluir' })) remove.mutate(s.id)
                          },
                        },
                      ]} />
                    ) : undefined} />
                </Card>
              )
            })}
          </div>
        )}
      </QueryBoundary>
      {form && <SpaceForm space={form.space} onClose={() => setForm(null)} />}
    </>
  )
}

function SpacesPage() {
  const [params, setParams] = useSearchParams()
  const current = params.get('aba')
  const tab: Tab = current === 'bloqueios' || current === 'feriados' ? current : 'espacos'
  return (
    <>
      <PageHeader title="Espaços" subtitle="Áreas do clube, regras de reserva, bloqueios e datas especiais." />
      <Tabs<Tab> value={tab} onChange={(key) => setParams(key === 'espacos' ? {} : { aba: key }, { replace: true })} tabs={[
        { key: 'espacos', label: 'Espaços' }, { key: 'bloqueios', label: 'Bloqueios' }, { key: 'feriados', label: 'Feriados e datas especiais' },
      ]} />
      {tab === 'espacos' && <SpacesTab />}
      {tab === 'bloqueios' && <BlocksTab />}
      {tab === 'feriados' && <HolidaysTab />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/espacos', label: 'Espaços', icon: MapPin, group: 'Espaços e reservas', order: 3, permissions: ['spaces.view'] }],
  routes: [{ path: 'espacos', component: SpacesPage, permissions: ['spaces.view'] }],
}
export default feature
