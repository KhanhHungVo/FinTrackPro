import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useMergedActivity } from '../lib/useMergedActivity'
import { ActivityItem } from './ActivityItem'

export function RecentActivityWidget() {
  const { t, i18n } = useTranslation()
  const { items, isLoading } = useMergedActivity()

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="animate-pulse h-10 rounded-lg bg-gray-100 dark:bg-white/5" />
        ))}
      </div>
    )
  }

  return (
    <div className="space-y-3">
      <h2 className="text-lg font-semibold">{t('dashboard.recentActivity')}</h2>
      {items.length === 0 ? (
        <div className="glass-card p-5 text-center space-y-2">
          <p className="text-sm text-gray-400 dark:text-slate-500">{t('dashboard.noRecentActivity')}</p>
          <div className="flex items-center justify-center gap-4 text-xs">
            <Link to="/transactions" className="text-blue-500 hover:underline">
              {t('nav.transactions')} →
            </Link>
            <Link to="/trades" className="text-blue-500 hover:underline">
              {t('nav.trades')} →
            </Link>
          </div>
        </div>
      ) : (
        <div className="glass-card px-4 py-2">
          <ul className="divide-y divide-gray-50 dark:divide-white/5">
            {items.map((item) => (
              <ActivityItem key={item.id} item={item} locale={i18n.language} />
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
