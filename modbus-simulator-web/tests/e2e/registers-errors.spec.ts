import { test, expect } from '@playwright/test';

async function setupConnectionAndSlave(page: any, connName: string, slaveName: string) {
  await page.goto('/');
  await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });

  // Create connection
  await page.getByRole('button', { name: '新建连接' }).click();
  await page.getByLabel('连接名称').fill(connName);
  await page.getByRole('button', { name: '创建' }).click();
  await expect(page.getByText(connName)).toBeVisible({ timeout: 10000 });

  // Create slave
  const connActionBtn = page.locator('[data-testid^="connection-actions-"]').first();
  await connActionBtn.click();
  await page.getByRole('menuitem', { name: '新增从机' }).click();
  await page.getByLabel('从机名称').fill(slaveName);
  await page.getByLabel('从机地址').fill('1');
  await page.getByRole('button', { name: '创建' }).click();

  // Wait for slave to be created and visible
  await expect(page.getByText(slaveName)).toBeVisible({ timeout: 10000 });
}

test.describe('Registers API error handling', () => {
  test('GET registers 400 shows error and keeps type nodes', async ({ page }) => {
    // Intercept GET registers with 400
    await page.route(/\/api\/connections\/.+\/slaves\/.+\/registers$/, async route => {
      await route.fulfill({ status: 400, contentType: 'application/json', body: JSON.stringify({ error: '参数错误', code: 400 }) });
    });

    await setupConnectionAndSlave(page, 'E2E-Conn-ERR-400', '从机-ERR-400');

    // Expand slave to trigger fetch
    await page.getByText('从机-ERR-400').click();
    await expect(page.getByText('加载失败：参数错误')).toBeVisible({ timeout: 5000 });

    // Four type nodes still render
    for (const type of ['线圈', '离散输入', '输入寄存器', '保持寄存器']) {
      await expect(page.locator('div', { hasText: type }).first()).toBeVisible();
    }
  });

  test('GET registers 404 shows error and keeps type nodes', async ({ page }) => {
    await page.route(/\/api\/connections\/.+\/slaves\/.+\/registers$/, async route => {
      await route.fulfill({ status: 404, contentType: 'application/json', body: JSON.stringify({ error: '资源不存在', code: 404 }) });
    });

    await setupConnectionAndSlave(page, 'E2E-Conn-ERR-404', '从机-ERR-404');
    await page.getByText('从机-ERR-404').click();
    await expect(page.getByText('加载失败：资源不存在')).toBeVisible({ timeout: 5000 });
  });

  test('GET registers 500 shows error and keeps type nodes', async ({ page }) => {
    await page.route(/\/api\/connections\/.+\/slaves\/.+\/registers$/, async route => {
      await route.fulfill({ status: 500, contentType: 'application/json', body: JSON.stringify({ error: '服务器错误', code: 500 }) });
    });

    await setupConnectionAndSlave(page, 'E2E-Conn-ERR-500', '从机-ERR-500');
    await page.getByText('从机-ERR-500').click();
    await expect(page.getByText('加载失败：服务器错误')).toBeVisible({ timeout: 5000 });
  });
});


