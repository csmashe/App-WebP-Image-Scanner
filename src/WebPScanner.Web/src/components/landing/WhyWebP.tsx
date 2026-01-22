import { motion } from 'framer-motion'
import { Zap, Globe2, TrendingUp, CheckCircle2 } from 'lucide-react'

const stats = [
  {
    value: '26-34%',
    label: 'Smaller than PNG',
    description: 'WebP lossless images are significantly smaller than PNG.',
  },
  {
    value: '25-34%',
    label: 'Smaller than JPEG',
    description: 'WebP lossy images are notably smaller than comparable JPEG images.',
  },
  {
    value: '97%+',
    label: 'Browser Support',
    description: 'WebP is supported by all major browsers including Chrome, Firefox, Safari, and Edge.',
  },
]

const benefits = [
  {
    icon: Zap,
    title: 'Faster Page Loads',
    description: 'Smaller images mean faster downloads and improved user experience.',
  },
  {
    icon: Globe2,
    title: 'Lower Bandwidth',
    description: 'Reduce server costs and help users on limited data plans.',
  },
  {
    icon: TrendingUp,
    title: 'Better SEO',
    description: 'Page speed is a ranking factor. Faster sites rank higher.',
  },
]

export function WhyWebP() {
  return (
    <section className="py-20 px-4 sm:px-6 lg:px-8 bg-gradient-to-b from-slate-50/80 via-white to-slate-100/50 dark:from-slate-950/60 dark:via-slate-950/90 dark:to-slate-900/40 transition-colors duration-300">
      <div className="mx-auto max-w-6xl">
        <motion.div
          className="text-center mb-16"
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.5 }}
        >
          <span className="text-xs uppercase tracking-[0.35em] text-slate-500 dark:text-slate-400">Why WebP</span>
          <h2 className="text-3xl font-bold text-slate-900 dark:text-white mb-4 transition-colors duration-300">
            Modern compression, measurable wins.
          </h2>
          <p className="text-slate-600 dark:text-slate-400 max-w-2xl mx-auto transition-colors duration-300">
            WebP delivers higher quality images at smaller sizes, helping teams ship faster experiences without sacrificing design.
          </p>
        </motion.div>

        <div className="grid grid-cols-1 lg:grid-cols-[1.1fr_0.9fr] gap-10 items-start">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {stats.map((stat, index) => (
              <motion.div
                key={stat.label}
                className="rounded-2xl border border-slate-200/70 dark:border-slate-800/70 bg-white/80 dark:bg-slate-900/50 p-6 text-center transition-colors duration-300"
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.5, delay: index * 0.1 }}
              >
                <div className="text-4xl font-bold bg-gradient-to-r from-[#883043] to-[#8B3A42] dark:from-[#c9787f] dark:to-[#d49ca2] bg-clip-text text-transparent mb-2">
                  {stat.value}
                </div>
                <div className="text-lg font-semibold text-slate-900 dark:text-white mb-1 transition-colors duration-300">
                  {stat.label}
                </div>
                <p className="text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
                  {stat.description}
                </p>
              </motion.div>
            ))}
          </div>

          <div className="rounded-3xl border border-slate-200/70 dark:border-slate-800/60 bg-white/80 dark:bg-slate-900/50 p-8 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-6">What improves when you switch</h3>
            <div className="space-y-5">
              {benefits.map((benefit, index) => (
                <motion.div
                  key={benefit.title}
                  className="flex gap-4"
                  initial={{ opacity: 0, x: -20 }}
                  whileInView={{ opacity: 1, x: 0 }}
                  viewport={{ once: true }}
                  transition={{ duration: 0.5, delay: index * 0.1 }}
                >
                  <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-[#883043]/10 border border-[#883043]/20">
                    <benefit.icon className="h-6 w-6 text-[#883043] dark:text-[#c9787f]" />
                  </div>
                  <div>
                    <h4 className="font-semibold text-slate-900 dark:text-white mb-1 transition-colors duration-300">
                      {benefit.title}
                    </h4>
                    <p className="text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
                      {benefit.description}
                    </p>
                  </div>
                </motion.div>
              ))}
            </div>
            <motion.div
              className="mt-10 flex items-center gap-2 text-sm text-slate-500"
              initial={{ opacity: 0 }}
              whileInView={{ opacity: 1 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: 0.3 }}
            >
              <CheckCircle2 className="h-4 w-4 text-green-500" />
              Supported by Chrome, Firefox, Safari, Edge, and Opera
            </motion.div>
          </div>
        </div>
      </div>
    </section>
  )
}
