import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { useCreateTransaction } from '@/entities/transaction'
import { useLocaleStore } from '@/features/locale'
import { TransactionCategorySelector, useLastUsedTransactionCategory } from '@/features/select-transaction-category'
import { cn } from '@/shared/lib/cn'
import { classifyApiError, errorToastMessage, type ProblemDetails } from '@/shared/lib/apiError'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

const addTransactionSchema = z.object({
  amount: z.number({ error: 'Amount is required' }).positive('Amount must be greater than zero'),
  note: z.string()
    .max(500, 'Note must be 500 characters or fewer')
    .regex(/^[^<>]*$/, 'Note must not contain angle brackets (< >)')
    .nullable(),
})

type FieldErrors = { amount?: string; note?: string }

export function AddTransactionForm({ onSuccess }: { onSuccess?: () => void }) {
  const { t } = useTranslation()
  const { mutate, isPending } = useCreateTransaction()
  const defaultCurrency = useLocaleStore((s) => s.currency)
  const { getLastUsedId, setLastUsedId } = useLastUsedTransactionCategory()

  const [type, setType] = useState<'Income' | 'Expense'>('Expense')
  const [amount, setAmount] = useState('')
  const [currency, setCurrency] = useState(defaultCurrency)
  const [categoryId, setCategoryId] = useState(getLastUsedId() ?? '')
  const [note, setNote] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [serverErrors, setServerErrors] = useState<ProblemDetails | null>(null)
  const currentMonth = new Date().toISOString().slice(0, 7)

  function clearFieldError(field: keyof FieldErrors) {
    if (fieldErrors[field]) setFieldErrors((prev) => { const next = { ...prev }; delete next[field]; return next })
  }

  function validateAmount() {
    const result = addTransactionSchema.shape.amount.safeParse(parseFloat(amount))
    if (!result.success) {
      setFieldErrors((prev) => ({ ...prev, amount: result.error.issues[0].message }))
      return false
    }
    setFieldErrors((prev) => { const next = { ...prev }; delete next.amount; return next })
    return true
  }

  function validateNote() {
    if (!note) return true
    const result = addTransactionSchema.shape.note.safeParse(note)
    if (!result.success) {
      setFieldErrors((prev) => ({ ...prev, note: result.error.issues[0].message }))
      return false
    }
    setFieldErrors((prev) => { const next = { ...prev }; delete next.note; return next })
    return true
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setServerErrors(null)

    const raw = {
      amount: parseFloat(amount),
      note: note || null,
    }

    const result = addTransactionSchema.safeParse(raw)
    if (!result.success) {
      const errors: FieldErrors = {}
      for (const issue of result.error.issues) {
        const field = issue.path[0] as keyof FieldErrors
        if (!errors[field]) errors[field] = issue.message
      }
      setFieldErrors(errors)
      return
    }

    mutate(
      {
        type,
        amount: result.data.amount,
        currency,
        categoryId,
        note: result.data.note,
        budgetMonth: currentMonth,
      },
      {
        onSuccess: () => {
          setLastUsedId(categoryId)
          setAmount('')
          setNote('')
          setFieldErrors({})
          onSuccess?.()
        },
        onError: (err) => {
          const kind = classifyApiError(err)
          if (kind.type === 'validation') {
            setServerErrors(kind.details)
          } else {
            toast.error(errorToastMessage(err))
          }
        },
      },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="page-card space-y-4 p-4">
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
                : 'bg-gray-100 text-gray-700 dark:bg-white/5 dark:text-slate-300',
            )}
          >
            {ty === 'Income' ? t('transactions.income') : t('transactions.expense')}
          </button>
        ))}
      </div>

      <div className="flex gap-2 items-start">
        <div className="flex flex-col gap-1 flex-1">
          <input
            type="number"
            placeholder={t('transactions.amount')}
            value={amount}
            onChange={(e) => { setAmount(e.target.value); clearFieldError('amount') }}
            onBlur={validateAmount}
            className={cn(
              'w-full rounded-md border px-3 py-2 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:text-white',
              fieldErrors.amount ? 'border-red-400 dark:border-red-500' : 'border-gray-300 dark:border-white/12',
            )}
          />
          {fieldErrors.amount && <p className="text-xs text-red-600 dark:text-red-400">{fieldErrors.amount}</p>}
        </div>
        <select
          value={currency}
          onChange={(e) => setCurrency(e.target.value)}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
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

      <div className="flex flex-col gap-1">
        <textarea
          placeholder={t('transactions.note')}
          value={note}
          onChange={(e) => { setNote(e.target.value); clearFieldError('note') }}
          onBlur={validateNote}
          rows={4}
          className={cn(
            'w-full rounded-md border px-3 py-2 text-sm resize-y outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:text-white',
            fieldErrors.note ? 'border-red-400 dark:border-red-500' : 'border-gray-300 dark:border-white/12',
          )}
        />
        {fieldErrors.note && <p className="text-xs text-red-600 dark:text-red-400">{fieldErrors.note}</p>}
      </div>

      {serverErrors && (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-500/10 dark:text-red-400 dark:border-red-500/20">
          <p className="font-medium">{serverErrors.title ?? 'Validation failed'}</p>
          {serverErrors.errors && (
            <ul className="mt-1 list-disc list-inside space-y-0.5">
              {Object.entries(serverErrors.errors).flatMap(([, msgs]) =>
                msgs.map((m, i) => <li key={i}>{m}</li>),
              )}
            </ul>
          )}
        </div>
      )}

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
