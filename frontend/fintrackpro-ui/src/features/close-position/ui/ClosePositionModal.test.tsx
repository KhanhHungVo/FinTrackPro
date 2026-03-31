import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { ClosePositionModal } from './ClosePositionModal'
import type { Trade } from '@/entities/trade'

vi.mock('@/entities/trade', () => ({
  useClosePosition: () => ({ mutate: vi.fn(), isPending: false }),
}))
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (k: string) => k,
    i18n: { language: 'en' },
  }),
}))
vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

const openTrade: Trade = {
  id: 'tr1',
  symbol: 'BTCUSDT',
  direction: 'Long',
  status: 'Open',
  entryPrice: 30000,
  exitPrice: null,
  currentPrice: 32000,
  positionSize: 0.1,
  fees: 0,
  currency: 'USD',
  rateToUsd: 1,
  result: 0,
  unrealizedResult: 200,
  notes: null,
  createdAt: '2026-03-01T00:00:00Z',
}

beforeEach(() => vi.clearAllMocks())

describe('ClosePositionModal', () => {
  it('renders null when trade is null', () => {
    const { container } = render(<ClosePositionModal trade={null} onClose={vi.fn()} />)

    expect(container.firstChild).toBeNull()
  })

  it('shows pre-filled read-only info when open trade is provided', () => {
    render(<ClosePositionModal trade={openTrade} onClose={vi.fn()} />)

    expect(screen.getByText('BTCUSDT')).toBeInTheDocument()
    expect(screen.getByText('30000')).toBeInTheDocument()
    expect(screen.getByText('0.1')).toBeInTheDocument()
  })

  it('shows validation error when Exit Price is empty on submit', async () => {
    render(<ClosePositionModal trade={openTrade} onClose={vi.fn()} />)

    fireEvent.click(screen.getByText('trades.closeTrade'))

    expect(await screen.findByText(/exit price is required to close a position/i)).toBeInTheDocument()
  })

  it('calls onClose when cancel is clicked', () => {
    const onClose = vi.fn()
    render(<ClosePositionModal trade={openTrade} onClose={onClose} />)

    fireEvent.click(screen.getByText('common.cancel'))

    expect(onClose).toHaveBeenCalled()
  })
})
