import { Eye, Pencil, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Attachments } from '@/components/shared'
import { Badge, Button, EmptyState, ErrorState, FileImage, Info, LoadingState, Modal, QueryBoundary, StatusBadge, Tabs, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { AnnouncementForm } from './AnnouncementForm'
import { audienceOf, type Announcement } from './audience'

type Tab = 'conteudo' | 'anexos' | 'leituras'

export function AnnouncementDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [tab, setTab] = useState<Tab>('conteudo')
  const [editing, setEditing] = useState(false)
  const announcement = useQuery({ queryKey: ['comunicado', id], queryFn: () => api.get<Announcement>(`/api/comunicados/${id}`) })
  const close = () => { void queryClient.invalidateQueries({ queryKey: ['/api/comunicados'] }); onClose() }
  const remove = useMutation({
    mutationFn: () => api.delete(`/api/comunicados/${id}`),
    onSuccess: () => { toast.success('Comunicado excluído.'); close() },
    onError: toast.error,
  })
  const a = announcement.data

  if (editing && a) return <AnnouncementForm announcement={a} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />
  return (
    <Modal open onClose={close} size="lg" title={a?.title ?? 'Comunicado'}
      footer={a && <>
        <Button variant="ghost" icon={Trash2} className="mr-auto text-danger hover:text-danger" loading={remove.isPending} onClick={async () => {
          if (await confirm({ title: 'Excluir comunicado?', message: `"${a.title}" será removido do mural de todos os destinatários. Esta ação não pode ser desfeita.`, danger: true, confirmLabel: 'Excluir' })) remove.mutate()
        }}>Excluir</Button>
        <Button onClick={close}>Fechar</Button>
        <Button variant="primary" icon={Pencil} onClick={() => setEditing(true)}>Editar</Button>
      </>}>
      <QueryBoundary query={announcement}>
        {(item) => (
          <>
            <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'conteudo', label: 'Conteúdo' }, { key: 'anexos', label: 'Anexos', count: item.attachments }, { key: 'leituras', label: 'Leituras', count: item.readCount }]} />
            {tab === 'conteudo' && (
              <div className="space-y-5">
                {item.imageFileId && <FileImage fileId={item.imageFileId} alt={item.title} className="max-h-72 w-full rounded-(--radius-control) object-cover" />}
                <div className="flex flex-wrap items-center gap-1.5">
                  <StatusBadge dict={labels.announcementCategory} value={item.category} />
                  <Badge tone={labels.priority[item.priority].tone}>Prioridade {labels.priority[item.priority].label.toLowerCase()}</Badge>
                  {item.publishedAt ? <Badge tone="success">Publicado</Badge> : <Badge tone="warning">Rascunho</Badge>}
                  {audienceOf(item).map((audience) => <Badge key={audience} tone={item.isSpecific ? 'info' : 'neutral'}>{audience}</Badge>)}
                </div>
                <p className="text-sm leading-relaxed whitespace-pre-wrap text-ink">{item.body}</p>
                <dl className="grid grid-cols-4 gap-4 border-t border-line pt-4">
                  <Info label="Autor">{item.authorName || '—'}</Info>
                  <Info label="Criado em">{fmt.dateTime(item.createdAt)}</Info>
                  <Info label="Publicado em">{item.publishedAt ? fmt.dateTime(item.publishedAt) : 'Ainda não publicado'}</Info>
                  <Info label="Expira em">{item.expiresAt ? fmt.dateTime(item.expiresAt) : 'Sem expiração'}</Info>
                </dl>
              </div>
            )}
            {tab === 'anexos' && <Attachments ownerType="Announcement" ownerId={item.id} scope="ClubPublic" canEdit />}
            {tab === 'leituras' && <Readers id={item.id} published={!!item.publishedAt} />}
          </>
        )}
      </QueryBoundary>
    </Modal>
  )
}

function Readers({ id, published }: { id: string; published: boolean }) {
  const readers = useQuery({ queryKey: ['comunicado', id, 'leituras'], queryFn: () => api.get<Schema<'AnnouncementReaderDto'>[]>(`/api/comunicados/${id}/leituras`) })
  if (readers.isLoading) return <LoadingState />
  if (readers.error) return <ErrorState error={readers.error} onRetry={() => readers.refetch()} />
  if (readers.data!.length === 0) return <EmptyState icon={Eye} title="Nenhuma leitura registrada" description={published ? 'As leituras aparecem aqui conforme os destinatários abrem o comunicado.' : 'O comunicado ainda não foi publicado.'} />
  return (
    <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
      {readers.data!.map((reader) => (
        <li key={reader.userId} className="flex items-center gap-3 px-4 py-2.5 text-sm">
          <span className="min-w-0 flex-1 truncate font-medium text-ink">{reader.name}</span>
          <StatusBadge dict={labels.userKind} value={reader.kind} />
          <span className="w-36 text-right text-xs text-muted">{fmt.dateTime(reader.readAt)}</span>
        </li>
      ))}
    </ul>
  )
}
