import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Clock } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { Feature } from '@/app/features'
import { Alert, Badge, Button, Card, CardTitle, Input, PageHeader, QueryBoundary, Select, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { dayOfWeek, daysOfWeek } from '@/lib/labels'
import { clubBody, clubDefaults, ClubFields, clubSchema, type ClubValues } from './ClubFields'

type Club = Schema<'ClubDto'>
type OpeningHour = Schema<'OpeningHourDto'>

function ClubPage() {
  const club = useQuery({ queryKey: ['clube-atual'], queryFn: () => api.get<Club>('/api/clubes/atual') })
  return (
    <>
      <PageHeader title="Dados do clube" subtitle="Informações institucionais e horários de funcionamento." />
      <QueryBoundary query={club}>
        {(c) => (
          <div className="grid items-start gap-6 xl:grid-cols-3">
            <ClubForm key={c.id} club={c} />
            <OpeningHours />
          </div>
        )}
      </QueryBoundary>
    </>
  )
}

function ClubForm({ club }: { club: Club }) {
  const { reload } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<ClubValues>({ resolver: zodResolver(clubSchema), defaultValues: clubDefaults(club) })
  const save = useMutation({
    mutationFn: (values: ClubValues) => api.put<Club>(`/api/clubes/${club.id}`, clubBody(values)),
    onSuccess: (saved) => {
      toast.success('Dados do clube atualizados.')
      queryClient.setQueryData(['clube-atual'], saved)
      void queryClient.invalidateQueries({ queryKey: ['plataforma', 'clubes'] })
      form.reset(clubDefaults(saved))
      void reload().catch(() => undefined) // nome e logotipo no menu lateral
    },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof ClubValues, { message: messages[0] })
      else toast.error(error)
    },
  })
  return (
    <Card className="xl:col-span-2">
      <CardTitle icon={Building2} title="Dados institucionais" description={<>Identificador na plataforma: <span className="font-mono">{club.slug}</span> · cadastrado em {fmt.date(club.createdAt)}</>} />
      <form noValidate onSubmit={form.handleSubmit((values) => save.mutate(values))}>
        <ClubFields form={form} withLogo />
        <div className="mt-6 flex items-center justify-end gap-2 border-t border-line pt-4">
          <Button disabled={!form.formState.isDirty || save.isPending} onClick={() => form.reset(clubDefaults(club))}>Descartar</Button>
          <Button type="submit" variant="primary" disabled={!form.formState.isDirty} loading={save.isPending}>Salvar alterações</Button>
        </div>
      </form>
    </Card>
  )
}

type DayMode = 'undefined' | 'open' | 'closed'
type DayRow = { mode: DayMode; open: string; close: string }

const toMinutes = (time: string) => { const [h, m] = time.split(':').map(Number); return h * 60 + m }
/** Dia ausente na resposta = horário ainda não definido (nenhum valor é presumido). */
const toRows = (hours: OpeningHour[]): Record<string, DayRow> => Object.fromEntries(daysOfWeek.map((day) => {
  const hour = hours.find((h) => h.dayOfWeek === day)
  return [day, !hour ? { mode: 'undefined', open: '', close: '' } : hour.isClosed ? { mode: 'closed', open: '', close: '' } : { mode: 'open', open: fmt.minutes(hour.openMinute), close: fmt.minutes(hour.closeMinute) }]
}))

function OpeningHours() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const manage = can('settings.manage')
  const hours = useQuery({ queryKey: ['clube-horarios'], queryFn: () => api.get<OpeningHour[]>('/api/clubes/atual/horarios') })
  const [rows, setRows] = useState<Record<string, DayRow> | null>(null)
  const [errors, setErrors] = useState<Record<string, string>>({})
  useEffect(() => { if (hours.data) setRows(toRows(hours.data)) }, [hours.data])
  const dirty = !!rows && !!hours.data && JSON.stringify(rows) !== JSON.stringify(toRows(hours.data))
  const set = (day: string, patch: Partial<DayRow>) => { setRows((current) => current && { ...current, [day]: { ...current[day], ...patch } }); setErrors((current) => ({ ...current, [day]: '' })) }

  const save = useMutation({
    mutationFn: (body: OpeningHour[]) => api.put<OpeningHour[]>('/api/clubes/atual/horarios', body),
    onSuccess: (saved) => { toast.success('Horários de funcionamento atualizados.'); queryClient.setQueryData(['clube-horarios'], saved) },
    onError: toast.error,
  })
  const submit = () => {
    if (!rows) return
    const problems: Record<string, string> = {}
    const body: OpeningHour[] = []
    for (const day of daysOfWeek) {
      const row = rows[day]
      if (row.mode === 'undefined') continue
      if (row.mode === 'closed') { body.push({ dayOfWeek: day, isClosed: true, openMinute: 0, closeMinute: 0 }); continue }
      if (!row.open || !row.close) { problems[day] = 'Informe a abertura e o fechamento'; continue }
      const openMinute = toMinutes(row.open)
      const closeMinute = toMinutes(row.close) || 1440 // 00:00 no fechamento = meia-noite (fim do dia)
      if (closeMinute <= openMinute) { problems[day] = 'O fechamento deve ser posterior à abertura'; continue }
      body.push({ dayOfWeek: day, isClosed: false, openMinute, closeMinute })
    }
    setErrors(problems)
    if (Object.keys(problems).length === 0) save.mutate(body)
  }

  return (
    <Card>
      <CardTitle icon={Clock} title="Horários de funcionamento" description="Horário geral do clube em cada dia da semana." />
      <QueryBoundary query={hours}>
        {(data) => !rows ? null : (
          <>
            {data.length < 7 && <Alert tone="warning" className="mb-4">Há dias sem horário definido. Defina-os para que o funcionamento seja aplicado às regras do clube.</Alert>}
            <ul className="divide-y divide-line">
              {daysOfWeek.map((day) => {
                const row = rows[day]
                return (
                  <li key={day} className="py-3">
                    <div className="flex items-center gap-3">
                      <span className="w-28 shrink-0 text-sm font-medium text-ink">{dayOfWeek[day].label}</span>
                      {manage ? (
                        <div className="min-w-0 flex-1"><Select className="h-9" value={row.mode} onChange={(e) => set(day, { mode: e.target.value as DayMode })}><option value="undefined">Não definido</option><option value="open">Aberto</option><option value="closed">Fechado</option></Select></div>
                      ) : row.mode === 'open' ? <span className="tabular text-sm text-ink">{row.open} – {row.close}</span> : <Badge tone={row.mode === 'closed' ? 'neutral' : 'warning'}>{row.mode === 'closed' ? 'Fechado' : 'Não definido'}</Badge>}
                    </div>
                    {manage && row.mode === 'open' && (
                      <div className="mt-2 flex items-center gap-2">
                        <Input type="time" className="h-9 min-w-0 flex-1" aria-label={`Abertura — ${dayOfWeek[day].label}`} aria-invalid={!!errors[day]} value={row.open} onChange={(e) => set(day, { open: e.target.value })} />
                        <span className="text-sm text-muted">às</span>
                        <Input type="time" className="h-9 min-w-0 flex-1" aria-label={`Fechamento — ${dayOfWeek[day].label}`} aria-invalid={!!errors[day]} value={row.close} onChange={(e) => set(day, { close: e.target.value })} />
                      </div>
                    )}
                    {errors[day] && <p className="mt-1 text-xs text-danger">{errors[day]}</p>}
                  </li>
                )
              })}
            </ul>
            {manage ? (
              <div className="mt-4 flex items-center justify-between gap-2 border-t border-line pt-4">
                <span className="text-xs text-muted">Fechamento às 00:00 significa meia-noite.</span>
                <Button variant="primary" size="sm" disabled={!dirty} loading={save.isPending} onClick={submit}>Salvar horários</Button>
              </div>
            ) : <p className="mt-4 border-t border-line pt-4 text-xs text-muted">Somente quem pode alterar configurações edita os horários.</p>}
          </>
        )}
      </QueryBoundary>
    </Card>
  )
}

const feature: Feature = {
  nav: [{ to: '/clube', label: 'Dados do clube', icon: Building2, group: 'Administração', order: 3, permissions: ['club.manage'] }],
  routes: [{ path: 'clube', component: ClubPage, permissions: ['club.manage'] }],
}
export default feature
