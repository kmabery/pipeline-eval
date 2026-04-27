import { expect, test } from '@playwright/test'

/** Mirrors `cognitoPool.ts` — Playwright runs in Node; use env loaded from repo `.env`. */
const cognitoConfigured = Boolean(
  process.env.VITE_COGNITO_USER_POOL_ID && process.env.VITE_COGNITO_CLIENT_ID,
)

/**
 * Pre-auth regression guard. Uses whatever baseURL Playwright has
 * (`E2E_BASE_URL` in CI, `http://127.0.0.1:5173` locally).
 *
 * Ensures the public sign-in screen only exposes sign-in / sign-up / confirm
 * and no longer advertises the "Invite user" flow, which moved to the
 * admin-only post-auth surface.
 *
 * When Cognito env is absent, `App` renders `TodosPage` directly (no auth shell);
 * there is nothing to assert here — skip instead of a false red.
 */
test.describe('auth screen (pre sign-in)', () => {
  test.beforeEach(async ({ context }) => {
    await context.clearCookies()
    // Cognito persists session in localStorage; clear before each load so we
    // always hit the public sign-in shell (matches cold browser / CI).
    await context.addInitScript(() => {
      try {
        localStorage.clear()
        sessionStorage.clear()
      } catch {
        /* ignore non-browser contexts */
      }
    })
  })

  test('shows expected tabs and hides Invite user', async ({ page }) => {
    test.skip(
      !cognitoConfigured,
      'VITE_COGNITO_USER_POOL_ID + VITE_COGNITO_CLIENT_ID not set; app has no sign-in shell locally.',
    )

    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')

    await expect(page.getByRole('heading', { name: /todo cat pics/i })).toBeVisible({ timeout: 20_000 })

    // Auth tabs live in a dedicated container (not inside the <form>).
    const tabs = page.locator('.auth-tabs')
    await expect(tabs.getByRole('button', { name: 'Sign in' })).toBeVisible({ timeout: 20_000 })
    await expect(tabs.getByRole('button', { name: 'Sign up' })).toBeVisible({ timeout: 20_000 })
    await expect(tabs.getByRole('button', { name: 'Confirm email' })).toBeVisible({ timeout: 20_000 })

    // Regression guard: Invite user must NEVER appear pre-authentication.
    await expect(tabs.getByRole('button', { name: 'Invite user' })).toHaveCount(0)
    await expect(page.getByRole('heading', { name: /invite by email/i })).toHaveCount(0)
  })
})
