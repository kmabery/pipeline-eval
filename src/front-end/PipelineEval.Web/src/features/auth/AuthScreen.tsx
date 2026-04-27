import { Fragment, useState } from 'react'
import { useAuth } from './AuthContext'

export function AuthScreen() {
  const {
    signIn,
    signUp,
    confirmSignUp,
    needsNewPassword,
    completeNewPassword,
  } = useAuth()
  const [tab, setTab] = useState<'signin' | 'signup' | 'confirm'>('signin')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [code, setCode] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSignIn(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setMessage(null)
    setBusy(true)
    try {
      await signIn(email, password)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign in failed.')
    } finally {
      setBusy(false)
    }
  }

  async function onSetNewPassword(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setMessage(null)
    setBusy(true)
    try {
      await completeNewPassword(newPassword)
      setNewPassword('')
      setMessage('Password updated. You are signed in.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not set password.')
    } finally {
      setBusy(false)
    }
  }

  async function onSignUp(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setMessage(null)
    setBusy(true)
    try {
      await signUp(email, password)
      setMessage('Check your email for a confirmation code, then use the Confirm tab.')
      setTab('confirm')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign up failed.')
    } finally {
      setBusy(false)
    }
  }

  async function onConfirm(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setMessage(null)
    setBusy(true)
    try {
      await confirmSignUp(email, code)
      setMessage('Email confirmed. You can sign in.')
      setTab('signin')
      setCode('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Confirmation failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="app auth-screen">
      <header className="header">
        <h1>Todo cat pics</h1>
        <p className="lede">Sign in with your account. New users can register; administrators can invite teammates after signing in.</p>
      </header>

      {needsNewPassword ? (
        <>
          {error ? (
            <div className="banner banner-error" role="alert">
              {error}
            </div>
          ) : null}
          {message ? (
            <div className="banner banner-info" role="status" aria-live="polite">
              {message}
            </div>
          ) : null}
          <form className="card form-new" onSubmit={onSetNewPassword}>
            <h2>Set permanent password</h2>
            <p className="muted small">
              You signed in with a temporary password from an invitation. Choose a new password to finish.
            </p>
            <label className="field">
              <span>New password</span>
              <input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                autoComplete="new-password"
                required
              />
            </label>
            <button type="submit" className="btn primary" disabled={busy}>
              {busy ? 'Saving…' : 'Save and continue'}
            </button>
          </form>
        </>
      ) : null}

      {!needsNewPassword ? (
        <Fragment>
          <div className="auth-tabs" role="tablist" aria-label="Account access">
            <button
              type="button"
              role="tab"
              id="tab-signin"
              aria-selected={tab === 'signin'}
              aria-controls="panel-signin"
              className={tab === 'signin' ? 'active' : ''}
              onClick={() => setTab('signin')}
            >
              Sign in
            </button>
            <button
              type="button"
              role="tab"
              id="tab-signup"
              aria-selected={tab === 'signup'}
              aria-controls="panel-signup"
              className={tab === 'signup' ? 'active' : ''}
              onClick={() => setTab('signup')}
            >
              Sign up
            </button>
            <button
              type="button"
              role="tab"
              id="tab-confirm"
              aria-selected={tab === 'confirm'}
              aria-controls="panel-confirm"
              className={tab === 'confirm' ? 'active' : ''}
              onClick={() => setTab('confirm')}
            >
              Confirm email
            </button>
          </div>

          {error ? (
            <div className="banner banner-error" role="alert">
              {error}
            </div>
          ) : null}
          {message ? (
            <div className="banner banner-info" role="status" aria-live="polite">
              {message}
            </div>
          ) : null}

          <div
            id="panel-signin"
            role="tabpanel"
            aria-labelledby="tab-signin"
            hidden={tab !== 'signin'}
          >
            <form className="card form-new" onSubmit={onSignIn}>
              <h2>Sign in</h2>
              <label className="field">
                <span>Email</span>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="username"
                  required
                />
              </label>
              <label className="field">
                <span>Password</span>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                  required
                />
              </label>
              <button type="submit" className="btn primary" disabled={busy}>
                {busy ? 'Signing in…' : 'Sign in'}
              </button>
            </form>
          </div>

          <div
            id="panel-signup"
            role="tabpanel"
            aria-labelledby="tab-signup"
            hidden={tab !== 'signup'}
          >
            <form className="card form-new" onSubmit={onSignUp}>
              <h2>Create account</h2>
              <p className="muted small">
                Cognito sends a confirmation code to your email (verification message).
              </p>
              <label className="field">
                <span>Email</span>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="email"
                  required
                />
              </label>
              <label className="field">
                <span>Password</span>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                  required
                />
              </label>
              <button type="submit" className="btn primary" disabled={busy}>
                {busy ? 'Creating account…' : 'Sign up'}
              </button>
            </form>
          </div>

          <div
            id="panel-confirm"
            role="tabpanel"
            aria-labelledby="tab-confirm"
            hidden={tab !== 'confirm'}
          >
            <form className="card form-new" onSubmit={onConfirm}>
              <h2>Confirm email</h2>
              <label className="field">
                <span>Email</span>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </label>
              <label className="field">
                <span>Confirmation code</span>
                <input
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  autoComplete="one-time-code"
                  required
                />
              </label>
              <button type="submit" className="btn primary" disabled={busy}>
                {busy ? 'Confirming…' : 'Confirm'}
              </button>
            </form>
          </div>
        </Fragment>
      ) : null}
    </div>
  )
}
