import { motion } from 'framer-motion'
import { Clock, Users } from 'lucide-react'
import { useScanStore } from '../../store/scanStore'

/** Truncate URL for display */
function truncateUrl(url: string, maxLength: number = 40): string {
  if (url.length <= maxLength) return url
  return url.substring(0, maxLength - 3) + '...'
}

export function QueueDisplay() {
  const { targetUrl, queuePosition, totalInQueue } = useScanStore()

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-[#883043]/10 dark:bg-[#883043]/20 transition-colors duration-300"
          animate={{ scale: [1, 1.05, 1] }}
          transition={{ duration: 2, repeat: Infinity }}
        >
          <Clock className="h-8 w-8 text-[#883043] dark:text-[#c9787f]" />
        </motion.div>
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white transition-colors duration-300">
          Waiting in Queue
        </h3>
        {targetUrl && (
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300" title={targetUrl}>
            {truncateUrl(targetUrl)}
          </p>
        )}
      </div>

      {/* Queue position */}
      <div className="rounded-xl bg-slate-100 dark:bg-slate-800/50 p-4 transition-colors duration-300">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2 text-slate-600 dark:text-slate-400 transition-colors duration-300">
            <Users className="h-4 w-4" />
            <span className="text-sm">Queue Position</span>
          </div>
          <div className="text-right">
            <span className="text-2xl font-bold text-slate-900 dark:text-white transition-colors duration-300">
              #{queuePosition}
            </span>
            <span className="ml-1 text-sm text-slate-500">of {totalInQueue}</span>
          </div>
        </div>

        {/* Visual queue indicator */}
        <div className="mt-4">
          <div className="flex gap-1">
            {Array.from({ length: Math.min(totalInQueue, 10) }).map((_, i) => (
              <motion.div
                key={i}
                className={`h-2 flex-1 rounded-full transition-colors duration-300 ${
                  i < queuePosition - 1
                    ? 'bg-slate-300 dark:bg-slate-700'
                    : i === queuePosition - 1
                    ? 'bg-[#883043]'
                    : 'bg-slate-200 dark:bg-slate-700/50'
                }`}
                initial={{ scaleY: 0 }}
                animate={{ scaleY: 1 }}
                transition={{ delay: i * 0.05 }}
              />
            ))}
          </div>
          {totalInQueue > 10 && (
            <p className="mt-2 text-center text-xs text-slate-500">
              Showing first 10 of {totalInQueue} in queue
            </p>
          )}
        </div>
      </div>

      {/* Status message */}
      <div className="flex items-center justify-center gap-2 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
        <motion.div
          className="h-2 w-2 rounded-full bg-[#883043]"
          animate={{ opacity: [1, 0.5, 1] }}
          transition={{ duration: 1.5, repeat: Infinity }}
        />
        Your scan will start automatically when it's your turn
      </div>
    </div>
  )
}
