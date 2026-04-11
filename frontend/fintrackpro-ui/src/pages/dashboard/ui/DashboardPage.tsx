import { useTranslation } from 'react-i18next'
import { FearGreedWidget } from '@/widgets/fear-greed-widget'
import { SignalsList } from '@/widgets/signals-list'
import { TrendingCoinsWidget } from '@/widgets/trending-coins-widget'
import { KpiSummaryWidget } from '@/widgets/kpi-summary'
import { FreePlanAdBanner } from '@/shared/ui/FreePlanAdBanner'

export function DashboardPage() {
  const { t } = useTranslation()

  return (
    <>
      <FreePlanAdBanner />
      <div className="mx-auto max-w-5xl p-4 md:p-6 space-y-6">
        <h1 className="text-2xl font-bold">{t('dashboard.title')}</h1>

        <KpiSummaryWidget />

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <FearGreedWidget />
          <TrendingCoinsWidget />
        </div>

        <div>
          <h2 className="text-lg font-semibold mb-3">{t('dashboard.recentSignals')}</h2>
          <SignalsList />
        </div>
      </div>
    </>
  )
}
