import type { ReactNode } from 'react'
import { motion } from 'framer-motion'
import { ArrowLeft, Shield, Clock, Globe, Mail, AlertTriangle, Server, Scale } from 'lucide-react'
import { Button } from '../ui/button'

interface TermsOfServiceProps {
  onBack: () => void
}

export function TermsOfService({ onBack }: TermsOfServiceProps) {
  const lastUpdated = 'January 19, 2026'

  return (
    <motion.div
      className="min-h-screen pt-24 pb-16 px-4 sm:px-6 lg:px-8"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.3 }}
    >
      <div className="mx-auto max-w-3xl">
        {/* Back button */}
        <Button
          variant="ghost"
          onClick={onBack}
          className="mb-6 -ml-2 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
        >
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back to Home
        </Button>

        {/* Header */}
        <div className="mb-10">
          <h1 className="text-3xl sm:text-4xl font-bold text-slate-900 dark:text-white mb-4 transition-colors duration-300">
            Terms of Service
          </h1>
          <p className="text-slate-500 dark:text-slate-400 transition-colors duration-300">
            Last updated: {lastUpdated}
          </p>
        </div>

        {/* Content */}
        <div className="space-y-10">
          {/* Introduction */}
          <section className="prose dark:prose-invert max-w-none">
            <p className="text-slate-600 dark:text-slate-300 leading-relaxed transition-colors duration-300">
              Welcome to WebP Scanner. By using our service, you agree to these Terms of Service.
              Please read them carefully before submitting a scan request.
            </p>
          </section>

          {/* Fair Use Policy */}
          <Section
            icon={<Scale className="h-5 w-5" />}
            title="Fair Use Policy"
            iconColor="text-[#883043]"
          >
            <p className="text-slate-600 dark:text-slate-300 mb-4 transition-colors duration-300">
              To ensure fair access for all users, we implement the following policies:
            </p>
            <ul className="space-y-3">
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Queue System:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Scan requests are scheduled using a fair-share algorithm. Jobs are organized into
                  submission "slots" — all users' first submissions are processed before second
                  submissions, and so on. Within each slot, jobs are ordered by submission time.
                  Priority scores also age over time to ensure jobs that have waited longer get
                  processed fairly. Your current position is displayed in real-time while you wait.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Rate Limiting:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  To prevent abuse, we limit the number of scan requests per IP address. Excessive
                  submissions may result in temporary delays or rejection of new requests.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Fairness Algorithm:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Users who submit many requests may experience longer wait times to ensure fair access
                  for all users. Priority is given to users who have waited longer.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Resource Limits:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Each scan is limited to a maximum number of pages to ensure timely processing for all
                  users. Very large websites may not be fully scanned.
                </span>
              </ListItem>
            </ul>
          </Section>

          {/* Service Limitations */}
          <Section
            icon={<AlertTriangle className="h-5 w-5" />}
            title="Service Limitations"
            iconColor="text-amber-500"
          >
            <p className="text-slate-600 dark:text-slate-300 mb-4 transition-colors duration-300">
              Please be aware of the following limitations:
            </p>
            <ul className="space-y-3">
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Public Pages Only:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Our scanner can only analyze publicly accessible web pages. Content behind
                  authentication, paywalls, or login forms cannot be scanned.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Authentication Pages Skipped:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Pages that require login credentials or redirect to login forms are automatically
                  detected and skipped during the scan.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">robots.txt Compliance:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  We respect website owners' wishes as expressed in their robots.txt file. Pages
                  marked as disallowed will not be scanned.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Estimation Accuracy:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  WebP file size savings are estimates based on empirical conversion ratios. Actual
                  savings may vary depending on image content and compression settings.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Same-Domain Scanning:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  For security reasons, the scanner only follows links within the same domain as the
                  submitted URL. External links are not crawled.
                </span>
              </ListItem>
            </ul>
          </Section>

          {/* Data Handling */}
          <Section
            icon={<Shield className="h-5 w-5" />}
            title="Data Handling"
            iconColor="text-emerald-500"
          >
            <p className="text-slate-600 dark:text-slate-300 mb-4 transition-colors duration-300">
              We take your privacy seriously:
            </p>
            <ul className="space-y-3">
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Data Retention:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Scan data is retained for 7 days after completion to allow you to download your
                  report. After this retention period, scan results and associated data are
                  automatically and permanently deleted from our systems.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Report Delivery:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Reports can be downloaded directly from the scan results page or via the
                  /api/scan/&#123;scanId&#125;/report endpoint while your data is retained. If you
                  provide an email address, a copy will also be sent to your inbox for permanent
                  keeping.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Email Usage:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  If provided, your email address is used solely to deliver your scan report. We do not send
                  marketing emails, newsletters, or share your email with third parties.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">IP Address Logging:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  We temporarily log IP addresses for rate limiting and abuse prevention purposes
                  only. This data is not used for tracking or marketing.
                </span>
              </ListItem>
            </ul>
          </Section>

          {/* Third-Party Sites Disclaimer */}
          <Section
            icon={<Globe className="h-5 w-5" />}
            title="Scanning Third-Party Websites"
            iconColor="text-blue-500"
          >
            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4 mb-4 transition-colors duration-300">
              <p className="text-amber-800 dark:text-amber-200 text-sm transition-colors duration-300">
                <strong>Important:</strong> You should only scan websites that you own, manage, or
                have explicit permission to analyze.
              </p>
            </div>
            <ul className="space-y-3">
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Your Responsibility:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  By submitting a URL, you represent that you have the right to scan the website or
                  have obtained appropriate permission from the website owner.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">No Unauthorized Access:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Do not use this service to scan websites where such scanning would violate the
                  website's terms of service or applicable laws.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Liability:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  We are not responsible for any consequences arising from your use of this service
                  to scan third-party websites without proper authorization.
                </span>
              </ListItem>
            </ul>
          </Section>

          {/* Technical Information */}
          <Section
            icon={<Server className="h-5 w-5" />}
            title="Technical Information"
            iconColor="text-[#8B3A42]"
          >
            <ul className="space-y-3">
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">User Agent:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Our scanner identifies itself with a standard user agent string when accessing
                  your website.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Request Rate:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  We implement delays between page requests to minimize impact on your server.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">JavaScript Support:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  Our scanner uses a full browser engine to render pages, including JavaScript-powered
                  single-page applications (SPAs).
                </span>
              </ListItem>
            </ul>
          </Section>

          {/* Service Availability */}
          <Section
            icon={<Clock className="h-5 w-5" />}
            title="Service Availability"
            iconColor="text-rose-500"
          >
            <ul className="space-y-3">
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">No Guarantees:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  This is a free service provided as-is. We do not guarantee uptime, availability,
                  or accuracy of results.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Service Changes:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  We reserve the right to modify, suspend, or discontinue the service at any time
                  without prior notice.
                </span>
              </ListItem>
              <ListItem>
                <strong className="text-slate-900 dark:text-white transition-colors duration-300">Terms Updates:</strong>{' '}
                <span className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
                  These terms may be updated from time to time. Continued use of the service
                  constitutes acceptance of any changes.
                </span>
              </ListItem>
            </ul>
          </Section>

          {/* Contact */}
          <Section
            icon={<Mail className="h-5 w-5" />}
            title="Contact"
            iconColor="text-cyan-500"
          >
            <p className="text-slate-600 dark:text-slate-300 transition-colors duration-300">
              If you have questions about these Terms of Service, please open an issue on our{' '}
              <a
                href="https://github.com/csmashe/App-WebP-Image-Scanner"
                target="_blank"
                rel="noopener noreferrer"
                className="text-[#883043] hover:text-[#6d2635] dark:text-[#c9787f] dark:hover:text-[#d49ca2] underline transition-colors"
              >
                GitHub repository
              </a>
              .
            </p>
          </Section>
        </div>

        {/* Footer */}
        <div className="mt-12 pt-8 border-t border-slate-200 dark:border-slate-800 transition-colors duration-300">
          <Button
            variant="default"
            onClick={onBack}
            className="w-full sm:w-auto"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Return to Home
          </Button>
        </div>
      </div>
    </motion.div>
  )
}

interface SectionProps {
  icon: ReactNode
  title: string
  iconColor: string
  children: ReactNode
}

function Section({ icon, title, iconColor, children }: SectionProps) {
  return (
    <section className="rounded-xl border border-slate-200 dark:border-slate-800 bg-white/50 dark:bg-slate-900/30 p-6 transition-colors duration-300">
      <div className="flex items-center gap-3 mb-4">
        <div className={`flex h-10 w-10 items-center justify-center rounded-lg bg-slate-100 dark:bg-slate-800 ${iconColor} transition-colors duration-300`}>
          {icon}
        </div>
        <h2 className="text-xl font-semibold text-slate-900 dark:text-white transition-colors duration-300">
          {title}
        </h2>
      </div>
      {children}
    </section>
  )
}

interface ListItemProps {
  children: ReactNode
}

function ListItem({ children }: ListItemProps) {
  return (
    <li className="flex items-start gap-3">
      <span className="mt-2 h-1.5 w-1.5 flex-shrink-0 rounded-full bg-slate-400 dark:bg-slate-600 transition-colors duration-300" />
      <span className="text-sm leading-relaxed">{children}</span>
    </li>
  )
}
