import { zodResolver } from '@hookform/resolvers/zod'
import { MoreHorizontal, Receipt, ShieldCheck, ShieldOff, UserRound } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { MemberPicker } from '@/components/shared'
import { DataTable, Toolbar, type Column } from '@/components/table'
import { Alert, Badge, Button, Card, Field, IconButton, Input, Menu, Modal, PageHeader, SearchInput, Tabs, Textarea, useConfirm, useToast } from '@/components/ui'
import { api, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { dates, fmt } from '@/lib/format'
import { MemberLink, useInvalidateFinance } from './common'

type Delinquent = Schema<'DelinquentDto'>
type Release = Schema<'ReleaseDto'>
type Tab = 'inadimplentes' | 'liberacoes'
type Picked = { id: string; name: string }

export default function DelinquencyPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const toast = useToast()
  const confirm = useConfirm()
  const invalidate = useInvalidateFinance()
  const canRelease = can('finance.release')
  const [tab, setTab] = useState<Tab>('inadimplentes')
  const [search, setSearch] = useState('')
  const [onlyActive, setOnlyActive] = useState(false)
  const [releasing, setReleasing] = useState<{ member: Picked | null } | null>(null)

  const delinquents = useQuery({ queryKey: ['/api/financeiro/inadimplentes'], queryFn: () => api.get<Delinquent[]>('/api/financeiro/inadimplentes') })
  const releases = useQuery({ queryKey: ['/api/financeiro/liberacoes'], queryFn: () => api.get<Release[]>('/api/financeiro/liberacoes') })
  const revoke = useMutation({
    mutationFn: (id: string) => api.delete(`/api/financeiro/liberacoes/${id}`),
    onSuccess: () => { toast.success('Liberação revogada.'); invalidate() },
    onError: toast.error,
  })

  const term = search.trim().toLocaleLowerCase('pt-BR')
  const rows = (delinquents.data ?? []).filter((d) => !term || d.fullName.toLocaleLowerCase('pt-BR').includes(term) || String(d.number).includes(term) || (d.email ?? '').toLowerCase().includes(term))
  const releaseRows = (releases.data ?? []).filter((r) => (!onlyActive || r.isActive) && (!term || r.memberName.toLocaleLowerCase('pt-BR').includes(term)))
  const totalOverdue = (delinquents.data ?? []).reduce((total, d) => total + d.overdueAmount, 0)

  const delinquentColumns: Column<Delinquent>[] = [
    { key: 'member', header: 'Associado', render: (d) => <MemberLink id={d.memberId} name={d.fullName} number={d.number} /> },
    { key: 'contact', header: 'Contato', render: (d) => <div><p>{fmt.phone(d.phone)}</p><p className="text-xs text-muted">{d.email ?? 'Sem e-mail'}</p></div> },
    { key: 'count', header: 'Cobranças em atraso', align: 'center', render: (d) => d.overdueCharges },
    { key: 'amount', header: 'Valor em atraso', align: 'right', render: (d) => <span className="font-medium text-danger">{fmt.money(d.overdueAmount)}</span> },
    { key: 'oldest', header: 'Mais antiga', render: (d) => <span className="tabular">{fmt.day(d.oldestDueDate)}</span> },
    { key: 'release', header: 'Liberação', render: (d) => (d.hasActiveRelease ? <Badge tone="info">Liberação ativa</Badge> : <span className="text-muted">—</span>) },
  ]
  const releaseColumns: Column<Release>[] = [
    { key: 'member', header: 'Associado', render: (r) => <MemberLink id={r.memberId} name={r.memberName} /> },
    { key: 'reason', header: 'Justificativa', render: (r) => <span className="line-clamp-2 max-w-md text-ink/80">{r.reason}</span> },
    { key: 'valid', header: 'Válida até', render: (r) => <span className="tabular">{fmt.day(r.validUntil)}</span> },
    { key: 'granted', header: 'Concedida', render: (r) => <div><p className="tabular">{fmt.dateTime(r.grantedAt)}</p><p className="text-xs text-muted">{r.grantedBy ?? '—'}</p></div> },
    {
      key: 'status', header: 'Situação',
      render: (r) => (r.isActive ? <Badge tone="success">Ativa</Badge> : r.revokedAt ? <span title={`Revogada em ${fmt.dateTime(r.revokedAt)}`}><Badge tone="danger">Revogada</Badge></span> : <Badge>Expirada</Badge>),
    },
  ]

  return (
    <>
      <PageHeader title="Inadimplência" subtitle="Associados com cobranças vencidas além da tolerância e liberações excepcionais."
        actions={canRelease && <Button variant="primary" icon={ShieldCheck} onClick={() => setReleasing({ member: null })}>Liberar excepcionalmente</Button>} />

      <Alert tone="info" className="mb-6">
        Os bloqueios de acesso e de reservas para inadimplentes só são aplicados quando o clube ativa a política correspondente em{' '}
        <Link to="/configuracoes" className="font-medium underline">Configurações</Link>. A liberação excepcional suspende esses bloqueios para o associado até a data de validade.
      </Alert>

      <Tabs<Tab> value={tab} onChange={setTab} tabs={[
        { key: 'inadimplentes', label: 'Inadimplentes', count: delinquents.data?.length },
        { key: 'liberacoes', label: 'Liberações', count: releases.data?.filter((r) => r.isActive).length },
      ]} />

      {tab === 'inadimplentes' && (
        <Card padded={false}>
          <Toolbar actions={delinquents.data && delinquents.data.length > 0 && <span className="text-sm text-muted">Total em atraso: <strong className="tabular text-danger">{fmt.money(totalOverdue)}</strong></span>}>
            <SearchInput value={search} onChange={setSearch} placeholder="Nome, matrícula ou e-mail…" />
          </Toolbar>
          <DataTable columns={delinquentColumns} rows={rows} rowKey={(d) => d.memberId} isLoading={delinquents.isLoading} error={delinquents.error} onRetry={() => delinquents.refetch()}
            rowActions={(d) => (
              <Menu trigger={<IconButton icon={MoreHorizontal} label="Ações" />} items={[
                { label: 'Ver cobranças vencidas', icon: Receipt, onClick: () => navigate(`/financeiro/cobrancas?memberId=${d.memberId}&overdue=true`) },
                { label: 'Abrir cadastro', icon: UserRound, onClick: () => navigate(`/associados/${d.memberId}`) },
                canRelease && !d.hasActiveRelease && { label: 'Liberar excepcionalmente', icon: ShieldCheck, onClick: () => setReleasing({ member: { id: d.memberId, name: d.fullName } }) },
              ]} />
            )}
            empty={{ title: term ? 'Nenhum inadimplente encontrado' : 'Nenhum associado inadimplente', description: term ? 'Ajuste a pesquisa.' : 'Não há cobranças vencidas além da tolerância.' }} />
        </Card>
      )}

      {tab === 'liberacoes' && (
        <Card padded={false}>
          <Toolbar>
            <SearchInput value={search} onChange={setSearch} placeholder="Associado…" />
            <label className="inline-flex cursor-pointer items-center gap-2 text-sm text-ink"><input type="checkbox" className="size-4 rounded border-line accent-brand-600" checked={onlyActive} onChange={(e) => setOnlyActive(e.target.checked)} />Somente ativas</label>
          </Toolbar>
          <DataTable columns={releaseColumns} rows={releaseRows} rowKey={(r) => r.id} isLoading={releases.isLoading} error={releases.error} onRetry={() => releases.refetch()}
            rowActions={(r) => canRelease && r.isActive && (
              <IconButton icon={ShieldOff} label="Revogar liberação" disabled={revoke.isPending} onClick={async () => {
                if (await confirm({ title: 'Revogar liberação?', message: <>A liberação de <strong>{r.memberName}</strong> deixará de valer imediatamente e os bloqueios por inadimplência voltam a ser aplicados conforme a política do clube.</>, danger: true, confirmLabel: 'Revogar' })) revoke.mutate(r.id)
              }} />
            )}
            empty={{ title: 'Nenhuma liberação encontrada', description: term || onlyActive ? 'Ajuste a pesquisa ou o filtro.' : 'Liberações excepcionais concedidas aparecem aqui (as 200 mais recentes).' }} />
        </Card>
      )}

      {releasing && <ReleaseForm member={releasing.member} onClose={() => setReleasing(null)} />}
    </>
  )
}

const schema = z.object({
  member: z.object({ id: z.string(), name: z.string() }).nullable().refine((v) => v != null, 'Selecione o associado'),
  reason: z.string().trim().min(5, 'Informe a justificativa (mínimo de 5 caracteres)').max(500, 'Máximo de 500 caracteres'),
  validUntil: z.string().min(1, 'Informe a validade').refine((v) => !v || v >= dates.today(), 'A validade não pode ser uma data passada'),
})
type Values = z.input<typeof schema>

function ReleaseForm({ member, onClose }: { member: Picked | null; onClose: () => void }) {
  const toast = useToast()
  const invalidate = useInvalidateFinance()
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { member, reason: '', validUntil: dates.addDays(dates.today(), 7) } })
  const { errors } = form.formState
  const grant = useMutation({
    mutationFn: (values: Values) => api.post<Release>('/api/financeiro/liberacoes', { memberId: values.member!.id, reason: values.reason.trim(), validUntil: values.validUntil } satisfies Schema<'GrantReleaseRequest'>),
    onSuccess: (release) => { toast.success(`Liberação concedida a ${release.memberName} até ${fmt.day(release.validUntil)}.`); invalidate(); onClose() },
    onError: toast.error,
  })
  return (
    <Modal open onClose={onClose} title="Liberação excepcional" description="Suspende temporariamente os bloqueios por inadimplência para o associado. A dívida continua em aberto."
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={grant.isPending} onClick={form.handleSubmit((values) => grant.mutate(values))}>Conceder liberação</Button>
      </>}>
      <form className="grid grid-cols-2 gap-4" noValidate onSubmit={(e) => e.preventDefault()}>
        <Field className="col-span-2" label="Associado" required error={errors.member?.message}>
          <Controller control={form.control} name="member" render={({ field }) => <MemberPicker value={field.value} onChange={field.onChange} onlyActive={false} />} />
        </Field>
        <Field label="Válida até" required error={errors.validUntil?.message} hint="Inclui o dia informado."><Input type="date" min={dates.today()} aria-invalid={!!errors.validUntil} {...form.register('validUntil')} /></Field>
        <Field className="col-span-2" label="Justificativa" required error={errors.reason?.message} hint="Fica registrada na auditoria.">
          <Textarea rows={3} aria-invalid={!!errors.reason} placeholder="Ex.: acordo de parcelamento firmado com a diretoria" {...form.register('reason')} />
        </Field>
      </form>
    </Modal>
  )
}
