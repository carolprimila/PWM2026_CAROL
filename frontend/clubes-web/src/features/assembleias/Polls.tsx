import { zodResolver } from '@hookform/resolvers/zod'
import { Megaphone, Pencil, Play, Plus, Square, Trash2, Vote } from 'lucide-react'
import { useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Badge, Button, Card, cn, EmptyState, ErrorState, Field, IconButton, Input, LoadingState, Modal, Select, StatusBadge, Switch, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, emptyToNull, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Poll = Schema<'PollDto'>

/** Lista de votações (de uma assembleia ou de todo o clube) com criação, abertura, encerramento e resultados. */
export function PollsPanel({ assemblyId, readOnly }: { assemblyId?: string; readOnly?: boolean }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const manage = can('assemblies.manage') && !readOnly
  const [form, setForm] = useState<{ poll?: Poll } | null>(null)
  const [closing, setClosing] = useState<Poll | null>(null)
  const polls = useQuery({ queryKey: ['/api/votacoes', assemblyId ?? null], queryFn: () => api.get<Poll[]>('/api/votacoes', { assemblyId }) })
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['/api/votacoes'] })
    void queryClient.invalidateQueries({ queryKey: ['/api/assembleias'] })
    void queryClient.invalidateQueries({ queryKey: ['assembleia'] })
  }
  const action = useMutation({
    mutationFn: ({ poll, path, body }: { poll: Poll; path: string; body?: unknown; message: string }) => api.post<Poll>(`/api/votacoes/${poll.id}/${path}`, body),
    onSuccess: (_, variables) => { toast.success(variables.message); setClosing(null); refresh() },
    onError: toast.error,
  })

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Alert tone="info" className="flex-1">Somente associados votam; dependentes nunca participam. O resultado parcial só é visível à equipe até a divulgação.</Alert>
        {manage && <Button variant="primary" icon={Plus} onClick={() => setForm({})}>Nova votação</Button>}
      </div>

      {polls.isLoading ? <Card><LoadingState /></Card>
        : polls.error ? <Card><ErrorState error={polls.error} onRetry={() => polls.refetch()} /></Card>
        : polls.data!.length === 0 ? <Card><EmptyState icon={Vote} title="Nenhuma votação" description={manage ? 'Crie uma votação em rascunho, revise as opções e abra-a quando estiver pronta.' : undefined} /></Card>
        : (
          <div className="grid gap-4 xl:grid-cols-2">
            {polls.data!.map((poll) => (
              <Card key={poll.id}>
                <header className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 className="text-[15px] font-semibold text-ink">{poll.title}</h3>
                    <p className="mt-0.5 text-xs text-muted">
                      {poll.assemblyTitle ? `Assembleia: ${poll.assemblyTitle}` : 'Sem assembleia vinculada'} · {labels.pollCriterion[poll.criterion].label}
                    </p>
                  </div>
                  <div className="flex shrink-0 flex-wrap justify-end gap-1.5">
                    <StatusBadge dict={labels.pollStatus} value={poll.status} />
                    {poll.status === 'Open' && !poll.isOpenNow && <Badge tone="warning">{poll.opensAt && new Date(poll.opensAt) > new Date() ? 'Aguardando abertura' : 'Prazo encerrado'}</Badge>}
                    {poll.resultsPublished && <Badge tone="info">Resultado divulgado</Badge>}
                  </div>
                </header>
                {poll.description && <p className="mt-3 text-[13px] leading-relaxed whitespace-pre-wrap text-ink/80">{poll.description}</p>}
                <p className="mt-3 text-xs text-muted">
                  Abertura: {poll.opensAt ? fmt.dateTime(poll.opensAt) : poll.status === 'Draft' ? 'ao abrir' : '—'} · Encerramento: {poll.closesAt ? fmt.dateTime(poll.closesAt) : 'manual'}
                </p>
                <PollResults poll={poll} />
                {manage && (poll.status !== 'Closed' || !poll.resultsPublished) && (
                  <footer className="mt-4 flex flex-wrap justify-end gap-2 border-t border-line pt-4">
                    {poll.status === 'Draft' && <>
                      <Button size="sm" icon={Pencil} onClick={() => setForm({ poll })}>Editar</Button>
                      <Button size="sm" variant="primary" icon={Play} disabled={action.isPending} onClick={async () => {
                        if (await confirm({ title: 'Abrir votação?', confirmLabel: 'Abrir votação', message: 'Todos os associados serão notificados de que a votação está aberta. Depois de aberta, o título, as opções e o critério não poderão mais ser editados.' }))
                          action.mutate({ poll, path: 'abrir', message: 'Votação aberta. Os associados foram notificados.' })
                      }}>Abrir votação</Button>
                    </>}
                    {poll.status === 'Open' && <Button size="sm" variant="primary" icon={Square} onClick={() => setClosing(poll)}>Encerrar</Button>}
                    {poll.status === 'Closed' && !poll.resultsPublished && (
                      <Button size="sm" variant="primary" icon={Megaphone} disabled={action.isPending} onClick={async () => {
                        if (await confirm({ title: 'Divulgar resultado?', confirmLabel: 'Divulgar', message: 'Os associados serão notificados e passarão a ver a contagem de votos no aplicativo.' }))
                          action.mutate({ poll, path: 'encerrar', body: { publishResults: true }, message: 'Resultado divulgado aos associados.' })
                      }}>Divulgar resultado</Button>
                    )}
                  </footer>
                )}
              </Card>
            ))}
          </div>
        )}

      {form && <PollForm poll={form.poll} assemblyId={assemblyId} onClose={() => setForm(null)} onSaved={() => { setForm(null); refresh() }} />}
      {closing && <ClosePollModal poll={closing} loading={action.isPending} onClose={() => setClosing(null)}
        onConfirm={(publishResults) => action.mutate({ poll: closing, path: 'encerrar', body: { publishResults }, message: publishResults ? 'Votação encerrada e resultado divulgado.' : 'Votação encerrada.' })} />}
    </div>
  )
}

/** Barras horizontais com contagem e percentual. A equipe sempre recebe a contagem; associados só após a divulgação. */
function PollResults({ poll }: { poll: Poll }) {
  const total = poll.totalVotes ?? 0
  const top = Math.max(0, ...poll.options.map((o) => o.votes ?? 0))
  return (
    <div className="mt-4 space-y-2.5">
      {poll.options.map((option) => {
        const votes = option.votes ?? 0
        const percent = total > 0 ? (votes / total) * 100 : 0
        const leading = poll.status === 'Closed' && votes > 0 && votes === top
        return (
          <div key={option.id}>
            <div className="mb-1 flex items-baseline justify-between gap-3 text-[13px]">
              <span className={cn('min-w-0 truncate', leading ? 'font-semibold text-ink' : 'text-ink')}>{option.text}</span>
              {poll.status !== 'Draft' && option.votes != null && <span className="tabular shrink-0 text-muted">{fmt.number(votes)} voto(s) · {percent.toFixed(1).replace('.', ',')}%</span>}
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-canvas"><div className={cn('h-full rounded-full transition-all', leading ? 'bg-brand-600' : 'bg-brand-400')} style={{ width: `${percent}%` }} /></div>
          </div>
        )
      })}
      {poll.status !== 'Draft' && poll.totalVotes != null && <p className="tabular pt-1 text-xs text-muted">Total: {fmt.number(total)} voto(s)</p>}
    </div>
  )
}

function ClosePollModal({ poll, loading, onClose, onConfirm }: { poll: Poll; loading: boolean; onClose: () => void; onConfirm: (publishResults: boolean) => void }) {
  const [publish, setPublish] = useState(true)
  return (
    <Modal open onClose={onClose} size="sm" title="Encerrar votação"
      footer={<><Button onClick={onClose}>Voltar</Button><Button variant="primary" loading={loading} onClick={() => onConfirm(publish)}>Encerrar votação</Button></>}>
      <div className="space-y-4">
        <p className="text-sm text-ink/80">"{poll.title}" deixará de receber votos imediatamente. Esta ação não pode ser desfeita.</p>
        <Switch checked={publish} onChange={setPublish} label="Divulgar o resultado aos associados"
          description={publish ? 'Os associados serão notificados e verão a contagem de votos.' : 'O resultado fica visível apenas para a equipe; você pode divulgá-lo depois.'} />
      </div>
    </Modal>
  )
}

const schema = z.object({
  assemblyId: z.string(),
  title: z.string().trim().min(3, 'Informe o título (mínimo de 3 caracteres)').max(200, 'Máximo de 200 caracteres'),
  description: z.string().max(4000, 'Máximo de 4.000 caracteres'),
  criterion: z.string(),
  opensAt: z.string(),
  closesAt: z.string(),
  options: z.array(z.object({ text: z.string() })),
}).superRefine((v, ctx) => {
  const texts = v.options.map((o) => o.text.trim()).filter(Boolean)
  if (new Set(texts).size < 2) ctx.addIssue({ code: 'custom', path: ['options'], message: 'Informe ao menos duas opções diferentes' })
  if (v.opensAt && v.closesAt && new Date(v.closesAt) <= new Date(v.opensAt)) ctx.addIssue({ code: 'custom', path: ['closesAt'], message: 'O encerramento deve ser posterior à abertura' })
  if (v.criterion === 'PresentAtAssembly' && !v.assemblyId) ctx.addIssue({ code: 'custom', path: ['criterion'], message: 'Este critério exige uma assembleia vinculada' })
})
type Values = z.infer<typeof schema>

function PollForm({ poll, assemblyId, onClose, onSaved }: { poll?: Poll; assemblyId?: string; onClose: () => void; onSaved: () => void }) {
  const toast = useToast()
  const assemblies = useQuery({
    queryKey: ['/api/assembleias', 'opcoes'], enabled: !assemblyId,
    queryFn: () => api.get<Paged<Schema<'AssemblyDto'>>>('/api/assembleias', { pageSize: 50 }),
  })
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      assemblyId: poll?.assemblyId ?? assemblyId ?? '', title: poll?.title ?? '', description: poll?.description ?? '', criterion: poll?.criterion ?? 'ActiveMembers',
      opensAt: dates.toInput(poll?.opensAt), closesAt: dates.toInput(poll?.closesAt), options: poll ? poll.options.map((o) => ({ text: o.text })) : [{ text: '' }, { text: '' }],
    },
  })
  const { errors } = form.formState
  const fields = useFieldArray({ control: form.control, name: 'options' })
  const optionsError = errors.options?.message ?? errors.options?.root?.message

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body: Schema<'SavePollRequest'> = {
        assemblyId: values.assemblyId || null, title: values.title, description: emptyToNull(values.description), criterion: values.criterion as Schema<'PollCriterion'>,
        opensAt: dates.fromInput(values.opensAt), closesAt: dates.fromInput(values.closesAt), options: values.options.map((o) => o.text.trim()).filter(Boolean),
      }
      return poll ? api.put<Poll>(`/api/votacoes/${poll.id}`, body) : api.post<Poll>('/api/votacoes', body)
    },
    onSuccess: () => { toast.success(poll ? 'Votação atualizada.' : 'Votação criada em rascunho.'); onSaved() },
    onError: toast.error,
  })

  return (
    <Modal open onClose={onClose} size="lg" title={poll ? 'Editar votação' : 'Nova votação'} description="A votação fica em rascunho até ser aberta. Somente associados votam — um voto por associado."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{poll ? 'Salvar alterações' : 'Salvar rascunho'}</Button>
      </>}>
      <form className="space-y-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field label="Título" required error={errors.title?.message}><Input aria-invalid={!!errors.title} maxLength={200} placeholder="Ex.: Aprovação das contas do exercício" {...form.register('title')} /></Field>
        <Field label="Descrição" error={errors.description?.message}><Textarea rows={3} {...form.register('description')} /></Field>
        <div className="grid grid-cols-2 gap-4">
          {!assemblyId && (
            <Field label="Assembleia" hint="Opcional. Votações também podem ocorrer fora de assembleias.">
              <Select {...form.register('assemblyId')} value={form.watch('assemblyId')}>
                <option value="">Sem assembleia</option>
                {(assemblies.data?.items ?? []).filter((a) => a.status !== 'Cancelled' || a.id === poll?.assemblyId).map((a) => <option key={a.id} value={a.id}>{a.title} — {fmt.date(a.scheduledAt)}</option>)}
              </Select>
            </Field>
          )}
          <Field label="Quem pode votar" error={errors.criterion?.message} className={assemblyId ? 'col-span-2' : undefined}>
            <Select aria-invalid={!!errors.criterion} {...form.register('criterion')}>{options(labels.pollCriterion).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
          </Field>
          <Field label="Abertura" hint="Em branco: no momento em que a votação for aberta."><Input type="datetime-local" {...form.register('opensAt')} /></Field>
          <Field label="Encerramento" hint="Em branco: encerramento manual." error={errors.closesAt?.message}><Input type="datetime-local" aria-invalid={!!errors.closesAt} {...form.register('closesAt')} /></Field>
        </div>
        <fieldset>
          <legend className="mb-1.5 text-[13px] font-medium text-ink">Opções de voto <span className="text-danger">*</span></legend>
          <div className="space-y-2">
            {fields.fields.map((field, index) => (
              <div key={field.id} className="flex items-center gap-2">
                <span className="tabular w-5 text-right text-xs text-muted">{index + 1}.</span>
                <Input placeholder={`Opção ${index + 1}`} maxLength={200} {...form.register(`options.${index}.text`)} />
                <IconButton icon={Trash2} label="Remover opção" disabled={fields.fields.length <= 2} onClick={() => fields.remove(index)} />
              </div>
            ))}
          </div>
          {optionsError && <p className="mt-1.5 text-xs text-danger">{optionsError}</p>}
          <Button size="sm" icon={Plus} className="mt-3" onClick={() => fields.append({ text: '' })}>Adicionar opção</Button>
        </fieldset>
      </form>
    </Modal>
  )
}
