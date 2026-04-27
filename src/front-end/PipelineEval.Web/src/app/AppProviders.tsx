import type { ReactNode } from 'react'
import { FluentThemeRoot } from '../FluentThemeRoot'
import { AuthProvider } from '../features/auth/AuthContext'

/**
 * Single composition point for cross-cutting providers. Adding a new
 * provider (e.g. a future TanStack Query client) should happen here so
 * `main.tsx` and tests never need to wire them up individually.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <FluentThemeRoot>
      <AuthProvider>{children}</AuthProvider>
    </FluentThemeRoot>
  )
}
