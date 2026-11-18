import { DoorOpen, History } from 'lucide-react'
import { lazy } from 'react'
import type { Feature } from '@/app/features'

const feature: Feature = {
  nav: [
    { to: '/portaria', label: 'Portaria', icon: DoorOpen, group: 'Portaria', order: 1, permissions: ['gate.operate'] },
    { to: '/portaria/historico', label: 'Histórico de acessos', icon: History, group: 'Portaria', order: 2, permissions: ['gate.history'] },
  ],
  routes: [
    { path: 'portaria', component: lazy(() => import('./Gate')), permissions: ['gate.operate'] },
    { path: 'portaria/historico', component: lazy(() => import('./AccessHistory')), permissions: ['gate.history'] },
  ],
}
export default feature
