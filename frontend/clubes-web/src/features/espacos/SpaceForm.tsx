import { zodResolver } from '@hookform/resolvers/zod'
import { Copy } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, Checkbox, Field, FileImage, FileUpload, IconButton, Input, Modal, MoneyInput, Select, Switch, Textarea, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { emptyToNull } from '@/lib/format'
import { dayOfWeek, daysOfWeek, labels, options } from '@/lib/labels'
import { minutesToTime, timeToMinutes, type Space } from './helpers'

const integer = (min: number, max: number, message: string, allowEmpty = false) =>
  z.string().refine((v) => (v.trim() === '' ? allowEmpty : /^\d+$/.test(v.trim()) && Number(v) >= min && Number(v) <= max), message)

const schema = z.object({
  name: z.string().trim().min(2, 'Informe o nome do espaço').max(100, 'Máximo de 100 caracteres'),
  type: z.string(),
  subtype: z.string().max(50, 'Máximo de 50 caracteres'),
  description: z.string().max(2000, 'Máximo de 2000 caracteres'),
  capacity: integer(1, 100_000, 'Informe um número de pessoas válido', true),
  isReservable: z.boolean(),
  bookingMode: z.string(),
  slotMinutes: integer(15, 720, 'Entre 15 e 720 minutos'),
  fixedDays: integer(1, 30, 'Entre 1 e 30 dias'),
  hourlyPrice: z.number().min(0).nullable(),
  periodPrice: z.number().min(0).nullable(),
  cleaningFee: z.number().min(0).nullable(),
  chargeMode: z.string(),
  approvalMode: z.string(),
  usageRules: z.string().max(4000, 'Máximo de 4000 caracteres'),
  imageFileId: z.string().nullable(),
  sortOrder: integer(0, 100_000, 'Informe um número'),
  isActive: z.boolean(),
})
type Values = z.infer<typeof schema>
type Day = Schema<'DayOfWeek'>
type WeekSchedule = Record<Day, { on: boolean; open: string; close: string }>

export function SpaceForm({ space, onClose }: { space?: Space; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: space?.name ?? '', type: space?.type ?? 'Court', subtype: space?.subtype ?? '', description: space?.description ?? '',
      capacity: space?.capacity?.toString() ?? '', isReservable: space?.isReservable ?? true, bookingMode: space?.bookingMode ?? 'Hourly',
      slotMinutes: String(space?.slotMinutes ?? 60), fixedDays: String(space?.fixedDays ?? 1), hourlyPrice: space?.hourlyPrice ?? null,
      periodPrice: space?.periodPrice ?? null, cleaningFee: space?.cleaningFee ?? null, chargeMode: space?.chargeMode ?? '', approvalMode: space?.approvalMode ?? '',
      usageRules: space?.usageRules ?? '', imageFileId: space?.imageFileId ?? null, sortOrder: String(space?.sortOrder ?? 0), isActive: space?.isActive ?? true,
    },
  })
  const { errors } = form.formState
  const [week, setWeek] = useState<WeekSchedule>(() => Object.fromEntries(daysOfWeek.map((day) => {
    const current = space?.schedules.find((s) => s.dayOfWeek === day)
    return [day, { on: !!current, open: minutesToTime(current?.openMinute ?? 480), close: minutesToTime(current?.closeMinute ?? 1320) }]
  })) as WeekSchedule)
  const [weekError, setWeekError] = useState<string | null>(null)
  const setDay = (day: Day, patch: Partial<WeekSchedule[Day]>) => setWeek((current) => ({ ...current, [day]: { ...current[day], ...patch } }))

  const type = form.watch('type')
  const reservable = form.watch('isReservable') && type !== 'Pool'
  const mode = form.watch('bookingMode')
  const imageFileId = form.watch('imageFileId')

  const save = useMutation({
    mutationFn: (values: Values) => {
      const schedules: Schema<'ScheduleDto'>[] = []
      for (const day of daysOfWeek) {
        if (!week[day].on) continue
        const openMinute = timeToMinutes(week[day].open)
        const closeMinute = timeToMinutes(week[day].close, true)
        if (openMinute == null || closeMinute == null) throw new ApiError(400, 'horario', `Informe abertura e fechamento de ${dayOfWeek[day].label.toLowerCase()}.`)
        if (closeMinute <= openMinute) throw new ApiError(400, 'horario', `Em ${dayOfWeek[day].label.toLowerCase()}, o fechamento deve ser posterior à abertura.`)
        schedules.push({ dayOfWeek: day, openMinute, closeMinute })
      }
      const body: Schema<'SaveSpaceRequest'> = {
        name: values.name.trim(), type: values.type as Schema<'SpaceType'>, subtype: emptyToNull(values.subtype), description: emptyToNull(values.description),
        capacity: values.capacity.trim() === '' ? null : Number(values.capacity), isReservable: values.isReservable && values.type !== 'Pool',
        bookingMode: values.bookingMode as Schema<'BookingMode'>, slotMinutes: Number(values.slotMinutes), fixedDays: Number(values.fixedDays),
        hourlyPrice: values.hourlyPrice, periodPrice: values.periodPrice, cleaningFee: values.cleaningFee,
        chargeMode: (values.chargeMode || null) as Schema<'SaveSpaceRequest'>['chargeMode'], approvalMode: (values.approvalMode || null) as Schema<'SaveSpaceRequest'>['approvalMode'],
        usageRules: emptyToNull(values.usageRules), imageFileId: values.imageFileId, isActive: values.isActive, sortOrder: Number(values.sortOrder), schedules,
      }
      return space ? api.put<Space>(`/api/espacos/${space.id}`, body) : api.post<Space>('/api/espacos', body)
    },
    onMutate: () => setWeekError(null),
    onSuccess: () => {
      toast.success(space ? 'Espaço atualizado.' : 'Espaço cadastrado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/espacos'] })
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
      onClose()
    },
    onError: (error) => {
      if (error instanceof ApiError && error.code === 'horario') { setWeekError(error.message); return }
      if (error instanceof ApiError && error.errors) {
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
      }
      toast.error(error)
    },
  })

  const group = 'text-xs font-semibold tracking-wider text-muted uppercase'
  return (
    <Modal open onClose={onClose} size="lg" title={space ? `Editar ${space.name}` : 'Novo espaço'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{space ? 'Salvar alterações' : 'Cadastrar'}</Button>
      </>}>
      <form className="space-y-6" noValidate onSubmit={(e) => e.preventDefault()}>
        <fieldset className="space-y-4">
          <legend className={group}>Identificação</legend>
          <div className="grid grid-cols-6 gap-4">
            <Field className="col-span-3" label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} {...form.register('name')} /></Field>
            <Field className="col-span-3" label="Tipo" required>
              <Select {...form.register('type')}>{options(labels.spaceType).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
            </Field>
            <Field className="col-span-3" label="Subtipo" hint="Ex.: Tênis, Futebol society, Churrasqueira." error={errors.subtype?.message}><Input {...form.register('subtype')} /></Field>
            <Field className="col-span-2" label="Capacidade (pessoas)" required={reservable} error={errors.capacity?.message}><Input inputMode="numeric" aria-invalid={!!errors.capacity} {...form.register('capacity')} /></Field>
            <Field className="col-span-1" label="Ordem" error={errors.sortOrder?.message}><Input inputMode="numeric" {...form.register('sortOrder')} /></Field>
            <Field className="col-span-6" label="Descrição" error={errors.description?.message}><Textarea rows={2} {...form.register('description')} /></Field>
          </div>
          <div className="flex items-center gap-4">
            {imageFileId ? <FileImage fileId={imageFileId} alt="Imagem do espaço" className="h-16 w-24 rounded-lg object-cover" /> : <span className="flex h-16 w-24 items-center justify-center rounded-lg bg-canvas text-xs text-muted">Sem imagem</span>}
            <FileUpload size="sm" label="Enviar imagem" accept="image/jpeg,image/png,image/webp" scope="ClubPublic" onUploaded={(file) => form.setValue('imageFileId', file.id, { shouldDirty: true })} />
            {imageFileId && <Button size="sm" variant="ghost" onClick={() => form.setValue('imageFileId', null, { shouldDirty: true })}>Remover</Button>}
          </div>
          <Controller control={form.control} name="isActive" render={({ field }) => <Switch checked={field.value} onChange={field.onChange} label="Espaço ativo" description="Espaços inativos não aparecem para os associados nem aceitam novas reservas." />} />
        </fieldset>

        <fieldset className="space-y-4">
          <legend className={group}>Reservas</legend>
          <Controller control={form.control} name="isReservable" render={({ field }) => (
            <Switch checked={field.value && type !== 'Pool'} onChange={field.onChange} disabled={type === 'Pool'} label="Permite reserva"
              description={type === 'Pool' ? 'Piscinas não podem ser reservadas individualmente.' : 'Espaços reserváveis exigem capacidade máxima definida.'} />
          )} />
          {reservable && (
            <div className="grid grid-cols-6 gap-4">
              <Field className="col-span-3" label="Modo de reserva">
                <Select {...form.register('bookingMode')}>{options(labels.bookingMode).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
              </Field>
              {mode === 'Hourly'
                ? <Field className="col-span-3" label="Duração do intervalo (min)" error={errors.slotMinutes?.message} hint="O funcionamento deve ser múltiplo desta duração."><Input inputMode="numeric" aria-invalid={!!errors.slotMinutes} {...form.register('slotMinutes')} /></Field>
                : <Field className="col-span-3" label="Dias fixos do período" error={errors.fixedDays?.message} hint="Inclui montagem, evento, retirada e limpeza."><Input inputMode="numeric" aria-invalid={!!errors.fixedDays} {...form.register('fixedDays')} /></Field>}
              {mode === 'Hourly'
                ? <Field className="col-span-2" label="Preço por hora" hint="Vazio = não definido."><Controller control={form.control} name="hourlyPrice" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} placeholder="Não definido" />} /></Field>
                : <Field className="col-span-2" label="Preço do período" hint="Vazio = não definido."><Controller control={form.control} name="periodPrice" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} placeholder="Não definido" />} /></Field>}
              <Field className="col-span-2" label="Taxa de limpeza" hint="Incluída no valor total exibido ao associado.">
                <Controller control={form.control} name="cleaningFee" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} placeholder="Não definida" />} />
              </Field>
              <Field className="col-span-2" label="Forma de cobrança">
                <Select {...form.register('chargeMode')}><option value="">Não definida</option>{options(labels.chargeMode).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
              </Field>
              <Field className="col-span-3" label="Aprovação das reservas">
                <Select {...form.register('approvalMode')}><option value="">Seguir a política do clube</option>{options(labels.approvalMode).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
              </Field>
              <Field className="col-span-6" label="Regras de uso" error={errors.usageRules?.message}><Textarea rows={3} {...form.register('usageRules')} /></Field>
            </div>
          )}
        </fieldset>

        <fieldset className="space-y-3">
          <legend className={group}>Funcionamento semanal</legend>
          <p className="text-[13px] text-muted">Marque os dias em que o espaço funciona. Fechamento às 00:00 significa o fim do dia.</p>
          {weekError && <Alert tone="danger">{weekError}</Alert>}
          <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
            {daysOfWeek.map((day) => (
              <li key={day} className="flex items-center gap-4 px-4 py-2">
                <Checkbox className="w-40" label={dayOfWeek[day].label} checked={week[day].on} onChange={(e) => setDay(day, { on: e.target.checked })} />
                {week[day].on ? (
                  <>
                    <Input type="time" className="h-9 w-28" aria-label={`Abertura — ${dayOfWeek[day].label}`} value={week[day].open} onChange={(e) => setDay(day, { open: e.target.value })} />
                    <span className="text-sm text-muted">às</span>
                    <Input type="time" className="h-9 w-28" aria-label={`Fechamento — ${dayOfWeek[day].label}`} value={week[day].close} onChange={(e) => setDay(day, { close: e.target.value })} />
                    <IconButton className="ml-auto" icon={Copy} label="Copiar para todos os dias"
                      onClick={() => setWeek(Object.fromEntries(daysOfWeek.map((d) => [d, { ...week[day] }])) as WeekSchedule)} />
                  </>
                ) : <span className="text-sm text-muted">Não funciona</span>}
              </li>
            ))}
          </ul>
        </fieldset>
      </form>
    </Modal>
  )
}
