import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { CategoryFormModal } from './CategoryFormModal'

const mockCreate = vi.fn()
const mockUpdate = vi.fn()

vi.mock('@/entities/transaction-category', () => ({
  useCreateTransactionCategory: () => ({ mutate: mockCreate, isPending: false }),
  useUpdateTransactionCategory: () => ({ mutate: mockUpdate, isPending: false }),
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k }),
}))

vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

const noop = () => {}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('CategoryFormModal — create mode', () => {
  it('renders create mode heading and create button', () => {
    render(<CategoryFormModal open onClose={noop} />)

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByText('transactionCategories.createTitle')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'transactionCategories.createCategory' })).toBeInTheDocument()
  })

  it('Expense chip is active by default', () => {
    render(<CategoryFormModal open onClose={noop} />)

    const expenseBtn = screen.getByRole('button', { name: 'transactionCategories.expense' })
    expect(expenseBtn.className).toMatch(/bg-red-50/)
  })

  it('clicking Income chip switches active state', () => {
    render(<CategoryFormModal open onClose={noop} />)

    fireEvent.click(screen.getByRole('button', { name: 'transactionCategories.income' }))

    const incomeBtn = screen.getByRole('button', { name: 'transactionCategories.income' })
    expect(incomeBtn.className).toMatch(/bg-green-50/)
  })

  it('selecting an emoji updates the preview badge', () => {
    render(<CategoryFormModal open onClose={noop} />)

    fireEvent.click(screen.getByTitle('🎮'))

    expect(screen.getByLabelText('selected icon').textContent).toBe('🎮')
  })

  it('typing EN name updates the slug preview', () => {
    render(<CategoryFormModal open onClose={noop} />)

    const enInput = screen.getByPlaceholderText('transactionCategories.namePlaceholderEn')
    fireEvent.change(enInput, { target: { value: 'Pet Care' } })

    expect(screen.getByText('pet_care')).toBeInTheDocument()
  })

  it('shows validation errors when submitting with empty names', () => {
    render(<CategoryFormModal open onClose={noop} />)

    fireEvent.click(screen.getByRole('button', { name: 'transactionCategories.createCategory' }))

    expect(screen.getByText('transactionCategories.nameEnRequired')).toBeInTheDocument()
    expect(screen.getByText('transactionCategories.nameViRequired')).toBeInTheDocument()
    expect(mockCreate).not.toHaveBeenCalled()
  })

  it('calls createCategory mutation with correct payload on valid submit', async () => {
    render(<CategoryFormModal open onClose={noop} />)

    fireEvent.change(screen.getByPlaceholderText('transactionCategories.namePlaceholderEn'), {
      target: { value: 'Gaming' },
    })
    fireEvent.change(screen.getByPlaceholderText('transactionCategories.namePlaceholderVi'), {
      target: { value: 'Trò chơi' },
    })
    fireEvent.click(screen.getByTitle('🎮'))
    fireEvent.click(screen.getByRole('button', { name: 'transactionCategories.createCategory' }))

    await waitFor(() => {
      expect(mockCreate).toHaveBeenCalledWith(
        expect.objectContaining({
          labelEn: 'Gaming',
          labelVi: 'Trò chơi',
          icon: '🎮',
          slug: 'gaming',
          type: 'Expense',
        }),
        expect.any(Object),
      )
    })
  })

  it('does not render when open=false', () => {
    render(<CategoryFormModal open={false} onClose={noop} />)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})

describe('CategoryFormModal — edit mode', () => {
  const existingCategory = {
    id: 'cat-1',
    slug: 'gaming',
    labelEn: 'Gaming',
    labelVi: 'Trò chơi',
    icon: '🎮',
    type: 'Expense' as const,
    isSystem: false,
    sortOrder: 1,
  }

  it('renders edit mode heading and pre-filled values', () => {
    render(<CategoryFormModal open onClose={noop} category={existingCategory} />)

    expect(screen.getByText('transactionCategories.editTitle')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'transactionCategories.saveChanges' })).toBeInTheDocument()
    expect(
      (screen.getByPlaceholderText('transactionCategories.namePlaceholderEn') as HTMLInputElement).value,
    ).toBe('Gaming')
    expect(
      (screen.getByPlaceholderText('transactionCategories.namePlaceholderVi') as HTMLInputElement).value,
    ).toBe('Trò chơi')
  })

  it('type chips are disabled in edit mode', () => {
    render(<CategoryFormModal open onClose={noop} category={existingCategory} />)

    expect(screen.getByRole('button', { name: 'transactionCategories.expense' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'transactionCategories.income' })).toBeDisabled()
  })

  it('calls updateCategory mutation on save', async () => {
    render(<CategoryFormModal open onClose={noop} category={existingCategory} />)

    fireEvent.change(screen.getByPlaceholderText('transactionCategories.namePlaceholderEn'), {
      target: { value: 'Gaming Updated' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'transactionCategories.saveChanges' }))

    await waitFor(() => {
      expect(mockUpdate).toHaveBeenCalledWith(
        expect.objectContaining({
          id: 'cat-1',
          labelEn: 'Gaming Updated',
          labelVi: 'Trò chơi',
          icon: '🎮',
        }),
        expect.any(Object),
      )
    })
  })
})
