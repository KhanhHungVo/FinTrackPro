import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useCreateTransaction } from '@/entities/transaction'
import { useLocaleStore } from '@/features/locale'
import { TransactionCategorySelector, useLastUsedTransactionCategory } from '@/features/select-transaction-category'
import { cn } from '@/shared/lib/cn'
import { errorToastMessage } from '@/shared/lib/apiError'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

export function AddTransactionForm() {
  const { t } = useTranslation()
  const { mutate, isPending } = useCreateTransaction()
  const defaultCurrency = useLocaleStore((s) => s.currency)
  const { getLastUsedId, setLastUsedId } = useLastUsedTransactionCategory()

  const [type, setType] = useState<'Income' | 'Expense'>('Expense')
  const [amount, setAmount] = useState('')
  const [currency, setCurrency] = useState(defaultCurrency)
  const [categoryId, setCategoryId] = useState(getLastUsedId() ?? '')
  const [note, setNote] = useState('')
  const currentMonth = new Date().toISOString().slice(0, 7)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate(
      {
        type,
        amount: parseFloat(amount),
        currency,
        categoryId,
        note: note || null,
        budgetMonth: currentMonth,
      },
      {
        onSuccess: () => {
          setLastUsedId(categoryId)
          setAmount('')
          setNote('')
        },
        onError: (err) => toast.error(errorToastMessage(err)),
      },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border p-4">
      <h2 className="text-lg font-semibold">{t('transactions.addTransaction')}</h2>

      <div className="flex gap-2">
        {(['Income', 'Expense'] as const).map((ty) => (
          <button
            key={ty}
            type="button"
            onClick={() => { setType(ty); setCategoryId('') }}
            className={cn(
              'flex-1 rounded-md py-2 text-sm font-medium',
              type === ty
                ? ty === 'Income'
                  ? 'bg-green-600 text-white'
                  : 'bg-red-600 text-white'
                : 'bg-gray-100 text-gray-700',
            )}
          >
            {ty === 'Income' ? t('transactions.income') : t('transactions.expense')}
          </button>
        ))}
      </div>

      <div className="flex gap-2">
        <input
          type="number"
          placeholder={t('transactions.amount')}
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          required
          className="flex-1 rounded-md border px-3 py-2 text-sm"
        />
        <select
          value={currency}
          onChange={(e) => setCurrency(e.target.value)}
          className="rounded-md border px-3 py-2 text-sm"
        >
          {SUPPORTED_CURRENCIES.map((c) => (
            <option key={c} value={c}>{c}</option>
          ))}
        </select>
      </div>

      <TransactionCategorySelector
        type={type}
        value={categoryId}
        onChange={setCategoryId}
        lastUsedId={getLastUsedId()}
      />

      <textarea
        placeholder={t('transactions.note')}
        value={note}
        onChange={(e) => setNote(e.target.value)}
        rows={4}
        className="w-full rounded-md border px-3 py-2 text-sm resize-y"
      />

      <button
        type="submit"
        disabled={isPending || !categoryId}
        className="w-full rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
      >
        {isPending ? t('common.loading') : t('transactions.addTransaction')}
      </button>
    </form>
  )
}
