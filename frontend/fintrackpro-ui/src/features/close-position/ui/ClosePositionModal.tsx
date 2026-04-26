import { useState } from 'react'
import { X } from 'lucide-react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useClosePosition } from '@/entities/trade'
import type { Trade } from '@/entities/trade'
import { cn } from '@/shared/lib/cn'
import { closePositionSchema, type ClosePositionInput } from '@/shared/lib/tradeSchema'
import { classifyApiError, errorToastMessage, type ProblemDetails } from '@/shared/lib/apiError'

interface ClosePositionModalProps {
  trade: Trade | null
  onClose: () => void
}

type FieldErrors = Partial<Record<keyof ClosePositionInput, string>>

export function ClosePositionModal({ trade, onClose }: ClosePositionModalProps) {
  const { t } = useTranslation()
  const { mutate, isPending } = useClosePosition()
  const [exitPrice, setExitPrice] = useState('')
  const [fees, setFees] = useState('')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [serverErrors, setServerErrors] = useState<ProblemDetails | null>(null)

  if (!trade) return null

  function clearFieldError(field: keyof FieldErrors) {
    if (fieldErrors[field]) {
      setFieldErrors((prev) => { const next = { ...prev }; delete next[field]; return next })
    }
  }

  const handleClose = () => {
    setExitPrice('')
    setFees('')
    setFieldErrors({})
    setServerErrors(null)
    onClose()
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setServerErrors(null)

    const raw = {
      exitPrice: parseFloat(exitPrice) || 0,
      fees: parseFloat(fees) || 0,
    }

    const result = closePositionSchema.safeParse(raw)
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
        onSuccess: handleClose,
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
      onClick={(e) => { if (e.target === e.currentTarget) handleClose() }}
    >
      <div className="w-full max-w-sm rounded-lg bg-white shadow-xl dark:bg-[#161a25]">
        <div className="flex items-center justify-between border-b px-4 py-3 dark:border-white/6">
          <h2 className="text-lg font-semibold">{t('trades.closePosition')}</h2>
          <button
            type="button"
            onClick={handleClose}
            className="text-gray-400 hover:text-gray-600 text-xl leading-none dark:text-slate-500 dark:hover:text-slate-300"
            aria-label={t('common.cancel')}
          >
            <X size={16} aria-hidden="true" />
          </button>
        </div>

        <div className="px-4 pt-4 pb-2 space-y-1 text-sm text-gray-600 bg-gray-50 rounded-none dark:bg-white/4 dark:text-slate-400">
          <div className="flex justify-between">
            <span className="text-gray-400">{t('trades.symbol')}</span>
            <span className="font-mono font-medium">{trade.symbol}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-400">{t('trades.entryPrice')}</span>
            <span>{trade.entryPrice}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-400">{t('trades.positionSize')}</span>
            <span>{trade.positionSize}</span>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 p-4">
          <div className="flex flex-col gap-1">
            <input
              type="number"
              placeholder={t('trades.exitPrice')}
              value={exitPrice}
              onChange={(e) => { setExitPrice(e.target.value); clearFieldError('exitPrice') }}
              className={cn(
                'rounded-md border px-3 py-2 text-sm dark:bg-slate-800 dark:border-white/10 dark:text-white',
                fieldErrors.exitPrice && 'border-red-400',
              )}
              autoFocus
            />
            {fieldErrors.exitPrice && (
              <p className="text-xs text-red-600">{fieldErrors.exitPrice}</p>
            )}
          </div>

          <div className="flex flex-col gap-1">
            <input
              type="number"
              placeholder={`${t('trades.fees')} (${t('common.add').toLowerCase()}…)`}
              value={fees}
              onChange={(e) => { setFees(e.target.value); clearFieldError('fees') }}
              className={cn(
                'rounded-md border px-3 py-2 text-sm dark:bg-slate-800 dark:border-white/10 dark:text-white',
                fieldErrors.fees && 'border-red-400',
              )}
            />
            {fieldErrors.fees && <p className="text-xs text-red-600">{fieldErrors.fees}</p>}
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
              onClick={handleClose}
              className="flex-1 rounded-md border py-2 text-sm text-gray-600 hover:bg-gray-50 dark:border-white/10 dark:text-slate-400 dark:hover:bg-white/5"
            >
              {t('common.cancel')}
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="flex-1 rounded-md bg-emerald-600 py-2 text-sm text-white disabled:opacity-50"
            >
              {isPending ? t('common.loading') : t('trades.closeTrade')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
