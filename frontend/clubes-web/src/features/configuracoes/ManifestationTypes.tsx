import { zodResolver } from '@hookform/resolvers/zod'
import { MessageSquareText, Pencil, Plus } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { DataTable, type Column } from '@/components/table'
import { Alert, Badge, Button, Card, CardTitle, Field, IconButton, Input, Modal, Switch, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'

type ManifestationType = Schema<'ManifestationTypeDto'>
const KEY = ['/api/manifestacoes/tipos']

const schema = z.object({
  name: z.string().trim().min(2, 'Informe o nome (mínimo de 2 caracteres)').max(60, 'Máximo de 60 caracteres'),
  isSuggestion: z.boolean(),
  isActive: z.boolean(),
})
type Values = z.infer<typeof schema>

/** Tipos de manifestação (reclamação, sugestão…) que associados e dependentes escolhem ao abrir um atendimento. Salvos na hora, fora da barra de políticas. */
export function ManifestationTypes() {
  const { can } = useAuth()
  const manage = can('settings.manage', 'manifestations.manage')
  const [editing, setEditing] = useState<ManifestationType | 'new' | null>(null)
  const types = useQuery({ queryKey: [...KEY, 'todos'], queryFn: () => api.get<ManifestationType[]>('/api/manifestacoes/tipos', { includeInactive: true }) })

  const columns: Column<ManifestationType>[] = [
    { key: 'name', header: 'Tipo', render: (t) => <span className="font-medium text-ink">{t.name}</span> },
    { key: 'kind', header: 'Tratamento', render: (t) => (t.isSuggestion ? <Badge tone="info">Sugestão</Badge> : <Badge>Atendimento</Badge>) },
    { key: 'status', header: 'Situação', render: (t) => (t.isActive ? <Badge tone="success">Ativo</Badge> : <Badge>Inativo</Badge>) },
  ]
  return (
    <Card padded={false}>
      <div className="px-5 pt-5">
        <CardTitle icon={MessageSquareText} title="Tipos de manifestação" description="Opções exibidas ao abrir uma manifestação no aplicativo. Tipos inativos deixam de ser oferecidos, sem afetar manifestações já registradas."
          actions={manage && <Button size="sm" icon={Plus} onClick={() => setEditing('new')}>Novo tipo</Button>} />
      </div>
      <div className="border-t border-line">
        <DataTable columns={columns} rows={types.data ?? []} rowKey={(t) => t.id} isLoading={types.isLoading} error={types.error} onRetry={() => types.refetch()}
          onRowClick={manage ? (t) => setEditing(t) : undefined} empty={{ title: 'Nenhum tipo de manifestação', description: manage ? 'Cadastre ao menos um tipo para que os associados possam enviar manifestações.' : undefined }}
          rowActions={manage ? (t) => <IconButton icon={Pencil} label="Editar" onClick={() => setEditing(t)} /> : undefined} />
      </div>
      {editing && <TypeForm type={editing === 'new' ? undefined : editing} onClose={() => setEditing(null)} />}
    </Card>
  )
}

function TypeForm({ type, onClose }: { type?: ManifestationType; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { name: type?.name ?? '', isSuggestion: type?.isSuggestion ?? false, isActive: type?.isActive ?? true } })
  const save = useMutation({
    mutationFn: (values: Values) => (type ? api.put<ManifestationType>(`/api/manifestacoes/tipos/${type.id}`, values) : api.post<ManifestationType>('/api/manifestacoes/tipos', values)),
    onSuccess: () => { toast.success(type ? 'Tipo atualizado.' : 'Tipo criado.'); void queryClient.invalidateQueries({ queryKey: KEY }); onClose() },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
    },
  })
  return (
    <Modal open onClose={onClose} size="sm" title={type ? 'Editar tipo de manifestação' : 'Novo tipo de manifestação'}
      footer={<><Button onClick={onClose}>Cancelar</Button><Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{type ? 'Salvar alterações' : 'Criar tipo'}</Button></>}>
      <form className="space-y-5" noValidate onSubmit={(e) => e.preventDefault()}>
        {save.error && !(save.error instanceof ApiError && save.error.errors) ? <Alert tone="danger">{(save.error as Error).message}</Alert> : null}
        <Field label="Nome" required error={form.formState.errors.name?.message}><Input autoFocus placeholder="Ex.: Reclamação" aria-invalid={!!form.formState.errors.name} {...form.register('name')} /></Field>
        <Controller control={form.control} name="isSuggestion" render={({ field }) => <Switch checked={field.value} onChange={field.onChange} label="É sugestão" description="Identifica o tipo como sugestão de melhoria; os demais são tratados como atendimento." />} />
        <Controller control={form.control} name="isActive" render={({ field }) => <Switch checked={field.value} onChange={field.onChange} label="Ativo" description="Disponível para novas manifestações." />} />
      </form>
    </Modal>
  )
}
