import { CalendarDays } from 'lucide-react'
import { lazy } from 'react'
import type { Feature } from '@/app/features'

const feature: Feature = {
  nav: [{ to: '/calendario', label: 'Calendário', icon: CalendarDays, group: 'Espaços e reservas', order: 4, permissions: ['calendar.view'] }],
  routes: [{ path: 'calendario', component: lazy(() => import('./CalendarPage')), permissions: ['calendar.view'] }],
}
export default feature
