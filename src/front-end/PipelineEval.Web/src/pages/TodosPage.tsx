import { useCallback, useState } from 'react'
import { useAuth } from '../features/auth/AuthContext'
import { useTodos } from '../features/todos/useTodos'
import { useCatPhotoUpload } from '../features/todos/useCatPhotoUpload'
import { useCreateTodoWithPhoto } from '../features/todos/useCreateTodoWithPhoto'
import { TodoList } from '../features/todos/components/TodoList'
import { NewTaskDialog } from '../features/todos/components/NewTaskDialog'
import { InviteDialog } from '../features/invite/components/InviteDialog'
import { AppHeader } from '../shell/AppHeader'
import {
  AppNav,
  NAV_INVITE,
  NAV_NEW_TASK,
  type NavAction,
} from '../shell/AppNav'
import { ErrorBanner } from '../shared/ui/ErrorBanner'

/**
 * The post-authentication "home" route. Owns the todo list lifecycle, the
 * create-task orchestration, and the three transient UI states (drawer +
 * two dialogs). App.tsx stays free of IO and just decides which page to
 * render based on auth state.
 */
export function TodosPage() {
  const { cognitoConfigured, isAuthenticated, isAdmin, signOut } = useAuth()

  const {
    todos,
    listLoading,
    error,
    setError,
    reload,
    toggleTodo,
    deleteTodo,
  } = useTodos({ enabled: true })

  const { busyId, uploadForTodo } = useCatPhotoUpload({
    onError: (message) => setError(message),
    onUploaded: () => reload(),
  })

  const createTodoWithPhoto = useCreateTodoWithPhoto({
    onCreated: reload,
    clearError: () => setError(null),
  })

  const [navOpen, setNavOpen] = useState(false)
  const [newTaskOpen, setNewTaskOpen] = useState(false)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [infoBanner, setInfoBanner] = useState<string | null>(null)

  const handleNavAction = useCallback((action: NavAction) => {
    setNavOpen(false)
    if (action === NAV_NEW_TASK) {
      setNewTaskOpen(true)
      return
    }
    if (action === NAV_INVITE) {
      setInviteOpen(true)
    }
  }, [])

  const closeNav = useCallback(() => setNavOpen(false), [])

  return (
    <div className="app">
      <AppHeader
        navOpen={navOpen}
        onToggleNav={() => setNavOpen((o) => !o)}
        showSignOut={cognitoConfigured && isAuthenticated}
        onSignOut={signOut}
      />

      {error ? <ErrorBanner message={error} /> : null}
      {infoBanner ? <ErrorBanner tone="info" message={infoBanner} /> : null}

      <div className="app-shell">
        <AppNav
          open={navOpen}
          showInvite={!cognitoConfigured || isAdmin}
          onAction={handleNavAction}
          onRequestClose={closeNav}
        />

        <main className="app-main">
          <TodoList
            todos={todos}
            listLoading={listLoading}
            busyId={busyId}
            onToggle={(t) => void toggleTodo(t)}
            onDelete={(id) => void deleteTodo(id)}
            onPickFile={(id, file) => void uploadForTodo(id, file)}
          />
        </main>
      </div>

      <NewTaskDialog
        open={newTaskOpen}
        onOpenChange={setNewTaskOpen}
        onSubmit={createTodoWithPhoto}
      />

      <InviteDialog
        open={inviteOpen}
        onOpenChange={setInviteOpen}
        onError={(message) => setError(message)}
        onSuccess={(message) => {
          setInfoBanner(message)
          setError(null)
        }}
      />
    </div>
  )
}
