import { motion, AnimatePresence } from 'framer-motion'
import { X, CheckCircle, AlertCircle, AlertTriangle, Info } from 'lucide-react'
import { useToastStore, type ToastType } from '../../store/toastStore'

const icons: Record<ToastType, React.ComponentType<{ className?: string }>> = {
  success: CheckCircle,
  error: AlertCircle,
  warning: AlertTriangle,
  info: Info,
}

const styles: Record<ToastType, { bg: string; icon: string; border: string }> = {
  success: {
    bg: 'bg-green-50 dark:bg-green-900/20',
    icon: 'text-green-500 dark:text-green-400',
    border: 'border-green-200 dark:border-green-800/50',
  },
  error: {
    bg: 'bg-red-50 dark:bg-red-900/20',
    icon: 'text-red-500 dark:text-red-400',
    border: 'border-red-200 dark:border-red-800/50',
  },
  warning: {
    bg: 'bg-yellow-50 dark:bg-yellow-900/20',
    icon: 'text-yellow-500 dark:text-yellow-400',
    border: 'border-yellow-200 dark:border-yellow-800/50',
  },
  info: {
    bg: 'bg-blue-50 dark:bg-blue-900/20',
    icon: 'text-blue-500 dark:text-blue-400',
    border: 'border-blue-200 dark:border-blue-800/50',
  },
}

export function ToastContainer() {
  const { toasts, removeToast } = useToastStore()

  return (
    <div className="fixed bottom-4 right-4 z-50 flex flex-col gap-2 max-w-sm w-full pointer-events-none">
      <AnimatePresence mode="popLayout">
        {toasts.map((toast) => {
          const Icon = icons[toast.type]
          const style = styles[toast.type]

          return (
            <motion.div
              key={toast.id}
              initial={{ opacity: 0, y: 20, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, x: 100, scale: 0.95 }}
              transition={{ type: 'spring', stiffness: 500, damping: 30 }}
              className={`
                pointer-events-auto rounded-lg border p-4 shadow-lg backdrop-blur-sm
                ${style.bg} ${style.border}
                transition-colors duration-300
              `}
            >
              <div className="flex items-start gap-3">
                <Icon className={`h-5 w-5 flex-shrink-0 mt-0.5 ${style.icon}`} />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-slate-900 dark:text-white transition-colors duration-300">
                    {toast.title}
                  </p>
                  {toast.message && (
                    <p className="mt-1 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
                      {toast.message}
                    </p>
                  )}
                </div>
                <button
                  onClick={() => removeToast(toast.id)}
                  className="flex-shrink-0 rounded-md p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 hover:bg-slate-200/50 dark:hover:bg-slate-700/50 transition-colors duration-200"
                  aria-label="Dismiss"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            </motion.div>
          )
        })}
      </AnimatePresence>
    </div>
  )
}
