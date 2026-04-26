import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import {
  useTransactionCategories,
  useDeleteTransactionCategory,
  type TransactionCategory,
} from '@/entities/transaction-category'
import { useLocaleStore } from '@/features/locale'
import { CategoryFormModal } from './CategoryFormModal'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/shared/ui'

export function ManageCategoriesSection() {
  const { t } = useTranslation()
  const language = useLocaleStore((s) => s.language)
  const { data: categories, isLoading } = useTransactionCategories()
  const { mutate: deleteCategory } = useDeleteTransactionCategory()

  const [modalOpen, setModalOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState<TransactionCategory | undefined>()

  const userCategories = categories?.filter((c) => !c.isSystem) ?? []

  const getLabel = (cat: TransactionCategory) =>
    language === 'vi' ? cat.labelVi : cat.labelEn

  function openCreate() {
    setEditingCategory(undefined)
    setModalOpen(true)
  }

  function openEdit(cat: TransactionCategory) {
    setEditingCategory(cat)
    setModalOpen(true)
  }

  function handleDelete(id: string) {
    deleteCategory(id, {
      onError: () => toast.error(t('transactionCategories.deleteError')),
    })
  }

  return (
    <>
      <div className="rounded-xl border border-gray-200 bg-white p-4 dark:bg-white/4 dark:border-white/6">
        {/* Header */}
        <div className="flex items-start justify-between mb-4">
          <div>
            <p className="text-sm font-semibold text-gray-800 dark:text-slate-200">{t('settings.myCategories')}</p>
            <p className="text-xs text-gray-400 dark:text-slate-500 mt-0.5">{t('settings.myCategoriesHint')}</p>
          </div>
          <Button type="button" variant="primary" size="sm" onClick={openCreate}>
            <Plus size={12} aria-hidden="true" />
            {t('transactionCategories.newCategory')}
          </Button>
        </div>

        {/* Loading */}
        {isLoading && (
          <div className="py-6 text-center text-sm text-gray-400 dark:text-slate-500">
            {t('transactionCategories.loading')}
          </div>
        )}

        {/* Empty state */}
        {!isLoading && userCategories.length === 0 && (
          <div className="py-10 flex flex-col items-center gap-3 text-center">
            <span className="text-4xl" aria-hidden="true">🗂️</span>
            <p className="text-sm font-medium text-gray-500 dark:text-slate-400">
              {t('transactionCategories.emptyTitle')}
            </p>
            <p className="text-xs text-gray-400 dark:text-slate-500 max-w-xs">
              {t('transactionCategories.emptyHint')}
            </p>
            <Button type="button" variant="primary" size="sm" onClick={openCreate} className="mt-1">
              <Plus size={12} aria-hidden="true" />
              {t('transactionCategories.createFirst')}
            </Button>
          </div>
        )}

        {/* Category list */}
        {!isLoading && userCategories.length > 0 && (
          <div className="space-y-2">
            {userCategories.map((cat) => (
              <div
                key={cat.id}
                className="flex items-center gap-3 rounded-lg border border-gray-100 bg-white px-3 py-2.5 hover:shadow-sm transition-shadow dark:border-white/5 dark:bg-white/4"
              >
                <span className="text-xl" aria-hidden="true">{cat.icon}</span>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 dark:text-slate-200 truncate">
                    {getLabel(cat)}
                  </p>
                  <p className="text-xs text-gray-400 dark:text-slate-500 truncate">
                    {cat.labelEn} · {cat.labelVi}
                  </p>
                </div>
                <span
                  className={`shrink-0 px-2.5 py-0.5 rounded-full text-xs font-medium border ${
                    cat.type === 'Expense'
                      ? 'bg-red-50 text-red-600 border-red-200 dark:bg-red-500/10 dark:text-red-400 dark:border-red-500/20'
                      : 'bg-green-50 text-green-600 border-green-200 dark:bg-green-500/10 dark:text-green-400 dark:border-green-500/20'
                  }`}
                >
                  {cat.type === 'Expense'
                    ? t('transactionCategories.expense')
                    : t('transactionCategories.income')}
                </span>
                <div className="flex gap-1 shrink-0">
                  <Button type="button" variant="ghost" size="sm" onClick={() => openEdit(cat)}>
                    <Pencil size={12} aria-hidden="true" />
                    {t('common.edit')}
                  </Button>
                  <Button type="button" variant="danger-ghost" size="sm" onClick={() => handleDelete(cat.id)}>
                    <Trash2 size={12} aria-hidden="true" />
                    {t('common.delete')}
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <CategoryFormModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        category={editingCategory}
      />
    </>
  )
}
