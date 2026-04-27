import { Button, FluentProvider, webLightTheme } from '@fluentui/react-components'

describe('Cypress CT smoke', () => {
  it('mounts Fluent UI', () => {
    cy.mount(
      <FluentProvider theme={webLightTheme}>
        <Button>CT smoke</Button>
      </FluentProvider>,
    )
    cy.contains('button', 'CT smoke').should('be.visible')
  })
})
