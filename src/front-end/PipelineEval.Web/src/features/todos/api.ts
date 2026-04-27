import { getAuthHeaders } from '../auth/authToken'

const base = import.meta.env.VITE_API_URL ?? ''
const DEFAULT_TIMEOUT_MS = 15_000

export type Todo = {
  id: string
  title: string
  notes: string | null
  isCompleted: boolean
  catImageObjectKey: string | null
  catImageUrl: string | null
  createdAtUtc: string
}

async function parseError(res: Response): Promise<string> {
  try {
    const j = (await res.json()) as { error?: string }
    return j.error ?? res.statusText
  } catch {
    return res.statusText
  }
}

/**
 * fetch wrapper that enforces a timeout via AbortController so a dead API
 * never leaves the UI stuck with an async-but-never-resolving request. Also
 * accepts an external signal so callers can cancel in-flight requests (used
 * by useTodos to discard stale StrictMode double-mount responses).
 */
async function apiFetch(
  input: RequestInfo | URL,
  init: RequestInit = {},
  timeoutMs: number = DEFAULT_TIMEOUT_MS,
  externalSignal?: AbortSignal,
): Promise<Response> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)
  const onExternalAbort = () => controller.abort()
  if (externalSignal) {
    if (externalSignal.aborted) controller.abort()
    else externalSignal.addEventListener('abort', onExternalAbort, { once: true })
  }
  try {
    return await fetch(input, { ...init, signal: controller.signal })
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError') {
      if (externalSignal?.aborted) throw err
      throw new Error(
        `Request timed out after ${Math.round(timeoutMs / 1000)}s. Check that the API is running (Aspire dashboard → pipeline-eval-api).`,
        { cause: err },
      )
    }
    if (err instanceof TypeError) {
      throw new Error('Network error contacting the API. Check that the Aspire AppHost is running.', {
        cause: err,
      })
    }
    throw err
  } finally {
    clearTimeout(timer)
    if (externalSignal) externalSignal.removeEventListener('abort', onExternalAbort)
  }
}

export async function fetchTodos(signal?: AbortSignal): Promise<Todo[]> {
  const res = await apiFetch(
    `${base}/api/todos`,
    { headers: await getAuthHeaders() },
    DEFAULT_TIMEOUT_MS,
    signal,
  )
  if (!res.ok) throw new Error(await parseError(res))
  return res.json() as Promise<Todo[]>
}

export async function createTodo(title: string, notes?: string): Promise<{ id: string }> {
  const res = await apiFetch(`${base}/api/todos`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify({ title, notes: notes || null }),
  })
  if (!res.ok) throw new Error(await parseError(res))
  return res.json() as Promise<{ id: string }>
}

export async function updateTodo(
  id: string,
  patch: Partial<{
    title: string
    notes: string | null
    isCompleted: boolean
    catImageObjectKey: string | null
  }>,
): Promise<Todo> {
  const res = await apiFetch(`${base}/api/todos/${id}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify(patch),
  })
  if (!res.ok) throw new Error(await parseError(res))
  return res.json() as Promise<Todo>
}

export async function deleteTodo(id: string): Promise<void> {
  const res = await apiFetch(`${base}/api/todos/${id}`, {
    method: 'DELETE',
    headers: await getAuthHeaders(),
  })
  if (!res.ok) throw new Error(await parseError(res))
}

export type UploadUrlResult = {
  uploadUrl: string
  objectKey: string
  expiresInSeconds: number
}

export async function requestUploadUrl(
  todoId: string,
  fileName: string,
  contentType: string,
): Promise<UploadUrlResult> {
  const res = await apiFetch(`${base}/api/todos/${todoId}/upload-url`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify({ fileName, contentType }),
  })
  if (res.status === 503) {
    throw new Error(
      'Uploads are disabled: configure S3:BucketName and AWS credentials for the API.',
    )
  }
  if (!res.ok) throw new Error(await parseError(res))
  return res.json() as Promise<UploadUrlResult>
}

export async function putFileToS3(
  uploadUrl: string,
  file: File,
  contentType: string,
): Promise<void> {
  // Allow up to 60s for the object PUT — images can be larger than JSON payloads.
  const res = await apiFetch(
    uploadUrl,
    {
      method: 'PUT',
      headers: { 'Content-Type': contentType },
      body: file,
    },
    60_000,
  )
  if (!res.ok) throw new Error(`Upload failed (${res.status})`)
}
