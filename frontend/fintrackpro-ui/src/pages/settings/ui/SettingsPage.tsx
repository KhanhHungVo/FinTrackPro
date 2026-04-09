import { useTranslation } from 'react-i18next'
import { NotificationSettingsForm } from '@/features/notification-settings'
import { WatchlistManager } from '@/features/manage-watchlist'
import { SubscriptionSection } from '@/features/upgrade'

export function SettingsPage() {
  const { t } = useTranslation()

  return (
    <div className="mx-auto max-w-3xl space-y-8 p-4 md:p-6">
      <h1 className="text-2xl font-bold">{t('settings.title')}</h1>

      <section>
        <h2 className="text-base font-semibold text-gray-700 mb-3">{t('settings.subscription')}</h2>
        <SubscriptionSection />
      </section>

      <section>
        <h2 className="text-base font-semibold text-gray-700 mb-3">{t('settings.notifications')}</h2>
        <NotificationSettingsForm />
      </section>

      <section>
        <h2 className="text-base font-semibold text-gray-700 mb-3">{t('settings.watchlist')}</h2>
        <WatchlistManager />
      </section>
    </div>
  )
}
