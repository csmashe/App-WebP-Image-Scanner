import { motion, AnimatePresence } from 'framer-motion'
import { useScanStore } from '../../store/scanStore'
import { QueueDisplay } from './QueueDisplay'
import { ScanningDisplay } from './ScanningDisplay'
import { CompletedDisplay } from './CompletedDisplay'
import { FailedDisplay } from './FailedDisplay'

interface ScanProgressProps {
  onReset: () => void
}

export function ScanProgress({ onReset }: ScanProgressProps) {
  const { viewState, isConnecting, connectionError, email } = useScanStore()

  return (
    <motion.section
      className="relative z-10 mx-auto max-w-xl px-4 sm:px-6 lg:px-8 -mt-4"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
    >
      <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/50 p-6 sm:p-8 backdrop-blur-sm shadow-xl transition-colors duration-300">
        {/* Connection status indicator */}
        {isConnecting && (
          <div className="mb-4 flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
            <div className="h-2 w-2 animate-pulse rounded-full bg-yellow-500" />
            Connecting to progress updates...
          </div>
        )}

        {connectionError && (
          <div className="mb-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3 text-sm text-red-700 dark:text-red-400 transition-colors duration-300">
            Connection error: {connectionError}
          </div>
        )}

        <AnimatePresence mode="wait">
          {viewState === 'queued' && (
            <motion.div
              key="queued"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.3 }}
            >
              <QueueDisplay />
            </motion.div>
          )}

          {viewState === 'scanning' && (
            <motion.div
              key="scanning"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.3 }}
            >
              <ScanningDisplay />
            </motion.div>
          )}

          {viewState === 'completed' && (
            <motion.div
              key="completed"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.3 }}
            >
              <CompletedDisplay onReset={onReset} />
            </motion.div>
          )}

          {viewState === 'failed' && (
            <motion.div
              key="failed"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.3 }}
            >
              <FailedDisplay onReset={onReset} />
            </motion.div>
          )}
        </AnimatePresence>

        {/* "Feel free to close" message for active scans - only if email provided */}
        {(viewState === 'queued' || viewState === 'scanning') && email && (
          <motion.p
            className="mt-6 text-center text-xs text-slate-500 transition-colors duration-300"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.5 }}
          >
            Feel free to close this tab. We'll email you the report when it's ready.
          </motion.p>
        )}
      </div>
    </motion.section>
  )
}
