import {
  NavDrawer,
  NavDrawerBody,
  NavItem,
  type OnNavItemSelectData,
} from '@fluentui/react-nav'
import { useCallback } from 'react'

export const NAV_NEW_TASK = 'new-task'
export const NAV_INVITE = 'invite'

export type NavAction = typeof NAV_NEW_TASK | typeof NAV_INVITE

type AppNavProps = {
  open: boolean
  showInvite: boolean
  onAction: (action: NavAction) => void
  onRequestClose: () => void
}

/**
 * The hamburger-triggered drawer is action-only: it exposes "New task" and,
 * for admins, "Invite teammate" as entry points to the respective modals.
 *
 * The drawer collapses the moment an item is selected. The parent still
 * owns the `open` state (so the header button can toggle it), but this
 * component ALSO calls `onRequestClose` synchronously inside the select
 * handler as a defensive belt-and-braces — the parent and the drawer agree
 * on "closed" before the dialog opens.
 */
export function AppNav({
  open,
  showInvite,
  onAction,
  onRequestClose,
}: AppNavProps) {
  const handleSelect = useCallback(
    (_: unknown, data: OnNavItemSelectData) => {
      if (data.value === NAV_NEW_TASK || data.value === NAV_INVITE) {
        onRequestClose()
        onAction(data.value)
      }
    },
    [onAction, onRequestClose],
  )

  return (
    <div
      className="nav-drawer-host"
      data-testid="nav-drawer"
      data-nav-open={open ? 'true' : 'false'}
      aria-hidden={!open}
      inert={!open}
    >
      <NavDrawer
        type="inline"
        open={open}
        separator
        position="start"
        onNavItemSelect={handleSelect}
      >
        <NavDrawerBody>
          <NavItem value={NAV_NEW_TASK} data-testid="nav-item-new-task">
            New task
          </NavItem>
          {showInvite ? (
            <NavItem value={NAV_INVITE} data-testid="nav-item-invite">
              Invite teammate
            </NavItem>
          ) : null}
        </NavDrawerBody>
      </NavDrawer>
    </div>
  )
}
