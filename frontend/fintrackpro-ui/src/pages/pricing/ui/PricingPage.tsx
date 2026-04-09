import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { useSubscriptionStatus, useCreatePortalSession } from '@/entities/subscription'
import { UpgradeButton, useBankTransferStore } from '@/features/upgrade'

const FREE_FEATURES = [
  '50 transactions / month',
  '90-day history',
  '3 active budgets',
  '20 trades stored',
  '3 watchlist symbols',
  '7-day signal history',
  '✗  Telegram alerts',
  '✗  Ad-free dashboard',
]

const PRO_FEATURES = [
  'Unlimited transactions',
  'Full history',
  'Unlimited budgets',
  'Unlimited trades',
  'Unlimited watchlist',
  'Full signal history',
  '✓  Telegram alerts',
  '✓  Ad-free dashboard',
]

export function PricingPage() {
  const { data: status } = useSubscriptionStatus()
  const { mutate: createPortal, isPending: portalPending } = useCreatePortalSession()
  const navigate = useNavigate()
  const isPro = status?.plan === 'Pro'
  const openBankTransfer = useBankTransferStore((s) => s.openModal)

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

  return (
    <div className="mx-auto max-w-4xl space-y-8 p-4 md:p-8">
      <div className="space-y-2 text-center">
        <h1 className="text-3xl font-bold text-gray-900">Simple, transparent pricing</h1>
        <p className="text-gray-500">Start free. Upgrade when you need more.</p>
      </div>

      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        {/* Free card */}
        <div
          className={`space-y-6 rounded-xl border p-6 ${
            !isPro ? 'ring-2 ring-blue-500' : ''
          }`}
        >
          <div>
            <h2 className="text-xl font-bold text-gray-900">Free</h2>
            <p className="mt-2 text-3xl font-bold">
              $0<span className="text-base font-normal text-gray-500">/mo</span>
            </p>
          </div>
          <ul className="space-y-2">
            {FREE_FEATURES.map((f) => (
              <li key={f} className="text-sm text-gray-600">
                {f}
              </li>
            ))}
          </ul>
          <button
            disabled
            className="w-full cursor-not-allowed rounded-md border py-2 text-sm font-medium text-gray-400"
          >
            {!isPro ? 'Current plan' : 'Downgrade'}
          </button>
        </div>

        {/* Pro card */}
        <div
          className={`space-y-6 rounded-xl border p-6 ${
            isPro ? 'ring-2 ring-blue-500' : ''
          }`}
        >
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-bold text-gray-900">Pro</h2>
              <span className="rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-700">
                Popular
              </span>
            </div>
            <p className="mt-2 text-3xl font-bold">
              $9<span className="text-base font-normal text-gray-500">/mo</span>
            </p>
          </div>
          <ul className="space-y-2">
            {PRO_FEATURES.map((f) => (
              <li key={f} className="text-sm text-gray-600">
                {f}
              </li>
            ))}
          </ul>
          {isPro ? (
            <button
              onClick={handleManage}
              disabled={portalPending}
              className="w-full rounded-md border border-blue-500 py-2 text-sm font-medium text-blue-600 hover:bg-blue-50 disabled:opacity-50"
            >
              {portalPending ? 'Loading…' : 'Manage subscription'}
            </button>
          ) : (
            <div className="flex flex-col items-center gap-2">
              <UpgradeButton className="w-full rounded-md bg-blue-600 py-2 text-sm font-medium text-white disabled:opacity-50" />
              <button
                onClick={openBankTransfer}
                className="text-sm text-gray-500 hover:text-gray-700 underline"
              >
                or pay via bank transfer →
              </button>
            </div>
          )}
        </div>
      </div>

      <div className="text-center">
        <button
          onClick={() => navigate(-1)}
          className="text-sm text-gray-500 hover:text-gray-700"
        >
          ← Back
        </button>
      </div>
    </div>
  )
}
