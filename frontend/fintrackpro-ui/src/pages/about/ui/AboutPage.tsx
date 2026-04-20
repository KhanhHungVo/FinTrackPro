import { useState } from 'react'
import { useNavigate, Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { DonationModal } from '@/shared/ui/DonationModal'

function GitHubIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
      <path d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0 1 12 6.844a9.59 9.59 0 0 1 2.504.337c1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.02 10.02 0 0 0 22 12.017C22 6.484 17.522 2 12 2z" />
    </svg>
  )
}

function EmailIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      <rect x="2" y="4" width="20" height="16" rx="2" />
      <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7" />
    </svg>
  )
}

export function AboutPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [donationOpen, setDonationOpen] = useState(false)

  return (
    <div className="mx-auto max-w-2xl space-y-4 p-4 md:p-8">
      <button
        onClick={() => navigate(-1)}
        className="text-sm text-gray-400 dark:text-slate-500 hover:text-gray-600 dark:hover:text-slate-300 transition-colors"
      >
        {t('about.back')}
      </button>

      {/* App identity */}
      <div className="rounded-xl border bg-white p-6 dark:bg-white/4 dark:border-white/6 space-y-3">
        <div className="space-y-1.5">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{t('about.title')}</h1>
          <p className="text-sm text-blue-600 dark:text-blue-400 font-medium">{t('about.tagline')}</p>
        </div>
        <p className="text-sm text-gray-600 dark:text-slate-400 leading-relaxed">{t('about.description')}</p>

        <div className="pt-2 border-t border-gray-100 dark:border-white/6 space-y-2.5">
          <p className="text-xs text-gray-400 dark:text-slate-500">{t('about.version')} v1.0.0</p>

          <Link
            to="/pricing"
            className="flex items-center gap-1.5 text-sm text-blue-600 dark:text-blue-400 hover:underline"
          >
            {t('about.viewPricing')} →
          </Link>

          <button
            onClick={() => setDonationOpen(true)}
            className="flex items-center gap-1.5 text-sm text-gray-500 dark:text-slate-400 hover:text-gray-700 dark:hover:text-slate-300 transition-colors"
          >
            {t('about.supportCta')}
          </button>
        </div>
      </div>

      {donationOpen && <DonationModal onClose={() => setDonationOpen(false)} />}

      {/* Author */}
      <div className="rounded-xl border bg-white p-6 dark:bg-white/4 dark:border-white/6 space-y-3">
        <p className="text-xs font-medium uppercase tracking-wide text-gray-400 dark:text-slate-500">
          {t('about.builtBy')}
        </p>
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-gray-900 dark:text-white">Khanh Hung Vo</span>
          <div className="flex items-center gap-1">
            <a
              href="#"
              aria-label="GitHub"
              className="p-1.5 rounded-md text-gray-400 hover:text-gray-600 dark:text-slate-500 dark:hover:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5 transition-colors"
            >
              <GitHubIcon />
            </a>
            <a
              href="#"
              aria-label={t('about.contact')}
              className="p-1.5 rounded-md text-gray-400 hover:text-gray-600 dark:text-slate-500 dark:hover:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5 transition-colors"
            >
              <EmailIcon />
            </a>
          </div>
        </div>
      </div>
    </div>
  )
}
