import { motion } from 'framer-motion'
import { Scan, FileImage, Globe } from 'lucide-react'
import { useScanStore } from '../../store/scanStore'

/** Truncate URL for display */
function truncateUrl(url: string, maxLength: number = 50): string {
  if (url.length <= maxLength) return url
  return url.substring(0, maxLength - 3) + '...'
}

export function ScanningDisplay() {
  const {
    targetUrl,
    currentUrl,
    pagesScanned,
    pagesDiscovered,
    progressPercent,
    nonWebPImagesCount,
  } = useScanStore()

  return (
    <div className="space-y-6">
      {/* Header with animated scanner icon */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-100 dark:bg-green-500/20 transition-colors duration-300"
          animate={{ rotate: 360 }}
          transition={{ duration: 3, repeat: Infinity, ease: 'linear' }}
        >
          <Scan className="h-8 w-8 text-green-600 dark:text-green-400" />
        </motion.div>
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white transition-colors duration-300">
          Scanning in Progress
        </h3>
        {targetUrl && (
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300" title={targetUrl}>
            {truncateUrl(targetUrl)}
          </p>
        )}
      </div>

      {/* Progress bar */}
      <div className="space-y-2">
        <div className="flex justify-between text-sm">
          <span className="text-slate-600 dark:text-slate-400 transition-colors duration-300">Progress</span>
          <span className="text-slate-900 dark:text-white font-medium transition-colors duration-300">{progressPercent}%</span>
        </div>
        <div className="h-3 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800 transition-colors duration-300">
          <motion.div
            className="h-full bg-gradient-to-r from-green-500 to-emerald-400"
            initial={{ width: 0 }}
            animate={{ width: `${progressPercent}%` }}
            transition={{ duration: 0.5 }}
          />
        </div>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-2 gap-4">
        {/* Pages scanned */}
        <div className="rounded-xl bg-slate-100 dark:bg-slate-800/50 p-4 transition-colors duration-300">
          <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400 mb-2 transition-colors duration-300">
            <Globe className="h-4 w-4" />
            <span className="text-xs">Pages Scanned</span>
          </div>
          <div>
            <span className="text-2xl font-bold text-slate-900 dark:text-white transition-colors duration-300">{pagesScanned}</span>
            <span className="text-sm text-slate-500">/{pagesDiscovered}</span>
          </div>
        </div>

        {/* Non-WebP images found */}
        <div className="rounded-xl bg-slate-100 dark:bg-slate-800/50 p-4 transition-colors duration-300">
          <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400 mb-2 transition-colors duration-300">
            <FileImage className="h-4 w-4" />
            <span className="text-xs">Non-WebP Images</span>
          </div>
          <motion.div
            key={nonWebPImagesCount}
            initial={{ scale: 1.1 }}
            animate={{ scale: 1 }}
            transition={{ duration: 0.2 }}
          >
            <span className="text-2xl font-bold text-amber-600 dark:text-amber-400">{nonWebPImagesCount}</span>
          </motion.div>
        </div>
      </div>

      {/* Current URL */}
      {currentUrl && (
        <div className="rounded-xl bg-slate-100 dark:bg-slate-800/30 p-3 transition-colors duration-300">
          <p className="text-xs text-slate-500 mb-1">Currently scanning:</p>
          <p className="text-sm text-slate-700 dark:text-slate-300 truncate transition-colors duration-300" title={currentUrl}>
            {truncateUrl(currentUrl, 60)}
          </p>
        </div>
      )}

      {/* Scanning indicator */}
      <div className="flex items-center justify-center gap-2 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
        <motion.div
          className="flex gap-1"
          initial="initial"
          animate="animate"
        >
          {[0, 1, 2].map((i) => (
            <motion.div
              key={i}
              className="h-2 w-2 rounded-full bg-green-500"
              animate={{ opacity: [0.3, 1, 0.3] }}
              transition={{
                duration: 1,
                repeat: Infinity,
                delay: i * 0.2,
              }}
            />
          ))}
        </motion.div>
        <span className="ml-2">Analyzing pages for image optimization opportunities</span>
      </div>
    </div>
  )
}
