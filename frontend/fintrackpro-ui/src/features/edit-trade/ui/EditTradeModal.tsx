import { useEffect, useState } from 'react'
import { useUpdateTrade } from '@/entities/trade'
import type { Trade, TradeDirection } from '@/entities/trade'
import { cn } from '@/shared/lib/cn'

interface EditTradeModalProps {
  trade: Trade | null
  onClose: () => void
}

export function EditTradeModal({ trade, onClose }: EditTradeModalProps) {
  const { mutate, isPending, error } = useUpdateTrade()
  const [direction, setDirection] = useState<TradeDirection>('Long')
  const [symbol, setSymbol] = useState('')
  const [entryPrice, setEntryPrice] = useState('')
  const [exitPrice, setExitPrice] = useState('')
  const [positionSize, setPositionSize] = useState('')
  const [fees, setFees] = useState('')
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (trade) {
      setDirection(trade.direction)
      setSymbol(trade.symbol)
      setEntryPrice(String(trade.entryPrice))
      setExitPrice(String(trade.exitPrice))
      setPositionSize(String(trade.positionSize))
      setFees(String(trade.fees))
      setNotes(trade.notes ?? '')
    }
  }, [trade])

  if (!trade) return null

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate(
      {
        id: trade.id,
        symbol,
        direction,
        entryPrice: parseFloat(entryPrice),
        exitPrice: parseFloat(exitPrice),
        positionSize: parseFloat(positionSize),
        fees: parseFloat(fees) || 0,
        notes: notes || null,
      },
      { onSuccess: onClose },
    )
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={(e) => { if (e.target === e.currentTarget) onClose() }}
    >
      <div className="w-full max-w-md rounded-lg bg-white shadow-xl">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <h2 className="text-lg font-semibold">Edit Trade</h2>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-xl leading-none"
            aria-label="Close"
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
                {d}
              </button>
            ))}
          </div>

          <input
            type="text"
            placeholder="Symbol (e.g. BTCUSDT)"
            value={symbol}
            onChange={(e) => setSymbol(e.target.value.toUpperCase())}
            required
            className="w-full rounded-md border px-3 py-2 text-sm font-mono"
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            <input
              type="number"
              placeholder="Entry price"
              value={entryPrice}
              onChange={(e) => setEntryPrice(e.target.value)}
              required
              className="rounded-md border px-3 py-2 text-sm"
            />
            <input
              type="number"
              placeholder="Exit price"
              value={exitPrice}
              onChange={(e) => setExitPrice(e.target.value)}
              required
              className="rounded-md border px-3 py-2 text-sm"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            <input
              type="number"
              placeholder="Position size"
              value={positionSize}
              onChange={(e) => setPositionSize(e.target.value)}
              required
              className="rounded-md border px-3 py-2 text-sm"
            />
            <input
              type="number"
              placeholder="Fees"
              value={fees}
              onChange={(e) => setFees(e.target.value)}
              className="rounded-md border px-3 py-2 text-sm"
            />
          </div>

          <textarea
            placeholder="Notes (optional)"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={4}
            className="w-full rounded-md border px-3 py-2 text-sm resize-y"
          />

          {error && (
            <p className="text-sm text-red-600">{(error as Error).message}</p>
          )}

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-md border py-2 text-sm text-gray-600 hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isPending}
              className="flex-1 rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
            >
              {isPending ? 'Saving...' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
