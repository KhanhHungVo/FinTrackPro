import { X } from 'lucide-react'
import { toast } from 'sonner'
import { useDismissSignal } from '@/entities/signal'
import { useGuardedMutation } from '@/shared/lib/useGuardedMutation'
import { useTranslation } from 'react-i18next'

interface DismissSignalButtonProps {
  signalId: string
}

export function DismissSignalButton({ signalId }: DismissSignalButtonProps) {
  const { t } = useTranslation()
  const { mutate } = useDismissSignal()
  const { guarded } = useGuardedMutation(mutate)

  return (
    <button
      type="button"
      aria-label={t('signals.dismiss')}
      className="ml-1 shrink-0 text-gray-400 hover:text-red-500 dark:text-slate-500 dark:hover:text-red-400 transition-colors"
      onClick={() =>
        guarded(signalId, {
          onError: () => toast.error(t('signals.dismissError')),
        })
      }
    >
      <X size={12} />
    </button>
  )
}
