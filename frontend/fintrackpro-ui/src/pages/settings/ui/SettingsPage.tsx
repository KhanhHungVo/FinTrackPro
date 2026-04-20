import { useSearchParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { NotificationSettingsForm } from '@/features/notification-settings'
import { WatchlistManager } from '@/features/manage-watchlist'
import { SubscriptionSection } from '@/features/upgrade'
import { ManageCategoriesSection } from '@/features/manage-transaction-categories'
import { cn } from '@/shared/lib/cn'

type TabSlug = 'account' | 'billing' | 'notifications' | 'categories' | 'watchlist'

function useSettingsTabs() {
  const { t } = useTranslation()
  return [
    { slug: 'account'       as TabSlug, label: t('settings.account')          },
    { slug: 'billing'       as TabSlug, label: t('settings.billing')           },
    { slug: 'notifications' as TabSlug, label: t('settings.notifications_tab') },
    { slug: 'categories'    as TabSlug, label: t('settings.categories_tab')    },
    { slug: 'watchlist'     as TabSlug, label: t('settings.watchlist_tab')     },
  ]
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
  const tabs = useSettingsTabs()

  const rawTab = searchParams.get('tab') as TabSlug | null
  const validSlugs = tabs.map((tab) => tab.slug)
  const activeTab: TabSlug = rawTab && validSlugs.includes(rawTab) ? rawTab : 'account'

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

      {/* Mobile: horizontal scrollable tab strip */}
      <div className="flex gap-1 overflow-x-auto pb-1 md:hidden">
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
        </div>
      </div>

      {/* Mobile: content panel */}
      <div className="md:hidden">
        {activeTab === 'account'       && <AccountTab />}
        {activeTab === 'billing'       && <SubscriptionSection />}
        {activeTab === 'notifications' && <NotificationSettingsForm />}
        {activeTab === 'categories'    && <ManageCategoriesSection />}
        {activeTab === 'watchlist'     && <WatchlistManager />}
      </div>
    </div>
  )
}
