import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { JSDOM } from 'jsdom'
import { afterEach, vi } from 'vitest'

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  configurable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Fluent UI / tabster references bare `NodeFilter` in MutationObserver callbacks.
const nodeFilter =
  typeof window !== 'undefined' && window.NodeFilter
    ? window.NodeFilter
    : new JSDOM('').window.NodeFilter
globalThis.NodeFilter = nodeFilter

// Let tabster MutationObserver microtasks finish after unmount so teardown does not hit a stripped jsdom realm.
afterEach(
  async () =>
    new Promise<void>((resolve) => {
      cleanup()
      setTimeout(resolve, 0)
    }),
)
