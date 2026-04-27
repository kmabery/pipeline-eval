import { expect, test } from '@playwright/test'

/**
 * Deployed user journey against E2E_BASE_URL (CloudFront in CI).
 *
 * Covers the user's smoke flow:
 *   1. Sign in with seeded credentials.
 *   2. Open the nav, choose New task, add a task with an optional cat image in the modal.
 *   3. Validates API + presigned S3 PUT + CORS.
 *   4. Delete the task so the suite is idempotent.
 *
 * Also fails the test with a readable diagnosis if any network request fails
 * (the "Failed to fetch" fingerprint we just fixed).
 */

const EMAIL = process.env.E2E_TEST_EMAIL
const PASSWORD = process.env.E2E_TEST_PASSWORD
const FIXTURE = 'tests/e2e/fixtures/catOne.jpg'
const UNIQUE_TITLE = `Deployed smoke ${new Date().toISOString()}`

test.describe('deployed user journey', () => {
  test.skip(
    !EMAIL || !PASSWORD,
    'Set E2E_TEST_EMAIL and E2E_TEST_PASSWORD env vars (see .env.example) to run the deployed suite.',
  )

  test.beforeEach(async ({ page }, testInfo) => {
    // Diagnostic only. Chromium can fire `requestfailed` with ERR_ABORTED
    // even when the HTTP response succeeded, if the JS that issued the fetch
    // re-renders before consuming the response (common with React state flips
    // after upload). The real user-visible failure is the in-page
    // "Failed to fetch" banner, which we assert below. Record the noise here
    // so deployed failures have actionable network diagnostics in the report.
    page.on('requestfailed', (req) => {
      const url = req.url()
      if (url.includes('amazonaws.com') || url.includes('awsapprunner.com')) {
        const failure = req.failure()?.errorText ?? 'unknown'
        testInfo.annotations.push({
          type: 'network-failure',
          description: `${req.method()} ${url} -> ${failure}`,
        })
      }
    })
  })

  test('sign in, add task, upload cat photo, delete', async ({ page }) => {
    // CloudFront + SPA can exceed the default 30s for first paint; login form
    // must be visible before fill() (avoids "waiting for getByRole('textbox'…" timeouts).
    test.setTimeout(120_000)
    await page.goto('/', { waitUntil: 'domcontentloaded' })

    const email = page.getByRole('textbox', { name: 'Email' })
    const password = page.getByRole('textbox', { name: 'Password' })
    await expect(email).toBeVisible({ timeout: 60_000 })
    await expect(password).toBeVisible()
    await email.fill(EMAIL!)
    await password.fill(PASSWORD!)
    await page.locator('form').getByRole('button', { name: 'Sign in' }).click()

    await expect(page.getByRole('heading', { name: 'Your list' })).toBeVisible()
    await expect(page.getByText('Failed to fetch')).toHaveCount(0)

    await expect(page.getByTestId('nav-drawer')).toBeAttached()

    await page.getByTestId('app-nav-hamburger').click()
    await page.getByTestId('nav-item-new-task').click()

    const dialog = page.getByRole('dialog', { name: 'New task' })
    await expect(dialog).toBeVisible()

    const newTaskForm = dialog.getByTestId('new-task-form')
    await newTaskForm.getByRole('textbox', { name: 'Title' }).fill(UNIQUE_TITLE)
    await newTaskForm.getByRole('textbox', { name: /notes/i }).fill('Playwright deployed smoke')
    await newTaskForm.locator('input[type="file"]').setInputFiles(FIXTURE)
    await newTaskForm.getByRole('button', { name: 'Add todo' }).click()

    const yourList = page.getByTestId('section-your-list')
    const todoItem = yourList.getByRole('listitem').filter({ hasText: UNIQUE_TITLE })
    await expect(todoItem).toBeVisible()
    await expect(page.getByText('Failed to fetch')).toHaveCount(0)

    // App flips the label from "Add cat photo" -> "Uploading…" -> "Replace photo"
    // once the presigned PUT succeeds and the thumbnail loads (upload ran in the modal).
    await expect(todoItem.getByText('Replace photo')).toBeVisible({ timeout: 30_000 })
    await expect(todoItem.locator('img.thumb')).toBeVisible()
    await expect(page.getByText('Failed to fetch')).toHaveCount(0)

    await todoItem.getByRole('button', { name: 'Delete' }).click()
    await expect(yourList.getByRole('listitem').filter({ hasText: UNIQUE_TITLE })).toHaveCount(0)
    await expect(page.getByText('Failed to fetch')).toHaveCount(0)
  })
})
