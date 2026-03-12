import { NotificationSettingsForm } from '@/features/notification-settings'
import { WatchlistManager } from '@/features/manage-watchlist'

export function SettingsPage() {
  return (
    <div className="mx-auto max-w-3xl space-y-8 p-6">
      <h1 className="text-2xl font-bold">Settings</h1>

      <section>
        <h2 className="text-base font-semibold text-gray-700 mb-3">Notifications</h2>
        <NotificationSettingsForm />
      </section>

      <section>
        <h2 className="text-base font-semibold text-gray-700 mb-3">Signal Watchlist</h2>
        <WatchlistManager />
      </section>
    </div>
  )
}
