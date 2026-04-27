import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  Textarea,
} from '@fluentui/react-components'
import { useCallback, useRef, useState } from 'react'
import { IMAGE_ACCEPT_ATTR, isSupportedImage } from '../useCatPhotoUpload'
import { ErrorBanner } from '../../../shared/ui/ErrorBanner'

type NewTaskDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  /**
   * Create the todo and optionally upload a photo. Returns a string error on
   * validation or server failure so the dialog can surface it inline and
   * keep itself open; returning `null` tells the dialog the flow succeeded
   * and it should close and reset.
   */
  onSubmit: (input: {
    title: string
    notes: string
    file: File | null
  }) => Promise<string | null>
}

export function NewTaskDialog({ open, onOpenChange, onSubmit }: NewTaskDialogProps) {
  const [title, setTitle] = useState('')
  const [notes, setNotes] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [busy, setBusy] = useState(false)
  const [dialogError, setDialogError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const reset = useCallback(() => {
    setTitle('')
    setNotes('')
    setFile(null)
    setDialogError(null)
    const el = fileInputRef.current
    if (el) el.value = ''
  }, [])

  const handleOpenChange = useCallback(
    (next: boolean) => {
      onOpenChange(next)
      if (!next) reset()
    },
    [onOpenChange, reset],
  )

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault()
      if (!title.trim()) return
      if (file && !isSupportedImage(file)) {
        setDialogError('Please choose a JPEG, PNG, WebP, or GIF image.')
        return
      }
      setDialogError(null)
      setBusy(true)
      try {
        const error = await onSubmit({
          title: title.trim(),
          notes: notes.trim(),
          file,
        })
        if (error) {
          setDialogError(error)
          return
        }
        reset()
        onOpenChange(false)
      } finally {
        setBusy(false)
      }
    },
    [file, notes, onOpenChange, onSubmit, reset, title],
  )

  return (
    <Dialog open={open} onOpenChange={(_, data) => handleOpenChange(data.open)}>
      <DialogSurface data-testid="dialog-new-task">
        <DialogBody>
          <DialogTitle>New task</DialogTitle>
          <DialogContent>
            {dialogError ? (
              <div className="dialog-error" data-testid="new-task-error">
                <ErrorBanner message={dialogError} />
              </div>
            ) : null}
            <form
              className="new-task-modal-form"
              data-testid="new-task-form"
              onSubmit={(e) => void handleSubmit(e)}
            >
              <Field label="Title" required>
                <Input
                  value={title}
                  onChange={(_, v) => setTitle(v.value)}
                  placeholder="e.g. Post tabby Tuesday"
                  autoComplete="off"
                />
              </Field>
              <Field label="Notes (optional)">
                <Textarea
                  value={notes}
                  onChange={(_, v) => setNotes(v.value)}
                  placeholder="Extra context…"
                  rows={2}
                  resize="vertical"
                />
              </Field>
              <Field
                label={
                  <span className="new-task-file-label">
                    Cat photo{' '}
                    <span className="muted optional-tag">(optional)</span>
                  </span>
                }
              >
                <div className="new-task-file-row">
                  <label className="btn secondary upload-label">
                    {file ? file.name : 'Choose image…'}
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept={IMAGE_ACCEPT_ATTR}
                      className="sr-only"
                      disabled={busy}
                      onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                    />
                  </label>
                  {file ? (
                    <Button
                      appearance="secondary"
                      type="button"
                      disabled={busy}
                      onClick={() => {
                        setFile(null)
                        const el = fileInputRef.current
                        if (el) el.value = ''
                      }}
                    >
                      Clear
                    </Button>
                  ) : null}
                </div>
              </Field>
              <DialogActions className="new-task-dialog-actions">
                <Button
                  appearance="secondary"
                  type="button"
                  disabled={busy}
                  onClick={() => {
                    reset()
                    onOpenChange(false)
                  }}
                >
                  Cancel
                </Button>
                <Button
                  appearance="primary"
                  type="submit"
                  disabled={busy || !title.trim()}
                >
                  {busy ? 'Saving…' : 'Add todo'}
                </Button>
              </DialogActions>
            </form>
          </DialogContent>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
