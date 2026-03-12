import { useState, useEffect } from 'react'
import {
  useNotificationPreference,
  useSaveNotificationPreference,
} from '@/entities/notification-preference'

export function NotificationSettingsForm() {
  const { data: pref, isLoading } = useNotificationPreference()
  const { mutate, isPending, isSuccess } = useSaveNotificationPreference()

  const [chatId, setChatId] = useState('')
  const [enabled, setEnabled] = useState(true)

  useEffect(() => {
    if (pref) {
      setChatId(pref.telegramChatId ?? '')
      setEnabled(pref.isEnabled)
    }
  }, [pref])

  if (isLoading) return <div className="animate-pulse h-32 rounded-lg bg-gray-100" />

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    mutate({ telegramChatId: chatId, isEnabled: enabled })
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4 rounded-lg border p-6 max-w-md">
      <h2 className="text-lg font-semibold">Telegram Notifications</h2>

      <div className="rounded-md bg-blue-50 border border-blue-200 p-3 text-sm text-blue-800 space-y-1">
        <p className="font-medium">Setup steps:</p>
        <ol className="list-decimal list-inside space-y-0.5">
          <li>Open Telegram and start <strong>your FinTrackPro Bot</strong></li>
          <li>Send <code>/start</code> — the bot replies with your Chat ID</li>
          <li>Paste the Chat ID below and save</li>
        </ol>
      </div>

      <div>
        <label className="block text-sm font-medium mb-1">Telegram Chat ID</label>
        <input
          type="text"
          placeholder="e.g. 123456789"
          value={chatId}
          onChange={(e) => setChatId(e.target.value)}
          required
          className="w-full rounded-md border px-3 py-2 text-sm font-mono"
        />
      </div>

      <label className="flex items-center gap-2 cursor-pointer">
        <input
          type="checkbox"
          checked={enabled}
          onChange={(e) => setEnabled(e.target.checked)}
          className="h-4 w-4 rounded"
        />
        <span className="text-sm">Enable notifications</span>
      </label>

      <button
        type="submit"
        disabled={isPending}
        className="w-full rounded-md bg-blue-600 py-2 text-sm text-white disabled:opacity-50"
      >
        {isPending ? 'Saving...' : 'Save Preferences'}
      </button>

      {isSuccess && (
        <p className="text-sm text-green-600">Preferences saved successfully.</p>
      )}
    </form>
  )
}
