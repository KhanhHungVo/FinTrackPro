import { useNavigate } from 'react-router'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useSubscriptionStatus, useCreatePortalSession } from '@/entities/subscription'
import { UpgradeButton, useBankTransferStore } from '@/features/upgrade'

interface Feature {
  label: string
  included: boolean
}

const FREE_FEATURES: Feature[] = [
  { label: '50 transactions / month', included: true },
  { label: '60-day history', included: true },
  { label: '3 active budgets', included: true },
  { label: '20 trades stored', included: true },
  { label: '1 watchlist symbol', included: true },
  { label: '7-day signal history', included: true },
  { label: 'Telegram alerts', included: false },
  { label: 'Ad-free dashboard', included: false },
]

const PRO_FEATURES: Feature[] = [
  { label: '500 transactions / month', included: true },
  { label: '1-year history', included: true },
  { label: '20 active budgets', included: true },
  { label: '200 trades stored', included: true },
  { label: '20 watchlist symbols', included: true },
  { label: '90-day signal history', included: true },
  { label: 'Telegram alerts', included: true },
  { label: 'Ad-free dashboard', included: true },
]

function CheckIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="shrink-0 text-emerald-500"
    >
      <circle cx="8" cy="8" r="7.25" stroke="currentColor" strokeWidth="1.5" />
      <path
        d="M5 8l2 2 4-4"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

function XIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="shrink-0 text-gray-300 dark:text-slate-600"
    >
      <circle cx="8" cy="8" r="7.25" stroke="currentColor" strokeWidth="1.5" />
      <path
        d="M5.5 5.5l5 5M10.5 5.5l-5 5"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  )
}

function FeatureRow({ label, included }: Feature) {
  return (
    <li className="flex items-center gap-2.5">
      {included ? <CheckIcon /> : <XIcon />}
      <span className={`text-sm ${included ? 'text-gray-700 dark:text-slate-300' : 'text-gray-400 dark:text-slate-500'}`}>{label}</span>
    </li>
  )
}

export function PricingPage() {
  const { t } = useTranslation()
  const { data: status } = useSubscriptionStatus()
  const { mutate: createPortal, isPending: portalPending } = useCreatePortalSession()
  const navigate = useNavigate()
  const isPro = status?.plan === 'Pro'
  const openBankTransfer = useBankTransferStore((s) => s.openModal)

  function handleManage() {
    createPortal(
      { returnUrl: `${window.location.origin}/settings?tab=billing` },
      {
        onSuccess: ({ portalUrl }) => {
          window.location.href = portalUrl
        },
        onError: () => toast.error('Failed to open billing portal.'),
      },
    )
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6 p-4 md:p-8">
      {/* Header */}
      <div className="space-y-1.5 text-center">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white sm:text-3xl">
          {t('pricing.title')}
        </h1>
        <p className="text-sm text-gray-500 dark:text-slate-400 sm:text-base">
          {t('pricing.subtitle')}
        </p>
      </div>

      {/* Launch campaign notice */}
      {/* <div className="flex items-start gap-2.5 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 sm:items-center dark:border-amber-500/20 dark:bg-amber-500/10">
        <span className="text-lg leading-none">🎉</span>
        <p className="text-sm text-amber-900 dark:text-amber-300">
          <span className="font-semibold">{t('pricing.launchNotice')}</span>{' '}
          {t('pricing.launchDescription')}
        </p>
      </div> */}

      {/* Plan cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 sm:gap-6">
        {/* Free card */}
        <div
          className={`flex flex-col rounded-xl border bg-white p-5 sm:p-6 dark:bg-white/4 dark:border-white/6 ${
            !isPro ? 'ring-2 ring-blue-500' : ''
          }`}
        >
          <div className="mb-5">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-bold text-gray-900 dark:text-white">{t('subscription.free')}</h2>
              {!isPro && (
                <span className="rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-700 dark:bg-blue-500/15 dark:text-blue-400">
                  {t('pricing.currentPlan')}
                </span>
              )}
            </div>
            <p className="mt-2 text-3xl font-bold text-gray-900 dark:text-white">
              $0
              <span className="text-base font-normal text-gray-400 dark:text-slate-500"> {t('pricing.perMonth')}</span>
            </p>
          </div>

          <ul className="flex-1 space-y-3">
            {FREE_FEATURES.map((f) => (
              <FeatureRow key={f.label} {...f} />
            ))}
          </ul>

          <button
            disabled
            className="mt-6 w-full cursor-not-allowed rounded-lg border py-2.5 text-sm font-medium text-gray-400"
          >
            {!isPro ? t('pricing.currentPlan') : t('pricing.downgrade')}
          </button>
        </div>

        {/* Pro card */}
        <div
          className={`relative flex flex-col rounded-xl border bg-white p-5 sm:p-6 dark:bg-white/4 dark:border-white/6 ${
            isPro ? 'ring-2 ring-blue-500' : ''
          }`}
        >
          <div className="mb-5">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <h2 className="text-lg font-bold text-gray-900 dark:text-white">{t('subscription.pro')}</h2>
                <span className="rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-700 dark:bg-blue-500/15 dark:text-blue-400">
                  {t('pricing.popular')}
                </span>
              </div>
              {isPro && (
                <span className="rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-medium text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400">
                  {t('subscription.active')}
                </span>
              )}
            </div>
            <div className="mt-2 flex items-baseline gap-1.5">
              <p className="text-3xl font-bold text-gray-900 dark:text-white">
                99,000 ₫
                <span className="text-base font-normal text-gray-400 dark:text-slate-500"> {t('pricing.perMonth')}</span>
              </p>
            </div>
            <p className="mt-1 text-xs text-gray-400 dark:text-slate-500">{t('pricing.usdNote')}</p>
          </div>

          <ul className="flex-1 space-y-3">
            {PRO_FEATURES.map((f) => (
              <FeatureRow key={f.label} {...f} />
            ))}
          </ul>

          {isPro ? (
            <button
              onClick={handleManage}
              disabled={portalPending}
              className="mt-6 w-full rounded-lg border border-blue-500 py-2.5 text-sm font-medium text-blue-600 transition-colors hover:bg-blue-50 dark:border-blue-400 dark:text-blue-400 dark:hover:bg-blue-500/10 disabled:opacity-50"
            >
              {portalPending ? t('common.loading') : t('subscription.manageSubscription')}
            </button>
          ) : (
            <div className="mt-6 flex flex-col gap-2">
              <UpgradeButton className="w-full rounded-lg bg-blue-600 py-2.5 text-sm font-medium text-white transition-colors hover:bg-blue-700 disabled:opacity-50 active:scale-[0.98]" />
              <button
                onClick={openBankTransfer}
                className="w-full py-1.5 text-sm text-gray-500 dark:text-slate-400 underline-offset-2 hover:text-gray-700 hover:underline"
              >
                {t('bankTransfer.orPayViaTransfer')}
              </button>
            </div>
          )}
        </div>
      </div>

      <div className="text-center">
        <button
          onClick={() => navigate(-1)}
          className="text-sm text-gray-400 dark:text-slate-400 transition-colors hover:text-gray-600"
        >
          {t('pricing.back')}
        </button>
      </div>
    </div>
  )
}
