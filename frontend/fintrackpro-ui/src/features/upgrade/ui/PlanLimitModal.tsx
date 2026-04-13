import { useTranslation } from 'react-i18next'
import { usePlanLimitStore } from '../model/planLimitStore'
import { UpgradeButton } from './UpgradeButton'

export function PlanLimitModal() {
  const { t } = useTranslation()
  const open = usePlanLimitStore((s) => s.open)
  const title = usePlanLimitStore((s) => s.title)
  const clear = usePlanLimitStore((s) => s.clear)

  if (!open) return null

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/50" onClick={clear} />
      <div className="fixed left-1/2 top-1/2 z-50 w-full max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-white p-6 shadow-xl dark:bg-[#161a25] dark:border-white/10">
        <div className="flex items-start justify-between">
          <h2 className="text-base font-semibold text-gray-900 dark:text-white">{title}</h2>
          <button
            onClick={clear}
            className="ml-4 text-gray-400 hover:text-gray-600 dark:text-slate-500 dark:hover:text-slate-300"
            aria-label={t('donation.dismiss')}
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path
                d="M2 2L14 14M14 2L2 14"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          </button>
        </div>
        <p className="mt-2 text-sm text-gray-600 dark:text-slate-400">
          {t('planLimitModal.description')}
        </p>
        <div className="mt-5 flex flex-col gap-2">
          <UpgradeButton className="w-full rounded-md bg-blue-600 py-2 text-sm font-medium text-white disabled:opacity-50" />
          <button
            onClick={clear}
            className="w-full rounded-md border py-2 text-sm text-gray-700 hover:bg-gray-50 dark:border-white/10 dark:text-slate-300 dark:hover:bg-white/5"
          >
            {t('stripeUnavailable.maybeLater')}
          </button>
        </div>
      </div>
    </>
  )
}
