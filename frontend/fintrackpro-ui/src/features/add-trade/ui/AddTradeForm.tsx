import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useCreateTrade } from '@/entities/trade'
import { useLocaleStore } from '@/features/locale'
import { cn } from '@/shared/lib/cn'
import { createTradeSchema, type CreateTradeInput } from '@/shared/lib/tradeSchema'
import { classifyApiError, errorToastMessage, type ProblemDetails } from '@/shared/lib/apiError'

const SUPPORTED_CURRENCIES = ['USD', 'EUR', 'GBP', 'VND']

type FieldErrors = Partial<Record<keyof CreateTradeInput, string>>

export function AddTradeForm() {
  const { t } = useTranslation()
  const { mutate, isPending } = useCreateTrade()
  const defaultCurrency = useLocaleStore((s) => s.currency)
  const [direction, setDirection] = useState<'Long' | 'Short'>('Long')
  const [symbol, setSymbol] = useState('')
  const [entryPrice, setEntryPrice] = useState('')
  const [exitPrice, setExitPrice] = useState('')
  const [positionSize, setPositionSize] = useState('')
  const [fees, setFees] = useState('')
  const [currency, setCurrency] = useState(defaultCurrency)
  const [notes, setNotes] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [serverErrors, setServerErrors] = useState<ProblemDetails | null>(null)

  function clearFieldError(field: keyof FieldErrors) {
    if (fieldErrors[field]) {
      setFieldErrors((prev) => { const next = { ...prev }; delete next[field]; return next })
    }
  }

  function validateField(field: keyof CreateTradeInput, value: unknown) {
    const result = createTradeSchema.shape[field].safeParse(value)
    if (!result.success) {
      setFieldErrors((prev) => ({ ...prev, [field]: result.error.issues[0].message }))
    } else {
      setFieldErrors((prev) => { const next = { ...prev }; delete next[field]; return next })
    }
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setServerErrors(null)

    const raw = {
      symbol,
      direction,
      entryPrice: parseFloat(entryPrice),
      exitPrice: parseFloat(exitPrice),
      positionSize: parseFloat(positionSize),
      fees: parseFloat(fees) || 0,
      currency,
      notes: notes || null,
    }

    const result = createTradeSchema.safeParse(raw)
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
      { ...result.data, rateToUsd: 1 }, // server resolves actual rate
      {
        onSuccess: () => {
          setSymbol('')
          setEntryPrice('')
          setExitPrice('')
          setPositionSize('')
          setFees('')
          setNotes('')
          setFieldErrors({})
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
    <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border p-4">
      <h2 className="text-lg font-semibold">{t('trades.addTrade')}</h2>

      <div className="flex gap-2">
        {(['Long', 'Short'] as const).map((d) => (
          <button
            key={d}
            type="button"
            onClick={() => setDirection(d)}
            className={cn(
              'flex-1 rounded-md py-2 text-sm font-medium',
              direction === d
                ? d === 'Long'
                  ? 'bg-green-600 text-white'
                  : 'bg-red-600 text-white'
                : 'bg-gray-100 text-gray-700',
            )}
          >
            {d === 'Long' ? t('trades.long') : t('trades.short')}
          </button>
        ))}
      </div>

      <div className="flex gap-2">
        <div className="flex flex-col gap-1 flex-1">
          <input
            type="text"
            placeholder={`${t('trades.symbol')} (e.g. BTCUSDT)`}
            value={symbol}
            onChange={(e) => { setSymbol(e.target.value.toUpperCase()); clearFieldError('symbol') }}
            onBlur={() => validateField('symbol', symbol)}
            className={cn(
              'w-full rounded-md border px-3 py-2 text-sm font-mono',
              fieldErrors.symbol && 'border-red-400',
            )}
          />
          {fieldErrors.symbol && <p className="text-xs text-red-600">{fieldErrors.symbol}</p>}
        </div>
        <select
          value={currency}
          onChange={(e) => setCurrency(e.target.value)}
          className="rounded-md border px-3 py-2 text-sm self-start"
        >
          {SUPPORTED_CURRENCIES.map((c) => (
            <option key={c} value={c}>{c}</option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
        <div className="flex flex-col gap-1">
          <input
            type="number"
            placeholder={t('trades.entryPrice')}
            value={entryPrice}
            onChange={(e) => { setEntryPrice(e.target.value); clearFieldError('entryPrice') }}
            onBlur={() => validateField('entryPrice', parseFloat(entryPrice))}
            className={cn(
              'rounded-md border px-3 py-2 text-sm',
              fieldErrors.entryPrice && 'border-red-400',
            )}
          />
          {fieldErrors.entryPrice && (
            <p className="text-xs text-red-600">{fieldErrors.entryPrice}</p>
          )}
        </div>
        <div className="flex flex-col gap-1">
          <input
            type="number"
            placeholder={t('trades.exitPrice')}
            value={exitPrice}
            onChange={(e) => { setExitPrice(e.target.value); clearFieldError('exitPrice') }}
            onBlur={() => validateField('exitPrice', parseFloat(exitPrice))}
            className={cn(
              'rounded-md border px-3 py-2 text-sm',
              fieldErrors.exitPrice && 'border-red-400',
            )}
          />
          {fieldErrors.exitPrice && (
            <p className="text-xs text-red-600">{fieldErrors.exitPrice}</p>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
        <div className="flex flex-col gap-1">
          <input
            type="number"
            placeholder={t('trades.positionSize')}
            value={positionSize}
            onChange={(e) => { setPositionSize(e.target.value); clearFieldError('positionSize') }}
            onBlur={() => validateField('positionSize', parseFloat(positionSize))}
            className={cn(
              'rounded-md border px-3 py-2 text-sm',
              fieldErrors.positionSize && 'border-red-400',
            )}
          />
          {fieldErrors.positionSize && (
            <p className="text-xs text-red-600">{fieldErrors.positionSize}</p>
          )}
        </div>
        <div className="flex flex-col gap-1">
          <input
            type="number"
            placeholder={t('trades.fees')}
            value={fees}
            onChange={(e) => { setFees(e.target.value); clearFieldError('fees') }}
            onBlur={() => validateField('fees', parseFloat(fees) || 0)}
            className={cn(
              'rounded-md border px-3 py-2 text-sm',
              fieldErrors.fees && 'border-red-400',
            )}
          />
          {fieldErrors.fees && <p className="text-xs text-red-600">{fieldErrors.fees}</p>}
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <input
          type="text"
          placeholder={t('trades.notes')}
          value={notes}
          onChange={(e) => { setNotes(e.target.value); clearFieldError('notes') }}
          onBlur={() => validateField('notes', notes || null)}
          className={cn(
            'w-full rounded-md border px-3 py-2 text-sm',
            fieldErrors.notes && 'border-red-400',
          )}
        />
        {fieldErrors.notes && <p className="text-xs text-red-600">{fieldErrors.notes}</p>}
      </div>

      {serverErrors && (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
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
        disabled={isPending}
        className="w-full rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
      >
        {isPending ? t('common.loading') : t('trades.addTrade')}
      </button>
    </form>
  )
}
