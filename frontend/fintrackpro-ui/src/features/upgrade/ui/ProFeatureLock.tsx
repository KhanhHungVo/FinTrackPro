import { useSubscriptionStatus } from '@/entities/subscription'
import { UpgradeButton } from './UpgradeButton'

interface ProFeatureLockProps {
  title: string
  tagline?: string
  features?: { icon: string; label: string }[]
  compact?: boolean
  children: React.ReactNode
}

function LockIcon() {
  return (
    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" aria-hidden="true" className="text-amber-400">
      <rect x="3" y="11" width="18" height="11" rx="2" stroke="currentColor" strokeWidth="1.5" />
      <path d="M7 11V7a5 5 0 0 1 10 0v4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}

function LockOverlay({ title }: { title: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 px-6 py-8">
      <div className="flex items-center gap-2">
        <LockIcon />
        <span className="inline-flex items-center rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-semibold text-amber-700 dark:bg-amber-500/15 dark:text-amber-400">
          Pro
        </span>
      </div>
      <h3 className="text-sm font-semibold text-gray-900 dark:text-white text-center">{title}</h3>
      <UpgradeButton />
    </div>
  )
}

export function ProFeatureLock({ title, tagline, compact = false, children }: ProFeatureLockProps) {
  const { data: status } = useSubscriptionStatus()

  // status undefined = query in-flight; treat same as free so children never mount
  // and no spurious requests fire before we know the plan.
  const isPro = status?.plan === 'Pro' && status?.isActive !== false

  if (isPro) return <>{children}</>

  if (compact) {
    return (
      <div className="glass-card flex items-center gap-3 px-4 py-3">
        <LockIcon />
        <span className="flex-1 text-sm font-medium text-gray-700 dark:text-slate-300">
          {title}
          <span className="ml-2 inline-flex items-center rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-700 dark:bg-amber-500/15 dark:text-amber-400">Pro</span>
        </span>
        {tagline && <span className="sr-only">{tagline}</span>}
        <UpgradeButton className="shrink-0 rounded-md bg-blue-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-blue-700 disabled:opacity-50 transition-colors" />
      </div>
    )
  }

  return (
    <div className="glass-card overflow-hidden">
      <LockOverlay title={title} />
    </div>
  )
}
