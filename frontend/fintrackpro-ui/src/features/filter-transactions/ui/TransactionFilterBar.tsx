import type { TransactionQueryParams } from '@/entities/transaction'
import type { TransactionCategory } from '@/entities/transaction-category'
import { useTranslation } from 'react-i18next'
import { useLocaleStore } from '@/features/locale'

export interface TransactionFilters {
  search: string
  month: string
  type: '' | 'Income' | 'Expense'
  categoryId: string
}

interface TransactionFilterBarProps {
  value: TransactionFilters
  onChange: (next: Partial<TransactionFilters>) => void
  categories?: TransactionCategory[]
  monthOptions?: string[]
}

export function TransactionFilterBar({
  value,
  onChange,
  categories = [],
  monthOptions = [],
}: TransactionFilterBarProps) {
  const { t } = useTranslation()
  const language = useLocaleStore((s) => s.language)

  return (
    <div className="flex flex-wrap items-end gap-2">
      {/* Search */}
      <input
        type="search"
        placeholder={t('common.search')}
        value={value.search}
        onChange={(e) => onChange({ search: e.target.value })}
        className="rounded-md border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white dark:placeholder:text-slate-500 w-44"
      />

      {/* Month */}
      {monthOptions.length > 0 && (
        <select
          value={value.month}
          onChange={(e) => onChange({ month: e.target.value })}
          className="rounded-md border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
        >
          <option value="">{t('common.allMonths')}</option>
          {monthOptions.map((m) => (
            <option key={m} value={m}>{m}</option>
          ))}
        </select>
      )}

      {/* Type toggle */}
      <div className="flex rounded-md border border-gray-300 dark:border-white/12 overflow-hidden text-sm">
        {(['', 'Income', 'Expense'] as const).map((opt) => (
          <button
            key={opt || 'all'}
            onClick={() => onChange({ type: opt })}
            className={
              value.type === opt
                ? 'px-3 py-1.5 bg-blue-600 text-white font-medium'
                : 'px-3 py-1.5 text-gray-600 dark:text-slate-300 hover:bg-gray-50 dark:hover:bg-white/5'
            }
          >
            {opt === '' ? t('common.all') : opt === 'Income' ? t('transactions.income') : t('transactions.expense')}
          </button>
        ))}
      </div>

      {/* Category */}
      {categories.length > 0 && (
        <select
          value={value.categoryId}
          onChange={(e) => onChange({ categoryId: e.target.value })}
          className="rounded-md border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
        >
          <option value="">{t('common.allCategories')}</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.icon} {language === 'vi' ? c.labelVi : c.labelEn}
            </option>
          ))}
        </select>
      )}
    </div>
  )
}

// Re-export type alias for page layer
export type { TransactionQueryParams }
