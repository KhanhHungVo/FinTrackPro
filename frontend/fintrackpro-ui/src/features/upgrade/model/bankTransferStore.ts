import { create } from 'zustand'

interface BankTransferState {
  open: boolean
  openModal: () => void
  closeModal: () => void
}

export const useBankTransferStore = create<BankTransferState>((set) => ({
  open: false,
  openModal: () => set({ open: true }),
  closeModal: () => set({ open: false }),
}))
