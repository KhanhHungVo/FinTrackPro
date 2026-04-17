import { useTranslation } from 'react-i18next'
import { useTrades } from '@/entities/trade'
import { OpenPositionsPanel } from './OpenPositionsPanel'
import { ClosedTradesPanel } from './ClosedTradesPanel'

export function TradingIntelligenceWidget() {
  const { t } = useTranslation()

  // Lightweight probe to decide whether to render at all
  const { data: allTrades, isLoading } = useTrades({ pageSize: 1 })
  const totalTrades = allTrades?.totalCount ?? 0

  if (isLoading) return null
  if (totalTrades === 0) return null

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
