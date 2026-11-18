import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Avatar, Button, Field, FileUpload, Input, MaskedInput, Modal, Select, Textarea, useToast } from '@/components/ui'
import { api, ApiError, type Schema } from '@/lib/api'
import { emptyToNull, masks } from '@/lib/format'

const optional = z.string().optional().default('')
const schema = z.object({
  fullName: z.string().trim().min(3, 'Informe o nome completo'),
  cpf: z.string().refine((v) => v.replace(/\D/g, '').length === 11, 'Informe os 11 dígitos do CPF'),
  rg: optional,
  birthDate: optional,
  gender: optional,
  maritalStatus: optional,
  email: z.union([z.literal(''), z.string().email('E-mail inválido')]),
  phone: optional,
  zipCode: optional,
  street: optional,
  addressNumber: optional,
  complement: optional,
  district: optional,
  city: optional,
  state: z.string().max(2, 'Use a sigla (UF)').optional().default(''),
  joinedAt: optional,
  notes: optional,
  photoFileId: z.string().nullable(),
})
type Values = z.input<typeof schema>

const UF = ['AC', 'AL', 'AP', 'AM', 'BA', 'CE', 'DF', 'ES', 'GO', 'MA', 'MT', 'MS', 'MG', 'PA', 'PB', 'PR', 'PE', 'PI', 'RJ', 'RN', 'RS', 'RO', 'RR', 'SC', 'SP', 'SE', 'TO']

export function MemberForm({ member, onClose, onSaved }: { member?: Schema<'MemberDto'>; onClose: () => void; onSaved: (member: Schema<'MemberDto'>) => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      fullName: member?.fullName ?? '', cpf: masks.cpf(member?.cpf ?? ''), rg: member?.rg ?? '', birthDate: member?.birthDate ?? '', gender: member?.gender ?? '',
      maritalStatus: member?.maritalStatus ?? '', email: member?.email ?? '', phone: masks.phone(member?.phone ?? ''), zipCode: masks.cep(member?.zipCode ?? ''),
      street: member?.street ?? '', addressNumber: member?.addressNumber ?? '', complement: member?.complement ?? '', district: member?.district ?? '',
      city: member?.city ?? '', state: member?.state ?? '', joinedAt: member?.joinedAt ?? '', notes: member?.notes ?? '', photoFileId: member?.photoFileId ?? null,
    },
  })
  const { errors } = form.formState

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body = {
        fullName: values.fullName, cpf: values.cpf, rg: emptyToNull(values.rg), birthDate: emptyToNull(values.birthDate), gender: emptyToNull(values.gender),
        maritalStatus: emptyToNull(values.maritalStatus), email: emptyToNull(values.email), phone: emptyToNull(values.phone), zipCode: emptyToNull(values.zipCode),
        street: emptyToNull(values.street), addressNumber: emptyToNull(values.addressNumber), complement: emptyToNull(values.complement), district: emptyToNull(values.district),
        city: emptyToNull(values.city), state: emptyToNull(values.state), joinedAt: emptyToNull(values.joinedAt), notes: emptyToNull(values.notes), photoFileId: values.photoFileId,
      }
      return member ? api.put<Schema<'MemberDto'>>(`/api/associados/${member.id}`, body) : api.post<Schema<'MemberDto'>>('/api/associados', body)
    },
    onSuccess: (saved) => {
      toast.success(member ? 'Cadastro atualizado.' : 'Associado cadastrado. Aguardando aprovação.')
      void queryClient.invalidateQueries({ queryKey: ['/api/associados'] })
      void queryClient.invalidateQueries({ queryKey: ['associado', saved.id] })
      onSaved(saved)
    },
    onError: (error) => {
      // Erros de validação do servidor voltam para o campo correspondente.
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
    },
  })

  const group = 'text-xs font-semibold tracking-wider text-muted uppercase'
  return (
    <Modal open onClose={onClose} size="lg" title={member ? 'Editar associado' : 'Novo associado'}
      description={member ? undefined : 'O cadastro entra como "Aguardando aprovação" até ser aprovado pela administração.'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{member ? 'Salvar alterações' : 'Cadastrar'}</Button>
      </>}>
      <form className="space-y-6" noValidate onSubmit={(e) => e.preventDefault()}>
        {save.error && !(save.error instanceof ApiError && save.error.errors) ? <Alert tone="danger">{(save.error as Error).message}</Alert> : null}

        <div className="flex items-center gap-4">
          <Avatar fileId={form.watch('photoFileId')} name={form.watch('fullName')} size={64} />
          <FileUpload size="sm" label="Enviar fotografia" accept="image/jpeg,image/png,image/webp" scope="Staff" ownerMemberId={member?.id} onUploaded={(file) => form.setValue('photoFileId', file.id, { shouldDirty: true })} />
        </div>

        <fieldset className="space-y-4">
          <legend className={group}>Identificação</legend>
          <div className="grid grid-cols-6 gap-4">
            <Field className="col-span-4" label="Nome completo" required error={errors.fullName?.message}><Input aria-invalid={!!errors.fullName} {...form.register('fullName')} /></Field>
            <Field className="col-span-2" label="CPF" required error={errors.cpf?.message}>
              <Controller control={form.control} name="cpf" render={({ field }) => <MaskedInput mask="cpf" value={field.value} onChange={field.onChange} aria-invalid={!!errors.cpf} placeholder="000.000.000-00" />} />
            </Field>
            <Field className="col-span-2" label="RG"><Input {...form.register('rg')} /></Field>
            <Field className="col-span-2" label="Data de nascimento" error={errors.birthDate?.message}><Input type="date" {...form.register('birthDate')} /></Field>
            <Field className="col-span-2" label="Data de associação"><Input type="date" {...form.register('joinedAt')} /></Field>
            <Field className="col-span-3" label="Gênero">
              <Select {...form.register('gender')}><option value="">Não informado</option><option>Feminino</option><option>Masculino</option><option>Outro</option></Select>
            </Field>
            <Field className="col-span-3" label="Estado civil">
              <Select {...form.register('maritalStatus')}><option value="">Não informado</option><option>Solteiro(a)</option><option>Casado(a)</option><option>União estável</option><option>Divorciado(a)</option><option>Viúvo(a)</option></Select>
            </Field>
          </div>
        </fieldset>

        <fieldset className="space-y-4">
          <legend className={group}>Contato</legend>
          <div className="grid grid-cols-6 gap-4">
            <Field className="col-span-4" label="E-mail" error={errors.email?.message} hint="Usado para o acesso ao aplicativo."><Input type="email" aria-invalid={!!errors.email} {...form.register('email')} /></Field>
            <Field className="col-span-2" label="Telefone">
              <Controller control={form.control} name="phone" render={({ field }) => <MaskedInput mask="phone" value={field.value ?? ''} onChange={field.onChange} placeholder="(00) 00000-0000" />} />
            </Field>
          </div>
        </fieldset>

        <fieldset className="space-y-4">
          <legend className={group}>Endereço</legend>
          <div className="grid grid-cols-6 gap-4">
            <Field className="col-span-2" label="CEP">
              <Controller control={form.control} name="zipCode" render={({ field }) => <MaskedInput mask="cep" value={field.value ?? ''} onChange={field.onChange} placeholder="00000-000" />} />
            </Field>
            <Field className="col-span-3" label="Logradouro"><Input {...form.register('street')} /></Field>
            <Field className="col-span-1" label="Número"><Input {...form.register('addressNumber')} /></Field>
            <Field className="col-span-2" label="Complemento"><Input {...form.register('complement')} /></Field>
            <Field className="col-span-2" label="Bairro"><Input {...form.register('district')} /></Field>
            <Field className="col-span-1" label="Cidade"><Input {...form.register('city')} /></Field>
            <Field className="col-span-1" label="UF" error={errors.state?.message}><Select {...form.register('state')}><option value="">—</option>{UF.map((uf) => <option key={uf}>{uf}</option>)}</Select></Field>
          </div>
        </fieldset>

        <Field label="Informações complementares" hint="Visível apenas para a equipe."><Textarea rows={3} {...form.register('notes')} /></Field>
      </form>
    </Modal>
  )
}
