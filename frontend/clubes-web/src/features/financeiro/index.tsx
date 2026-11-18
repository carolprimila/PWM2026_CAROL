import { AlertTriangle, ArrowLeftRight, CalendarCheck, HandCoins, LineChart, Lock, Receipt } from 'lucide-react'
import { lazy } from 'react'
import type { Feature } from '@/app/features'

const view = ['finance.view']

const feature: Feature = {
  nav: [
    { to: '/financeiro/cobrancas', label: 'Cobranças', icon: Receipt, group: 'Financeiro', order: 1, permissions: view },
    { to: '/financeiro/mensalidades', label: 'Mensalidades', icon: CalendarCheck, group: 'Financeiro', order: 2, permissions: view },
    { to: '/financeiro/pagamentos', label: 'Pagamentos', icon: HandCoins, group: 'Financeiro', order: 3, permissions: view },
    { to: '/financeiro/lancamentos', label: 'Receitas e despesas', icon: ArrowLeftRight, group: 'Financeiro', order: 4, permissions: view },
    { to: '/financeiro/fluxo-de-caixa', label: 'Fluxo de caixa', icon: LineChart, group: 'Financeiro', order: 5, permissions: view },
    { to: '/financeiro/inadimplencia', label: 'Inadimplência', icon: AlertTriangle, group: 'Financeiro', order: 6, permissions: view },
    { to: '/financeiro/fechamentos', label: 'Fechamento mensal', icon: Lock, group: 'Financeiro', order: 7, permissions: view },
  ],
  routes: [
    { path: 'financeiro/cobrancas', component: lazy(() => import('./Charges')), permissions: view },
    { path: 'financeiro/cobrancas/:id', component: lazy(() => import('./ChargeDetail')), permissions: view },
    { path: 'financeiro/mensalidades', component: lazy(() => import('./MonthlyFees')), permissions: view },
    { path: 'financeiro/pagamentos', component: lazy(() => import('./Payments')), permissions: view },
    { path: 'financeiro/lancamentos', component: lazy(() => import('./Entries')), permissions: view },
    { path: 'financeiro/fluxo-de-caixa', component: lazy(() => import('./CashFlow')), permissions: view },
    { path: 'financeiro/inadimplencia', component: lazy(() => import('./Delinquency')), permissions: view },
    { path: 'financeiro/fechamentos', component: lazy(() => import('./Closings')), permissions: view },
  ],
}
export default feature
