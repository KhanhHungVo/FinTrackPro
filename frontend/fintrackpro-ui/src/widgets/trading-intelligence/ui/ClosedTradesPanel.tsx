import { useTranslation } from 'react-i18next'
import {
  LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, ReferenceLine,
} from 'recharts'
import { useClosedTradesSummary } from '../lib/useClosedTradesSummary'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { useLocaleStore } from '@/features/locale'
import { cn } from '@/shared/lib/cn'

export function ClosedTradesPanel() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const {
    totalTrades,
    realisedPnl,
    winRate,
    winRateFraction,
    avgPnl,
    avgWin,
    avgLoss,
    riskReward,
    weekBuckets,
    symbolBreakdown,
    longPnl,
    shortPnl,
    isLoading,
  } = useClosedTradesSummary()

  if (isLoading) {
    return <div className="animate-pulse h-40 rounded-lg bg-gray-100 dark:bg-white/5" />
  }

  if (totalTrades === 0) {
    return (
      <p className="text-sm text-gray-400 dark:text-slate-500 text-center py-4">
        {t('dashboard.noClosedTradesThisMonth')}
      </p>
    )
  }

  const maxSymbolAbs = Math.max(...symbolBreakdown.map((s) => Math.abs(s.pnl)), 1)
  const maxDirectionAbs = Math.max(Math.abs(longPnl), Math.abs(shortPnl), 1)

  return (
    <div className="space-y-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-slate-400">
        {t('dashboard.closedTrades')} · {t('dashboard.thisMonth')}
      </p>

      {/* Metrics cards */}
      <div className="grid grid-cols-3 gap-3">
        <div className="rounded-lg bg-gray-50 dark:bg-white/5 p-3">
          <p className="text-xs text-blue-600 dark:text-blue-400 font-semibold uppercase tracking-wide">
            {t('dashboard.realisedPnl')}
          </p>
          <p className={cn('text-lg font-bold mt-0.5', realisedPnl >= 0 ? 'text-green-600' : 'text-red-600')}>
            {realisedPnl >= 0 ? '+' : ''}{formatCurrency(realisedPnl, currency, i18n.language)}
          </p>
        </div>
        <div className="rounded-lg bg-gray-50 dark:bg-white/5 p-3">
          <p className="text-xs text-gray-500 dark:text-slate-400 font-semibold uppercase tracking-wide">
            {t('dashboard.winRate')}
          </p>
          <p className="text-lg font-bold mt-0.5 text-gray-800 dark:text-slate-100">
            {winRate.toFixed(0)}%
          </p>
          <p className="text-xs text-gray-400 dark:text-slate-500">{winRateFraction}</p>
        </div>
        <div className="rounded-lg bg-gray-50 dark:bg-white/5 p-3">
          <p className="text-xs text-gray-500 dark:text-slate-400 font-semibold uppercase tracking-wide">
            {t('dashboard.avgPnlPerTrade')}
          </p>
          <p className={cn('text-lg font-bold mt-0.5', avgPnl >= 0 ? 'text-green-600' : 'text-red-600')}>
            {avgPnl >= 0 ? '+' : ''}{formatCurrency(avgPnl, currency, i18n.language)}
          </p>
        </div>
      </div>

      {/* Cumulative P&L line chart */}
      <div>
        <p className="text-xs text-gray-400 dark:text-slate-500 mb-2">{t('dashboard.cumulativePnl')} · {t('dashboard.thisMonth')}</p>
        <ResponsiveContainer width="100%" height={100}>
          <LineChart data={weekBuckets} margin={{ top: 4, right: 4, left: 0, bottom: 0 }}>
            <XAxis dataKey="label" tick={{ fontSize: 10, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
            <YAxis hide />
            <Tooltip
              formatter={(v) => formatCurrency(Number(v), currency, i18n.language)}
              contentStyle={{ fontSize: 11, borderRadius: 6, border: '1px solid #e5e7eb' }}
            />
            <ReferenceLine y={0} stroke="#e5e7eb" strokeDasharray="3 3" />
            <Line
              type="monotone"
              dataKey="cumulativePnl"
              stroke="#3b82f6"
              strokeWidth={2}
              dot={{ r: 3, fill: '#3b82f6' }}
              activeDot={{ r: 4 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* By symbol breakdown */}
      {symbolBreakdown.length > 0 && (
        <div className="space-y-1.5">
          <p className="text-xs text-gray-400 dark:text-slate-500">{t('dashboard.bySymbol')}</p>
          {symbolBreakdown.map((s) => (
            <div key={s.symbol} className="flex items-center gap-2 text-xs">
              <span className="w-16 font-mono text-gray-600 dark:text-slate-300 flex-shrink-0">{s.symbol}</span>
              <div className="flex-1 h-1.5 bg-gray-100 dark:bg-white/10 rounded-full overflow-hidden">
                <div
                  className={cn('h-1.5 rounded-full', s.pnl >= 0 ? 'bg-green-500' : 'bg-red-500')}
                  style={{ width: `${(Math.abs(s.pnl) / maxSymbolAbs) * 100}%` }}
                />
              </div>
              <span className={cn('w-24 text-right tabular-nums', s.pnl >= 0 ? 'text-green-600' : 'text-red-600')}>
                {s.pnl >= 0 ? '+' : ''}{formatCurrency(s.pnl, currency, i18n.language)}
              </span>
            </div>
          ))}
        </div>
      )}

      {/* By direction */}
      <div className="space-y-1.5">
        <p className="text-xs text-gray-400 dark:text-slate-500">{t('dashboard.byDirection')}</p>
        {[
          { label: 'Long', value: longPnl },
          { label: 'Short', value: shortPnl },
        ].map((row) => (
          <div key={row.label} className="flex items-center gap-2 text-xs">
            <span className="w-16 text-gray-600 dark:text-slate-300 flex-shrink-0">{row.label}</span>
            <div className="flex-1 h-1.5 bg-gray-100 dark:bg-white/10 rounded-full overflow-hidden">
              <div
                className={cn('h-1.5 rounded-full', row.value >= 0 ? 'bg-blue-500' : 'bg-red-500')}
                style={{ width: `${(Math.abs(row.value) / maxDirectionAbs) * 100}%` }}
              />
            </div>
            <span className={cn('w-24 text-right tabular-nums', row.value >= 0 ? 'text-green-600' : 'text-red-600')}>
              {row.value >= 0 ? '+' : ''}{formatCurrency(row.value, currency, i18n.language)}
            </span>
          </div>
        ))}
      </div>

      {/* Behavior metrics */}
      <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-slate-400 pt-1 border-t border-gray-100 dark:border-white/8">
        <span>
          {t('dashboard.avgWin')}: <span className="text-green-600 font-medium">{formatCurrency(avgWin, currency, i18n.language)}</span>
        </span>
        <span>
          {t('dashboard.avgLoss')}: <span className="text-red-600 font-medium">{formatCurrency(avgLoss, currency, i18n.language)}</span>
        </span>
        {riskReward !== null && (
          <span>
            R:R <span className="font-medium text-gray-700 dark:text-slate-300">{riskReward.toFixed(1)}</span>
          </span>
        )}
      </div>
    </div>
  )
}
