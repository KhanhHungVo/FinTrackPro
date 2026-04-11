import { useTranslation } from 'react-i18next'
import { useTransactions } from '@/entities/transaction'
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

export function KpiSummaryWidget() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)

  const currentMonth = new Date().toISOString().slice(0, 7)
  const previousMonth = getPreviousMonth(currentMonth)

  const { data: currentTx } = useTransactions(currentMonth)
  const { data: previousTx } = useTransactions(previousMonth)
  const { data: trades } = useTrades()
  const { data: rates } = useExchangeRates([currency])

  const preferredRate = rates?.[currency] ?? 1

  const sumTx = (txs: typeof currentTx, type: 'Income' | 'Expense') =>
    txs
      ?.filter((tx) => tx.type === type)
      .reduce((s, tx) => s + convertAmount(tx.amount, tx.rateToUsd, preferredRate, tx.currency, currency), 0) ?? 0

  const sumPnl = (month: string) =>
    trades
      ?.filter((tr) => tr.status === 'Closed' && tr.createdAt.slice(0, 7) === month)
      .reduce((s, tr) => s + convertAmount(tr.result, tr.rateToUsd, preferredRate, tr.currency, currency), 0) ?? 0

  const income = sumTx(currentTx, 'Income')
  const expense = sumTx(currentTx, 'Expense')
  const pnl = sumPnl(currentMonth)

  const prevIncome = sumTx(previousTx, 'Income')
  const prevExpense = sumTx(previousTx, 'Expense')
  const prevPnl = sumPnl(previousMonth)

  const incomeDelta = calcDelta(income, prevIncome)
  const expenseDelta = calcDelta(expense, prevExpense)
  const pnlDelta = calcDelta(pnl, prevPnl)

  const hasPrevMonth = (previousTx?.length ?? 0) > 0

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
      {cards.map((card) => (
        <div key={card.label} className="rounded-lg border p-4">
          <p className={`text-xs font-semibold uppercase tracking-wide ${card.labelColor}`}>
            {card.label} · {t('dashboard.thisMonth')}
          </p>
          <p className={`text-2xl font-semibold mt-1 ${card.valueColor}`}>
            {card.value >= 0 && card.label === t('dashboard.tradingPnl') && card.value > 0 ? '+' : ''}
            {formatCurrency(card.value, currency, i18n.language)}
          </p>
          {card.delta !== null && (
            <div className="mt-2">
              <DeltaBadge delta={card.delta} label={t('dashboard.vsLastMonth')} />
            </div>
          )}
          {hasPrevMonth && card.prevValue > 0 && (
            <p className="text-xs text-gray-400 mt-1">
              {t('dashboard.lastMonth')}: {formatCurrency(card.prevValue, currency, i18n.language)}
            </p>
          )}
        </div>
      ))}
    </div>
  )
}
