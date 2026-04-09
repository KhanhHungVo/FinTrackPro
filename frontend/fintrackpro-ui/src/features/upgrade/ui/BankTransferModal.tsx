import { useTranslation } from 'react-i18next'
import { env } from '@/shared/config/env'
import { BankTransferDetails } from '@/shared/ui/BankTransferDetails'
import { useBankTransferStore } from '../model/bankTransferStore'

export function BankTransferModal() {
  const { t } = useTranslation()
  const open = useBankTransferStore((s) => s.open)
  const closeModal = useBankTransferStore((s) => s.closeModal)

  if (!open) return null

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/50" onClick={closeModal} />
      <div className="fixed left-1/2 top-1/2 z-50 w-[calc(100vw-2rem)] max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-white p-5 shadow-xl max-h-[90dvh] overflow-y-auto sm:p-6">
        <div className="flex items-start justify-between">
          <h2 className="text-base font-semibold text-gray-900">
            {t('bankTransfer.orPayViaTransfer')}
          </h2>
          <button
            onClick={closeModal}
            className="ml-4 text-gray-400 hover:text-gray-600"
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

        {/* Admin contact notice */}
        <div className="mt-3 rounded-lg bg-amber-50 border border-amber-200 px-3 py-2 text-sm text-amber-800">
          ⚠ {t('bankTransfer.contactNotice')}
        </div>

        {/* Contact buttons */}
        <div className="mt-3 flex gap-2">
          {env.ADMIN_TELEGRAM && (
            <a
              href={`https://t.me/${env.ADMIN_TELEGRAM}`}
              target="_blank"
              rel="noopener noreferrer"
              className="flex-1 rounded-md border border-blue-400 py-2 text-center text-sm font-medium text-blue-600 hover:bg-blue-50"
            >
              {t('bankTransfer.telegramButton')}
            </a>
          )}
          {env.ADMIN_EMAIL && (
            <a
              href={`mailto:${env.ADMIN_EMAIL}`}
              className="flex-1 rounded-md border py-2 text-center text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              {t('bankTransfer.emailButton')}
            </a>
          )}
        </div>

        <BankTransferDetails showTransferAmountWithNote />

        <button
          onClick={closeModal}
          className="mt-4 w-full rounded-md border py-2 text-sm text-gray-700 hover:bg-gray-50"
        >
          {t('common.cancel')}
        </button>
      </div>
    </>
  )
}
