import { useTranslation } from 'react-i18next'
import { useTransactionSummary } from '@/entities/transaction'
import { useTrades } from '@/entities/trade'
import { useExchangeRates } from '@/entities/exchange-rate'
import { useLocaleStore } from '@/features/locale'
import { convertAmount } from '@/shared/lib/convertAmount'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { calcDelta } from '@/shared/lib/calcDelta'
import { DeltaBadge } from './DeltaBadge'

function getPreviousMonth(yyyyMm: string): string {
  const [y, m] = yyyyMm.split('-').map(Number)
  return new Date(y, m - 2, 1).toISOString().slice(0, 7)
}

function lastDayOf(yyyyMm: string): string {
  const [y, m] = yyyyMm.split('-').map(Number)
  const day = new Date(y, m, 0).getDate() // day 0 of next month = last day of this month
  return `${yyyyMm}-${String(day).padStart(2, '0')}`
}

export function KpiSummaryWidget() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)

  const currentMonth = new Date().toISOString().slice(0, 7)
  const previousMonth = getPreviousMonth(currentMonth)

  // Use summary endpoints — no rows transferred, only scalars
  const { data: currentSummary } = useTransactionSummary({ month: currentMonth })
  const { data: previousSummary } = useTransactionSummary({ month: previousMonth })

  // Trades: fetch current and prev month pages for P&L calculation (small set per month)
  const { data: currentTrades } = useTrades({ dateFrom: `${currentMonth}-01`, dateTo: lastDayOf(currentMonth), pageSize: 100 })
  const { data: previousTrades } = useTrades({ dateFrom: `${previousMonth}-01`, dateTo: lastDayOf(previousMonth), pageSize: 100 })

  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1

  // Income/Expense come directly from summary (already aggregated in USD on server)
  const income = convertAmount(currentSummary?.totalIncome ?? 0, 1, preferredRate, 'USD', currency)
  const expense = convertAmount(currentSummary?.totalExpense ?? 0, 1, preferredRate, 'USD', currency)
  const prevIncome = convertAmount(previousSummary?.totalIncome ?? 0, 1, preferredRate, 'USD', currency)
  const prevExpense = convertAmount(previousSummary?.totalExpense ?? 0, 1, preferredRate, 'USD', currency)

  // P&L: sum from filtered trade pages
  const sumPnl = (items: typeof currentTrades) =>
    items?.items
      ?.filter((tr) => tr.status === 'Closed')
      .reduce((s, tr) => s + convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency), 0) ?? 0

  const pnl = sumPnl(currentTrades)
  const prevPnl = sumPnl(previousTrades)

  const incomeDelta = calcDelta(income, prevIncome)
  const expenseDelta = calcDelta(expense, prevExpense)
  const pnlDelta = calcDelta(pnl, prevPnl)

  const hasPrevMonth = (previousSummary?.totalIncome ?? 0) > 0 || (previousSummary?.totalExpense ?? 0) > 0

  const cards = [
    {
      label: t('dashboard.income'),
      value: income,
      prevValue: prevIncome,
      delta: incomeDelta,
      labelColor: 'text-green-600',
      valueColor: 'text-green-600',
    },
    {
      label: t('dashboard.expenses'),
      value: expense,
      prevValue: prevExpense,
      delta: expenseDelta,
      labelColor: 'text-red-600',
      valueColor: 'text-red-600',
    },
    {
      label: t('dashboard.tradingPnl'),
      value: pnl,
      prevValue: prevPnl,
      delta: pnlDelta,
      labelColor: pnl >= 0 ? 'text-green-600' : 'text-red-600',
      valueColor: pnl >= 0 ? 'text-green-600' : 'text-red-600',
    },
  ]

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
      {cards.map((card, i) => {
        const borderColors = ['border-l-green-500', 'border-l-red-500', 'border-l-blue-500']
        return (
          <div key={card.label} className={`rounded-xl p-5 bg-white dark:bg-white/4 dark:backdrop-blur-sm border-l-4 ${borderColors[i]}`}>
            <p className={`text-xs font-semibold uppercase tracking-wide ${card.labelColor}`}>
              {card.label} · {t('dashboard.thisMonth')}
            </p>
            <p className={`text-3xl font-bold tracking-tight mt-1 ${card.valueColor}`}>
              {card.value >= 0 && card.label === t('dashboard.tradingPnl') && card.value > 0 ? '+' : ''}
              {formatCurrency(card.value, currency, i18n.language)}
            </p>
            {card.delta !== null && (
              <div className="mt-2">
                <DeltaBadge delta={card.delta} label={t('dashboard.vsLastMonth')} />
              </div>
            )}
            {hasPrevMonth && card.prevValue > 0 && (
              <p className="text-xs text-gray-400 dark:text-slate-500 mt-1">
                {t('dashboard.lastMonth')}: {formatCurrency(card.prevValue, currency, i18n.language)}
              </p>
            )}
          </div>
        )
      })}
    </div>
  )
}
