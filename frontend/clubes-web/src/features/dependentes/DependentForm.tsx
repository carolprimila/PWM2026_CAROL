import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { MemberPicker } from '@/components/shared'
import { Alert, Avatar, Button, Field, FileUpload, Input, MaskedInput, Modal, Select, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { dates, emptyToNull, masks } from '@/lib/format'
import { isValidCpf } from './cpf'

type Dependent = Schema<'DependentDto'>

const schema = z.object({
  fullName: z.string().trim().min(3, 'Informe o nome completo').max(150, 'Máximo de 150 caracteres'),
  relationship: z.string().trim().min(1, 'Informe o parentesco').max(50, 'Máximo de 50 caracteres'),
  cpf: z.string().refine((v) => v === '' || isValidCpf(v), 'CPF inválido'),
  rg: z.string().max(20, 'Máximo de 20 caracteres'),
  birthDate: z.string().refine((v) => !v || v <= dates.today(), 'Data de nascimento inválida'),
  gender: z.string(),
  email: z.union([z.literal(''), z.string().email('E-mail inválido')]),
  phone: z.string(),
  photoFileId: z.string().nullable(),
})
type Values = z.infer<typeof schema>

const RELATIONSHIPS = ['Cônjuge', 'Companheiro(a)', 'Filho(a)', 'Enteado(a)', 'Pai', 'Mãe', 'Irmão(ã)', 'Neto(a)', 'Outro']

/** Cadastro e edição de dependente. No cadastro pela equipe o dependente já entra ativo; o servidor valida o limite por associado. */
export function DependentForm({ dependent, member: preset, onClose, onSaved }: {
  dependent?: Dependent; member?: { id: string; name: string } | null; onClose: () => void; onSaved: (dependent: Dependent) => void
}) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const [member, setMember] = useState<{ id: string; name: string } | null>(dependent ? { id: dependent.memberId, name: dependent.memberName } : preset ?? null)
  const [memberError, setMemberError] = useState('')
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      fullName: dependent?.fullName ?? '', relationship: dependent?.relationship ?? '', cpf: masks.cpf(dependent?.cpf ?? ''), rg: dependent?.rg ?? '',
      birthDate: dependent?.birthDate ?? '', gender: dependent?.gender ?? '', email: dependent?.email ?? '', phone: masks.phone(dependent?.phone ?? ''),
      photoFileId: dependent?.photoFileId ?? null,
    },
  })
  const { errors } = form.formState

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body = {
        fullName: values.fullName, relationship: values.relationship, cpf: emptyToNull(values.cpf), rg: emptyToNull(values.rg), birthDate: emptyToNull(values.birthDate),
        gender: emptyToNull(values.gender), email: emptyToNull(values.email), phone: emptyToNull(values.phone), photoFileId: values.photoFileId,
      }
      return dependent ? api.put<Dependent>(`/api/dependentes/${dependent.id}`, body) : api.post<Dependent>('/api/dependentes', body, { memberId: member!.id })
    },
    onSuccess: (saved) => {
      toast.success(dependent ? 'Cadastro atualizado.' : 'Dependente cadastrado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/dependentes'] })
      void queryClient.invalidateQueries({ queryKey: ['dependente', saved.id] })
      void queryClient.invalidateQueries({ queryKey: ['associado', saved.memberId] })
      onSaved(saved)
    },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
    },
  })
  const submit = form.handleSubmit((values) => {
    if (!member) { setMemberError('Selecione o associado responsável'); return }
    save.mutate(values)
  })

  return (
    <Modal open onClose={onClose} size="lg" title={dependent ? 'Editar dependente' : 'Novo dependente'}
      description={dependent ? undefined : 'Cadastrado pela equipe, o dependente já entra ativo. O limite de dependentes por associado é verificado pelo servidor.'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={submit}>{dependent ? 'Salvar alterações' : 'Cadastrar'}</Button>
      </>}>
      <form className="space-y-6" noValidate onSubmit={(e) => e.preventDefault()}>
        {save.error && !(save.error instanceof ApiError && save.error.errors) ? <Alert tone="danger">{(save.error as Error).message}</Alert> : null}

        <Field label="Associado responsável" required error={memberError}>
          {dependent
            ? <Input value={dependent.memberName} disabled readOnly />
            : <MemberPicker value={member} onChange={(m) => { setMember(m); setMemberError('') }} onlyActive={false} />}
        </Field>

        <div className="flex items-center gap-4">
          <Avatar fileId={form.watch('photoFileId')} name={form.watch('fullName')} size={64} />
          {member
            ? <FileUpload size="sm" label="Enviar fotografia" accept="image/jpeg,image/png,image/webp" scope="Staff" ownerMemberId={member.id} onUploaded={(file) => form.setValue('photoFileId', file.id, { shouldDirty: true })} />
            : <p className="text-[13px] text-muted">Selecione o associado responsável para enviar a fotografia.</p>}
        </div>

        <div className="grid grid-cols-6 gap-4">
          <Field className="col-span-4" label="Nome completo" required error={errors.fullName?.message}><Input aria-invalid={!!errors.fullName} {...form.register('fullName')} /></Field>
          <Field className="col-span-2" label="Parentesco" required error={errors.relationship?.message}>
            <Input list="dependent-relationships" aria-invalid={!!errors.relationship} placeholder="Ex.: Filho(a)" {...form.register('relationship')} />
            <datalist id="dependent-relationships">{RELATIONSHIPS.map((r) => <option key={r} value={r} />)}</datalist>
          </Field>
          <Field className="col-span-2" label="CPF" error={errors.cpf?.message}>
            <Controller control={form.control} name="cpf" render={({ field }) => <MaskedInput mask="cpf" value={field.value} onChange={field.onChange} aria-invalid={!!errors.cpf} placeholder="000.000.000-00" />} />
          </Field>
          <Field className="col-span-2" label="RG" error={errors.rg?.message}><Input {...form.register('rg')} /></Field>
          <Field className="col-span-2" label="Data de nascimento" error={errors.birthDate?.message}><Input type="date" aria-invalid={!!errors.birthDate} {...form.register('birthDate')} /></Field>
          <Field className="col-span-2" label="Gênero">
            <Select {...form.register('gender')}><option value="">Não informado</option><option>Feminino</option><option>Masculino</option><option>Outro</option></Select>
          </Field>
          <Field className="col-span-2" label="E-mail" error={errors.email?.message} hint="Usado para o acesso ao aplicativo."><Input type="email" aria-invalid={!!errors.email} {...form.register('email')} /></Field>
          <Field className="col-span-2" label="Telefone">
            <Controller control={form.control} name="phone" render={({ field }) => <MaskedInput mask="phone" value={field.value} onChange={field.onChange} placeholder="(00) 00000-0000" />} />
          </Field>
        </div>
      </form>
    </Modal>
  )
}
