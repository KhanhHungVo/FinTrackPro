import ReactDOM from 'react-dom'
import { useTranslation } from 'react-i18next'
import { useEscapeKey } from '@/shared/lib/useEscapeKey'
import { cn } from '@/shared/lib/cn'

interface BottomSheetProps {
  open: boolean
  onClose: () => void
  children: React.ReactNode
}

function BottomSheet({ open, onClose, children }: BottomSheetProps) {
  const { t } = useTranslation()
  useEscapeKey(onClose, open)

  const sheet = (
    <div
      aria-modal="true"
      role="dialog"
      className={cn(
        'fixed inset-0 z-50 flex items-end',
        // Critical: when closed, the full-screen wrapper must not intercept touches
        open ? 'pointer-events-auto' : 'pointer-events-none',
      )}
    >
      {/* backdrop */}
      <div
        onClick={onClose}
        className={cn(
          'absolute inset-0 bg-black/40 transition-opacity duration-300',
          open ? 'opacity-100' : 'opacity-0',
        )}
      />
      {/* panel */}
      <div
        className={cn(
          'relative z-10 w-full rounded-t-2xl border-t border-gray-200 bg-white px-5 pb-8 pt-4 shadow-xl transition-transform duration-300 ease-out dark:border-white/10 dark:bg-[#161a25]',
          open ? 'translate-y-0' : 'translate-y-full',
        )}
      >
        <div className="mx-auto mb-4 h-1 w-10 rounded-full bg-gray-300 dark:bg-slate-600" />
        <div className="flex items-start justify-between gap-3">
          <div className="flex-1 text-base text-gray-800 dark:text-slate-200">{children}</div>
          <button
            onClick={onClose}
            aria-label={t('common.close')}
            className="flex-shrink-0 rounded p-1 text-lg leading-none text-gray-400 hover:text-gray-600 dark:text-slate-500 dark:hover:text-slate-300"
          >
            ×
          </button>
        </div>
      </div>
    </div>
  )

  return ReactDOM.createPortal(sheet, document.body)
}

export { BottomSheet }
