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
          className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 transition-colors"
        >
          {showForm ? t('common.cancel') : `+ ${t('trades.addTrade')}`}
        </button>
      </div>

      {/* Summary cards */}
      <div className={cn('grid grid-cols-1 gap-4', openTradesWithPrice.length > 0 ? 'sm:grid-cols-4' : 'sm:grid-cols-3')}>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('trades.totalPnl')}</p>
          <p className={cn('text-xl font-semibold', totalPnl >= 0 ? 'text-green-600' : 'text-red-600')}>
            {totalPnl >= 0 ? '+' : ''}{formatCurrency(totalPnl, currency, i18n.language)}
          </p>
        </div>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('trades.winRate')}</p>
          <p className="text-xl font-semibold">{winRate}%</p>
        </div>
        <div className="page-card p-4">
          <p className="text-xs text-gray-500 dark:text-slate-400">{t('trades.totalTrades')}</p>
          <p className="text-xl font-semibold">{totalAllTrades}</p>
        </div>
        {openTradesWithPrice.length > 0 && (
          <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 dark:border-emerald-500/20 dark:bg-emerald-500/10">
            <p className="text-xs text-emerald-600 dark:text-emerald-400">{t('trades.unrealizedPnl')}</p>
            <p className={cn('text-xl font-semibold', unrealizedPnl >= 0 ? 'text-emerald-700 dark:text-emerald-400' : 'text-red-600')}>
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
            <div key={i} className="animate-pulse h-16 rounded-lg bg-gray-100 dark:bg-white/5" />
          ))}
        </div>
      ) : trades?.length === 0 ? (
        <p className="text-center text-sm text-gray-400 dark:text-slate-500 py-8">
          {t('trades.noTrades')}
        </p>
      ) : (
        <div className="page-card overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b border-gray-100 dark:border-white/6 bg-gray-50/60 dark:bg-white/3">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.symbol')}</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.direction')}</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.status')}</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.pnl')}</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.entryPrice')}</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.exitCurrentPrice')}</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.positionSize')}</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.fees')}</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-400 dark:text-slate-500 uppercase tracking-wide">{t('trades.date')}</th>
                  <th className="px-4 py-3 w-20" />
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-white/5">
                {trades?.map((trade) => {
                  const isOpen = trade.status === 'Open'
                  const displayPrice = isOpen ? trade.currentPrice : trade.exitPrice
                  const pnlRaw = isOpen ? (trade.unrealizedResult ?? null) : trade.result
                  const displayPnl = pnlRaw != null
                    ? convertAmount(pnlRaw, trade.rateToUsd, preferredRate)
                    : null
                  const displayFees = convertAmount(trade.fees, trade.rateToUsd, preferredRate)

                  return (
                    <tr key={trade.id} className="group hover:bg-gray-50/70 dark:hover:bg-white/3 transition-colors">
                      {/* Symbol */}
                      <td className="px-4 py-3">
                        <span className="font-mono font-semibold text-gray-900 dark:text-slate-100">{trade.symbol}</span>
                      </td>

                      {/* Direction */}
                      <td className="px-4 py-3">
                        <span
                          className={cn(
                            'inline-flex w-fit rounded px-1.5 py-0.5 text-xs font-semibold',
                            trade.direction === 'Long'
                              ? 'bg-green-100 text-green-700 dark:bg-green-500/15 dark:text-green-400'
                              : 'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400',
                          )}
                        >
                          {trade.direction === 'Long' ? t('trades.long') : t('trades.short')}
                        </span>
                      </td>

                      {/* Status */}
                      <td className="px-4 py-3">
                        <span
                          className={cn(
                            'inline-flex w-fit rounded px-1.5 py-0.5 text-xs font-medium',
                            isOpen
                              ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400'
                              : 'bg-gray-100 text-gray-500 dark:bg-white/5 dark:text-slate-500',
                          )}
                        >
                          {isOpen ? t('trades.open') : t('trades.closed')}
                        </span>
                      </td>

                      {/* P&L — most important, shown prominently */}
                      <td className="px-4 py-3 text-right">
                        {displayPnl != null ? (
                          <span
                            className={cn(
                              'text-sm font-bold',
                              isOpen
                                ? 'text-gray-400 dark:text-slate-500'
                                : displayPnl >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400',
                            )}
                          >
                            {displayPnl >= 0 ? '+' : ''}{formatCurrency(displayPnl, currency, i18n.language)}
                            {isOpen && <span className="ml-1 text-xs font-normal italic">(unrlzd)</span>}
                          </span>
                        ) : (
                          <span className="text-gray-300 dark:text-slate-600">—</span>
                        )}
                      </td>

                      {/* Entry price */}
                      <td className="px-4 py-3 text-right text-gray-600 dark:text-slate-300 tabular-nums">
                        {formatCurrency(convertAmount(trade.entryPrice, trade.rateToUsd, preferredRate), currency, i18n.language)}
                      </td>

                      {/* Exit / current price */}
                      <td className="px-4 py-3 text-right tabular-nums">
                        {displayPrice != null ? (
                          <span className="text-gray-600 dark:text-slate-300">
                            {formatCurrency(convertAmount(displayPrice, trade.rateToUsd, preferredRate), currency, i18n.language)}
                          </span>
                        ) : (
                          <span className="text-gray-300 dark:text-slate-600">—</span>
                        )}
                      </td>

                      {/* Position size */}
                      <td className="px-4 py-3 text-right text-gray-500 dark:text-slate-400 tabular-nums">
                        {trade.positionSize}
                      </td>

                      {/* Fees — dimmed, secondary info */}
                      <td className="px-4 py-3 text-right text-gray-400 dark:text-slate-500 tabular-nums text-xs">
                        {formatCurrency(displayFees, currency, i18n.language)}
                      </td>

                      {/* Date */}
                      <td className="px-4 py-3 text-xs text-gray-400 dark:text-slate-500 whitespace-nowrap">
                        {new Date(trade.createdAt).toLocaleDateString(i18n.language, { month: 'short', day: 'numeric' })}
                      </td>

                      {/* Actions — visible on hover */}
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          {isOpen && (
                            <button
                              onClick={() => setClosingTrade(trade)}
                              title={t('trades.closeTrade')}
                              className="rounded px-1.5 py-1 text-xs font-medium text-emerald-600 hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-500/10 transition-colors"
                            >
                              ✓
                            </button>
                          )}
                          <button
                            onClick={() => setEditingTrade(trade)}
                            title={t('common.edit')}
                            className="rounded px-1.5 py-1 text-xs text-gray-400 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-500/10 transition-colors"
                          >
                            ✎
                          </button>
                          <button
                            onClick={() => guardedDelete(trade.id, { onError: (err) => toast.error(errorToastMessage(err)) })}
                            disabled={isDeleting(trade.id)}
                            title={t('common.delete')}
                            className="rounded px-1.5 py-1 text-xs text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 disabled:opacity-50 transition-colors"
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
        </div>
      )}

      <EditTradeModal trade={editingTrade} onClose={() => setEditingTrade(null)} />
      <ClosePositionModal trade={closingTrade} onClose={() => setClosingTrade(null)} />
    </div>
  )
}
