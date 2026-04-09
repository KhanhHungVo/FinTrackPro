import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useBankTransferStore } from '@/features/upgrade'

const DISMISSED_KEY = 'ftpro-donation-dismissed'

export function DonationFooter() {
  const { t } = useTranslation()
  const [dismissed, setDismissed] = useState(() =>
    localStorage.getItem(DISMISSED_KEY) === '1',
  )
  const openBankTransfer = useBankTransferStore((s) => s.openModal)

  if (dismissed) return null

  function handleDismiss() {
    localStorage.setItem(DISMISSED_KEY, '1')
    setDismissed(true)
  }

  return (
    <div className="w-full border-t border-amber-200 bg-amber-50 px-4 py-3">
      <div className="mx-auto flex max-w-5xl flex-col items-start justify-between gap-2 sm:flex-row sm:items-center">
        <p className="text-sm text-amber-900">
          <span className="mr-1.5">☕</span>
          <span className="font-semibold">{t('donation.message')}</span>{' '}
          <span className="text-amber-800">{t('donation.hint')}</span>
        </p>
        <div className="flex shrink-0 items-center gap-2">
          <button
            onClick={openBankTransfer}
            className="rounded-md bg-amber-500 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-amber-600 active:scale-95"
          >
            {t('donation.cta')}
          </button>
          <button
            onClick={handleDismiss}
            className="rounded p-1 text-amber-500 transition-colors hover:bg-amber-100 hover:text-amber-700"
            aria-label={t('donation.dismiss')}
          >
            <svg width="14" height="14" viewBox="0 0 14 14" fill="none" aria-hidden="true">
              <path
                d="M1 1l12 12M13 1L1 13"
                stroke="currentColor"
                strokeWidth="1.75"
                strokeLinecap="round"
              />
            </svg>
          </button>
        </div>
      </div>
    </div>
  )
}
