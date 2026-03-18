import { useEffect, useState } from 'react'
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
      {/* Warning icon */}
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={2}
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-4 w-4 shrink-0 text-white"
        aria-hidden="true"
      >
        <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
        <line x1="12" y1="9" x2="12" y2="13" />
        <line x1="12" y1="17" x2="12.01" y2="17" />
      </svg>

      <p className="text-sm font-medium text-white">
        Authentication server is offline. You&apos;re using a cached session
        —&nbsp;expires in{' '}
        <span className="font-bold">{formatCountdown(remaining)}</span>.
      </p>
    </div>
  )
}
