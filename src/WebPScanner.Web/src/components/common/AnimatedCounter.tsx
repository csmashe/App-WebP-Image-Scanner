import { useEffect, useRef, useState } from 'react'

interface AnimatedCounterProps {
  value: number
  duration?: number // Animation duration in ms
  formatter?: (value: number) => string
  className?: string
}

/**
 * Animated counter that smoothly counts up/down to a target value.
 */
export function AnimatedCounter({
  value,
  duration = 1500,
  formatter = (v) => v.toLocaleString(),
  className = '',
}: AnimatedCounterProps) {
  const [displayValue, setDisplayValue] = useState(value)
  const previousValueRef = useRef(value)
  const animationRef = useRef<number | null>(null)

  useEffect(() => {
    const startValue = previousValueRef.current
    const endValue = value
    const startTime = performance.now()

    if (startValue === endValue) return

    const easeOutQuart = (t: number): number => {
      return 1 - Math.pow(1 - t, 4)
    }

    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime
      const progress = Math.min(elapsed / duration, 1)
      const easedProgress = easeOutQuart(progress)

      const currentValue = Math.round(
        startValue + (endValue - startValue) * easedProgress
      )
      setDisplayValue(currentValue)

      if (progress < 1) {
        animationRef.current = requestAnimationFrame(animate)
      } else {
        setDisplayValue(endValue)
        previousValueRef.current = endValue
      }
    }

    if (animationRef.current) cancelAnimationFrame(animationRef.current)
    animationRef.current = requestAnimationFrame(animate)

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current)
      }
    }
  }, [value, duration])

  // Update ref when value changes without animation (initial render)
  useEffect(() => {
    if (previousValueRef.current === 0 && value > 0) {
      previousValueRef.current = 0
      setDisplayValue(0)
    }
  }, [value])

  return <span className={className}>{formatter(displayValue)}</span>
}

/**
 * Animated counter for string values that include units (e.g., "969.1 KB")
 */
interface AnimatedFormattedValueProps {
  value: string
  className?: string
}

function parseFormattedValue(value: string): { num: number; unit: string } | null {
  const match = value.match(/^([\d.]+)\s*(.*)$/)
  if (!match) return null
  return { num: parseFloat(match[1]), unit: match[2] }
}

export function AnimatedFormattedValue({
  value,
  className = '',
}: AnimatedFormattedValueProps) {
  const parsed = parseFormattedValue(value)
  // Initialize with the actual value to avoid animating from 0 on first render
  const [animatedNum, setAnimatedNum] = useState(parsed?.num ?? 0)
  const [displayUnit, setDisplayUnit] = useState(parsed?.unit ?? '')
  // Initialize previousValueRef with current value to prevent animation on first render
  const previousValueRef = useRef<{ num: number; unit: string } | null>(
    parsed ? { num: parsed.num, unit: parsed.unit } : null
  )
  const animationRef = useRef<number | null>(null)

  useEffect(() => {
    const parsed = parseFormattedValue(value)
    if (!parsed) return

    const endNum = parsed.num
    const unit = parsed.unit
    const prevUnit = previousValueRef.current?.unit

    if (animationRef.current) cancelAnimationFrame(animationRef.current)

    // Skip animation if unit changed (e.g., KB to MB) - animating would produce nonsense values
    if (prevUnit && prevUnit !== unit) {
      previousValueRef.current = { num: endNum, unit }
      // Schedule the state update via requestAnimationFrame to avoid synchronous setState in effect
      animationRef.current = requestAnimationFrame(() => {
        setAnimatedNum(endNum)
        setDisplayUnit(unit)
      })
      return
    }

    const startNum = previousValueRef.current?.num ?? endNum

    if (startNum === endNum) {
      previousValueRef.current = { num: endNum, unit }
      // Schedule state update for unit even if number hasn't changed
      animationRef.current = requestAnimationFrame(() => {
        setDisplayUnit(unit)
      })
      return
    }

    previousValueRef.current = { num: endNum, unit }

    const startTime = performance.now()
    const duration = 1500

    const easeOutQuart = (t: number): number => {
      return 1 - Math.pow(1 - t, 4)
    }

    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime
      const progress = Math.min(elapsed / duration, 1)
      const easedProgress = easeOutQuart(progress)

      const currentNum = startNum + (endNum - startNum) * easedProgress
      setAnimatedNum(currentNum)
      setDisplayUnit(unit)

      if (progress < 1) {
        animationRef.current = requestAnimationFrame(animate)
      } else {
        setAnimatedNum(endNum)
      }
    }

    animationRef.current = requestAnimationFrame(animate)

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current)
      }
    }
  }, [value])

  if (!parsed) {
    return <span className={className}>{value}</span>
  }

  const formatted = animatedNum >= 100 ? Math.round(animatedNum).toString() : animatedNum.toFixed(1)
  return <span className={className}>{formatted} {displayUnit}</span>
}
