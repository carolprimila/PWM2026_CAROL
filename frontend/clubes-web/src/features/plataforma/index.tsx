import { zodResolver } from '@hookform/resolvers/zod'
import { Building, LogIn, Pencil, Plus, Power, RotateCcw } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm, type UseFormReturn } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import type { Feature } from '@/app/features'
import { DataTable, type Column } from '@/components/table'
import { Alert, Badge, Button, Card, Field, IconButton, Input, Modal, PageHeader, Switch, useConfirm, useToast } from '@/components/ui'
import { TemporaryPasswordModal } from '@/features/associados/MemberDetail'
import { clubBody, clubDefaults, ClubFields, clubSchema, type ClubValues } from '@/features/clube/ClubFields'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'

type Club = Schema<'ClubDto'>
const KEY = ['plataforma', 'clubes'] // mesma chave do seletor de clubes da barra superior

function ClubsPage() {
  const { clubId, selectClub } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [creating, setCreating] = useState(false)
  const [editing, setEditing] = useState<Club | null>(null)
  const [admin, setAdmin] = useState<Schema<'TemporaryPasswordDto'> | null>(null)
  const clubs = useQuery({ queryKey: KEY, queryFn: () => api.get<Club[]>('/api/clubes') })
  const setActive = useMutation({
    mutationFn: ({ club, active }: { club: Club; active: boolean }) => api.post(`/api/clubes/${club.id}/${active ? 'ativar' : 'desativar'}`),
    onSuccess: (_, { active }) => { toast.success(active ? 'Clube ativado.' : 'Clube desativado.'); void queryClient.invalidateQueries({ queryKey: KEY }) },
    onError: toast.error,
  })
  const toggle = async (club: Club) => {
    const active = !club.isActive
    const ok = await confirm({
      title: active ? 'Ativar clube?' : 'Desativar clube?', danger: !active, confirmLabel: active ? 'Ativar' : 'Desativar',
      message: active ? `Os usuários de ${club.name} voltam a conseguir entrar.` : `Os usuários de ${club.name} deixam de conseguir entrar no painel e no aplicativo. Nenhum dado é excluído.`,
    })
    if (ok) setActive.mutate({ club, active })
  }

  const columns: Column<Club>[] = [
    { key: 'name', header: 'Clube', render: (c) => <div className="min-w-0"><p className="flex items-center gap-2 font-medium text-ink">{c.name}{c.id === clubId && <Badge tone="brand">Em operação</Badge>}</p><p className="truncate font-mono text-xs text-muted">{c.slug}</p></div> },
    { key: 'cnpj', header: 'CNPJ', render: (c) => <span className="tabular text-muted">{fmt.cnpj(c.cnpj)}</span> },
    { key: 'city', header: 'Cidade', render: (c) => (c.city ? `${c.city}${c.state ? `/${c.state}` : ''}` : <span className="text-muted">—</span>) },
    { key: 'zone', header: 'Fuso horário', render: (c) => <span className="text-muted">{c.timeZoneId}</span> },
    { key: 'created', header: 'Cadastro', render: (c) => fmt.date(c.createdAt) },
    { key: 'status', header: 'Situação', render: (c) => (c.isActive ? <Badge tone="success">Ativo</Badge> : <Badge>Inativo</Badge>) },
  ]

  return (
    <>
      <PageHeader title="Clubes da plataforma" subtitle="Cadastro e situação dos clubes atendidos."
        actions={<Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo clube</Button>} />
      <Alert tone="info" className="mb-6" title="Como operar um clube">
        Use o seletor de clubes na barra superior para escolher o clube em que deseja trabalhar. Com um clube selecionado, o menu passa a exibir os módulos daquele clube e todas as operações são feitas nele; sem seleção, apenas esta área da plataforma fica disponível.
      </Alert>
      <Card padded={false}>
        <DataTable columns={columns} rows={clubs.data ?? []} rowKey={(c) => c.id} isLoading={clubs.isLoading} error={clubs.error} onRetry={() => clubs.refetch()}
          onRowClick={(c) => setEditing(c)}
          empty={{ title: 'Nenhum clube cadastrado', description: 'Cadastre o primeiro clube da plataforma.', action: <Button variant="primary" size="sm" icon={Plus} onClick={() => setCreating(true)}>Novo clube</Button> }}
          rowActions={(c) => (
            <div className="flex justify-end">
              {c.isActive && c.id !== clubId && <IconButton icon={LogIn} label="Operar este clube" onClick={() => { selectClub(c.id); toast.info(`Operando em ${c.name}.`) }} />}
              <IconButton icon={Pencil} label="Editar" onClick={() => setEditing(c)} />
              <IconButton icon={c.isActive ? Power : RotateCcw} label={c.isActive ? 'Desativar' : 'Ativar'} disabled={setActive.isPending} onClick={() => toggle(c)} />
            </div>
          )} />
      </Card>
      {creating && <CreateClub onClose={() => setCreating(false)} onCreated={(result) => { setCreating(false); setAdmin(result.admin) }} />}
      {editing && <EditClub club={editing} onClose={() => setEditing(null)} />}
      <TemporaryPasswordModal access={admin} onClose={() => setAdmin(null)} />
    </>
  )
}

const createSchema = clubSchema.extend({
  adminName: z.string().trim().min(3, 'Informe o nome do administrador').max(150, 'Máximo de 150 caracteres'),
  adminEmail: z.string().trim().email('E-mail inválido'),
  applyStandardStructure: z.boolean(),
})
type CreateValues = z.infer<typeof createSchema>

/** Erros de validação do servidor chegam como "Club.Name" / "AdminEmail": o último segmento identifica o campo. */
function serverField(key: string) {
  const name = key.split('.').pop() ?? key
  return name.charAt(0).toLowerCase() + name.slice(1)
}

function CreateClub({ onClose, onCreated }: { onClose: () => void; onCreated: (result: Schema<'CreateClubResult'>) => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<CreateValues>({ resolver: zodResolver(createSchema), defaultValues: { ...clubDefaults(), adminName: '', adminEmail: '', applyStandardStructure: true } })
  const { errors } = form.formState
  const create = useMutation({
    mutationFn: (values: CreateValues) => api.post<Schema<'CreateClubResult'>>('/api/clubes', {
      club: clubBody(values), adminName: values.adminName, adminEmail: values.adminEmail, applyStandardStructure: values.applyStandardStructure,
    }),
    onSuccess: (result) => { toast.success(`Clube ${result.club.name} cadastrado.`); void queryClient.invalidateQueries({ queryKey: KEY }); onCreated(result) },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError(serverField(field) as keyof CreateValues, { message: messages[0] })
    },
  })
  return (
    <Modal open onClose={onClose} size="lg" title="Novo clube" description="O clube nasce com configurações em branco, funções padrão e um administrador com senha temporária."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={create.isPending} onClick={form.handleSubmit((values) => create.mutate(values))}>Cadastrar clube</Button>
      </>}>
      <form className="space-y-6" noValidate onSubmit={(e) => e.preventDefault()}>
        {create.error && !(create.error instanceof ApiError && create.error.errors) ? <Alert tone="danger">{(create.error as Error).message}</Alert> : null}
        <ClubFields form={form as unknown as UseFormReturn<ClubValues>} />
        <fieldset className="space-y-4">
          <legend className="text-xs font-semibold tracking-wider text-muted uppercase">Administrador do clube</legend>
          <div className="grid grid-cols-2 gap-4">
            <Field label="Nome" required error={errors.adminName?.message}><Input aria-invalid={!!errors.adminName} {...form.register('adminName')} /></Field>
            <Field label="E-mail (login)" required error={errors.adminEmail?.message}><Input type="email" aria-invalid={!!errors.adminEmail} {...form.register('adminEmail')} /></Field>
          </div>
          <Controller control={form.control} name="applyStandardStructure" render={({ field }) => (
            <Switch checked={field.value} onChange={field.onChange} label="Criar a estrutura física padrão"
              description="4 quadras, 10 quiosques (5 fechados e 5 abertos), salão de festas e piscinas. Tudo pode ser ajustado depois em Espaços." />
          )} />
        </fieldset>
      </form>
    </Modal>
  )
}

function EditClub({ club, onClose }: { club: Club; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<ClubValues>({ resolver: zodResolver(clubSchema), defaultValues: clubDefaults(club) })
  const save = useMutation({
    mutationFn: (values: ClubValues) => api.put<Club>(`/api/clubes/${club.id}`, clubBody(values)),
    onSuccess: () => {
      toast.success('Clube atualizado.')
      void queryClient.invalidateQueries({ queryKey: KEY })
      void queryClient.invalidateQueries({ queryKey: ['clube-atual'] })
      onClose()
    },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError(serverField(field) as keyof ClubValues, { message: messages[0] })
    },
  })
  return (
    <Modal open onClose={onClose} size="lg" title="Editar clube" description={<>Identificador: <span className="font-mono">{club.slug}</span> · o logotipo é enviado em Administração → Dados do clube, com o clube selecionado.</>}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>Salvar alterações</Button>
      </>}>
      <form noValidate onSubmit={(e) => e.preventDefault()}>
        {save.error && !(save.error instanceof ApiError && save.error.errors) ? <Alert tone="danger" className="mb-5">{(save.error as Error).message}</Alert> : null}
        <ClubFields form={form} />
      </form>
    </Modal>
  )
}

const feature: Feature = {
  nav: [{ to: '/plataforma/clubes', label: 'Clubes', icon: Building, group: 'Plataforma', order: 1, kinds: ['SuperAdmin'] }],
  routes: [{ path: 'plataforma/clubes', component: ClubsPage, kinds: ['SuperAdmin'] }],
}
export default feature
