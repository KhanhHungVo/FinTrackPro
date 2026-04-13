import { cn } from '@/shared/lib/cn'

interface PaginationProps {
  page: number
  totalPages: number
  pageSize: number
  onPageChange: (page: number) => void
  onPageSizeChange: (size: number) => void
  pageSizeOptions?: number[]
  disabled?: boolean
}

function buildPageNumbers(page: number, totalPages: number): (number | '...')[] {
  if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1)

  const pages: (number | '...')[] = [1]

  if (page > 3) pages.push('...')

  const start = Math.max(2, page - 1)
  const end = Math.min(totalPages - 1, page + 1)
  for (let i = start; i <= end; i++) pages.push(i)

  if (page < totalPages - 2) pages.push('...')

  pages.push(totalPages)
  return pages
}

export function Pagination({
  page,
  totalPages,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50],
  disabled = false,
}: PaginationProps) {
  if (totalPages <= 1 && pageSizeOptions.length === 0) return null

  const pageNumbers = buildPageNumbers(page, totalPages)

  const btnBase =
    'inline-flex items-center justify-center rounded px-2.5 py-1 text-sm transition-colors disabled:cursor-not-allowed disabled:opacity-40'

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
      {/* Page size selector */}
      <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-slate-400">
        <span>Rows:</span>
        <select
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          disabled={disabled}
          className="rounded border border-gray-300 px-2 py-0.5 text-sm dark:bg-slate-800 dark:border-white/12 dark:text-white"
        >
          {pageSizeOptions.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>

      {/* Page buttons */}
      {totalPages > 1 && (
        <div className="flex items-center gap-1">
          <button
            onClick={() => onPageChange(page - 1)}
            disabled={disabled || page <= 1}
            className={cn(btnBase, 'text-gray-500 hover:bg-gray-100 dark:hover:bg-white/8')}
          >
            ‹
          </button>

          {pageNumbers.map((p, i) =>
            p === '...' ? (
              <span key={`ellipsis-${i}`} className="px-1.5 text-sm text-gray-400">…</span>
            ) : (
              <button
                key={p}
                onClick={() => onPageChange(p)}
                disabled={disabled}
                className={cn(
                  btnBase,
                  p === page
                    ? 'bg-blue-600 text-white font-semibold'
                    : 'text-gray-600 hover:bg-gray-100 dark:text-slate-300 dark:hover:bg-white/8',
                )}
              >
                {p}
              </button>
            ),
          )}

          <button
            onClick={() => onPageChange(page + 1)}
            disabled={disabled || page >= totalPages}
            className={cn(btnBase, 'text-gray-500 hover:bg-gray-100 dark:hover:bg-white/8')}
          >
            ›
          </button>
        </div>
      )}
    </div>
  )
}
