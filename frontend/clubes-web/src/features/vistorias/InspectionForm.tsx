import { Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, cn, Field, IconButton, Input, Modal, Select, Textarea, useToast } from '@/components/ui'
import { api, type Paged, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { emptyToNull, fmt } from '@/lib/format'
import { labels, options } from '@/lib/labels'

type Inspection = Schema<'InspectionDto'>
type Category = Schema<'InspectionCategory'>
type State = 'ok' | 'notOk' | 'damage'
type Row = { key: number; category: Category; description: string; state: State; notes: string }

const DEFAULTS: Record<Category, string> = {
  Conservation: 'Estado geral de conservação', Cleaning: 'Limpeza do ambiente', Equipment: 'Equipamentos funcionando',
  Furniture: 'Móveis em bom estado', Utensils: 'Utensílios completos e conservados',
}
const STATES: { value: State; label: string; active: string }[] = [
  { value: 'ok', label: 'OK', active: 'bg-success-soft text-success' },
  { value: 'notOk', label: 'Não OK', active: 'bg-warning-soft text-warning' },
  { value: 'damage', label: 'Dano', active: 'bg-danger-soft text-danger' },
]

export function InspectionForm({ spaces, onClose, onSaved }: { spaces: Schema<'SpaceDto'>[]; onClose: () => void; onSaved: (saved: Inspection) => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const [spaceId, setSpaceId] = useState('')
  const [reservationId, setReservationId] = useState('')
  const [moment, setMoment] = useState<Schema<'InspectionMoment'>>('Before')
  const [notes, setNotes] = useState('')
  const [damageDescription, setDamageDescription] = useState('')
  const [rows, setRows] = useState<Row[]>(() => (Object.keys(DEFAULTS) as Category[]).map((category, key) => ({ key, category, description: DEFAULTS[category], state: 'ok', notes: '' })))
  const [nextKey, setNextKey] = useState(rows.length)
  const [submitted, setSubmitted] = useState(false)

  const reservations = useQuery({
    queryKey: ['/api/reservas', 'vistoria', spaceId], enabled: !!spaceId && can('reservations.view'),
    queryFn: () => api.get<Paged<Schema<'ReservationDto'>>>('/api/reservas', { spaceId, pageSize: 20 }),
  })
  const hasDamage = rows.some((row) => row.state === 'damage')
  const patch = (key: number, change: Partial<Row>) => setRows((list) => list.map((row) => (row.key === key ? { ...row, ...change } : row)))
  const errors = {
    space: !spaceId ? 'Selecione o espaço' : undefined,
    rows: rows.length === 0 ? 'Inclua ao menos um item no checklist' : rows.some((row) => !row.description.trim()) ? 'Preencha a descrição de todos os itens' : undefined,
    damage: hasDamage && !damageDescription.trim() ? 'Descreva os danos identificados' : undefined,
  }

  const save = useMutation({
    mutationFn: () => api.post<Inspection>('/api/vistorias', {
      spaceId, reservationId: reservationId || null, moment, notes: emptyToNull(notes), damageDescription: hasDamage ? emptyToNull(damageDescription) : null,
      items: rows.map((row) => ({ category: row.category, description: row.description.trim(), isOk: row.state === 'ok', isDamage: row.state === 'damage', notes: emptyToNull(row.notes) })),
    } satisfies Schema<'CreateInspectionRequest'>),
    onSuccess: (saved) => {
      toast.success(saved.hasDamage ? 'Vistoria registrada. Uma ocorrência de dano foi gerada para análise.' : 'Vistoria registrada.')
      void queryClient.invalidateQueries({ queryKey: ['/api/vistorias'] })
      void queryClient.invalidateQueries({ queryKey: ['/api/ocorrencias'] })
      onSaved(saved)
    },
    onError: toast.error,
  })
  const submit = () => { setSubmitted(true); if (!errors.space && !errors.rows && !errors.damage) save.mutate() }

  return (
    <Modal open onClose={onClose} size="xl" title="Nova vistoria" description="Registre o estado do espaço. As fotos podem ser anexadas logo após salvar."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={submit}>Registrar vistoria</Button>
      </>}>
      <div className="space-y-5">
        <div className="grid grid-cols-3 gap-4">
          <Field label="Espaço" required error={submitted ? errors.space : undefined}>
            <Select aria-invalid={submitted && !!errors.space} value={spaceId} onChange={(e) => { setSpaceId(e.target.value); setReservationId('') }}>
              <option value="">Selecione…</option>
              {spaces.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </Select>
          </Field>
          <Field label="Reserva" hint={!can('reservations.view') ? 'Requer a permissão de visualizar reservas.' : !spaceId ? 'Selecione o espaço para listar as reservas recentes.' : 'Opcional. Vincula a vistoria ao associado da reserva.'}>
            <Select disabled={!spaceId || !can('reservations.view')} value={reservationId} onChange={(e) => setReservationId(e.target.value)}>
              <option value="">{reservations.isLoading ? 'Carregando…' : 'Sem reserva vinculada'}</option>
              {(reservations.data?.items ?? []).map((r) => <option key={r.id} value={r.id}>{r.code} — {r.memberName ?? r.title ?? labels.reservationKind[r.kind].label} — {fmt.dateTime(r.startAt)}</option>)}
            </Select>
          </Field>
          <Field label="Momento">
            <Select value={moment} onChange={(e) => setMoment(e.target.value as Schema<'InspectionMoment'>)}>{options(labels.inspectionMoment).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select>
          </Field>
        </div>

        <fieldset>
          <legend className="mb-2 text-xs font-semibold tracking-wider text-muted uppercase">Checklist</legend>
          <div className="divide-y divide-line rounded-(--radius-control) border border-line">
            {rows.map((row) => (
              <div key={row.key} className="flex items-center gap-2 px-3 py-2.5">
                <div className="w-40 shrink-0"><Select value={row.category} onChange={(e) => patch(row.key, { category: e.target.value as Category })}>{options(labels.inspectionCategory).map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</Select></div>
                <div className="min-w-0 flex-1"><Input maxLength={200} placeholder="O que foi verificado" aria-invalid={submitted && !row.description.trim()} value={row.description} onChange={(e) => patch(row.key, { description: e.target.value })} /></div>
                <div className="shrink-0">
                  <div className="inline-flex rounded-(--radius-control) border border-line bg-canvas p-0.5">
                    {STATES.map((state) => (
                      <button key={state.value} type="button" onClick={() => patch(row.key, { state: state.value })}
                        className={cn('rounded-lg px-2.5 py-1.5 text-xs font-medium whitespace-nowrap transition-colors', row.state === state.value ? `${state.active} shadow-sm` : 'text-muted hover:text-ink')}>{state.label}</button>
                    ))}
                  </div>
                </div>
                <div className="w-56 shrink-0"><Input maxLength={500} placeholder={row.state === 'ok' ? 'Observações' : 'Descreva o problema'} value={row.notes} onChange={(e) => patch(row.key, { notes: e.target.value })} /></div>
                <div className="shrink-0"><IconButton icon={Trash2} label="Remover item" onClick={() => setRows((list) => list.filter((r) => r.key !== row.key))} /></div>
              </div>
            ))}
            {rows.length === 0 && <p className="px-3 py-6 text-center text-sm text-muted">Nenhum item no checklist.</p>}
          </div>
          {submitted && errors.rows && <p className="mt-1.5 text-xs text-danger">{errors.rows}</p>}
          <Button size="sm" icon={Plus} className="mt-3" onClick={() => { setRows((list) => [...list, { key: nextKey, category: 'Conservation', description: '', state: 'ok', notes: '' }]); setNextKey(nextKey + 1) }}>Adicionar item</Button>
        </fieldset>

        {hasDamage && (
          <div className="space-y-3">
            <Alert tone="danger" title="Dano identificado">
              Ao salvar, uma <strong>ocorrência será gerada automaticamente</strong>{reservationId ? ', vinculada à reserva e ao associado,' : ''} para análise administrativa. Nenhuma cobrança é lançada sem aprovação.
            </Alert>
            <Field label="Descrição dos danos" required error={submitted ? errors.damage : undefined}>
              <Textarea rows={3} maxLength={2000} aria-invalid={submitted && !!errors.damage} placeholder="Descreva o que foi danificado, a extensão e as circunstâncias." value={damageDescription} onChange={(e) => setDamageDescription(e.target.value)} />
            </Field>
          </div>
        )}
        <Field label="Observações gerais"><Textarea rows={2} maxLength={2000} value={notes} onChange={(e) => setNotes(e.target.value)} /></Field>
      </div>
    </Modal>
  )
}
