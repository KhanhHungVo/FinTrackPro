import { env } from '@/shared/config/env'
import { useBankTransferStore } from '../model/bankTransferStore'

export function BankTransferModal() {
  const open = useBankTransferStore((s) => s.open)
  const closeModal = useBankTransferStore((s) => s.closeModal)

  if (!open) return null

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/50" onClick={closeModal} />
      <div className="fixed left-1/2 top-1/2 z-50 w-full max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-white p-6 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-base font-semibold text-gray-900">Pay via Bank Transfer</h2>
          <button
            onClick={closeModal}
            className="ml-4 text-gray-400 hover:text-gray-600"
            aria-label="Close"
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
          ⚠ Please contact admin before transferring so we can activate your plan promptly.
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
              Telegram
            </a>
          )}
          {env.ADMIN_EMAIL && (
            <a
              href={`mailto:${env.ADMIN_EMAIL}`}
              className="flex-1 rounded-md border py-2 text-center text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              Email admin
            </a>
          )}
        </div>

        {/* QR code */}
        {env.BANK_QR_URL && (
          <div className="mt-4 flex justify-center">
            <img
              src={env.BANK_QR_URL}
              alt="Bank transfer QR code"
              className="h-48 w-48 rounded-lg border object-contain"
            />
          </div>
        )}

        {/* Text fallback */}
        <div className="mt-3 rounded-lg bg-gray-50 px-3 py-2 text-xs text-gray-600 space-y-1">
          {env.BANK_NAME && (
            <div className="flex justify-between">
              <span className="font-medium">Bank</span>
              <span>{env.BANK_NAME}</span>
            </div>
          )}
          {env.BANK_ACCOUNT_NUMBER && (
            <div className="flex justify-between">
              <span className="font-medium">Account</span>
              <span className="font-mono">{env.BANK_ACCOUNT_NUMBER}</span>
            </div>
          )}
          {env.BANK_ACCOUNT_NAME && (
            <div className="flex justify-between">
              <span className="font-medium">Holder</span>
              <span>{env.BANK_ACCOUNT_NAME}</span>
            </div>
          )}
          <div className="flex justify-between">
            <span className="font-medium">Amount</span>
            <span>{Number(env.BANK_TRANSFER_AMOUNT).toLocaleString('vi-VN')} VND / month</span>
          </div>
          <div className="flex justify-between">
            <span className="font-medium">Note</span>
            <span className="italic">your registered email</span>
          </div>
        </div>

        <button
          onClick={closeModal}
          className="mt-4 w-full rounded-md border py-2 text-sm text-gray-700 hover:bg-gray-50"
        >
          Close
        </button>
      </div>
    </>
  )
}
