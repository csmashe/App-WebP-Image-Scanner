import { useState, useEffect, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { useStatsUpdates } from '../../hooks/useStatsUpdates'
import { AnimatedCounter, AnimatedFormattedValue } from '../common/AnimatedCounter'
import type { AggregateStats } from '../../types/scan'

interface DisplayCategory {
  label: string
  width: number // Percentage as number for animation
  percent: number
}

export function Hero() {
  const [stats, setStats] = useState<AggregateStats | null>(null)
  const [loading, setLoading] = useState(true)

  // Handle stats update from SignalR
  const handleStatsUpdate = useCallback((newStats: AggregateStats) => {
    console.log('Stats update received:', newStats)
    setStats(newStats)
  }, [])

  // Connect to SignalR for real-time stats updates
  const { connected } = useStatsUpdates(handleStatsUpdate)

  // Initial fetch of stats
  useEffect(() => {
    async function fetchStats() {
      try {
        const response = await fetch('/api/scan/stats')
        if (response.ok) {
          const data = await response.json()
          setStats(data)
        }
      } catch (error) {
        console.error('Failed to fetch aggregate stats:', error)
      } finally {
        setLoading(false)
      }
    }

    fetchStats()
  }, [])

  // Calculate total savings across all categories for percentage calculation
  const totalCategorySavings = stats?.topCategories?.reduce((sum, cat) => sum + cat.totalSavingsBytes, 0) || 1

  const displayCategories: DisplayCategory[] = stats?.topCategories?.length
    ? stats.topCategories.slice(0, 3).map(cat => {
        const percentOfTotal = Math.round((cat.totalSavingsBytes / totalCategorySavings) * 100)
        return {
          label: cat.category,
          width: percentOfTotal,
          percent: percentOfTotal,
        }
      })
    : []

  const hasStats = stats && stats.totalScans > 0

  return (
    <section className="relative overflow-hidden pt-32 pb-20">
      <div className="absolute inset-0 pointer-events-none">
        <div className="hero-grid absolute inset-0 opacity-60" />
        <motion.div
          className="absolute -top-24 -left-32 h-[420px] w-[420px] rounded-full bg-gradient-to-br from-[#883043]/35 via-[#8B3A42]/25 to-transparent blur-3xl floating-orb"
          animate={{
            x: [0, 30, 0],
            y: [0, 20, 0],
          }}
          transition={{
            duration: 9,
            repeat: Infinity,
            ease: 'easeInOut',
          }}
        />
        <motion.div
          className="absolute bottom-0 right-0 h-[480px] w-[480px] rounded-full bg-gradient-to-br from-indigo-500/25 via-purple-500/15 to-transparent blur-3xl"
          animate={{
            x: [0, -40, 0],
            y: [0, -35, 0],
          }}
          transition={{
            duration: 11,
            repeat: Infinity,
            ease: 'easeInOut',
            delay: 1,
          }}
        />
      </div>

      <div className="relative z-10 mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="grid items-center gap-12 lg:grid-cols-[1.1fr_0.9fr]">
          <div>
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6 }}
            >
              <span className="inline-flex items-center gap-2 rounded-full border border-white/20 bg-white/10 px-4 py-1.5 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600 dark:text-slate-200 mb-6">
                <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
                Open source & self-hostable
              </span>
            </motion.div>

            <motion.h1
              className="text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight text-slate-900 dark:text-white mb-6 transition-colors duration-300"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.1 }}
            >
              Scan your site for{' '}
              <span className="bg-gradient-to-r from-[#883043] via-[#8B3A42] to-indigo-500 dark:from-[#c9787f] dark:via-[#d49ca2] dark:to-indigo-300 bg-clip-text text-transparent">
                image savings
              </span>{' '}
              in minutes.
            </motion.h1>

            <motion.p
              className="text-lg sm:text-xl text-slate-600 dark:text-slate-400 max-w-2xl mb-8 transition-colors duration-300"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.2 }}
            >
              Discover every non-WebP image, quantify wasted bytes, and export a polished PDF report. Run it on our hosted instance or deploy it internally for your team.
            </motion.p>

            <motion.div
              className="flex flex-wrap items-center gap-3 text-sm text-slate-600 dark:text-slate-300"
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.3 }}
            >
              {['Chrome DevTools analysis', 'Auto WebP conversion', 'Automated PDF delivery'].map((item) => (
                <span
                  key={item}
                  className="inline-flex items-center gap-2 rounded-full border border-slate-200/60 dark:border-slate-700/60 bg-white/60 dark:bg-slate-900/40 px-4 py-2 backdrop-blur"
                >
                  <span className="h-1.5 w-1.5 rounded-full bg-[#883043]" />
                  {item}
                </span>
              ))}
            </motion.div>
          </div>

          <motion.div
            className="relative"
            initial={{ opacity: 0, y: 30 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.4 }}
          >
            <div className="glass-panel rounded-3xl border border-white/20 dark:border-slate-800/60 bg-white/70 dark:bg-slate-950/60 p-6 shadow-2xl">
              <div className="flex items-center justify-between text-sm text-slate-500 dark:text-slate-300">
                <span className="font-semibold text-slate-700 dark:text-slate-200">
                  {hasStats ? 'All-time statistics' : 'Statistics'}
                </span>
                <div className="flex items-center gap-2">
                  {connected && (
                    <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" title="Live updates active" />
                  )}
                  {hasStats && (
                    <span className="rounded-full bg-emerald-500/10 px-3 py-1 text-emerald-500">
                      <AnimatedCounter value={stats.totalScans} /> scans
                    </span>
                  )}
                  {loading && (
                    <span className="rounded-full bg-slate-500/10 px-3 py-1 text-slate-500">
                      Loading...
                    </span>
                  )}
                  {!loading && !hasStats && (
                    <span className="rounded-full bg-amber-500/10 px-3 py-1 text-amber-500">
                      No scans yet
                    </span>
                  )}
                </div>
              </div>

              <div className="mt-6 grid grid-cols-2 gap-4">
                <div className="rounded-2xl border border-slate-200/60 dark:border-slate-800/60 bg-white/60 dark:bg-slate-900/40 p-4">
                  <p className="text-xs uppercase tracking-wide text-slate-500">Pages crawled</p>
                  <p className="mt-2 text-2xl font-semibold text-slate-900 dark:text-white">
                    {hasStats ? <AnimatedCounter value={stats.totalPagesCrawled} /> : '—'}
                  </p>
                </div>
                <div className="rounded-2xl border border-slate-200/60 dark:border-slate-800/60 bg-white/60 dark:bg-slate-900/40 p-4">
                  <p className="text-xs uppercase tracking-wide text-slate-500">Images found</p>
                  <p className="mt-2 text-2xl font-semibold text-slate-900 dark:text-white">
                    {hasStats ? <AnimatedCounter value={stats.totalImagesFound} /> : '—'}
                  </p>
                </div>
                <div className="rounded-2xl border border-slate-200/60 dark:border-slate-800/60 bg-white/60 dark:bg-slate-900/40 p-4">
                  <p className="text-xs uppercase tracking-wide text-slate-500">Savings found</p>
                  <p className="mt-2 text-2xl font-semibold text-slate-900 dark:text-white">
                    {hasStats ? <AnimatedFormattedValue value={stats.totalSavingsFormatted} /> : '—'}
                  </p>
                </div>
                <div className="rounded-2xl border border-slate-200/60 dark:border-slate-800/60 bg-white/60 dark:bg-slate-900/40 p-4">
                  <p className="text-xs uppercase tracking-wide text-slate-500">Avg savings</p>
                  <p className="mt-2 text-2xl font-semibold text-slate-900 dark:text-white">
                    {hasStats ? (
                      <>
                        <AnimatedCounter value={Math.round(stats.averageSavingsPercent)} />%
                      </>
                    ) : '—'}
                  </p>
                </div>
              </div>

              <AnimatePresence mode="wait">
                {displayCategories.length > 0 && (
                  <motion.div
                    key="categories"
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: 'auto' }}
                    exit={{ opacity: 0, height: 0 }}
                    transition={{ duration: 0.3 }}
                    className="mt-6 rounded-2xl border border-slate-200/60 dark:border-slate-800/60 bg-white/60 dark:bg-slate-900/40 p-4"
                  >
                    <div className="flex items-center justify-between text-xs text-slate-500">
                      <span>Top categories by savings</span>
                      <span>Top {displayCategories.length}</span>
                    </div>
                    <div className="mt-4 space-y-3">
                      {displayCategories.map((row) => (
                        <div key={row.label}>
                          <div className="flex items-center justify-between text-xs text-slate-500">
                            <motion.span
                              initial={{ opacity: 0 }}
                              animate={{ opacity: 1 }}
                              transition={{ duration: 0.3 }}
                            >
                              {row.label}
                            </motion.span>
                            <span className="text-emerald-500">
                              -<AnimatedCounter value={row.percent} duration={1000} />%
                            </span>
                          </div>
                          <div className="mt-2 h-2 rounded-full bg-slate-200/60 dark:bg-slate-800/60 overflow-hidden">
                            <motion.div
                              className="h-full rounded-full bg-gradient-to-r from-[#883043] via-[#8B3A42] to-indigo-500"
                              initial={{ width: 0 }}
                              animate={{ width: `${row.width}%` }}
                              transition={{ duration: 1, ease: 'easeOut' }}
                            />
                          </div>
                        </div>
                      ))}
                    </div>
                  </motion.div>
                )}

                {displayCategories.length === 0 && !loading && (
                  <motion.div
                    key="no-categories"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    className="mt-6 rounded-2xl border border-slate-200/60 dark:border-slate-800/60 bg-white/60 dark:bg-slate-900/40 p-4"
                  >
                    <div className="text-center text-sm text-slate-500 py-4">
                      <p>Run your first scan to see optimization insights!</p>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </motion.div>
        </div>
      </div>
    </section>
  )
}
