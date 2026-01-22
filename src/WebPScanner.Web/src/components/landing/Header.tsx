import { Github } from 'lucide-react'
import { ThemeToggle } from '../ui/theme-toggle'

export function Header() {
  return (
    <header className="fixed top-0 left-0 right-0 z-50 border-b border-slate-200/70 dark:border-slate-800/70 bg-white/70 dark:bg-slate-950/70 backdrop-blur-xl transition-colors duration-300">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <a href="/" className="flex items-center gap-2 text-slate-900 dark:text-white hover:opacity-90 transition-opacity">
            <picture>
              <source srcSet="/favicon.webp" type="image/webp" />
              <img src="/favicon.png" alt="" className="h-8 w-8" />
            </picture>
            <span className="text-lg font-semibold tracking-tight">WebP Scanner</span>
          </a>

          <nav className="flex items-center gap-3">
            <a
              href="#how-it-works"
              className="hidden sm:inline-flex items-center rounded-full px-4 py-2 text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white transition-colors"
            >
              How it works
            </a>
            <a
              href="#scan"
              className="hidden sm:inline-flex items-center rounded-full border border-[#883043]/30 bg-[#883043]/10 px-4 py-2 text-sm font-semibold text-[#883043] dark:text-[#c9787f] hover:bg-[#883043]/20 transition-colors"
            >
              Start scan
            </a>
            <a
              href="https://github.com/csmashe/App-WebP-Image-Scanner"
              target="_blank"
              rel="noopener noreferrer"
              aria-label="View on GitHub"
              className="inline-flex h-10 w-10 items-center justify-center rounded-xl text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
            >
              <Github className="h-5 w-5" />
            </a>

            <ThemeToggle />
          </nav>
        </div>
      </div>
    </header>
  )
}
