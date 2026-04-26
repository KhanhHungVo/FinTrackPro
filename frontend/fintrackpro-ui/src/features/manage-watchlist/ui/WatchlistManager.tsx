import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  useWatchedSymbols,
  useAddWatchedSymbol,
  useRemoveWatchedSymbol,
} from '@/entities/watched-symbol'
import { classifyApiError, errorToastMessage } from '@/shared/lib/apiError'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'
import { ProFeatureLock } from '@/features/upgrade'
import { watchlistSymbolSchema } from '@/shared/lib/tradeSchema'
import { cn } from '@/shared/lib/cn'

function WatchlistForm() {
  const { data: symbols, isLoading } = useWatchedSymbols()
  const { mutate: add, isPending: adding } = useAddWatchedSymbol()
  const { mutate: remove } = useRemoveWatchedSymbol()
  const { guarded: guardedRemove, isPending: isRemoving } = useGuardedMutation(remove)
  const [symbol, setSymbol] = useState('')
  const [symbolError, setSymbolError] = useState<string | null>(null)

  function validateSymbol(value: string): boolean {
    const result = watchlistSymbolSchema.safeParse({ symbol: value })
    if (!result.success) {
      setSymbolError(result.error.issues[0].message)
      return false
    }
    setSymbolError(null)
    return true
  }

  const handleAdd = (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault()
    if (!validateSymbol(symbol)) return

    add(symbol, {
      onSuccess: () => { setSymbol(''); setSymbolError(null) },
      onError: (err) => {
        const kind = classifyApiError(err)
        if (kind.type === 'validation') {
          const fieldMsg = kind.details.errors?.['Symbol']?.[0]
          setSymbolError(fieldMsg ?? kind.details.title ?? 'Invalid symbol')
        } else if (kind.type === 'conflict') {
          setSymbolError(kind.message)
        } else {
          toast.error(errorToastMessage(err))
        }
      },
    })
  }

  return (
    <div className="space-y-4">
      <form onSubmit={handleAdd} className="flex gap-2 items-start">
        <div className="flex flex-col gap-1 flex-1">
          <input
            type="text"
            placeholder="e.g. BTCUSDT"
            value={symbol}
            onChange={(e) => {
              setSymbol(e.target.value.toUpperCase())
              if (symbolError) setSymbolError(null)
            }}
            onBlur={() => { if (symbol) validateSymbol(symbol) }}
            className={cn(
              'w-full rounded-md border px-3 py-2 text-sm font-mono outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:text-white',
              symbolError
                ? 'border-red-400 dark:border-red-500'
                : 'border-gray-300 dark:border-white/12',
            )}
          />
          {symbolError && (
            <p className="text-xs text-red-600 dark:text-red-400">{symbolError}</p>
          )}
        </div>
        <button
          type="submit"
          disabled={adding}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white disabled:opacity-50"
        >
          {adding ? '...' : 'Add'}
        </button>
      </form>

      {isLoading ? (
        <div className="animate-pulse h-16 rounded bg-gray-100 dark:bg-white/5" />
      ) : symbols?.length === 0 ? (
        <p className="text-sm text-gray-400 dark:text-slate-500">No symbols watched yet.</p>
      ) : (
        <ul className="space-y-1">
          {symbols?.map((ws) => (
            <li
              key={ws.id}
              className="flex items-center justify-between rounded-md border px-3 py-2 dark:border-white/6"
            >
              <span className="font-mono text-sm font-medium">{ws.symbol}</span>
              <button
                onClick={() => guardedRemove(ws.id, { onError: (err) => toast.error(errorToastMessage(err)) })}
                disabled={isRemoving(ws.id)}
                className="min-h-[44px] min-w-[44px] rounded px-2 text-xs text-red-500 hover:text-red-700 disabled:opacity-50"
              >
                Remove
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

export function WatchlistManager() {
  const { t } = useTranslation()
  return (
    <ProFeatureLock title={t('proLock.watchlist')}>
      <div className="page-card p-4 md:p-6 w-full">
        <WatchlistForm />
      </div>
    </ProFeatureLock>
  )
}
