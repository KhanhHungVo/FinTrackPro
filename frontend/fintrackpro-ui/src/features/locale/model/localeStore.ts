import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { Language } from '@/shared/i18n'

const SUPPORTED_CURRENCIES = ['USD', 'EUR', 'GBP', 'VND'] as const
export type SupportedCurrency = (typeof SUPPORTED_CURRENCIES)[number]

interface LocaleState {
  language: Language
  currency: string
  setLanguage: (language: Language) => void
  setCurrency: (currency: string) => void
}

export const useLocaleStore = create<LocaleState>()(
  persist(
    (set) => ({
      language: 'en',
      currency: 'USD',
      setLanguage: (language) => set({ language }),
      setCurrency: (currency) => set({ currency }),
    }),
    { name: 'fintrackpro-locale' },
  ),
)
