import { useRef, useCallback } from 'react'

type MutateOptions<TData, TError, TVariables, TContext> = Parameters<
  (variables: TVariables, options?: {
    onSuccess?: (data: TData, variables: TVariables, context: TContext) => void
    onError?: (error: TError, variables: TVariables, context: TContext | undefined) => void
    onSettled?: (data: TData | undefined, error: TError | null, variables: TVariables, context: TContext | undefined) => void
  }) => void
>[1]

/**
 * Wraps a React Query `mutate` function so that concurrent calls for the same
 * `id` are ignored — preventing duplicate API calls on rapid multi-clicks.
 *
 * Usage:
 *   const { mutate: deleteTx } = useDeleteTransaction()
 *   const guardedDelete = useGuardedMutation(deleteTx)
 *   // guardedDelete(id, options) — subsequent calls with the same id are no-ops
 *   // until the first call settles (success or error).
 *
 * isPending(id) — returns true while a call for that id is in flight,
 * useful for disabling buttons.
 */
export function useGuardedMutation<TData, TError, TVariables extends string, TContext = unknown>(
  mutate: (variables: TVariables, options?: MutateOptions<TData, TError, TVariables, TContext>) => void,
) {
  const inFlight = useRef<Set<string>>(new Set())

  const guarded = useCallback(
    (id: TVariables, options?: MutateOptions<TData, TError, TVariables, TContext>) => {
      if (inFlight.current.has(id)) return
      inFlight.current.add(id)
      mutate(id, {
        ...options,
        onSettled(data, error, variables, context) {
          inFlight.current.delete(id)
          options?.onSettled?.(data, error, variables, context)
        },
      })
    },
    [mutate],
  )

  const isPending = useCallback((id: string) => inFlight.current.has(id), [])

  return { guarded, isPending }
}
