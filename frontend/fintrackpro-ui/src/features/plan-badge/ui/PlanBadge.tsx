import { useNavigate } from 'react-router'
import { useSubscriptionStatus } from '@/entities/subscription'

export function PlanBadge() {
  const { data: status } = useSubscriptionStatus()
  const navigate = useNavigate()
  const isPro = status?.plan === 'Pro'

  if (isPro) {
    return (
      <span className="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-700">
        Pro
      </span>
    )
  }

  return (
    <button
      onClick={() => navigate('/pricing')}
      className="inline-flex items-center gap-1 rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-medium text-gray-600 transition-colors hover:bg-gray-200"
    >
      Free
      <svg width="10" height="10" viewBox="0 0 10 10" fill="none" aria-hidden="true">
        <path
          d="M2 5h6M5 2l3 3-3 3"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </button>
  )
}
