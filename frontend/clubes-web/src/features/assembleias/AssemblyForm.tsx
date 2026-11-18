import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, Field, Input, Modal, Textarea, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { dates, emptyToNull } from '@/lib/format'

type Assembly = Schema<'AssemblyDto'>

const schema = z.object({
  title: z.string().trim().min(3, 'Informe o título (mínimo de 3 caracteres)').max(150, 'Máximo de 150 caracteres'),
  scheduledAt: z.string().min(1, 'Informe a data e a hora'),
  location: z.string().max(200, 'Máximo de 200 caracteres'),
  agenda: z.string().max(8000, 'Máximo de 8.000 caracteres'),
})
type Values = z.infer<typeof schema>

export function AssemblyForm({ assembly, onClose, onSaved }: { assembly?: Assembly; onClose: () => void; onSaved: (saved: Assembly) => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { title: assembly?.title ?? '', scheduledAt: dates.toInput(assembly?.scheduledAt), location: assembly?.location ?? '', agenda: assembly?.agenda ?? '' },
  })
  const { errors } = form.formState
  const save = useMutation({
    mutationFn: (values: Values) => {
      const body: Schema<'SaveAssemblyRequest'> = { title: values.title, scheduledAt: dates.fromInput(values.scheduledAt)!, location: emptyToNull(values.location), agenda: emptyToNull(values.agenda) }
      return assembly ? api.put<Assembly>(`/api/assembleias/${assembly.id}`, body) : api.post<Assembly>('/api/assembleias', body)
    },
    onSuccess: (saved) => {
      toast.success(assembly ? 'Assembleia atualizada.' : 'Assembleia criada. Os associados foram notificados da convocação.')
      void queryClient.invalidateQueries({ queryKey: ['/api/assembleias'] })
      void queryClient.invalidateQueries({ queryKey: ['assembleia', saved.id] })
      onSaved(saved)
    },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} title={assembly ? 'Editar assembleia' : 'Nova assembleia'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{assembly ? 'Salvar alterações' : 'Criar e convocar'}</Button>
      </>}>
      <form className="space-y-4" noValidate onSubmit={(e) => e.preventDefault()}>
        {!assembly && <Alert tone="info">Ao criar a assembleia, todos os associados recebem a notificação de convocação. Dependentes não participam.</Alert>}
        <Field label="Título" required error={errors.title?.message}><Input aria-invalid={!!errors.title} maxLength={150} placeholder="Ex.: Assembleia Geral Ordinária" {...form.register('title')} /></Field>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Data e hora" required error={errors.scheduledAt?.message}><Input type="datetime-local" aria-invalid={!!errors.scheduledAt} {...form.register('scheduledAt')} /></Field>
          <Field label="Local" error={errors.location?.message}><Input placeholder="Ex.: Salão social" {...form.register('location')} /></Field>
        </div>
        <Field label="Pauta" error={errors.agenda?.message} hint="Itens que serão discutidos. Visível aos associados no aplicativo."><Textarea rows={7} {...form.register('agenda')} /></Field>
      </form>
    </Modal>
  )
}
