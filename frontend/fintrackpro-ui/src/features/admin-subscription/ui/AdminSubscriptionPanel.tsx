import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  useAdminUsers,
  useAdminActivateSubscription,
  useAdminRevokeSubscription,
} from '@/entities/subscription'
import type { AdminUser } from '@/entities/subscription'
import { cn } from '@/shared/lib/cn'
import { XCircle } from 'lucide-react'
import { Button } from '@/shared/ui'

function PlanPill({ plan }: { plan: string }) {
  const isPro = plan === 'Pro'
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium',
        isPro
          ? 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400'
          : 'bg-gray-100 text-gray-600 dark:bg-white/5 dark:text-slate-400',
      )}
    >
      {plan}
    </span>
  )
}

function UserRow({ user }: { user: AdminUser }) {
  const { t } = useTranslation()
  const [revokeConfirm, setRevokeConfirm] = useState(false)
  const activate = useAdminActivateSubscription()
  const revoke = useAdminRevokeSubscription()

  const expires = user.subscriptionExpiresAt
    ? new Date(user.subscriptionExpiresAt).toLocaleDateString()
    : '—'

  function handleActivate(period: 'Monthly' | 'Yearly') {
    activate.mutate({ userId: user.id, period })
  }

  function handleRevoke() {
    if (!revokeConfirm) { setRevokeConfirm(true); return }
    revoke.mutate(user.id, { onSettled: () => setRevokeConfirm(false) })
  }

  const busy = activate.isPending || revoke.isPending

  return (
    <tr className="border-b border-gray-100 dark:border-white/5 last:border-b-0 hover:bg-gray-50 dark:hover:bg-white/3 transition-colors">
      <td className="px-4 py-2.5 text-sm text-gray-700 dark:text-slate-300 max-w-[180px] truncate">
        {user.email ?? '—'}
      </td>
      <td className="px-4 py-2.5">
        <PlanPill plan={user.plan} />
      </td>
      <td className="px-4 py-2.5 text-sm font-mono text-gray-500 dark:text-slate-400 whitespace-nowrap">
        {expires}
      </td>
      <td className="px-4 py-2.5">
        <div className="flex items-center gap-1.5 justify-end">
          <Button variant="primary-soft" size="sm" disabled={busy} onClick={() => handleActivate('Monthly')}>
            +1m
          </Button>
          <Button variant="primary-soft" size="sm" disabled={busy} onClick={() => handleActivate('Yearly')}>
            +1y
          </Button>
          {user.plan === 'Pro' && (
            revokeConfirm ? (
              <>
                <Button variant="danger" size="sm" disabled={busy} onClick={handleRevoke}>
                  {t('common.confirmRevoke')}
                </Button>
                <Button variant="ghost" size="sm" disabled={busy} onClick={() => setRevokeConfirm(false)}>
                  {t('common.cancel')}
                </Button>
              </>
            ) : (
              <Button variant="danger-ghost" size="sm" disabled={busy} onClick={() => setRevokeConfirm(true)}>
                <XCircle size={12} aria-hidden="true" />
                {t('common.revoke')}
              </Button>
            )
          )}
        </div>
      </td>
    </tr>
  )
}

export function AdminSubscriptionPanel() {
  const [page, setPage] = useState(1)
  const [emailInput, setEmailInput] = useState('')
  const [emailFilter, setEmailFilter] = useState('')

  const { data, isLoading } = useAdminUsers(page, emailFilter || undefined)

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    setPage(1)
    setEmailFilter(emailInput)
  }

  return (
    <div className="space-y-4">
      <form onSubmit={handleSearch} className="flex gap-2">
        <input
          type="text"
          value={emailInput}
          onChange={(e) => setEmailInput(e.target.value)}
          placeholder="Search by email..."
          className="flex-1 rounded-md border px-3 py-1.5 text-sm bg-white dark:bg-white/5 dark:border-white/10 dark:text-slate-200 dark:placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <Button type="submit" variant="primary" size="sm">
          Search
        </Button>
      </form>

      <div className="rounded-xl border bg-white dark:bg-white/4 dark:border-white/6 overflow-hidden">
        <table className="w-full text-left">
          <thead>
            <tr className="border-b border-gray-100 dark:border-white/6 bg-gray-50 dark:bg-white/3">
              <th className="px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-gray-500 dark:text-slate-400">User</th>
              <th className="px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-gray-500 dark:text-slate-400">Plan</th>
              <th className="px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-gray-500 dark:text-slate-400">Expires</th>
              <th className="px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-gray-500 dark:text-slate-400 text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-sm text-gray-400 dark:text-slate-500">
                  Loading…
                </td>
              </tr>
            ) : data?.items.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-sm text-gray-400 dark:text-slate-500">
                  No users found.
                </td>
              </tr>
            ) : (
              data?.items.map((user) => <UserRow key={user.id} user={user} />)
            )}
          </tbody>
        </table>
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-gray-500 dark:text-slate-400">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={!data.hasPreviousPage}
            className="rounded px-3 py-1 hover:bg-gray-100 dark:hover:bg-white/5 disabled:opacity-40 transition-colors"
          >
            ← Prev
          </button>
          <span>Page {data.page} of {data.totalPages}</span>
          <button
            onClick={() => setPage((p) => p + 1)}
            disabled={!data.hasNextPage}
            className="rounded px-3 py-1 hover:bg-gray-100 dark:hover:bg-white/5 disabled:opacity-40 transition-colors"
          >
            Next →
          </button>
        </div>
      )}
    </div>
  )
}
