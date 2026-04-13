import { useEffect } from 'react'
import { useSearchParams } from 'react-router'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useSubscriptionStatus, useCreatePortalSession } from '@/entities/subscription'
import { UpgradeButton } from './UpgradeButton'

export function SubscriptionSection() {
  const { t } = useTranslation()
  const { data: status, isLoading } = useSubscriptionStatus()
  const { mutate: createPortal, isPending: portalPending } = useCreatePortalSession()
  const [searchParams, setSearchParams] = useSearchParams()

  useEffect(() => {
    if (searchParams.get('subscribed') === '1') {
      toast.success("You're now on Pro!")
      setSearchParams((prev) => {
        prev.delete('subscribed')
        return prev
      })
    }
  }, [searchParams, setSearchParams])

  function handleManage() {
    createPortal(
      { returnUrl: `${window.location.origin}/settings` },
      {
        onSuccess: ({ portalUrl }) => {
          window.location.href = portalUrl
        },
        onError: () => toast.error('Failed to open billing portal.'),
      },
    )
  }

  if (isLoading) return <div className="animate-pulse h-24 rounded-lg bg-gray-100 dark:bg-white/5" />

  const isPro = status?.plan === 'Pro'

  return (
    <div className="page-card p-4 md:p-6 w-full space-y-4">
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-sm text-gray-500 dark:text-slate-400">{t('subscription.planLabel')}</span>
          <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
              isPro ? 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400' : 'bg-gray-100 text-gray-600 dark:bg-white/5 dark:text-slate-400'
            }`}
          >
            {status?.plan ?? t('subscription.free')}
          </span>
        </div>
        {isPro && (
          <>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-slate-400">{t('subscription.statusLabel')}</span>
              <span className="text-sm font-medium text-green-600">{t('subscription.active')}</span>
            </div>
            {status?.expiresAt && (
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-500 dark:text-slate-400">{t('subscription.renewsLabel')}</span>
                <span className="text-sm text-gray-700">
                  {new Date(status.expiresAt).toLocaleDateString()}
                </span>
              </div>
            )}
          </>
        )}
      </div>
      <div className="border-t pt-4">
        {isPro ? (
          <button
            onClick={handleManage}
            disabled={portalPending}
            className="w-full rounded-md border py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 dark:border-white/10 dark:text-slate-300 dark:hover:bg-white/5"
          >
            {portalPending ? t('common.loading') : t('subscription.manageSubscription')}
          </button>
        ) : (
          <UpgradeButton className="w-full rounded-md bg-blue-600 py-2 text-sm font-medium text-white disabled:opacity-50" />
        )}
      </div>
    </div>
  )
}
