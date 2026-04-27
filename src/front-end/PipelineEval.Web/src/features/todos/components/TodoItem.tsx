import { Button } from '@fluentui/react-components'
import { IMAGE_ACCEPT_ATTR } from '../useCatPhotoUpload'
import type { Todo } from '../api'

type TodoItemProps = {
  todo: Todo
  busyId: string | null
  onToggle: (todo: Todo) => void
  onDelete: (id: string) => void
  onPickFile: (id: string, file: File | null) => void
}

export function TodoItem({
  todo,
  busyId,
  onToggle,
  onDelete,
  onPickFile,
}: TodoItemProps) {
  const uploading = busyId === todo.id
  const disableControls = busyId !== null

  return (
    <li className={`card todo ${todo.isCompleted ? 'done' : ''}`}>
      <div className="todo-main">
        <label className="check">
          <input
            type="checkbox"
            checked={todo.isCompleted}
            onChange={() => onToggle(todo)}
          />
          <span className="todo-title">{todo.title}</span>
        </label>
        <div className="todo-body">
          {todo.notes ? <p className="todo-notes">{todo.notes}</p> : null}
          <div className="todo-meta">
            <span className="muted">
              {new Date(todo.createdAtUtc).toLocaleString()}
            </span>
          </div>
        </div>
      </div>
      <div className="todo-side">
        {todo.catImageUrl ? (
          <a
            href={todo.catImageUrl}
            target="_blank"
            rel="noreferrer"
            className="thumb-link"
            aria-label={`Open full-size cat photo for “${todo.title}”`}
          >
            <img
              src={todo.catImageUrl}
              alt={`Cat photo for “${todo.title}”`}
              className="thumb"
              loading="lazy"
              decoding="async"
            />
          </a>
        ) : null}
        <label className="btn secondary upload-label">
          {uploading
            ? 'Uploading…'
            : todo.catImageUrl
              ? 'Replace photo'
              : 'Add cat photo'}
          <input
            type="file"
            accept={IMAGE_ACCEPT_ATTR}
            className="sr-only"
            disabled={disableControls}
            onChange={(e) => onPickFile(todo.id, e.target.files?.[0] ?? null)}
          />
        </label>
        <Button
          appearance="secondary"
          className="btn-ghost-danger"
          type="button"
          onClick={() => onDelete(todo.id)}
          disabled={disableControls}
        >
          Delete
        </Button>
      </div>
    </li>
  )
}
