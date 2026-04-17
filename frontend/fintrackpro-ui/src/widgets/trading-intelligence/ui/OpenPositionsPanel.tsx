import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useOpenPositions } from '../lib/useOpenPositions'
import { AllocationDonut } from './AllocationDonut'
import { RiskSignals } from './RiskSignals'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { useLocaleStore } from '@/features/locale'
import { cn } from '@/shared/lib/cn'

export function OpenPositionsPanel() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const { positions, winning, losing, totalCapital, totalUnrealized, riskSignals, allocationData, isLoading } = useOpenPositions()

  if (isLoading) {
    return <div className="animate-pulse h-40 rounded-lg bg-gray-100 dark:bg-white/5" />
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-slate-400">
          {t('dashboard.openPositions', { count: positions.length })} · {t('dashboard.liveSnapshot')}
        </p>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 gap-3">
        <div className="rounded-lg bg-gray-50 dark:bg-white/5 p-3">
          <p className="text-xs text-purple-600 dark:text-purple-400 font-semibold uppercase tracking-wide">
            {t('dashboard.unrealizedPnl')}
          </p>
          <p className={cn('text-xl font-bold mt-0.5', totalUnrealized >= 0 ? 'text-green-600' : 'text-red-600')}>
            {totalUnrealized >= 0 ? '+' : ''}{formatCurrency(totalUnrealized, currency, i18n.language)}
          </p>
        </div>
        <div className="rounded-lg bg-gray-50 dark:bg-white/5 p-3">
          <p className="text-xs text-gray-500 dark:text-slate-400 font-semibold uppercase tracking-wide">
            {t('dashboard.openPositionsCount')}
          </p>
          <p className="text-xl font-bold mt-0.5 text-gray-800 dark:text-slate-100">
            {positions.length}
          </p>
        </div>
      </div>

      {positions.length > 0 && (
        <>
          {/* Capital allocation + P&L list */}
          <div className="flex flex-col sm:flex-row gap-4 items-start">
            {allocationData.length > 0 && (
              <AllocationDonut
                data={allocationData}
                totalCapital={totalCapital}
                currency={currency}
                locale={i18n.language}
                centerLabel={t('dashboard.invested')}
              />
            )}

            <div className="flex-1 space-y-3 w-full">
              {winning.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-green-600 dark:text-green-400 mb-1.5">
                    {t('dashboard.winning')} ({winning.length})
                  </p>
                  <ul className="space-y-1">
                    {winning.slice(0, 3).map((p) => (
                      <li key={p.id} className="flex items-center justify-between text-sm">
                        <span className="font-mono text-xs text-gray-600 dark:text-slate-300">{p.symbol}</span>
                        <span className="text-green-600 tabular-nums text-xs">
                          +{formatCurrency(p.unrealizedPnl, currency, i18n.language)}
                          {' '}
                          <span className="text-gray-400">+{p.unrealizedPct.toFixed(1)}%</span>
                        </span>
                        <span className="text-xs text-gray-400 dark:text-slate-500 tabular-nums w-14 text-right">
                          {p.portfolioWeight.toFixed(0)}% of port.
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              {losing.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-red-600 dark:text-red-400 mb-1.5">
                    {t('dashboard.losing')} ({losing.length})
                  </p>
                  <ul className="space-y-1">
                    {losing.slice(0, 3).map((p) => (
                      <li key={p.id} className="flex items-center justify-between text-sm">
                        <span className="font-mono text-xs text-gray-600 dark:text-slate-300">{p.symbol}</span>
                        <span className="text-red-600 tabular-nums text-xs">
                          {formatCurrency(p.unrealizedPnl, currency, i18n.language)}
                          {' '}
                          <span className="text-gray-400">{p.unrealizedPct.toFixed(1)}%</span>
                        </span>
                        <span className="text-xs text-gray-400 dark:text-slate-500 tabular-nums w-14 text-right">
                          {p.portfolioWeight.toFixed(0)}% of port.
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              {positions.length > 6 && (
                <Link to="/trades" className="text-xs text-blue-500 hover:underline">
                  {t('dashboard.viewAll')} → /trades
                </Link>
              )}
            </div>
          </div>

          <RiskSignals signals={riskSignals} />
        </>
      )}
    </div>
  )
}
