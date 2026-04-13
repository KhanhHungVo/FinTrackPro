import { useTranslation } from 'react-i18next'
import { BankTransferDetails } from './BankTransferDetails'

interface DonationModalProps {
  onClose: () => void
}

export function DonationModal({ onClose }: DonationModalProps) {
  const { t } = useTranslation()

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/50" onClick={onClose} />
      <div className="fixed left-1/2 top-1/2 z-50 w-[calc(100vw-2rem)] max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-white p-5 shadow-xl max-h-[90dvh] overflow-y-auto sm:p-6 dark:bg-[#161a25] dark:border-white/6">
        <div className="flex items-start justify-between">
          <h2 className="text-base font-semibold text-gray-900 dark:text-white">
            <span className="mr-1.5">☕</span>
            {t('donation.cta')}
          </h2>
          <button
            onClick={onClose}
            className="ml-4 text-gray-400 hover:text-gray-600 dark:text-slate-500 dark:hover:text-slate-300"
            aria-label={t('donation.dismiss')}
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <path
                d="M2 2L14 14M14 2L2 14"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          </button>
        </div>

        <BankTransferDetails />

        <button
          onClick={onClose}
          className="mt-4 w-full rounded-md border py-2 text-sm text-gray-700 hover:bg-gray-50 dark:border-white/10 dark:text-slate-300 dark:hover:bg-white/5"
        >
          {t('common.cancel')}
        </button>
      </div>
    </>
  )
}
