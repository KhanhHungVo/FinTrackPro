import { useTranslation } from 'react-i18next'
import { env } from '@/shared/config/env'

export function BankTransferDetails() {
  const { t } = useTranslation()

  return (
    <>
      {env.BANK_QR_URL && (
        <div className="mt-4 flex justify-center">
          <img
            src={env.BANK_QR_URL}
            alt={t('bankTransfer.bankLabel')}
            className="h-48 w-48 rounded-lg border object-contain"
          />
        </div>
      )}

      <div className="mt-3 rounded-lg bg-gray-50 px-3 py-2 text-xs text-gray-600 space-y-1">
        {env.BANK_NAME && (
          <div className="flex justify-between">
            <span className="font-medium">{t('bankTransfer.bankLabel')}</span>
            <span>{env.BANK_NAME}</span>
          </div>
        )}
        {env.BANK_ACCOUNT_NUMBER && (
          <div className="flex justify-between">
            <span className="font-medium">{t('bankTransfer.accountLabel')}</span>
            <span className="font-mono">{env.BANK_ACCOUNT_NUMBER}</span>
          </div>
        )}
        {env.BANK_ACCOUNT_NAME && (
          <div className="flex justify-between">
            <span className="font-medium">{t('bankTransfer.holderLabel')}</span>
            <span>{env.BANK_ACCOUNT_NAME}</span>
          </div>
        )}
        <div className="flex justify-between">
          <span className="font-medium">{t('bankTransfer.amountLabel')}</span>
          <span>
            {Number(env.BANK_TRANSFER_AMOUNT).toLocaleString('vi-VN')} VND /{' '}
            {t('pricing.perMonth')}
          </span>
        </div>
        <div className="flex justify-between">
          <span className="font-medium">{t('bankTransfer.noteLabel')}</span>
          <span className="italic">{t('bankTransfer.noteHint')}</span>
        </div>
      </div>
    </>
  )
}
