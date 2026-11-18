import { CalendarClock, ChevronLeft, ChevronRight, Eye, Megaphone, Paperclip, Plus } from 'lucide-react'
import { useState } from 'react'
import type { Feature } from '@/app/features'
import { usePaged } from '@/components/table'
import { Badge, Button, Card, cn, EmptyState, ErrorState, FileImage, LoadingState, PageHeader, SearchInput } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'
import { AnnouncementDetail } from './AnnouncementDetail'
import { AnnouncementForm } from './AnnouncementForm'
import { audienceOf, type Announcement } from './audience'

function AnnouncementCard({ item, onOpen }: { item: Announcement; onOpen: () => void }) {
  const expired = !!item.expiresAt && new Date(item.expiresAt) <= new Date()
  return (
    <button type="button" onClick={onOpen}
      className="group flex flex-col overflow-hidden rounded-(--radius-card) border border-line bg-surface text-left shadow-(--shadow-card) transition-shadow hover:shadow-(--shadow-pop)">
      <div className="relative h-36 w-full shrink-0 overflow-hidden bg-brand-50">
        {item.imageFileId
          ? <FileImage fileId={item.imageFileId} alt={item.title} className="size-full object-cover transition-transform duration-300 group-hover:scale-[1.03]" />
          : <div className="flex size-full items-center justify-center text-brand-600/40"><Megaphone className="size-10" /></div>}
        <div className="absolute top-3 left-3 flex flex-wrap gap-1.5">
          <Badge tone={labels.announcementCategory[item.category].tone} className="bg-surface/95 shadow-sm">{labels.announcementCategory[item.category].label}</Badge>
          {item.priority !== 'Normal' && <Badge tone={labels.priority[item.priority].tone} className="bg-surface/95 shadow-sm">{labels.priority[item.priority].label}</Badge>}
        </div>
        {!item.publishedAt && <Badge tone="warning" className="absolute top-3 right-3 bg-surface/95 shadow-sm">Rascunho</Badge>}
      </div>
      <div className="flex flex-1 flex-col p-4">
        <h3 className="line-clamp-2 text-[15px] font-semibold text-ink">{item.title}</h3>
        <p className="mt-1.5 line-clamp-3 text-[13px] leading-relaxed text-ink/70">{item.body}</p>
        <div className="mt-3 flex flex-wrap gap-1.5">
          {audienceOf(item).map((audience) => <Badge key={audience} tone={item.isSpecific ? 'info' : 'neutral'}>{audience}</Badge>)}
        </div>
        <div className="mt-auto flex flex-wrap items-center gap-x-4 gap-y-1 pt-4 text-xs text-muted">
          <span>{item.publishedAt ? `Publicado em ${fmt.date(item.publishedAt)}` : `Criado em ${fmt.date(item.createdAt)}`}</span>
          <span className="inline-flex items-center gap-1" title="Leituras"><Eye className="size-3.5" />{item.readCount}</span>
          {item.attachments > 0 && <span className="inline-flex items-center gap-1" title="Anexos"><Paperclip className="size-3.5" />{item.attachments}</span>}
          {item.expiresAt && <span className={cn('inline-flex items-center gap-1', expired && 'font-medium text-danger')}><CalendarClock className="size-3.5" />{expired ? 'Expirado em' : 'Expira em'} {fmt.date(item.expiresAt)}</span>}
        </div>
      </div>
    </button>
  )
}

function AnnouncementsPage() {
  const [category, setCategory] = useState<Schema<'AnnouncementCategory'> | ''>('')
  const [creating, setCreating] = useState(false)
  const [openId, setOpenId] = useState<string | null>(null)
  const list = usePaged<Announcement>('/api/comunicados', { category: category || undefined }, { pageSize: 12 })
  const pages = Math.max(1, Math.ceil(list.total / list.pageSize))
  const chips: { value: Schema<'AnnouncementCategory'> | ''; label: string }[] = [{ value: '', label: 'Todos' }, ...options(labels.announcementCategory)]

  return (
    <>
      <PageHeader title="Comunicados" subtitle="Avisos, notícias e comunicados publicados no mural do aplicativo."
        actions={<Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo comunicado</Button>} />

      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-1.5">
          {chips.map((chip) => (
            <button key={chip.value} type="button" onClick={() => setCategory(chip.value)}
              className={cn('rounded-full border px-3 py-1.5 text-[13px] font-medium transition-colors', category === chip.value ? 'border-brand-600 bg-brand-600 text-white' : 'border-line bg-surface text-muted hover:text-ink')}>
              {chip.label}
            </button>
          ))}
        </div>
        <SearchInput value={list.search} onChange={list.setSearch} placeholder="Pesquisar pelo título…" />
      </div>

      {list.isLoading && list.rows.length === 0 ? <Card><LoadingState /></Card>
        : list.error ? <Card><ErrorState error={list.error} onRetry={() => list.refetch()} /></Card>
        : list.rows.length === 0 ? (
          <Card><EmptyState icon={Megaphone} title="Nenhum comunicado encontrado"
            description={list.search || category ? 'Ajuste a pesquisa ou a categoria.' : 'Crie o primeiro comunicado para os associados, dependentes ou funcionários.'}
            action={!list.search && !category ? <Button variant="primary" icon={Plus} onClick={() => setCreating(true)}>Novo comunicado</Button> : undefined} /></Card>
        ) : (
          <>
            <div className={cn('grid gap-5 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4', list.isFetching && 'opacity-70')}>
              {list.rows.map((item) => <AnnouncementCard key={item.id} item={item} onOpen={() => setOpenId(item.id)} />)}
            </div>
            <footer className="mt-5 flex items-center justify-between text-[13px] text-muted">
              <span>{fmt.number(list.total)} comunicado(s)</span>
              {pages > 1 && (
                <div className="flex items-center gap-2">
                  <Button size="sm" icon={ChevronLeft} disabled={list.page <= 1} onClick={() => list.setPage(list.page - 1)}>Anterior</Button>
                  <span className="tabular px-1">Página {list.page} de {pages}</span>
                  <Button size="sm" disabled={list.page >= pages} onClick={() => list.setPage(list.page + 1)}>Próxima<ChevronRight className="size-4" /></Button>
                </div>
              )}
            </footer>
          </>
        )}

      {creating && <AnnouncementForm onClose={() => setCreating(false)} onSaved={(saved) => { setCreating(false); setOpenId(saved.id) }} />}
      {openId && <AnnouncementDetail id={openId} onClose={() => setOpenId(null)} />}
    </>
  )
}

const feature: Feature = {
  nav: [{ to: '/comunicados', label: 'Comunicados', icon: Megaphone, group: 'Comunicação', order: 1, permissions: ['announcements.manage'] }],
  routes: [{ path: 'comunicados', component: AnnouncementsPage, permissions: ['announcements.manage'] }],
}
export default feature
