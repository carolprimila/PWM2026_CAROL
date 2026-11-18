import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, ArrowRight, Check, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { Controller, useFieldArray, useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { MemberPicker } from '@/components/shared'
import { Alert, Badge, Button, Card, CardTitle, cn, Field, IconButton, Info, Input, MaskedInput, PageHeader, QueryBoundary, Textarea, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { dates, emptyToNull, fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { AvailabilityPicker, period, spaceGroups, useSpaces, type Space } from './availability'

type Kind = Schema<'ReservationKind'>
type Quote = Schema<'QuoteDto'>

const steps = ['Tipo', 'Espaço e data', 'Horários', 'Pessoas e convidados', 'Valores e confirmação']
const kinds: { value: Kind; title: string; description: string }[] = [
  { value: 'Member', title: 'Associado', description: 'Reserva em nome de um associado, com cobrança conforme as regras do espaço.' },
  { value: 'Administrative', title: 'Administrativa', description: 'Uso interno do clube (manutenção programada, cessão, cortesia). Sem cobrança.' },
  { value: 'InternalEvent', title: 'Evento interno', description: 'Evento promovido pelo clube que ocupa o espaço. Sem cobrança.' },
]

const schema = z.object({
  title: z.string().max(150, 'Máximo de 150 caracteres'),
  peopleCount: z.string().refine((v) => /^\d+$/.test(v.trim()) && Number(v) >= 1, 'Informe a quantidade de pessoas'),
  notes: z.string().max(1000, 'Máximo de 1000 caracteres'),
  guests: z.array(z.object({
    fullName: z.string().trim().min(3, 'Informe o nome completo').max(150, 'Máximo de 150 caracteres'),
    document: z.string().max(30, 'Máximo de 30 caracteres'),
    phone: z.string(),
  })),
})
type Values = z.infer<typeof schema>

export default function NewReservation() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [params] = useSearchParams()
  const spaces = useSpaces()

  const [step, setStep] = useState(1)
  const [kind, setKind] = useState<Kind>('Member')
  const [member, setMember] = useState<{ id: string; name: string } | null>(null)
  const [spaceId, setSpaceId] = useState(params.get('spaceId') ?? '')
  const [date, setDate] = useState(params.get('date') ?? dates.today())
  const [minutes, setMinutes] = useState<number[]>(() => { const start = Number(params.get('start')); return params.get('start') && Number.isInteger(start) ? [start] : [] })
  const [quote, setQuote] = useState<Quote | null>(null)
  const [stepError, setStepError] = useState<string | null>(null)

  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { title: '', peopleCount: '1', notes: '', guests: [] } })
  const guests = useFieldArray({ control: form.control, name: 'guests' })
  const { errors } = form.formState

  const reservable = (spaces.data ?? []).filter((s) => s.isReservable && s.isActive)
  const space = reservable.find((s) => s.id === spaceId)
  const hourly = space?.bookingMode === 'Hourly'

  const body = (): Schema<'CreateReservationRequest'> => {
    const values = form.getValues()
    return {
      spaceId, date, startMinutes: hourly ? minutes : [], peopleCount: Number(values.peopleCount),
      guests: kind === 'Member' ? values.guests.map((g) => ({ fullName: g.fullName.trim(), document: emptyToNull(g.document), phone: emptyToNull(g.phone) })) : [],
      notes: emptyToNull(values.notes), memberId: kind === 'Member' ? member?.id ?? null : null, kind, title: kind === 'Member' ? null : values.title.trim(),
    }
  }
  // Conflito (409): o servidor é a autoridade — recarrega a disponibilidade e volta à escolha de horários.
  const onError = (error: unknown) => {
    toast.error(error)
    if (error instanceof ApiError && error.status === 409) {
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade', spaceId] })
      setQuote(null)
      setStep(3)
    }
  }
  const simulate = useMutation({
    mutationFn: () => api.post<Quote>('/api/reservas/simular', body()),
    onSuccess: (result) => { setQuote(result); setStep(5) },
    onError,
  })
  const create = useMutation({
    mutationFn: () => api.post<Schema<'ReservationDetailDto'>>('/api/reservas', body()),
    onSuccess: (detail) => {
      toast.success(`Reserva ${detail.reservation.code} criada.`)
      void queryClient.invalidateQueries({ queryKey: ['/api/reservas'] })
      void queryClient.invalidateQueries({ queryKey: ['disponibilidade'] })
      navigate(`/reservas/${detail.reservation.id}`)
    },
    onError,
  })

  const selectSpace = (next: Space) => {
    if (next.id === spaceId) return
    setSpaceId(next.id)
    setMinutes([])
    setDate(next.bookingMode === 'Hourly' ? date || dates.today() : '')
  }

  const next = async () => {
    setStepError(null)
    if (step === 1) {
      if (kind === 'Member' && !member) return setStepError('Selecione o associado responsável pela reserva.')
      if (kind !== 'Member' && form.getValues('title').trim().length < 3) return form.setError('title', { message: 'Informe um título para a reserva' })
      form.clearErrors('title')
    }
    if (step === 2) {
      if (!space) return setStepError('Selecione o espaço.')
      if (hourly && !date) return setStepError('Informe a data da reserva.')
    }
    if (step === 3) {
      if (hourly && minutes.length === 0) return setStepError('Selecione ao menos um horário disponível.')
      if (!hourly && !date) return setStepError('Selecione a data de início do período.')
    }
    if (step === 4) {
      if (!(await form.trigger(['peopleCount', 'notes', 'guests']))) return
      const people = Number(form.getValues('peopleCount'))
      if (space?.capacity != null && people > space.capacity) return form.setError('peopleCount', { message: `A capacidade máxima de ${space.name} é de ${space.capacity} pessoas` })
      return simulate.mutate()
    }
    setStep(step + 1)
  }

  const values = form.watch()
  return (
    <>
      <PageHeader title="Nova reserva" subtitle="Reserva feita pela equipe em nome de um associado ou para uso do clube."
        back={<Link to="/reservas" className="mb-2 inline-flex items-center gap-1.5 text-[13px] text-muted hover:text-ink"><ArrowLeft className="size-3.5" />Reservas</Link>} />

      <ol className="mb-6 flex flex-wrap items-center gap-x-2 gap-y-2">
        {steps.map((label, index) => {
          const number = index + 1
          return (
            <li key={label} className="flex items-center gap-2">
              <button type="button" disabled={number >= step} onClick={() => { setStepError(null); setStep(number) }}
                className={cn('flex items-center gap-2 rounded-full py-1 pr-3 pl-1 text-[13px] font-medium transition-colors', number === step ? 'bg-brand-50 text-brand-700' : number < step ? 'text-ink hover:bg-black/5' : 'text-muted')}>
                <span className={cn('tabular flex size-6 items-center justify-center rounded-full text-xs', number < step ? 'bg-brand-600 text-white' : number === step ? 'bg-brand-600 text-white' : 'bg-black/5 text-muted')}>
                  {number < step ? <Check className="size-3.5" /> : number}
                </span>
                {label}
              </button>
              {number < steps.length && <span className="h-px w-6 bg-line" />}
            </li>
          )
        })}
      </ol>

      <QueryBoundary query={spaces}>
        {() => (
          <Card>
            {stepError && <Alert tone="danger" className="mb-4">{stepError}</Alert>}

            {step === 1 && (
              <div className="space-y-5">
                <CardTitle title="Tipo de reserva" description="Define quem é o responsável e se há cobrança." />
                <div className="grid gap-3 md:grid-cols-3">
                  {kinds.map((k) => (
                    <button key={k.value} type="button" onClick={() => { setKind(k.value); setStepError(null) }} aria-pressed={kind === k.value}
                      className={cn('rounded-(--radius-card) border p-4 text-left transition-colors', kind === k.value ? 'border-brand-500 bg-brand-50' : 'border-line hover:bg-canvas')}>
                      <p className="text-sm font-semibold text-ink">{k.title}</p>
                      <p className="mt-1 text-[13px] text-muted">{k.description}</p>
                    </button>
                  ))}
                </div>
                {kind === 'Member' ? (
                  <div className="max-w-xl">
                    <p className="mb-1.5 text-[13px] font-medium text-ink">Associado responsável<span className="text-danger"> *</span></p>
                    <MemberPicker value={member} onChange={(m) => { setMember(m); setStepError(null) }} />
                    <p className="mt-1 text-xs text-muted">A situação cadastral e financeira do associado é validada pelo servidor ao confirmar.</p>
                  </div>
                ) : (
                  <Field className="max-w-xl" label="Título" required error={errors.title?.message}>
                    <Input aria-invalid={!!errors.title} placeholder={kind === 'InternalEvent' ? 'Ex.: Torneio interno de tênis' : 'Ex.: Uso da diretoria'} {...form.register('title')} />
                  </Field>
                )}
              </div>
            )}

            {step === 2 && (
              <div className="space-y-5">
                <CardTitle title="Espaço e data" description="Somente espaços ativos e reserváveis são exibidos." />
                {reservable.length === 0 ? <Alert tone="warning">Nenhum espaço reservável cadastrado.</Alert> : spaceGroups.map((group) => {
                  const items = reservable.filter((s) => s.type === group.type)
                  if (items.length === 0) return null
                  return (
                    <div key={group.type}>
                      <p className="mb-2 text-xs font-semibold tracking-wider text-muted uppercase">{group.title}</p>
                      <div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
                        {items.map((s) => (
                          <button key={s.id} type="button" onClick={() => { selectSpace(s); setStepError(null) }} aria-pressed={s.id === spaceId}
                            className={cn('rounded-(--radius-control) border px-3 py-2.5 text-left transition-colors', s.id === spaceId ? 'border-brand-500 bg-brand-50' : 'border-line hover:bg-canvas')}>
                            <p className="truncate text-sm font-medium text-ink">{s.name}</p>
                            <p className="truncate text-xs text-muted">
                              {s.capacity != null ? `Até ${s.capacity} pessoas · ` : ''}{s.bookingMode === 'Hourly' ? `intervalos de ${s.slotMinutes} min` : `período de ${s.fixedDays} dia(s)`}
                            </p>
                          </button>
                        ))}
                      </div>
                    </div>
                  )
                })}
                {space && hourly && <Field className="w-52" label="Data" required><Input type="date" min={dates.today()} value={date} onChange={(e) => { setDate(e.target.value); setMinutes([]) }} /></Field>}
                {space && !hourly && <Alert>{space.name} é reservado por período fixo de {space.fixedDays} dia(s). A data de início é escolhida na próxima etapa.</Alert>}
                {space?.usageRules && <Alert title="Regras de uso"><span className="whitespace-pre-wrap">{space.usageRules}</span></Alert>}
              </div>
            )}

            {step === 3 && space && (
              <div className="space-y-5">
                <CardTitle title={hourly ? `Horários — ${space.name}` : `Período — ${space.name}`}
                  description={hourly ? 'Selecione um ou mais intervalos disponíveis. A disponibilidade é atualizada em tempo real.' : 'Selecione a data de início entre as datas disponíveis.'} />
                <AvailabilityPicker space={space} date={date} onDate={setDate} minutes={minutes} onMinutes={setMinutes} />
                {hourly && minutes.length > 0 && <p className="text-sm text-ink">Selecionado: <span className="tabular font-medium">{minutes.map((m) => `${fmt.minutes(m)}–${fmt.minutes(m + space.slotMinutes)}`).join(', ')}</span></p>}
              </div>
            )}

            {step === 4 && space && (
              <form className="space-y-5" noValidate onSubmit={(e) => e.preventDefault()}>
                <CardTitle title="Pessoas e convidados" description="A quantidade de pessoas inclui o responsável, dependentes e convidados." />
                <Field className="w-64" label="Quantidade de pessoas" required error={errors.peopleCount?.message} hint={space.capacity != null ? `Capacidade máxima: ${space.capacity} pessoas.` : undefined}>
                  <Input inputMode="numeric" aria-invalid={!!errors.peopleCount} {...form.register('peopleCount')} />
                </Field>
                {kind === 'Member' && (
                  <div>
                    <div className="mb-2 flex items-center justify-between">
                      <div><p className="text-[13px] font-medium text-ink">Convidados</p><p className="text-xs text-muted">O limite de convidados por associado e as taxas são aplicados pelo servidor.</p></div>
                      <Button size="sm" icon={Plus} onClick={() => guests.append({ fullName: '', document: '', phone: '' })}>Adicionar convidado</Button>
                    </div>
                    {guests.fields.length === 0 ? <p className="rounded-(--radius-control) border border-dashed border-line px-4 py-4 text-center text-[13px] text-muted">Nenhum convidado informado.</p> : (
                      <ul className="space-y-2">
                        {guests.fields.map((guest, index) => (
                          <li key={guest.id} className="grid grid-cols-12 items-start gap-3">
                            <Field className="col-span-5" error={errors.guests?.[index]?.fullName?.message}><Input placeholder="Nome completo" aria-invalid={!!errors.guests?.[index]?.fullName} {...form.register(`guests.${index}.fullName`)} /></Field>
                            <Field className="col-span-3" error={errors.guests?.[index]?.document?.message}><Input placeholder="Documento" {...form.register(`guests.${index}.document`)} /></Field>
                            <Field className="col-span-3">
                              <Controller control={form.control} name={`guests.${index}.phone`} render={({ field }) => <MaskedInput mask="phone" placeholder="Telefone" value={field.value} onChange={field.onChange} />} />
                            </Field>
                            <div className="col-span-1 pt-1"><IconButton icon={Trash2} label="Remover convidado" onClick={() => guests.remove(index)} /></div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                )}
                <Field label="Observações" error={errors.notes?.message}><Textarea rows={3} {...form.register('notes')} /></Field>
              </form>
            )}

            {step === 5 && space && quote && (
              <div className="space-y-5">
                <CardTitle title="Valor total da locação e seus componentes" description="Confira os dados antes de confirmar. A reserva feita pela equipe já entra aprovada." />
                <div className="grid gap-6 lg:grid-cols-2">
                  <dl className="grid grid-cols-2 gap-x-6 gap-y-4">
                    <Info label="Tipo"><Badge tone={labels.reservationKind[kind].tone}>{labels.reservationKind[kind].label}</Badge></Info>
                    <Info label={kind === 'Member' ? 'Associado' : 'Título'}>{kind === 'Member' ? member?.name : values.title}</Info>
                    <Info label="Espaço">{space.name}</Info>
                    <Info label="Período"><span className="tabular">{period(quote.startAt, quote.endAt)}</span></Info>
                    {hourly && <Info label="Horários" className="col-span-2"><span className="tabular">{minutes.map((m) => `${fmt.minutes(m)}–${fmt.minutes(m + space.slotMinutes)}`).join(', ')}</span></Info>}
                    <Info label="Pessoas">{values.peopleCount}</Info>
                    <Info label="Convidados">{kind === 'Member' ? values.guests.length : '—'}</Info>
                    {values.notes && <Info label="Observações" className="col-span-2"><span className="whitespace-pre-wrap">{values.notes}</span></Info>}
                  </dl>
                  <div className="rounded-(--radius-card) border border-line bg-canvas/60 p-5">
                    <dl className="space-y-2.5 text-sm">
                      <div className="flex justify-between"><dt className="text-muted">Locação</dt><dd className="tabular text-ink">{fmt.money(quote.rentalAmount)}</dd></div>
                      <div className="flex justify-between"><dt className="text-muted">Taxa de limpeza</dt><dd className="tabular text-ink">{fmt.money(quote.cleaningFee)}</dd></div>
                      <div className="flex justify-between border-t border-line pt-2.5"><dt className="font-semibold text-ink">Valor total</dt><dd className="tabular text-lg font-semibold text-ink">{fmt.money(quote.totalAmount)}</dd></div>
                      <div className="flex justify-between pt-1"><dt className="text-muted">Forma de cobrança</dt><dd className="text-ink">{quote.chargeMode ? labels.chargeMode[quote.chargeMode].label : 'Não definida'}</dd></div>
                    </dl>
                    {kind !== 'Member' && <p className="mt-3 text-xs text-muted">Reservas administrativas e eventos internos não geram cobrança: os valores acima são apenas a referência do espaço.</p>}
                  </div>
                </div>
                {quote.notes.length > 0 && <Alert><ul className="list-disc space-y-1 pl-4">{quote.notes.map((note) => <li key={note}>{note}</li>)}</ul></Alert>}
              </div>
            )}

            <footer className="mt-6 flex items-center justify-between border-t border-line pt-4">
              <Button icon={ArrowLeft} disabled={step === 1 || create.isPending} onClick={() => { setStepError(null); setStep(step - 1) }}>Voltar</Button>
              {step < 5
                ? <Button variant="primary" loading={simulate.isPending} onClick={() => void next()}>{step === 4 ? 'Calcular valores' : 'Continuar'}<ArrowRight className="size-4" /></Button>
                : <Button variant="primary" icon={Check} loading={create.isPending} onClick={() => create.mutate()}>Confirmar reserva</Button>}
            </footer>
          </Card>
        )}
      </QueryBoundary>
    </>
  )
}
