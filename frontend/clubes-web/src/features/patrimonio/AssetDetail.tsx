import { PackageX, Pencil } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Attachments, HistoryList } from '@/components/shared'
import { Button, Info, Modal, QueryBoundary, StatusBadge, Tabs, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'
import { labels } from '@/lib/labels'
import { AssetForm } from './AssetForm'

type Asset = Schema<'AssetDto'>
type Tab = 'dados' | 'historico' | 'anexos'

export function AssetDetail({ id, categories, onClose }: { id: string; categories: string[]; onClose: () => void }) {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const toast = useToast()
  const confirm = useConfirm()
  const [tab, setTab] = useState<Tab>('dados')
  const [editing, setEditing] = useState(false)
  const asset = useQuery({ queryKey: ['patrimonio', id], queryFn: () => api.get<Asset>(`/api/patrimonio/${id}`) })
  const writeOff = useMutation({
    mutationFn: (reason: string) => api.post(`/api/patrimonio/${id}/baixar`, { reason }),
    onSuccess: () => { toast.success('Baixa do bem registrada.'); void queryClient.invalidateQueries({ queryKey: ['/api/patrimonio'] }); onClose() },
    onError: toast.error,
  })
  const a = asset.data
  const manage = can('assets.manage')

  if (editing && a) return <AssetForm asset={a} categories={categories} onClose={() => setEditing(false)} onSaved={() => setEditing(false)} />
  return (
    <Modal open onClose={onClose} size="lg" title={a ? `${a.code} — ${a.name}` : 'Bem patrimonial'}
      footer={<>
        {manage && a && <>
          <Button variant="ghost" icon={PackageX} className="mr-auto text-danger hover:text-danger" loading={writeOff.isPending} onClick={async () => {
            const result = await confirm({ title: 'Dar baixa no bem', message: `"${a.name}" sairá do patrimônio ativo. A justificativa fica registrada na auditoria.`, reason: 'required', reasonLabel: 'Justificativa da baixa', danger: true, confirmLabel: 'Dar baixa' })
            if (result) writeOff.mutate(result.reason)
          }}>Dar baixa</Button>
          <Button icon={Pencil} onClick={() => setEditing(true)}>Editar</Button>
        </>}
        <Button variant="primary" onClick={onClose}>Fechar</Button>
      </>}>
      <QueryBoundary query={asset}>
        {(item) => (
          <>
            <Tabs<Tab> value={tab} onChange={setTab} tabs={[{ key: 'dados', label: 'Dados' }, { key: 'historico', label: 'Histórico' }, { key: 'anexos', label: 'Anexos' }]} />
            {tab === 'dados' && (
              <dl className="grid grid-cols-3 gap-x-6 gap-y-4">
                <Info label="Identificação">{item.code}</Info>
                <Info label="Categoria">{item.category || '—'}</Info>
                <Info label="Estado de conservação"><StatusBadge dict={labels.assetCondition} value={item.condition} /></Info>
                <Info label="Espaço">{item.spaceName || '—'}</Info>
                <Info label="Localização">{item.location || '—'}</Info>
                <Info label="Responsável">{item.responsibleName || '—'}</Info>
                <Info label="Data de aquisição">{fmt.day(item.acquiredOn)}</Info>
                <Info label="Valor">{fmt.money(item.value)}</Info>
                <Info label="Manutenções relacionadas">
                  {item.maintenances === 0 ? 'Nenhuma' : <span className="inline-flex items-center gap-2">{item.maintenances} manutenção(ões){can('maintenance.view') && <Link to="/manutencoes" className="text-[13px] font-medium text-brand-600 hover:underline">Ver manutenções</Link>}</span>}
                </Info>
                <Info label="Descrição" className="col-span-3"><span className="whitespace-pre-wrap">{item.description || '—'}</span></Info>
                <Info label="Cadastrado em">{fmt.dateTime(item.createdAt)}</Info>
              </dl>
            )}
            {tab === 'historico' && <HistoryList entityType="Asset" id={item.id} labelOf={(s) => labels.assetCondition[s as keyof typeof labels.assetCondition]?.label ?? s} />}
            {tab === 'anexos' && <Attachments ownerType="Asset" ownerId={item.id} canEdit={manage} />}
          </>
        )}
      </QueryBoundary>
    </Modal>
  )
}
