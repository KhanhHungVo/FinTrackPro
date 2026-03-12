import { useState } from 'react'
import { useBudgets } from '@/entities/budget'
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
  const { data: budgets, isLoading } = useBudgets(month)
  const { data: transactions } = useTransactions(month)
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
