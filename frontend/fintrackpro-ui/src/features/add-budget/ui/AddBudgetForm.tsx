import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useCreateBudget } from '@/entities/budget'
import { useLocaleStore } from '@/features/locale'
import { errorToastMessage } from '@/shared/lib/apiError'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

interface Props {
  month: string
  onAdded?: () => void
}

export function AddBudgetForm({ month, onAdded }: Props) {
  const { t } = useTranslation()
  const { mutate, isPending } = useCreateBudget()
  const defaultCurrency = useLocaleStore((s) => s.currency)
  const [category, setCategory] = useState('')
  const [limit, setLimit] = useState('')
  const [currency, setCurrency] = useState(defaultCurrency)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate(
      { category, limitAmount: parseFloat(limit), currency, month },
      {
        onSuccess: () => { setCategory(''); setLimit(''); onAdded?.() },
        onError: (err) => toast.error(errorToastMessage(err)),
      },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 sm:flex-row sm:items-end sm:gap-2">
      <div className="flex-1">
        <label className="block text-xs text-gray-500 mb-1">{t('transactions.category')}</label>
        <input
          type="text"
          placeholder="e.g. Food"
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          required
          className="w-full rounded-md border px-3 py-2 text-sm"
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
        disabled={isPending}
        className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white disabled:opacity-50"
      >
        {isPending ? '...' : t('budgets.addBudget')}
      </button>
    </form>
  )
}
