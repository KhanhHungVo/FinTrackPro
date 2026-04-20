import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useWatchedSymbols } from '@/entities/watched-symbol'
import { SignalsList } from '@/widgets/signals-list'

export function ContextualSignalsWidget() {
  const { t } = useTranslation()
  const { data: watchedSymbols, isLoading } = useWatchedSymbols()

  if (isLoading) return null

  // Hide entirely when watchlist is empty
  if (!watchedSymbols || watchedSymbols.length === 0) return null

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">{t('dashboard.signalsForWatchlist')}</h2>
        <Link to="/settings?tab=watchlist" className="text-xs text-blue-500 hover:underline">
          {t('dashboard.manageWatchlist')} →
        </Link>
      </div>
      <SignalsList count={5} />
    </div>
  )
}
