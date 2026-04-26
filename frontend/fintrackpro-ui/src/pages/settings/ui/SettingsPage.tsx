import { useSearchParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { NotificationSettingsForm } from '@/features/notification-settings'
import { WatchlistManager } from '@/features/manage-watchlist'
import { SubscriptionSection } from '@/features/upgrade'
import { ManageCategoriesSection } from '@/features/manage-transaction-categories'
import { AdminSubscriptionPanel } from '@/features/admin-subscription'
import { useAuthStore } from '@/features/auth'
import { cn } from '@/shared/lib/cn'

type TabSlug = 'account' | 'billing' | 'notifications' | 'categories' | 'watchlist' | 'admin'

function useSettingsTabs(isAdmin: boolean) {
  const { t } = useTranslation()
  const tabs: { slug: TabSlug; label: string }[] = [
    { slug: 'account',       label: t('settings.account')          },
    { slug: 'billing',       label: t('settings.billing')           },
    { slug: 'notifications', label: t('settings.notifications_tab') },
    { slug: 'categories',    label: t('settings.categories_tab')    },
    { slug: 'watchlist',     label: t('settings.watchlist_tab')     },
  ]
  if (isAdmin) tabs.push({ slug: 'admin', label: '⚙ Admin' })
  return tabs
}

function AccountTab() {
  const { t } = useTranslation()
  return (
    <div className="rounded-xl border bg-white p-6 dark:bg-white/4 dark:border-white/6 space-y-2">
      <h2 className="text-base font-semibold text-gray-700 dark:text-slate-300">
        {t('settings.accountPlaceholderTitle')}
      </h2>
      <p className="text-sm text-gray-500 dark:text-slate-400">
        {t('settings.accountPlaceholderBody')}
      </p>
    </div>
  )
}

export function SettingsPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const isAdmin = useAuthStore((s) => s.isAdmin)
  const tabs = useSettingsTabs(isAdmin)

  const rawTab = searchParams.get('tab') as TabSlug | null
  const validSlugs = tabs.map((tab) => tab.slug)
  const activeTab: TabSlug =
    rawTab && validSlugs.includes(rawTab) && (rawTab !== 'admin' || isAdmin)
      ? rawTab
      : 'account'

  function handleTabChange(slug: TabSlug) {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      next.set('tab', slug)
      return next
    })
  }

  return (
    <div className="mx-auto max-w-4xl p-4 md:p-6 space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
        {t('settings.title')}
      </h1>

      {/* Mobile: horizontal scrollable tab strip — bleed to viewport edges so first/last tabs aren't clipped by parent padding */}
      <div className="-mx-4 flex gap-1 overflow-x-auto px-4 pb-3 md:hidden scrollbar-hide">
        {tabs.map(({ slug, label }) => (
          <button
            key={slug}
            onClick={() => handleTabChange(slug)}
            className={cn(
              'shrink-0 rounded-md px-3 py-1.5 text-sm font-medium transition-colors whitespace-nowrap',
              activeTab === slug
                ? 'bg-blue-600 text-white'
                : 'text-gray-600 hover:bg-gray-100 dark:text-slate-400 dark:hover:bg-white/5',
            )}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Desktop: sidebar + content */}
      <div className="hidden md:flex gap-6 items-start">
        <nav className="w-44 shrink-0 flex flex-col gap-1" aria-label="Settings navigation">
          {tabs.map(({ slug, label }) => (
            <button
              key={slug}
              onClick={() => handleTabChange(slug)}
              className={cn(
                'w-full rounded-md px-3 py-2 text-left text-sm font-medium transition-colors',
                activeTab === slug
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-600 hover:bg-gray-100 dark:text-slate-400 dark:hover:bg-white/5',
              )}
            >
              {label}
            </button>
          ))}
        </nav>

        <div className="flex-1 min-w-0">
          {activeTab === 'account'       && <AccountTab />}
          {activeTab === 'billing'       && <SubscriptionSection />}
          {activeTab === 'notifications' && <NotificationSettingsForm />}
          {activeTab === 'categories'    && <ManageCategoriesSection />}
          {activeTab === 'watchlist'     && <WatchlistManager />}
          {activeTab === 'admin'         && isAdmin && <AdminSubscriptionPanel />}
        </div>
      </div>

      {/* Mobile: content panel */}
      <div className="md:hidden">
        {activeTab === 'account'       && <AccountTab />}
        {activeTab === 'billing'       && <SubscriptionSection />}
        {activeTab === 'notifications' && <NotificationSettingsForm />}
        {activeTab === 'categories'    && <ManageCategoriesSection />}
        {activeTab === 'watchlist'     && <WatchlistManager />}
        {activeTab === 'admin'         && isAdmin && <AdminSubscriptionPanel />}
      </div>
    </div>
  )
}
