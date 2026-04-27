import { useState, useRef, useCallback, useId } from 'react'
import ReactDOM from 'react-dom'
import { cn } from '@/shared/lib/cn'

// ── Data schema ──────────────────────────────────────────────────────────────

export interface RowHoverCardBadge {
  label: string
  color: 'green' | 'red' | 'emerald' | 'gray' | 'amber' | 'blue'
}

export interface RowHoverCardData {
  /** Display name — falls back to symbol if omitted */
  name?: string
  symbol: string
  rank?: number
  price?: number | null
  /** Optional formatted price string (pre-formatted, e.g. with currency symbol) */
  priceFormatted?: string
  change1h?: number | null
  change24h?: number | null
  change7d?: number | null
  marketCap?: number | null
  /** RSI values shown in a 2×2 grid */
  rsi?: {
    '1h'?: number | null
    '4h'?: number | null
    daily?: number | null
    weekly?: number | null
  }
  /** Inline badges (direction, status, etc.) shown in the header */
  badges?: RowHoverCardBadge[]
  /** Arbitrary label/value rows appended at the bottom */
  extra?: { label: string; value: string; highlight?: 'green' | 'red' | 'gray' }[]
  /** Footer hint text */
  hint?: string
}

// ── Formatting helpers ────────────────────────────────────────────────────────

function formatUsdPrice(v: number | null | undefined) {
  if (v == null) return null
  if (v >= 1000) return `$${v.toLocaleString('en-US', { maximumFractionDigits: 0 })}`
  if (v >= 0.01) return `$${v.toFixed(2)}`
  return `$${v.toFixed(6)}`
}

function formatMCap(v: number | null | undefined) {
  if (v == null) return null
  if (v >= 1e12) return `$${(v / 1e12).toFixed(2)}T`
  if (v >= 1e9)  return `$${(v / 1e9).toFixed(2)}B`
  if (v >= 1e6)  return `$${(v / 1e6).toFixed(2)}M`
  return `$${v.toLocaleString()}`
}

// ── Sub-components ───────────────────────────────────────────────────────────

function Pct({ v, cols }: { v: number | null | undefined; cols?: boolean }) {
  if (v == null) return <span className={cn('font-mono text-xs text-slate-600', cols && 'text-center')}>—</span>
  const pos = v >= 0
  return (
    <span className={cn(
      'font-mono text-xs tabular-nums font-medium',
      pos ? 'text-emerald-400' : 'text-red-400',
      cols && 'text-center',
    )}>
      {pos ? '+' : ''}{v.toFixed(2)}%
    </span>
  )
}

const RSI_BADGE_CLASS: Record<string, string> = {
  oversold:  'bg-blue-500/20 text-blue-300',
  overbought:'bg-red-500/20 text-red-300',
  neutral:   'bg-white/5 text-slate-400',
}

function RsiValue({ v }: { v: number | null | undefined }) {
  if (v == null) return <span className="font-mono text-[11px] text-slate-600">—</span>
  const state = v < 30 ? 'oversold' : v > 70 ? 'overbought' : 'neutral'
  return (
    <span className={cn('inline-flex items-center rounded px-1.5 py-0.5 font-mono text-[11px] font-semibold', RSI_BADGE_CLASS[state])}>
      {v.toFixed(1)}
      {state !== 'neutral' && (
        <span className="ml-1 text-[9px] uppercase tracking-wider opacity-70">
          {state === 'oversold' ? 'OS' : 'OB'}
        </span>
      )}
    </span>
  )
}

const BADGE_COLORS: Record<RowHoverCardBadge['color'], string> = {
  green:   'bg-green-500/20 text-green-400 ring-green-500/20',
  red:     'bg-red-500/20 text-red-400 ring-red-500/20',
  emerald: 'bg-emerald-500/20 text-emerald-400 ring-emerald-500/20',
  gray:    'bg-white/8 text-slate-400 ring-white/10',
  amber:   'bg-amber-500/20 text-amber-400 ring-amber-500/20',
  blue:    'bg-blue-500/20 text-blue-400 ring-blue-500/20',
}

const HIGHLIGHT_COLORS = {
  green: 'text-emerald-400',
  red:   'text-red-400',
  gray:  'text-slate-400',
}

// ── Card layout ───────────────────────────────────────────────────────────────

const CARD_WIDTH = 252

function CardContent({ data }: { data: RowHoverCardData }) {
  const displayName = data.name ?? data.symbol
  const price = data.priceFormatted ?? formatUsdPrice(data.price)
  const mcap = formatMCap(data.marketCap)
  const hasChanges = data.change1h != null || data.change24h != null || data.change7d != null
  const hasRsi = data.rsi && Object.values(data.rsi).some((v) => v != null)

  return (
    <>
      {/* ── Header ── */}
      <div className="px-4 pt-3.5 pb-3 border-b border-white/[0.07]">
        <div className="flex items-center gap-2 flex-wrap">
          {data.rank != null && (
            <span className="font-mono text-[10px] text-slate-600 tabular-nums">#{data.rank}</span>
          )}
          <span className="text-[13px] font-semibold text-slate-100 leading-none tracking-tight">
            {displayName}
          </span>
          {displayName !== data.symbol && (
            <span className="font-mono text-[10px] text-slate-500 tracking-widest">
              {data.symbol.toUpperCase()}
            </span>
          )}
          {data.badges?.map((b) => (
            <span
              key={b.label}
              className={cn(
                'inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-bold tracking-wide ring-1',
                BADGE_COLORS[b.color],
              )}
            >
              {b.label}
            </span>
          ))}
        </div>
        {price && (
          <p className="mt-2 font-mono text-[15px] font-bold text-white tracking-tight leading-none">
            {price}
          </p>
        )}
      </div>

      {/* ── Price changes ── */}
      {hasChanges && (
        <div className="grid grid-cols-3 divide-x divide-white/[0.06]">
          {[
            { label: '1H', v: data.change1h },
            { label: '24H', v: data.change24h },
            { label: '7D', v: data.change7d },
          ].map(({ label, v }) => (
            <div key={label} className="flex flex-col items-center py-2.5 gap-1">
              <span className="text-[9px] font-medium uppercase tracking-[0.1em] text-slate-600">{label}</span>
              <Pct v={v} cols />
            </div>
          ))}
        </div>
      )}

      {/* ── RSI grid ── */}
      {hasRsi && (
        <div className="border-t border-white/[0.06]">
          <div className="px-4 py-1.5">
            <span className="text-[9px] font-medium uppercase tracking-[0.1em] text-slate-600">RSI</span>
          </div>
          <div className="grid grid-cols-2 divide-x divide-y divide-white/[0.05] border-t border-white/[0.05]">
            {[
              { label: '1H', v: data.rsi?.['1h'] },
              { label: '4H', v: data.rsi?.['4h'] },
              { label: 'DAILY', v: data.rsi?.daily },
              { label: 'WEEKLY', v: data.rsi?.weekly },
            ].map(({ label, v }) => (
              <div key={label} className="flex items-center justify-between px-3 py-2 gap-2">
                <span className="text-[9px] font-medium uppercase tracking-[0.1em] text-slate-600">{label}</span>
                <RsiValue v={v} />
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── Market cap ── */}
      {mcap && (
        <div className="flex items-center justify-between px-4 py-2.5 border-t border-white/[0.06]">
          <span className="text-[9px] font-medium uppercase tracking-[0.1em] text-slate-600">Mkt Cap</span>
          <span className="font-mono text-xs text-slate-300">{mcap}</span>
        </div>
      )}

      {/* ── Extra rows ── */}
      {data.extra?.map(({ label, value, highlight }) => (
        <div key={label} className="flex items-center justify-between px-4 py-2 border-t border-white/[0.06]">
          <span className="text-[9px] font-medium uppercase tracking-[0.1em] text-slate-600">{label}</span>
          <span className={cn('font-mono text-xs', highlight ? HIGHLIGHT_COLORS[highlight] : 'text-slate-300')}>
            {value}
          </span>
        </div>
      ))}

      {/* ── Footer hint ── */}
      {data.hint && (
        <div className="px-4 py-2.5 border-t border-white/[0.06]">
          <span className="text-[10px] text-slate-600">{data.hint}</span>
        </div>
      )}
    </>
  )
}

// ── Constants ─────────────────────────────────────────────────────────────────

// Offset from cursor so the card never sits directly under the pointer
const CURSOR_OFFSET_X = 16
const CURSOR_OFFSET_Y = 12
// Estimated card height for bottom-edge clamping (used before the DOM measures it)
const ESTIMATED_CARD_HEIGHT = 340

// ── Main component ────────────────────────────────────────────────────────────

interface RowHoverCardProps {
  data: RowHoverCardData
  href?: string
  /** Render as <tr> wrapper for HTML table rows */
  as?: 'tr' | 'div' | 'li' | 'a'
  children: React.ReactNode
  className?: string
}

function RowHoverCard({ data, href, as: Tag = 'div', children, className }: RowHoverCardProps) {
  const [visible, setVisible] = useState(false)
  const [pos, setPos] = useState({ top: 0, left: 0 })
  const rowRef = useRef<HTMLElement>(null)
  const cardRef = useRef<HTMLDivElement>(null)
  const hideTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const cardId = useId()

  // Position card near the cursor, flipping left/above when near viewport edges
  const placeAtCursor = useCallback((cx: number, cy: number) => {
    const vw = window.innerWidth
    const vh = window.innerHeight
    const cardW = CARD_WIDTH
    const cardH = cardRef.current?.offsetHeight ?? ESTIMATED_CARD_HEIGHT

    // Prefer right of cursor; flip left if it would overflow
    const left = cx + CURSOR_OFFSET_X + cardW > vw
      ? cx - cardW - CURSOR_OFFSET_X
      : cx + CURSOR_OFFSET_X

    // Prefer below cursor; flip above if it would overflow
    const top = cy + CURSOR_OFFSET_Y + cardH > vh
      ? cy - cardH - CURSOR_OFFSET_Y
      : cy + CURSOR_OFFSET_Y

    setPos({ top: Math.max(top, 8), left: Math.max(left, 8) })
  }, [])

  const handleMouseMove = useCallback((e: React.MouseEvent) => {
    placeAtCursor(e.clientX, e.clientY)
  }, [placeAtCursor])

  const show = useCallback((e: React.MouseEvent) => {
    if (hideTimer.current) clearTimeout(hideTimer.current)
    placeAtCursor(e.clientX, e.clientY)
    setVisible(true)
  }, [placeAtCursor])

  const hide = useCallback(() => {
    hideTimer.current = setTimeout(() => setVisible(false), 80)
  }, [])

  const keepOpen = useCallback(() => {
    if (hideTimer.current) clearTimeout(hideTimer.current)
  }, [])

  const card = ReactDOM.createPortal(
    <div
      ref={cardRef}
      onMouseEnter={keepOpen}
      onMouseLeave={hide}
      style={{ top: pos.top, left: pos.left, width: CARD_WIDTH }}
      className={cn(
        'fixed z-[9999] overflow-hidden rounded-xl border border-white/[0.09] bg-[#0d1017] shadow-2xl shadow-black/60 ring-1 ring-black/30',
        'transition-[opacity,transform] duration-150 ease-out',
        visible ? 'opacity-100 translate-y-0' : 'opacity-0 -translate-y-1.5 pointer-events-none',
      )}
      role="tooltip"
      id={cardId}
    >
      <CardContent data={data} />
    </div>,
    document.body,
  )

  // For <tr> we can't use a wrapper element — attach events directly to the <tr>
  if (Tag === 'tr') {
    return (
      <>
        <tr
          ref={rowRef as React.RefObject<HTMLTableRowElement>}
          aria-describedby={visible ? cardId : undefined}
          className={className}
          onMouseEnter={show}
          onMouseMove={handleMouseMove}
          onMouseLeave={hide}
        >
          {children}
        </tr>
        {card}
      </>
    )
  }

  const props = {
    ref: rowRef as React.RefObject<HTMLAnchorElement & HTMLDivElement & HTMLLIElement>,
    'aria-describedby': visible ? cardId : undefined,
    className,
    onMouseEnter: show,
    onMouseMove: handleMouseMove,
    onMouseLeave: hide,
    ...(href ? { href } : {}),
  }

  return (
    <>
      <Tag {...(props as React.HTMLAttributes<HTMLElement>)}>
        {children}
      </Tag>
      {card}
    </>
  )
}

export { RowHoverCard }
