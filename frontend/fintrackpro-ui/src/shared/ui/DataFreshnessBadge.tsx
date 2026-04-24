import { useDataAge } from '@/shared/lib/useDataAge'
import { cn } from '@/shared/lib/cn'

interface Props {
  dataUpdatedAt: number
  onRefetch: () => void
  isFetching: boolean
  label?: string
  cacheTtlSeconds?: number
}

export function DataFreshnessBadge({
  dataUpdatedAt,
  onRefetch,
  isFetching,
  label = 'live',
  cacheTtlSeconds = 60,
}: Props) {
  const secondsAgo = useDataAge(dataUpdatedAt)
  const withinTtl = secondsAgo < cacheTtlSeconds

  const ageText =
    secondsAgo < 60
      ? `${secondsAgo}s ago`
      : `${Math.floor(secondsAgo / 60)}m ${secondsAgo % 60}s ago`

  return (
    <div className="flex items-center gap-1.5">
      <span className="font-mono text-[9px] text-gray-400 dark:text-slate-500 tracking-[0.04em]">
        Updated {ageText}
      </span>

      <button
        type="button"
        onClick={onRefetch}
        disabled={isFetching || withinTtl}
        title={withinTtl ? `Cache refreshes after ${cacheTtlSeconds}s` : 'Refresh now'}
        className={cn(
          'flex items-center justify-center w-4 h-4 rounded transition-opacity duration-[120ms]',
          isFetching || withinTtl
            ? 'opacity-30 cursor-not-allowed'
            : 'opacity-60 hover:opacity-100 cursor-pointer'
        )}
        aria-label="Refresh data"
      >
        <svg
          width="10"
          height="10"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className={cn(
            'text-gray-500 dark:text-slate-400',
            isFetching && 'animate-spin'
          )}
          aria-hidden="true"
        >
          <path d="M21 2v6h-6" />
          <path d="M3 12a9 9 0 0 1 15-6.7L21 8" />
          <path d="M3 22v-6h6" />
          <path d="M21 12a9 9 0 0 1-15 6.7L3 16" />
        </svg>
      </button>

      <span className="font-mono text-[9px] text-gray-500 bg-gray-50 border border-gray-200 rounded px-1.5 py-0.5 tracking-[0.08em] uppercase dark:bg-green-500/10 dark:border-green-500/20 dark:text-green-400">
        {label}
      </span>
    </div>
  )
}
