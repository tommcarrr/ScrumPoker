import { test, expect, Page, ConsoleMessage } from '@playwright/test';

// Utility to collect console errors
async function captureConsoleErrors(page: Page) {
  const errors: string[] = [];
  page.on('console', (msg: ConsoleMessage) => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });
  return errors;
}

test.describe('ScrumPoker root page', () => {
  test('loads without Blazor root component errors', async ({ page }) => {
    const errors = await captureConsoleErrors(page);
    await page.goto('/');

    // Basic sanity: title or visible element (fallback to checking any h1, nav, or body content)
    await expect(page.locator('body')).toBeVisible();

    // Wait for Blazor to start (look for the boot script or a known element from layout)
    await page.waitForSelector('nav, header, footer, body');

    // Fail test if specific aggregate/Routes error appears
    const aggregated = errors.filter(e => e.includes('Root component type') && e.includes('Routes'));
    if (aggregated.length > 0) {
      throw new Error('Encountered Blazor root component error: ' + aggregated.join('\n'));
    }
  });
});
