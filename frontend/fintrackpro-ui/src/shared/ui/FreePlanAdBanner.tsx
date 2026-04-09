import { useNavigate } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useSubscriptionStatus } from '@/entities/subscription'

export function FreePlanAdBanner() {
  const { t } = useTranslation()
  const { data: status } = useSubscriptionStatus()
  const navigate = useNavigate()

  if (!status || status.plan === 'Pro') return null

  return (
    <div className="w-full bg-gradient-to-r from-blue-600 to-indigo-600 px-4 py-3">
      <div className="mx-auto flex max-w-5xl flex-col items-center justify-between gap-2 sm:flex-row">
        <p className="text-sm text-white">
          <span className="font-medium">✦ {t('freePlanBanner.title')}</span>{' '}
          {t('freePlanBanner.description')}
        </p>
        <button
          onClick={() => navigate('/pricing')}
          className="shrink-0 rounded-md bg-white px-3 py-1.5 text-xs font-semibold text-blue-600 transition-colors hover:bg-blue-50"
        >
          {t('freePlanBanner.cta')}
        </button>
      </div>
    </div>
  )
}
