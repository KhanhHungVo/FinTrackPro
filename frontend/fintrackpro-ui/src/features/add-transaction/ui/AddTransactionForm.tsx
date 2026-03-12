import { useState } from 'react'
import { useCreateTransaction } from '@/entities/transaction'
import { cn } from '@/shared/lib/cn'

export function AddTransactionForm() {
  const { mutate, isPending } = useCreateTransaction()
  const [type, setType] = useState<'Income' | 'Expense'>('Expense')
  const [amount, setAmount] = useState('')
  const [category, setCategory] = useState('')
  const [note, setNote] = useState('')
  const currentMonth = new Date().toISOString().slice(0, 7)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate({
      type,
      amount: parseFloat(amount),
      category,
      note: note || null,
      budgetMonth: currentMonth,
    })
    setAmount('')
    setCategory('')
    setNote('')
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border p-4">
      <h2 className="text-lg font-semibold">Add Transaction</h2>

      <div className="flex gap-2">
        {(['Income', 'Expense'] as const).map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setType(t)}
            className={cn(
              'flex-1 rounded-md py-2 text-sm font-medium',
              type === t
                ? t === 'Income'
                  ? 'bg-green-600 text-white'
                  : 'bg-red-600 text-white'
                : 'bg-gray-100 text-gray-700',
            )}
          >
            {t}
          </button>
        ))}
      </div>

      <input
        type="number"
        placeholder="Amount"
        value={amount}
        onChange={(e) => setAmount(e.target.value)}
        required
        className="w-full rounded-md border px-3 py-2 text-sm"
      />
      <input
        type="text"
        placeholder="Category"
        value={category}
        onChange={(e) => setCategory(e.target.value)}
        required
        className="w-full rounded-md border px-3 py-2 text-sm"
      />
      <input
        type="text"
        placeholder="Note (optional)"
        value={note}
        onChange={(e) => setNote(e.target.value)}
        className="w-full rounded-md border px-3 py-2 text-sm"
      />

      <button
        type="submit"
        disabled={isPending}
        className="w-full rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
      >
        {isPending ? 'Saving...' : 'Add Transaction'}
      </button>
    </form>
  )
}
