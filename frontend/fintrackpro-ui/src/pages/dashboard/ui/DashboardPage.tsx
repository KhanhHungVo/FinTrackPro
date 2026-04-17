import { useTranslation } from 'react-i18next'
import { KpiSummaryWidget } from '@/widgets/kpi-summary'
import { FreePlanAdBanner } from '@/shared/ui/FreePlanAdBanner'
import { useAuthStore } from '@/features/auth'
import { ExpenseAllocationWidget } from '@/widgets/expense-allocation'
import { BudgetHealthWidget } from '@/widgets/budget-health'
import { TradingIntelligenceWidget } from '@/widgets/trading-intelligence'
import { RecentActivityWidget } from '@/widgets/recent-activity'
import { ContextualSignalsWidget } from '@/widgets/contextual-signals'

function useGreeting() {
  const { t } = useTranslation()
  const hour = new Date().getHours()
  if (hour < 12) return t('dashboard.goodMorning')
  if (hour < 18) return t('dashboard.goodAfternoon')
  return t('dashboard.goodEvening')
}

export function DashboardPage() {
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

        {/* Section 1: KPI Header */}
        <KpiSummaryWidget />

        {/* Section 2: Expense Allocation + Budget Health */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <ExpenseAllocationWidget />
          <BudgetHealthWidget />
        </div>

        {/* Section 3: Trading Intelligence (hidden if 0 trades) */}
        <TradingIntelligenceWidget />

        {/* Section 4: Recent Activity */}
        <RecentActivityWidget />

        {/* Section 5: Contextual Signals (hidden if watchlist empty) */}
        <ContextualSignalsWidget />
      </div>
    </>
  )
}
