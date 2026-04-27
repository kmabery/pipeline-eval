import { Button, Title3, Tooltip } from '@fluentui/react-components'
import { Hamburger } from '@fluentui/react-nav'

type AppHeaderProps = {
  navOpen: boolean
  onToggleNav: () => void
  showSignOut: boolean
  onSignOut: () => void
}

export function AppHeader({
  navOpen,
  onToggleNav,
  showSignOut,
  onSignOut,
}: AppHeaderProps) {
  const label = navOpen ? 'Collapse navigation' : 'Expand navigation'
  return (
    <header className="header">
      <div className="header-row">
        <div className="header-brand">
          <div className="header-toolbar">
            <Tooltip content={label} relationship="label">
              <Hamburger
                appearance="primary"
                data-testid="app-nav-hamburger"
                aria-expanded={navOpen}
                aria-label={label}
                onClick={onToggleNav}
              />
            </Tooltip>
            <Title3 as="h1" className="header-title">
              Todo cat pics
            </Title3>
          </div>
        </div>
        {showSignOut ? (
          <Button appearance="secondary" onClick={onSignOut}>
            Sign out
          </Button>
        ) : null}
      </div>
    </header>
  )
}
