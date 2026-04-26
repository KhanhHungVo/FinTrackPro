import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { TradesPage } from './TradesPage'
import type { Trade } from '@/entities/trade'
import type { PagedResult } from '@/shared/api/types'
import type { UseQueryResult } from '@tanstack/react-query'
import type { TradesSummary } from '@/entities/trade'

type TradesResult = UseQueryResult<PagedResult<Trade>>
type SummaryResult = UseQueryResult<TradesSummary>

const mockDeleteTrade = vi.fn()
const mockGuardedDelete = vi.fn()

vi.mock('@/entities/trade', () => ({
  useTrades: vi.fn(),
  useTradesSummary: vi.fn(),
  useDeleteTrade: () => ({ mutate: mockDeleteTrade }),
}))
vi.mock('@/features/add-trade', () => ({ AddTradeForm: () => null }))
vi.mock('@/features/edit-trade', () => ({ EditTradeModal: () => null }))
vi.mock('@/features/close-position', () => ({ ClosePositionModal: () => null }))
vi.mock('@/features/filter-trades', () => ({
  TradeFilterBar: () => null,
}))
vi.mock('@/features/locale', () => ({
  useLocaleStore: () => 'USD',
}))
vi.mock('@/entities/exchange-rate', () => ({
  useExchangeRates: () => ({ data: { USD: 1 } }),
}))
vi.mock('@/shared/lib/convertAmount', () => ({ convertAmount: (v: number) => v }))
vi.mock('@/shared/lib/formatCurrency', () => ({ formatCurrency: (v: number) => String(v) }))
vi.mock('@/shared/lib/useGuardedMutation', () => ({
  useGuardedMutation: () => ({ guarded: mockGuardedDelete, isPending: () => false }),
}))
vi.mock('@/shared/lib/useDebounce', () => ({ useDebounce: (v: unknown) => v }))
vi.mock('@/shared/ui', () => ({
  ConfirmDeleteDialog: () => null,
  Pagination: () => null,
  SortableColumnHeader: ({ label }: { label: string }) => <span>{label}</span>,
}))
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k, i18n: { language: 'en' } }),
}))
vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

import { useTrades, useTradesSummary } from '@/entities/trade'

const closedTrade: Trade = {
  id: 'c1',
  symbol: 'BTCUSDT',
  direction: 'Long',
  status: 'Closed',
  entryPrice: 30000,
  exitPrice: 35000,
  currentPrice: null,
  positionSize: 0.1,
  fees: 5,
  currency: 'USD',
  rateToUsd: 1,
  result: 495,
  unrealizedResult: null,
  notes: null,
  createdAt: '2026-03-01T00:00:00Z',
}

const openTradeWithPrice: Trade = {
  id: 'o1',
  symbol: 'ETHUSDT',
  direction: 'Long',
  status: 'Open',
  entryPrice: 2000,
  exitPrice: null,
  currentPrice: 2200,
  positionSize: 1,
  fees: 0,
  currency: 'USD',
  rateToUsd: 1,
  result: 0,
  unrealizedResult: 200,
  notes: null,
  createdAt: '2026-03-02T00:00:00Z',
}

const openTradeNoPrice: Trade = {
  ...openTradeWithPrice,
  id: 'o2',
  currentPrice: null,
  unrealizedResult: null,
}

function pagedOf(items: Trade[]): PagedResult<Trade> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1, hasPreviousPage: false, hasNextPage: false }
}

const defaultSummary = { totalPnl: 495, winRate: 100, totalTrades: 1, unrealizedPnl: 0 }

beforeEach(() => {
  vi.clearAllMocks()
  vi.mocked(useTradesSummary).mockReturnValue({ data: defaultSummary, isLoading: false } as unknown as SummaryResult)
})

describe('TradesPage — summary cards', () => {
  it('shows Total P&L from summary hook', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([closedTrade]), isLoading: false } as unknown as TradesResult)
    vi.mocked(useTradesSummary).mockReturnValue({ data: { totalPnl: 495, winRate: 100, totalTrades: 1, unrealizedPnl: 0 }, isLoading: false } as unknown as SummaryResult)

    render(<TradesPage />)

    expect(screen.getByText('trades.totalPnl')).toBeInTheDocument()
    expect(screen.getAllByText('+495').length).toBeGreaterThan(0)
  })

  it('Win Rate comes from summary hook', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([closedTrade]), isLoading: false } as unknown as TradesResult)
    vi.mocked(useTradesSummary).mockReturnValue({ data: { totalPnl: 495, winRate: 100, totalTrades: 1, unrealizedPnl: 0 }, isLoading: false } as unknown as SummaryResult)

    render(<TradesPage />)

    expect(screen.getByText('100%')).toBeInTheDocument()
  })

  it('Total Trades comes from summary hook', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([closedTrade, openTradeWithPrice]), isLoading: false } as unknown as TradesResult)
    vi.mocked(useTradesSummary).mockReturnValue({ data: { totalPnl: 495, winRate: 50, totalTrades: 2, unrealizedPnl: 200 }, isLoading: false } as unknown as SummaryResult)

    render(<TradesPage />)

    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('Unrealized P&L card appears when summary unrealizedPnl is non-zero', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([openTradeWithPrice]), isLoading: false } as unknown as TradesResult)
    vi.mocked(useTradesSummary).mockReturnValue({ data: { totalPnl: 0, winRate: 0, totalTrades: 1, unrealizedPnl: 200 }, isLoading: false } as unknown as SummaryResult)

    render(<TradesPage />)

    expect(screen.getByText('trades.unrealizedPnl')).toBeInTheDocument()
  })

  it('Unrealized P&L card is absent when summary unrealizedPnl is zero', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([closedTrade]), isLoading: false } as unknown as TradesResult)
    vi.mocked(useTradesSummary).mockReturnValue({ data: { totalPnl: 495, winRate: 100, totalTrades: 1, unrealizedPnl: 0 }, isLoading: false } as unknown as SummaryResult)

    render(<TradesPage />)

    expect(screen.queryByText('trades.unrealizedPnl')).not.toBeInTheDocument()
  })
})

describe('TradesPage — trade table badges', () => {
  beforeEach(() => {
    vi.mocked(useTradesSummary).mockReturnValue({ data: defaultSummary, isLoading: false } as unknown as SummaryResult)
  })

  it('open trade row shows Open badge', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([openTradeWithPrice]), isLoading: false } as unknown as TradesResult)

    render(<TradesPage />)

    expect(screen.getByText('trades.open')).toBeInTheDocument()
  })

  it('closed trade row shows Closed badge', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([closedTrade]), isLoading: false } as unknown as TradesResult)

    render(<TradesPage />)

    expect(screen.getByText('trades.closed')).toBeInTheDocument()
  })

  it('open trade with no current price shows — in Exit/Current Price column', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([openTradeNoPrice]), isLoading: false } as unknown as TradesResult)

    render(<TradesPage />)

    expect(screen.getAllByText('—').length).toBeGreaterThan(0)
  })

  it('Close button is visible only for open trades', () => {
    vi.mocked(useTrades).mockReturnValue({ data: pagedOf([closedTrade, openTradeWithPrice]), isLoading: false } as unknown as TradesResult)

    render(<TradesPage />)

    const closeButtons = screen.getAllByTitle('trades.closeTrade')
    expect(closeButtons).toHaveLength(1)
  })
})
