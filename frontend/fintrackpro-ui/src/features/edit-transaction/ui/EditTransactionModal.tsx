import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useUpdateTransaction } from '@/entities/transaction'
import type { Transaction, TransactionType } from '@/entities/transaction'
import { TransactionCategorySelector } from '@/features/select-transaction-category'
import { cn } from '@/shared/lib/cn'
import { classifyApiError, errorToastMessage, type ProblemDetails } from '@/shared/lib/apiError'
import { z } from 'zod'
import { X } from 'lucide-react'
import { Button, IconButton } from '@/shared/ui'

const SUPPORTED_CURRENCIES = ['USD', 'VND']

const updateTransactionSchema = z.object({
  type: z.enum(['Income', 'Expense']),
  amount: z.number().positive('Amount must be greater than zero'),
  currency: z.string().min(1, 'Currency is required').max(3),
  categoryId: z.string().min(1, 'Category is required'),
  note: z.string()
    .max(500, 'Note must be 500 characters or fewer')
    .regex(/^[^<>]*$/, 'Note must not contain angle brackets (< >)')
    .nullable()
    .optional(),
})

type UpdateTransactionInput = z.infer<typeof updateTransactionSchema>
type FieldErrors = Partial<Record<keyof UpdateTransactionInput, string>>

interface EditTransactionModalProps {
  transaction: Transaction | null
  onClose: () => void
}

type FormState = {
  type: TransactionType
  amount: string
  currency: string
  categoryId: string
  note: string
  fieldErrors: FieldErrors
  serverErrors: ProblemDetails | null
}

function formStateFromTransaction(transaction: Transaction): FormState {
  return {
    type: transaction.type,
    amount: String(transaction.amount),
    currency: transaction.currency,
    categoryId: transaction.categoryId ?? '',
    note: transaction.note ?? '',
    fieldErrors: {},
    serverErrors: null,
  }
}

const emptyForm: FormState = {
  type: 'Expense', amount: '', currency: 'USD', categoryId: '', note: '', fieldErrors: {}, serverErrors: null,
}

export function EditTransactionModal({ transaction, onClose }: EditTransactionModalProps) {
  const { t } = useTranslation()
  const { mutate, isPending } = useUpdateTransaction()

  const [form, setForm] = useState<FormState>(() =>
    transaction ? formStateFromTransaction(transaction) : emptyForm,
  )

  const { type, amount, currency, categoryId, note, fieldErrors, serverErrors } = form

  const patch = (partial: Partial<FormState>) => setForm((prev) => ({ ...prev, ...partial }))

  const clearFieldError = (field: keyof FieldErrors) => {
    if (form.fieldErrors[field]) {
      setForm((prev) => {
        const next = { ...prev.fieldErrors }
        delete next[field]
        return { ...prev, fieldErrors: next }
      })
    }
  }

  const [prevTransaction, setPrevTransaction] = useState(transaction)
  if (prevTransaction !== transaction) {
    setPrevTransaction(transaction)
    setForm(transaction ? formStateFromTransaction(transaction) : emptyForm)
  }

  if (!transaction) return null

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    patch({ serverErrors: null })

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
      patch({ fieldErrors: errors })
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
            patch({ serverErrors: kind.details })
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
          <IconButton
            type="button"
            variant="ghost"
            size="sm"
            onClick={onClose}
            aria-label={t('common.cancel')}
          >
            <X size={16} aria-hidden="true" />
          </IconButton>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 p-4">
          {/* Type toggle */}
          <div className="flex gap-2">
            {(['Income', 'Expense'] as const).map((ty) => (
              <button
                key={ty}
                type="button"
                onClick={() => patch({ type: ty, categoryId: '', fieldErrors: { ...fieldErrors, categoryId: undefined } })}
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
                onChange={(e) => { patch({ amount: e.target.value }); clearFieldError('amount') }}
                className={cn(
                  'w-full rounded-md border px-3 py-2 text-sm dark:bg-slate-800 dark:border-white/10 dark:text-white',
                  fieldErrors.amount && 'border-red-400',
                )}
              />
              {fieldErrors.amount && <p className="text-xs text-red-600">{fieldErrors.amount}</p>}
            </div>
            <select
              value={currency}
              onChange={(e) => patch({ currency: e.target.value })}
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
              onChange={(id) => { patch({ categoryId: id }); clearFieldError('categoryId') }}
              showManageLink={false}
            />
            {fieldErrors.categoryId && (
              <p className="text-xs text-red-600">{fieldErrors.categoryId}</p>
            )}
          </div>

          {/* Note */}
          <div className="flex flex-col gap-1">
            <textarea
              placeholder={t('transactions.note')}
              value={note}
              onChange={(e) => { patch({ note: e.target.value }); clearFieldError('note') }}
              onBlur={() => {
                if (!note) return
                const result = updateTransactionSchema.shape.note.safeParse(note)
                if (!result.success) {
                  patch({ fieldErrors: { ...fieldErrors, note: result.error.issues[0].message } })
                } else {
                  clearFieldError('note')
                }
              }}
              rows={3}
              className={cn(
                'w-full rounded-md border px-3 py-2 text-sm resize-y dark:bg-slate-800 dark:text-white',
                fieldErrors.note ? 'border-red-400 dark:border-red-500' : 'dark:border-white/10',
              )}
            />
            {fieldErrors.note && <p className="text-xs text-red-600 dark:text-red-400">{fieldErrors.note}</p>}
          </div>

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
            <Button type="button" variant="secondary" size="md" onClick={onClose} className="flex-1">
              {t('common.cancel')}
            </Button>
            <Button type="submit" variant="primary" size="md" loading={isPending} disabled={!categoryId} className="flex-1">
              {t('common.save')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
