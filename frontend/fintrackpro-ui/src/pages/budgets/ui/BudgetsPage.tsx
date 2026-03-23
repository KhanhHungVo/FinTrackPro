import { useState } from 'react'
import { useBudgets, useDeleteBudget, useUpdateBudget } from '@/entities/budget'
import { useTransactions } from '@/entities/transaction'
import { AddBudgetForm } from '@/features/add-budget'
import { cn } from '@/shared/lib/cn'

function monthsBack(n: number): string {
  const d = new Date()
  d.setMonth(d.getMonth() - n)
  return d.toISOString().slice(0, 7)
}

export function BudgetsPage() {
  const [month, setMonth] = useState(monthsBack(0))
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editLimit, setEditLimit] = useState('')
  const { data: budgets, isLoading } = useBudgets(month)
  const { data: transactions } = useTransactions(month)
  const { mutate: deleteBudget, isPending: isDeleting, variables: deletingId } = useDeleteBudget()
  const { mutate: updateBudget, isPending: isSaving } = useUpdateBudget()

  function startEdit(id: string, current: number) {
    setEditingId(id)
    setEditLimit(String(current))
  }

  function commitEdit(id: string) {
    const val = parseFloat(editLimit)
    if (!isNaN(val) && val > 0) {
      updateBudget({ id, limitAmount: val }, { onSuccess: () => setEditingId(null) })
    } else {
      setEditingId(null)
    }
  }
  const monthOptions = Array.from({ length: 6 }, (_, i) => monthsBack(i))

  // Calculate spending per category from transactions
  const spentByCategory: Record<string, number> = {}
  transactions
    ?.filter((t) => t.type === 'Expense')
    .forEach((t) => {
      spentByCategory[t.category] = (spentByCategory[t.category] ?? 0) + t.amount
    })

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Budgets</h1>
        <select
          value={month}
          onChange={(e) => setMonth(e.target.value)}
          className="rounded-md border px-3 py-1.5 text-sm"
        >
          {monthOptions.map((m) => (
            <option key={m} value={m}>{m}</option>
          ))}
        </select>
      </div>

      <AddBudgetForm month={month} />

      {isLoading ? (
        <div className="space-y-2">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="animate-pulse h-20 rounded-lg bg-gray-100" />
          ))}
        </div>
      ) : budgets?.length === 0 ? (
        <p className="text-center text-sm text-gray-400 py-8">
          No budgets set for {month}.
        </p>
      ) : (
        <ul className="space-y-3">
          {budgets?.map((budget) => {
            const spent = spentByCategory[budget.category] ?? 0
            const pct = Math.min((spent / budget.limitAmount) * 100, 100)
            const overrun = spent > budget.limitAmount

            return (
              <li key={budget.id} className="rounded-lg border p-4 space-y-2">
                <div className="flex items-center justify-between">
                  <p className="font-medium">{budget.category}</p>
                  <div className="flex items-center gap-2">
                    {editingId === budget.id ? (
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={editLimit}
                        onChange={(e) => setEditLimit(e.target.value)}
                        onBlur={() => commitEdit(budget.id)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') commitEdit(budget.id)
                          if (e.key === 'Escape') setEditingId(null)
                        }}
                        disabled={isSaving}
                        className="w-24 rounded border px-2 py-0.5 text-sm text-right focus:outline-none focus:ring-1 focus:ring-blue-400"
                        autoFocus
                      />
                    ) : (
                      <span
                        className={cn(
                          'text-sm font-semibold',
                          overrun ? 'text-red-600' : 'text-gray-700',
                        )}
                      >
                        ${spent.toFixed(2)}
                        <span className="font-normal text-gray-400">
                          {' '}/ ${budget.limitAmount.toFixed(2)}
                        </span>
                      </span>
                    )}
                    <button
                      onClick={() => startEdit(budget.id, budget.limitAmount)}
                      className="text-gray-400 hover:text-blue-500 transition-colors text-sm"
                      aria-label="Edit limit"
                      title="Edit limit"
                    >
                      ✎
                    </button>
                    <button
                      onClick={() => deleteBudget(budget.id)}
                      disabled={isDeleting && deletingId === budget.id}
                      className="text-gray-400 hover:text-red-500 transition-colors disabled:opacity-50"
                      aria-label="Delete budget"
                      title="Delete budget"
                    >
                      ×
                    </button>
                  </div>
                </div>

                {/* Progress bar */}
                <div className="h-2 w-full rounded-full bg-gray-100">
                  <div
                    className={cn(
                      'h-2 rounded-full transition-all',
                      overrun ? 'bg-red-500' : pct > 80 ? 'bg-yellow-400' : 'bg-green-500',
                    )}
                    style={{ width: `${pct}%` }}
                  />
                </div>

                {overrun && (
                  <p className="text-xs text-red-500">
                    Over budget by ${(spent - budget.limitAmount).toFixed(2)}
                  </p>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
