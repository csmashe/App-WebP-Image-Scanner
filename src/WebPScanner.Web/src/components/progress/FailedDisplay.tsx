import { motion } from 'framer-motion'
import { XCircle, RotateCcw, AlertTriangle } from 'lucide-react'
import { useScanStore } from '../../store/scanStore'
import { Button } from '../ui/button'

interface FailedDisplayProps {
  onReset: () => void
}

/** Truncate URL for display */
function truncateUrl(url: string, maxLength: number = 40): string {
  if (url.length <= maxLength) return url
  return url.substring(0, maxLength - 3) + '...'
}

export function FailedDisplay({ onReset }: FailedDisplayProps) {
  const { targetUrl, errorMessage, connectionError } = useScanStore()

  // Use connection error if no specific error message
  const displayError = errorMessage || connectionError || 'An unexpected error occurred during the scan.'

  return (
    <div className="space-y-6">
      {/* Error header */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/20 transition-colors duration-300"
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{ type: 'spring', stiffness: 200, damping: 10 }}
        >
          <XCircle className="h-8 w-8 text-red-600 dark:text-red-400" />
        </motion.div>
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white transition-colors duration-300">
          Scan Failed
        </h3>
        {targetUrl && (
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300" title={targetUrl}>
            {truncateUrl(targetUrl)}
          </p>
        )}
      </div>

      {/* Error message */}
      <div className="rounded-xl bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 p-4 transition-colors duration-300">
        <div className="flex items-start gap-3">
          <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400 mt-0.5 flex-shrink-0" />
          <div>
            <p className="text-sm text-slate-900 dark:text-white font-medium transition-colors duration-300">
              What went wrong:
            </p>
            <p className="text-sm text-red-700 dark:text-red-300 mt-1 transition-colors duration-300">
              {displayError}
            </p>
          </div>
        </div>
      </div>

      {/* Common issues */}
      <div className="rounded-xl bg-slate-100 dark:bg-slate-800/30 p-4 transition-colors duration-300">
        <p className="text-sm text-slate-600 dark:text-slate-400 mb-3 transition-colors duration-300">
          Common reasons for scan failures:
        </p>
        <ul className="space-y-2 text-sm text-slate-500 dark:text-slate-500">
          <li className="flex items-center gap-2">
            <div className="h-1 w-1 rounded-full bg-slate-400 dark:bg-slate-600" />
            Website requires authentication
          </li>
          <li className="flex items-center gap-2">
            <div className="h-1 w-1 rounded-full bg-slate-400 dark:bg-slate-600" />
            Website blocked automated requests
          </li>
          <li className="flex items-center gap-2">
            <div className="h-1 w-1 rounded-full bg-slate-400 dark:bg-slate-600" />
            Connection timeout or network issues
          </li>
          <li className="flex items-center gap-2">
            <div className="h-1 w-1 rounded-full bg-slate-400 dark:bg-slate-600" />
            Invalid URL or website not accessible
          </li>
        </ul>
      </div>

      {/* Actions */}
      <div className="flex justify-center">
        <Button
          onClick={onReset}
          className="gap-2"
        >
          <RotateCcw className="h-4 w-4" />
          Try Again
        </Button>
      </div>
    </div>
  )
}
