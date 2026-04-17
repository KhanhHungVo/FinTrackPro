import { useTranslation } from 'react-i18next'
import { useTransactionSummary } from '@/entities/transaction'
import { useTrades, useTradesSummary } from '@/entities/trade'
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
  const day = new Date(y, m, 0).getDate()
  return `${yyyyMm}-${String(day).padStart(2, '0')}`
}

export function KpiSummaryWidget() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)

  const currentMonth = new Date().toISOString().slice(0, 7)
  const previousMonth = getPreviousMonth(currentMonth)

  const { data: currentSummary } = useTransactionSummary({ month: currentMonth })
  const { data: previousSummary } = useTransactionSummary({ month: previousMonth })

  // Closed trades for P&L calculation
  const { data: currentTrades } = useTrades({ dateFrom: `${currentMonth}-01`, dateTo: lastDayOf(currentMonth), pageSize: 100 })
  const { data: previousTrades } = useTrades({ dateFrom: `${previousMonth}-01`, dateTo: lastDayOf(previousMonth), pageSize: 100 })

  // Open positions for unrealized P&L
  const { data: openSummary } = useTradesSummary({ status: 'Open' })
  const { data: openTrades } = useTrades({ status: 'Open', pageSize: 100 })

  const { data: rates } = useExchangeRates([currency])
  const preferredRate = rates?.[currency] ?? 1

  const income = convertAmount(currentSummary?.totalIncome ?? 0, 1, preferredRate, 'USD', currency)
  const expense = convertAmount(currentSummary?.totalExpense ?? 0, 1, preferredRate, 'USD', currency)
  const prevIncome = convertAmount(previousSummary?.totalIncome ?? 0, 1, preferredRate, 'USD', currency)
  const prevExpense = convertAmount(previousSummary?.totalExpense ?? 0, 1, preferredRate, 'USD', currency)

  const sumClosedPnl = (items: typeof currentTrades) =>
    items?.items
      ?.filter((tr) => tr.status === 'Closed')
      .reduce((s, tr) => s + convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency), 0) ?? 0

  const pnl = sumClosedPnl(currentTrades)
  const prevPnl = sumClosedPnl(previousTrades)

  // Unrealized P&L: sum unrealizedResult from open positions
  const unrealizedPnl = openTrades?.items
    ?.filter((tr) => tr.status === 'Open')
    .reduce((s, tr) => s + convertAmount(tr.unrealizedResult ?? 0, tr.rateToUsd, preferredRate, tr.currency, currency), 0) ?? 0
  const openCount = openSummary?.totalTrades ?? openTrades?.items?.filter((tr) => tr.status === 'Open').length ?? 0

  const incomeDelta = calcDelta(income, prevIncome)
  const expenseDelta = calcDelta(expense, prevExpense)
  const pnlDelta = calcDelta(pnl, prevPnl)

  const hasPrevMonth = (previousSummary?.totalIncome ?? 0) > 0 || (previousSummary?.totalExpense ?? 0) > 0

  const cards = [
    {
      label: t('dashboard.income'),
      sublabel: t('dashboard.thisMonth'),
      value: income,
      prevValue: prevIncome,
      delta: incomeDelta,
      borderColor: 'border-l-green-500',
      labelColor: 'text-green-600',
      valueColor: 'text-green-600',
      subtitle: hasPrevMonth && prevIncome > 0
        ? `${t('dashboard.lastMonth')}: ${formatCurrency(prevIncome, currency, i18n.language)}`
        : null,
    },
    {
      label: t('dashboard.expenses'),
      sublabel: t('dashboard.thisMonth'),
      value: expense,
      prevValue: prevExpense,
      delta: expenseDelta,
      borderColor: 'border-l-red-500',
      labelColor: 'text-red-600',
      valueColor: 'text-red-600',
      subtitle: hasPrevMonth && prevExpense > 0
        ? `${t('dashboard.lastMonth')}: ${formatCurrency(prevExpense, currency, i18n.language)}`
        : null,
    },
    {
      label: t('dashboard.tradingPnl'),
      sublabel: t('dashboard.thisMonth'),
      value: pnl,
      prevValue: prevPnl,
      delta: pnlDelta,
      borderColor: 'border-l-blue-500',
      labelColor: 'text-blue-600',
      valueColor: pnl >= 0 ? 'text-green-600' : 'text-red-600',
      subtitle: hasPrevMonth && prevPnl !== 0
        ? `${t('dashboard.lastMonth')}: ${formatCurrency(prevPnl, currency, i18n.language)}`
        : null,
    },
    {
      label: t('dashboard.unrealizedPnl'),
      sublabel: null,
      value: unrealizedPnl,
      prevValue: null,
      delta: null,
      borderColor: 'border-l-purple-500',
      labelColor: 'text-purple-600',
      valueColor: unrealizedPnl >= 0 ? 'text-green-600' : 'text-red-600',
      subtitle: openCount > 0
        ? t('dashboard.openPositions', { count: openCount })
        : t('dashboard.noOpenPositions'),
    },
  ]

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {cards.map((card) => (
        <div
          key={card.label}
          className={`rounded-xl p-5 bg-white dark:bg-white/4 dark:backdrop-blur-sm border-l-4 ${card.borderColor}`}
        >
          <p className={`text-xs font-semibold uppercase tracking-wide ${card.labelColor}`}>
            {card.label}{card.sublabel ? ` · ${card.sublabel}` : ''}
          </p>
          <p className={`text-2xl font-bold tracking-tight mt-1 ${card.valueColor}`}>
            {card.value > 0 && card.label !== t('dashboard.income') && card.label !== t('dashboard.expenses') ? '+' : ''}
            {formatCurrency(card.value, currency, i18n.language)}
          </p>
          {card.delta !== null && (
            <div className="mt-2">
              <DeltaBadge delta={card.delta} label={t('dashboard.vsLastMonth')} />
            </div>
          )}
          {card.subtitle && (
            <p className="text-xs text-gray-400 dark:text-slate-500 mt-1">{card.subtitle}</p>
          )}
        </div>
      ))}
    </div>
  )
}
