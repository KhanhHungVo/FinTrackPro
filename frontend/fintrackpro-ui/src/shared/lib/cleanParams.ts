/**
 * Strips undefined and empty-string values from a params object before an Axios call.
 */
export function cleanParams<T extends Record<string, unknown>>(params: T): Partial<T> {
  return Object.fromEntries(
    Object.entries(params).filter(([, v]) => v !== undefined && v !== ''),
  ) as Partial<T>
}
