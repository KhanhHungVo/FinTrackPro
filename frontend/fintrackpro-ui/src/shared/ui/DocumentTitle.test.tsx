import { act, render, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import i18n from '@/shared/i18n'
import { formatDocumentTitle, resolvePageTitle } from '@/shared/lib/pageTitle'
import { DocumentTitle } from './DocumentTitle'

describe('pageTitle', () => {
  it('resolves known routes with translated titles', async () => {
    await i18n.changeLanguage('en')

    expect(resolvePageTitle('/dashboard', i18n.t)).toBe('Dashboard | FinTrackPro')
    expect(resolvePageTitle('/transactions', i18n.t)).toBe('Transactions | FinTrackPro')
    expect(resolvePageTitle('/budgets', i18n.t)).toBe('Budgets | FinTrackPro')
    expect(resolvePageTitle('/trades', i18n.t)).toBe('Trades | FinTrackPro')
    expect(resolvePageTitle('/market', i18n.t)).toBe('Market | FinTrackPro')
    expect(resolvePageTitle('/settings', i18n.t)).toBe('Settings | FinTrackPro')
    expect(resolvePageTitle('/pricing', i18n.t)).toBe('Plan & Billing | FinTrackPro')
    expect(resolvePageTitle('/about', i18n.t)).toBe('About | FinTrackPro')
    expect(resolvePageTitle('/', i18n.t)).toBe('Home | FinTrackPro')
  })

  it('uses the not found title for unmatched routes', async () => {
    await i18n.changeLanguage('en')

    expect(resolvePageTitle('/missing-page', i18n.t)).toBe('Page Not Found | FinTrackPro')
  })

  it('falls back to the app name when no page name is provided', () => {
    expect(formatDocumentTitle()).toBe('FinTrackPro')
  })
})

describe('DocumentTitle', () => {
  it('updates the document title when the route changes', async () => {
    await act(async () => {
      await i18n.changeLanguage('en')
    })

    const { unmount } = render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <DocumentTitle />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(document.title).toBe('Dashboard | FinTrackPro')
    })

    unmount()

    render(
      <MemoryRouter initialEntries={['/market']}>
        <DocumentTitle />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(document.title).toBe('Market | FinTrackPro')
    })
  })

  it('updates the document title when the language changes', async () => {
    await act(async () => {
      await i18n.changeLanguage('en')
    })

    render(
      <MemoryRouter initialEntries={['/settings']}>
        <DocumentTitle />
      </MemoryRouter>,
    )

    await waitFor(() => {
      expect(document.title).toBe('Settings | FinTrackPro')
    })

    await act(async () => {
      await i18n.changeLanguage('vi')
    })

    await waitFor(() => {
      expect(document.title).toBe('Cài đặt | FinTrackPro')
    })

    await act(async () => {
      await i18n.changeLanguage('en')
    })
  })
})
