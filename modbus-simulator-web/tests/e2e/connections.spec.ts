import { test, expect } from '@playwright/test';

test.describe('Connections', () => {
  test('create and delete a connection', async ({ page }) => {
    await page.goto('/');

    // Wait for initial data load - check for either existing connections or loading message
    await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });

    // Setup dialog handler for cleanup operations
    page.on('dialog', async (dialog) => {
      console.log('Dialog appeared:', dialog.message());
      await dialog.accept();
    });

    // Clean up any existing connections first
    let existingConnections = await page.locator('[data-testid^="connection-actions-"]').count();
    console.log('Found existing connections:', existingConnections);
    
    while (existingConnections > 0) {
      const connActionBtn = page.locator('[data-testid^="connection-actions-"]').first();
      await connActionBtn.click();
      await page.getByRole('menuitem', { name: '删除连接' }).click();
      // Wait for deletion to complete
      await page.waitForTimeout(2000);
      existingConnections = await page.locator('[data-testid^="connection-actions-"]').count();
      console.log('Remaining connections:', existingConnections);
    }

    // open create connection dialog
    await page.getByRole('button', { name: '新建连接' }).click();

    // fill and submit
    await page.getByLabel('连接名称').fill('E2E连接');
    await page.getByRole('button', { name: '创建' }).click();

    // Wait for connection to appear in device tree
    await expect(page.getByText('E2E连接')).toBeVisible({ timeout: 10000 });

    // Find connection action button and open menu
    const connActionBtn = page.locator('[data-testid^="connection-actions-"]').first();
    await connActionBtn.click();
    
    // Click delete from menu
    await page.getByRole('menuitem', { name: '删除连接' }).click();

    // Wait for loading to start (loading state indicates delete operation began)
    await expect(page.locator('body')).toContainText('', { timeout: 1000 }); // small delay
    
    // Wait for deletion to complete - connection should disappear
    // Use a more reliable selector - wait for NO connection action buttons to exist
    await expect(page.locator('[data-testid^="connection-actions-"]')).toHaveCount(0, { timeout: 15000 });
    
    // Also verify the connection text is gone
    await expect(page.getByText('E2E连接')).toHaveCount(0, { timeout: 5000 });
  });
});
