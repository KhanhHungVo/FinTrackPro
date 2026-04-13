import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Pagination } from './Pagination'

describe('Pagination', () => {
  it('renders page buttons for small page counts', () => {
    render(
      <Pagination
        page={1}
        totalPages={3}
        pageSize={20}
        onPageChange={vi.fn()}
        onPageSizeChange={vi.fn()}
      />,
    )

    expect(screen.getByText('1')).toBeInTheDocument()
    expect(screen.getByText('2')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
  })

  it('calls onPageChange when a page button is clicked', () => {
    const onPageChange = vi.fn()
    render(
      <Pagination
        page={1}
        totalPages={3}
        pageSize={20}
        onPageChange={onPageChange}
        onPageSizeChange={vi.fn()}
      />,
    )

    fireEvent.click(screen.getByText('2'))
    expect(onPageChange).toHaveBeenCalledWith(2)
  })

  it('calls onPageChange with page-1 when prev button clicked', () => {
    const onPageChange = vi.fn()
    render(
      <Pagination
        page={2}
        totalPages={3}
        pageSize={20}
        onPageChange={onPageChange}
        onPageSizeChange={vi.fn()}
      />,
    )

    fireEvent.click(screen.getByText('‹'))
    expect(onPageChange).toHaveBeenCalledWith(1)
  })

  it('disables prev button on first page', () => {
    render(
      <Pagination
        page={1}
        totalPages={3}
        pageSize={20}
        onPageChange={vi.fn()}
        onPageSizeChange={vi.fn()}
      />,
    )

    expect(screen.getByText('‹')).toBeDisabled()
  })

  it('disables next button on last page', () => {
    render(
      <Pagination
        page={3}
        totalPages={3}
        pageSize={20}
        onPageChange={vi.fn()}
        onPageSizeChange={vi.fn()}
      />,
    )

    expect(screen.getByText('›')).toBeDisabled()
  })

  it('renders ellipsis for large page counts', () => {
    render(
      <Pagination
        page={5}
        totalPages={15}
        pageSize={20}
        onPageChange={vi.fn()}
        onPageSizeChange={vi.fn()}
      />,
    )

    expect(screen.getAllByText('…').length).toBeGreaterThan(0)
  })

  it('calls onPageSizeChange when select changes', () => {
    const onPageSizeChange = vi.fn()
    render(
      <Pagination
        page={1}
        totalPages={3}
        pageSize={20}
        onPageChange={vi.fn()}
        onPageSizeChange={onPageSizeChange}
        pageSizeOptions={[10, 20, 50]}
      />,
    )

    fireEvent.change(screen.getByRole('combobox'), { target: { value: '50' } })
    expect(onPageSizeChange).toHaveBeenCalledWith(50)
  })
})
