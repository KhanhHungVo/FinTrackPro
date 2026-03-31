import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { useTrades, useDeleteTrade } from '@/entities/trade'
import type { Trade } from '@/entities/trade'
import { AddTradeForm } from '@/features/add-trade'
import { EditTradeModal } from '@/features/edit-trade'
import { ClosePositionModal } from '@/features/close-position'
import { useLocaleStore } from '@/features/locale'
import { useExchangeRates } from '@/entities/exchange-rate'
import { convertAmount } from '@/shared/lib/convertAmount'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { cn } from '@/shared/lib/cn'
import { errorToastMessage } from '@/shared/lib/apiError'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'

export function TradesPage() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const { data: trades, isLoading } = useTrades()
  const { mutate: deleteTrade } = useDeleteTrade()
  const { guarded: guardedDelete, isPending: isDeleting } = useGuardedMutation(deleteTrade)
  const [showForm, setShowForm] = useState(false)
  const [editingTrade, setEditingTrade] = useState<Trade | null>(null)
  const [closingTrade, setClosingTrade] = useState<Trade | null>(null)
  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 0

  const closedTrades = trades?.filter((t) => t.status === 'Closed') ?? []
  const openTradesWithPrice = trades?.filter(
    (t) => t.status === 'Open' && t.currentPrice != null,
  ) ?? []

  const totalPnl = closedTrades.reduce(
    (s, t) => s + convertAmount(t.result, t.rateToUsd, preferredRate), 0,
  )
  const wins = closedTrades.filter((t) => t.result > 0).length
  const totalClosed = closedTrades.length
  const winRate = totalClosed > 0 ? Math.round((wins / totalClosed) * 100) : 0
  const totalAllTrades = trades?.length ?? 0

  const unrealizedPnl = openTradesWithPrice.reduce((s, t) => {
    const raw = t.unrealizedResult ?? 0
    return s + convertAmount(raw, t.rateToUsd, preferredRate)
  }, 0)

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-4 md:p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t('trades.title')}</h1>
        <button
          onClick={() => setShowForm((v) => !v)}
          className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white"
        >
          {showForm ? t('common.cancel') : `+ ${t('trades.addTrade')}`}
        </button>
      </div>

      {/* Summary cards */}
      <div className={cn('grid grid-cols-1 gap-4', openTradesWithPrice.length > 0 ? 'sm:grid-cols-4' : 'sm:grid-cols-3')}>
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">{t('trades.totalPnl')}</p>
          <p className={cn('text-xl font-semibold', totalPnl >= 0 ? 'text-green-600' : 'text-red-600')}>
            {totalPnl >= 0 ? '+' : ''}{formatCurrency(totalPnl, currency, i18n.language)}
          </p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">{t('trades.winRate')}</p>
          <p className="text-xl font-semibold">{winRate}%</p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-xs text-gray-500">{t('trades.totalTrades')}</p>
          <p className="text-xl font-semibold">{totalAllTrades}</p>
        </div>
        {openTradesWithPrice.length > 0 && (
          <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
            <p className="text-xs text-emerald-600">{t('trades.unrealizedPnl')}</p>
            <p className={cn('text-xl font-semibold', unrealizedPnl >= 0 ? 'text-emerald-700' : 'text-red-600')}>
              {unrealizedPnl >= 0 ? '+' : ''}{formatCurrency(unrealizedPnl, currency, i18n.language)}
            </p>
          </div>
        )}
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
          {t('trades.noTrades')}
        </p>
      ) : (
        <div className="overflow-x-auto rounded-lg border -mx-4 sm:mx-0">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-xs text-gray-500 uppercase">
              <tr>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-left">{t('trades.symbol')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-left">{t('trades.direction')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-left">{t('trades.status')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">{t('trades.entryPrice')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">{t('trades.exitCurrentPrice')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">{t('trades.positionSize')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">{t('trades.fees')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-right">{t('trades.pnl')}</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3 text-left">Date</th>
                <th className="px-3 py-2 sm:px-4 sm:py-3" />
              </tr>
            </thead>
            <tbody className="divide-y">
              {trades?.map((trade) => {
                const isOpen = trade.status === 'Open'
                const displayPrice = isOpen ? trade.currentPrice : trade.exitPrice
                const pnlRaw = isOpen ? (trade.unrealizedResult ?? null) : trade.result
                const displayPnl = pnlRaw != null
                  ? convertAmount(pnlRaw, trade.rateToUsd, preferredRate)
                  : null
                const displayFees = convertAmount(trade.fees, trade.rateToUsd, preferredRate)

                return (
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
                        {trade.direction === 'Long' ? t('trades.long') : t('trades.short')}
                      </span>
                    </td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3">
                      <span
                        className={cn(
                          'rounded px-1.5 py-0.5 text-xs font-medium',
                          isOpen
                            ? 'bg-emerald-100 text-emerald-700'
                            : 'bg-gray-100 text-gray-600',
                        )}
                      >
                        {isOpen ? t('trades.open') : t('trades.closed')}
                      </span>
                    </td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-600">
                      {formatCurrency(convertAmount(trade.entryPrice, trade.rateToUsd, preferredRate), currency, i18n.language)}
                    </td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-600">
                      {displayPrice != null
                        ? formatCurrency(convertAmount(displayPrice, trade.rateToUsd, preferredRate), currency, i18n.language)
                        : '—'}
                    </td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-600">{trade.positionSize}</td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3 text-right text-gray-400">
                      {formatCurrency(displayFees, currency, i18n.language)}
                    </td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3 text-right">
                      {displayPnl != null ? (
                        <span
                          className={cn(
                            isOpen
                              ? 'italic text-gray-400'
                              : cn('font-semibold', displayPnl >= 0 ? 'text-green-600' : 'text-red-600'),
                          )}
                        >
                          {displayPnl >= 0 ? '+' : ''}{formatCurrency(displayPnl, currency, i18n.language)}
                        </span>
                      ) : (
                        <span className="text-gray-300">—</span>
                      )}
                    </td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3 text-xs text-gray-400">
                      {new Date(trade.createdAt).toLocaleDateString(i18n.language)}
                    </td>
                    <td className="px-3 py-2 sm:px-4 sm:py-3">
                      <div className="flex gap-2">
                        {isOpen && (
                          <button
                            onClick={() => setClosingTrade(trade)}
                            className="text-xs text-gray-300 hover:text-emerald-600"
                            title={t('trades.closeTrade')}
                          >
                            ✓
                          </button>
                        )}
                        <button
                          onClick={() => setEditingTrade(trade)}
                          className="text-xs text-gray-300 hover:text-blue-500"
                          title={t('common.edit')}
                        >
                          ✎
                        </button>
                        <button
                          onClick={() => guardedDelete(trade.id, { onError: (err) => toast.error(errorToastMessage(err)) })}
                          disabled={isDeleting(trade.id)}
                          className="text-xs text-gray-300 hover:text-red-500 disabled:opacity-50"
                          title={t('common.delete')}
                        >
                          ✕
                        </button>
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      <EditTradeModal trade={editingTrade} onClose={() => setEditingTrade(null)} />
      <ClosePositionModal trade={closingTrade} onClose={() => setClosingTrade(null)} />
    </div>
  )
}
