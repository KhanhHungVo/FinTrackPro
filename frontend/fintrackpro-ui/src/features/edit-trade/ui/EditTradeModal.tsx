import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useUpdateTrade } from '@/entities/trade'
import type { Trade, TradeDirection } from '@/entities/trade'
import { cn } from '@/shared/lib/cn'
import { updateTradeSchema, type UpdateTradeInput } from '@/shared/lib/tradeSchema'
import { classifyApiError, errorToastMessage, type ProblemDetails } from '@/shared/lib/apiError'

const SUPPORTED_CURRENCIES = ['USD', 'EUR', 'GBP', 'VND']

interface EditTradeModalProps {
  trade: Trade | null
  onClose: () => void
}

type FieldErrors = Partial<Record<keyof UpdateTradeInput, string>>

export function EditTradeModal({ trade, onClose }: EditTradeModalProps) {
  const { t } = useTranslation()
  const { mutate, isPending } = useUpdateTrade()
  const [direction, setDirection] = useState<TradeDirection>('Long')
  const [symbol, setSymbol] = useState('')
  const [entryPrice, setEntryPrice] = useState('')
  const [exitPrice, setExitPrice] = useState('')
  const [positionSize, setPositionSize] = useState('')
  const [fees, setFees] = useState('')
  const [currency, setCurrency] = useState('USD')
  const [notes, setNotes] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [serverErrors, setServerErrors] = useState<ProblemDetails | null>(null)

  useEffect(() => {
    if (trade) {
      setDirection(trade.direction)
      setSymbol(trade.symbol)
      setEntryPrice(String(trade.entryPrice))
      setExitPrice(String(trade.exitPrice))
      setPositionSize(String(trade.positionSize))
      setFees(String(trade.fees))
      setCurrency(trade.currency)
      setNotes(trade.notes ?? '')
      setFieldErrors({})
      setServerErrors(null)
    }
  }, [trade])

  if (!trade) return null

  function clearFieldError(field: keyof FieldErrors) {
    if (fieldErrors[field]) {
      setFieldErrors((prev) => { const next = { ...prev }; delete next[field]; return next })
    }
  }

  function validateField(field: keyof UpdateTradeInput, value: unknown) {
    const result = updateTradeSchema.shape[field].safeParse(value)
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

    const result = updateTradeSchema.safeParse(raw)
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
      { id: trade.id, ...result.data },
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
      <div className="w-full max-w-md rounded-lg bg-white shadow-xl">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <h2 className="text-lg font-semibold">{t('common.edit')} {t('trades.title')}</h2>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-xl leading-none"
            aria-label={t('common.cancel')}
          >
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 p-4">
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
                placeholder={t('trades.symbol')}
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
            <textarea
              placeholder={t('trades.notes')}
              value={notes}
              onChange={(e) => { setNotes(e.target.value); clearFieldError('notes') }}
              onBlur={() => validateField('notes', notes || null)}
              rows={4}
              className={cn(
                'w-full rounded-md border px-3 py-2 text-sm resize-y',
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

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-md border py-2 text-sm text-gray-600 hover:bg-gray-50"
            >
              {t('common.cancel')}
            </button>
            <button
              type="submit"
              disabled={isPending}
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
