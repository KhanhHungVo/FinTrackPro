import { useState, useRef, useCallback } from 'react'
import ReactDOM from 'react-dom'
import { cn } from '@/shared/lib/cn'

interface TooltipProps {
  content: string
  children: React.ReactNode
  className?: string
}

function Tooltip({ content, children, className }: TooltipProps) {
  const [visible, setVisible] = useState(false)
  const [pos, setPos] = useState({ top: 0, left: 0 })
  const triggerRef = useRef<HTMLSpanElement>(null)

  const show = useCallback(() => {
    if (!triggerRef.current) return
    const rect = triggerRef.current.getBoundingClientRect()
    setPos({
      top: rect.bottom + 6,
      left: rect.left + rect.width / 2,
    })
    setVisible(true)
  }, [])

  const hide = useCallback(() => setVisible(false), [])

  const tooltip = (
    <div
      role="tooltip"
      style={{ top: pos.top, left: pos.left, transform: 'translateX(-50%)' }}
      className={cn(
        'pointer-events-none fixed z-50 max-w-xs rounded px-2.5 py-1.5 text-xs font-medium shadow-lg transition-opacity duration-200',
        'bg-gray-900 text-white dark:bg-slate-700 dark:text-slate-100',
        visible ? 'opacity-100' : 'opacity-0',
      )}
    >
      {content}
    </div>
  )

  return (
    <>
      <span
        ref={triggerRef}
        className={cn('cursor-default', className)}
        onMouseEnter={show}
        onMouseLeave={hide}
        onFocus={show}
        onBlur={hide}
      >
        {children}
      </span>
      {ReactDOM.createPortal(tooltip, document.body)}
    </>
  )
}

export { Tooltip }
