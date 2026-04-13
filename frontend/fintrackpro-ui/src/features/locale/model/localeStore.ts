import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { Language } from '@/shared/i18n'

const SUPPORTED_CURRENCIES = ['USD', 'VND'] as const
export type SupportedCurrency = (typeof SUPPORTED_CURRENCIES)[number]

export type Theme = 'light' | 'dark'

interface LocaleState {
  language: Language
  currency: string
  theme: Theme
  setLanguage: (language: Language) => void
  setCurrency: (currency: string) => void
  setTheme: (theme: Theme) => void
}

export const useLocaleStore = create<LocaleState>()(
  persist(
    (set) => ({
      language: 'en',
      currency: 'USD',
      theme: 'light',
      setLanguage: (language) => set({ language }),
      setCurrency: (currency) => set({ currency }),
      setTheme: (theme) => set({ theme }),
    }),
    { name: 'fintrackpro-locale' },
  ),
)
