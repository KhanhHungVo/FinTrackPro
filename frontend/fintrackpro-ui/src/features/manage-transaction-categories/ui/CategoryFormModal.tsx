import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  useCreateTransactionCategory,
  useUpdateTransactionCategory,
  type TransactionCategory,
} from '@/entities/transaction-category'
import type { TransactionType } from '@/entities/transaction/model/types'

const EMOJIS = [
  // Food & drink
  '🍜', '🍕', '🍔', '🍣', '🥗', '☕', '🍺', '🥤', '🍱', '🍦',
  // Transport
  '🚗', '🚌', '✈️', '🚂', '🛵', '🚲', '⛽', '🚕',
  // Home
  '🏠', '💡', '🔧', '🛋️', '🖥️', '📦', '🧹', '🏡',
  // Health
  '💊', '🏥', '🧘', '🏃', '🩺', '🏋️',
  // Entertainment
  '🎮', '🎬', '🎵', '📚', '🎭', '🎨', '🎲', '🎤',
  // Shopping
  '🛍️', '👗', '👟', '💎', '🧴', '🛒',
  // People & family
  '👶', '🐾', '🌻', '🎓', '💼', '🏆', '🌟',
  // Income
  '💵', '📈', '🤝', '🎁', '⭐', '✨', '💰', '🏦',
]

function deriveSlug(name: string): string {
  return name.toLowerCase().replace(/\s+/g, '_').replace(/[^a-z0-9_]/g, '')
}

interface Props {
  open: boolean
  onClose: () => void
  category?: TransactionCategory
  defaultType?: TransactionType
}

export function CategoryFormModal({ open, onClose, category, defaultType = 'Expense' }: Props) {
  const { t } = useTranslation()
  const isEdit = !!category

  const [selectedType, setSelectedType] = useState<TransactionType>(
    category?.type ?? defaultType,
  )
  const [selectedIcon, setSelectedIcon] = useState(category?.icon ?? '📦')
  const [nameEn, setNameEn] = useState(category?.labelEn ?? '')
  const [nameVi, setNameVi] = useState(category?.labelVi ?? '')
  const [errors, setErrors] = useState<{ nameEn?: string; nameVi?: string }>({})

  const { mutate: createCategory, isPending: isCreating } = useCreateTransactionCategory()
  const { mutate: updateCategory, isPending: isUpdating } = useUpdateTransactionCategory()
  const isPending = isCreating || isUpdating

  // Reset form when modal opens for a different category (or fresh create)
  useEffect(() => {
    if (open) {
      setSelectedType(category?.type ?? defaultType)
      setSelectedIcon(category?.icon ?? '📦')
      setNameEn(category?.labelEn ?? '')
      setNameVi(category?.labelVi ?? '')
      setErrors({})
    }
  }, [open, category, defaultType])

  if (!open) return null

  function validate() {
    const next: typeof errors = {}
    if (!nameEn.trim()) next.nameEn = t('transactionCategories.nameEnRequired')
    if (!nameVi.trim()) next.nameVi = t('transactionCategories.nameViRequired')
    setErrors(next)
    return Object.keys(next).length === 0
  }

  function handleSubmit() {
    if (!validate()) return

    if (isEdit) {
      updateCategory(
        { id: category!.id, labelEn: nameEn.trim(), labelVi: nameVi.trim(), icon: selectedIcon },
        {
          onSuccess: onClose,
          onError: () => toast.error(t('transactionCategories.updateError')),
        },
      )
    } else {
      createCategory(
        {
          type: selectedType,
          slug: deriveSlug(nameEn.trim()),
          labelEn: nameEn.trim(),
          labelVi: nameVi.trim(),
          icon: selectedIcon,
        },
        {
          onSuccess: onClose,
          onError: () => toast.error(t('transactionCategories.createError')),
        },
      )
    }
  }

  const slug = deriveSlug(nameEn) || 'my_category'

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-40 bg-black/40"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Modal */}
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="category-modal-title"
        className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 px-4"
      >
        <div className="rounded-2xl border border-gray-100 bg-white shadow-2xl overflow-hidden dark:bg-[#161a25] dark:border-white/6">

          {/* Header */}
          <div className="flex items-start justify-between px-6 pt-6 pb-4 border-b border-gray-100 dark:border-white/6">
            <div>
              <p className="text-xs font-semibold text-blue-600 uppercase tracking-widest mb-1">
                {isEdit ? t('common.edit') : t('transactionCategories.newCategory')}
              </p>
              <h2
                id="category-modal-title"
                className="text-xl font-semibold text-gray-900 dark:text-white"
              >
                {isEdit
                  ? t('transactionCategories.editTitle')
                  : t('transactionCategories.createTitle')}
              </h2>
            </div>
            <button
              onClick={onClose}
              className="ml-4 text-gray-400 hover:text-gray-600 transition-colors dark:text-slate-500 dark:hover:text-slate-300"
              aria-label={t('common.cancel')}
            >
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </div>

          <div className="px-6 py-5 space-y-5">

            {/* Type toggle — disabled in edit mode (type is immutable) */}
            <div>
              <p className="block text-xs text-gray-500 dark:text-slate-400 font-medium mb-2">
                {t('transactionCategories.type')}
              </p>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={isEdit}
                  onClick={() => setSelectedType('Expense')}
                  className={`px-3 py-1.5 rounded-full text-sm font-medium border-[1.5px] transition-all disabled:cursor-not-allowed ${
                    selectedType === 'Expense'
                      ? 'bg-red-50 text-red-600 border-red-300'
                      : 'bg-gray-50 text-gray-500 border-gray-200 dark:bg-white/4 dark:text-slate-400 dark:border-white/6'
                  }`}
                >
                  {t('transactionCategories.expense')}
                </button>
                <button
                  type="button"
                  disabled={isEdit}
                  onClick={() => setSelectedType('Income')}
                  className={`px-3 py-1.5 rounded-full text-sm font-medium border-[1.5px] transition-all disabled:cursor-not-allowed ${
                    selectedType === 'Income'
                      ? 'bg-green-50 text-green-600 border-green-300'
                      : 'bg-gray-50 text-gray-500 border-gray-200 dark:bg-white/4 dark:text-slate-400 dark:border-white/6'
                  }`}
                >
                  {t('transactionCategories.income')}
                </button>
              </div>
            </div>

            {/* Icon picker */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <p className="text-xs text-gray-500 font-medium">
                  {t('transactionCategories.icon')}
                </p>
                {/* Live preview badge */}
                <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-gray-50 border border-gray-200 text-sm font-medium text-gray-700 dark:bg-white/4 dark:border-white/6 dark:text-slate-300">
                  <span aria-label="selected icon">{selectedIcon}</span>
                  <span className="text-gray-400">{nameEn || 'Category name'}</span>
                </span>
              </div>
              <div className="max-h-[188px] overflow-y-auto rounded-xl border border-gray-100 bg-gray-50/60 p-3 dark:border-white/5 dark:bg-white/4">
                <div className="flex flex-wrap gap-1" role="listbox" aria-label={t('transactionCategories.icon')}>
                  {EMOJIS.map((emoji) => (
                    <button
                      key={emoji}
                      type="button"
                      role="option"
                      aria-selected={emoji === selectedIcon}
                      onClick={() => setSelectedIcon(emoji)}
                      className={`w-10 h-10 flex items-center justify-center text-xl rounded-lg transition-all border-2 ${
                        emoji === selectedIcon
                          ? 'bg-blue-50 border-blue-500 scale-110 dark:bg-blue-500/15'
                          : 'border-transparent hover:bg-gray-100 hover:scale-110 dark:hover:bg-white/5'
                      }`}
                      title={emoji}
                    >
                      {emoji}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            {/* Name fields */}
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs text-gray-500 dark:text-slate-400 font-medium mb-1.5">
                  {t('transactionCategories.nameEn')}
                </label>
                <input
                  type="text"
                  value={nameEn}
                  onChange={(e) => {
                    setNameEn(e.target.value)
                    if (errors.nameEn) setErrors((prev) => ({ ...prev, nameEn: undefined }))
                  }}
                  placeholder={t('transactionCategories.namePlaceholderEn')}
                  className={`w-full rounded-md border px-3 py-2 text-sm dark:bg-slate-800 dark:border-white/10 dark:text-white ${errors.nameEn ? 'border-red-400' : ''}`}
                />
                {errors.nameEn && (
                  <p className="mt-1 text-xs text-red-500">{errors.nameEn}</p>
                )}
              </div>
              <div>
                <label className="block text-xs text-gray-500 dark:text-slate-400 font-medium mb-1.5">
                  {t('transactionCategories.nameVi')}
                </label>
                <input
                  type="text"
                  value={nameVi}
                  onChange={(e) => {
                    setNameVi(e.target.value)
                    if (errors.nameVi) setErrors((prev) => ({ ...prev, nameVi: undefined }))
                  }}
                  placeholder={t('transactionCategories.namePlaceholderVi')}
                  className={`w-full rounded-md border px-3 py-2 text-sm dark:bg-slate-800 dark:border-white/10 dark:text-white ${errors.nameVi ? 'border-red-400' : ''}`}
                />
                {errors.nameVi && (
                  <p className="mt-1 text-xs text-red-500">{errors.nameVi}</p>
                )}
              </div>
            </div>

            {/* Slug preview */}
            {!isEdit && (
              <p className="text-[11px] text-gray-400 dark:text-slate-500 font-mono -mt-2">
                {t('transactionCategories.slugPreview')}: <strong>{slug}</strong>
              </p>
            )}
          </div>

          {/* Footer */}
          <div className="px-6 pb-6 flex gap-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-md border border-gray-200 py-2.5 text-sm text-gray-700 font-medium hover:bg-gray-50 transition-colors dark:border-white/10 dark:text-slate-300 dark:hover:bg-white/5"
            >
              {t('common.cancel')}
            </button>
            <button
              type="button"
              onClick={handleSubmit}
              disabled={isPending}
              className="flex-1 rounded-md bg-blue-600 py-2.5 text-sm text-white font-medium hover:bg-blue-700 transition-colors disabled:opacity-50"
            >
              {isPending
                ? '…'
                : isEdit
                  ? t('transactionCategories.saveChanges')
                  : t('transactionCategories.createCategory')}
            </button>
          </div>

        </div>
      </div>
    </>
  )
}
