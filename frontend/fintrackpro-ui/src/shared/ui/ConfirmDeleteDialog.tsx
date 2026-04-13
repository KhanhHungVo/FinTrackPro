import { useTranslation } from 'react-i18next'

interface ConfirmDeleteDialogProps {
  open: boolean
  onConfirm: () => void
  onCancel: () => void
  description?: string
}

export function ConfirmDeleteDialog({ open, onConfirm, onCancel, description }: ConfirmDeleteDialogProps) {
  const { t } = useTranslation()

  if (!open) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={(e) => { if (e.target === e.currentTarget) onCancel() }}
    >
      <div className="w-full max-w-sm rounded-lg bg-white shadow-xl dark:bg-[#161a25]">
        <div className="flex items-center justify-between border-b px-4 py-3 dark:border-white/6">
          <h2 className="text-base font-semibold">{t('common.confirmDelete')}</h2>
          <button
            type="button"
            onClick={onCancel}
            className="text-gray-400 hover:text-gray-600 text-xl leading-none dark:text-slate-500 dark:hover:text-slate-300"
            aria-label={t('common.cancel')}
          >
            ✕
          </button>
        </div>
        <div className="space-y-4 p-4">
          <p className="text-sm text-gray-600 dark:text-slate-400">
            {description ?? t('common.confirmDeleteDescription')}
          </p>
          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onCancel}
              className="flex-1 rounded-md border py-2 text-sm text-gray-600 hover:bg-gray-50 dark:border-white/10 dark:text-slate-400 dark:hover:bg-white/5"
            >
              {t('common.cancel')}
            </button>
            <button
              type="button"
              onClick={onConfirm}
              className="flex-1 rounded-md bg-red-600 py-2 text-sm text-white hover:bg-red-700"
            >
              {t('common.delete')}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
