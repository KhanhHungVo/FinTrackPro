import type { TradeDirection, TradeStatus } from '@/entities/trade'
import { useTranslation } from 'react-i18next'

export interface TradeFilters {
  search: string
  status: '' | TradeStatus
  direction: '' | TradeDirection
  dateFrom: string
  dateTo: string
}

interface TradeFilterBarProps {
  value: TradeFilters
  onChange: (next: Partial<TradeFilters>) => void
}

export function TradeFilterBar({ value, onChange }: TradeFilterBarProps) {
  const { t } = useTranslation()

  return (
    <div className="flex flex-wrap items-end gap-2">
      {/* Search by symbol */}
      <input
        type="search"
        placeholder={t('trades.symbol')}
        value={value.search}
        onChange={(e) => onChange({ search: e.target.value })}
        className="rounded-md border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white dark:placeholder:text-slate-500 w-36"
      />

      {/* Status toggle */}
      <div className="flex rounded-md border border-gray-300 dark:border-white/12 overflow-hidden text-sm">
        {(['', 'Open', 'Closed'] as const).map((opt) => (
          <button
            key={opt || 'all'}
            onClick={() => onChange({ status: opt })}
            className={
              value.status === opt
                ? 'px-3 py-1.5 bg-blue-600 text-white font-medium'
                : 'px-3 py-1.5 text-gray-600 dark:text-slate-300 hover:bg-gray-50 dark:hover:bg-white/5'
            }
          >
            {opt === '' ? t('common.all') : opt === 'Open' ? t('trades.open') : t('trades.closed')}
          </button>
        ))}
      </div>

      {/* Direction toggle */}
      <div className="flex rounded-md border border-gray-300 dark:border-white/12 overflow-hidden text-sm">
        {(['', 'Long', 'Short'] as const).map((opt) => (
          <button
            key={opt || 'all'}
            onClick={() => onChange({ direction: opt })}
            className={
              value.direction === opt
                ? 'px-3 py-1.5 bg-blue-600 text-white font-medium'
                : 'px-3 py-1.5 text-gray-600 dark:text-slate-300 hover:bg-gray-50 dark:hover:bg-white/5'
            }
          >
            {opt === '' ? t('common.all') : opt === 'Long' ? t('trades.long') : t('trades.short')}
          </button>
        ))}
      </div>

      {/* Date range */}
      <input
        type="date"
        value={value.dateFrom}
        onChange={(e) => onChange({ dateFrom: e.target.value })}
        className="rounded-md border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
      />
      <span className="text-sm text-gray-400 dark:text-slate-500">–</span>
      <input
        type="date"
        value={value.dateTo}
        onChange={(e) => onChange({ dateTo: e.target.value })}
        className="rounded-md border border-gray-300 px-3 py-1.5 text-sm outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
      />
    </div>
  )
}
