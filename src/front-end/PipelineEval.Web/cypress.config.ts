import { defineConfig } from 'cypress'

export default defineConfig({
  video: false,
  allowCypressEnv: false,
  component: {
    specPattern: 'cypress/component/**/*.cy.{js,jsx,ts,tsx}',
    supportFile: 'cypress/support/component.ts',
    indexHtmlFile: 'cypress/support/component-index.html',
    devServer: {
      framework: 'react',
      bundler: 'vite',
    },
  },
})
