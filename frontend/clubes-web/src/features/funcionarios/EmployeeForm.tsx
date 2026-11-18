import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Button, Field, Input, MaskedInput, Modal, useToast } from '@/components/ui'
import { isValidCpf } from '@/features/dependentes/cpf'
import { api, ApiError, type Schema } from '@/lib/api'
import { emptyToNull, masks } from '@/lib/format'

type Employee = Schema<'EmployeeDto'>

const schema = z.object({
  fullName: z.string().trim().min(3, 'Informe o nome completo').max(150, 'Máximo de 150 caracteres'),
  cpf: z.string().refine(isValidCpf, 'Informe um CPF válido'),
  phone: z.string(),
  email: z.union([z.literal(''), z.string().email('E-mail inválido')]),
  position: z.string().max(80, 'Máximo de 80 caracteres'),
  department: z.string().max(80, 'Máximo de 80 caracteres'),
  hiredAt: z.string(),
})
type Values = z.infer<typeof schema>

export function EmployeeForm({ employee, onClose, onSaved }: { employee?: Employee; onClose: () => void; onSaved: (employee: Employee) => void }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      fullName: employee?.fullName ?? '', cpf: masks.cpf(employee?.cpf ?? ''), phone: masks.phone(employee?.phone ?? ''), email: employee?.email ?? '',
      position: employee?.position ?? '', department: employee?.department ?? '', hiredAt: employee?.hiredAt ?? '',
    },
  })
  const { errors } = form.formState

  const save = useMutation({
    mutationFn: (values: Values) => {
      const body = {
        fullName: values.fullName, cpf: values.cpf, phone: emptyToNull(values.phone), email: emptyToNull(values.email),
        position: emptyToNull(values.position), department: emptyToNull(values.department), hiredAt: emptyToNull(values.hiredAt),
      }
      return employee ? api.put<Employee>(`/api/funcionarios/${employee.id}`, body) : api.post<Employee>('/api/funcionarios', body)
    },
    onSuccess: (saved) => {
      toast.success(employee ? 'Cadastro atualizado.' : 'Funcionário cadastrado.')
      void queryClient.invalidateQueries({ queryKey: ['/api/funcionarios'] })
      void queryClient.invalidateQueries({ queryKey: ['funcionario', saved.id] })
      onSaved(saved)
    },
    onError: (error) => {
      if (error instanceof ApiError && error.errors)
        for (const [field, messages] of Object.entries(error.errors)) form.setError((field.charAt(0).toLowerCase() + field.slice(1)) as keyof Values, { message: messages[0] })
    },
  })

  return (
    <Modal open onClose={onClose} title={employee ? 'Editar funcionário' : 'Novo funcionário'}
      description={employee ? undefined : 'O cadastro não cria login: depois de salvar, use "Criar usuário de acesso" para liberar o painel.'}
      footer={<>
        <Button onClick={onClose}>Cancelar</Button>
        <Button variant="primary" loading={save.isPending} onClick={form.handleSubmit((values) => save.mutate(values))}>{employee ? 'Salvar alterações' : 'Cadastrar'}</Button>
      </>}>
      <form className="space-y-5" noValidate onSubmit={(e) => e.preventDefault()}>
        {save.error && !(save.error instanceof ApiError && save.error.errors) ? <Alert tone="danger">{(save.error as Error).message}</Alert> : null}
        <div className="grid grid-cols-6 gap-4">
          <Field className="col-span-4" label="Nome completo" required error={errors.fullName?.message}><Input aria-invalid={!!errors.fullName} {...form.register('fullName')} /></Field>
          <Field className="col-span-2" label="CPF" required error={errors.cpf?.message}>
            <Controller control={form.control} name="cpf" render={({ field }) => <MaskedInput mask="cpf" value={field.value} onChange={field.onChange} aria-invalid={!!errors.cpf} placeholder="000.000.000-00" />} />
          </Field>
          <Field className="col-span-3" label="Cargo" error={errors.position?.message}><Input {...form.register('position')} /></Field>
          <Field className="col-span-3" label="Setor" error={errors.department?.message}><Input {...form.register('department')} /></Field>
          <Field className="col-span-2" label="Admissão"><Input type="date" {...form.register('hiredAt')} /></Field>
          <Field className="col-span-2" label="Telefone">
            <Controller control={form.control} name="phone" render={({ field }) => <MaskedInput mask="phone" value={field.value} onChange={field.onChange} placeholder="(00) 00000-0000" />} />
          </Field>
          <Field className="col-span-6" label="E-mail" error={errors.email?.message}><Input type="email" aria-invalid={!!errors.email} {...form.register('email')} /></Field>
        </div>
      </form>
    </Modal>
  )
}
