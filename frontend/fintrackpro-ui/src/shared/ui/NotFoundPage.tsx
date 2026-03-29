import { useTranslation } from 'react-i18next'

export function NotFoundPage() {
  const { t } = useTranslation()
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4 text-center px-4">
      <div className="text-6xl font-black text-gray-200">404</div>
      <h1 className="text-2xl font-semibold text-gray-800">{t('common.noData')}</h1>
      <p className="text-gray-500">The page you&apos;re looking for doesn&apos;t exist.</p>
      <a
        href="/dashboard"
        className="rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
      >
        {t('nav.dashboard')}
      </a>
    </div>
  )
}
