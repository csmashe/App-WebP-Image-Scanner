import { describe, it, expect } from 'vitest'

describe('Vitest Setup', () => {
  it('should run tests successfully', () => {
    expect(true).toBe(true)
  })

  it('should have jest-dom matchers available', () => {
    const element = document.createElement('div')
    element.textContent = 'Hello'
    document.body.appendChild(element)
    expect(element).toBeInTheDocument()
    document.body.removeChild(element)
  })

  it('should have localStorage mock available', () => {
    expect(window.localStorage).toBeDefined()
    expect(window.localStorage.getItem).toBeDefined()
  })

  it('should have matchMedia mock available', () => {
    expect(window.matchMedia).toBeDefined()
    const mq = window.matchMedia('(prefers-color-scheme: dark)')
    expect(mq.matches).toBe(false)
  })
})
