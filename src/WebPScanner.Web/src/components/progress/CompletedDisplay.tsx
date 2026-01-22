import { useState } from 'react'
import { motion } from 'framer-motion'
import { CheckCircle, Mail, FileImage, Globe, RotateCcw, Download, Loader2, Package } from 'lucide-react'
import { useScanStore } from '../../store/scanStore'
import { Button } from '../ui/button'
import { toast } from '../../store/toastStore'
import { truncateUrl, formatDuration, parseFilenameFromContentDisposition } from '../../lib/completedDisplayUtils'

interface CompletedDisplayProps {
  onReset: () => void
}

export function CompletedDisplay({ onReset }: CompletedDisplayProps) {
  const [isDownloading, setIsDownloading] = useState(false)
  const [isDownloadingWebP, setIsDownloadingWebP] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  const {
    scanId,
    targetUrl,
    email,
    pagesScanned,
    nonWebPImagesCount,
    duration,
    reachedPageLimit,
    convertToWebP,
  } = useScanStore()

  const handleDownload = async () => {
    if (!scanId) return

    setIsDownloading(true)
    setDownloadError(null)

    try {
      const response = await fetch(`/api/scan/${scanId}/report`)

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Failed to download report' }))
        throw new Error(errorData.message || 'Failed to download report')
      }

      // Get the filename from the Content-Disposition header or use default
      const contentDisposition = response.headers.get('Content-Disposition')
      const filename = parseFilenameFromContentDisposition(contentDisposition, `webp-scan-report-${scanId}.pdf`)

      // Create blob and download
      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = filename
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Download error:', error)
      setDownloadError(error instanceof Error ? error.message : 'Failed to download report')
    } finally {
      setIsDownloading(false)
    }
  }

  const handleDownloadWebP = async () => {
    if (!scanId) return

    setIsDownloadingWebP(true)
    setDownloadError(null)

    try {
      const response = await fetch(`/api/scan/${scanId}/images`)

      if (response.status === 410) {
        // Link has expired
        toast.warning('Download expired', 'The converted images download link has expired. WebP images are only available for 6 hours after scan completion.')
        return
      }

      if (response.status === 404) {
        toast.error('Not available', 'WebP conversion was not requested for this scan.')
        return
      }

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Failed to download converted images' }))
        throw new Error(errorData.message || 'Failed to download converted images')
      }

      // Get the filename from the Content-Disposition header or use default
      const contentDisposition = response.headers.get('Content-Disposition')
      const filename = parseFilenameFromContentDisposition(contentDisposition, `webp-images-${scanId}.zip`)

      // Create blob and download
      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = filename
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)

      toast.success('Download started', 'Your converted WebP images are downloading.')
    } catch (error) {
      console.error('WebP download error:', error)
      setDownloadError(error instanceof Error ? error.message : 'Failed to download converted images')
    } finally {
      setIsDownloadingWebP(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Success header */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-green-100 dark:bg-green-500/20 transition-colors duration-300"
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{ type: 'spring', stiffness: 200, damping: 10 }}
        >
          <CheckCircle className="h-8 w-8 text-green-600 dark:text-green-400" />
        </motion.div>
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white transition-colors duration-300">
          Scan Complete!
        </h3>
        {targetUrl && (
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300" title={targetUrl}>
            {truncateUrl(targetUrl)}
          </p>
        )}
      </div>

      {/* Stats summary */}
      <div className="grid grid-cols-2 gap-4">
        <div className="rounded-xl bg-slate-100 dark:bg-slate-800/50 p-4 text-center transition-colors duration-300">
          <Globe className="mx-auto h-5 w-5 text-slate-600 dark:text-slate-400 mb-2" />
          <div className="text-2xl font-bold text-slate-900 dark:text-white transition-colors duration-300">{pagesScanned}</div>
          <div className="text-xs text-slate-500">Pages Scanned</div>
        </div>
        <div className="rounded-xl bg-slate-100 dark:bg-slate-800/50 p-4 text-center transition-colors duration-300">
          <FileImage className="mx-auto h-5 w-5 text-amber-600 dark:text-amber-400 mb-2" />
          <div className="text-2xl font-bold text-amber-600 dark:text-amber-400">{nonWebPImagesCount}</div>
          <div className="text-xs text-slate-500">Non-WebP Images</div>
        </div>
      </div>

      {/* Duration */}
      <div className="text-center text-sm text-slate-600 dark:text-slate-400 transition-colors duration-300">
        Scan completed in {formatDuration(duration)}
      </div>

      {/* Page limit warning */}
      {reachedPageLimit && (
        <div className="rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/50 p-3 text-sm text-amber-700 dark:text-amber-400 transition-colors duration-300">
          <strong>Note:</strong> The scan reached the page limit. Some pages may not have been analyzed.
        </div>
      )}

      {/* Email notification - only show if email was provided */}
      {email && (
        <div className="rounded-xl bg-[#883043]/10 dark:bg-[#883043]/20 border border-[#883043]/30 dark:border-[#883043]/30 p-4 transition-colors duration-300">
          <div className="flex items-start gap-3">
            <Mail className="h-5 w-5 text-[#883043] dark:text-[#c9787f] mt-0.5" />
            <div>
              <p className="text-sm text-slate-900 dark:text-white font-medium transition-colors duration-300">
                Report on its way!
              </p>
              <p className="text-xs text-slate-600 dark:text-slate-400 mt-1 transition-colors duration-300">
                We're sending a detailed PDF report to <span className="text-[#883043] dark:text-[#c9787f]">{email}</span> with
                optimization recommendations for your non-WebP images.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Download error */}
      {downloadError && (
        <div className="rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 p-3 text-sm text-red-700 dark:text-red-400 transition-colors duration-300">
          {downloadError}
        </div>
      )}

      {/* Actions */}
      <div className="flex flex-wrap justify-center gap-3">
        <Button
          onClick={handleDownload}
          disabled={isDownloading}
          className="gap-2"
        >
          {isDownloading ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Download className="h-4 w-4" />
          )}
          {isDownloading ? 'Generating...' : 'Download Report'}
        </Button>
        {convertToWebP && nonWebPImagesCount > 0 && (
          <Button
            onClick={handleDownloadWebP}
            disabled={isDownloadingWebP}
            variant="secondary"
            className="gap-2"
          >
            {isDownloadingWebP ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Package className="h-4 w-4" />
            )}
            {isDownloadingWebP ? 'Preparing...' : 'Download WebP Images'}
          </Button>
        )}
        <Button
          variant="outline"
          onClick={onReset}
          className="gap-2"
        >
          <RotateCcw className="h-4 w-4" />
          Scan Another Website
        </Button>
      </div>

      {/* WebP conversion notice */}
      {convertToWebP && nonWebPImagesCount > 0 && (
        <div className="rounded-lg bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800/50 p-3 text-sm text-emerald-700 dark:text-emerald-400 transition-colors duration-300">
          <strong>WebP images ready!</strong> Your converted images are available for download. The download link expires in 6 hours.
        </div>
      )}
    </div>
  )
}
