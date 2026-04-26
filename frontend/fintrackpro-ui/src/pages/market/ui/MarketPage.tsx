import { useTranslation } from 'react-i18next'
import { FearGreedWidget } from '@/widgets/fear-greed-widget'
import { TrendingCoinsWidget } from '@/widgets/trending-coins-widget'
import { TopMarketCapWidget } from '@/widgets/top-market-cap-widget'
import { WatchlistAnalysisWidget } from '@/widgets/watchlist-analysis-widget'
import { SignalsList } from '@/widgets/signals-list'
import { ProFeatureLock } from '@/features/upgrade'

export function MarketPage() {
  const { t } = useTranslation()
  return (
    <div className="mx-auto max-w-6xl p-4 md:p-6 space-y-6">
      <h1 className="text-2xl font-bold">{t('nav.market')}</h1>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <TrendingCoinsWidget />
        <TopMarketCapWidget />
      </div>

      <FearGreedWidget horizontal />

      <ProFeatureLock title={t('proLock.marketIntelligence')}>
        <div className="space-y-6">
          <WatchlistAnalysisWidget />
          <div>
            <h2 className="text-lg font-semibold mb-3">{t('dashboard.recentSignals')}</h2>
            <SignalsList count={20} />
          </div>
        </div>
      </ProFeatureLock>
    </div>
  )
}
