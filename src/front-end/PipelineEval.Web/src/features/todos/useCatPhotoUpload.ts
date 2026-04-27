import { useCallback, useState } from 'react'
import {
  putFileToS3,
  requestUploadUrl,
  updateTodo,
} from './api'

export const IMAGE_TYPES = [
  'image/jpeg',
  'image/png',
  'image/webp',
  'image/gif',
] as const

export const IMAGE_ACCEPT_ATTR = 'image/jpeg,image/png,image/webp,image/gif'

export function isSupportedImage(file: File): boolean {
  return IMAGE_TYPES.includes(file.type as (typeof IMAGE_TYPES)[number])
}

type UseCatPhotoUploadOptions = {
  onUploaded: () => void | Promise<void>
  onError: (message: string) => void
}

/**
 * Encapsulates the three-step cat-photo upload (request presigned URL,
 * PUT to S3, patch the todo). Tracks the currently-uploading todo id so the
 * list can disable controls while an upload is in flight.
 */
export function useCatPhotoUpload({ onUploaded, onError }: UseCatPhotoUploadOptions) {
  const [busyId, setBusyId] = useState<string | null>(null)

  const uploadForTodo = useCallback(
    async (todoId: string, file: File | null) => {
      if (!file) return
      if (!isSupportedImage(file)) {
        onError('Please choose a JPEG, PNG, WebP, or GIF image.')
        return
      }
      setBusyId(todoId)
      try {
        const { uploadUrl, objectKey } = await requestUploadUrl(
          todoId,
          file.name,
          file.type,
        )
        await putFileToS3(uploadUrl, file, file.type)
        await updateTodo(todoId, { catImageObjectKey: objectKey })
        await onUploaded()
      } catch (err) {
        onError(err instanceof Error ? err.message : 'Upload failed.')
      } finally {
        setBusyId(null)
      }
    },
    [onError, onUploaded],
  )

  return { busyId, uploadForTodo }
}
