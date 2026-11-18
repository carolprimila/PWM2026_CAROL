import { Settings2 } from 'lucide-react'
import { lazy } from 'react'
import type { Feature } from '@/app/features'

const feature: Feature = {
  nav: [{ to: '/configuracoes', label: 'Configurações', icon: Settings2, group: 'Administração', order: 4, permissions: ['settings.view', 'settings.manage'] }],
  routes: [{ path: 'configuracoes', component: lazy(() => import('./SettingsPage')), permissions: ['settings.view', 'settings.manage'] }],
}
export default feature
