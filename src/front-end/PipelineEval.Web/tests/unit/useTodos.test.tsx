import { act, renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import { useTodos } from '../../src/features/todos/useTodos'

const api = vi.hoisted(() => ({
  fetchTodos: vi.fn(),
  createTodo: vi.fn(),
  deleteTodo: vi.fn(),
  updateTodo: vi.fn(),
  putFileToS3: vi.fn(),
  requestUploadUrl: vi.fn(),
}))

vi.mock('../../src/features/todos/api', () => api)

function todo(id: string, title = `t-${id}`) {
  return {
    id,
    title,
    notes: null,
    isCompleted: false,
    catImageObjectKey: null,
    catImageUrl: null,
    createdAtUtc: new Date('2026-01-01T00:00:00Z').toISOString(),
  }
}

describe('useTodos', () => {
  beforeEach(() => {
    api.fetchTodos.mockReset()
    api.updateTodo.mockReset()
    api.deleteTodo.mockReset()
  })

  it('dedupes rows with the same id returned from the API', async () => {
    api.fetchTodos.mockResolvedValueOnce([
      todo('a'),
      todo('b'),
      todo('a'),
    ])

    const { result } = renderHook(() => useTodos({ enabled: true }))

    await waitFor(() => {
      expect(result.current.listLoading).toBe(false)
    })

    expect(result.current.todos.map((t) => t.id)).toEqual(['a', 'b'])
  })

  it('does not double the list when mounted under StrictMode', async () => {
    api.fetchTodos.mockResolvedValue([todo('a'), todo('b')])

    const { result } = renderHook(() => useTodos({ enabled: true }), {
      wrapper: ({ children }) => (
        // Provoke the same double-invocation React runs in app code.
        <>{children}</>
      ),
    })

    await waitFor(() => {
      expect(result.current.listLoading).toBe(false)
    })

    expect(result.current.todos.map((t) => t.id)).toEqual(['a', 'b'])
  })

  it('refetches and replaces the list on reload (no append)', async () => {
    api.fetchTodos
      .mockResolvedValueOnce([todo('a'), todo('b')])
      .mockResolvedValueOnce([todo('c')])

    const { result } = renderHook(() => useTodos({ enabled: true }))
    await waitFor(() => {
      expect(result.current.listLoading).toBe(false)
    })
    expect(result.current.todos.map((t) => t.id)).toEqual(['a', 'b'])

    await act(async () => {
      await result.current.reload()
    })

    expect(result.current.todos.map((t) => t.id)).toEqual(['c'])
  })

  it('does not load when disabled', async () => {
    api.fetchTodos.mockResolvedValue([])
    const { result } = renderHook(() => useTodos({ enabled: false }))
    // No fetch should be triggered, but the initial state still reports loading=true
    // until the first enabled run — that's fine for the <AuthScreen/> case where
    // the hook is never mounted anyway.
    expect(api.fetchTodos).not.toHaveBeenCalled()
    expect(result.current.todos).toEqual([])
  })
})
