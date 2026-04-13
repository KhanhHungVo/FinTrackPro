import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useUpdateTransaction } from '@/entities/transaction'
import type { Transaction, TransactionType } from '@/entities/transaction'
import { TransactionCategorySelector } from '@/features/select-transaction-category'
import { cn } from '@/shared/lib/cn'
import { classifyApiError, errorToastMessage, type ProblemDetails } from '@/shared/lib/apiError'
import { z } from 'zod'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

const updateTransactionSchema = z.object({
  type: z.enum(['Income', 'Expense']),
  amount: z.number().positive('Amount must be greater than zero'),
  currency: z.string().min(1, 'Currency is required').max(3),
  categoryId: z.string().min(1, 'Category is required'),
  note: z.string().max(500).nullable().optional(),
})

type UpdateTransactionInput = z.infer<typeof updateTransactionSchema>
type FieldErrors = Partial<Record<keyof UpdateTransactionInput, string>>

interface EditTransactionModalProps {
  transaction: Transaction | null
  onClose: () => void
}

export function EditTransactionModal({ transaction, onClose }: EditTransactionModalProps) {
  const { t } = useTranslation()
  const { mutate, isPending } = useUpdateTransaction()

  const [type, setType] = useState<TransactionType>('Expense')
  const [amount, setAmount] = useState('')
  const [currency, setCurrency] = useState('USD')
  const [categoryId, setCategoryId] = useState('')
  const [note, setNote] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [serverErrors, setServerErrors] = useState<ProblemDetails | null>(null)

  useEffect(() => {
    if (transaction) {
      setType(transaction.type)
      setAmount(String(transaction.amount))
      setCurrency(transaction.currency)
      setCategoryId(transaction.categoryId ?? '')
      setNote(transaction.note ?? '')
      setFieldErrors({})
      setServerErrors(null)
    }
  }, [transaction])

  if (!transaction) return null

  function clearFieldError(field: keyof FieldErrors) {
    if (fieldErrors[field]) {
      setFieldErrors((prev) => { const next = { ...prev }; delete next[field]; return next })
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setServerErrors(null)

    const raw = {
      type,
      amount: parseFloat(amount),
      currency,
      categoryId,
      note: note || null,
    }

    const result = updateTransactionSchema.safeParse(raw)
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
        id: transaction.id,
        type: result.data.type,
        amount: result.data.amount,
        currency: result.data.currency,
        category: transaction.category,
        note: result.data.note,
        categoryId: result.data.categoryId,
      },
      {
        onSuccess: onClose,
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
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={(e) => { if (e.target === e.currentTarget) onClose() }}
    >
      <div className="w-full max-w-md rounded-lg bg-white shadow-xl dark:bg-[#161a25]">
        <div className="flex items-center justify-between border-b px-4 py-3 dark:border-white/6">
          <h2 className="text-lg font-semibold">{t('common.edit')} {t('transactions.title')}</h2>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-xl leading-none dark:text-slate-500 dark:hover:text-slate-300"
            aria-label={t('common.cancel')}
          >
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 p-4">
          {/* Type toggle */}
          <div className="flex gap-2">
            {(['Income', 'Expense'] as const).map((ty) => (
              <button
                key={ty}
                type="button"
                onClick={() => { setType(ty); setCategoryId(''); clearFieldError('categoryId') }}
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

          {/* Amount + currency */}
          <div className="flex gap-2">
            <div className="flex flex-col gap-1 flex-1">
              <input
                type="number"
                placeholder={t('transactions.amount')}
                value={amount}
                onChange={(e) => { setAmount(e.target.value); clearFieldError('amount') }}
                className={cn(
                  'w-full rounded-md border px-3 py-2 text-sm dark:bg-slate-800 dark:border-white/10 dark:text-white',
                  fieldErrors.amount && 'border-red-400',
                )}
              />
              {fieldErrors.amount && <p className="text-xs text-red-600">{fieldErrors.amount}</p>}
            </div>
            <select
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className="rounded-md border px-3 py-2 text-sm self-start dark:bg-slate-800 dark:border-white/10 dark:text-white"
            >
              {SUPPORTED_CURRENCIES.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </div>

          {/* Category selector */}
          <div className="flex flex-col gap-1">
            <TransactionCategorySelector
              type={type}
              value={categoryId}
              onChange={(id) => { setCategoryId(id); clearFieldError('categoryId') }}
              showManageLink={false}
            />
            {fieldErrors.categoryId && (
              <p className="text-xs text-red-600">{fieldErrors.categoryId}</p>
            )}
          </div>

          {/* Note */}
          <textarea
            placeholder={t('transactions.note')}
            value={note}
            onChange={(e) => setNote(e.target.value)}
            rows={3}
            className="w-full rounded-md border px-3 py-2 text-sm resize-y dark:bg-slate-800 dark:border-white/10 dark:text-white"
          />

          {/* Server-side validation errors */}
          {serverErrors && (
            <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-500/10 dark:border-red-500/20">
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

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-md border py-2 text-sm text-gray-600 hover:bg-gray-50 dark:border-white/10 dark:text-slate-400 dark:hover:bg-white/5"
            >
              {t('common.cancel')}
            </button>
            <button
              type="submit"
              disabled={isPending || !categoryId}
              className="flex-1 rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
            >
              {isPending ? t('common.loading') : t('common.save')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
