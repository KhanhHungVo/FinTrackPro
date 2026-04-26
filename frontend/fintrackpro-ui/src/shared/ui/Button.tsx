import { forwardRef } from 'react'
import { cn } from '@/shared/lib/cn'
import { AppSpinner } from './icons'

const variantClasses = {
  primary:
    'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 focus-visible:ring-blue-500',
  'primary-soft':
    'bg-blue-50 text-blue-700 hover:bg-blue-100 active:bg-blue-200 disabled:opacity-40 focus-visible:ring-blue-400 dark:bg-blue-500/10 dark:text-blue-400 dark:hover:bg-blue-500/20 dark:active:bg-blue-500/30',
  secondary:
    'border border-gray-200 text-gray-700 hover:bg-gray-50 active:bg-gray-100 disabled:opacity-50 focus-visible:ring-gray-400 dark:border-white/10 dark:text-slate-300 dark:hover:bg-white/5 dark:active:bg-white/10',
  ghost:
    'text-gray-500 hover:text-gray-800 hover:bg-gray-100 active:bg-gray-200 disabled:opacity-50 focus-visible:ring-gray-400 dark:text-slate-500 dark:hover:text-slate-200 dark:hover:bg-white/5 dark:active:bg-white/10',
  danger:
    'bg-red-600 text-white hover:bg-red-700 active:bg-red-800 disabled:opacity-50 focus-visible:ring-red-500',
  'danger-ghost':
    'text-gray-400 hover:text-red-600 hover:bg-red-50 active:bg-red-100 disabled:opacity-50 focus-visible:ring-red-400 dark:text-slate-500 dark:hover:text-red-400 dark:hover:bg-red-500/10 dark:active:bg-red-500/20',
} as const

const sizeClasses = {
  sm: 'h-8 px-3 text-xs rounded',
  md: 'h-9 px-4 text-sm rounded-md',
  lg: 'h-10 px-5 text-sm rounded-md',
} as const

export type ButtonVariant = keyof typeof variantClasses
export type ButtonSize = keyof typeof sizeClasses

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  loading?: boolean
  iconOnly?: boolean
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      variant = 'primary',
      size = 'md',
      loading = false,
      iconOnly = false,
      disabled,
      className,
      children,
      ...props
    },
    ref,
  ) => {
    return (
      <button
        ref={ref}
        disabled={disabled || loading}
        className={cn(
          'inline-flex items-center justify-center gap-1.5 font-medium transition-colors cursor-pointer border-0',
          'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1',
          variantClasses[variant],
          sizeClasses[size],
          iconOnly && 'px-0 aspect-square',
          (disabled || loading) && 'cursor-not-allowed',
          className,
        )}
        {...props}
      >
        {loading && <AppSpinner />}
        {children}
      </button>
    )
  },
)
Button.displayName = 'Button'

export interface IconButtonProps extends Omit<ButtonProps, 'iconOnly'> {
  'aria-label': string
}

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(
  (props, ref) => <Button ref={ref} iconOnly {...props} />,
)
IconButton.displayName = 'IconButton'
