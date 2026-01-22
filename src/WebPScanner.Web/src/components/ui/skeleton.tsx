import { cn } from '../../lib/utils'

interface SkeletonProps {
  className?: string
}

export function Skeleton({ className }: SkeletonProps) {
  return (
    <div
      className={cn(
        'animate-pulse rounded-md bg-slate-200 dark:bg-slate-800 transition-colors duration-300',
        className
      )}
    />
  )
}

/** Skeleton for a form card */
export function FormSkeleton() {
  return (
    <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/50 p-6 sm:p-8 space-y-4 transition-colors duration-300">
      {/* Label */}
      <div className="space-y-2">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-12 w-full" />
      </div>
      {/* Label */}
      <div className="space-y-2">
        <Skeleton className="h-4 w-28" />
        <Skeleton className="h-12 w-full" />
      </div>
      {/* Button */}
      <Skeleton className="h-12 w-full mt-2" />
      {/* Text */}
      <Skeleton className="h-4 w-3/4 mx-auto mt-4" />
    </div>
  )
}

/** Skeleton for progress display */
export function ProgressSkeleton() {
  return (
    <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/50 p-6 sm:p-8 space-y-6 transition-colors duration-300">
      {/* Icon */}
      <div className="flex justify-center">
        <Skeleton className="h-16 w-16 rounded-full" />
      </div>
      {/* Title */}
      <div className="text-center space-y-2">
        <Skeleton className="h-6 w-48 mx-auto" />
        <Skeleton className="h-4 w-32 mx-auto" />
      </div>
      {/* Progress bar */}
      <Skeleton className="h-2 w-full rounded-full" />
      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        <Skeleton className="h-16 rounded-lg" />
        <Skeleton className="h-16 rounded-lg" />
        <Skeleton className="h-16 rounded-lg" />
      </div>
    </div>
  )
}

/** Skeleton for a stats card */
export function StatCardSkeleton() {
  return (
    <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 transition-colors duration-300">
      <Skeleton className="h-4 w-20 mb-2" />
      <Skeleton className="h-8 w-16" />
    </div>
  )
}
