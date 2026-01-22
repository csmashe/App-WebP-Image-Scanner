import { Component, type ErrorInfo, type ReactNode } from 'react'
import { AlertTriangle, RotateCcw, Home } from 'lucide-react'
import { Button } from '../ui/button'

interface ErrorBoundaryProps {
  children: ReactNode
  fallback?: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
  error: Error | null
  errorInfo: ErrorInfo | null
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
    }
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('ErrorBoundary caught an error:', error, errorInfo)
    this.setState({ errorInfo })
  }

  handleReset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
    })
  }

  handleGoHome = (): void => {
    window.location.href = '/'
  }

  render(): ReactNode {
    const { hasError, error } = this.state
    const { children, fallback } = this.props

    if (hasError) {
      if (fallback) {
        return fallback
      }

      return (
        <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 p-4 transition-colors duration-300">
          <div className="max-w-md w-full rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-8 shadow-xl text-center transition-colors duration-300">
            {/* Error icon */}
            <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/20 transition-colors duration-300">
              <AlertTriangle className="h-8 w-8 text-red-600 dark:text-red-400" />
            </div>

            {/* Error message */}
            <h2 className="text-xl font-semibold text-slate-900 dark:text-white mb-2 transition-colors duration-300">
              Something went wrong
            </h2>
            <p className="text-slate-600 dark:text-slate-400 mb-4 transition-colors duration-300">
              An unexpected error occurred. Please try again.
            </p>

            {/* Error details (development only) */}
            {import.meta.env.DEV && error && (
              <div className="mb-6 rounded-lg bg-slate-100 dark:bg-slate-800 p-4 text-left transition-colors duration-300">
                <p className="text-xs font-mono text-red-600 dark:text-red-400 break-all">
                  {error.message}
                </p>
              </div>
            )}

            {/* Actions */}
            <div className="flex flex-col sm:flex-row gap-3 justify-center">
              <Button onClick={this.handleReset} variant="outline" className="gap-2">
                <RotateCcw className="h-4 w-4" />
                Try Again
              </Button>
              <Button onClick={this.handleGoHome} className="gap-2">
                <Home className="h-4 w-4" />
                Go Home
              </Button>
            </div>
          </div>
        </div>
      )
    }

    return children
  }
}
