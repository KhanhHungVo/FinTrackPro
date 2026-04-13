import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { SortableColumnHeader } from './SortableColumnHeader'

describe('SortableColumnHeader', () => {
  it('renders label and default indicator when inactive', () => {
    render(
      <SortableColumnHeader
        label="Date"
        field="date"
        currentSortBy={null}
        currentSortDir={null}
        onSort={vi.fn()}
      />,
    )

    expect(screen.getByText('Date')).toBeInTheDocument()
    expect(screen.getByText('↕')).toBeInTheDocument()
  })

  it('shows desc indicator when active desc', () => {
    render(
      <SortableColumnHeader
        label="Amount"
        field="amount"
        currentSortBy="amount"
        currentSortDir="desc"
        onSort={vi.fn()}
      />,
    )

    expect(screen.getByText('↓')).toBeInTheDocument()
  })

  it('shows asc indicator when active asc', () => {
    render(
      <SortableColumnHeader
        label="Amount"
        field="amount"
        currentSortBy="amount"
        currentSortDir="asc"
        onSort={vi.fn()}
      />,
    )

    expect(screen.getByText('↑')).toBeInTheDocument()
  })

  it('cycles null → desc on first click (inactive column)', () => {
    const onSort = vi.fn()
    render(
      <SortableColumnHeader
        label="Date"
        field="date"
        currentSortBy={null}
        currentSortDir={null}
        onSort={onSort}
      />,
    )

    fireEvent.click(screen.getByRole('button'))
    expect(onSort).toHaveBeenCalledWith('date', 'desc')
  })

  it('cycles desc → asc on second click', () => {
    const onSort = vi.fn()
    render(
      <SortableColumnHeader
        label="Date"
        field="date"
        currentSortBy="date"
        currentSortDir="desc"
        onSort={onSort}
      />,
    )

    fireEvent.click(screen.getByRole('button'))
    expect(onSort).toHaveBeenCalledWith('date', 'asc')
  })

  it('cycles asc → null on third click', () => {
    const onSort = vi.fn()
    render(
      <SortableColumnHeader
        label="Date"
        field="date"
        currentSortBy="date"
        currentSortDir="asc"
        onSort={onSort}
      />,
    )

    fireEvent.click(screen.getByRole('button'))
    expect(onSort).toHaveBeenCalledWith('date', null)
  })
})
