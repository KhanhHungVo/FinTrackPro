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
  status: z.enum(['Open', 'Closed']),
  entryPrice: z
    .number({ error: 'Entry price is required' })
    .positive('Entry price must be greater than zero'),
  exitPrice: z
    .number()
    .positive('Exit price must be greater than zero')
    .nullable(),
  currentPrice: z
    .number()
    .positive('Current price must be greater than zero')
    .nullable(),
  positionSize: z
    .number({ error: 'Position size is required' })
    .positive('Position size must be greater than zero'),
  fees: z
    .number({ error: 'Fees must be a number' })
    .min(0, 'Fees cannot be negative'),
  currency: z.string().min(1).max(3),
  notes: z.string().max(1000, 'Notes must be 1000 characters or fewer').nullable(),
})

export const createTradeSchema = baseSchema.superRefine((val, ctx) => {
  if (val.status === 'Closed' && !val.exitPrice) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Exit price is required for a closed trade',
      path: ['exitPrice'],
    })
  }
})

export const updateTradeSchema = baseSchema.superRefine((val, ctx) => {
  if (val.status === 'Closed' && !val.exitPrice) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Exit price is required for a closed trade',
      path: ['exitPrice'],
    })
  }
})

export const closePositionSchema = z.object({
  exitPrice: z
    .number({ error: 'Exit price is required' })
    .positive('Exit price is required to close a position'),
  fees: z
    .number({ error: 'Fees must be a number' })
    .min(0, 'Fees cannot be negative'),
})

export type CreateTradeInput = z.infer<typeof createTradeSchema>
export type UpdateTradeInput = z.infer<typeof updateTradeSchema>
export type ClosePositionInput = z.infer<typeof closePositionSchema>
