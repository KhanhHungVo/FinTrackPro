import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useBudgetHealth } from '../lib/useBudgetHealth'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { cn } from '@/shared/lib/cn'
import { useLocaleStore } from '@/features/locale'

export function BudgetHealthWidget() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const currentMonth = new Date().toISOString().slice(0, 7)
  const { items, onTrackCount, totalCount, isLoading } = useBudgetHealth(currentMonth)

  if (isLoading) {
    return <div className="animate-pulse h-64 rounded-xl bg-gray-100 dark:bg-white/5" />
  }

  if (items.length === 0) {
    return (
      <div className="glass-card p-5 flex flex-col items-center justify-center gap-2 h-64">
        <p className="text-sm text-gray-400 dark:text-slate-500">{t('dashboard.noBudgetsSet')}</p>
        <Link to="/budgets" className="text-xs text-blue-500 hover:underline">
          {t('dashboard.setBudgets')} →
        </Link>
      </div>
    )
  }

  return (
    <div className="glass-card p-5 space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm font-semibold uppercase tracking-wide text-yellow-600 dark:text-yellow-500">
          {t('dashboard.budgetHealth')}
        </p>
        <span className="text-xs text-gray-500 dark:text-slate-400">
          {t('dashboard.budgetsOnTrack', { count: onTrackCount, total: totalCount })}
        </span>
      </div>

      <ul className="space-y-3">
        {items.map((item) => {
          const pct = Math.min(item.pct, 100)
          const barColor =
            item.overrun
              ? 'bg-red-500'
              : item.pct > 80
              ? 'bg-yellow-400'
              : 'bg-green-500'

          return (
            <li key={item.id} className="space-y-1">
              <div className="flex items-center justify-between text-sm">
                <span className="capitalize text-gray-700 dark:text-slate-300 truncate max-w-[55%]">
                  {item.category}
                </span>
                <span
                  className={cn(
                    'text-xs font-semibold tabular-nums',
                    item.overrun
                      ? 'text-red-600 dark:text-red-400'
                      : item.pct > 80
                      ? 'text-yellow-600 dark:text-yellow-400'
                      : 'text-gray-500 dark:text-slate-400',
                  )}
                >
                  {formatCurrency(item.spent, currency, i18n.language)}
                  {' / '}
                  {formatCurrency(item.limit, currency, i18n.language)}
                </span>
              </div>
              <div className="h-1.5 w-full rounded-full bg-gray-100 dark:bg-white/10">
                <div
                  className={cn('h-1.5 rounded-full transition-all', barColor)}
                  style={{ width: `${pct}%` }}
                />
              </div>
              <div className="flex justify-end">
                <span
                  className={cn(
                    'text-xs tabular-nums',
                    item.overrun
                      ? 'text-red-500 dark:text-red-400'
                      : 'text-gray-400 dark:text-slate-500',
                  )}
                >
                  {item.pct.toFixed(0)}%
                  {item.overrun && ` · ${t('budgets.overBudget')}`}
                </span>
              </div>
            </li>
          )
        })}
      </ul>

      <Link
        to="/budgets"
        className="block text-center text-xs text-blue-500 hover:underline pt-1"
      >
        {t('dashboard.viewAllBudgets')} →
      </Link>
    </div>
  )
}
