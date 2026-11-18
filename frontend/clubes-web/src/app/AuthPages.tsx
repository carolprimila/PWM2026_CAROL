import { zodResolver } from '@hookform/resolvers/zod'
import { LogOut, MonitorSmartphone, ShieldCheck, Smartphone, Trash2 } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { Alert, Badge, Button, Card, CardTitle, Field, IconButton, Input, PageHeader, QueryBoundary, useToast } from '@/components/ui'
import { api, API_URL, ApiError, errorMessage, session, type Schema } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { fmt } from '@/lib/format'

const password = z.string().min(8, 'Use ao menos 8 caracteres').regex(/[A-Za-z]/, 'Inclua ao menos uma letra').regex(/\d/, 'Inclua ao menos um número')

function AuthShell({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <div className="grid min-h-full lg:grid-cols-[1fr_1.1fr]">
      <aside className="relative hidden overflow-hidden bg-brand-800 p-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="pointer-events-none absolute -top-24 -right-24 size-96 rounded-full bg-brand-600/40 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-32 -left-20 size-96 rounded-full bg-gold-500/15 blur-3xl" />
        <div className="relative flex items-center gap-3">
          <span className="flex size-10 items-center justify-center rounded-xl bg-white/10 text-lg font-bold">C</span>
          <span className="text-lg font-semibold tracking-tight">Clubes</span>
        </div>
        <div className="relative max-w-md">
          <p className="text-3xl leading-tight font-semibold tracking-tight">A gestão completa do clube, em um só lugar.</p>
          <p className="mt-4 text-sm leading-relaxed text-white/70">Associados, financeiro, reservas, portaria e comunicação integrados — com segurança e isolamento por clube.</p>
        </div>
        <p className="relative text-xs text-white/50">Acesso restrito à equipe autorizada.</p>
      </aside>
      <main className="flex items-center justify-center bg-canvas p-8">
        <div className="w-full max-w-sm">
          <h1 className="text-2xl font-semibold tracking-tight text-ink">{title}</h1>
          {subtitle && <p className="mt-1.5 text-sm text-muted">{subtitle}</p>}
          <div className="mt-8">{children}</div>
        </div>
      </main>
    </div>
  )
}

export function LoginPage() {
  const { login } = useAuth()
  const [error, setError] = useState<string | null>(null)
  const schema = z.object({ email: z.string().email('Informe um e-mail válido'), password: z.string().min(1, 'Informe a senha') })
  const form = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema), defaultValues: { email: '', password: '' } })
  const submit = form.handleSubmit(async (values) => {
    setError(null)
    try { await login(values.email, values.password) } catch (e) { setError(errorMessage(e)) }
  })
  return (
    <AuthShell title="Entrar" subtitle="Acesse o painel de gestão do clube.">
      <form onSubmit={submit} className="space-y-4" noValidate>
        {error && <Alert tone="danger">{error}</Alert>}
        <Field label="E-mail" error={form.formState.errors.email?.message}>
          <Input type="email" autoComplete="username" autoFocus placeholder="voce@clube.com.br" aria-invalid={!!form.formState.errors.email} {...form.register('email')} />
        </Field>
        <Field label="Senha" error={form.formState.errors.password?.message}>
          <Input type="password" autoComplete="current-password" aria-invalid={!!form.formState.errors.password} {...form.register('password')} />
        </Field>
        <Button type="submit" variant="primary" className="w-full" loading={form.formState.isSubmitting}>Entrar</Button>
        <p className="text-center text-sm"><Link to="/esqueci-senha" className="font-medium text-brand-600 hover:underline">Esqueci minha senha</Link></p>
      </form>
      {devLogins.length > 0 && (
        <div className="mt-8 border-t border-line pt-5">
          <p className="mb-3 text-center text-xs font-medium tracking-wide text-muted uppercase">Entrada rápida · ambiente de desenvolvimento</p>
          <div className="grid grid-cols-2 gap-2">
            {devLogins.map((dev) => (
              <Button key={dev.email} size="sm" disabled={form.formState.isSubmitting} title={dev.email} onClick={() => {
                form.setValue('email', dev.email)
                form.setValue('password', dev.password)
                void submit()
              }}>{dev.label}</Button>
            ))}
          </div>
        </div>
      )}
    </AuthShell>
  )
}

/**
 * Contas de teste para entrada rápida. Só existem no servidor de desenvolvimento (`npm run dev`) e somente se o arquivo
 * local não versionado `.env.local` foi gerado por `database/scripts/contas-dev.py`. No build de produção a lista é vazia.
 */
const devLogins: { label: string; email: string; password: string }[] = (() => {
  if (!import.meta.env.DEV) return []
  try { return JSON.parse((import.meta.env.VITE_DEV_LOGINS as string | undefined) ?? '[]') } catch { return [] }
})()

export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false)
  const schema = z.object({ email: z.string().email('Informe um e-mail válido') })
  const form = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema), defaultValues: { email: '' } })
  const [error, setError] = useState<string | null>(null)
  const submit = form.handleSubmit(async (values) => {
    setError(null)
    try { await api.post('/api/auth/esqueci-senha', values); setSent(true) } catch (e) { setError(errorMessage(e)) }
  })
  return (
    <AuthShell title="Recuperar senha" subtitle="Enviaremos as instruções para o e-mail cadastrado.">
      {sent ? (
        <div className="space-y-4">
          <Alert tone="success" title="Solicitação registrada">Se o e-mail estiver cadastrado, você receberá um código para criar uma nova senha.</Alert>
          <Link to="/redefinir-senha" className="block"><Button variant="primary" className="w-full">Já tenho o código</Button></Link>
          <p className="text-center text-sm"><Link to="/login" className="font-medium text-brand-600 hover:underline">Voltar ao login</Link></p>
        </div>
      ) : (
        <form onSubmit={submit} className="space-y-4" noValidate>
          {error && <Alert tone="danger">{error}</Alert>}
          <Field label="E-mail" error={form.formState.errors.email?.message}><Input type="email" autoFocus {...form.register('email')} /></Field>
          <Button type="submit" variant="primary" className="w-full" loading={form.formState.isSubmitting}>Enviar instruções</Button>
          <p className="text-center text-sm"><Link to="/login" className="font-medium text-brand-600 hover:underline">Voltar ao login</Link></p>
        </form>
      )}
    </AuthShell>
  )
}

export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const toast = useToast()
  const schema = z.object({ token: z.string().min(10, 'Informe o código recebido'), newPassword: password, confirm: z.string() })
    .refine((v) => v.newPassword === v.confirm, { path: ['confirm'], message: 'As senhas não conferem' })
  const form = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema), defaultValues: { token: params.get('token') ?? '', newPassword: '', confirm: '' } })
  const [error, setError] = useState<string | null>(null)
  const submit = form.handleSubmit(async ({ token, newPassword }) => {
    setError(null)
    try {
      await api.post('/api/auth/redefinir-senha', { token, newPassword })
      toast.success('Senha redefinida. Entre com a nova senha.')
      navigate('/login')
    } catch (e) { setError(errorMessage(e)) }
  })
  const errors = form.formState.errors
  return (
    <AuthShell title="Criar nova senha" subtitle="Informe o código recebido por e-mail e a nova senha.">
      <form onSubmit={submit} className="space-y-4" noValidate>
        {error && <Alert tone="danger">{error}</Alert>}
        <Field label="Código" error={errors.token?.message}><Input autoComplete="one-time-code" {...form.register('token')} /></Field>
        <Field label="Nova senha" error={errors.newPassword?.message} hint="Mínimo de 8 caracteres, com letras e números."><Input type="password" autoComplete="new-password" {...form.register('newPassword')} /></Field>
        <Field label="Confirmar nova senha" error={errors.confirm?.message}><Input type="password" autoComplete="new-password" {...form.register('confirm')} /></Field>
        <Button type="submit" variant="primary" className="w-full" loading={form.formState.isSubmitting}>Redefinir senha</Button>
        <p className="text-center text-sm"><Link to="/login" className="font-medium text-brand-600 hover:underline">Voltar ao login</Link></p>
      </form>
    </AuthShell>
  )
}

function ChangePasswordForm({ onDone }: { onDone: () => void }) {
  const schema = z.object({ currentPassword: z.string().min(1, 'Informe a senha atual'), newPassword: password, confirm: z.string() })
    .refine((v) => v.newPassword === v.confirm, { path: ['confirm'], message: 'As senhas não conferem' })
  const form = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema), defaultValues: { currentPassword: '', newPassword: '', confirm: '' } })
  const [error, setError] = useState<string | null>(null)
  const submit = form.handleSubmit(async ({ currentPassword, newPassword }) => {
    setError(null)
    try { await api.post('/api/auth/alterar-senha', { currentPassword, newPassword }); onDone() } catch (e) { setError(errorMessage(e)) }
  })
  const errors = form.formState.errors
  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      {error && <Alert tone="danger">{error}</Alert>}
      <Field label="Senha atual" error={errors.currentPassword?.message}><Input type="password" autoComplete="current-password" {...form.register('currentPassword')} /></Field>
      <Field label="Nova senha" error={errors.newPassword?.message} hint="Mínimo de 8 caracteres, com letras e números."><Input type="password" autoComplete="new-password" {...form.register('newPassword')} /></Field>
      <Field label="Confirmar nova senha" error={errors.confirm?.message}><Input type="password" autoComplete="new-password" {...form.register('confirm')} /></Field>
      <Button type="submit" variant="primary" loading={form.formState.isSubmitting}>Alterar senha</Button>
    </form>
  )
}

/** Primeiro acesso: troca obrigatória da senha temporária. A alteração encerra as sessões, então o usuário entra de novo. */
export function ForcedPasswordPage() {
  const { logout } = useAuth()
  const toast = useToast()
  return (
    <AuthShell title="Defina sua senha" subtitle="Por segurança, troque a senha temporária antes de continuar.">
      <ChangePasswordForm onDone={() => { toast.success('Senha alterada. Entre novamente com a nova senha.'); void logout() }} />
      <button className="mt-6 flex items-center gap-2 text-sm text-muted hover:text-ink" onClick={() => void logout()}><LogOut className="size-4" />Sair</button>
    </AuthShell>
  )
}

/** Consentimento LGPD: o usuário precisa aceitar a política de privacidade vigente do clube. */
export function PrivacyPage() {
  const { logout, reload } = useAuth()
  const policies = useQuery({ queryKey: ['politicas'], queryFn: () => api.get<Schema<'PublicPoliciesDto'>>('/api/configuracoes/politicas'), retry: false })
  const accept = useMutation({ mutationFn: () => api.post('/api/auth/aceitar-privacidade'), onSuccess: reload })
  // O superadministrador sem clube selecionado não tem política de clube para exibir.
  const noClub = policies.error instanceof ApiError && policies.error.code === 'clube_nao_selecionado'
  return (
    <div className="flex min-h-full items-center justify-center bg-canvas p-8">
      <Card className="w-full max-w-2xl">
        <CardTitle icon={ShieldCheck} title="Privacidade e tratamento de dados" description="Para continuar, leia e aceite a política de privacidade." />
        <div className="max-h-80 overflow-y-auto rounded-(--radius-control) border border-line bg-canvas p-4 text-sm leading-relaxed whitespace-pre-wrap text-ink/80">
          {policies.isLoading ? 'Carregando…'
            : policies.data?.privacyPolicy
            ?? (noClub
              ? 'Os dados pessoais tratados na plataforma são utilizados exclusivamente para a gestão dos clubes, com acesso restrito a pessoas autorizadas, conforme a Lei Geral de Proteção de Dados (LGPD).'
              : 'O clube ainda não publicou o texto da política de privacidade. Os dados pessoais são tratados exclusivamente para a gestão do clube, com acesso restrito à equipe autorizada, conforme a Lei Geral de Proteção de Dados (LGPD). Você será convidado a aceitar novamente quando o texto definitivo for publicado.')}
        </div>
        {accept.error ? <Alert tone="danger" className="mt-4">{errorMessage(accept.error)}</Alert> : null}
        <div className="mt-5 flex items-center justify-between">
          <Button variant="ghost" icon={LogOut} onClick={() => void logout()}>Sair</Button>
          <Button variant="primary" loading={accept.isPending} disabled={policies.isLoading} onClick={() => accept.mutate()}>Li e aceito</Button>
        </div>
      </Card>
    </div>
  )
}

export function AccountPasswordPage() {
  const { logout } = useAuth()
  const toast = useToast()
  return (
    <>
      <PageHeader title="Alterar senha" subtitle="Ao alterar a senha, todas as sessões são encerradas e você precisará entrar novamente." />
      <Card className="max-w-md"><ChangePasswordForm onDone={() => { toast.success('Senha alterada. Entre novamente.'); void logout() }} /></Card>
    </>
  )
}

export function AccountSessionsPage() {
  const queryClient = useQueryClient()
  const toast = useToast()
  const sessions = useQuery({
    queryKey: ['sessoes'],
    queryFn: async () => {
      // O token de renovação atual identifica a sessão corrente na lista.
      const response = await fetch(`${API_URL}/api/auth/sessoes`, {
        headers: { Authorization: `Bearer ${session.accessToken}`, 'X-Refresh-Token': session.refreshToken ?? '' },
      })
      if (!response.ok) throw new ApiError(response.status, 'erro', 'Não foi possível carregar as sessões.')
      return (await response.json()) as Schema<'SessionDto'>[]
    },
  })
  const revoke = useMutation({
    mutationFn: (id: string) => api.delete(`/api/auth/sessoes/${id}`),
    onSuccess: () => { toast.success('Sessão encerrada.'); void queryClient.invalidateQueries({ queryKey: ['sessoes'] }) },
    onError: toast.error,
  })
  return (
    <>
      <PageHeader title="Sessões ativas" subtitle="Dispositivos em que sua conta está conectada." />
      <Card padded={false} className="max-w-3xl">
        <QueryBoundary query={sessions}>
          {(items) => (
            <ul className="divide-y divide-line">
              {items.map((item) => (
                <li key={item.id} className="flex items-center gap-4 px-5 py-4">
                  <span className="flex size-10 items-center justify-center rounded-xl bg-canvas text-muted">{/mobile|android|ios/i.test(item.device ?? '') ? <Smartphone className="size-5" /> : <MonitorSmartphone className="size-5" />}</span>
                  <div className="min-w-0 flex-1">
                    <p className="flex items-center gap-2 text-sm font-medium text-ink"><span className="truncate">{item.device || 'Dispositivo não identificado'}</span>{item.isCurrent && <Badge tone="success">Esta sessão</Badge>}</p>
                    <p className="mt-0.5 text-xs text-muted">IP {item.ip ?? '—'} · iniciada em {fmt.dateTime(item.createdAt)} · expira em {fmt.dateTime(item.expiresAt)}</p>
                  </div>
                  {!item.isCurrent && <IconButton icon={Trash2} label="Encerrar sessão" onClick={() => revoke.mutate(item.id)} />}
                </li>
              ))}
            </ul>
          )}
        </QueryBoundary>
      </Card>
    </>
  )
}
