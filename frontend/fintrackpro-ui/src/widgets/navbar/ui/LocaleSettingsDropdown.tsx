import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocaleStore } from '@/features/locale'
import { useUpdateUserPreferences } from '@/entities/user-preferences'
import { SUPPORTED_LANGUAGES, type Language } from '@/shared/i18n'
import { cn } from '@/shared/lib/cn'
import type { Theme } from '@/features/locale/model/localeStore'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

export function LocaleSettingsDropdown() {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const language = useLocaleStore((s) => s.language)
  const currency = useLocaleStore((s) => s.currency)
  const theme = useLocaleStore((s) => s.theme)
  const setLanguage = useLocaleStore((s) => s.setLanguage)
  const setCurrency = useLocaleStore((s) => s.setCurrency)
  const setTheme = useLocaleStore((s) => s.setTheme)
  const updatePreferences = useUpdateUserPreferences()

  function handleLanguageChange(lang: Language) {
    setLanguage(lang)
    updatePreferences.mutate({ language: lang, currency })
  }

  function handleCurrencyChange(cur: string) {
    setCurrency(cur)
    updatePreferences.mutate({ language, currency: cur })
  }

  function handleThemeChange(t: Theme) {
    setTheme(t)
    setOpen(false)
  }

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1 rounded-md px-2 py-1.5 text-sm text-gray-600 hover:bg-gray-100 transition-colors dark:text-slate-400 dark:hover:bg-white/5"
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
          <div className="absolute right-0 z-20 mt-2 w-52 rounded-md border bg-white py-2 shadow-lg dark:bg-[#161a25] dark:border-white/10">
            <div className="px-3 pb-1">
              <p className="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500">
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
                        ? 'bg-blue-50 text-blue-700 font-medium dark:bg-blue-500/15 dark:text-blue-400'
                        : 'text-gray-700 hover:bg-gray-100 dark:text-slate-300 dark:hover:bg-white/5',
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>
            <div className="mx-3 my-2 border-t dark:border-white/6" />
            <div className="px-3">
              <p className="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500">
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
                        ? 'bg-blue-50 text-blue-700 font-medium dark:bg-blue-500/15 dark:text-blue-400'
                        : 'text-gray-700 hover:bg-gray-100 dark:text-slate-300 dark:hover:bg-white/5',
                    )}
                  >
                    {cur}
                  </button>
                ))}
              </div>
            </div>
            <div className="mx-3 my-2 border-t dark:border-white/6" />
            <div className="px-3">
              <p className="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-slate-500">
                {t('common.theme')}
              </p>
              <div className="mt-1 flex gap-1">
                <button
                  onClick={() => handleThemeChange('light')}
                  className={cn(
                    'flex flex-1 items-center justify-center gap-1.5 rounded px-2 py-1.5 text-sm transition-colors',
                    theme === 'light'
                      ? 'bg-blue-50 text-blue-700 font-medium dark:bg-blue-500/15 dark:text-blue-400'
                      : 'text-gray-700 hover:bg-gray-100 dark:text-slate-300 dark:hover:bg-white/5',
                  )}
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                    <circle cx="12" cy="12" r="5" />
                    <path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42" />
                  </svg>
                  {t('common.light')}
                </button>
                <button
                  onClick={() => handleThemeChange('dark')}
                  className={cn(
                    'flex flex-1 items-center justify-center gap-1.5 rounded px-2 py-1.5 text-sm transition-colors',
                    theme === 'dark'
                      ? 'bg-blue-50 text-blue-700 font-medium dark:bg-blue-500/15 dark:text-blue-400'
                      : 'text-gray-700 hover:bg-gray-100 dark:text-slate-300 dark:hover:bg-white/5',
                  )}
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                    <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
                  </svg>
                  {t('common.dark')}
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
