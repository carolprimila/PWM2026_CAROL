import { Building2, LogOut, Smartphone } from 'lucide-react'
import { Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { Button, EmptyState, LoadingState, Spinner } from '@/components/ui'
import { useAuth } from '@/lib/auth'
import { AccountPasswordPage, AccountSessionsPage, ForcedPasswordPage, ForgotPasswordPage, LoginPage, PrivacyPage, ResetPasswordPage } from './AuthPages'
import { allNav, allRoutes, navGroups, type FeatureRoute } from './features'
import { Forbidden, Layout, NotFound } from './Layout'

/** Guarda de rota: a interface esconde o que o usuário não pode acessar; a API valida tudo novamente. */
function Guarded({ route }: { route: FeatureRoute }) {
  const { user, can, isSuperAdmin, clubId } = useAuth()
  if (route.kinds && (!user || !route.kinds.includes(user.kind))) return <Forbidden />
  if (route.permissions && !can(...route.permissions)) return <Forbidden />
  if (isSuperAdmin && !clubId && !route.kinds?.includes('SuperAdmin'))
    return <EmptyState icon={Building2} title="Selecione um clube" description="Escolha no topo da tela o clube em que deseja operar para acessar esta área." />
  const Component = route.component
  return <Suspense fallback={<LoadingState />}><Component /></Suspense>
}

function AppOnlyNotice() {
  const { logout, user } = useAuth()
  return (
    <div className="flex min-h-full items-center justify-center bg-canvas p-8">
      <EmptyState icon={Smartphone} title={`Olá, ${user?.name.split(' ')[0] ?? ''}!`}
        description="O painel Web é destinado à equipe do clube. Associados e dependentes utilizam o aplicativo para carteirinha, reservas, financeiro e comunicados."
        action={<Button icon={LogOut} onClick={() => void logout()}>Sair</Button>} />
    </div>
  )
}

export function App() {
  const { status, user, isSuperAdmin, clubId, can } = useAuth()

  if (status === 'loading') return <div className="flex h-full items-center justify-center"><Spinner className="size-7" /></div>

  if (status === 'anonymous' || !user) {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/esqueci-senha" element={<ForgotPasswordPage />} />
        <Route path="/redefinir-senha" element={<ResetPasswordPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    )
  }

  if (user.mustChangePassword) return <ForcedPasswordPage />
  if (!user.privacyAccepted) return <PrivacyPage />
  if (user.kind === 'Member' || user.kind === 'Dependent') return <AppOnlyNotice />

  // O superadministrador sem clube selecionado começa pela gestão da plataforma; quem não tem acesso ao
  // painel (ex.: portaria) começa pela primeira área do menu que pode acessar.
  const firstAllowed = allNav
    .filter((item) => item.to !== '/' && (!item.kinds || item.kinds.includes(user.kind)) && (!item.permissions || can(...item.permissions)))
    .sort((a, b) => navGroups.indexOf(a.group) - navGroups.indexOf(b.group) || (a.order ?? 50) - (b.order ?? 50))[0]?.to
  const home = isSuperAdmin && !clubId ? '/plataforma/clubes' : can('dashboard.view') ? '/' : firstAllowed ?? '/'
  return (
    <Routes>
      <Route element={<Layout />}>
        {allRoutes.map((route) => (
          route.path === '' ? <Route key="index" index element={home !== '/' ? <Navigate to={home} replace /> : <Guarded route={route} />} />
            : <Route key={route.path} path={route.path} element={<Guarded route={route} />} />
        ))}
        <Route path="conta/senha" element={<AccountPasswordPage />} />
        <Route path="conta/sessoes" element={<AccountSessionsPage />} />
        <Route path="*" element={<NotFound />} />
      </Route>
      <Route path="/login" element={<Navigate to={home} replace />} />
    </Routes>
  )
}
