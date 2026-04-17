import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useTrades } from '@/entities/trade'
import { OpenPositionsPanel } from './OpenPositionsPanel'
import { ClosedTradesPanel } from './ClosedTradesPanel'

export function TradingIntelligenceWidget() {
  const { t } = useTranslation()

  const { data: allTrades, isLoading } = useTrades({ pageSize: 1 })
  const totalTrades = allTrades?.totalCount ?? 0

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="animate-pulse h-7 w-48 rounded-lg bg-gray-100 dark:bg-white/5" />
        <div className="animate-pulse rounded-xl bg-gray-100 dark:bg-white/5 h-48 sm:h-64 md:h-80" />
      </div>
    )
  }

  if (totalTrades === 0) {
    return (
      <div className="space-y-4">
        <h2 className="text-lg font-semibold">{t('dashboard.tradingIntelligence')}</h2>
        <div className="glass-card p-5 flex flex-col items-center justify-center gap-2 h-64">
          <p className="text-sm text-gray-400 dark:text-slate-500">{t('dashboard.noTrades')}</p>
          <Link to="/trades" className="text-xs text-blue-500 hover:underline">
            {t('dashboard.goToTrades')} →
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <h2 className="text-lg font-semibold">{t('dashboard.tradingIntelligence')}</h2>

      <div className="glass-card p-5 space-y-6">
        <OpenPositionsPanel />
        <hr className="border-gray-100 dark:border-white/8" />
        <ClosedTradesPanel />
      </div>
    </div>
  )
}
