import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { AddTradeForm } from './AddTradeForm'

// Minimal mocks
vi.mock('@/entities/trade', () => ({
  useCreateTrade: () => ({ mutate: vi.fn(), isPending: false }),
}))
vi.mock('@/features/locale', () => ({
  useLocaleStore: () => 'USD',
}))
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (k: string) => k,
    i18n: { language: 'en' },
  }),
}))
vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

beforeEach(() => vi.clearAllMocks())

describe('AddTradeForm status toggle', () => {
  it('shows Current Price field and hides Exit Price when Open is selected (default)', () => {
    render(<AddTradeForm />)

    expect(screen.getByPlaceholderText('trades.currentPrice')).toBeInTheDocument()
    expect(screen.queryByPlaceholderText('trades.exitPrice')).not.toBeInTheDocument()
  })

  it('shows Exit Price field and hides Current Price when Closed is selected', () => {
    render(<AddTradeForm />)

    fireEvent.click(screen.getByText('trades.closedTrade'))

    expect(screen.getByPlaceholderText('trades.exitPrice')).toBeInTheDocument()
    expect(screen.queryByPlaceholderText('trades.currentPrice')).not.toBeInTheDocument()
  })

  it('switching back to Open hides Exit Price and shows Current Price', () => {
    render(<AddTradeForm />)

    fireEvent.click(screen.getByText('trades.closedTrade'))
    fireEvent.click(screen.getByText('trades.openPosition'))

    expect(screen.getByPlaceholderText('trades.currentPrice')).toBeInTheDocument()
    expect(screen.queryByPlaceholderText('trades.exitPrice')).not.toBeInTheDocument()
  })

  it('shows validation error when Closed is selected and Exit Price is empty on submit', async () => {
    render(<AddTradeForm />)

    fireEvent.click(screen.getByText('trades.closedTrade'))

    // Fill required fields except exitPrice
    fireEvent.change(screen.getByPlaceholderText(/trades.symbol/i), { target: { value: 'BTCUSDT' } })
    fireEvent.change(screen.getByPlaceholderText('trades.entryPrice'), { target: { value: '30000' } })
    fireEvent.change(screen.getByPlaceholderText('trades.positionSize'), { target: { value: '0.1' } })

    fireEvent.click(screen.getByRole('button', { name: 'trades.addTrade' }))

    // Zod schema produces this error message for the exitPrice field
    expect(await screen.findByText('Exit price is required for a closed trade')).toBeInTheDocument()
  })
})
