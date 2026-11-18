import type { ComponentType, LazyExoticComponent } from 'react'
import type { Schema } from '@/lib/api'

export const navGroups = ['Visão geral', 'Cadastros', 'Financeiro', 'Espaços e reservas', 'Portaria', 'Comunicação', 'Operação', 'Administração', 'Plataforma'] as const
export type NavGroup = (typeof navGroups)[number]

export type NavItem = {
  to: string
  label: string
  icon: ComponentType<{ className?: string }>
  group: NavGroup
  order?: number
  /** Exibido se o usuário tiver ao menos uma destas permissões. */
  permissions?: string[]
  /** Restringe a tipos de usuário (ex.: ['SuperAdmin']). */
  kinds?: Schema<'UserKind'>[]
}

export type FeatureRoute = {
  /** Caminho relativo à raiz autenticada, ex.: "associados/:id". */
  path: string
  component: ComponentType | LazyExoticComponent<ComponentType>
  permissions?: string[]
  kinds?: Schema<'UserKind'>[]
}

/**
 * Cada módulo vive em `src/features/<modulo>/index.tsx` e exporta por padrão um Feature.
 * O roteador e o menu lateral descobrem os módulos automaticamente — não há arquivo central a editar.
 */
export type Feature = { nav?: NavItem[]; routes: FeatureRoute[] }

const modules = import.meta.glob<{ default: Feature }>('../features/*/index.tsx', { eager: true })

export const features: Feature[] = Object.values(modules).map((module) => module.default)
export const allRoutes: FeatureRoute[] = features.flatMap((feature) => feature.routes)
export const allNav: NavItem[] = features.flatMap((feature) => feature.nav ?? [])
