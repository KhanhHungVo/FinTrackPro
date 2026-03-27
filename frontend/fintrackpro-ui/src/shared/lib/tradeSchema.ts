import { z } from 'zod'

// Mirrors backend regex: uppercase letters/digits, optional / or - separator.
// Valid: BTCUSDT, AAPL, EUR/USD, BTC-USDT, VIC
const symbolRegex = /^[A-Z0-9]{1,10}([/\-][A-Z0-9]{1,10})?$/

const baseSchema = z.object({
  symbol: z
    .string()
    .min(1, 'Symbol is required')
    .max(20, 'Symbol must be 20 characters or fewer')
    .regex(symbolRegex, 'Symbol must be uppercase letters/digits (e.g. BTCUSDT, AAPL, EUR/USD)'),
  direction: z.enum(['Long', 'Short']),
  entryPrice: z
    .number({ error: 'Entry price is required' })
    .positive('Entry price must be greater than zero'),
  exitPrice: z
    .number({ error: 'Exit price is required' })
    .positive('Exit price must be greater than zero'),
  positionSize: z
    .number({ error: 'Position size is required' })
    .positive('Position size must be greater than zero'),
  fees: z
    .number({ error: 'Fees must be a number' })
    .min(0, 'Fees cannot be negative'),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer').nullable(),
})

export const createTradeSchema = baseSchema
export const updateTradeSchema = baseSchema
export type CreateTradeInput = z.infer<typeof createTradeSchema>
export type UpdateTradeInput = z.infer<typeof updateTradeSchema>
