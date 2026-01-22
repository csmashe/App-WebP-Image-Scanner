import { Moon, Sun, Monitor } from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { Button } from './button'
import { useTheme } from '../../context/useTheme'

interface ThemeToggleProps {
  /** Show just toggle (dark/light) or include system option */
  showSystemOption?: boolean
  className?: string
}

export function ThemeToggle({ showSystemOption = false, className = '' }: ThemeToggleProps) {
  const { theme, setTheme, toggleTheme, isDark } = useTheme()

  if (showSystemOption) {
    // Three-way toggle: dark, light, system
    return (
      <div className={`flex items-center gap-1 rounded-lg bg-slate-800/50 dark:bg-slate-800/50 p-1 ${className}`}>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setTheme('light')}
          className={`h-8 w-8 p-0 ${theme === 'light' ? 'bg-slate-700 text-white' : 'text-slate-400 hover:text-white'}`}
          aria-label="Light mode"
        >
          <Sun className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setTheme('system')}
          className={`h-8 w-8 p-0 ${theme === 'system' ? 'bg-slate-700 text-white' : 'text-slate-400 hover:text-white'}`}
          aria-label="System theme"
        >
          <Monitor className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setTheme('dark')}
          className={`h-8 w-8 p-0 ${theme === 'dark' ? 'bg-slate-700 text-white' : 'text-slate-400 hover:text-white'}`}
          aria-label="Dark mode"
        >
          <Moon className="h-4 w-4" />
        </Button>
      </div>
    )
  }

  // Simple toggle button
  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={toggleTheme}
      className={`relative text-slate-400 hover:text-white dark:text-slate-400 dark:hover:text-white light:text-slate-600 light:hover:text-slate-900 ${className}`}
      aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
    >
      <AnimatePresence mode="wait" initial={false}>
        {isDark ? (
          <motion.div
            key="sun"
            initial={{ scale: 0, rotate: -90, opacity: 0 }}
            animate={{ scale: 1, rotate: 0, opacity: 1 }}
            exit={{ scale: 0, rotate: 90, opacity: 0 }}
            transition={{ duration: 0.2, ease: 'easeInOut' }}
          >
            <Sun className="h-5 w-5" />
          </motion.div>
        ) : (
          <motion.div
            key="moon"
            initial={{ scale: 0, rotate: 90, opacity: 0 }}
            animate={{ scale: 1, rotate: 0, opacity: 1 }}
            exit={{ scale: 0, rotate: -90, opacity: 0 }}
            transition={{ duration: 0.2, ease: 'easeInOut' }}
          >
            <Moon className="h-5 w-5" />
          </motion.div>
        )}
      </AnimatePresence>
    </Button>
  )
}
