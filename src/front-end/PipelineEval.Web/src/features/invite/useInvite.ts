import { useCallback, useState } from 'react'
import { useAuth } from '../auth/AuthContext'

type UseInviteOptions = {
  onError: (message: string) => void
  onSuccess: (message: string) => void
}

/**
 * Owns the invite dialog's transient state (email input + busy flag) and
 * exposes a submit helper that calls the auth context and invokes the
 * caller-provided success/error handlers so the parent can close the dialog
 * and surface a top-level confirmation banner.
 */
export function useInvite({ onError, onSuccess }: UseInviteOptions) {
  const { inviteUser } = useAuth()
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = useCallback(async () => {
    const target = email.trim()
    if (!target) return
    setBusy(true)
    try {
      await inviteUser(target)
      onSuccess(`Invitation sent to ${target} (or user already exists).`)
      setEmail('')
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Invite failed.')
    } finally {
      setBusy(false)
    }
  }, [email, inviteUser, onError, onSuccess])

  return { email, setEmail, busy, submit }
}
