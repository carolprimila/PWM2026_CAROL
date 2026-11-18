import { zodResolver } from '@hookform/resolvers/zod'
import { Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, Checkbox, cn, Field, FileImage, FileUpload, Input, Modal, Select, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import type { Announcement } from './audience'
import { UserPicker, type Target } from './UserPicker'

const schema = z.object({
  title: z.string().trim().min(3, 'Informe o título (mínimo de 3 caracteres)').max(150, 'Máximo de 150 caracteres'),
  body: z.string().trim().min(1, 'Escreva o conteúdo do comunicado').max(20000, 'Máximo de 20.000 caracteres'),
  category: z.string(),
  priority: z.string(),
  expiresAt: z.string(),
  eventId: z.string(),
  isSpecific: z.boolean(),
  audienceMembers: z.boolean(),
  audienceDependents: z.boolean(),
  audienceStaff: z.boolean(),
  imageFileId: z.string().nullable(),
})
type Values = z.infer<typeof schema>

export function AnnouncementForm({ announcement, onClose, onSaved }: { announcement?: Announcement; onClose: () => void; onSaved: (saved: Announcement) => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const published = !!announcement?.publishedAt
  const canPickUsers = can('users.view')
  const [targets, setTargets] = useState<Target[]>(() => (announcement?.targetUserIds ?? []).map((id) => ({ id, name: '' })))
  const [audienceError, setAudienceError] = useState<string | null>(null)

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: announcement?.title ?? '', body: announcement?.body ?? '', category: announcement?.category ?? 'Communication', priority: announcement?.priority ?? 'Normal',
      expiresAt: dates.toInput(announcement?.expiresAt), eventId: announcement?.eventId ?? '', isSpecific: announcement?.isSpecific ?? false,
      audienceMembers: announcement?.audienceMembers ?? true, audienceDependents: announcement?.audienceDependents ?? false, audienceStaff: announcement?.audienceStaff ?? false,
      imageFileId: announcement?.imageFileId ?? null,
    },
  })
  const { errors } = form.formState
  const isSpecific = form.watch('isSpecific')
  const imageFileId = form.watch('imageFileId')

  const events = useQuery({
    queryKey: ['/api/eventos', 'opcoes'], enabled: can('events.view'),
    queryFn: () => api.get<Paged<Schema<'EventDto'>>>('/api/eventos', { pageSize: 50 }),
  })
  // Nomes dos destinatários já gravados (a API devolve apenas os identificadores).
  const savedIds = announcement?.targetUserIds ?? []
  const savedUsers = useQuery({
    queryKey: ['usuarios', 'nomes', savedIds], enabled: canPickUsers && savedIds.length > 0,
    queryFn: () => Promise.all(savedIds.map((id) => api.get<Schema<'UserDto'>>(`/api/usuarios/${id}`))),
  })
  const namedTargets = targets.map((t) => ({ id: t.id, name: t.name || savedUsers.data?.find((u) => u.id === t.id)?.name || '' }))

  const save = useMutation({
    mutationFn: ({ values, publish }: { values: Values; publish: boolean }) => {
      const body: Schema<'SaveAnnouncementRequest'> = {
        title: values.title, body: values.body, category: values.category as Schema<'AnnouncementCategory'>, priority: values.priority as Schema<'Priority'>,
        isSpecific: values.isSpecific, audienceMembers: !values.isSpecific && values.audienceMembers, audienceDependents: !values.isSpecific && values.audienceDependents,
        audienceStaff: !values.isSpecific && values.audienceStaff, targetUserIds: values.isSpecific ? targets.map((t) => t.id) : [],
        imageFileId: values.imageFileId, eventId: values.eventId || null, expiresAt: dates.fromInput(values.expiresAt), publish,
      }
      return announcement ? api.put<Announcement>(`/api/comunicados/${announcement.id}`, body) : api.post<Announcement>('/api/comunicados', body)
    },
    onSuccess: (saved, { publish }) => {
      toast.success(published ? 'Comunicado atualizado.' : publish ? 'Comunicado publicado. Os destinatários foram notificados.' : 'Rascunho salvo.')
      void queryClient.invalidateQueries({ queryKey: ['/api/comunicados'] })
      void queryClient.invalidateQueries({ queryKey: ['comunicado', saved.id] })
      onSaved(saved)
    },
    onError: toast.error,
  })

  const submit = (publish: boolean) => form.handleSubmit(async (values) => {
    if (values.isSpecific && targets.length === 0) return setAudienceError('Selecione ao menos um destinatário.')
    if (!values.isSpecific && !(values.audienceMembers || values.audienceDependents || values.audienceStaff)) return setAudienceError('Selecione ao menos um público.')
    setAudienceError(null)
    if (publish && !published) {
      const who = values.isSpecific ? `${targets.length} usuário(s) selecionado(s)`
        : [values.audienceMembers && 'associados', values.audienceDependents && 'dependentes', values.audienceStaff && 'funcionários'].filter(Boolean).join(', ')
      const ok = await confirm({
        title: 'Publicar comunicado?', confirmLabel: 'Publicar',
        message: <>O comunicado ficará visível no mural e uma notificação será enviada para: <strong>{who}</strong>.{!values.isSpecific && !values.audienceDependents && ' Dependentes não verão este comunicado.'}</>,
      })
      if (!ok) return
    }
    save.mutate({ values, publish })
  })()

  const group = 'text-xs font-semibold tracking-wider text-muted uppercase'
  return (
    <Modal open onClose={onClose} size="lg" title={announcement ? 'Editar comunicado' : 'Novo comunicado'}
      description={published ? `Publicado em ${fmt.dateTime(announcement?.publishedAt)}. As alterações não geram nova notificação.` : 'Salve como rascunho ou publique para notificar os destinatários.'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        {published ? <Button variant="primary" loading={save.isPending} onClick={() => submit(true)}>Salvar alterações</Button> : <>
          <Button loading={save.isPending && !save.variables?.publish} disabled={save.isPending} onClick={() => submit(false)}>Salvar rascunho</Button>
          <Button variant="primary" loading={save.isPending && !!save.variables?.publish} disabled={save.isPending} onClick={() => submit(true)}>Publicar</Button>
        </>}
      </>}>
      <form className="space-y-6" noValidate onSubmit={(e) => e.preventDefault()}>
        <fieldset className="space-y-4">
          <legend className={group}>Conteúdo</legend>
          <Field label="Título" required error={errors.title?.message}><Input aria-invalid={!!errors.title} maxLength={150} {...form.register('title')} /></Field>
          <Field label="Texto" required error={errors.body?.message}><Textarea rows={8} aria-invalid={!!errors.body} {...form.register('body')} /></Field>
          <div className="grid grid-cols-3 gap-4">
            <Field label="Categoria"><Select {...form.register('category')}>{options(labels.announcementCategory).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></Field>
            <Field label="Prioridade"><Select {...form.register('priority')}>{options(labels.priority).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></Field>
            <Field label="Expira em" hint="Opcional. Depois desta data sai do mural."><Input type="datetime-local" {...form.register('expiresAt')} /></Field>
          </div>
          <div className="flex items-center gap-4">
            {imageFileId
              ? <FileImage fileId={imageFileId} alt="Imagem do comunicado" className="h-20 w-36 rounded-(--radius-control) border border-line object-cover" />
              : <div className="flex h-20 w-36 items-center justify-center rounded-(--radius-control) border border-dashed border-line text-xs text-muted">Sem imagem</div>}
            <div className="flex items-center gap-2">
              <FileUpload size="sm" label={imageFileId ? 'Trocar imagem' : 'Enviar imagem'} accept="image/jpeg,image/png,image/webp" scope="ClubPublic" onUploaded={(file) => form.setValue('imageFileId', file.id, { shouldDirty: true })} />
              {imageFileId && <Button size="sm" variant="ghost" icon={Trash2} onClick={() => form.setValue('imageFileId', null, { shouldDirty: true })}>Remover</Button>}
            </div>
          </div>
        </fieldset>

        <fieldset className="space-y-3">
          <legend className={group}>Público</legend>
          <div className="inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5">
            {[{ value: false, label: 'Por grupo' }, { value: true, label: 'Público específico' }].map((mode) => (
              <button key={mode.label} type="button" disabled={mode.value && !canPickUsers && !announcement?.isSpecific} onClick={() => { form.setValue('isSpecific', mode.value); setAudienceError(null) }}
                className={cn('rounded-lg px-3 py-1.5 text-[13px] font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50', isSpecific === mode.value ? 'bg-surface text-ink shadow-sm' : 'text-muted hover:text-ink')}>
                {mode.label}
              </button>
            ))}
          </div>
          {!canPickUsers && <p className="text-xs text-muted">A seleção de destinatários específicos exige a permissão de visualizar usuários.</p>}
          {!isSpecific ? (
            <>
              <div className="flex flex-wrap gap-x-6 gap-y-2">
                <Checkbox label="Associados" {...form.register('audienceMembers')} />
                <Checkbox label="Dependentes" {...form.register('audienceDependents')} />
                <Checkbox label="Funcionários" {...form.register('audienceStaff')} />
              </div>
              <Alert tone="info">Dependentes <strong>somente</strong> veem comunicados em que "Dependentes" estiver marcado — nunca os comunicados destinados apenas aos associados.</Alert>
            </>
          ) : canPickUsers ? (
            <>
              <UserPicker value={namedTargets} onChange={(value) => { setTargets(value); setAudienceError(null) }} />
              <p className="text-xs text-muted">Somente os usuários selecionados verão e serão notificados deste comunicado.</p>
            </>
          ) : <p className="text-sm text-ink">{targets.length} destinatário(s) definido(s). Eles serão mantidos ao salvar.</p>}
          {audienceError && <p className="text-xs text-danger">{audienceError}</p>}
        </fieldset>

        {can('events.view') && (
          <Field label="Evento vinculado" hint="Opcional. O aplicativo exibe um atalho para o evento.">
            <Select {...form.register('eventId')} value={form.watch('eventId')}>
              <option value="">Nenhum</option>
              {(events.data?.items ?? []).map((event) => <option key={event.id} value={event.id}>{event.title} — {fmt.date(event.startAt)}</option>)}
            </Select>
          </Field>
        )}
      </form>
    </Modal>
  )
}
