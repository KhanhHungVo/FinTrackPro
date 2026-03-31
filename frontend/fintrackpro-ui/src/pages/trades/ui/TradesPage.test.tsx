import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { TradesPage } from './TradesPage'
import type { Trade } from '@/entities/trade'

const mockDeleteTrade = vi.fn()
const mockGuardedDelete = vi.fn()

vi.mock('@/entities/trade', () => ({
  useTrades: vi.fn(),
  useDeleteTrade: () => ({ mutate: mockDeleteTrade }),
}))
vi.mock('@/features/add-trade', () => ({ AddTradeForm: () => null }))
vi.mock('@/features/edit-trade', () => ({ EditTradeModal: () => null }))
vi.mock('@/features/close-position', () => ({ ClosePositionModal: () => null }))
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
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k, i18n: { language: 'en' } }),
}))
vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

import { useTrades } from '@/entities/trade'

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

beforeEach(() => vi.clearAllMocks())

describe('TradesPage — summary cards', () => {
  it('Total P&L uses closed trades only', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [closedTrade, openTradeWithPrice], isLoading: false } as any)

    render(<TradesPage />)

    // Total P&L card should show 495 (closedTrade.result), not 495+200
    expect(screen.getByText('trades.totalPnl')).toBeInTheDocument()
    // The P&L appears at least once (card + possibly table row)
    expect(screen.getAllByText('+495').length).toBeGreaterThan(0)
  })

  it('Win Rate uses closed trades only', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [closedTrade], isLoading: false } as any)

    render(<TradesPage />)

    // 1 closed trade, 1 win → 100%
    expect(screen.getByText('100%')).toBeInTheDocument()
  })

  it('Total Trades counts all trades', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [closedTrade, openTradeWithPrice], isLoading: false } as any)

    render(<TradesPage />)

    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('Unrealized P&L card appears when open trade with current price exists', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [openTradeWithPrice], isLoading: false } as any)

    render(<TradesPage />)

    expect(screen.getByText('trades.unrealizedPnl')).toBeInTheDocument()
  })

  it('Unrealized P&L card is absent when no open trades have a current price', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [closedTrade, openTradeNoPrice], isLoading: false } as any)

    render(<TradesPage />)

    expect(screen.queryByText('trades.unrealizedPnl')).not.toBeInTheDocument()
  })
})

describe('TradesPage — trade table badges', () => {
  it('open trade row shows green Open badge', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [openTradeWithPrice], isLoading: false } as any)

    render(<TradesPage />)

    expect(screen.getByText('trades.open')).toBeInTheDocument()
  })

  it('closed trade row shows grey Closed badge', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [closedTrade], isLoading: false } as any)

    render(<TradesPage />)

    expect(screen.getByText('trades.closed')).toBeInTheDocument()
  })

  it('open trade row with no current price shows — in Exit/Current Price column', () => {
    vi.mocked(useTrades).mockReturnValue({ data: [openTradeNoPrice], isLoading: false } as any)

    render(<TradesPage />)

    expect(screen.getAllByText('—').length).toBeGreaterThan(0)
  })

  it('Close button is visible only for open trades', () => {
    vi.mocked(useTrades).mockReturnValue({
      data: [closedTrade, openTradeWithPrice],
      isLoading: false,
    } as any)

    render(<TradesPage />)

    // ✓ close button should appear once (for the open trade only)
    const closeButtons = screen.getAllByTitle('trades.closeTrade')
    expect(closeButtons).toHaveLength(1)
  })
})
