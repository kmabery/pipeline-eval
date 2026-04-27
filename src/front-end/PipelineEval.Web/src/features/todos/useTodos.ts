import { useCallback, useEffect, useRef, useState } from 'react'
import {
  deleteTodo as apiDeleteTodo,
  fetchTodos,
  updateTodo as apiUpdateTodo,
  type Todo,
} from './api'

type UseTodosOptions = {
  enabled: boolean
}

/**
 * Dedupe a list by `id` keeping the first occurrence. Defensive guard so a
 * future regression (in the API, in a proxy, or in StrictMode wiring) can
 * never surface the same row twice in the UI.
 */
function dedupeById(list: Todo[]): Todo[] {
  const seen = new Set<string>()
  const out: Todo[] = []
  for (const t of list) {
    if (seen.has(t.id)) continue
    seen.add(t.id)
    out.push(t)
  }
  return out
}

/**
 * Owns the todo list lifecycle. React's StrictMode intentionally runs mount
 * effects twice in development to flush out resource-leak bugs — we handle
 * that by (a) aborting the first request on cleanup via AbortController and
 * (b) tagging each request with a monotonic id so only the latest response
 * can commit, plus a defensive `dedupeById` on the committed list.
 */
export function useTodos({ enabled }: UseTodosOptions) {
  const [todos, setTodos] = useState<Todo[]>([])
  const [listLoading, setListLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const latestReqRef = useRef(0)

  const load = useCallback(async (signal?: AbortSignal) => {
    const reqId = ++latestReqRef.current
    setError(null)
    setListLoading(true)
    try {
      const result = await fetchTodos(signal)
      if (signal?.aborted) return
      if (reqId !== latestReqRef.current) return
      setTodos(dedupeById(result))
    } catch (e) {
      if (signal?.aborted) return
      if (reqId !== latestReqRef.current) return
      if (e instanceof DOMException && e.name === 'AbortError') return
      setError(e instanceof Error ? e.message : 'Failed to load todos.')
    } finally {
      if (reqId === latestReqRef.current) setListLoading(false)
    }
  }, [])

  useEffect(() => {
    if (!enabled) return
    const ac = new AbortController()
    // Defer the initial fetch onto a microtask so the effect body does not
    // set state synchronously during render; the AbortController still lets
    // us discard the in-flight response if the effect's cleanup runs first
    // (e.g. StrictMode double-mount in development).
    queueMicrotask(() => {
      if (ac.signal.aborted) return
      void load(ac.signal)
    })
    return () => ac.abort()
  }, [enabled, load])

  const reload = useCallback(() => load(), [load])

  const toggleTodo = useCallback(
    async (t: Todo) => {
      setError(null)
      try {
        await apiUpdateTodo(t.id, { isCompleted: !t.isCompleted })
        await load()
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Update failed.')
      }
    },
    [load],
  )

  const deleteTodo = useCallback(
    async (id: string) => {
      setError(null)
      try {
        await apiDeleteTodo(id)
        await load()
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Delete failed.')
      }
    },
    [load],
  )

  return {
    todos,
    listLoading,
    error,
    setError,
    reload,
    toggleTodo,
    deleteTodo,
  }
}

export type { Todo }
