/* Blocos reutilizáveis entre módulos: anexos, histórico de situação e seletor de associado. */
import { Download, FileText, History, Paperclip, Trash2 } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, type Paged, type Schema } from '@/lib/api'
import { fmt } from '@/lib/format'
import { Avatar, cn, EmptyState, ErrorState, FileUpload, IconButton, Input, LoadingState, Spinner, useConfirm, useToast } from './ui'

/**
 * Anexos de um registro (documentos, fotos). `ownerType`: Member, Dependent, Employee, Assembly, Announcement,
 * MaintenanceOrder, Inspection, Occurrence, Asset ou FinancialEntry. O servidor valida a permissão por tipo.
 */
export function Attachments({ ownerType, ownerId, canEdit, ownerMemberId, scope = 'Restricted' }: {
  ownerType: string; ownerId: string; canEdit: boolean; ownerMemberId?: string | null; scope?: Schema<'FileScope'>
}) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const key = ['anexos', ownerType, ownerId]
  const list = useQuery({ queryKey: key, queryFn: () => api.get<Schema<'AttachmentDto'>[]>('/api/anexos', { ownerType, ownerId }) })
  const invalidate = () => queryClient.invalidateQueries({ queryKey: key })
  const add = useMutation({
    mutationFn: (file: Schema<'FileDto'>) => api.post('/api/anexos', { ownerType, ownerId, fileId: file.id, title: file.fileName }),
    onSuccess: () => { toast.success('Anexo adicionado.'); void invalidate() }, onError: toast.error,
  })
  const remove = useMutation({ mutationFn: (id: string) => api.delete(`/api/anexos/${id}`), onSuccess: () => { toast.success('Anexo removido.'); void invalidate() }, onError: toast.error })

  if (list.isLoading) return <LoadingState />
  if (list.error) return <ErrorState error={list.error} onRetry={() => list.refetch()} />
  return (
    <div>
      {canEdit && <div className="mb-3 flex justify-end"><FileUpload size="sm" label="Adicionar anexo" scope={scope} ownerMemberId={ownerMemberId} onUploaded={(file) => add.mutate(file)} /></div>}
      {list.data!.length === 0 ? <EmptyState icon={Paperclip} title="Nenhum anexo" description={canEdit ? 'Envie imagens ou PDFs de até 10 MB.' : undefined} /> : (
        <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
          {list.data!.map((item) => (
            <li key={item.id} className="flex items-center gap-3 px-4 py-3">
              <FileText className="size-5 shrink-0 text-muted" />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-ink">{item.title || item.fileName}</p>
                <p className="text-xs text-muted">{fmt.fileSize(item.size)} · {fmt.dateTime(item.createdAt)}</p>
              </div>
              <IconButton icon={Download} label="Baixar" onClick={() => api.download(`/api/arquivos/${item.fileId}`, undefined, item.fileName).catch(toast.error)} />
              {canEdit && <IconButton icon={Trash2} label="Remover" onClick={async () => { if (await confirm({ title: 'Remover anexo?', message: item.title || item.fileName, danger: true, confirmLabel: 'Remover' })) remove.mutate(item.id) }} />}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

/** Linha do tempo de mudanças de situação. `entityType`: Member, Dependent, Employee, Reservation, MaintenanceOrder, Occurrence, Asset, Assembly ou Poll. */
export function HistoryList({ entityType, id, labelOf }: { entityType: string; id: string; labelOf?: (status: string) => string }) {
  const history = useQuery({ queryKey: ['historico', entityType, id], queryFn: () => api.get<Schema<'HistoryDto'>[]>(`/api/historico/${entityType}/${id}`) })
  if (history.isLoading) return <LoadingState />
  if (history.error) return <ErrorState error={history.error} onRetry={() => history.refetch()} />
  if (history.data!.length === 0) return <EmptyState icon={History} title="Sem histórico" />
  return <Timeline items={history.data!.map((h) => ({ id: h.id, title: labelOf ? labelOf(h.toStatus) : h.toStatus, note: h.note, by: h.userName, at: h.at }))} />
}

export function Timeline({ items }: { items: { id: string; title: string; note?: string | null; by?: string | null; at: string }[] }) {
  return (
    <ol className="relative ml-2 space-y-5 border-l border-line pl-6">
      {items.map((item) => (
        <li key={item.id} className="relative">
          <span className="absolute top-1.5 -left-[29px] size-2.5 rounded-full border-2 border-surface bg-brand-500 ring-1 ring-line" />
          <p className="text-sm font-medium text-ink">{item.title}</p>
          {item.note && <p className="mt-0.5 text-[13px] text-ink/75">{item.note}</p>}
          <p className="mt-0.5 text-xs text-muted">{fmt.dateTime(item.at)}{item.by ? ` · ${item.by}` : ''}</p>
        </li>
      ))}
    </ol>
  )
}

/** Busca e seleção de associado (autocomplete por nome, CPF ou matrícula). */
export function MemberPicker({ value, onChange, onlyActive = true, placeholder = 'Buscar associado por nome, CPF ou matrícula…' }: {
  value: { id: string; name: string } | null; onChange: (member: { id: string; name: string } | null) => void; onlyActive?: boolean; placeholder?: string
}) {
  const [term, setTerm] = useState('')
  const [debounced, setDebounced] = useState('')
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => { const timer = setTimeout(() => setDebounced(term), 250); return () => clearTimeout(timer) }, [term])
  useEffect(() => {
    const onDown = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', onDown)
    return () => document.removeEventListener('mousedown', onDown)
  }, [])
  const results = useQuery({
    queryKey: ['associados', 'busca', debounced, onlyActive],
    queryFn: () => api.get<Paged<Schema<'MemberListDto'>>>('/api/associados', { search: debounced, pageSize: 8, status: onlyActive ? 'Active' : undefined }),
    enabled: open && debounced.trim().length >= 2,
  })
  if (value) {
    return (
      <div className="flex h-10 items-center justify-between rounded-(--radius-control) border border-line bg-canvas px-3 text-sm">
        <span className="truncate font-medium text-ink">{value.name}</span>
        <button type="button" className="text-[13px] font-medium text-brand-600 hover:underline" onClick={() => { onChange(null); setTerm('') }}>Trocar</button>
      </div>
    )
  }
  return (
    <div className="relative" ref={ref}>
      <Input value={term} placeholder={placeholder} onFocus={() => setOpen(true)} onChange={(e) => { setTerm(e.target.value); setOpen(true) }} />
      {open && debounced.trim().length >= 2 && (
        <div className="animate-pop absolute z-30 mt-1 max-h-72 w-full overflow-y-auto rounded-xl border border-line bg-surface p-1 shadow-(--shadow-pop)">
          {results.isLoading ? <div className="flex justify-center py-4"><Spinner /></div>
            : (results.data?.items.length ?? 0) === 0 ? <p className="px-3 py-3 text-sm text-muted">Nenhum associado encontrado.</p>
            : results.data!.items.map((m) => (
              <button type="button" key={m.id} onClick={() => { onChange({ id: m.id, name: m.fullName }); setOpen(false) }}
                className={cn('flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left text-sm hover:bg-canvas')}>
                <Avatar fileId={m.photoFileId} name={m.fullName} size={28} />
                <span className="min-w-0 flex-1"><span className="block truncate font-medium text-ink">{m.fullName}</span><span className="block text-xs text-muted">Matrícula {m.number} · {m.cpfMasked}</span></span>
              </button>
            ))}
        </div>
      )}
    </div>
  )
}
