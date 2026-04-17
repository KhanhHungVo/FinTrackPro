import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer } from 'recharts'
import { formatCurrency } from '@/shared/lib/formatCurrency'

const PALETTE = [
  '#3b82f6', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6',
  '#06b6d4', '#f97316', '#ec4899', '#14b8a6', '#6366f1',
]

interface AllocationDonutProps {
  data: { symbol: string; value: number }[]
  totalCapital: number
  currency: string
  locale: string
  centerLabel: string
}

export function AllocationDonut({ data, totalCapital, currency, locale, centerLabel }: AllocationDonutProps) {
  return (
    <div className="relative w-32 h-32 flex-shrink-0">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={34}
            outerRadius={56}
            dataKey="value"
            nameKey="symbol"
            strokeWidth={1}
            stroke="transparent"
          >
            {data.map((_, i) => (
              <Cell key={i} fill={PALETTE[i % PALETTE.length]} />
            ))}
          </Pie>
          <Tooltip
            formatter={(value) => formatCurrency(Number(value), currency, locale)}
            contentStyle={{
              background: '#fff',
              border: '1px solid #e5e7eb',
              borderRadius: '8px',
              fontSize: '11px',
            }}
          />
        </PieChart>
      </ResponsiveContainer>
      <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
        <span className="text-[10px] text-gray-400 dark:text-slate-500">{centerLabel}</span>
        <span className="text-xs font-bold text-gray-800 dark:text-slate-100 leading-tight text-center px-1">
          {formatCurrency(totalCapital, currency, locale)}
        </span>
      </div>
    </div>
  )
}
