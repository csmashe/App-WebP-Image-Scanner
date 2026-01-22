import { useCallback, useState, useEffect } from 'react'
import { ThemeProvider } from './context/ThemeProvider'
import { ErrorBoundary } from './components/error/ErrorBoundary'
import { ToastContainer } from './components/ui/toast'
import { Header } from './components/landing/Header'
import { Hero } from './components/landing/Hero'
import { ScanForm } from './components/landing/ScanForm'
import { HowItWorks } from './components/landing/HowItWorks'
import { ReportPreview } from './components/landing/ReportPreview'
import { WhyWebP } from './components/landing/WhyWebP'
import { Footer } from './components/landing/Footer'
import { ScanProgress } from './components/progress'
import { TermsOfService } from './components/legal'
import { useScanStore } from './store/scanStore'
import { useScanProgress } from './hooks/useScanProgress'
import { useNetworkStatus } from './hooks/useNetworkStatus'
import { apiFetch, ApiRequestError } from './lib/api'
import { toast } from './store/toastStore'
import type { ScanResponse } from './types/scan'
import './App.css'

type AppPage = 'home' | 'terms'

function AppContent() {
  const { viewState, startScan, reset, setConnectionError } = useScanStore()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [currentPage, setCurrentPage] = useState<AppPage>('home')

  // Initialize SignalR hook (handles connection based on store state)
  useScanProgress()

  // Monitor network status
  const { isOnline } = useNetworkStatus()

  // Handle browser navigation (back/forward buttons)
  useEffect(() => {
    const handlePopState = () => {
      // Normalize path by removing trailing slash for consistent matching
      const path = window.location.pathname.replace(/\/$/, '') || '/'
      if (path === '/terms') {
        setCurrentPage('terms')
      } else {
        setCurrentPage('home')
      }
    }

    handlePopState()

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const navigateTo = useCallback((page: AppPage) => {
    const path = page === 'terms' ? '/terms' : '/'
    window.history.pushState({}, '', path)
    setCurrentPage(page)
    window.scrollTo(0, 0)
  }, [])

  const handleSubmit = useCallback(async (url: string, email: string, convertToWebP: boolean) => {
    if (!isOnline) {
      toast.error('No internet connection', 'Please check your connection and try again.')
      return
    }

    setIsSubmitting(true)

    try {
      const data = await apiFetch<ScanResponse>('/api/scan', {
        method: 'POST',
        body: JSON.stringify({ url, email, convertToWebP }),
      })

      toast.success('Scan submitted!', 'Your scan has been queued.')
      startScan(data.scanId, url, email, data.queuePosition, convertToWebP)
    } catch (error) {
      console.error('Submit error:', error)

      if (error instanceof ApiRequestError) {
        if (error.isRateLimited) {
          toast.error('Rate limited', 'Too many requests. Please wait a moment before trying again.')
        } else if (error.isNetworkError) {
          toast.error('Connection error', error.message)
        } else if (error.isValidationError) {
          toast.error('Invalid request', error.message)
        } else {
          toast.error('Submission failed', error.message)
        }
        setConnectionError(error.message)
      } else {
        const message = error instanceof Error ? error.message : 'An unexpected error occurred'
        toast.error('Submission failed', message)
        setConnectionError(message)
      }
    } finally {
      setIsSubmitting(false)
    }
  }, [isOnline, startScan, setConnectionError])

  const handleReset = useCallback(() => {
    reset()
  }, [reset])

  const showProgress = viewState !== 'idle'

  if (currentPage === 'terms') {
    return (
      <div className="min-h-screen flex flex-col transition-colors duration-300">
        <Header />
        <main className="flex-1">
          <TermsOfService onBack={() => navigateTo('home')} />
        </main>
        <Footer onNavigate={navigateTo} />
        <ToastContainer />
      </div>
    )
  }

  return (
    <div className="min-h-screen flex flex-col transition-colors duration-300">
      <Header />

      <main className="flex-1">
        <Hero />
        {showProgress ? (
          <ScanProgress onReset={handleReset} />
        ) : (
          <ScanForm onSubmit={handleSubmit} isSubmitting={isSubmitting} onNavigate={navigateTo} />
        )}
        <HowItWorks />
        <ReportPreview />
        <WhyWebP />
      </main>

      <Footer onNavigate={navigateTo} />
      <ToastContainer />
    </div>
  )
}

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider defaultTheme="system">
        <AppContent />
      </ThemeProvider>
    </ErrorBoundary>
  )
}

export default App
