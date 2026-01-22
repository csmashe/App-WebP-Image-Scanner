import { motion } from 'framer-motion'
import { FileText, Download, CheckCircle, TrendingDown } from 'lucide-react'

export function ReportPreview() {
  return (
    <section className="relative bg-slate-950 py-24 overflow-hidden">
      {/* Background */}
      <div className="absolute inset-0">
        <div className="absolute inset-0 bg-gradient-to-b from-slate-950 via-slate-900 to-slate-950" />
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] rounded-full bg-[#883043]/10 blur-[150px]" />
      </div>

      <div className="relative z-10 mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="grid lg:grid-cols-2 gap-12 lg:gap-16 items-center">
          {/* Left side - Text content */}
          <motion.div
            initial={{ opacity: 0, x: -20 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.6 }}
          >
            <div className="inline-flex items-center gap-2 rounded-full border border-[#883043]/40 bg-[#883043]/10 px-3 py-1 text-xs font-medium text-[#c9787f] mb-6">
              <FileText className="h-3.5 w-3.5" />
              PDF Report
            </div>

            <h2 className="text-3xl sm:text-4xl font-bold text-white mb-6">
              Get a Detailed
              <br />
              <span className="bg-gradient-to-r from-[#c9787f] to-[#e8a5aa] bg-clip-text text-transparent">
                Optimization Report
              </span>
            </h2>

            <p className="text-slate-400 text-lg mb-8 leading-relaxed">
              Download a comprehensive PDF report with actionable insights to improve
              your website's performance.
            </p>

            {/* Features list */}
            <div className="space-y-4">
              {[
                { icon: CheckCircle, text: 'Complete list of images needing optimization' },
                { icon: TrendingDown, text: 'Estimated file size savings per image' },
                { icon: Download, text: 'Recommendations for conversion tools' },
              ].map((item, index) => (
                <motion.div
                  key={index}
                  className="flex items-center gap-3"
                  initial={{ opacity: 0, x: -20 }}
                  whileInView={{ opacity: 1, x: 0 }}
                  viewport={{ once: true }}
                  transition={{ duration: 0.4, delay: index * 0.1 }}
                >
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-[#883043]/20">
                    <item.icon className="h-4 w-4 text-[#c9787f]" />
                  </div>
                  <span className="text-slate-300">{item.text}</span>
                </motion.div>
              ))}
            </div>
          </motion.div>

          {/* Right side - Report mockup */}
          <motion.div
            className="relative"
            initial={{ opacity: 0, x: 20 }}
            whileInView={{ opacity: 1, x: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.6, delay: 0.2 }}
          >
            {/* Glow effect behind */}
            <div className="absolute -inset-4 bg-gradient-to-r from-[#883043]/20 to-[#8B3A42]/20 rounded-2xl blur-2xl" />

            {/* Report mockup */}
            <div className="relative bg-white rounded-xl shadow-2xl overflow-hidden">
              {/* Report header */}
              <div className="bg-[#883043] px-6 py-4">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="text-white font-bold text-lg">WebP Image Scanner</div>
                    <div className="text-white/70 text-sm">Website Optimization Report</div>
                  </div>
                  <div className="text-right">
                    <div className="text-white/70 text-xs">Generated</div>
                    <div className="text-white text-sm font-medium">Jan 18, 2026</div>
                  </div>
                </div>
              </div>

              {/* Report content preview */}
              <div className="p-6 space-y-4">
                {/* URL scanned */}
                <div>
                  <div className="text-xs text-slate-500 mb-1">Scanned Website</div>
                  <div className="text-slate-800 font-medium">https://example.com</div>
                </div>

                {/* Quick stats */}
                <div className="grid grid-cols-3 gap-3">
                  <div className="bg-slate-50 rounded-lg p-3 text-center">
                    <div className="text-2xl font-bold text-[#883043]">24</div>
                    <div className="text-xs text-slate-500">Pages</div>
                  </div>
                  <div className="bg-slate-50 rounded-lg p-3 text-center">
                    <div className="text-2xl font-bold text-amber-600">47</div>
                    <div className="text-xs text-slate-500">Images</div>
                  </div>
                  <div className="bg-slate-50 rounded-lg p-3 text-center">
                    <div className="text-2xl font-bold text-green-600">32%</div>
                    <div className="text-xs text-slate-500">Savings</div>
                  </div>
                </div>

                {/* Sample table */}
                <div className="border border-slate-200 rounded-lg overflow-hidden">
                  <div className="bg-[#883043] px-3 py-2 text-xs font-medium text-white">
                    Top Images by Potential Savings
                  </div>
                  <div className="divide-y divide-slate-100">
                    {[
                      { name: 'hero-banner.jpg', size: '2.4 MB', savings: '780 KB' },
                      { name: 'product-1.png', size: '1.8 MB', savings: '540 KB' },
                      { name: 'team-photo.jpg', size: '1.2 MB', savings: '360 KB' },
                    ].map((row, i) => (
                      <div key={i} className="flex items-center justify-between px-3 py-2 text-xs">
                        <span className="text-slate-700 truncate flex-1">{row.name}</span>
                        <span className="text-slate-500 w-16 text-right">{row.size}</span>
                        <span className="text-green-600 font-medium w-16 text-right">-{row.savings}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Fade effect at bottom */}
                <div className="h-12 bg-gradient-to-t from-white via-white/80 to-transparent -mb-6" />
              </div>
            </div>

            {/* Floating badges */}
            <motion.div
              className="absolute -right-4 top-8 bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 shadow-xl"
              animate={{ y: [0, -5, 0] }}
              transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
            >
              <div className="flex items-center gap-2">
                <div className="h-2 w-2 rounded-full bg-green-400" />
                <span className="text-xs text-slate-300">PDF Ready</span>
              </div>
            </motion.div>

            <motion.div
              className="absolute -left-4 bottom-16 bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 shadow-xl"
              animate={{ y: [0, 5, 0] }}
              transition={{ duration: 4, repeat: Infinity, ease: "easeInOut", delay: 1 }}
            >
              <div className="flex items-center gap-2">
                <TrendingDown className="h-4 w-4 text-green-400" />
                <span className="text-xs text-slate-300">Save 2.1 MB</span>
              </div>
            </motion.div>
          </motion.div>
        </div>
      </div>
    </section>
  )
}
