import { useEffect, useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { cn } from '@/shared/lib/cn'

export interface AuthDegradedBannerProps {
  tokenExpiresAt: Date
}

function formatCountdown(ms: number): string {
  if (ms <= 0) return '0m 0s'
  const totalSeconds = Math.floor(ms / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}m ${seconds}s`
}

export function AuthDegradedBanner({ tokenExpiresAt }: AuthDegradedBannerProps) {
  const [remaining, setRemaining] = useState(() =>
    tokenExpiresAt.getTime() - Date.now(),
  )

  useEffect(() => {
    const interval = setInterval(() => {
      const ms = tokenExpiresAt.getTime() - Date.now()
      if (ms <= 0) {
        clearInterval(interval)
        window.location.reload()
        return
      }
      setRemaining(ms)
    }, 1000)

    return () => clearInterval(interval)
  }, [tokenExpiresAt])

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        'fixed top-0 left-0 right-0 z-50 w-full',
        'bg-amber-600 px-4 py-2',
        'flex items-center justify-center gap-2',
      )}
    >
      <AlertTriangle size={16} className="shrink-0 text-white" aria-hidden="true" />

      <p className="text-sm font-medium text-white">
        Authentication server is offline. You&apos;re using a cached session
        —&nbsp;expires in{' '}
        <span className="font-bold">{formatCountdown(remaining)}</span>.
      </p>
    </div>
  )
}
