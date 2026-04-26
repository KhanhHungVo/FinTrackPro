import { X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button, IconButton } from './Button'

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
          <IconButton
            type="button"
            variant="ghost"
            size="sm"
            onClick={onCancel}
            aria-label={t('common.cancel')}
          >
            <X size={16} aria-hidden="true" />
          </IconButton>
        </div>
        <div className="space-y-4 p-4">
          <p className="text-sm text-gray-600 dark:text-slate-400">
            {description ?? t('common.confirmDeleteDescription')}
          </p>
          <div className="flex gap-2 pt-1">
            <Button type="button" variant="secondary" size="md" onClick={onCancel} className="flex-1">
              {t('common.cancel')}
            </Button>
            <Button type="button" variant="danger" size="md" onClick={onConfirm} className="flex-1">
              {t('common.delete')}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
