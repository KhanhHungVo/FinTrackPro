import { useState } from 'react'
import { useCreateTrade } from '@/entities/trade'
import { cn } from '@/shared/lib/cn'

export function AddTradeForm() {
  const { mutate, isPending, error } = useCreateTrade()
  const [direction, setDirection] = useState<'Long' | 'Short'>('Long')
  const [symbol, setSymbol] = useState('')
  const [entryPrice, setEntryPrice] = useState('')
  const [exitPrice, setExitPrice] = useState('')
  const [positionSize, setPositionSize] = useState('')
  const [fees, setFees] = useState('')
  const [notes, setNotes] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate({
      symbol,
      direction,
      entryPrice: parseFloat(entryPrice),
      exitPrice: parseFloat(exitPrice),
      positionSize: parseFloat(positionSize),
      fees: parseFloat(fees) || 0,
      notes: notes || null,
    })
    setSymbol('')
    setEntryPrice('')
    setExitPrice('')
    setPositionSize('')
    setFees('')
    setNotes('')
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border p-4">
      <h2 className="text-lg font-semibold">Log Trade</h2>

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
      <input
        type="text"
        placeholder="Notes (optional)"
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
        className="w-full rounded-md border px-3 py-2 text-sm"
      />

      {error && (
        <p className="text-sm text-red-600">
          {(error as Error).message}
        </p>
      )}

      <button
        type="submit"
        disabled={isPending}
        className="w-full rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
      >
        {isPending ? 'Saving...' : 'Log Trade'}
      </button>
    </form>
  )
}
