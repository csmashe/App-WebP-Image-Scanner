import { useEffect } from 'react'
import './types.d.ts'

/**
 * Initialize Google Analytics with the given measurement ID.
 * Only loads if VITE_GA_MEASUREMENT_ID environment variable is set.
 */
function initializeGA(measurementId: string): void {
  // Prevent duplicate initialization
  if (document.querySelector(`script[src*="googletagmanager.com/gtag/js?id=${measurementId}"]`)) {
    return
  }

  // Load gtag.js script
  const script = document.createElement('script')
  script.async = true
  script.src = `https://www.googletagmanager.com/gtag/js?id=${measurementId}`
  document.head.appendChild(script)

  // Initialize dataLayer and gtag function
  window.dataLayer = window.dataLayer || []
  window.gtag = function gtag(...args: unknown[]) {
    window.dataLayer.push(args)
  }

  // Configure gtag with anonymized IP for privacy
  window.gtag('js', new Date())
  window.gtag('config', measurementId, {
    anonymize_ip: true,
    send_page_view: true,
  })
}

/**
 * GoogleAnalytics component that conditionally loads Google Analytics
 * when VITE_GA_MEASUREMENT_ID is configured.
 *
 * This component renders nothing visible - it only handles script loading.
 */
export function GoogleAnalytics(): null {
  useEffect(() => {
    const measurementId = import.meta.env.VITE_GA_MEASUREMENT_ID

    // Only initialize if measurement ID is configured and not empty
    if (measurementId && typeof measurementId === 'string' && measurementId.trim() !== '') {
      initializeGA(measurementId.trim())
    }
  }, [])

  return null
}

export default GoogleAnalytics
