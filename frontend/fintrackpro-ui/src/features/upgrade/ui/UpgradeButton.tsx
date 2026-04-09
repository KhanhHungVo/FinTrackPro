import { toast } from 'sonner'
import { useCreateCheckoutSession } from '@/entities/subscription'

interface Props {
  className?: string
}

export function UpgradeButton({ className }: Props) {
  const { mutate, isPending } = useCreateCheckoutSession()

  function handleUpgrade() {
    mutate(
      {
        successUrl: `${window.location.origin}/settings?subscribed=1`,
        cancelUrl: `${window.location.origin}/pricing`,
      },
      {
        onSuccess: ({ sessionUrl }) => {
          window.location.href = sessionUrl
        },
        onError: () => {
          toast.error('Failed to start checkout. Please try again.')
        },
      },
    )
  }

  return (
    <button
      onClick={handleUpgrade}
      disabled={isPending}
      className={
        className ??
        'rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-50'
      }
    >
      {isPending ? 'Loading…' : 'Upgrade to Pro'}
    </button>
  )
}
