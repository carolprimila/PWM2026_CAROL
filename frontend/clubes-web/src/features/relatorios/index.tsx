import { BarChart3 } from 'lucide-react'
import { lazy } from 'react'
import type { Feature } from '@/app/features'

const feature: Feature = {
  nav: [{ to: '/relatorios', label: 'Relatórios', icon: BarChart3, group: 'Visão geral', order: 3, permissions: ['reports.view'] }],
  routes: [{ path: 'relatorios', component: lazy(() => import('./ReportsPage')), permissions: ['reports.view'] }],
}
export default feature
