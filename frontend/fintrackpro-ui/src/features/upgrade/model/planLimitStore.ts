import { create } from 'zustand'

interface PlanLimitState {
  open: boolean
  feature: string
  title: string
  setLimit: (feature: string, title: string) => void
  clear: () => void
}

export const usePlanLimitStore = create<PlanLimitState>((set) => ({
  open: false,
  feature: '',
  title: '',
  setLimit: (feature, title) => set({ open: true, feature, title }),
  clear: () => set({ open: false, feature: '', title: '' }),
}))
