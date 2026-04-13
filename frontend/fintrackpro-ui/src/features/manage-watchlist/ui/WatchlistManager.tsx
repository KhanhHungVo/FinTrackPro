import { useState } from 'react'
import { toast } from 'sonner'
import {
  useWatchedSymbols,
  useAddWatchedSymbol,
  useRemoveWatchedSymbol,
} from '@/entities/watched-symbol'
import { errorToastMessage } from '@/shared/lib/apiError'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'

export function WatchlistManager() {
  const { data: symbols, isLoading } = useWatchedSymbols()
  const { mutate: add, isPending: adding } = useAddWatchedSymbol()
  const { mutate: remove } = useRemoveWatchedSymbol()
  const { guarded: guardedRemove, isPending: isRemoving } = useGuardedMutation(remove)
  const [symbol, setSymbol] = useState('')

  const handleAdd = (e: React.FormEvent) => {
    e.preventDefault()
    add(symbol, {
      onSuccess: () => setSymbol(''),
      onError: (err) => toast.error(errorToastMessage(err)),
    })
  }

  return (
    <div className="page-card p-4 md:p-6 w-full space-y-4">

      <form onSubmit={handleAdd} className="flex gap-2">
        <input
          type="text"
          placeholder="e.g. BTCUSDT"
          value={symbol}
          onChange={(e) => setSymbol(e.target.value.toUpperCase())}
          required
          className="flex-1 rounded-md border border-gray-300 px-3 py-2 text-sm font-mono outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
        />
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
                className="text-xs text-red-500 hover:text-red-700 disabled:opacity-50"
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
