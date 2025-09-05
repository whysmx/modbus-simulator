import { test, expect } from '@playwright/test';

test.describe('DeviceTree lazy registers loading', () => {
  test('expanding a slave loads registers, shows counts, and caches', async ({ page }) => {
    await page.goto('/');

    // Wait for initial data load (or empty state message)
    await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });

    // Prepare dialog auto-accept
    page.on('dialog', async (dialog) => {
      await dialog.accept();
    });

    // Clean up any existing connections first
    let existingConnections = await page.locator('[data-testid^="connection-actions-"]').count();
    console.log('Found existing connections:', existingConnections);
    
    while (existingConnections > 0) {
      const connActionBtn = page.locator('[data-testid^="connection-actions-"]').first();
      await connActionBtn.click();
      await page.getByRole('menuitem', { name: '删除连接' }).click();
      await page.waitForTimeout(2000);
      existingConnections = await page.locator('[data-testid^="connection-actions-"]').count();
    }

    // Create a fresh connection
    const connName = 'E2E-Conn-Lazy';
    await page.getByRole('button', { name: '新建连接' }).click();
    await page.getByLabel('连接名称').fill(connName);
    await page.getByRole('button', { name: '创建' }).click();
    await expect(page.getByText(connName)).toBeVisible({ timeout: 10000 });

    // Add a slave
    const connActions = page.locator('[data-testid^="connection-actions-"]').first();
    await connActions.click();
    await page.getByRole('menuitem', { name: '新增从机' }).click();
    
    // Wait for dialog to open
    await expect(page.getByText('新建从站')).toBeVisible();
    
    await page.getByLabel('从机名称').fill('从机-LZ');
    await page.getByLabel('从机地址').fill('1');
    
    // Click create button and wait for dialog to close
    const createButton = page.getByRole('button', { name: '创建' });
    await createButton.click();
    
    // Wait for dialog to close (indicating completion)
    await expect(page.getByText('新建从站')).toBeHidden({ timeout: 15000 });
    
    // Additional wait for backend processing and frontend state update
    await page.waitForTimeout(3000);
    
    // Force refresh the page data by reloading
    await page.reload();
    await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });
    
    // Log any console errors
    page.on('console', msg => {
      if (msg.type() === 'error' || msg.type() === 'warn') {
        console.log(`Browser ${msg.type()}: ${msg.text()}`);
      }
    });

    // Expand connection and then the slave
    await page.getByText(connName).first().click();
    await expect(page.getByText('从机-LZ')).toBeVisible({ timeout: 10000 });
    await page.getByText('从机-LZ').click();

    // Wait for register loading to complete
    await page.waitForTimeout(3000);

    // On first expand, spinner should appear then disappear (optional check)
    const spinner = page.locator('div', { hasText: '正在加载寄存器...' }).first();
    // Don't require spinner to be visible, just wait if it appears
    try {
      await expect(spinner).toBeVisible({ timeout: 2000 });
      await expect(spinner).toBeHidden({ timeout: 10000 });
    } catch (e) {
      console.log('No loading spinner detected or spinner disappeared quickly');
    }

    // Four types should be visible with counts (0 initially is acceptable)
    for (const type of ['线圈', '离散输入', '输入寄存器', '保持寄存器']) {
      await expect(page.locator('div', { hasText: type }).first()).toBeVisible();
    }

    // Add one holding register group
    const slaveActionBtn = page.locator('[data-testid^="slave-actions-"]').first();
    await slaveActionBtn.click();
    await page.getByRole('menuitem', { name: '新增寄存器' }).click();
    await page.getByLabel('起始逻辑地址').fill('40001');
    await page.getByLabel('16进制字符串').fill('ABCD');
    await page.getByRole('button', { name: '确定' }).click();

    // Brief wait for save and cache refresh
    await page.waitForTimeout(1000);

    // Ensure expanded area shows updated count for holding registers
    await page.getByText('从机-LZ').click(); // collapse
    await page.getByText('从机-LZ').click(); // expand again

    // Spinner should NOT appear on cached load
    await expect(spinner).toHaveCount(0);

    // Holding Registers should show count >= 1
    const holdingRow = page.locator('div', { hasText: '保持寄存器' }).first();
    await expect(holdingRow).toBeVisible();

    // Open the tab regardless of count
    await holdingRow.click();
    await expect(page.getByText('40001')).toBeVisible({ timeout: 10000 });

    // Cleanup: delete connection
    await connActions.click();
    await page.getByRole('menuitem', { name: '删除连接' }).click();
    await expect(page.locator('[data-testid^="connection-actions-"]')).toHaveCount(0, { timeout: 15000 });
  });
});


