import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation } from 'react-router'
import { resolvePageTitle } from '@/shared/lib/pageTitle'

export function DocumentTitle() {
  const location = useLocation()
  const { t, i18n } = useTranslation()

  useEffect(() => {
    document.title = resolvePageTitle(location.pathname, t)
  }, [i18n.language, location.pathname, t])

  return null
}
