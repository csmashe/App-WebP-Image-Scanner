import { useState, useCallback, useEffect } from 'react'
import { motion } from 'framer-motion'
import { Globe, Mail, ArrowRight, Loader2 } from 'lucide-react'
import { Input } from '../ui/input'
import { Button } from '../ui/button'
import { trackScanSubmit } from '../analytics'
import { validateUrl, validateEmail } from '../../lib/urlValidation'

type AppPage = 'home' | 'terms'

interface AppConfig {
  emailEnabled: boolean
}

interface ScanFormProps {
  onSubmit: (url: string, email: string, convertToWebP: boolean) => Promise<void>
  isSubmitting?: boolean
  onNavigate?: (page: AppPage) => void
}

export function ScanForm({ onSubmit, isSubmitting: externalIsSubmitting, onNavigate }: ScanFormProps) {
  const [url, setUrl] = useState('')
  const [email, setEmail] = useState('')
  const [convertToWebP, setConvertToWebP] = useState(false)
  const [urlError, setUrlError] = useState<string | undefined>()
  const [emailError, setEmailError] = useState<string | undefined>()
  const [internalSubmitting, setInternalSubmitting] = useState(false)
  const [config, setConfig] = useState<AppConfig>({ emailEnabled: true })

  // Fetch app config on mount
  useEffect(() => {
    fetch('/api/config')
      .then(res => res.json())
      .then((data: AppConfig) => setConfig(data))
      .catch(() => setConfig({ emailEnabled: false }))
  }, [])

  // Use external submitting state if provided, otherwise use internal
  const isSubmitting = externalIsSubmitting ?? internalSubmitting

  const handleUrlChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value
    setUrl(value)
    setUrlError(prev => prev ? validateUrl(value) : prev)
  }, [])

  const handleEmailChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value
    setEmail(value)
    setEmailError(prev => prev ? validateEmail(value) : prev)
  }, [])

  const handleUrlBlur = useCallback(() => {
    setUrlError(validateUrl(url))
  }, [url])

  const handleEmailBlur = useCallback(() => {
    setEmailError(validateEmail(email))
  }, [email])

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault()

    const newUrlError = validateUrl(url)
    const newEmailError = validateEmail(email)

    setUrlError(newUrlError)
    setEmailError(newEmailError)

    if (newUrlError || newEmailError) {
      return
    }

    setInternalSubmitting(true)
    try {
      trackScanSubmit({ hasEmail: !!email, convertToWebP })
      await onSubmit(url, email, convertToWebP)
    } finally {
      setInternalSubmitting(false)
    }
  }, [url, email, convertToWebP, onSubmit])

  const isValid = !validateUrl(url) && !validateEmail(email) && url

  return (
    <motion.section
      id="scan"
      className="relative z-10 mx-auto max-w-2xl px-4 sm:px-6 lg:px-8 -mt-8"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.6, delay: 0.3 }}
    >
      <form
        onSubmit={handleSubmit}
        className="rounded-3xl border border-slate-200/70 dark:border-slate-800/60 bg-white/80 dark:bg-slate-900/40 p-6 sm:p-8 backdrop-blur-sm shadow-2xl transition-colors duration-300"
      >
        <div className="space-y-5">
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-xs uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Start a free scan</p>
              <h2 className="text-2xl font-semibold text-slate-900 dark:text-white">Get your WebP report</h2>
            </div>
            <span className="hidden sm:inline-flex items-center rounded-full bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-500">
              Instant queueing
            </span>
          </div>
          {/* URL Input */}
          <div className="space-y-2">
            <label htmlFor="url" className="block text-sm font-medium text-slate-700 dark:text-slate-300 transition-colors duration-300">
              Website URL
            </label>
            <div className="relative">
              <Globe className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-500" />
              <Input
                id="url"
                type="url"
                placeholder="https://example.com"
                value={url}
                onChange={handleUrlChange}
                onBlur={handleUrlBlur}
                error={urlError}
                className="pl-12"
                disabled={isSubmitting}
              />
            </div>
          </div>

          {/* Email Input - only show if email is enabled */}
          {config.emailEnabled && (
            <div className="space-y-2">
              <label htmlFor="email" className="block text-sm font-medium text-slate-700 dark:text-slate-300 transition-colors duration-300">
                Email Address <span className="text-slate-400 font-normal">(optional)</span>
              </label>
              <div className="relative">
                <Mail className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-500" />
                <Input
                  id="email"
                  type="email"
                  placeholder="you@example.com"
                  value={email}
                  onChange={handleEmailChange}
                  onBlur={handleEmailBlur}
                  error={emailError}
                  className="pl-12"
                  disabled={isSubmitting}
                />
              </div>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                We'll send you a detailed PDF report with optimization recommendations.
              </p>
            </div>
          )}

          {/* Convert to WebP Checkbox */}
          <div className="flex items-start gap-3 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/30 border border-slate-200 dark:border-slate-700/50 transition-colors duration-300">
            <input
              type="checkbox"
              id="convertToWebP"
              checked={convertToWebP}
              onChange={(e) => setConvertToWebP(e.target.checked)}
              disabled={isSubmitting}
              className="mt-0.5 h-4 w-4 rounded border-slate-300 dark:border-slate-600 text-[#883043] focus:ring-[#883043] dark:focus:ring-[#c9787f] bg-white dark:bg-slate-800 cursor-pointer"
            />
            <div className="flex-1">
              <label htmlFor="convertToWebP" className="block text-sm font-medium text-slate-700 dark:text-slate-300 cursor-pointer transition-colors duration-300">
                Convert Images To WebP
              </label>
              <p className="mt-1 text-xs text-slate-500 dark:text-slate-400 transition-colors duration-300">
                Download a zip file with all images converted to WebP format. The download link expires after 6 hours.
              </p>
            </div>
          </div>

          {/* Submit Button */}
          <Button
            type="submit"
            size="lg"
            className="w-full mt-2 shadow-lg"
            disabled={!isValid || isSubmitting}
          >
            {isSubmitting ? (
              <>
                <Loader2 className="h-5 w-5 animate-spin" />
                Starting Scan...
              </>
            ) : (
              <>
                Start Free Scan
                <ArrowRight className="h-5 w-5" />
              </>
            )}
          </Button>
        </div>

        <p className="mt-4 text-center text-xs text-slate-500 dark:text-slate-500 transition-colors duration-300">
          By submitting, you agree to our{' '}
          <a
            href="/terms"
            onClick={(e) => {
              if (onNavigate) {
                e.preventDefault()
                onNavigate('terms')
              }
            }}
            className="text-[#883043] hover:text-[#6d2635] dark:text-[#c9787f] dark:hover:text-[#d49ca2] underline transition-colors"
          >
            Terms of Service
          </a>
          {config.emailEnabled && email && '. We\'ll only email you the scan report.'}
        </p>
      </form>
    </motion.section>
  )
}
