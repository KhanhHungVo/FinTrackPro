import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ProFeatureLock } from './ProFeatureLock'

vi.mock('@/entities/subscription', () => ({
  useSubscriptionStatus: vi.fn(),
}))
vi.mock('./UpgradeButton', () => ({
  UpgradeButton: () => <button>Upgrade to Pro</button>,
}))

import { useSubscriptionStatus } from '@/entities/subscription'
const mockStatus = useSubscriptionStatus as ReturnType<typeof vi.fn>

const FEATURES = [{ icon: '📈', label: 'RSI analysis' }]

describe('ProFeatureLock', () => {
  it('Pro user — children rendered, no lock overlay', () => {
    mockStatus.mockReturnValue({ data: { plan: 'Pro', isActive: true } })

    render(
      <ProFeatureLock title="Watchlist" tagline="Track symbols" features={FEATURES}>
        <span>Pro content</span>
      </ProFeatureLock>,
    )

    expect(screen.getByText('Pro content')).toBeInTheDocument()
    expect(screen.queryByText('Upgrade to Pro')).not.toBeInTheDocument()
  })

  it('Free user — lock overlay shown with title and upgrade button', () => {
    mockStatus.mockReturnValue({ data: { plan: 'Free', isActive: false } })

    render(
      <ProFeatureLock title="Watchlist" tagline="Track symbols" features={FEATURES}>
        <span>Pro content</span>
      </ProFeatureLock>,
    )

    expect(screen.getByText('Watchlist')).toBeInTheDocument()
    expect(screen.getByText('Upgrade to Pro')).toBeInTheDocument()
  })

  it('Free user — compact variant renders single-row teaser', () => {
    mockStatus.mockReturnValue({ data: { plan: 'Free', isActive: false } })

    render(
      <ProFeatureLock compact title="Signals" tagline="Get trading signals" features={[]}>
        {null}
      </ProFeatureLock>,
    )

    expect(screen.getByText('Get trading signals')).toBeInTheDocument()
    expect(screen.getByText('Upgrade to Pro')).toBeInTheDocument()
    // Full-variant title heading should not appear in compact mode
    expect(screen.queryByRole('heading')).not.toBeInTheDocument()
  })

  it('Upgrade button is rendered inside locked shell', () => {
    mockStatus.mockReturnValue({ data: { plan: 'Free', isActive: false } })

    render(
      <ProFeatureLock title="Signals" tagline="Tagline" features={[]}>
        <span>Content</span>
      </ProFeatureLock>,
    )

    expect(screen.getByText('Upgrade to Pro')).toBeInTheDocument()
  })
})
