import { useQuery } from '@tanstack/react-query'
import { apiClient } from '@/shared/api/client'

export function useExchangeRates(currencies: string[]) {
  return useQuery({
    queryKey: ['exchange-rates', currencies],
    queryFn: async () => {
      const { data } = await apiClient.get<Record<string, number>>(
        '/api/market/rates',
        { params: { currencies: currencies.join(',') } },
      )
      return data
    },
    staleTime: 1000 * 60 * 60 * 8, // 8 hours — matches server cache TTL
    enabled: currencies.length > 0,
  })
}
