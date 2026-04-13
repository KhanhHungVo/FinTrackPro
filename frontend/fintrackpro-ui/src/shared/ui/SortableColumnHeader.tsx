import { cn } from '@/shared/lib/cn'

type SortDir = 'asc' | 'desc' | null

interface SortableColumnHeaderProps {
  label: string
  field: string
  currentSortBy: string | null
  currentSortDir: SortDir
  onSort: (field: string, dir: SortDir) => void
  align?: 'left' | 'right'
  className?: string
}

function nextDir(current: SortDir): SortDir {
  if (current === null) return 'desc'
  if (current === 'desc') return 'asc'
  return null
}

function indicator(active: boolean, dir: SortDir): string {
  if (!active || dir === null) return '↕'
  return dir === 'desc' ? '↓' : '↑'
}

export function SortableColumnHeader({
  label,
  field,
  currentSortBy,
  currentSortDir,
  onSort,
  align = 'left',
  className,
}: SortableColumnHeaderProps) {
  const active = currentSortBy === field

  function handleClick() {
    const next = active ? nextDir(currentSortDir) : 'desc'
    onSort(field, next)
  }

  return (
    <button
      onClick={handleClick}
      className={cn(
        'flex items-center gap-0.5 text-xs font-semibold uppercase tracking-wide transition-colors',
        align === 'right' && 'ml-auto',
        active
          ? 'text-blue-600 dark:text-blue-400'
          : 'text-gray-400 dark:text-slate-500 hover:text-gray-600 dark:hover:text-slate-300',
        className,
      )}
    >
      {label}
      <span className="ml-0.5 text-[10px]">{indicator(active, currentSortDir)}</span>
    </button>
  )
}
