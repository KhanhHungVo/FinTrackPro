import { useState } from 'react'
import { useTrades, useDeleteTrade } from '@/entities/trade'
import { AddTradeForm } from '@/features/add-trade'
import { cn } from '@/shared/lib/cn'

export function TradesPage() {
  const { data: trades, isLoading } = useTrades()
  const { mutate: deleteTrade } = useDeleteTrade()
  const [showForm, setShowForm] = useState(false)

  const totalPnl   = trades?.reduce((s, t) => s + t.result, 0) ?? 0
  const wins       = trades?.filter((t) => t.result > 0).length ?? 0
  const total      = trades?.length ?? 0
  const winRate    = total > 0 ? Math.round((wins / total) * 100) : 0

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-4 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Trading Journal</h1>
        <button
          onClick={() => setShowForm((v) => !v)}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white"
        >
          {showForm ? 'Cancel' : '+ Log Trade'}
        </button>
      </div>

      {/* Summary stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">Total P&amp;L</p>
          <p className={cn('text-xl font-semibold', totalPnl >= 0 ? 'text-green-600' : 'text-red-600')}>
            {totalPnl >= 0 ? '+' : ''}${totalPnl.toFixed(2)}
          </p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">Win Rate</p>
          <p className="text-xl font-semibold">{winRate}%</p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">Total Trades</p>
          <p className="text-xl font-semibold">{total}</p>
        </div>
      </div>

      {showForm && (
        <div>
          <AddTradeForm />
        </div>
      )}

      {/* Trade list */}
      {isLoading ? (
        <div className="space-y-2">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="animate-pulse h-16 rounded-lg bg-gray-100" />
          ))}
        </div>
      ) : trades?.length === 0 ? (
        <p className="text-center text-sm text-gray-400 py-8">
          No trades logged yet. Click &ldquo;Log Trade&rdquo; to get started.
        </p>
      ) : (
        <div className="overflow-x-auto rounded-lg border -mx-4 sm:mx-0">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-xs text-gray-500 uppercase">
              <tr>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-left">Symbol</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-left">Direction</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">Entry</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">Exit</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">Size</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">Fees</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">P&amp;L</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-left">Date</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3" />
              </tr>
            </thead>
            <tbody className="divide-y">
              {trades?.map((trade) => (
                <tr key={trade.id} className="hover:bg-gray-50">
                  <td className="px-3 py-2 sm:px-4 sm:py-3 font-mono font-medium">{trade.symbol}</td>
                  <td className="px-3 py-2 sm:px-4 sm:py-3">
                    <span
                      className={cn(
                        'rounded px-1.5 py-0.5 text-xs font-medium',
                        trade.direction === 'Long'
                          ? 'bg-green-100 text-green-700'
                          : 'bg-red-100 text-red-700',
                      )}
                    >
                      {trade.direction}
                    </span>
                  </td>
                  <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-600">${trade.entryPrice.toFixed(2)}</td>
                  <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-600">${trade.exitPrice.toFixed(2)}</td>
                  <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-600">{trade.positionSize}</td>
                  <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-400">${trade.fees.toFixed(2)}</td>
                  <td
                    className={cn(
                      'px-3 py-2 sm:px-4 sm:py-3 text-right font-semibold',
                      trade.result >= 0 ? 'text-green-600' : 'text-red-600',
                    )}
                  >
                    {trade.result >= 0 ? '+' : ''}${trade.result.toFixed(2)}
                  </td>
                  <td className="px-3 py-2 sm:px-4 sm:py-3 text-xs text-gray-400">
                    {new Date(trade.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-3 py-2 sm:px-4 sm:py-3">
                    <button
                      onClick={() => deleteTrade(trade.id)}
                      className="text-xs text-gray-300 hover:text-red-500"
                      title="Delete"
                    >
                      ✕
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
