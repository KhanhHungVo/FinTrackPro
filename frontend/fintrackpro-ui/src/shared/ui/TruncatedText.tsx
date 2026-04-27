import { useState, useRef, useEffect, useCallback } from 'react'
import ReactDOM from 'react-dom'
import { cn } from '@/shared/lib/cn'
import { useClickOutside } from '@/shared/lib/useClickOutside'
import { useEscapeKey } from '@/shared/lib/useEscapeKey'
import { BottomSheet } from './BottomSheet'

type Breakpoint = 'mobile' | 'tablet' | 'desktop'

function getBreakpoint(): Breakpoint {
  if (typeof window === 'undefined') return 'desktop'
  if (window.matchMedia('(min-width: 1280px)').matches) return 'desktop'
  if (window.matchMedia('(min-width: 768px)').matches) return 'tablet'
  return 'mobile'
}

interface TruncatedTextProps {
  text: string
  className?: string
  as?: 'p' | 'span'
}

function TruncatedText({ text, className, as: Tag = 'span' }: TruncatedTextProps) {
  const [breakpoint, setBreakpoint] = useState<Breakpoint>(getBreakpoint)
  const [tooltipVisible, setTooltipVisible] = useState(false)
  const [tooltipPos, setTooltipPos] = useState({ top: 0, left: 0 })
  const [popoverOpen, setPopoverOpen] = useState(false)
  const [popoverPos, setPopoverPos] = useState({ top: 0, left: 0 })
  const [sheetOpen, setSheetOpen] = useState(false)
  const triggerRef = useRef<HTMLElement>(null)
  const popoverRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function update() { setBreakpoint(getBreakpoint()) }
    const mql1 = window.matchMedia('(min-width: 1280px)')
    const mql2 = window.matchMedia('(min-width: 768px)')
    mql1.addEventListener('change', update)
    mql2.addEventListener('change', update)
    return () => {
      mql1.removeEventListener('change', update)
      mql2.removeEventListener('change', update)
    }
  }, [])

  const closePopover = useCallback(() => setPopoverOpen(false), [])
  const closeSheet = useCallback(() => setSheetOpen(false), [])
  useClickOutside(popoverRef, closePopover, popoverOpen)
  useEscapeKey(closePopover, popoverOpen)
  useEscapeKey(closeSheet, sheetOpen)

  // Always render with truncation so the layout is consistent.
  // Interaction is available on every breakpoint regardless of overflow —
  // the user shouldn't have to guess whether a value is truncated.
  const baseClass = cn('truncate', className)

  if (breakpoint === 'desktop') {
    function showTooltip() {
      const el = triggerRef.current
      if (!el) return
      const rect = el.getBoundingClientRect()
      setTooltipPos({ top: rect.bottom + 6, left: rect.left + rect.width / 2 })
      setTooltipVisible(true)
    }

    const tooltip = tooltipVisible
      ? ReactDOM.createPortal(
          <div
            role="tooltip"
            style={{ top: tooltipPos.top, left: tooltipPos.left, transform: 'translateX(-50%)' }}
            className="pointer-events-none fixed z-[9999] max-w-sm rounded-md bg-gray-950 px-3 py-2 text-xs font-medium leading-relaxed text-white shadow-xl ring-1 ring-white/10 dark:bg-slate-800"
          >
            {text}
          </div>,
          document.body,
        )
      : null

    return (
      <>
        <Tag
          ref={triggerRef as React.RefObject<HTMLParagraphElement & HTMLSpanElement>}
          className={baseClass}
          onMouseEnter={showTooltip}
          onMouseLeave={() => setTooltipVisible(false)}
          onFocus={showTooltip}
          onBlur={() => setTooltipVisible(false)}
        >
          {text}
        </Tag>
        {tooltip}
      </>
    )
  }

  if (breakpoint === 'tablet') {
    function openPopover(e: React.MouseEvent | React.KeyboardEvent) {
      if ('key' in e && e.key !== 'Enter' && e.key !== ' ') return
      e.preventDefault()
      const el = triggerRef.current
      if (!el) return
      const rect = el.getBoundingClientRect()
      setPopoverPos({ top: rect.bottom + 6, left: rect.left })
      setPopoverOpen(true)
    }

    return (
      <span className="relative min-w-0">
        <Tag
          ref={triggerRef as React.RefObject<HTMLParagraphElement & HTMLSpanElement>}
          role="button"
          tabIndex={0}
          className={cn(baseClass, 'cursor-pointer')}
          onClick={openPopover}
          onKeyDown={openPopover}
        >
          {text}
        </Tag>
        {popoverOpen &&
          ReactDOM.createPortal(
            <div
              ref={popoverRef}
              role="dialog"
              aria-modal="true"
              style={{ top: popoverPos.top, left: popoverPos.left }}
              className="fixed z-[9999] min-w-[14rem] max-w-sm rounded-lg border border-gray-200 bg-white p-3 shadow-xl dark:border-white/10 dark:bg-[#161a25]"
            >
              <div className="flex items-start justify-between gap-3">
                <p className="text-sm leading-relaxed text-gray-800 dark:text-slate-200">{text}</p>
                <button
                  onClick={closePopover}
                  aria-label="Close"
                  className="flex-shrink-0 rounded p-0.5 text-lg leading-none text-gray-400 hover:text-gray-600 dark:text-slate-500 dark:hover:text-slate-300"
                >
                  ×
                </button>
              </div>
            </div>,
            document.body,
          )}
      </span>
    )
  }

  // Mobile
  return (
    <>
      <Tag
        ref={triggerRef as React.RefObject<HTMLParagraphElement & HTMLSpanElement>}
        role="button"
        tabIndex={0}
        className={cn(baseClass, 'cursor-pointer')}
        onClick={() => setSheetOpen(true)}
        onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setSheetOpen(true) } }}
      >
        {text}
      </Tag>
      <BottomSheet open={sheetOpen} onClose={closeSheet}>
        {text}
      </BottomSheet>
    </>
  )
}

export { TruncatedText }
