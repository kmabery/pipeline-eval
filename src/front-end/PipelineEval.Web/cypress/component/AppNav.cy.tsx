import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import {
  AppNav,
  NAV_INVITE,
  NAV_NEW_TASK,
  type NavAction,
} from '../../src/shell/AppNav'

function Harness(props: {
  initialOpen?: boolean
  showInvite: boolean
  onActionCalls: NavAction[]
  onClose?: () => void
}) {
  return (
    <FluentProvider theme={webLightTheme}>
      <AppNav
        open={props.initialOpen ?? true}
        showInvite={props.showInvite}
        onAction={(a) => props.onActionCalls.push(a)}
        onRequestClose={() => props.onClose?.()}
      />
    </FluentProvider>
  )
}

describe('AppNav (burger menu)', () => {
  it('shows only "New task" for non-admins', () => {
    cy.mount(<Harness showInvite={false} onActionCalls={[]} />)
    cy.get('[data-testid="nav-item-new-task"]').should('exist')
    cy.get('[data-testid="nav-item-invite"]').should('not.exist')
  })

  it('shows both "New task" and "Invite teammate" for admins and nothing else', () => {
    cy.mount(<Harness showInvite onActionCalls={[]} />)
    cy.get('[data-testid="nav-item-new-task"]').should('exist')
    cy.get('[data-testid="nav-item-invite"]').should('exist')
    cy.get('[data-testid="nav-drawer"]')
      .find('[data-testid^="nav-item-"]')
      .should('have.length', 2)
  })

  it('fires onAction with NAV_NEW_TASK when "New task" is selected', () => {
    const calls: NavAction[] = []
    cy.mount(<Harness showInvite onActionCalls={calls} />)
    cy.get('[data-testid="nav-item-new-task"]')
      .click()
      .then(() => {
        expect(calls).to.deep.equal([NAV_NEW_TASK])
      })
  })

  it('requests close before firing onAction when an item is selected', () => {
    const calls: NavAction[] = []
    const closeCalls: string[] = []
    cy.mount(
      <Harness
        showInvite
        onActionCalls={calls}
        onClose={() => closeCalls.push('close')}
      />,
    )
    cy.get('[data-testid="nav-item-invite"]')
      .click()
      .then(() => {
        expect(closeCalls).to.deep.equal(['close'])
        expect(calls).to.deep.equal([NAV_INVITE])
      })
  })

  it('is inert and aria-hidden when closed', () => {
    cy.mount(<Harness initialOpen={false} showInvite onActionCalls={[]} />)
    cy.get('[data-testid="nav-drawer"]')
      .should('have.attr', 'aria-hidden', 'true')
      .and('have.attr', 'data-nav-open', 'false')
  })
})
