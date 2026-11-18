import { zodResolver } from '@hookform/resolvers/zod'
import { Ban, Check, MoreHorizontal, Plus, UserPlus, X } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import type { Feature } from '@/app/features'
import { MemberPicker } from '@/components/shared'
import { DataTable, Toolbar, usePaged, type Column } from '@/components/table'
import { Badge, Button, Card, Field, IconButton, Input, MaskedInput, Menu, Modal, PageHeader, SearchInput, Select, StatusBadge, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, emptyToNull, fmt } from '@/lib/format'
import { labels, options, type Tone } from '@/lib/labels'

type Guest = Schema<'GuestDto'>

const paymentTone: Record<string, Tone> = { Pago: 'success', Pendente: 'warning', Isento: 'neutral', Cancelado: 'neutral' }

const schema = z.object({
  fullName: z.string().trim().min(3, 'Informe o nome completo').max(150, 'Máximo de 150 caracteres'),
  document: z.string().max(30, 'Máximo de 30 caracteres'),
  phone: z.string(),
  visitDate: z.string().min(1, 'Informe a data da visita'),
  spaceId: z.string(),
})
type Values = z.infer<typeof schema>

function GuestForm({ defaultDate, onClose }: { defaultDate: string; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const [member, setMember] = useState<{ id: string; name: string } | null>(null)
  const [memberError, setMemberError] = useState(false)
  const spaces = useQuery({ queryKey: ['/api/espacos', {}], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { fullName: '', document: '', phone: '', visitDate: defaultDate < dates.today() ? dates.today() : defaultDate, spaceId: '' } })
  const { errors } = form.formState
  const create = useMutation({
    mutationFn: (values: Values) => api.post<Guest>('/api/convidados', {
      fullName: values.fullName.trim(), document: emptyToNull(values.document), phone: emptyToNull(values.phone), visitDate: values.visitDate, spaceId: values.spaceId || null, memberId: member?.id,
    } satisfies Schema<'CreateGuestRequest'>),
    onSuccess: (guest) => {
      toast.success(guest.feeAmount > 0 ? `Convidado cadastrado e autorizado. Taxa de ${fmt.money(guest.feeAmount)} lançada para o associado.` : 'Convidado cadastrado e autorizado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/convidados'] })
      onClose()
    },
    onError: toast.error,
  })
  const submit = form.handleSubmit((values) => { if (!member) { setMemberError(true); return } create.mutate(values) }, () => setMemberError(!member))

  return (
    <Modal open onClose={onClose} title="Novo convidado" description="Cadastros feitos pela equipe já entram autorizados. O limite diário por associado e a situação financeira são validados pelo servidor."
      footer={<><Button onClick={onClose}>Cancelar</Button><Button variant="primary" loading={create.isPending} onClick={() => void submit()}>Cadastrar</Button></>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <div className="col-span-2">
          <p className="mb-1.5 text-[13px] font-medium text-ink">Associado responsável<span className="text-danger"> *</span></p>
          <MemberPicker value={member} onChange={(m) => { setMember(m); setMemberError(false) }} />
          {memberError && <p className="mt-1 text-xs text-danger">Selecione o associado responsável</p>}
        </div>
        <Field className="col-span-2" label="Nome do convidado" required error={errors.fullName?.message}><Input aria-invalid={!!errors.fullName} {...form.register('fullName')} /></Field>
        <Field label="Documento" error={errors.document?.message} hint="Pode ser obrigatório conforme a política do clube."><Input {...form.register('document')} /></Field>
        <Field label="Telefone"><Controller control={form.control} name="phone" render={({ field }) => <MaskedInput mask="phone" value={field.value} onChange={field.onChange} placeholder="(00) 00000-0000" />} /></Field>
        <Field label="Data da visita" required error={errors.visitDate?.message}><Input type="date" min={dates.today()} aria-invalid={!!errors.visitDate} {...form.register('visitDate')} /></Field>
        <Field label="Espaço (opcional)">
          <Select {...form.register('spaceId')}><option value="">Não informado</option>{spaces.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
        </Field>
      </form>
    </Modal>
  )
}

function GuestsPage() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [date, setDate] = useState(dates.today())
  const [authorization, setAuthorization] = useState('')
  const [creating, setCreating] = useState(false)
  const manage = can('guests.manage')

  const list = usePaged<Guest>('/api/convidados', { date: date || undefined, authorization: authorization || undefined })
  const invalidate = () => void queryClient.invalidateQueries({ queryKey: ['/api/convidados'] })
  const authorize = useMutation({
    mutationFn: ({ id, authorized, note }: { id: string; authorized: boolean; note: string | null }) => api.post<Guest>(`/api/convidados/${id}/autorizacao`, { authorized, note }),
    onSuccess: (guest) => { toast.success(guest.authorization === 'Authorized' ? 'Convidado autorizado.' : 'Convidado não autorizado. O associado foi notificado.'); invalidate() },
    onError: toast.error,
  })
  const cancel = useMutation({
    mutationFn: (id: string) => api.post(`/api/convidados/${id}/cancelar`),
    onSuccess: () => { toast.success('Convite cancelado.'); invalidate() },
    onError: toast.error,
  })
  const deny = async (g: Guest) => {
    const result = await confirm({ title: `Não autorizar ${g.fullName}?`, message: 'O associado responsável será notificado com o motivo. A taxa ainda não paga é cancelada.', reason: 'required', reasonLabel: 'Motivo da recusa', danger: true, confirmLabel: 'Não autorizar' })
    if (result) authorize.mutate({ id: g.id, authorized: false, note: result.reason })
  }

  const columns: Column<Guest>[] = [
    { key: 'name', header: 'Convidado', render: (g) => <div><p className="font-medium text-ink">{g.fullName}</p><p className="text-xs text-muted">{fmt.phone(g.phone)}</p></div> },
    { key: 'document', header: 'Documento', render: (g) => <span className="tabular">{g.document ?? '—'}</span> },
    { key: 'member', header: 'Associado responsável', render: (g) => (can('members.view') ? <Link to={`/associados/${g.memberId}`} className="text-ink hover:text-brand-600 hover:underline" onClick={(e) => e.stopPropagation()}>{g.memberName}</Link> : g.memberName) },
    { key: 'date', header: 'Data', render: (g) => <span className="tabular">{fmt.day(g.visitDate)}</span> },
    {
      key: 'space', header: 'Espaço',
      render: (g) => g.reservationId && can('reservations.view') ? <Link to={`/reservas/${g.reservationId}`} className="text-brand-600 hover:underline">{g.spaceName ?? 'Reserva'}</Link> : g.spaceName ?? <span className="text-muted">—</span>,
    },
    {
      key: 'fee', header: 'Taxa',
      render: (g) => <div className="flex items-center gap-2"><span className="tabular">{g.feeAmount > 0 ? fmt.money(g.feeAmount) : '—'}</span><Badge tone={paymentTone[g.paymentStatus] ?? 'neutral'}>{g.paymentStatus}</Badge></div>,
    },
    {
      key: 'authorization', header: 'Autorização',
      render: (g) => g.isCancelled ? <Badge>Cancelado</Badge> : <span title={g.authorizationNote ?? undefined}><StatusBadge dict={labels.guestAuthorization} value={g.authorization} /></span>,
    },
    {
      key: 'access', header: 'Entrada / saída',
      render: (g) => g.enteredAt ? <span className="tabular whitespace-nowrap">{fmt.time(g.enteredAt)} – {g.exitedAt ? fmt.time(g.exitedAt) : <Badge tone="success">No clube</Badge>}</span> : <span className="text-muted">—</span>,
    },
  ]

  return (
    <>
      <PageHeader title="Convidados" subtitle="Cadastro antecipado, autorização e acompanhamento dos convidados dos associados."
        actions={manage && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo convidado</Button>} />
      <Card padded={false}>
        <Toolbar>
          <SearchInput value={list.search} onChange={list.setSearch} placeholder="Convidado, associado ou documento…" />
          <Input type="date" className="w-44" value={date} onChange={(e) => setDate(e.target.value)} aria-label="Data da visita" />
          {date !== dates.today() && <Button size="sm" onClick={() => setDate(dates.today())}>Hoje</Button>}
          {date && <Button size="sm" variant="ghost" onClick={() => setDate('')}>Todas as datas</Button>}
          <div className="w-56"><Select value={authorization} onChange={(e) => setAuthorization(e.target.value)}><option value="">Todas as autorizações</option>{options(labels.guestAuthorization).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
        </Toolbar>
        <DataTable columns={columns} rows={list.rows} rowKey={(g) => g.id} isLoading={list.isLoading} error={list.error} onRetry={() => list.refetch()}
          page={list.page} pageSize={list.pageSize} total={list.total} onPage={list.setPage}
          rowActions={manage ? (g) => (
            <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={g.isCancelled || g.enteredAt ? [] : [
              g.authorization !== 'Authorized' && { label: 'Autorizar', icon: Check, onClick: () => authorize.mutate({ id: g.id, authorized: true, note: null }) },
              g.authorization !== 'Denied' && { label: 'Não autorizar', icon: X, onClick: () => void deny(g) },
              {
                label: 'Cancelar convite', icon: Ban, danger: true, onClick: async () => {
                  if (await confirm({ title: 'Cancelar convite?', message: `${g.fullName} · ${fmt.day(g.visitDate)}. A taxa ainda não paga é cancelada.`, danger: true, confirmLabel: 'Cancelar convite' })) cancel.mutate(g.id)
                },
              },
            ]} />
          ) : undefined}
          empty={{
            title: 'Nenhum convidado encontrado',
            description: list.search || authorization ? 'Ajuste a pesquisa ou os filtros.' : date ? `Não há convidados cadastrados para ${fmt.day(date)}.` : 'Os convidados cadastrados pelos associados e pela equipe aparecem aqui.',
          }} />
      </Card>
      {creating && <GuestForm defaultDate={date || dates.today()} onClose={() => setCreating(false)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/convidados', label: 'Convidados', icon: UserPlus, group: 'Portaria', order: 3, permissions: ['guests.view'] }],
  routes: [{ path: 'convidados', component: GuestsPage, permissions: ['guests.view'] }],
}
export default feature
