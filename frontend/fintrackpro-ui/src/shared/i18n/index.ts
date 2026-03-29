import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import en from './en'
import vi from './vi'

export type Language = 'en' | 'vi'

export const SUPPORTED_LANGUAGES: { code: Language; label: string }[] = [
  { code: 'en', label: 'English' },
  { code: 'vi', label: 'Tiếng Việt' },
]

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      vi: { translation: vi },
    },
    lng: 'en',
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false,
    },
  })

export default i18n
