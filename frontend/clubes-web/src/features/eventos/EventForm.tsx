import { zodResolver } from '@hookform/resolvers/zod'
import { Trash2 } from 'lucide-react'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, Checkbox, Field, FileImage, FileUpload, Input, Modal, Select, Switch, Textarea, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { dates, emptyToNull } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import type { ClubEvent } from './audience'

const schema = z.object({
  title: z.string().trim().min(3, 'Informe o título (mínimo de 3 caracteres)').max(150, 'Máximo de 150 caracteres'),
  description: z.string().max(8000, 'Máximo de 8.000 caracteres'),
  imageFileId: z.string().nullable(),
  startAt: z.string().min(1, 'Informe o início'),
  endAt: z.string().min(1, 'Informe o término'),
  location: z.string().max(200, 'Máximo de 200 caracteres'),
  spaceId: z.string(),
  capacity: z.string().refine((v) => v === '' || (/^\d+$/.test(v) && Number(v) >= 1 && Number(v) <= 100000), 'Informe um número inteiro maior que zero'),
  audience: z.string(),
  allowMembers: z.boolean(),
  allowDependents: z.boolean(),
  allowStaff: z.boolean(),
  registrationRequired: z.boolean(),
  registrationDeadline: z.string(),
  cancellationDeadline: z.string(),
}).superRefine((v, ctx) => {
  if (v.startAt && v.endAt && new Date(v.endAt) <= new Date(v.startAt)) ctx.addIssue({ code: 'custom', path: ['endAt'], message: 'O término deve ser posterior ao início' })
  if (v.registrationRequired && v.registrationDeadline && v.startAt && new Date(v.registrationDeadline) > new Date(v.startAt)) ctx.addIssue({ code: 'custom', path: ['registrationDeadline'], message: 'Deve ser anterior ao início do evento' })
  if (v.audience === 'Specific' && !(v.allowMembers || v.allowDependents || v.allowStaff)) ctx.addIssue({ code: 'custom', path: ['audience'], message: 'Selecione ao menos um público' })
})
type Values = z.infer<typeof schema>

export function EventForm({ event, onClose, onSaved }: { event?: ClubEvent; onClose: () => void; onSaved: (saved: ClubEvent) => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const spaces = useQuery({ queryKey: ['/api/espacos'], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: event?.title ?? '', description: event?.description ?? '', imageFileId: event?.imageFileId ?? null, startAt: dates.toInput(event?.startAt), endAt: dates.toInput(event?.endAt),
      location: event?.location ?? '', spaceId: event?.spaceId ?? '', capacity: event?.capacity != null ? String(event.capacity) : '', audience: event?.audience ?? 'Public',
      allowMembers: event?.allowMembers ?? true, allowDependents: event?.allowDependents ?? false, allowStaff: event?.allowStaff ?? false,
      registrationRequired: event?.registrationRequired ?? false, registrationDeadline: dates.toInput(event?.registrationDeadline), cancellationDeadline: dates.toInput(event?.cancellationDeadline),
    },
  })
  const { errors } = form.formState
  const imageFileId = form.watch('imageFileId')
  const audience = form.watch('audience')
  const registrationRequired = form.watch('registrationRequired')

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body: Schema<'SaveEventRequest'> = {
        title: values.title, description: emptyToNull(values.description), imageFileId: values.imageFileId, startAt: dates.fromInput(values.startAt)!, endAt: dates.fromInput(values.endAt)!,
        location: emptyToNull(values.location), spaceId: values.spaceId || null, capacity: values.capacity ? Number(values.capacity) : null, audience: values.audience as Schema<'EventAudience'>,
        allowMembers: values.allowMembers, allowDependents: values.allowDependents, allowStaff: values.allowStaff, registrationRequired: values.registrationRequired,
        registrationDeadline: values.registrationRequired ? dates.fromInput(values.registrationDeadline) : null,
        cancellationDeadline: values.registrationRequired ? dates.fromInput(values.cancellationDeadline) : null,
      }
      return event ? api.put<ClubEvent>(`/api/eventos/${event.id}`, body) : api.post<ClubEvent>('/api/eventos', body)
    },
    onSuccess: (saved) => {
      toast.success(event ? 'Evento atualizado.' : 'Evento criado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/eventos'] })
      void queryClient.invalidateQueries({ queryKey: ['evento', saved.id] })
      onSaved(saved)
    },
    onError: toast.error,
  })

  const group = 'text-xs font-semibold tracking-wider text-muted uppercase'
  return (
    <Modal open onClose={onClose} size="lg" title={event ? 'Editar evento' : 'Novo evento'}
      description={event && event.registered > 0 ? 'Se a data ou o horário mudarem, os inscritos serão notificados.' : undefined}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{event ? 'Salvar alterações' : 'Criar evento'}</Button>
      </>}>
      <form className="space-y-6" noValidate onSubmit={(e) => e.preventDefault()}>
        <fieldset className="space-y-4">
          <legend className={group}>Evento</legend>
          <Field label="Título" required error={errors.title?.message}><Input aria-invalid={!!errors.title} maxLength={150} {...form.register('title')} /></Field>
          <Field label="Descrição" error={errors.description?.message}><Textarea rows={4} {...form.register('description')} /></Field>
          <div className="flex items-center gap-4">
            {imageFileId
              ? <FileImage fileId={imageFileId} alt="Imagem do evento" className="h-20 w-36 rounded-(--radius-control) border border-line object-cover" />
              : <div className="flex h-20 w-36 items-center justify-center rounded-(--radius-control) border border-dashed border-line text-xs text-muted">Sem imagem</div>}
            <div className="flex items-center gap-2">
              <FileUpload size="sm" label={imageFileId ? 'Trocar imagem' : 'Enviar imagem'} accept="image/jpeg,image/png,image/webp" scope="ClubPublic" onUploaded={(file) => form.setValue('imageFileId', file.id, { shouldDirty: true })} />
              {imageFileId && <Button size="sm" variant="ghost" icon={Trash2} onClick={() => form.setValue('imageFileId', null, { shouldDirty: true })}>Remover</Button>}
            </div>
          </div>
        </fieldset>

        <fieldset className="space-y-4">
          <legend className={group}>Data e local</legend>
          <div className="grid grid-cols-6 gap-4">
            <Field className="col-span-3" label="Início" required error={errors.startAt?.message}><Input type="datetime-local" aria-invalid={!!errors.startAt} {...form.register('startAt')} /></Field>
            <Field className="col-span-3" label="Término" required error={errors.endAt?.message}><Input type="datetime-local" aria-invalid={!!errors.endAt} {...form.register('endAt')} /></Field>
            <Field className="col-span-3" label="Local" error={errors.location?.message}><Input placeholder="Ex.: Salão social" {...form.register('location')} /></Field>
            <Field className="col-span-2" label="Espaço do clube" hint="Opcional.">
              <Select {...form.register('spaceId')} value={form.watch('spaceId')}><option value="">Nenhum</option>{(spaces.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
            </Field>
            <Field className="col-span-1" label="Capacidade" error={errors.capacity?.message}><Input inputMode="numeric" placeholder="Livre" aria-invalid={!!errors.capacity} {...form.register('capacity')} /></Field>
          </div>
        </fieldset>

        <fieldset className="space-y-3">
          <legend className={group}>Público</legend>
          <Field error={errors.audience?.message}>
            <Select {...form.register('audience')}>{options(labels.eventAudience).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
          </Field>
          {audience === 'Specific' && (
            <div className="flex flex-wrap gap-x-6 gap-y-2">
              <Checkbox label="Associados" {...form.register('allowMembers')} />
              <Checkbox label="Dependentes" {...form.register('allowDependents')} />
              <Checkbox label="Funcionários" {...form.register('allowStaff')} />
            </div>
          )}
          {audience === 'MembersOnly' && <Alert tone="info">Dependentes não verão este evento no aplicativo.</Alert>}
        </fieldset>

        <fieldset className="space-y-4">
          <legend className={group}>Inscrições</legend>
          <Controller control={form.control} name="registrationRequired" render={({ field }) => (
            <Switch checked={field.value} onChange={field.onChange} label="Exige inscrição" description="Associados e dependentes se inscrevem pelo aplicativo, respeitando a capacidade." />
          )} />
          {registrationRequired && (
            <div className="grid grid-cols-2 gap-4">
              <Field label="Prazo de inscrição" hint="Em branco: até o início do evento." error={errors.registrationDeadline?.message}><Input type="datetime-local" aria-invalid={!!errors.registrationDeadline} {...form.register('registrationDeadline')} /></Field>
              <Field label="Prazo para cancelar a inscrição" hint="Em branco: até o início do evento."><Input type="datetime-local" {...form.register('cancellationDeadline')} /></Field>
            </div>
          )}
        </fieldset>
      </form>
    </Modal>
  )
}
