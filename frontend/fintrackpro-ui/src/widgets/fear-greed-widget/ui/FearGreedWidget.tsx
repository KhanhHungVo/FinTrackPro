import { useTranslation } from 'react-i18next'
import { useFearGreed } from '@/entities/signal'

const CX = 150
const CY = 150
const R = 105
const ARC_STROKE = 28
const NEEDLE_LEN = 88
const GAP = 1 // degree gap between segments

interface Zone {
  label: string
  startDeg: number
  endDeg: number
  color: string
  minVal: number
  maxVal: number
}

const zones: Zone[] = [
  { label: 'EXTREME\nFEAR', startDeg: 180, endDeg: 216, color: '#f87171', minVal: 0, maxVal: 20 },
  { label: 'FEAR', startDeg: 216, endDeg: 252, color: '#fb923c', minVal: 20, maxVal: 40 },
  { label: 'NEUTRAL', startDeg: 252, endDeg: 288, color: '#facc15', minVal: 40, maxVal: 60 },
  { label: 'GREED', startDeg: 288, endDeg: 324, color: '#4ade80', minVal: 60, maxVal: 80 },
  { label: 'EXTREME\nGREED', startDeg: 324, endDeg: 360, color: '#16a34a', minVal: 80, maxVal: 100 },
]

const boundaryValues = [0, 25, 50, 75, 100]
const boundaryAngles = boundaryValues.map((v) => 180 + (v / 100) * 180)

function polarToCartesian(cx: number, cy: number, r: number, angleDeg: number) {
  const rad = (angleDeg * Math.PI) / 180
  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) }
}

function describeArc(cx: number, cy: number, r: number, startDeg: number, endDeg: number) {
  const start = polarToCartesian(cx, cy, r, startDeg)
  const end = polarToCartesian(cx, cy, r, endDeg)
  const largeArc = Math.abs(endDeg - startDeg) > 180 ? 1 : 0
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArc} 1 ${end.x} ${end.y}`
}

function describeWedge(cx: number, cy: number, rOuter: number, rInner: number, startDeg: number, endDeg: number) {
  const outerStart = polarToCartesian(cx, cy, rOuter, startDeg)
  const outerEnd = polarToCartesian(cx, cy, rOuter, endDeg)
  const innerStart = polarToCartesian(cx, cy, rInner, startDeg)
  const innerEnd = polarToCartesian(cx, cy, rInner, endDeg)
  const largeArc = Math.abs(endDeg - startDeg) > 180 ? 1 : 0
  return [
    `M ${outerStart.x} ${outerStart.y}`,
    `A ${rOuter} ${rOuter} 0 ${largeArc} 1 ${outerEnd.x} ${outerEnd.y}`,
    `L ${innerEnd.x} ${innerEnd.y}`,
    `A ${rInner} ${rInner} 0 ${largeArc} 0 ${innerStart.x} ${innerStart.y}`,
    'Z',
  ].join(' ')
}

function getActiveZone(value: number): number {
  for (let i = 0; i < zones.length; i++) {
    if (value <= zones[i].maxVal) return i
  }
  return zones.length - 1
}

interface FearGreedWidgetProps {
  compact?: boolean
  horizontal?: boolean
}

export function FearGreedWidget({ compact = false, horizontal = false }: FearGreedWidgetProps) {
  const { t } = useTranslation()
  const { data, isLoading } = useFearGreed()

  if (isLoading) {
    if (horizontal) return <div className="animate-pulse rounded-lg bg-gray-100 dark:bg-white/5 h-14 w-full" />
    return <div className={`animate-pulse rounded-lg bg-gray-100 dark:bg-white/5 ${compact ? 'h-32' : 'h-44'}`} />
  }
  if (!data) return null

  const activeIdx = getActiveZone(data.value)
  const needleAngle = 180 + (data.value / 100) * 180

  // ── Horizontal mood-bar strip ─────────────────────────────────────
  if (horizontal) {
    const activeZone = zones[activeIdx]
    return (
      <div className="glass-card px-4 py-3 w-full">
        {/* Mobile: title sits above, centered */}
        <p className="text-[9px] text-gray-500 dark:text-slate-400 tracking-[0.12em] uppercase mb-2 text-center sm:hidden">
          {t('market.fearGreedIndex')}
        </p>

        <div className="flex items-center gap-3">
          {/* Colored indicator box — same visual height as the value text */}
          <div
            className="w-9 h-9 shrink-0 rounded-lg flex items-center justify-center"
            style={{
              backgroundColor: `${activeZone.color}22`,
              border: `1.5px solid ${activeZone.color}`,
            }}
          >
            <div className="w-4 h-4 rounded-full" style={{ backgroundColor: activeZone.color }} />
          </div>

          {/* Right content column */}
          <div className="flex-1 min-w-0">
            {/* Row: title (desktop only) + value + sentiment */}
            <div className="flex items-center gap-2 mb-2">
              <p className="text-[9px] text-gray-500 dark:text-slate-400 tracking-[0.12em] uppercase hidden sm:block shrink-0">
                {t('market.fearGreedIndex')}
              </p>
              <span className="font-bold text-gray-800 dark:text-slate-100 sm:ml-auto">{data.value}</span>
              <span className="text-[9px] font-bold tracking-[0.15em]" style={{ color: activeZone.color }}>
                {activeZone.label.replace('\n', ' ')}
              </span>
            </div>

            {/* Gradient bar with position marker */}
            <div className="relative h-1.5 rounded-full" style={{
              background: 'linear-gradient(to right, #f87171 0%, #fb923c 25%, #facc15 50%, #4ade80 75%, #16a34a 100%)',
            }}>
              <div
                className="absolute top-1/2 -translate-y-1/2 w-1 h-3.5 rounded-full shadow-md"
                style={{
                  left: `clamp(2px, calc(${data.value}% - 2px), calc(100% - 4px))`,
                  backgroundColor: '#1e293b',
                  outline: '1.5px solid white',
                }}
              />
            </div>

            {/* Zone labels */}
            <div className="flex justify-between mt-1.5">
              {zones.map((z) => (
                <span
                  key={z.label}
                  className="text-[7px] leading-tight text-center"
                  style={{ color: z === activeZone ? activeZone.color : undefined }}
                >
                  <span className="text-gray-400 dark:text-slate-500 [span&]:text-inherit">
                    {z.label.split('\n').map((line, i) => (
                      <span key={i} className="block">{line}</span>
                    ))}
                  </span>
                </span>
              ))}
            </div>
          </div>
        </div>
      </div>
    )
  }

  // ── Compact semicircle (kept for other uses) ──────────────────────
  if (compact) {
    const activeZone = zones[activeIdx]
    return (
      <div className="glass-card flex flex-col items-center justify-center py-4 text-center w-full">
        <p className="text-[10px] text-gray-500 dark:text-slate-400 tracking-[0.1em] uppercase mb-1 px-4">
          {t('market.fearGreedIndex')}
        </p>
        <svg viewBox="20 30 260 185" className="w-full mx-auto">
          {zones.map((zone, i) => {
            const isActive = i === activeIdx
            const inset = GAP / 2
            return (
              <path
                key={zone.label}
                d={describeArc(CX, CY, R, zone.startDeg + inset, zone.endDeg - inset)}
                fill="none"
                stroke={zone.color}
                strokeWidth={ARC_STROKE}
                strokeLinecap="butt"
                opacity={isActive ? 1 : 0.22}
              />
            )
          })}
          <line
            x1={CX} y1={CY} x2={CX + NEEDLE_LEN} y2={CY}
            className="stroke-gray-800 dark:stroke-slate-200"
            strokeWidth={3.5} strokeLinecap="round"
            style={{ transform: `rotate(${needleAngle}deg)`, transformOrigin: `${CX}px ${CY}px`, transition: 'transform 0.8s cubic-bezier(0.4,0,0.2,1)' }}
          />
          <circle cx={CX} cy={CY} r={5} className="fill-gray-800 dark:fill-slate-200" />
          <text x={CX} y={CY + 30} textAnchor="middle" fontWeight="800" fontSize="36" className="fill-gray-800 dark:fill-slate-100">
            {data.value}
          </text>
          <text x={CX} y={CY + 58} textAnchor="middle" fontWeight="700" fontSize="13" letterSpacing="3" fill={activeZone.color}>
            {activeZone.label.replace('\n', ' ')}
          </text>
        </svg>
      </div>
    )
  }

  // ── Full gauge ────────────────────────────────────────────────────
  const dots: { x: number; y: number }[] = []
  const dotRadius = R - ARC_STROKE / 2 - 8
  for (let v = 0; v <= 100; v += 5) {
    if (boundaryValues.includes(v)) continue
    const angle = 180 + (v / 100) * 180
    dots.push(polarToCartesian(CX, CY, dotRadius, angle))
  }

  return (
    <div className="glass-card p-4 text-center">
      <p className="text-sm text-gray-500 dark:text-slate-400 mb-1">{t('market.fearGreedIndex')}</p>

      <svg viewBox="0 0 300 185" className="w-full max-w-[340px] mx-auto">
        {zones.map((zone, i) => {
          const isActive = i === activeIdx
          const inset = GAP / 2
          return (
            <g key={zone.label}>
              {isActive && (
                <path
                  d={describeWedge(
                    CX, CY,
                    R + ARC_STROKE / 2 + 4,
                    R - ARC_STROKE / 2 - 4,
                    zone.startDeg + inset,
                    zone.endDeg - inset,
                  )}
                  fill={zone.color}
                  opacity={0.18}
                />
              )}
              <path
                d={describeArc(CX, CY, R, zone.startDeg + inset, zone.endDeg - inset)}
                fill="none"
                stroke={zone.color}
                strokeWidth={ARC_STROKE}
                strokeLinecap="butt"
                opacity={isActive ? 1 : 0.3}
              />
            </g>
          )
        })}

        {dots.map((d, i) => (
          <circle key={i} cx={d.x} cy={d.y} r={1.5} className="fill-gray-400 dark:fill-slate-600" />
        ))}

        {boundaryValues.map((val, i) => {
          const pos = polarToCartesian(CX, CY, dotRadius - 6, boundaryAngles[i])
          return (
            <text
              key={val}
              x={pos.x}
              y={pos.y}
              textAnchor="middle"
              dominantBaseline="middle"
              fontSize="10"
              fontWeight="500"
              className="fill-gray-500 dark:fill-slate-400"
            >
              {val}
            </text>
          )
        })}

        {zones.map((zone) => {
          const midAngle = (zone.startDeg + zone.endDeg) / 2
          const labelR = R + ARC_STROKE / 2 + 16
          const pos = polarToCartesian(CX, CY, labelR, midAngle)
          const textRotation = midAngle + 90
          const lines = zone.label.split('\n')
          return (
            <text
              key={zone.label}
              x={pos.x}
              y={pos.y}
              textAnchor="middle"
              dominantBaseline="middle"
              fontSize="8"
              fontWeight="600"
              className="fill-gray-500 dark:fill-slate-400"
              transform={`rotate(${textRotation}, ${pos.x}, ${pos.y})`}
            >
              {lines.map((line, li) => (
                <tspan key={li} x={pos.x} dy={li === 0 ? 0 : 10}>
                  {line}
                </tspan>
              ))}
            </text>
          )
        })}

        <line
          x1={CX} y1={CY} x2={CX + NEEDLE_LEN} y2={CY}
          className="stroke-gray-800 dark:stroke-slate-200"
          strokeWidth={3.5}
          strokeLinecap="round"
          style={{
            transform: `rotate(${needleAngle}deg)`,
            transformOrigin: `${CX}px ${CY}px`,
            transition: 'transform 0.8s cubic-bezier(0.4, 0, 0.2, 1)',
          }}
        />

        <circle cx={CX} cy={CY} r={5} className="fill-gray-800 dark:fill-slate-200" />

        <text
          x={CX} y={CY + 28}
          textAnchor="middle"
          fontWeight="800"
          fontSize="32"
          className="fill-gray-800 dark:fill-slate-100"
        >
          {data.value}
        </text>
      </svg>
    </div>
  )
}
