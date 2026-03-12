export interface NotificationPreference {
  id: string
  channel: 'Telegram' | 'Email'
  telegramChatId: string | null
  isEnabled: boolean
}
