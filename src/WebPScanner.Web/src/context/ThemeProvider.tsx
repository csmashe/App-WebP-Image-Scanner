import { useEffect, useState, useCallback, useMemo, type ReactNode } from 'react'
import {
  ThemeContext,
  type Theme,
  type ResolvedTheme,
  getSystemTheme,
  getStoredTheme,
  resolveTheme,
  storeTheme,
} from './themeContextDef'

interface ThemeProviderProps {
  children: ReactNode
  defaultTheme?: Theme
}

export function ThemeProvider({ children, defaultTheme = 'system' }: ThemeProviderProps) {
  const [theme, setThemeState] = useState<Theme>(() => {
    const stored = getStoredTheme()
    return stored === 'system' ? defaultTheme : stored
  })

  // Track system theme changes separately for when theme is 'system'
  const [systemTheme, setSystemTheme] = useState<ResolvedTheme>(getSystemTheme)

  // Compute resolved theme from theme and systemTheme
  const resolvedTheme = useMemo<ResolvedTheme>(() => {
    if (theme === 'system') {
      return systemTheme
    }
    return theme
  }, [theme, systemTheme])

  // Listen for system preference changes
  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')

    const handleChange = (e: MediaQueryListEvent) => {
      setSystemTheme(e.matches ? 'dark' : 'light')
    }

    mediaQuery.addEventListener('change', handleChange)
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [])

  // Apply theme class to document
  useEffect(() => {
    const root = document.documentElement

    // Remove existing theme classes
    root.classList.remove('light', 'dark')

    // Add appropriate class - Tailwind expects 'dark' class for dark mode
    if (resolvedTheme === 'dark') {
      root.classList.add('dark')
    } else {
      root.classList.add('light')
    }
  }, [resolvedTheme])

  // Persist theme to localStorage
  useEffect(() => {
    storeTheme(theme)
  }, [theme])

  const setTheme = useCallback((newTheme: Theme) => {
    setThemeState(newTheme)
  }, [])

  const toggleTheme = useCallback(() => {
    setThemeState(prev => {
      // When toggling, we alternate between dark and light (not system)
      const currentResolved = resolveTheme(prev)
      return currentResolved === 'dark' ? 'light' : 'dark'
    })
  }, [])

  const value = {
    theme,
    resolvedTheme,
    setTheme,
    toggleTheme,
    isDark: resolvedTheme === 'dark',
  }

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  )
}
