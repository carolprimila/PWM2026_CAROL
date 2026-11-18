import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Button, Field, Input, Modal, MoneyInput, Select, Textarea, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Asset = Schema<'AssetDto'>

const schema = z.object({
  code: z.string().trim().min(1, 'Informe a identificação').max(40, 'Máximo de 40 caracteres'),
  name: z.string().trim().min(2, 'Informe o nome do bem').max(150, 'Máximo de 150 caracteres'),
  category: z.string().max(80, 'Máximo de 80 caracteres'),
  description: z.string().max(2000, 'Máximo de 2.000 caracteres'),
  location: z.string().max(150, 'Máximo de 150 caracteres'),
  spaceId: z.string(),
  condition: z.string(),
  acquiredOn: z.string(),
  value: z.number().min(0, 'Valor inválido').nullable(),
  responsibleEmployeeId: z.string(),
})
type Values = z.infer<typeof schema>

export function AssetForm({ asset, categories, onClose, onSaved }: { asset?: Asset; categories: string[]; onClose: () => void; onSaved: (saved: Asset) => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const spaces = useQuery({ queryKey: ['/api/espacos'], queryFn: () => api.get<Schema<'SpaceDto'>[]>('/api/espacos') })
  const employees = useQuery({
    queryKey: ['/api/funcionarios', 'opcoes'], enabled: can('employees.view'),
    queryFn: () => api.get<Paged<Schema<'EmployeeDto'>>>('/api/funcionarios', { pageSize: 100 }),
  })
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      code: asset?.code ?? '', name: asset?.name ?? '', category: asset?.category ?? '', description: asset?.description ?? '', location: asset?.location ?? '', spaceId: asset?.spaceId ?? '',
      condition: asset?.condition ?? 'Good', acquiredOn: asset?.acquiredOn ?? '', value: asset?.value ?? null, responsibleEmployeeId: asset?.responsibleEmployeeId ?? '',
    },
  })
  const { errors } = form.formState

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body: Schema<'SaveAssetRequest'> = {
        code: values.code, name: values.name, category: emptyToNull(values.category), description: emptyToNull(values.description), location: emptyToNull(values.location),
        spaceId: values.spaceId || null, condition: values.condition as Schema<'AssetCondition'>, acquiredOn: values.acquiredOn || null, value: values.value,
        responsibleEmployeeId: values.responsibleEmployeeId || null,
      }
      return asset ? api.put<Asset>(`/api/patrimonio/${asset.id}`, body) : api.post<Asset>('/api/patrimonio', body)
    },
    onSuccess: (saved) => {
      toast.success(asset ? 'Bem atualizado.' : 'Bem cadastrado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/patrimonio'] })
      void queryClient.invalidateQueries({ queryKey: ['patrimonio', saved.id] })
      void queryClient.invalidateQueries({ queryKey: ['historico', 'Asset', saved.id] })
      onSaved(saved)
    },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} size="lg" title={asset ? 'Editar bem' : 'Novo bem patrimonial'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{asset ? 'Salvar alterações' : 'Cadastrar'}</Button>
      </>}>
      <form className="grid grid-cols-6 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-2" label="Identificação" required error={errors.code?.message} hint="Código ou plaqueta do bem."><Input aria-invalid={!!errors.code} maxLength={40} placeholder="Ex.: PAT-0001" {...form.register('code')} /></Field>
        <Field className="col-span-4" label="Nome" required error={errors.name?.message}><Input aria-invalid={!!errors.name} maxLength={150} {...form.register('name')} /></Field>
        <Field className="col-span-3" label="Categoria" error={errors.category?.message}>
          <Input list="asset-categories" maxLength={80} placeholder="Ex.: Móveis, Equipamentos" {...form.register('category')} />
          <datalist id="asset-categories">{categories.map((c) => <option key={c} value={c} />)}</datalist>
        </Field>
        <Field className="col-span-3" label="Estado de conservação">
          <Select {...form.register('condition')}>{options(labels.assetCondition).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
        </Field>
        <Field className="col-span-6" label="Descrição" error={errors.description?.message}><Textarea rows={3} {...form.register('description')} /></Field>
        <Field className="col-span-3" label="Espaço">
          <Select {...form.register('spaceId')} value={form.watch('spaceId')}><option value="">Nenhum</option>{(spaces.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</Select>
        </Field>
        <Field className="col-span-3" label="Localização" error={errors.location?.message}><Input maxLength={150} placeholder="Ex.: Depósito, sala 2" {...form.register('location')} /></Field>
        <Field className="col-span-2" label="Data de aquisição"><Input type="date" {...form.register('acquiredOn')} /></Field>
        <Field className="col-span-2" label="Valor" error={errors.value?.message}>
          <Controller control={form.control} name="value" render={({ field }) => <MoneyInput value={field.value} onChange={field.onChange} />} />
        </Field>
        {can('employees.view') && (
          <Field className="col-span-2" label="Responsável">
            <Select {...form.register('responsibleEmployeeId')} value={form.watch('responsibleEmployeeId')}>
              <option value="">Sem responsável</option>
              {(employees.data?.items ?? []).filter((e) => e.status === 'Active' || e.id === asset?.responsibleEmployeeId).map((e) => <option key={e.id} value={e.id}>{e.fullName}</option>)}
            </Select>
          </Field>
        )}
      </form>
    </Modal>
  )
}
