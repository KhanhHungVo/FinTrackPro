import { useTranslation } from 'react-i18next'

interface ErrorPageProps {
  title?: string
  message?: string
  onRetry?: () => void
}

export function ErrorPage({
  title,
  message,
  onRetry,
}: ErrorPageProps) {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4 text-center px-4">
      <div className="text-5xl text-gray-300 dark:text-slate-600">⚠</div>
      <h1 className="text-2xl font-semibold text-gray-800 dark:text-slate-200">{title ?? t('common.error')}</h1>
      {message && <p className="text-gray-500 dark:text-slate-400 max-w-md">{message}</p>}
      <div className="flex gap-3">
        {onRetry && (
          <button
            onClick={onRetry}
            className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
          >
            {t('error.tryAgain')}
          </button>
        )}
        <a
          href="/dashboard"
          className="rounded-md border px-4 py-2 text-sm text-gray-600 hover:bg-gray-50 dark:border-white/10 dark:text-slate-300 dark:hover:bg-white/5"
        >
          {t('error.goToDashboard')}
        </a>
      </div>
    </div>
  )
}
