import { AuthScreen } from '../features/auth/AuthScreen'

/**
 * Route-level wrapper for the pre-authenticated sign-in / sign-up surface.
 * Keeps the route component thin so the implementation can evolve (e.g. add
 * a marketing hero above the form) without touching the app root.
 */
export function SignInPage() {
  return <AuthScreen />
}
