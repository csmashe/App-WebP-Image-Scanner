import { motion } from 'framer-motion'
import { Link2, Search, FileText } from 'lucide-react'

const steps = [
  {
    icon: Link2,
    title: 'Enter URL',
    description: 'Provide the website URL you want to scan for image optimization opportunities.',
  },
  {
    icon: Search,
    title: 'We Scan',
    description: 'Our crawler visits every page and analyzes all images using Chrome DevTools Protocol.',
  },
  {
    icon: FileText,
    title: 'Get Report',
    description: 'Receive a detailed PDF report with potential savings and optimization recommendations.',
  },
]

export function HowItWorks() {
  return (
    <section id="how-it-works" className="py-20 px-4 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl">
        <motion.div
          className="text-center mb-12"
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.5 }}
        >
          <span className="text-xs uppercase tracking-[0.35em] text-slate-500 dark:text-slate-400">Workflow</span>
          <h2 className="text-3xl font-bold text-slate-900 dark:text-white mb-4 transition-colors duration-300">
            How It Works
          </h2>
          <p className="text-slate-600 dark:text-slate-400 max-w-2xl mx-auto transition-colors duration-300">
            Three streamlined steps to uncover images that could benefit from WebP conversion and performance wins.
          </p>
        </motion.div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {steps.map((step, index) => (
            <motion.div
              key={step.title}
              className="relative"
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.5, delay: index * 0.1 }}
            >
              {index < steps.length - 1 && (
                <div className="hidden md:block absolute top-16 left-[calc(50%+48px)] w-[calc(100%-96px)] h-px bg-gradient-to-r from-[#883043]/50 via-[#8B3A42]/40 to-transparent" />
              )}

              <div className="flex flex-col items-center text-center rounded-3xl border border-slate-200/60 dark:border-slate-800/60 bg-white/70 dark:bg-slate-900/40 p-8 shadow-lg transition-colors duration-300">
                <div className="relative mb-5">
                  <div className="flex h-20 w-20 items-center justify-center rounded-2xl bg-gradient-to-br from-white to-slate-100 dark:from-slate-900 dark:to-slate-800 border border-slate-200/70 dark:border-slate-700 shadow">
                    <step.icon className="h-9 w-9 text-[#883043] dark:text-[#c9787f]" />
                  </div>
                  <span className="absolute -top-2 -right-2 flex h-8 w-8 items-center justify-center rounded-full bg-[#883043] text-sm font-bold text-white">
                    {index + 1}
                  </span>
                </div>

                <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2 transition-colors duration-300">
                  {step.title}
                </h3>
                <p className="text-sm text-slate-600 dark:text-slate-400 leading-relaxed transition-colors duration-300">
                  {step.description}
                </p>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  )
}
