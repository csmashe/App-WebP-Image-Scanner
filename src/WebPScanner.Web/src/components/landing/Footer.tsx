import { Github } from 'lucide-react'

type AppPage = 'home' | 'terms'

interface FooterProps {
  onNavigate?: (page: AppPage) => void
}

export function Footer({ onNavigate }: FooterProps) {
  const currentYear = new Date().getFullYear()

  const handleNavClick = (e: React.MouseEvent<HTMLAnchorElement>, page: AppPage) => {
    if (onNavigate) {
      e.preventDefault()
      onNavigate(page)
    }
  }

  return (
    <footer className="border-t border-slate-200/70 dark:border-slate-800/70 bg-slate-50/80 dark:bg-slate-950/60 py-12 px-4 sm:px-6 lg:px-8 transition-colors duration-300">
      <div className="mx-auto max-w-6xl">
        <div className="rounded-3xl border border-slate-200/70 dark:border-slate-800/60 bg-white/80 dark:bg-slate-900/40 p-8 shadow-xl">
          <div className="flex flex-col gap-8 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-col gap-4">
              <a
                href="/"
                onClick={(e) => handleNavClick(e, 'home')}
                className="flex items-center gap-2 text-slate-900 dark:text-white hover:opacity-90 transition-opacity"
              >
                <picture>
                  <source srcSet="/favicon.webp" type="image/webp" />
                  <img src="/favicon.png" alt="" className="h-7 w-7" />
                </picture>
                <span className="text-lg font-semibold">WebP Scanner</span>
              </a>
              <p className="text-sm text-slate-500 max-w-xs">
                Free, open-source tool to help you optimize images, ship faster pages, and self-host with confidence.
              </p>
              <div className="flex flex-wrap gap-3">
                <a
                  href="#scan"
                  className="inline-flex items-center rounded-full bg-[#883043] px-4 py-2 text-sm font-semibold text-white shadow"
                >
                  Start a scan
                </a>
                <a
                  href="https://github.com/csmashe/App-WebP-Image-Scanner"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-2 rounded-full border border-slate-200/70 dark:border-slate-700 px-4 py-2 text-sm font-semibold text-slate-600 dark:text-slate-200 hover:text-slate-900 dark:hover:text-white transition-colors"
                >
                  <Github className="h-4 w-4" />
                  View on GitHub
                </a>
              </div>
            </div>

            <div className="flex flex-col sm:flex-row gap-6 sm:gap-12">
              <div>
                <h4 className="text-sm font-semibold text-slate-900 dark:text-white mb-3 transition-colors duration-300">Resources</h4>
                <ul className="space-y-2">
                  <li>
                    <a
                      href="/terms"
                      onClick={(e) => handleNavClick(e, 'terms')}
                      className="text-sm text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors"
                    >
                      Terms of Service
                    </a>
                  </li>
                  <li>
                    <a
                      href="https://developers.google.com/speed/webp"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-sm text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors"
                    >
                      About WebP
                    </a>
                  </li>
                </ul>
              </div>

              <div>
                <h4 className="text-sm font-semibold text-slate-900 dark:text-white mb-3 transition-colors duration-300">Connect</h4>
                <ul className="space-y-2">
                  <li>
                    <a
                      href="https://github.com/csmashe/App-WebP-Image-Scanner"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="inline-flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors"
                    >
                      <Github className="h-4 w-4" />
                      GitHub
                    </a>
                  </li>
                </ul>
              </div>
            </div>
          </div>
        </div>

        <div className="mt-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 transition-colors duration-300">
          <p className="text-sm text-slate-500">
            © {currentYear} WebP Scanner. Open source under{' '}
            <a
              href="https://github.com/csmashe/App-WebP-Image-Scanner/blob/main/LICENSE"
              target="_blank"
              rel="noopener noreferrer"
              className="text-[#883043] hover:underline"
            >
              AGPL-3.0
            </a>
            .
          </p>
          <p className="inline-flex items-center gap-1 text-sm text-slate-500">
            Brought to you by{' '}
            <a
              href="https://excelontheweb.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-[#883043] hover:underline"
            >
              Excel on the Web
            </a>
          </p>
        </div>
      </div>
    </footer>
  )
}
