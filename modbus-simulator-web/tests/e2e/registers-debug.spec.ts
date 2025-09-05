import { test, expect } from '@playwright/test';

test.describe('Registers Debug', () => {
  test('debug register creation and display', async ({ page }) => {
    await page.goto('/');

    // Wait for initial data load
    await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });

    // Setup dialog handlers
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
      await page.waitForTimeout(2000);
      existingConnections = await page.locator('[data-testid^="connection-actions-"]').count();
    }

    // Create connection and slave for this test
    const connName = 'Debug-Conn-Reg';
    await page.getByRole('button', { name: '新建连接' }).click();
    await page.getByLabel('连接名称').fill(connName);
    await page.getByRole('button', { name: '创建' }).click();
    await expect(page.getByText(connName)).toBeVisible({ timeout: 10000 });

    const slaveName = '从机-Debug';
    const connActionBtn = page.locator('[data-testid^="connection-actions-"]').first();
    await connActionBtn.click();
    await page.getByRole('menuitem', { name: '新增从机' }).click();
    await page.getByLabel('从机名称').fill(slaveName);
    await page.getByRole('button', { name: '创建' }).click();
    await expect(page.getByText(slaveName)).toBeVisible({ timeout: 10000 });

    // Create register with API response monitoring
    console.log('Creating register...');
    
    // Monitor network requests
    const responses: any[] = [];
    page.on('response', response => {
      if (response.url().includes('/api/')) {
        responses.push({
          url: response.url(),
          status: response.status(),
          method: response.request().method()
        });
        console.log(`API call: ${response.request().method()} ${response.url()} - ${response.status()}`);
      }
    });
    
    const slaveActionBtn = page.locator('[data-testid^="slave-actions-"]').first();
    await slaveActionBtn.click();
    await page.getByRole('menuitem', { name: '新增寄存器' }).click();
    await page.getByLabel('起始逻辑地址').fill('40001');
    await page.getByLabel('16进制字符串').fill('ABCD');
    await page.getByRole('button', { name: '确定' }).click();

    console.log('Register creation dialog submitted, waiting...');
    await page.waitForTimeout(3000);
    
    console.log('API responses during register creation:', responses.filter(r => r.url.includes('registers')));

    // Click slave to select it
    console.log('Clicking slave to select it...');
    await page.getByText(slaveName).click();
    await page.waitForTimeout(1000);

    // Check if register types are visible
    const holdingRegisterVisible = await page.locator('div', { hasText: '保持寄存器' }).count();
    console.log('保持寄存器 elements found:', holdingRegisterVisible);

    if (holdingRegisterVisible > 0) {
      console.log('Clicking 保持寄存器...');
      await page.locator('div', { hasText: '保持寄存器' }).first().click();
      await page.waitForTimeout(2000);

      // Check if 40001 is visible
      const register40001Visible = await page.getByText('40001').count();
      console.log('40001 elements found:', register40001Visible);
    }

    // Take a screenshot for debugging
    await page.screenshot({ path: 'debug-registers.png', fullPage: true });

    // Just pass the test to see the debug output
    expect(true).toBe(true);
  });
});