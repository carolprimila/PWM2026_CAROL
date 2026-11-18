import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Button, Field, Input, Modal, MoneyInput, Select, Textarea, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Maintenance = Schema<'MaintenanceDto'>

const schema = z.object({
  title: z.string().trim().min(3, 'Descreva o problema (mínimo de 3 caracteres)').max(150, 'Máximo de 150 caracteres'),
  description: z.string().max(4000, 'Máximo de 4.000 caracteres'),
  kind: z.string(),
  spaceId: z.string(),
  assetId: z.string(),
  responsibleEmployeeId: z.string(),
  responsibleName: z.string().max(150, 'Máximo de 150 caracteres'),
  dueDate: z.string(),
  estimatedCost: z.number().min(0, 'Valor inválido').nullable(),
}).superRefine((v, ctx) => {
  if (!v.spaceId && !v.assetId) ctx.addIssue({ code: 'custom', path: ['spaceId'], message: 'Informe o espaço ou o bem afetado' })
})
type Values = z.infer<typeof schema>

export function MaintenanceForm({ maintenance, onClose, onSaved }: { maintenance?: Maintenance; onClose: () => void; onSaved: (saved: Maintenance) => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const spaces = useQuery({ queryKey: ['/api/espacos'], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })
  const assets = useQuery({ queryKey: ['/api/patrimonio', 'opcoes'], enabled: can('assets.view'), queryFn: () => api.get<Paged<Schema<'AssetDto'>>>('/api/patrimonio', { pageSize: 200 }) })
  const employees = useQuery({ queryKey: ['/api/funcionarios', 'opcoes'], enabled: can('employees.view'), queryFn: () => api.get<Paged<Schema<'EmployeeDto'>>>('/api/funcionarios', { pageSize: 100 }) })
  // Com funcionário vinculado, a API devolve o nome dele em responsibleName; o texto livre só vale sem vínculo.
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: maintenance?.title ?? '', description: maintenance?.description ?? '', kind: maintenance?.kind ?? 'Corrective', spaceId: maintenance?.spaceId ?? '', assetId: maintenance?.assetId ?? '',
      responsibleEmployeeId: maintenance?.responsibleEmployeeId ?? '', responsibleName: maintenance?.responsibleEmployeeId ? '' : maintenance?.responsibleName ?? '',
      dueDate: maintenance?.dueDate ?? '', estimatedCost: maintenance?.estimatedCost ?? null,
    },
  })
  const { errors } = form.formState
  const employeeId = form.watch('responsibleEmployeeId')

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body: Schema<'SaveMaintenanceRequest'> = {
        title: values.title, description: emptyToNull(values.description), kind: values.kind as Schema<'MaintenanceKind'>, spaceId: values.spaceId || null, assetId: values.assetId || null,
        responsibleEmployeeId: values.responsibleEmployeeId || null, responsibleName: values.responsibleEmployeeId ? null : emptyToNull(values.responsibleName),
        dueDate: values.dueDate || null, estimatedCost: values.estimatedCost,
      }
      return maintenance ? api.put<Maintenance>(`/api/manutencoes/${maintenance.id}`, body) : api.post<Maintenance>('/api/manutencoes', body)
    },
    onSuccess: (saved) => {
      toast.success(maintenance ? 'Manutenção atualizada.' : `Solicitação ${saved.code} aberta.`)
      void queryClient.invalidateQueries({ queryKey: ['/api/manutencoes'] })
      void queryClient.invalidateQueries({ queryKey: ['manutencao', saved.id] })
      onSaved(saved)
    },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} size="lg" title={maintenance ? `Editar ${maintenance.code}` : 'Nova manutenção'}
      description={maintenance ? undefined : 'A solicitação entra como "Solicitada" e a equipe de manutenção é notificada.'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{maintenance ? 'Salvar alterações' : 'Abrir solicitação'}</Button>
      </>}>
      <form className="grid grid-cols-6 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-4" label="Problema" required error={errors.title?.message}><Input aria-invalid={!!errors.title} maxLength={150} placeholder="Ex.: Vazamento no vestiário masculino" {...form.register('title')} /></Field>
        <Field className="col-span-2" label="Tipo"><Select {...form.register('kind')}>{options(labels.maintenanceKind).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></Field>
        <Field className="col-span-6" label="Descrição" error={errors.description?.message}><Textarea rows={3} {...form.register('description')} /></Field>
        <Field className="col-span-3" label="Espaço" error={errors.spaceId?.message} hint="Informe o espaço, o bem ou ambos.">
          <Select aria-invalid={!!errors.spaceId} {...form.register('spaceId')} value={form.watch('spaceId')}><option value="">Nenhum</option>{(spaces.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
        </Field>
        <Field className="col-span-3" label="Bem patrimonial" hint={can('assets.view') ? undefined : 'Requer a permissão de visualizar o patrimônio.'}>
          <Select disabled={!can('assets.view')} {...form.register('assetId')} value={form.watch('assetId')}>
            <option value="">Nenhum</option>
            {(assets.data?.items ?? []).map((a) => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
          </Select>
        </Field>
        {can('employees.view') && (
          <Field className="col-span-3" label="Responsável (funcionário)">
            <Select {...form.register('responsibleEmployeeId')} value={employeeId}>
              <option value="">Nenhum / terceiro</option>
              {(employees.data?.items ?? []).filter((e) => e.status === 'Active' || e.id === maintenance?.responsibleEmployeeId).map((e) => <option key={e.id} value={e.id}>{e.fullName}</option>)}
            </Select>
          </Field>
        )}
        <Field className="col-span-3" label={can('employees.view') ? 'Ou nome do responsável' : 'Responsável'} error={errors.responsibleName?.message} hint="Prestador externo ou empresa.">
          <Input maxLength={150} disabled={!!employeeId} placeholder={employeeId ? 'Funcionário selecionado' : 'Ex.: Hidráulica Silva'} {...form.register('responsibleName')} />
        </Field>
        <Field className="col-span-3" label="Prazo"><Input type="date" {...form.register('dueDate')} /></Field>
        <Field className="col-span-3" label="Custo estimado" error={errors.estimatedCost?.message}>
          <Controller control={form.control} name="estimatedCost" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} />} />
        </Field>
      </form>
    </Modal>
  )
}
