import { formatCurrency } from '@/shared/lib/formatCurrency'
import { cn } from '@/shared/lib/cn'
import type { ActivityItem as ActivityItemData } from '../lib/useMergedActivity'

function relativeTime(dateStr: string): string {
  const diffMs = Date.now() - new Date(dateStr).getTime()
  const diffMins = Math.floor(diffMs / 60_000)
  if (diffMins < 60) return `${diffMins}m ago`
  const diffHours = Math.floor(diffMins / 60)
  if (diffHours < 24) return `${diffHours}h ago`
  const diffDays = Math.floor(diffHours / 24)
  return `${diffDays}d ago`
}

interface ActivityItemProps {
  item: ActivityItemData
  locale: string
}

export function ActivityItem({ item, locale }: ActivityItemProps) {
  const borderColor =
    item.kind === 'income'
      ? 'border-l-green-500'
      : item.kind === 'expense'
      ? 'border-l-red-500'
      : 'border-l-blue-500'

  const amountColor =
    item.kind === 'income'
      ? 'text-green-600'
      : item.kind === 'expense'
      ? 'text-red-600'
      : item.amount >= 0
      ? 'text-green-600'
      : 'text-red-600'

  const prefix =
    item.kind === 'income' ? '+' : item.kind === 'expense' ? '-' : item.amount >= 0 ? '+' : ''

  const absAmount = item.kind === 'expense' ? Math.abs(item.amount) : item.amount

  return (
    <li className={cn('flex items-center gap-3 py-2.5 border-l-2 pl-3 rounded-r', borderColor)}>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium capitalize text-gray-800 dark:text-slate-200 truncate">
          {item.label}
        </p>
        <p className="text-xs text-gray-400 dark:text-slate-500">{item.detail}</p>
      </div>
      <span className={cn('text-sm font-semibold tabular-nums', amountColor)}>
        {prefix}{formatCurrency(absAmount, item.currency, locale)}
      </span>
      <span className="text-xs text-gray-400 dark:text-slate-500 whitespace-nowrap w-14 text-right">
        {relativeTime(item.createdAt)}
      </span>
    </li>
  )
}
