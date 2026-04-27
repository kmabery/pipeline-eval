import { Body1 } from '@fluentui/react-components'
import { useAuth } from '../features/auth/AuthContext'
import { SignInPage } from '../pages/SignInPage'
import { TodosPage } from '../pages/TodosPage'
import '../shared/styles/app.css'

/**
 * Pure composition. Decides which top-level page to render based on auth
 * state; does no IO and owns no feature UI state. Per react.dev guidance,
 * keeping the root component tiny makes the app tree easy to reason about
 * and lets each page own its own data and local state.
 */
export default function App() {
  const { cognitoConfigured, isAuthenticated, loading } = useAuth()

  if (cognitoConfigured && loading) {
    return (
      <div className="app">
        <Body1 className="muted">Loading session…</Body1>
      </div>
    )
  }

  if (cognitoConfigured && !isAuthenticated) {
    return <SignInPage />
  }

  return <TodosPage />
}
