import { expect, test } from '@playwright/test'

test.describe('smoke', () => {
  test('sanity check', async () => {
    expect(true).toBe(true)
  })
})
