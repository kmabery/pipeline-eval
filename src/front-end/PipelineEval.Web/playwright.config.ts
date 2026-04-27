import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig, devices } from '@playwright/test'

const repoRoot = path.resolve(fileURLToPath(new URL('.', import.meta.url)), '../../..')

// Small inline .env loader so the root .env drives LOCAL_* ports here too,
// without adding a dotenv devDependency. Existing process.env wins.
function loadRepoDotenvIntoProcess(): void {
  const envPath = path.join(repoRoot, '.env')
  if (!fs.existsSync(envPath)) return
  const raw = fs.readFileSync(envPath, 'utf8')
  for (const line of raw.split(/\r?\n/)) {
    const trimmed = line.trim()
    if (!trimmed || trimmed.startsWith('#')) continue
    const eq = trimmed.indexOf('=')
    if (eq <= 0) continue
    const key = trimmed.slice(0, eq).trim()
    let value = trimmed.slice(eq + 1).trim()
    if (
      (value.startsWith('"') && value.endsWith('"')) ||
      (value.startsWith("'") && value.endsWith("'"))
    ) {
      value = value.slice(1, -1)
    }
    if (process.env[key] === undefined) process.env[key] = value
  }
}

loadRepoDotenvIntoProcess()

const webPort = process.env.LOCAL_WEB_PORT ?? '5173'
const localBaseUrl = `http://127.0.0.1:${webPort}`
const deployedBaseUrl = process.env.E2E_BASE_URL ?? localBaseUrl

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI
    ? [['list'], ['html', { open: 'never' }], ['junit', { outputFile: 'test-results/junit.xml' }]]
    : 'list',
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      // Runs against the local Aspire stack. Start the AppHost first:
      //   dotnet run --project src/PipelineEval.Host
      // then: npm run test:e2e:local
      name: 'local',
      testMatch: /(local|auth)\..*\.spec\.ts$/,
      use: { ...devices['Desktop Chrome'], baseURL: localBaseUrl },
    },
    {
      // Runs against E2E_BASE_URL (CloudFront in CI). Requires E2E_TEST_EMAIL/PASSWORD.
      // Keep `deployed.*` only: `auth.signin` is a pre-auth UI guard that must run on a cold
      // sign-in surface; after the deployed journey it would see the post-auth shell and flake.
      name: 'deployed',
      testMatch: /deployed\..*\.spec\.ts$/,
      use: { ...devices['Desktop Chrome'], baseURL: deployedBaseUrl },
    },
  ],
})
