import { useState } from 'react'
import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import {
  useNotificationPreference,
  useSaveNotificationPreference,
} from '@/entities/notification-preference'
import { useSubscriptionStatus } from '@/entities/subscription'
import { UpgradeButton } from '@/features/upgrade'
import { classifyApiError, errorToastMessage } from '@/shared/lib/apiError'
import { cn } from '@/shared/lib/cn'
import { Button } from '@/shared/ui'

const notifSchema = z.object({
  telegramChatId: z.string()
    .min(1, 'Telegram chat ID is required')
    .max(100, 'Chat ID must be 100 characters or fewer'),
})

interface FormProps {
  initialChatId: string
  initialEnabled: boolean
  isFreePlan: boolean
}

function NotificationSettingsFormInner({ initialChatId, initialEnabled, isFreePlan }: FormProps) {
  const { t } = useTranslation()
  const { mutate, isPending } = useSaveNotificationPreference()

  const [chatId, setChatId] = useState(initialChatId)
  const [enabled, setEnabled] = useState(initialEnabled)
  const [chatIdError, setChatIdError] = useState<string | null>(null)

  function validateChatId(): boolean {
    const result = notifSchema.shape.telegramChatId.safeParse(chatId)
    if (!result.success) {
      setChatIdError(result.error.issues[0].message)
      return false
    }
    setChatIdError(null)
    return true
  }

  const handleSubmit = (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault()
    if (!validateChatId()) return

    mutate(
      { telegramChatId: chatId, isEnabled: enabled },
      {
        onSuccess: () => toast.success(t('notifications.saved')),
        onError: (err) => {
          const kind = classifyApiError(err)
          if (kind.type === 'validation') {
            const fieldMsg = kind.details.errors?.['TelegramChatId']?.[0]
            setChatIdError(fieldMsg ?? kind.details.title ?? 'Invalid chat ID')
          } else {
            toast.error(errorToastMessage(err))
          }
        },
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
            onChange={(e) => { setChatId(e.target.value); if (chatIdError) setChatIdError(null) }}
            onBlur={validateChatId}
            className={cn(
              'w-full rounded-md border px-3 py-2 text-sm font-mono outline-none focus:border-transparent focus:ring-2 focus:ring-blue-500 dark:bg-slate-800 dark:text-white',
              chatIdError ? 'border-red-400 dark:border-red-500' : 'border-gray-300 dark:border-white/12',
            )}
          />
          {chatIdError && (
            <p className="mt-1 text-xs text-red-600 dark:text-red-400">{chatIdError}</p>
          )}
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

        <Button type="submit" variant="primary" size="lg" loading={isPending} className="w-full">
          {t('notifications.save')}
        </Button>
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

export function NotificationSettingsForm() {
  const { data: pref, isLoading } = useNotificationPreference()
  const { data: subscription } = useSubscriptionStatus()

  if (isLoading) return <div className="animate-pulse h-32 rounded-lg bg-gray-100 dark:bg-white/5" />

  const isFreePlan = !subscription || subscription.plan === 'Free'

  return (
    <NotificationSettingsFormInner
      key={pref ? 'loaded' : 'empty'}
      initialChatId={pref?.telegramChatId ?? ''}
      initialEnabled={pref?.isEnabled ?? true}
      isFreePlan={isFreePlan}
    />
  )
}
