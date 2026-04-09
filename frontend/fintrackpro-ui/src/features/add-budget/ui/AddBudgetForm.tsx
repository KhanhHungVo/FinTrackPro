import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { useCreateBudget } from '@/entities/budget'
import { useTransactionCategories } from '@/entities/transaction-category'
import { useLocaleStore } from '@/features/locale'
import { TransactionCategorySelector } from '@/features/select-transaction-category'
import { errorToastMessage } from '@/shared/lib/apiError'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

interface Props {
  month: string
  onAdded?: () => void
}

export function AddBudgetForm({ month, onAdded }: Props) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { mutate, isPending } = useCreateBudget()
  const defaultCurrency = useLocaleStore((s) => s.currency)
  const { data: categories } = useTransactionCategories('Expense')
  const [categoryId, setCategoryId] = useState('')
  const [limit, setLimit] = useState('')
  const [currency, setCurrency] = useState(defaultCurrency)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const selected = categories?.find((c) => c.id === categoryId)
    if (!selected) return

    mutate(
      { category: selected.slug, limitAmount: parseFloat(limit), currency, month },
      {
        onSuccess: () => { setCategoryId(''); setLimit(''); onAdded?.() },
        onError: (err) => toast.error(errorToastMessage(err)),
      },
    )
  }

  return (
    <div className="flex flex-col gap-2">
      <form onSubmit={handleSubmit} className="flex flex-col gap-3 sm:flex-row sm:items-end sm:gap-2">
        <div className="flex-1">
          <label className="block text-xs text-gray-500 mb-1">{t('transactions.category')}</label>
          <TransactionCategorySelector
            type="Expense"
            value={categoryId}
            onChange={setCategoryId}
            showManageLink={false}
          />
        </div>
        <div className="w-full sm:w-36">
          <label className="block text-xs text-gray-500 mb-1">{t('budgets.limit')}</label>
          <input
            type="number"
            placeholder="500"
            value={limit}
            onChange={(e) => setLimit(e.target.value)}
            required
            className="w-full rounded-md border px-3 py-2 text-sm"
          />
        </div>
        <div className="w-full sm:w-24">
          <label className="block text-xs text-gray-500 mb-1">{t('common.currency')}</label>
          <select
            value={currency}
            onChange={(e) => setCurrency(e.target.value)}
            className="w-full rounded-md border px-3 py-2 text-sm"
          >
            {SUPPORTED_CURRENCIES.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>
        <button
          type="submit"
          disabled={isPending || !categoryId}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white disabled:opacity-50"
        >
          {isPending ? '...' : t('budgets.addBudget')}
        </button>
      </form>
      <button
        type="button"
        onClick={() => navigate('/settings')}
        className="flex items-center gap-1 w-fit text-xs text-gray-400 hover:text-blue-600 transition-colors"
      >
        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
        {t('transactionCategories.manageCategories')}
      </button>
    </div>
  )
}
