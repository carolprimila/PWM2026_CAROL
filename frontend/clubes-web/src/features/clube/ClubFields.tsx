import { Controller, type UseFormReturn } from 'react-hook-form'
import { z } from 'zod'
import { Button, Field, FileImage, FileUpload, Input, MaskedInput, Select, Textarea } from '@/components/ui'
import type { Schema } from '@/lib/api'
import { emptyToNull, masks } from '@/lib/format'

type Club = Schema<'ClubDto'>

export const clubSchema = z.object({
  name: z.string().trim().min(2, 'Informe o nome do clube').max(150, 'Máximo de 150 caracteres'),
  legalName: z.string().max(200, 'Máximo de 200 caracteres'),
  cnpj: z.string().refine((v) => [0, 14].includes(v.replace(/\D/g, '').length), 'Informe os 14 dígitos do CNPJ'),
  email: z.union([z.literal(''), z.string().email('E-mail inválido')]),
  phone: z.string(),
  zipCode: z.string(),
  street: z.string().max(200, 'Máximo de 200 caracteres'),
  number: z.string().max(20, 'Máximo de 20 caracteres'),
  complement: z.string().max(100, 'Máximo de 100 caracteres'),
  district: z.string().max(100, 'Máximo de 100 caracteres'),
  city: z.string().max(100, 'Máximo de 100 caracteres'),
  state: z.string(),
  about: z.string().max(4000, 'Máximo de 4000 caracteres'),
  timeZoneId: z.string(),
  logoFileId: z.string().nullable(),
})
export type ClubValues = z.infer<typeof clubSchema>

export const clubDefaults = (club?: Club): ClubValues => ({
  name: club?.name ?? '', legalName: club?.legalName ?? '', cnpj: masks.cnpj(club?.cnpj ?? ''), email: club?.email ?? '', phone: masks.phone(club?.phone ?? ''),
  zipCode: masks.cep(club?.zipCode ?? ''), street: club?.street ?? '', number: club?.number ?? '', complement: club?.complement ?? '', district: club?.district ?? '',
  city: club?.city ?? '', state: club?.state ?? '', about: club?.about ?? '', timeZoneId: club?.timeZoneId ?? 'America/Sao_Paulo', logoFileId: club?.logoFileId ?? null,
})

/** Corpo de SaveClubRequest: textos vazios viram null. */
export const clubBody = (values: ClubValues): Schema<'SaveClubRequest'> => ({
  name: values.name, legalName: emptyToNull(values.legalName), cnpj: emptyToNull(values.cnpj), email: emptyToNull(values.email), phone: emptyToNull(values.phone),
  zipCode: emptyToNull(values.zipCode), street: emptyToNull(values.street), number: emptyToNull(values.number), complement: emptyToNull(values.complement),
  district: emptyToNull(values.district), city: emptyToNull(values.city), state: emptyToNull(values.state), about: emptyToNull(values.about),
  timeZoneId: values.timeZoneId, logoFileId: values.logoFileId,
})

const UF = ['AC', 'AL', 'AP', 'AM', 'BA', 'CE', 'DF', 'ES', 'GO', 'MA', 'MT', 'MS', 'MG', 'PA', 'PB', 'PR', 'PE', 'PI', 'RJ', 'RN', 'RS', 'RO', 'RR', 'SC', 'SP', 'SE', 'TO']

/** Fusos horários brasileiros (IANA). As regras de funcionamento e reservas usam o fuso do clube. */
const TIME_ZONES: [string, string][] = [
  ['America/Sao_Paulo', 'Brasília (São Paulo, Rio de Janeiro, Sul, Sudeste, GO, DF)'], ['America/Bahia', 'Bahia'], ['America/Fortaleza', 'Fortaleza (CE, RN, PB, MA, PI)'],
  ['America/Recife', 'Recife (PE)'], ['America/Maceio', 'Maceió (AL, SE)'], ['America/Belem', 'Belém (PA, AP)'], ['America/Araguaina', 'Araguaína (TO)'],
  ['America/Cuiaba', 'Cuiabá (MT)'], ['America/Campo_Grande', 'Campo Grande (MS)'], ['America/Manaus', 'Manaus (AM)'], ['America/Porto_Velho', 'Porto Velho (RO)'],
  ['America/Boa_Vista', 'Boa Vista (RR)'], ['America/Rio_Branco', 'Rio Branco (AC)'], ['America/Noronha', 'Fernando de Noronha'],
]

const group = 'text-xs font-semibold tracking-wider text-muted uppercase'

/** Campos institucionais do clube, compartilhados por "Dados do clube" e pelo cadastro de clubes da plataforma. */
export function ClubFields({ form, withLogo = false }: { form: UseFormReturn<ClubValues>; withLogo?: boolean }) {
  const { errors } = form.formState
  const logo = form.watch('logoFileId')
  const zone = form.watch('timeZoneId')
  return (
    <div className="space-y-6">
      {withLogo && (
        <div className="flex items-center gap-4">
          {logo ? <FileImage fileId={logo} alt="Logotipo do clube" className="size-16 rounded-xl border border-line object-contain" />
            : <span className="flex size-16 items-center justify-center rounded-xl border border-dashed border-line text-xs text-muted">Sem logo</span>}
          <div className="flex items-center gap-2">
            <FileUpload size="sm" label={logo ? 'Trocar logotipo' : 'Enviar logotipo'} accept="image/jpeg,image/png,image/webp" scope="ClubPublic" onUploaded={(file) => form.setValue('logoFileId', file.id, { shouldDirty: true })} />
            {logo && <Button size="sm" variant="ghost" onClick={() => form.setValue('logoFileId', null, { shouldDirty: true })}>Remover</Button>}
          </div>
        </div>
      )}

      <fieldset className="space-y-4">
        <legend className={group}>Identificação</legend>
        <div className="grid grid-cols-6 gap-4">
          <Field className="col-span-3" label="Nome do clube" required error={errors.name?.message}><Input aria-invalid={!!errors.name} {...form.register('name')} /></Field>
          <Field className="col-span-3" label="Razão social" error={errors.legalName?.message}><Input {...form.register('legalName')} /></Field>
          <Field className="col-span-2" label="CNPJ" error={errors.cnpj?.message}>
            <Controller control={form.control} name="cnpj" render={({ field }) => <MaskedInput mask="cnpj" value={field.value} onChange={field.onChange} aria-invalid={!!errors.cnpj} placeholder="00.000.000/0000-00" />} />
          </Field>
          <Field className="col-span-2" label="E-mail" error={errors.email?.message}><Input type="email" aria-invalid={!!errors.email} {...form.register('email')} /></Field>
          <Field className="col-span-2" label="Telefone">
            <Controller control={form.control} name="phone" render={({ field }) => <MaskedInput mask="phone" value={field.value} onChange={field.onChange} placeholder="(00) 00000-0000" />} />
          </Field>
        </div>
      </fieldset>

      <fieldset className="space-y-4">
        <legend className={group}>Endereço</legend>
        <div className="grid grid-cols-6 gap-4">
          <Field className="col-span-2" label="CEP">
            <Controller control={form.control} name="zipCode" render={({ field }) => <MaskedInput mask="cep" value={field.value} onChange={field.onChange} placeholder="00000-000" />} />
          </Field>
          <Field className="col-span-3" label="Logradouro" error={errors.street?.message}><Input {...form.register('street')} /></Field>
          <Field className="col-span-1" label="Número" error={errors.number?.message}><Input {...form.register('number')} /></Field>
          <Field className="col-span-2" label="Complemento" error={errors.complement?.message}><Input {...form.register('complement')} /></Field>
          <Field className="col-span-2" label="Bairro" error={errors.district?.message}><Input {...form.register('district')} /></Field>
          <Field className="col-span-1" label="Cidade" error={errors.city?.message}><Input {...form.register('city')} /></Field>
          <Field className="col-span-1" label="UF"><Select {...form.register('state')}><option value="">—</option>{UF.map((uf) => <option key={uf}>{uf}</option>)}</Select></Field>
        </div>
      </fieldset>

      <fieldset className="space-y-4">
        <legend className={group}>Apresentação e fuso horário</legend>
        <div className="grid grid-cols-6 gap-4">
          <Field className="col-span-6" label="Sobre o clube" error={errors.about?.message} hint="Texto institucional exibido aos associados."><Textarea rows={4} {...form.register('about')} /></Field>
          <Field className="col-span-3" label="Fuso horário" hint="Usado nos horários de funcionamento, reservas e vencimentos.">
            <Select {...form.register('timeZoneId')}>
              {!TIME_ZONES.some(([id]) => id === zone) && zone && <option value={zone}>{zone}</option>}
              {TIME_ZONES.map(([id, label]) => <option key={id} value={id}>{label}</option>)}
            </Select>
          </Field>
        </div>
      </fieldset>
    </div>
  )
}
