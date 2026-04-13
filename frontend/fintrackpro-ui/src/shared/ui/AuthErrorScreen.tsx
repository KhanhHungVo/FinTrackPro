import { useEffect, useRef, useState } from 'react'
import { cn } from '@/shared/lib/cn'

export type AuthErrorType = 'network' | 'timeout' | 'config' | 'unknown'

export interface AuthErrorScreenProps {
  errorType: AuthErrorType
  onRetry: () => void
  retryCount: number
  maxRetries?: number
  autoRetrySeconds?: number
}

const ERROR_CONTENT = {
  network: {
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-8 w-8 text-orange-500"
        aria-hidden="true"
      >
        <line x1="1" y1="1" x2="23" y2="23" />
        <path d="M16.72 11.06A10.94 10.94 0 0 1 19 12.55" />
        <path d="M5 12.55a10.94 10.94 0 0 1 5.17-2.39" />
        <path d="M10.71 5.05A16 16 0 0 1 22.56 9" />
        <path d="M1.42 9a15.91 15.91 0 0 1 4.7-2.88" />
        <path d="M8.53 16.11a6 6 0 0 1 6.95 0" />
        <line x1="12" y1="20" x2="12.01" y2="20" />
      </svg>
    ),
    title: 'Login service temporarily unreachable',
    subtitle:
      'The login service is temporarily unreachable. This is usually a temporary issue on our end — not your network.',
    badgeLabel: 'Service Unavailable',
    badgeClass: 'bg-orange-100 text-orange-700 dark:bg-orange-500/15 dark:text-orange-400',
    iconBg: 'bg-orange-50 dark:bg-orange-500/10',
  },
  timeout: {
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-8 w-8 text-yellow-500"
        aria-hidden="true"
      >
        <circle cx="12" cy="12" r="10" />
        <polyline points="12 6 12 12 16 14" />
      </svg>
    ),
    title: 'Login service is taking too long',
    subtitle:
      'The login service is taking longer than expected. Please try again in a moment.',
    badgeLabel: 'Service Unavailable',
    badgeClass: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-500/15 dark:text-yellow-400',
    iconBg: 'bg-yellow-50 dark:bg-yellow-500/10',
  },
  config: {
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-8 w-8 text-red-500"
        aria-hidden="true"
      >
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
        <line x1="12" y1="8" x2="12" y2="12" />
        <line x1="12" y1="16" x2="12.01" y2="16" />
      </svg>
    ),
    title: 'Login service temporarily unavailable',
    subtitle:
      "We couldn't connect to the authentication provider. This is usually a temporary issue.",
    badgeLabel: 'Service Unavailable',
    badgeClass: 'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400',
    iconBg: 'bg-red-50 dark:bg-red-500/10',
  },
  unknown: {
    icon: (
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-8 w-8 text-slate-500"
        aria-hidden="true"
      >
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
        <line x1="12" y1="8" x2="12" y2="12" />
        <line x1="12" y1="16" x2="12.01" y2="16" />
      </svg>
    ),
    title: 'Login service temporarily unavailable',
    subtitle:
      "We couldn't connect to the authentication provider. This is usually a temporary issue. Please try again in a few moments.",
    badgeLabel: 'Service Unavailable',
    badgeClass: 'bg-slate-100 text-slate-600 dark:bg-white/5 dark:text-slate-400',
    iconBg: 'bg-slate-50 dark:bg-white/5',
  },
} as const

export function AuthErrorScreen({
  errorType,
  onRetry,
  retryCount,
  maxRetries = 3,
  autoRetrySeconds = 30,
}: AuthErrorScreenProps) {
  const content = ERROR_CONTENT[errorType]
  const isExhausted = retryCount >= maxRetries
  const [secondsLeft, setSecondsLeft] = useState(autoRetrySeconds)
  const [isRetrying, setIsRetrying] = useState(false)
  const h1Ref = useRef<HTMLHeadingElement>(null)

  // Focus the heading on mount for screen readers
  useEffect(() => {
    h1Ref.current?.focus()
  }, [])

  // Reset and run countdown on each new attempt
  useEffect(() => {
    if (isExhausted) return

    setSecondsLeft(autoRetrySeconds)

    const interval = setInterval(() => {
      setSecondsLeft((s) => {
        if (s <= 1) {
          clearInterval(interval)
          setIsRetrying(true)
          onRetry()
          return 0
        }
        return s - 1
      })
    }, 1000)

    return () => clearInterval(interval)
  }, [retryCount, isExhausted, autoRetrySeconds, onRetry])

  // Reset isRetrying when a new error arrives (retryCount changes)
  useEffect(() => {
    setIsRetrying(false)
  }, [retryCount])

  function handleManualRetry() {
    setIsRetrying(true)
    setSecondsLeft(0)
    onRetry()
  }

  return (
    <div
      role="alert"
      aria-live="assertive"
      aria-atomic="true"
      className="flex min-h-screen items-center justify-center bg-gray-50 dark:bg-[#0f1117] px-4"
    >
      <div className="w-full max-w-md rounded-2xl border border-gray-200 bg-white p-8 shadow-sm dark:bg-[#161a25] dark:border-white/6">
        {/* Brand strip */}
        <div className="mb-6 text-center">
          <span className="text-lg font-bold tracking-tight text-gray-900 dark:text-white">
            FinTrackPro
          </span>
        </div>

        {/* Icon circle */}
        <div className="mb-4 flex justify-center">
          <div className={cn('flex h-16 w-16 items-center justify-center rounded-full', content.iconBg)}>
            {content.icon}
          </div>
        </div>

        {/* Error badge */}
        <div className="mb-3 flex justify-center">
          <span
            className={cn(
              'rounded-full px-3 py-1 text-xs font-medium',
              content.badgeClass,
            )}
          >
            {content.badgeLabel}
          </span>
        </div>

        {/* Title */}
        <h1
          ref={h1Ref}
          tabIndex={-1}
          className="mb-2 text-center text-xl font-semibold text-gray-900 dark:text-white focus:outline-none"
        >
          {content.title}
        </h1>

        {/* Subtitle */}
        <p className="mb-6 text-center text-sm leading-relaxed text-gray-500 dark:text-slate-400">
          {content.subtitle}
        </p>

        {/* Retry button */}
        <button
          onClick={handleManualRetry}
          disabled={isExhausted || isRetrying}
          aria-busy={isRetrying}
          aria-label={
            isRetrying
              ? 'Trying to reconnect…'
              : isExhausted
                ? 'Maximum attempts reached'
                : 'Try again'
          }
          className={cn(
            'w-full rounded-md py-2.5 text-sm font-medium transition-colors',
            isExhausted || isRetrying
              ? 'cursor-not-allowed bg-gray-100 text-gray-400 dark:bg-white/5 dark:text-slate-500'
              : 'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800',
          )}
        >
          {isRetrying ? 'Trying…' : 'Try again'}
        </button>

        {/* Countdown pill */}
        {!isExhausted && (
          <p
            aria-live="polite"
            className="mt-2 text-center text-xs text-gray-400 dark:text-slate-500"
          >
            {isRetrying
              ? 'Connecting to login service…'
              : `Reconnecting to login service… retrying in ${secondsLeft}s`}
          </p>
        )}

        {/* Exhausted amber block */}
        {isExhausted && (
          <div className="mt-4 rounded-md bg-amber-50 px-4 py-3 text-sm text-amber-800 dark:bg-amber-500/10 dark:text-amber-300">
            <p className="font-medium">Still having trouble?</p>
            <p className="mt-1 text-amber-700 dark:text-amber-400">
              Please try again later or{' '}
              <a href="mailto:support@fintrackpro.dev" className="underline">
                contact support
              </a>
              .
            </p>
          </div>
        )}

      </div>
    </div>
  )
}

export function AuthLoadingSplash() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-gray-50 dark:bg-[#0f1117]">
      <span className="text-lg font-bold tracking-tight text-gray-900 dark:text-white">
        FinTrackPro
      </span>
      <div className="h-1 w-48 animate-pulse rounded-full bg-blue-200" />
      <p className="text-sm text-gray-400 dark:text-slate-500">Signing you in…</p>
    </div>
  )
}
