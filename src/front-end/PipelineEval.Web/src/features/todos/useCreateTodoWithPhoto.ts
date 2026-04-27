import { useCallback } from 'react'
import {
  createTodo,
  putFileToS3,
  requestUploadUrl,
  updateTodo,
} from './api'

type UseCreateTodoWithPhotoOptions = {
  /** Invoked after the new todo (and optional photo) is persisted. */
  onCreated: () => void | Promise<void>
  /** Cleared before starting the request so stale banners don't linger. */
  clearError: () => void
}

type CreateInput = {
  title: string
  notes: string
  file: File | null
}

/**
 * Orchestrates the multi-step "create todo + optional cat photo" flow:
 *   1. POST /api/todos        -> new id
 *   2. (if file) POST /upload-url, PUT to S3, PATCH the todo with objectKey
 *   3. Reload the list so the new row (with thumbnail) shows immediately.
 *
 * Returns `null` on success and an error message string on failure so the
 * dialog can render it inline while staying open.
 */
export function useCreateTodoWithPhoto({
  onCreated,
  clearError,
}: UseCreateTodoWithPhotoOptions) {
  return useCallback(
    async ({ title, notes, file }: CreateInput): Promise<string | null> => {
      clearError()
      try {
        const { id } = await createTodo(title, notes || undefined)
        if (file) {
          const { uploadUrl, objectKey } = await requestUploadUrl(
            id,
            file.name,
            file.type,
          )
          await putFileToS3(uploadUrl, file, file.type)
          await updateTodo(id, { catImageObjectKey: objectKey })
        }
        await onCreated()
        return null
      } catch (err) {
        return err instanceof Error ? err.message : 'Could not create task.'
      }
    },
    [clearError, onCreated],
  )
}
