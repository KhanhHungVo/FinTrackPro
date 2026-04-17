import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts'
import { useExpensesByCategory } from '../lib/useExpensesByCategory'
import { formatCurrency } from '@/shared/lib/formatCurrency'
import { useLocaleStore } from '@/features/locale'

const PALETTE = [
  '#3b82f6', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6',
  '#06b6d4', '#f97316', '#ec4899', '#14b8a6', '#6366f1',
  '#84cc16', '#a78bfa',
]

function getColor(index: number) {
  return PALETTE[index % PALETTE.length]
}

export function ExpenseAllocationWidget() {
  const { t, i18n } = useTranslation()
  const currency = useLocaleStore((s) => s.currency)
  const currentMonth = new Date().toISOString().slice(0, 7)
  const { slices, total, isLoading } = useExpensesByCategory(currentMonth)

  if (isLoading) {
    return <div className="animate-pulse h-64 rounded-xl bg-gray-100 dark:bg-white/5" />
  }

  if (slices.length === 0) {
    return (
      <div className="glass-card p-5 flex flex-col items-center justify-center gap-2 h-64">
        <p className="text-sm text-gray-400 dark:text-slate-500">{t('dashboard.noExpensesThisMonth')}</p>
        <Link to="/transactions" className="text-xs text-blue-500 hover:underline">
          {t('dashboard.addTransaction')} →
        </Link>
      </div>
    )
  }

  const chartData = slices.map((s, i) => ({ ...s, fill: getColor(i) }))

  return (
    <div className="glass-card p-5 space-y-4">
      <p className="text-sm font-semibold uppercase tracking-wide text-red-600">
        {t('dashboard.expenseAllocation')} · {t('dashboard.thisMonth')}
      </p>

      <div className="flex flex-col sm:flex-row items-center gap-4">
        <div className="relative w-40 h-40 flex-shrink-0">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={chartData}
                cx="50%"
                cy="50%"
                innerRadius={42}
                outerRadius={68}
                dataKey="amount"
                strokeWidth={2}
                stroke="transparent"
              >
                {chartData.map((entry) => (
                  <Cell key={entry.category} fill={entry.fill} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value) => formatCurrency(Number(value), currency, i18n.language)}
                contentStyle={{
                  background: 'var(--tooltip-bg, #fff)',
                  border: '1px solid #e5e7eb',
                  borderRadius: '8px',
                  fontSize: '12px',
                }}
              />
            </PieChart>
          </ResponsiveContainer>
          <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
            <span className="text-xs text-gray-400 dark:text-slate-500">{t('common.total')}</span>
            <span className="text-sm font-bold text-gray-800 dark:text-slate-100 leading-tight">
              {formatCurrency(total, currency, i18n.language)}
            </span>
          </div>
        </div>

        <ul className="flex-1 space-y-1.5 w-full">
          {slices.slice(0, 5).map((slice, i) => (
            <li key={slice.category} className="flex items-center gap-2 text-sm">
              <span
                className="w-2.5 h-2.5 rounded-full flex-shrink-0"
                style={{ background: getColor(i) }}
              />
              <span className="flex-1 truncate text-gray-700 dark:text-slate-300 capitalize">
                {slice.category}
              </span>
              <span className="font-medium text-gray-800 dark:text-slate-200 tabular-nums">
                {formatCurrency(slice.amount, currency, i18n.language)}
              </span>
              <span className="text-xs text-gray-400 dark:text-slate-500 w-9 text-right tabular-nums">
                {slice.percentage.toFixed(0)}%
              </span>
            </li>
          ))}
          {slices.length > 5 && (
            <li className="text-xs text-gray-400 dark:text-slate-500 pl-4">
              +{slices.length - 5} {t('dashboard.moreCategories')}
            </li>
          )}
        </ul>
      </div>
    </div>
  )
}
