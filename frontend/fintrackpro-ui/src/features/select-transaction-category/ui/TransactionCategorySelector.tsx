import { useTranslation } from 'react-i18next'
import { useTransactionCategories } from '@/entities/transaction-category'
import { useLocaleStore } from '@/features/locale'
import type { TransactionType } from '@/entities/transaction/model/types'

interface TransactionCategorySelectorProps {
  type: TransactionType
  value: string
  onChange: (id: string) => void
  lastUsedId?: string | null
}

export function TransactionCategorySelector({
  type,
  value,
  onChange,
  lastUsedId,
}: TransactionCategorySelectorProps) {
  const { t } = useTranslation()
  const { data: categories, isLoading } = useTransactionCategories(type)
  const language = useLocaleStore((s) => s.language)

  const getLabel = (labelEn: string, labelVi: string) =>
    language === 'vi' ? labelVi : labelEn

  const systemCategories = categories?.filter((c) => c.isSystem) ?? []
  const userCategories = categories?.filter((c) => !c.isSystem) ?? []

  return (
    <select
      value={value || ''}
      onChange={(e) => onChange(e.target.value)}
      disabled={isLoading}
      className="w-full rounded-md border px-3 py-2 text-sm"
    >
      <option value="" disabled>
        {isLoading ? t('transactionCategories.loading') : t('transactionCategories.selectCategory')}
      </option>

      {systemCategories.length > 0 && (
        <optgroup label={t('transactionCategories.categories')}>
          {systemCategories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.icon} {getLabel(c.labelEn, c.labelVi)}{c.id === lastUsedId ? ' ★' : ''}
            </option>
          ))}
        </optgroup>
      )}

      {userCategories.length > 0 && (
        <optgroup label={t('transactionCategories.myCategories')}>
          {userCategories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.icon} {getLabel(c.labelEn, c.labelVi)}{c.id === lastUsedId ? ' ★' : ''}
            </option>
          ))}
        </optgroup>
      )}
    </select>
  )
}
