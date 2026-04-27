import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import App from '../../src/app/App'
import { AppProviders } from '../../src/app/AppProviders'

const api = vi.hoisted(() => ({
  fetchTodos: vi.fn(() => Promise.resolve([])),
  createTodo: vi.fn(() => Promise.resolve({ id: '00000000-0000-0000-0000-000000000001' })),
  deleteTodo: vi.fn(() => Promise.resolve()),
  updateTodo: vi.fn(() =>
    Promise.resolve({
      id: '00000000-0000-0000-0000-000000000001',
      title: 'x',
      notes: null,
      isCompleted: false,
      catImageObjectKey: null,
      catImageUrl: null,
      createdAtUtc: new Date().toISOString(),
    }),
  ),
  putFileToS3: vi.fn(() => Promise.resolve()),
  requestUploadUrl: vi.fn(() =>
    Promise.resolve({
      uploadUrl: 'https://example.invalid/upload',
      objectKey: 'k',
      expiresInSeconds: 60,
    }),
  ),
}))

vi.mock('../../src/features/todos/api', () => api)

describe('App (integration)', () => {
  it('loads todos through AppProviders and shows the shell', async () => {
    render(
      <AppProviders>
        <App />
      </AppProviders>,
    )
    await waitFor(() => {
      expect(api.fetchTodos).toHaveBeenCalled()
    })
    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /todo cat pics/i })).toBeInTheDocument()
    })
  })
})
