import { useTranslation } from 'react-i18next'
import { FearGreedWidget } from '@/widgets/fear-greed-widget'
import { SignalsList } from '@/widgets/signals-list'
import { TrendingCoinsWidget } from '@/widgets/trending-coins-widget'
import { KpiSummaryWidget } from '@/widgets/kpi-summary'
import { FreePlanAdBanner } from '@/shared/ui/FreePlanAdBanner'
import { useAuthStore } from '@/features/auth'

function useGreeting() {
  const { t } = useTranslation()
  const hour = new Date().getHours()
  if (hour < 12) return t('dashboard.goodMorning')
  if (hour < 18) return t('dashboard.goodAfternoon')
  return t('dashboard.goodEvening')
}

export function DashboardPage() {
  const { t } = useTranslation()
  const displayName = useAuthStore((s) => s.displayName)
  const firstName = displayName?.split(' ')[0] ?? ''
  const greeting = useGreeting()

  const today = new Date().toLocaleDateString(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
  })

  return (
    <>
      <FreePlanAdBanner />
      <div className="mx-auto max-w-6xl p-4 md:p-6 space-y-6">
        <div>
          <h1 className="text-2xl font-bold">
            {greeting}{firstName ? `, ${firstName}` : ''} 👋
          </h1>
          <div className="mt-1 flex items-center gap-3 text-sm text-gray-500 dark:text-slate-400">
            <span>{today}</span>
          </div>
        </div>

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
