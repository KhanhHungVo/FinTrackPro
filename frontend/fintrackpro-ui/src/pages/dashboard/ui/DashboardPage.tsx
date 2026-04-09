import { useTranslation } from 'react-i18next'
import { FearGreedWidget } from '@/widgets/fear-greed-widget'
import { SignalsList } from '@/widgets/signals-list'
import { TrendingCoinsWidget } from '@/widgets/trending-coins-widget'
import { useTransactions } from '@/entities/transaction'
import { useTrades } from '@/entities/trade'
import { useLocaleStore } from '@/features/locale'
import { useExchangeRates } from '@/entities/exchange-rate'
import { convertAmount } from '@/shared/lib/convertAmount'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { FreePlanAdBanner } from '@/shared/ui/FreePlanAdBanner'

export function DashboardPage() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const currentMonth = new Date().toISOString().slice(0, 7)
  const { data: transactions } = useTransactions(currentMonth)
  const { data: trades } = useTrades()
  const { data: rates } = useExchangeRates([currency])

  const preferredRate = rates?.[currency] ?? 1

  const income = transactions
    ?.filter((t) => t.type === 'Income')
    .reduce((s, t) => s + convertAmount(t.amount, t.rateToUsd, preferredRate, t.currency, currency), 0) ?? 0
  const expense = transactions
    ?.filter((t) => t.type === 'Expense')
    .reduce((s, t) => s + convertAmount(t.amount, t.rateToUsd, preferredRate, t.currency, currency), 0) ?? 0
  const totalPnl = trades
    ?.reduce((s, t) => s + convertAmount(t.result, t.rateToUsd, preferredRate, t.currency, currency), 0) ?? 0

  return (
    <>
      <FreePlanAdBanner />
      <div className="mx-auto max-w-5xl p-4 md:p-6 space-y-6">
      <h1 className="text-2xl font-bold">{t('dashboard.title')}</h1>

      {/* Finance summary */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="rounded-lg border p-4">
          <p className="text-sm text-gray-500">{t('dashboard.income')} ({t('dashboard.thisMonth')})</p>
          <p className="text-2xl font-semibold text-green-600">
            {formatCurrency(income, currency, i18n.language)}
          </p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-sm text-gray-500">{t('dashboard.expenses')} ({t('dashboard.thisMonth')})</p>
          <p className="text-2xl font-semibold text-red-600">
            {formatCurrency(expense, currency, i18n.language)}
          </p>
        </div>
        <div className="rounded-lg border p-4">
          <p className="text-sm text-gray-500">{t('dashboard.tradingPnl')}</p>
          <p className={`text-2xl font-semibold ${totalPnl >= 0 ? 'text-green-600' : 'text-red-600'}`}>
            {totalPnl >= 0 ? '+' : ''}{formatCurrency(totalPnl, currency, i18n.language)}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Fear & Greed */}
        <FearGreedWidget />

        {/* Trending coins */}
        <TrendingCoinsWidget />
      </div>

      {/* Signals */}
      <div>
        <h2 className="text-lg font-semibold mb-3">Recent Signals</h2>
        <SignalsList />
      </div>
    </div>
    </>
  )
}
