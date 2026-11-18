import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { MemberPicker } from '@/components/shared'
import { Button, Field, Input, Modal, Select, Textarea, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Occurrence = Schema<'OccurrenceDto'>

const schema = z.object({
  kind: z.string(),
  description: z.string().trim().min(5, 'Descreva a ocorrência (mínimo de 5 caracteres)').max(4000, 'Máximo de 4.000 caracteres'),
  spaceId: z.string(),
  reservationId: z.string(),
  occurredAt: z.string().refine((v) => !v || new Date(v) <= new Date(), 'A data não pode ser futura'),
})
type Values = z.infer<typeof schema>

export function OccurrenceForm({ onClose, onSaved }: { onClose: () => void; onSaved: (saved: Occurrence) => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [member, setMember] = useState<{ id: string; name: string } | null>(null)
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { kind: 'Damage', description: '', spaceId: '', reservationId: '', occurredAt: dates.toInput(new Date().toISOString()) },
  })
  const { errors } = form.formState
  const spaceId = form.watch('spaceId')
  const spaces = useQuery({ queryKey: ['/api/espacos'], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })
  const reservations = useQuery({
    queryKey: ['/api/reservas', 'ocorrencia', spaceId], enabled: can('reservations.view'),
    queryFn: () => api.get<Paged<Schema<'ReservationDto'>>>('/api/reservas', { spaceId: spaceId || undefined, pageSize: 20 }),
  })

  const save = useMutation({
    mutationFn: (values: Values) => api.post<Occurrence>('/api/ocorrencias', {
      kind: values.kind as Schema<'OccurrenceKind'>, description: values.description, spaceId: values.spaceId || null, reservationId: values.reservationId || null,
      memberId: member?.id ?? null, occurredAt: dates.fromInput(values.occurredAt),
    } satisfies Schema<'CreateOccurrenceRequest'>),
    onSuccess: (saved) => {
      toast.success(`Ocorrência ${saved.code} registrada.`)
      void queryClient.invalidateQueries({ queryKey: ['/api/ocorrencias'] })
      onSaved(saved)
    },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} size="lg" title="Nova ocorrência"
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>Registrar ocorrência</Button>
      </>}>
      <form className="grid grid-cols-6 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-3" label="Tipo"><Select {...form.register('kind')}>{options(labels.occurrenceKind).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></Field>
        <Field className="col-span-3" label="Data e hora" error={errors.occurredAt?.message} hint="Em branco: momento do registro."><Input type="datetime-local" aria-invalid={!!errors.occurredAt} {...form.register('occurredAt')} /></Field>
        <Field className="col-span-6" label="Descrição" required error={errors.description?.message}><Textarea rows={4} aria-invalid={!!errors.description} placeholder="O que aconteceu, onde e quem estava envolvido." {...form.register('description')} /></Field>
        <Field className="col-span-3" label="Espaço">
          <Select {...form.register('spaceId', { onChange: () => form.setValue('reservationId', '') })} value={spaceId}><option value="">Nenhum</option>{(spaces.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
        </Field>
        <Field className="col-span-3" label="Reserva" hint={can('reservations.view') ? (spaceId ? 'Reservas recentes do espaço selecionado.' : 'Reservas recentes do clube.') : 'Requer a permissão de visualizar reservas.'}>
          <Select disabled={!can('reservations.view')} {...form.register('reservationId')} value={form.watch('reservationId')}>
            <option value="">{reservations.isLoading ? 'Carregando…' : 'Nenhuma'}</option>
            {(reservations.data?.items ?? []).map((r) => <option key={r.id} value={r.id}>{r.code} — {r.memberName ?? r.title ?? labels.reservationKind[r.kind].label} — {fmt.dateTime(r.startAt)}</option>)}
          </Select>
        </Field>
        <Field className="col-span-6" label="Associado envolvido" hint="Opcional. Se a reserva for informada e este campo ficar vazio, o associado da reserva é vinculado automaticamente.">
          <MemberPicker value={member} onChange={setMember} onlyActive={false} />
        </Field>
      </form>
    </Modal>
  )
}
