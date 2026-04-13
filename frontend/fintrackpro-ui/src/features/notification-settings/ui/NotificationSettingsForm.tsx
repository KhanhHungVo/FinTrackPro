import { useState, useEffect } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import {
  useNotificationPreference,
  useSaveNotificationPreference,
} from '@/entities/notification-preference'
import { useSubscriptionStatus } from '@/entities/subscription'
import { UpgradeButton } from '@/features/upgrade'
import { errorToastMessage } from '@/shared/lib/apiError'

export function NotificationSettingsForm() {
  const { t } = useTranslation()
  const { data: pref, isLoading } = useNotificationPreference()
  const { data: subscription } = useSubscriptionStatus()
  const { mutate, isPending } = useSaveNotificationPreference()

  const [chatId, setChatId] = useState('')
  const [enabled, setEnabled] = useState(true)

  useEffect(() => {
    if (pref) {
      setChatId(pref.telegramChatId ?? '')
      setEnabled(pref.isEnabled)
    }
  }, [pref])

  if (isLoading) return <div className="animate-pulse h-32 rounded-lg bg-gray-100 dark:bg-white/5" />

  const isFreePlan = !subscription || subscription.plan === 'Free'

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate(
      { telegramChatId: chatId, isEnabled: enabled },
      {
        onSuccess: () => toast.success(t('notifications.saved')),
        onError: (err) => toast.error(errorToastMessage(err)),
      },
    )
  }

  return (
    <div className="relative w-full">
      <form
        onSubmit={handleSubmit}
        className={`page-card space-y-4 p-4 md:p-6 ${isFreePlan ? 'pointer-events-none select-none opacity-50' : ''}`}
        aria-hidden={isFreePlan}
      >

        <div className="rounded-md bg-blue-50 border border-blue-200 p-3 text-sm text-blue-800 space-y-1 dark:bg-blue-500/10 dark:border-blue-500/20 dark:text-blue-300">
          <p className="font-medium">{t('notifications.setupSteps')}</p>
          <ol className="list-decimal list-inside space-y-0.5">
            <li>{t('notifications.setupStep1')}</li>
            <li dangerouslySetInnerHTML={{ __html: t('notifications.setupStep2').replace('/start', '<code>/start</code>') }} />
            <li>{t('notifications.setupStep3')}</li>
          </ol>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">{t('notifications.telegramChatId')}</label>
          <input
            type="text"
            placeholder={t('notifications.chatIdPlaceholder')}
            value={chatId}
            onChange={(e) => setChatId(e.target.value)}
            required
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm font-mono outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:border-white/12 dark:text-white"
          />
        </div>

        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={enabled}
            onChange={(e) => setEnabled(e.target.checked)}
            className="h-4 w-4 rounded"
          />
          <span className="text-sm">{t('notifications.enableNotifications')}</span>
        </label>

        <button
          type="submit"
          disabled={isPending}
          className="w-full rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
        >
          {isPending ? t('common.loading') : t('notifications.save')}
        </button>
      </form>

      {isFreePlan && (
        <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 rounded-lg bg-white/80 dark:bg-[#0f1117]/80 p-6 backdrop-blur-sm">
          <p className="text-sm font-medium text-gray-700 dark:text-slate-300 text-center">
            {t('notifications.proFeatureNotice')}
          </p>
          <UpgradeButton />
        </div>
      )}
    </div>
  )
}
