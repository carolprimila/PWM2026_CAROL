import { zodResolver } from '@hookform/resolvers/zod'
import { Ban, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { DataTable, Toolbar, type Column } from '@/components/table'
import { Alert, Button, Card, Field, IconButton, Input, Modal, Select, StatusBadge, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import type { Space } from './helpers'

type Block = Schema<'BlockDto'>
type Result = Schema<'BlockResult'>

const schema = z.object({
  spaceId: z.string(),
  startAt: z.string().min(1, 'Informe o início'),
  endAt: z.string().min(1, 'Informe o término'),
  reason: z.string().trim().min(3, 'Descreva o motivo (mínimo de 3 caracteres)').max(300, 'Máximo de 300 caracteres'),
  kind: z.string(),
}).refine((v) => !v.startAt || !v.endAt || new Date(v.endAt) > new Date(v.startAt), { path: ['endAt'], message: 'O término deve ser posterior ao início' })
type Values = z.infer<typeof schema>

function BlockForm({ spaces, onClose, onCreated }: { spaces: Space[]; onClose: () => void; onCreated: (result: Result) => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { spaceId: '', startAt: '', endAt: '', reason: '', kind: 'Administrative' } })
  const { errors } = form.formState
  const create = useMutation({
    mutationFn: (values: Values) => api.post<Result>('/api/espacos/bloqueios', {
      spaceId: values.spaceId || null, startAt: dates.fromInput(values.startAt), endAt: dates.fromInput(values.endAt), reason: values.reason.trim(), kind: values.kind,
    }),
    onSuccess: (result) => {
      toast.success('Bloqueio criado. A disponibilidade foi atualizada.')
      void queryClient.invalidateQueries({ queryKey: ['/api/espacos/bloqueios'] })
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
      onCreated(result)
    },
    onError: toast.error,
  })
  return (
    <Modal open onClose={onClose} title="Novo bloqueio" description="O bloqueio impede novas reservas no período e vale imediatamente."
      footer={<><Button onClick={onClose}>Cancelar</Button><Button variant="primary" loading={create.isPending} onClick={form.handleSubmit((values) => create.mutate(values))}>Criar bloqueio</Button></>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field label="Espaço">
          <Select {...form.register('spaceId')}><option value="">Todos os espaços</option>{spaces.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
        </Field>
        <Field label="Tipo">
          <Select {...form.register('kind')}>{options(labels.blockKind).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
        </Field>
        <Field label="Início" required error={errors.startAt?.message}><Input type="datetime-local" aria-invalid={!!errors.startAt} {...form.register('startAt')} /></Field>
        <Field label="Término" required error={errors.endAt?.message}><Input type="datetime-local" aria-invalid={!!errors.endAt} {...form.register('endAt')} /></Field>
        <Field className="col-span-2" label="Motivo" required error={errors.reason?.message}><Textarea rows={3} aria-invalid={!!errors.reason} {...form.register('reason')} /></Field>
      </form>
    </Modal>
  )
}

export function BlocksTab() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [from, setFrom] = useState(dates.today())
  const [to, setTo] = useState('')
  const [spaceId, setSpaceId] = useState('')
  const [creating, setCreating] = useState(false)
  const [result, setResult] = useState<Result | null>(null)
  const manage = can('spaces.manage')

  const spaces = useQuery({ queryKey: ['/api/espacos', { includeInactive: true }], queryFn: () => api.get<Space[]>('/api/espacos', { includeInactive: true }) })
  const filters = { from: from ? dates.startOfDay(from) : undefined, to: to ? dates.endOfDay(to) : undefined, spaceId: spaceId || undefined }
  const blocks = useQuery({ queryKey: ['/api/espacos/bloqueios', filters], queryFn: () => api.get<Block[]>('/api/espacos/bloqueios', filters) })
  const cancel = useMutation({
    mutationFn: (id: string) => api.delete(`/api/espacos/bloqueios/${id}`),
    onSuccess: () => {
      toast.success('Bloqueio cancelado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/espacos/bloqueios'] })
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
    },
    onError: toast.error,
  })

  const columns: Column<Block>[] = [
    { key: 'space', header: 'Espaço', render: (b) => <span className="font-medium text-ink">{b.spaceName ?? 'Todos os espaços'}</span> },
    { key: 'start', header: 'Início', render: (b) => <span className="tabular">{fmt.dateTime(b.startAt)}</span> },
    { key: 'end', header: 'Término', render: (b) => <span className="tabular">{fmt.dateTime(b.endAt)}</span> },
    { key: 'kind', header: 'Tipo', render: (b) => <StatusBadge dict={labels.blockKind} value={b.kind} /> },
    { key: 'reason', header: 'Motivo', render: (b) => <span className="text-ink/80">{b.reason}</span> },
    { key: 'created', header: 'Criado em', render: (b) => <span className="text-muted">{fmt.date(b.createdAt)}</span> },
  ]

  return (
    <>
      <Card padded={false}>
        <Toolbar actions={manage && <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo bloqueio</Button>}>
          <label className="flex items-center gap-2 text-[13px] text-muted">De<Input type="date" className="w-40" value={from} onChange={(e) => setFrom(e.target.value)} /></label>
          <label className="flex items-center gap-2 text-[13px] text-muted">até<Input type="date" className="w-40" value={to} onChange={(e) => setTo(e.target.value)} /></label>
          <div className="w-56"><Select value={spaceId} onChange={(e) => setSpaceId(e.target.value)}><option value="">Todos os espaços</option>{spaces.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select></div>
        </Toolbar>
        <DataTable columns={columns} rows={blocks.data ?? []} rowKey={(b) => b.id} isLoading={blocks.isLoading} error={blocks.error} onRetry={() => blocks.refetch()}
          empty={{ title: 'Nenhum bloqueio no período', description: 'Bloqueios impedem reservas durante manutenções, eventos e fechamentos.' }}
          rowActions={manage ? (b) => (
            <IconButton icon={Trash2} label="Cancelar bloqueio" onClick={async () => {
              if (await confirm({ title: 'Cancelar bloqueio?', message: `${b.spaceName ?? 'Todos os espaços'} · ${fmt.dateTime(b.startAt)} a ${fmt.dateTime(b.endAt)}. Os horários voltam a ficar disponíveis para reserva.`, danger: true, confirmLabel: 'Cancelar bloqueio' })) cancel.mutate(b.id)
            }} />
          ) : undefined} />
      </Card>

      {creating && <BlockForm spaces={spaces.data ?? []} onClose={() => setCreating(false)} onCreated={(created) => { setCreating(false); if (created.conflicts.length > 0) setResult(created) }} />}

      {result && (
        <Modal open onClose={() => setResult(null)} size="lg" title="Reservas existentes em conflito com o bloqueio" footer={<Button variant="primary" onClick={() => setResult(null)}>Entendi</Button>}>
          <Alert tone="warning" title={`${result.conflicts.length} reserva(s) no período bloqueado`}>
            O bloqueio foi criado, mas estas reservas continuam ativas. Abra cada uma para reagendar ou cancelar, avisando o associado.
          </Alert>
          <ul className="mt-4 divide-y divide-line rounded-(--radius-control) border border-line">
            {result.conflicts.map((c) => (
              <li key={c.reservationId} className="flex items-center gap-4 px-4 py-3 text-sm">
                <Ban className="size-4 shrink-0 text-warning" />
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium text-ink">{c.code} · {c.spaceName}</p>
                  <p className="truncate text-xs text-muted">{c.memberName ?? 'Reserva administrativa'} · {fmt.dateTime(c.startAt)} a {fmt.dateTime(c.endAt)}</p>
                </div>
                <StatusBadge dict={labels.reservationStatus} value={c.status} />
                <Link to={`/reservas/${c.reservationId}`} target="_blank" className="text-[13px] font-medium text-brand-600 hover:underline">Abrir reserva</Link>
              </li>
            ))}
          </ul>
        </Modal>
      )}
    </>
  )
}
