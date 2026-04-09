import { useBankTransferStore } from '../model/bankTransferStore'

interface Props {
  open: boolean
  onClose: () => void
}

export function StripeUnavailableModal({ open, onClose }: Props) {
  const openBankTransfer = useBankTransferStore((s) => s.openModal)

  if (!open) return null

  function handleViewQr() {
    onClose()
    openBankTransfer()
  }

  return (
    <>
      <div className="fixed inset-0 z-40 bg-black/50" onClick={onClose} />
      <div className="fixed left-1/2 top-1/2 z-50 w-full max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-white p-6 shadow-xl">
        <div className="flex items-start justify-between">
          <h2 className="text-base font-semibold text-gray-900">
            Card payment temporarily unavailable
          </h2>
          <button
            onClick={onClose}
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
        <p className="mt-2 text-sm text-gray-600">
          You can upgrade via bank transfer instead.
        </p>
        <div className="mt-5 flex flex-col gap-2">
          <button
            onClick={handleViewQr}
            className="w-full rounded-md bg-blue-600 py-2 text-sm font-medium text-white hover:bg-blue-700"
          >
            View Bank Transfer QR
          </button>
          <button
            onClick={onClose}
            className="w-full rounded-md border py-2 text-sm text-gray-700 hover:bg-gray-50"
          >
            Maybe later
          </button>
        </div>
      </div>
    </>
  )
}
