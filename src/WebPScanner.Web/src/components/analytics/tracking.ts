import './types.d.ts'

/**
 * Track a page view in Google Analytics.
 * @param path - The page path to track
 * @param title - Optional page title
 */
export function trackPageView(path: string, title?: string): void {
  if (typeof window.gtag === 'function') {
    window.gtag('event', 'page_view', {
      page_path: path,
      page_title: title,
    })
  }
}

/**
 * Track a custom event in Google Analytics.
 * @param eventName - The name of the event
 * @param eventParams - Optional event parameters
 */
export function trackEvent(eventName: string, eventParams?: Record<string, unknown>): void {
  if (typeof window.gtag === 'function') {
    window.gtag('event', eventName, eventParams)
  }
}

/**
 * Track scan form submission.
 */
export function trackScanSubmit(params: {
  hasEmail: boolean
  convertToWebP: boolean
}): void {
  trackEvent('scan_submitted', {
    has_email: params.hasEmail,
    convert_to_webp: params.convertToWebP,
  })
}

/**
 * Track report download.
 */
export function trackReportDownload(scanId: string): void {
  trackEvent('report_downloaded', {
    scan_id: scanId,
    format: 'pdf',
  })
}

/**
 * Track WebP images download.
 */
export function trackWebPDownload(scanId: string): void {
  trackEvent('webp_images_downloaded', {
    scan_id: scanId,
    format: 'zip',
  })
}

/**
 * Track when user starts a new scan after completion.
 */
export function trackScanAgain(): void {
  trackEvent('scan_again_clicked')
}

/**
 * Track retry after failure.
 */
export function trackRetry(errorMessage?: string): void {
  trackEvent('retry_scan_clicked', {
    previous_error: errorMessage,
  })
}

/**
 * Track theme changes.
 */
export function trackThemeChange(theme: string): void {
  trackEvent('theme_changed', { theme })
}

/**
 * Track external link clicks.
 */
export function trackExternalLink(url: string, label?: string): void {
  trackEvent('external_link_clicked', {
    url,
    link_label: label,
  })
}

/**
 * Track error recovery actions.
 */
export function trackErrorRecovery(action: 'retry' | 'go_home'): void {
  trackEvent('error_recovery', { action })
}
