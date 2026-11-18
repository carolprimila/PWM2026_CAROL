import { zodResolver } from '@hookform/resolvers/zod'
import { ChevronLeft, ChevronRight, MoreHorizontal, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { DataTable, Toolbar, type Column } from '@/components/table'
import { Badge, Button, Card, Field, IconButton, Input, Menu, Modal, Select, StatusBadge, Switch, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { labels, options, weekDays } from '@/lib/labels'
import { minutesToTime, timeToMinutes } from './helpers'

type Holiday = Schema<'HolidayDto'>

const schema = z.object({
  name: z.string().trim().min(2, 'Informe o nome').max(120, 'Máximo de 120 caracteres'),
  date: z.string().min(1, 'Informe a data'),
  scope: z.string(),
  isClosed: z.boolean(),
  open: z.string(),
  close: z.string(),
  notes: z.string().max(500, 'Máximo de 500 caracteres'),
}).superRefine((v, ctx) => {
  if (v.isClosed || (!v.open && !v.close)) return
  const open = timeToMinutes(v.open)
  const close = timeToMinutes(v.close, true)
  if (open == null || close == null) ctx.addIssue({ code: 'custom', path: ['close'], message: 'Informe abertura e fechamento do horário especial' })
  else if (close <= open) ctx.addIssue({ code: 'custom', path: ['close'], message: 'O fechamento deve ser posterior à abertura' })
})
type Values = z.infer<typeof schema>

function HolidayForm({ holiday, year, onClose }: { holiday?: Holiday; year: number; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: holiday?.name ?? '', date: holiday?.date ?? (year === new Date().getFullYear() ? '' : `${year}-01-01`), scope: holiday?.scope ?? 'National', isClosed: holiday?.isClosed ?? true,
      open: minutesToTime(holiday?.openMinute), close: minutesToTime(holiday?.closeMinute), notes: holiday?.notes ?? '',
    },
  })
  const { errors } = form.formState
  const closed = form.watch('isClosed')
  const save = useMutation({
    mutationFn: (values: Values) => {
      const body: Schema<'SaveHolidayRequest'> = {
        date: values.date, name: values.name.trim(), scope: values.scope as Schema<'HolidayScope'>, isClosed: values.isClosed,
        openMinute: values.isClosed ? null : timeToMinutes(values.open), closeMinute: values.isClosed ? null : timeToMinutes(values.close, true), notes: emptyToNull(values.notes),
      }
      return holiday ? api.put<Holiday>(`/api/espacos/feriados/${holiday.id}`, body) : api.post<Holiday>('/api/espacos/feriados', body)
    },
    onSuccess: () => {
      toast.success(holiday ? 'Data atualizada.' : 'Data cadastrada.')
      void queryClient.invalidateQueries({ queryKey: ['/api/espacos/feriados'] })
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
      onClose()
    },
    onError: toast.error,
  })
  return (
    <Modal open onClose={onClose} title={holiday ? 'Editar data' : 'Novo feriado ou data especial'}
      footer={<><Button onClick={onClose}>Cancelar</Button><Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{holiday ? 'Salvar alterações' : 'Cadastrar'}</Button></>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-2" label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} placeholder="Ex.: Natal, Aniversário do clube" {...form.register('name')} /></Field>
        <Field label="Data" required error={errors.date?.message}><Input type="date" aria-invalid={!!errors.date} {...form.register('date')} /></Field>
        <Field label="Classificação">
          <Select {...form.register('scope')}>{options(labels.holidayScope).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
        </Field>
        <div className="col-span-2">
          <Controller control={form.control} name="isClosed" render={({ field }) => (
            <Switch checked={field.value} onChange={field.onChange} label="Clube fechado nesta data" description="Fechado: nenhum espaço aceita reservas. Aberto: opcionalmente, informe um horário especial de funcionamento." />
          )} />
        </div>
        {!closed && (
          <>
            <Field label="Abertura (horário especial)" hint="Deixe os dois vazios para manter o horário normal."><Input type="time" {...form.register('open')} /></Field>
            <Field label="Fechamento (horário especial)" error={errors.close?.message} hint="00:00 = fim do dia."><Input type="time" aria-invalid={!!errors.close} {...form.register('close')} /></Field>
          </>
        )}
        <Field className="col-span-2" label="Observações" error={errors.notes?.message}><Textarea rows={2} {...form.register('notes')} /></Field>
      </form>
    </Modal>
  )
}

export function HolidaysTab() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [year, setYear] = useState(new Date().getFullYear())
  const [form, setForm] = useState<{ holiday?: Holiday } | null>(null)
  const manage = can('spaces.manage', 'settings.manage')

  const holidays = useQuery({ queryKey: ['/api/espacos/feriados', year], queryFn: () => api.get<Holiday[]>('/api/espacos/feriados', { year }) })
  const remove = useMutation({
    mutationFn: (id: string) => api.delete(`/api/espacos/feriados/${id}`),
    onSuccess: () => {
      toast.success('Data removida.')
      void queryClient.invalidateQueries({ queryKey: ['/api/espacos/feriados'] })
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
    },
    onError: toast.error,
  })

  const columns: Column<Holiday>[] = [
    {
      key: 'date', header: 'Data', width: '190px',
      render: (h) => <div><p className="tabular font-medium text-ink">{fmt.day(h.date)}</p><p className="text-xs text-muted">{weekDays[new Date(`${h.date}T12:00:00`).getDay()]}</p></div>,
    },
    { key: 'name', header: 'Nome', render: (h) => <div><p className="font-medium text-ink">{h.name}</p>{h.notes && <p className="text-xs text-muted">{h.notes}</p>}</div> },
    { key: 'scope', header: 'Classificação', render: (h) => <StatusBadge dict={labels.holidayScope} value={h.scope} /> },
    {
      key: 'hours', header: 'Funcionamento',
      render: (h) => h.isClosed ? <Badge tone="danger">Fechado</Badge>
        : h.openMinute != null && h.closeMinute != null ? <Badge tone="warning">Horário especial · {fmt.minutes(h.openMinute)}–{fmt.minutes(h.closeMinute)}</Badge>
        : <Badge tone="success">Horário normal</Badge>,
    },
  ]

  return (
    <>
      <Card padded={false}>
        <Toolbar actions={manage && <Button variant="primary" icon={Plus} onClick={() => setForm({})}>Nova data</Button>}>
          <IconButton icon={ChevronLeft} label="Ano anterior" onClick={() => setYear(year - 1)} />
          <span className="tabular w-14 text-center text-sm font-semibold text-ink">{year}</span>
          <IconButton icon={ChevronRight} label="Próximo ano" onClick={() => setYear(year + 1)} />
        </Toolbar>
        <DataTable columns={columns} rows={holidays.data ?? []} rowKey={(h) => h.id} isLoading={holidays.isLoading} error={holidays.error} onRetry={() => holidays.refetch()}
          empty={{ title: `Nenhuma data cadastrada em ${year}`, description: 'Cadastre feriados, fechamentos excepcionais e horários especiais. Eles refletem na disponibilidade das reservas.' }}
          rowActions={manage ? (h) => (
            <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
              { label: 'Editar', icon: Pencil, onClick: () => setForm({ holiday: h }) },
              {
                label: 'Excluir', icon: Trash2, danger: true, onClick: async () => {
                  if (await confirm({ title: 'Excluir data?', message: `${h.name} · ${fmt.day(h.date)}`, danger: true, confirmLabel: 'Excluir' })) remove.mutate(h.id)
                },
              },
            ]} />
          ) : undefined} />
      </Card>
      {form && <HolidayForm holiday={form.holiday} year={year} onClose={() => setForm(null)} />}
    </>
  )
}
