import { useCallback } from 'react'
import { useTransactionCategories } from '@/entities/transaction-category'
import { useLocaleStore } from '@/features/locale'

export function useCategoryLabel() {
  const language = useLocaleStore((s) => s.language)
  const { data: categories } = useTransactionCategories()

  const resolve = useCallback(
    (slug: string): string => {
      const cat = categories?.find((c) => c.slug === slug)
      if (!cat) return slug
      return `${cat.icon} ${language === 'vi' ? cat.labelVi : cat.labelEn}`
    },
    [categories, language],
  )

  return resolve
}
