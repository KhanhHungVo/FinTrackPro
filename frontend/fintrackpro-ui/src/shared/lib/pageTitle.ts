import type { TFunction } from 'i18next'
import { matchPath } from 'react-router'

const APP_NAME = 'FinTrackPro'

type RouteTitleDefinition = {
  path: string
  titleKey?: string
}

const routeTitleDefinitions: RouteTitleDefinition[] = [
  { path: '/', titleKey: 'nav.home' },
  { path: '/dashboard', titleKey: 'nav.dashboard' },
  { path: '/transactions', titleKey: 'nav.transactions' },
  { path: '/budgets', titleKey: 'nav.budgets' },
  { path: '/trades', titleKey: 'nav.trades' },
  { path: '/market', titleKey: 'nav.market' },
  { path: '/settings', titleKey: 'nav.settings' },
  { path: '/pricing', titleKey: 'nav.pricing' },
  { path: '/about', titleKey: 'nav.about' },
  { path: '*', titleKey: 'error.notFoundTitle' },
]

export function formatDocumentTitle(pageName?: string) {
  return pageName ? `${pageName} | ${APP_NAME}` : APP_NAME
}

export function resolvePageTitle(pathname: string, t: TFunction) {
  const route = routeTitleDefinitions.find((definition) =>
    matchPath({ path: definition.path, end: definition.path !== '*' }, pathname),
  )

  if (!route?.titleKey) {
    return formatDocumentTitle()
  }

  return formatDocumentTitle(t(route.titleKey))
}
