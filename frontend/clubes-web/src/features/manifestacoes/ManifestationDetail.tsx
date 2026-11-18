import { ArrowLeft, MessageSquareReply, PlayCircle, Send } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Timeline } from '@/components/shared'
import { Alert, Badge, Button, Card, CardTitle, Checkbox, Field, Info, Input, PageHeader, QueryBoundary, Select, StatusBadge, Textarea, useConfirm, useFileUrl, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'

type Detail = Schema<'ManifestationDetailDto'>
type Status = Schema<'ManifestationStatus'>

const FINAL: Status[] = ['Resolved', 'Refused']
const statusHints: Partial<Record<Status, string>> = {
  UnderReview: 'O autor será notificado de que a manifestação está em análise.',
  InProgress: 'O autor será notificado de que a manifestação está sendo resolvida.',
  Accepted: 'O autor será informado de que a sugestão foi aceita e será levada à assembleia.',
  ForwardedToAssembly: 'O autor será informado de que a sugestão foi encaminhada à assembleia.',
  Resolved: 'Encerra a manifestação. A resposta é obrigatória e será enviada ao autor.',
  Refused: 'Encerra a manifestação como não acolhida. A resposta é obrigatória e será enviada ao autor.',
}

export default function ManifestationDetail() {
  const { id } = useParams<{ id: string }>()
  const { can } = useAuth()
  const detail = useQuery({ queryKey: ['manifestacao', id], queryFn: () => api.get<Detail>(`/api/manifestacoes/${id}`) })

  return (
    <QueryBoundary query={detail}>
      {({ manifestation: m, updates }) => (
        <>
          <PageHeader
            back={<Link to="/manifestacoes" className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-muted hover:text-ink"><ArrowLeft className="size-3.5" />Manifestações</Link>}
            title={`Manifestação ${m.protocol}`}
            subtitle={<span className="flex flex-wrap items-center gap-2"><Badge tone={m.isSuggestion ? 'brand' : 'neutral'}>{m.typeName}</Badge><StatusBadge dict={labels.manifestationStatus} value={m.status} />Recebida em {fmt.dateTime(m.createdAt)}</span>}
          />
          <div className="grid gap-6 xl:grid-cols-3">
            <div className="space-y-6 xl:col-span-2">
              <Card>
                <CardTitle title="Descrição" />
                <p className="text-sm leading-relaxed whitespace-pre-wrap text-ink">{m.description}</p>
                {m.photoFileId && <Photo fileId={m.photoFileId} />}
              </Card>
              <Card>
                <CardTitle title="Andamento" description="Atualizações de situação, respostas ao autor e anotações internas." />
                <Timeline items={updates.map((u) => ({
                  id: u.id, at: u.at, by: u.userName, note: u.message,
                  title: `${labels.manifestationStatus[u.status].label}${u.isVisibleToAuthor ? '' : ' — anotação interna (não visível ao autor)'}`,
                }))} />
              </Card>
            </div>
            <div className="space-y-6">
              <Card>
                <CardTitle title="Dados" />
                <dl className="grid grid-cols-2 gap-4">
                  <Info label="Autor" className="col-span-2">
                    <span className="flex flex-wrap items-center gap-2">
                      {m.memberId && m.authorKind === 'Member' && can('members.view') ? <Link to={`/associados/${m.memberId}`} className="font-medium text-brand-600 hover:underline">{m.authorName}</Link> : <span className="font-medium">{m.authorName}</span>}
                      <StatusBadge dict={labels.userKind} value={m.authorKind} />
                    </span>
                  </Info>
                  <Info label="Classificação">{m.classification || 'Não classificada'}</Info>
                  <Info label="Encerrada em">{m.closedAt ? fmt.dateTime(m.closedAt) : 'Em aberto'}</Info>
                  {m.assemblyId && <Info label="Assembleia vinculada" className="col-span-2">
                    {can('assemblies.view') ? <Link to={`/assembleias/${m.assemblyId}`} className="font-medium text-brand-600 hover:underline">Abrir assembleia</Link> : 'Vinculada'}
                  </Info>}
                </dl>
              </Card>
              {can('manifestations.manage') && <Treatment detail={{ manifestation: m, updates }} />}
            </div>
          </div>
        </>
      )}
    </QueryBoundary>
  )
}

function Photo({ fileId }: { fileId: string }) {
  const url = useFileUrl(fileId)
  if (!url) return <div className="mt-4 h-48 w-72 animate-pulse rounded-(--radius-control) bg-canvas" />
  return (
    <button type="button" title="Abrir a foto em tamanho original" className="mt-4 block overflow-hidden rounded-(--radius-control) border border-line" onClick={() => window.open(url, '_blank', 'noopener')}>
      <img src={url} alt="Foto enviada pelo autor" className="max-h-72 max-w-full cursor-zoom-in object-contain" />
    </button>
  )
}

function Treatment({ detail }: { detail: Detail }) {
  const m = detail.manifestation
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const closed = FINAL.includes(m.status)
  const [status, setStatus] = useState<Status>(m.status === 'Received' ? 'UnderReview' : m.status)
  const [message, setMessage] = useState('')
  const [classification, setClassification] = useState(m.classification ?? '')
  const [assemblyId, setAssemblyId] = useState(m.assemblyId ?? '')
  const [reply, setReply] = useState('')
  const [visible, setVisible] = useState(true)

  const toAssembly = status === 'Accepted' || status === 'ForwardedToAssembly'
  const assemblies = useQuery({
    queryKey: ['/api/assembleias', 'opcoes'], enabled: m.isSuggestion && can('assemblies.view') && !closed,
    queryFn: () => api.get<Paged<Schema<'AssemblyDto'>>>('/api/assembleias', { pageSize: 50 }),
  })
  const done = (data: Detail, text: string) => {
    toast.success(text)
    queryClient.setQueryData(['manifestacao', m.id], data)
    void queryClient.invalidateQueries({ queryKey: ['/api/manifestacoes'] })
  }
  const update = useMutation({
    mutationFn: (body: Schema<'UpdateManifestationRequest'>) => api.post<Detail>(`/api/manifestacoes/${m.id}/andamento`, body),
    onSuccess: (data) => { done(data, 'Andamento atualizado. O autor foi notificado.'); setMessage('') },
    onError: toast.error,
  })
  const respond = useMutation({
    mutationFn: (body: Schema<'RespondManifestationRequest'>) => api.post<Detail>(`/api/manifestacoes/${m.id}/respostas`, body),
    onSuccess: (data, body) => { done(data, body.isVisibleToAuthor ? 'Resposta enviada ao autor.' : 'Anotação interna registrada.'); setReply('') },
    onError: toast.error,
  })
  const messageRequired = FINAL.includes(status)
  const available = (Object.keys(labels.manifestationStatus) as Status[]).filter((s) => s !== 'Received' && (m.isSuggestion || (s !== 'Accepted' && s !== 'ForwardedToAssembly')))

  return (
    <>
      <Card>
        <CardTitle title="Tratamento" description={closed ? 'Esta manifestação já foi encerrada.' : 'O autor é notificado a cada atualização de andamento.'} />
        {closed ? <Alert tone="success" title={labels.manifestationStatus[m.status].label}>Encerrada em {fmt.dateTime(m.closedAt)}. Ainda é possível enviar respostas ou registrar anotações.</Alert> : (
          <div className="space-y-4">
            {m.status === 'Received' && (
              <div className="rounded-(--radius-control) border border-line bg-canvas/60 p-3">
                <p className="mb-2 text-[13px] text-ink/80">Manifestação ainda não analisada. Ao iniciar a análise, o autor é notificado de que ela está sendo avaliada pela administração.</p>
                <Button variant="primary" size="sm" icon={PlayCircle} loading={update.isPending && update.variables?.status === 'UnderReview' && !update.variables.message} onClick={async () => {
                  if (await confirm({ title: 'Iniciar análise?', message: 'A situação passa para "Em análise" e o autor recebe uma notificação.', confirmLabel: 'Iniciar análise' })) update.mutate({ status: 'UnderReview' })
                }}>Iniciar análise</Button>
              </div>
            )}
            <Field label="Nova situação" hint={statusHints[status]}>
              <Select value={status} onChange={(e) => setStatus(e.target.value as Status)}>{available.map((s) => <option key={s} value={s}>{labels.manifestationStatus[s].label}</option>)}</Select>
            </Field>
            {!m.isSuggestion && <p className="-mt-2 text-xs text-muted">Somente manifestações do tipo sugestão podem ser aceitas e encaminhadas à assembleia.</p>}
            {toAssembly && can('assemblies.view') && (
              <Field label="Assembleia" hint="Opcional. Vincula a sugestão à assembleia em que será apreciada.">
                <Select value={assemblyId} onChange={(e) => setAssemblyId(e.target.value)}>
                  <option value="">Não vincular agora</option>
                  {(assemblies.data?.items ?? []).map((a) => <option key={a.id} value={a.id}>{a.title} — {fmt.date(a.scheduledAt)}</option>)}
                </Select>
              </Field>
            )}
            <Field label="Mensagem ao autor" required={messageRequired} hint={messageRequired ? undefined : 'Opcional. É incluída na notificação.'}>
              <Textarea rows={3} maxLength={2000} value={message} onChange={(e) => setMessage(e.target.value)} />
            </Field>
            <Field label="Classificação" hint="Uso interno (ex.: Infraestrutura, Atendimento).">
              <Input maxLength={80} value={classification} onChange={(e) => setClassification(e.target.value)} />
            </Field>
            <Button variant="primary" className="w-full" loading={update.isPending} disabled={messageRequired && !message.trim()} onClick={async () => {
              const body = { status, message: emptyToNull(message), classification: emptyToNull(classification), assemblyId: toAssembly && assemblyId ? assemblyId : null }
              if (!messageRequired || await confirm({ title: `Encerrar como "${labels.manifestationStatus[status].label}"?`, message: 'A manifestação será encerrada e não poderá mais mudar de situação. O autor receberá a resposta.', confirmLabel: 'Encerrar', danger: status === 'Refused' })) update.mutate(body)
            }}>Atualizar andamento</Button>
          </div>
        )}
      </Card>

      <Card>
        <CardTitle icon={MessageSquareReply} title="Responder" />
        <div className="space-y-3">
          <Textarea rows={3} maxLength={2000} placeholder="Escreva a resposta ou a anotação…" value={reply} onChange={(e) => setReply(e.target.value)} />
          <Checkbox label="Visível ao autor (envia notificação)" checked={visible} onChange={(e) => setVisible(e.target.checked)} />
          {!visible && <p className="text-xs text-muted">Será registrada como anotação interna: somente a equipe a vê.</p>}
          <Button icon={Send} className="w-full" loading={respond.isPending} disabled={reply.trim().length < 2} onClick={() => respond.mutate({ message: reply.trim(), isVisibleToAuthor: visible })}>
            {visible ? 'Enviar resposta' : 'Registrar anotação interna'}
          </Button>
        </div>
      </Card>
    </>
  )
}
