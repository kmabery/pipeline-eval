import { Body1, Title3 } from '@fluentui/react-components'
import { TodoItem } from './TodoItem'
import type { Todo } from '../api'

type TodoListProps = {
  todos: Todo[]
  listLoading: boolean
  busyId: string | null
  onToggle: (todo: Todo) => void
  onDelete: (id: string) => void
  onPickFile: (id: string, file: File | null) => void
}

export function TodoList({
  todos,
  listLoading,
  busyId,
  onToggle,
  onDelete,
  onPickFile,
}: TodoListProps) {
  return (
    <section
      id="your-list"
      className="content-section"
      data-testid="section-your-list"
      aria-labelledby="your-list-heading"
      aria-busy={listLoading}
    >
      <Title3 as="h2" className="section-heading" id="your-list-heading">
        Your list
      </Title3>
      {listLoading ? (
        <Body1 className="muted" aria-live="polite">
          Loading…
        </Body1>
      ) : null}
      {!listLoading && todos.length === 0 ? (
        <Body1 as="p" className="muted empty-state">
          No todos yet. Open the menu and choose <strong>New task</strong> to add one.
        </Body1>
      ) : null}
      <ul className="todo-list">
        {todos.map((t) => (
          <TodoItem
            key={t.id}
            todo={t}
            busyId={busyId}
            onToggle={onToggle}
            onDelete={onDelete}
            onPickFile={onPickFile}
          />
        ))}
      </ul>
    </section>
  )
}
