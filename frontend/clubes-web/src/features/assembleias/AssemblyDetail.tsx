import { ArrowLeft, Ban, CheckCircle2, Download, FileText, Pencil, Play, Trash2, UserCheck } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Attachments, HistoryList, MemberPicker } from '@/components/shared'
import { Alert, Button, Card, CardTitle, EmptyState, ErrorState, Field, FileUpload, IconButton, Info, LoadingState, PageHeader, QueryBoundary, StatusBadge, Tabs, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { AssemblyForm } from './AssemblyForm'
import { PollsPanel } from './Polls'

type Assembly = Schema<'AssemblyDto'>
type Attendance = Schema<'AttendanceDto'>
type Tab = 'pauta' | 'presenca' | 'votacoes' | 'ata' | 'historico'

export default function AssemblyDetail() {
  const { id } = useParams<{ id: string }>()
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [tab, setTab] = useState<Tab>('pauta')
  const [editing, setEditing] = useState(false)
  const assembly = useQuery({ queryKey: ['assembleia', id], queryFn: () => api.get<Assembly>(`/api/assembleias/${id}`) })
  const setStatus = useMutation({
    mutationFn: ({ status }: { status: Schema<'AssemblyStatus'>; message: string }) => api.post<Assembly>(`/api/assembleias/${id}/situacao`, { status }),
    onSuccess: (_, variables) => {
      toast.success(variables.message)
      void queryClient.invalidateQueries({ queryKey: ['assembleia', id] })
      void queryClient.invalidateQueries({ queryKey: ['/api/assembleias'] })
      void queryClient.invalidateQueries({ queryKey: ['historico', 'Assembly', id] })
    },
    onError: toast.error,
  })

  return (
    <QueryBoundary query={assembly}>
      {(a) => {
        const manage = can('assemblies.manage')
        const closed = a.status === 'Finished' || a.status === 'Cancelled'
        return (
          <>
            <PageHeader
              back={<Link to="/assembleias" className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-muted hover:text-ink"><ArrowLeft className="size-3.5" />Assembleias</Link>}
              title={a.title}
              subtitle={<span className="flex flex-wrap items-center gap-2"><StatusBadge dict={labels.assemblyStatus} value={a.status} />{fmt.dateTime(a.scheduledAt)}{a.location ? ` · ${a.location}` : ''}</span>}
              actions={manage && !closed && <>
                <Button icon={Pencil} onClick={() => setEditing(true)}>Editar</Button>
                {a.status === 'Scheduled' && <>
                  <Button icon={Ban} disabled={setStatus.isPending} onClick={async () => {
                    if (await confirm({ title: 'Cancelar assembleia?', message: 'Todos os associados serão notificados do cancelamento. Esta ação não pode ser desfeita.', danger: true, confirmLabel: 'Cancelar assembleia' }))
                      setStatus.mutate({ status: 'Cancelled', message: 'Assembleia cancelada. Os associados foram notificados.' })
                  }}>Cancelar</Button>
                  <Button variant="primary" icon={Play} disabled={setStatus.isPending} onClick={async () => {
                    if (await confirm({ title: 'Iniciar assembleia?', message: 'A situação passa para "Em andamento". Registre a presença dos associados e conduza as votações.', confirmLabel: 'Iniciar' }))
                      setStatus.mutate({ status: 'InProgress', message: 'Assembleia iniciada.' })
                  }}>Iniciar assembleia</Button>
                </>}
                {a.status === 'InProgress' && (
                  <Button variant="primary" icon={CheckCircle2} disabled={setStatus.isPending} onClick={async () => {
                    if (await confirm({ title: 'Encerrar assembleia?', message: 'As votações abertas precisam ser encerradas antes. Depois de encerrada, a assembleia e a lista de presença não podem mais ser alteradas; a ata continua editável.', confirmLabel: 'Encerrar' }))
                      setStatus.mutate({ status: 'Finished', message: 'Assembleia encerrada.' })
                  }}>Encerrar assembleia</Button>
                )}
              </>}
            />
            <Tabs<Tab> value={tab} onChange={setTab} tabs={[
              { key: 'pauta', label: 'Pauta e documentos' }, { key: 'presenca', label: 'Presença', count: a.attendances }, { key: 'votacoes', label: 'Votações', count: a.polls },
              { key: 'ata', label: 'Ata' }, { key: 'historico', label: 'Histórico' },
            ]} />

            {tab === 'pauta' && (
              <div className="grid gap-6 xl:grid-cols-2">
                <Card>
                  <CardTitle title="Pauta" />
                  {a.agenda ? <p className="text-sm leading-relaxed whitespace-pre-wrap text-ink">{a.agenda}</p> : <p className="text-sm text-muted">Nenhuma pauta informada.</p>}
                  <dl className="mt-5 grid grid-cols-2 gap-4 border-t border-line pt-4">
                    <Info label="Data e hora">{fmt.dateTime(a.scheduledAt)}</Info>
                    <Info label="Local">{a.location || '—'}</Info>
                  </dl>
                </Card>
                <Card>
                  <CardTitle title="Documentos" description="Editais, relatórios e demais documentos disponíveis aos associados." />
                  <Attachments ownerType="Assembly" ownerId={a.id} scope="ClubPublic" canEdit={manage} />
                </Card>
              </div>
            )}
            {tab === 'presenca' && <AttendanceTab assembly={a} canEdit={manage && !closed} canRemove={manage} />}
            {tab === 'votacoes' && <PollsPanel assemblyId={a.id} readOnly={a.status === 'Cancelled'} />}
            {tab === 'ata' && <MinutesTab assembly={a} canEdit={manage} />}
            {tab === 'historico' && <Card><HistoryList entityType="Assembly" id={a.id} labelOf={(s) => labels.assemblyStatus[s as keyof typeof labels.assemblyStatus]?.label ?? s} /></Card>}

            {editing && <AssemblyForm assembly={a} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />}
          </>
        )
      }}
    </QueryBoundary>
  )
}

function AttendanceTab({ assembly, canEdit, canRemove }: { assembly: Assembly; canEdit: boolean; canRemove: boolean }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [member, setMember] = useState<{ id: string; name: string } | null>(null)
  const key = ['assembleia', assembly.id, 'presencas']
  const list = useQuery({ queryKey: key, queryFn: () => api.get<Attendance[]>(`/api/assembleias/${assembly.id}/presencas`) })
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['assembleia', assembly.id] })
    void queryClient.invalidateQueries({ queryKey: ['/api/assembleias'] })
  }
  const add = useMutation({
    mutationFn: (memberId: string) => api.post<Attendance[]>(`/api/assembleias/${assembly.id}/presencas`, { memberId }),
    onSuccess: () => { toast.success('Presença registrada.'); setMember(null); refresh() },
    onError: toast.error,
  })
  const remove = useMutation({
    mutationFn: (attendanceId: string) => api.delete(`/api/assembleias/presencas/${attendanceId}`),
    onSuccess: () => { toast.success('Presença removida.'); refresh() },
    onError: toast.error,
  })

  return (
    <Card>
      <CardTitle icon={UserCheck} title={`Lista de presença${list.data ? ` (${list.data.length})` : ''}`}
        description="Somente associados ativos participam. A presença define quem vota nas votações restritas aos presentes." />
      {canEdit && (
        <div className="mb-4 flex items-start gap-2">
          <div className="max-w-md flex-1"><MemberPicker value={member} onChange={setMember} /></div>
          <Button variant="primary" loading={add.isPending} disabled={!member} onClick={() => member && add.mutate(member.id)}>Registrar presença</Button>
        </div>
      )}
      {list.isLoading ? <LoadingState /> : list.error ? <ErrorState error={list.error} onRetry={() => list.refetch()} />
        : list.data!.length === 0 ? <EmptyState icon={UserCheck} title="Nenhuma presença registrada" />
        : (
          <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
            {list.data!.map((item) => (
              <li key={item.id} className="flex items-center gap-3 px-4 py-2.5 text-sm">
                <span className="tabular w-16 text-muted">{item.memberNumber}</span>
                <span className="min-w-0 flex-1 truncate font-medium text-ink">{item.memberName}</span>
                <span className="text-xs text-muted">{fmt.dateTime(item.at)}</span>
                {canRemove && <IconButton icon={Trash2} label="Remover presença" onClick={async () => {
                  if (await confirm({ title: 'Remover presença?', message: `${item.memberName} deixará de constar na lista. Não é possível remover quem já votou nesta assembleia.`, danger: true, confirmLabel: 'Remover' })) remove.mutate(item.id)
                }} />}
              </li>
            ))}
          </ul>
        )}
    </Card>
  )
}

function MinutesTab({ assembly, canEdit }: { assembly: Assembly; canEdit: boolean }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const [minutes, setMinutes] = useState(assembly.minutes ?? '')
  const [fileId, setFileId] = useState(assembly.minutesFileId ?? null)
  const dirty = minutes !== (assembly.minutes ?? '') || fileId !== (assembly.minutesFileId ?? null)
  const save = useMutation({
    mutationFn: () => api.put<Assembly>(`/api/assembleias/${assembly.id}/ata`, { minutes: emptyToNull(minutes), minutesFileId: fileId } satisfies Schema<'SaveMinutesRequest'>),
    onSuccess: (saved) => {
      toast.success('Ata salva.')
      queryClient.setQueryData(['assembleia', assembly.id], saved)
      void queryClient.invalidateQueries({ queryKey: ['/api/assembleias'] })
    },
    onError: toast.error,
  })

  return (
    <Card>
      <CardTitle icon={FileText} title="Ata da assembleia" description="Registre o texto da ata e, se desejar, anexe o documento assinado (PDF ou imagem)." />
      {assembly.status === 'Scheduled' && <Alert tone="info" className="mb-4">A assembleia ainda não foi realizada. A ata pode ser registrada a qualquer momento.</Alert>}
      <Field label="Texto da ata">
        <Textarea rows={14} maxLength={50000} disabled={!canEdit} value={minutes} onChange={(e) => setMinutes(e.target.value)} placeholder={canEdit ? 'Deliberações, resultados das votações, encaminhamentos…' : 'Ata não registrada.'} />
      </Field>
      <div className="mt-4 flex flex-wrap items-center gap-3">
        {fileId ? (
          <span className="inline-flex items-center gap-2 rounded-(--radius-control) border border-line px-3 py-2 text-sm">
            <FileText className="size-4 text-muted" />Documento da ata anexado
            <IconButton icon={Download} label="Baixar" onClick={() => api.download(`/api/arquivos/${fileId}`, undefined, 'ata').catch(toast.error)} />
            {canEdit && <IconButton icon={Trash2} label="Remover documento" onClick={() => setFileId(null)} />}
          </span>
        ) : <span className="text-sm text-muted">Nenhum documento anexado.</span>}
        {canEdit && <FileUpload size="sm" scope="ClubPublic" label={fileId ? 'Substituir documento' : 'Anexar documento'} onUploaded={(file) => setFileId(file.id)} />}
        {canEdit && <Button variant="primary" className="ml-auto" loading={save.isPending} disabled={!dirty} onClick={() => save.mutate()}>Salvar ata</Button>}
      </div>
      {canEdit && dirty && <p className="mt-2 text-right text-xs text-warning">Há alterações não salvas.</p>}
    </Card>
  )
}
