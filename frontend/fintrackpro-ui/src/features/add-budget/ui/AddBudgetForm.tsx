import { useState } from 'react'
import { useCreateBudget } from '@/entities/budget'

interface Props {
  month: string
  onAdded?: () => void
}

export function AddBudgetForm({ month, onAdded }: Props) {
  const { mutate, isPending } = useCreateBudget()
  const [category, setCategory] = useState('')
  const [limit, setLimit] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate(
      { category, limitAmount: parseFloat(limit), month },
      { onSuccess: () => { setCategory(''); setLimit(''); onAdded?.() } },
    )
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 sm:flex-row sm:items-end sm:gap-2">
      <div className="flex-1">
        <label className="block text-xs text-gray-500 mb-1">Category</label>
        <input
          type="text"
          placeholder="e.g. Food"
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          required
          className="w-full rounded-md border px-3 py-2 text-sm"
        />
      </div>
      <div className="w-full sm:w-36">
        <label className="block text-xs text-gray-500 mb-1">Limit ($)</label>
        <input
          type="number"
          placeholder="500"
          value={limit}
          onChange={(e) => setLimit(e.target.value)}
          required
          className="w-full rounded-md border px-3 py-2 text-sm"
        />
      </div>
      <button
        type="submit"
        disabled={isPending}
        className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white disabled:opacity-50"
      >
        {isPending ? '...' : 'Add Budget'}
      </button>
    </form>
  )
}
