import {
  Body1,
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
} from '@fluentui/react-components'
import { useInvite } from '../useInvite'

type InviteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onError: (message: string) => void
  onSuccess: (message: string) => void
}

export function InviteDialog({
  open,
  onOpenChange,
  onError,
  onSuccess,
}: InviteDialogProps) {
  const { email, setEmail, busy, submit } = useInvite({
    onError: (message) => {
      onError(message)
    },
    onSuccess: (message) => {
      onSuccess(message)
      onOpenChange(false)
    },
  })

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface data-testid="dialog-invite">
        <DialogBody>
          <DialogTitle>Invite teammate</DialogTitle>
          <DialogContent>
            <Body1 className="muted small dialog-invite-lede">
              Administrators can send a Cognito invitation email to a new teammate.
            </Body1>
            <form
              className="invite-modal-form"
              onSubmit={(e) => {
                e.preventDefault()
                void submit()
              }}
            >
              <Field label="Email" required>
                <Input
                  type="email"
                  value={email}
                  onChange={(_, v) => setEmail(v.value)}
                  placeholder="teammate@example.com"
                  autoComplete="email"
                />
              </Field>
              <DialogActions className="invite-dialog-actions">
                <Button
                  appearance="secondary"
                  type="button"
                  onClick={() => onOpenChange(false)}
                >
                  Cancel
                </Button>
                <Button
                  appearance="primary"
                  type="submit"
                  disabled={busy || !email.trim()}
                >
                  {busy ? 'Sending…' : 'Send invitation'}
                </Button>
              </DialogActions>
            </form>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
