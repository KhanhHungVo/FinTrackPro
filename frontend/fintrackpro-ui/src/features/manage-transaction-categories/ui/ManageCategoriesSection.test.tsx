import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { ManageCategoriesSection } from './ManageCategoriesSection'

const mockDelete = vi.fn()

const userCategory = {
  id: 'cat-u1',
  slug: 'gaming',
  labelEn: 'Gaming',
  labelVi: 'Trò chơi',
  icon: '🎮',
  type: 'Expense' as const,
  isSystem: false,
  sortOrder: 10,
}

const systemCategory = {
  id: 'cat-s1',
  slug: 'food_beverage',
  labelEn: 'Food & Beverage',
  labelVi: 'Ăn uống',
  icon: '🍜',
  type: 'Expense' as const,
  isSystem: true,
  sortOrder: 1,
}

vi.mock('@/entities/transaction-category', () => ({
  useTransactionCategories: vi.fn(),
  useDeleteTransactionCategory: () => ({ mutate: mockDelete }),
}))

vi.mock('@/features/locale', () => ({
  useLocaleStore: () => 'en',
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string) => k }),
}))

vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

// Mock CategoryFormModal to avoid full render in these tests
vi.mock('./CategoryFormModal', () => ({
  CategoryFormModal: ({ open }: { open: boolean }) =>
    open ? <div data-testid="category-modal" /> : null,
}))

import { useTransactionCategories } from '@/entities/transaction-category'

beforeEach(() => {
  vi.clearAllMocks()
})

describe('ManageCategoriesSection', () => {
  it('shows empty state when no user categories', () => {
    vi.mocked(useTransactionCategories).mockReturnValue({
      data: [systemCategory],
      isLoading: false,
    } as unknown as ReturnType<typeof useTransactionCategories>)

    render(<ManageCategoriesSection />)

    expect(screen.getByText('transactionCategories.emptyTitle')).toBeInTheDocument()
    expect(screen.getByText('transactionCategories.emptyHint')).toBeInTheDocument()
  })

  it('renders user category row and hides system categories', () => {
    vi.mocked(useTransactionCategories).mockReturnValue({
      data: [systemCategory, userCategory],
      isLoading: false,
    } as unknown as ReturnType<typeof useTransactionCategories>)

    render(<ManageCategoriesSection />)

    expect(screen.getByText('Gaming')).toBeInTheDocument()
    expect(screen.queryByText('Food & Beverage')).not.toBeInTheDocument()
  })

  it('delete button calls deleteCategory with correct id', () => {
    vi.mocked(useTransactionCategories).mockReturnValue({
      data: [userCategory],
      isLoading: false,
    } as unknown as ReturnType<typeof useTransactionCategories>)

    render(<ManageCategoriesSection />)

    fireEvent.click(screen.getByRole('button', { name: /common\.delete/i }))

    expect(mockDelete).toHaveBeenCalledWith('cat-u1', expect.any(Object))
  })

  it('edit button opens modal with the category', () => {
    vi.mocked(useTransactionCategories).mockReturnValue({
      data: [userCategory],
      isLoading: false,
    } as unknown as ReturnType<typeof useTransactionCategories>)

    render(<ManageCategoriesSection />)

    expect(screen.queryByTestId('category-modal')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: /common\.edit/i }))
    expect(screen.getByTestId('category-modal')).toBeInTheDocument()
  })

  it('New category button opens modal in create mode', () => {
    vi.mocked(useTransactionCategories).mockReturnValue({
      data: [],
      isLoading: false,
    } as unknown as ReturnType<typeof useTransactionCategories>)

    render(<ManageCategoriesSection />)

    // Click either the header button or the empty-state button
    fireEvent.click(screen.getAllByRole('button', { name: /transactionCategories\.newCategory|transactionCategories\.createFirst/i })[0])
    expect(screen.getByTestId('category-modal')).toBeInTheDocument()
  })
})
