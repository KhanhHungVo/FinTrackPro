import { type ReactNode, useEffect } from 'react'
import { useLocaleStore } from '@/features/locale'
import i18n from '@/shared/i18n'

export function LocaleProvider({ children }: { children: ReactNode }) {
  const language = useLocaleStore((s) => s.language)

  useEffect(() => {
    if (i18n.language !== language) {
      i18n.changeLanguage(language)
    }
  }, [language])

  return <>{children}</>
}
