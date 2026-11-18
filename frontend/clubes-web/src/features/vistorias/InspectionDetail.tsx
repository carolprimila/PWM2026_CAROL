import { AlertTriangle, Check, X } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Attachments } from '@/components/shared'
import { Alert, Badge, Button, Info, Modal, QueryBoundary, StatusBadge } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'

type Inspection = Schema<'InspectionDto'>

export function InspectionDetail({ id, justCreated, onClose }: { id: string; justCreated?: boolean; onClose: () => void }) {
  const { can } = useAuth()
  const inspection = useQuery({ queryKey: ['vistoria', id], queryFn: () => api.get<Inspection>(`/api/vistorias/${id}`) })
  const i = inspection.data

  return (
    <Modal open onClose={onClose} size="lg" title={i ? `Vistoria — ${i.spaceName}` : 'Vistoria'} description={i ? `${labels.inspectionMoment[i.moment].label} · ${fmt.dateTime(i.performedAt)} · por ${i.inspectorName}` : undefined}
      footer={<Button variant="primary" onClick={onClose}>{justCreated ? 'Concluir' : 'Fechar'}</Button>}>
      <QueryBoundary query={inspection}>
        {(item) => (
          <div className="space-y-5">
            {justCreated && <Alert tone="success" title="Vistoria registrada">Adicione abaixo as fotos do espaço{item.hasDamage ? ' e dos danos identificados' : ''}.</Alert>}
            {item.hasDamage && (
              <Alert tone="danger" title="Danos identificados nesta vistoria">
                {item.occurrenceId ? <>
                  Uma ocorrência foi gerada automaticamente{item.reservationCode ? ` e vinculada à reserva ${item.reservationCode}` : ''} para análise administrativa.{' '}
                  {can('occurrences.view') && <Link to={`/ocorrencias?id=${item.occurrenceId}`} className="font-medium underline">Abrir ocorrência</Link>}
                </> : 'Não há ocorrência vinculada a esta vistoria.'}
              </Alert>
            )}
            <dl className="grid grid-cols-4 gap-4">
              <Info label="Espaço">{item.spaceName}</Info>
              <Info label="Momento"><StatusBadge dict={labels.inspectionMoment} value={item.moment} /></Info>
              <Info label="Reserva">
                {item.reservationId ? (can('reservations.view') ? <Link to={`/reservas/${item.reservationId}`} className="font-medium text-brand-600 hover:underline">{item.reservationCode}</Link> : item.reservationCode) : '—'}
              </Info>
              <Info label="Associado">{item.memberName || '—'}</Info>
              {item.notes && <Info label="Observações gerais" className="col-span-4"><span className="whitespace-pre-wrap">{item.notes}</span></Info>}
            </dl>

            <section>
              <h3 className="mb-2 text-xs font-semibold tracking-wider text-muted uppercase">Checklist</h3>
              <ul className="divide-y divide-line rounded-(--radius-control) border border-line">
                {item.items.map((entry, index) => (
                  <li key={index} className="flex items-start gap-3 px-4 py-2.5 text-sm">
                    <span className={entry.isDamage ? 'mt-0.5 text-danger' : entry.isOk ? 'mt-0.5 text-success' : 'mt-0.5 text-warning'}>
                      {entry.isDamage ? <AlertTriangle className="size-4" /> : entry.isOk ? <Check className="size-4" /> : <X className="size-4" />}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className="text-ink">{entry.description}</p>
                      {entry.notes && <p className="mt-0.5 text-[13px] text-ink/70">{entry.notes}</p>}
                    </div>
                    <Badge>{labels.inspectionCategory[entry.category].label}</Badge>
                    {entry.isDamage ? <Badge tone="danger">Dano</Badge> : entry.isOk ? <Badge tone="success">OK</Badge> : <Badge tone="warning">Não OK</Badge>}
                  </li>
                ))}
              </ul>
            </section>

            <section>
              <h3 className="mb-2 text-xs font-semibold tracking-wider text-muted uppercase">Fotos e anexos</h3>
              <Attachments ownerType="Inspection" ownerId={item.id} canEdit={can('inspections.manage')} />
            </section>
          </div>
        )}
      </QueryBoundary>
    </Modal>
  )
}
