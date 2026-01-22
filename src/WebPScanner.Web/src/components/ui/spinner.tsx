import { cn } from '../../lib/utils'

interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl'
  className?: string
  label?: string
}

const sizeClasses = {
  sm: 'h-4 w-4 border-2',
  md: 'h-6 w-6 border-2',
  lg: 'h-8 w-8 border-[3px]',
  xl: 'h-12 w-12 border-4',
}

export function Spinner({ size = 'md', className, label }: SpinnerProps) {
  return (
    <div className={cn('flex items-center justify-center gap-2', className)}>
      <div
        className={cn(
          'animate-spin rounded-full border-slate-200 dark:border-slate-700 border-t-[#883043] dark:border-t-[#c9787f] transition-colors duration-300',
          sizeClasses[size]
        )}
        role="status"
        aria-label={label || 'Loading'}
      />
      {label && (
        <span className="text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
          {label}
        </span>
      )}
    </div>
  )
}

interface LoadingOverlayProps {
  visible: boolean
  label?: string
}

export function LoadingOverlay({ visible, label }: LoadingOverlayProps) {
  if (!visible) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-white/80 dark:bg-slate-950/80 backdrop-blur-sm transition-colors duration-300">
      <div className="text-center">
        <Spinner size="xl" label={label} />
      </div>
    </div>
  )
}
