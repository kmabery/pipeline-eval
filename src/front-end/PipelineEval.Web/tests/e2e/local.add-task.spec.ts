import { expect, test } from '@playwright/test'

/**
 * Local user journey against the Aspire AppHost running on this machine.
 *
 * Prereq:  dotnet run --project src/PipelineEval.Host   (Docker Desktop running)
 * Runs with the "local" Playwright project (see playwright.config.ts). The
 * suite exercises the full create-task flow that regressed to a UI freeze:
 *   1. Open nav -> New task
 *   2. Fill title + notes, attach a cat image
 *   3. Submit and assert the item appears in "Your list" (upload completes).
 *   4. Delete so the suite is idempotent across re-runs.
 *
 * If the API is not reachable the test fails fast with an actionable message
 * instead of hanging.
 */

const FIXTURE = 'tests/e2e/fixtures/catOne.jpg'
const API_PORT = process.env.LOCAL_API_PORT ?? '5101'
const API_HEALTH_URL = `http://127.0.0.1:${API_PORT}/health`
const UNIQUE_TITLE = `Local smoke ${new Date().toISOString()}`

test.describe('local user journey', () => {
  test.beforeAll(async ({ request }) => {
    const deadline = Date.now() + 60_000
    let lastErr: unknown
    while (Date.now() < deadline) {
      try {
        const res = await request.get(API_HEALTH_URL, { timeout: 2_000 })
        if (res.ok()) return
      } catch (err) {
        lastErr = err
      }
      await new Promise((resolve) => setTimeout(resolve, 1_000))
    }
    throw new Error(
      `API is not healthy at ${API_HEALTH_URL}. Start the Aspire AppHost first ` +
        `(dotnet run --project src/PipelineEval.Host) and ensure Docker Desktop is running. ` +
        `Last error: ${lastErr instanceof Error ? lastErr.message : String(lastErr)}`,
    )
  })

  test('add task with image, verify, delete', async ({ page }) => {
    // Surface uncaught page errors in the test output so CI traces are actionable
    // without having to open the HTML report.
    page.on('pageerror', (err) => console.log('[browser:pageerror]', err.message))

    await page.goto('/')

    await expect(page.getByRole('heading', { name: 'Your list' })).toBeVisible({ timeout: 15_000 })
    await expect(page.getByText('Failed to fetch')).toHaveCount(0)

    await page.getByTestId('app-nav-hamburger').click()
    await page.getByTestId('nav-item-new-task').click()

    const dialog = page.getByRole('dialog', { name: 'New task' })
    await expect(dialog).toBeVisible()

    const form = dialog.getByTestId('new-task-form')
    await form.getByRole('textbox', { name: 'Title' }).fill(UNIQUE_TITLE)
    await form.getByRole('textbox', { name: /notes/i }).fill('Playwright local smoke')
    await form.locator('input[type="file"]').setInputFiles(FIXTURE)
    await form.getByRole('button', { name: 'Add todo' }).click()

    // Submit must unblock even if the dialog surfaces an error; the busy state
    // must not linger (regression guard for the old "Add Task freeze"). Poll
    // with a short timeout so any in-dialog error is captured in the trace.
    await expect(dialog).toBeHidden({ timeout: 20_000 })

    const yourList = page.getByTestId('section-your-list')
    const todoItem = yourList.getByRole('listitem').filter({ hasText: UNIQUE_TITLE })
    await expect(todoItem).toBeVisible({ timeout: 15_000 })

    // Cat image upload: label flips Add -> Uploading… -> Replace once the
    // presigned PUT to LocalStack S3 succeeds.
    await expect(todoItem.getByText('Replace photo')).toBeVisible({ timeout: 30_000 })
    await expect(todoItem.locator('img.thumb')).toBeVisible()

    await todoItem.getByRole('button', { name: 'Delete' }).click()
    await expect(yourList.getByRole('listitem').filter({ hasText: UNIQUE_TITLE })).toHaveCount(0)
  })
})
