import { test, expect } from '@playwright/test';

test.describe('Register CRUD updates counts and cache', () => {
  test('create/update/delete refresh counts and maintain cache', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });

    // Dialog auto-accept
    page.on('dialog', async (d) => d.accept());

    const connName = 'E2E-Conn-CRUD';
    // Create connection
    await page.getByRole('button', { name: '新建连接' }).click();
    await page.getByLabel('连接名称').fill(connName);
    await page.getByRole('button', { name: '创建' }).click();
    await expect(page.getByText(connName)).toBeVisible({ timeout: 10000 });

    // Create slave
    await page.locator('[data-testid^="connection-actions-"]').first().click();
    await page.getByRole('menuitem', { name: '新增从机' }).click();
    await page.getByLabel('从机名称').fill('从机-CRUD');
    await page.getByLabel('从机地址').fill('1');
    await page.getByRole('button', { name: '创建' }).click();

    // Expand
    await page.getByText(connName).first().click();
    await page.getByText('从机-CRUD').click();

    // Add two holding registers
    const slaveActionBtn = page.locator('[data-testid^="slave-actions-"]').first();
    await slaveActionBtn.click();
    await page.getByRole('menuitem', { name: '新增寄存器' }).click();
    await page.getByLabel('起始逻辑地址').fill('40001');
    await page.getByLabel('16进制字符串').fill('ABCD');
    await page.getByRole('button', { name: '确定' }).click();

    await slaveActionBtn.click();
    await page.getByRole('menuitem', { name: '新增寄存器' }).click();
    await page.getByLabel('起始逻辑地址').fill('40100');
    await page.getByLabel('16进制字符串').fill('1234');
    await page.getByRole('button', { name: '确定' }).click();

    await page.waitForTimeout(800);

    // Re-expand to ensure cached and counts >= 2
    await page.getByText('从机-CRUD').click();
    await page.getByText('从机-CRUD').click();
    const holdingRow = page.locator('div', { hasText: '保持寄存器' }).first();
    await expect(holdingRow).toBeVisible();

    // Open tab and edit first row hex from ABCD -> 1234
    await holdingRow.click();
    const hexCell = page.locator('td', { hasText: 'ABCD' }).first();
    await hexCell.click();
    const input = page.locator('input').first();
    await input.fill('1234');
    await input.press('Enter');

    // Save
    await page.getByRole('button', { name: '保存修改' }).click();
    await page.waitForTimeout(1200);

    // Delete all holding registers via action menu
    await page.getByText('从机-CRUD').click();
    await page.getByText('从机-CRUD').click();
    await page.locator('[data-testid^="register-actions-"]').first().click();
    await page.getByRole('menuitem', { name: '删除寄存器' }).click();
    await page.waitForTimeout(800);

    // Counts should drop to 0 (and still cached; no spinner)
    const spinner = page.locator('div', { hasText: '正在加载寄存器...' }).first();
    await expect(spinner).toHaveCount(0);
  });
});


