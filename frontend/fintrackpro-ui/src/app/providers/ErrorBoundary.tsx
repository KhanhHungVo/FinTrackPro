import { Component, type ReactNode } from 'react'
import { ErrorPage } from '@/shared/ui/ErrorPage'

interface State {
  hasError: boolean
}

export class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  render() {
    if (this.state.hasError) {
      return (
        <ErrorPage
          title="Unexpected error"
          message="An unexpected error occurred. Refresh the page or return to Dashboard."
          onRetry={() => {
            this.setState({ hasError: false })
            window.location.reload()
          }}
        />
      )
    }
    return this.props.children
  }
}
