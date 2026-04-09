import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { useTransactionCategories } from '@/entities/transaction-category'
import { useLocaleStore } from '@/features/locale'
import type { TransactionType } from '@/entities/transaction/model/types'

interface TransactionCategorySelectorProps {
  type: TransactionType
  value: string
  onChange: (id: string) => void
  lastUsedId?: string | null
  showManageLink?: boolean
}

export function TransactionCategorySelector({
  type,
  value,
  onChange,
  lastUsedId,
  showManageLink = true,
}: TransactionCategorySelectorProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { data: categories, isLoading } = useTransactionCategories(type)
  const language = useLocaleStore((s) => s.language)

  const getLabel = (labelEn: string, labelVi: string) =>
    language === 'vi' ? labelVi : labelEn

  const systemCategories = categories?.filter((c) => c.isSystem) ?? []
  const userCategories = categories?.filter((c) => !c.isSystem) ?? []

  return (
    <div>
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

      {showManageLink && <div className="mt-1.5 flex items-center gap-1">
        <svg
          width="12"
          height="12"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#94a3b8"
          strokeWidth="2.5"
          aria-hidden="true"
        >
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
        <button
          type="button"
          onClick={() => navigate('/settings')}
          className="text-xs text-gray-400 hover:text-blue-600 transition-colors"
        >
          {t('transactionCategories.manageCategories')}
        </button>
      </div>}
    </div>
  )
}
