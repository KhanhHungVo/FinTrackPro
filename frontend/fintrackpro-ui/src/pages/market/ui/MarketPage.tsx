import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { FearGreedWidget } from '@/widgets/fear-greed-widget'
import { TrendingCoinsWidget } from '@/widgets/trending-coins-widget'
import { SignalsList } from '@/widgets/signals-list'

export function MarketPage() {
  const { t } = useTranslation()
  return (
    <div className="mx-auto max-w-6xl p-4 md:p-6 space-y-6">
      <h1 className="text-2xl font-bold">{t('nav.market')}</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <FearGreedWidget />
        <TrendingCoinsWidget />
      </div>

      <div>
        <h2 className="text-lg font-semibold mb-3">{t('dashboard.recentSignals')}</h2>
        <SignalsList count={20} />
      </div>

      <div className="text-center">
        <Link to="/settings?tab=watchlist" className="text-sm text-blue-500 hover:underline">
          {t('dashboard.manageWatchlist')} →
        </Link>
      </div>
    </div>
  )
}
