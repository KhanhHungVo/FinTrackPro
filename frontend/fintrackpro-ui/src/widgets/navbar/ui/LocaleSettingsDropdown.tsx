import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocaleStore } from '@/features/locale'
import { useUpdateUserPreferences } from '@/entities/user-preferences'
import { SUPPORTED_LANGUAGES, type Language } from '@/shared/i18n'
import { cn } from '@/shared/lib/cn'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

export function LocaleSettingsDropdown() {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const language = useLocaleStore((s) => s.language)
  const currency = useLocaleStore((s) => s.currency)
  const setLanguage = useLocaleStore((s) => s.setLanguage)
  const setCurrency = useLocaleStore((s) => s.setCurrency)
  const updatePreferences = useUpdateUserPreferences()

  function handleLanguageChange(lang: Language) {
    setLanguage(lang)
    updatePreferences.mutate({ language: lang, currency })
  }

  function handleCurrencyChange(cur: string) {
    setCurrency(cur)
    updatePreferences.mutate({ language, currency: cur })
  }

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1 rounded-md px-2 py-1.5 text-sm text-gray-600 hover:bg-gray-100 transition-colors"
        aria-label="Language and currency settings"
      >
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
          <circle cx="8" cy="8" r="6.5" stroke="currentColor" strokeWidth="1.5" />
          <path d="M8 1.5C8 1.5 5.5 4 5.5 8s2.5 6.5 2.5 6.5M8 1.5C8 1.5 10.5 4 10.5 8S8 14.5 8 14.5M1.5 8h13" stroke="currentColor" strokeWidth="1.5" />
        </svg>
        <span className="hidden sm:inline font-medium">{language.toUpperCase()} / {currency}</span>
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
          <div className="absolute right-0 z-20 mt-2 w-52 rounded-md border bg-white py-2 shadow-lg">
            <div className="px-3 pb-1">
              <p className="text-xs font-semibold uppercase tracking-wider text-gray-400">
                {t('common.language')}
              </p>
              <div className="mt-1 flex flex-col gap-0.5">
                {SUPPORTED_LANGUAGES.map(({ code, label }) => (
                  <button
                    key={code}
                    onClick={() => { handleLanguageChange(code); setOpen(false) }}
                    className={cn(
                      'w-full rounded px-2 py-1.5 text-left text-sm transition-colors',
                      language === code
                        ? 'bg-blue-50 text-blue-700 font-medium'
                        : 'text-gray-700 hover:bg-gray-100',
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>
            <div className="mx-3 my-2 border-t" />
            <div className="px-3">
              <p className="text-xs font-semibold uppercase tracking-wider text-gray-400">
                {t('common.currency')}
              </p>
              <div className="mt-1 flex flex-col gap-0.5">
                {SUPPORTED_CURRENCIES.map((cur) => (
                  <button
                    key={cur}
                    onClick={() => { handleCurrencyChange(cur); setOpen(false) }}
                    className={cn(
                      'w-full rounded px-2 py-1.5 text-left text-sm transition-colors',
                      currency === cur
                        ? 'bg-blue-50 text-blue-700 font-medium'
                        : 'text-gray-700 hover:bg-gray-100',
                    )}
                  >
                    {cur}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
