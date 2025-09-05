import { test, expect } from '@playwright/test';

test.describe('Tabs and Save Flow', () => {
  test('open register tabs and save changes', async ({ page }) => {
    await page.goto('/');

    // Wait for initial data load
    await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });

    // Setup dialog handlers before operations
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

    const connName = 'E2E-Conn-Tabs';
    await page.getByRole('button', { name: '新建连接' }).click();
    await page.getByLabel('连接名称').fill(connName);
    await page.getByRole('button', { name: '创建' }).click();
    await expect(page.getByText(connName)).toBeVisible({ timeout: 10000 });

    const connRow = page.locator('[data-testid^="connection-actions-"]').first();
    await connRow.click();
    await page.getByRole('menuitem', { name: '新增从机' }).click();
    await page.getByLabel('从机名称').fill('从机-T1');
    await page.getByLabel('从机地址').fill('2');
    await page.getByRole('button', { name: '创建' }).click();

    // Wait for slave to be created and visible (like in working test)  
    await expect(page.getByText('从机-T1')).toBeVisible({ timeout: 10000 });
    
    // Click on slave to show register types
    await page.getByText('从机-T1').click();
    await page.waitForTimeout(1000);

    // 新增两个寄存器类型数据：保持(40001)和输入(30001)
    // Wait a bit before creating registers to ensure slave is fully ready
    await page.waitForTimeout(2000);
    
    const slaveActionBtn = page.locator('[data-testid^="slave-actions-"]').first();
    await slaveActionBtn.click();
    await page.getByRole('menuitem', { name: '新增寄存器' }).click();
    
    // Fill register form more carefully
    const addressInput = page.getByLabel('起始逻辑地址');
    await addressInput.clear();
    await addressInput.fill('40001');
    
    const hexInput = page.getByLabel('16进制字符串');
    await hexInput.clear();
    await hexInput.fill('ABCD');
    
    await page.getByRole('button', { name: '确定' }).click();

    // Wait for register creation and any UI updates
    await page.waitForTimeout(4000);
    
    // Check if we need to re-expand the slave
    const expandedSlave = await page.locator('div', { hasText: '保持寄存器' }).count();
    if (expandedSlave === 0) {
      await page.getByText('从机-T1').click();
      await page.waitForTimeout(1000);
    }

    // Create second register
    await slaveActionBtn.click();
    await page.getByRole('menuitem', { name: '新增寄存器' }).click();
    
    const addressInput2 = page.getByLabel('起始逻辑地址');
    await addressInput2.clear();
    await addressInput2.fill('30001');
    
    const hexInput2 = page.getByLabel('16进制字符串');
    await hexInput2.clear();
    await hexInput2.fill('1234');
    
    await page.getByRole('button', { name: '确定' }).click();

    // Wait for register creation and any UI updates
    await page.waitForTimeout(4000);
    
    // Check if we need to re-expand the slave again
    const expandedSlave2 = await page.locator('div', { hasText: '输入寄存器' }).count();
    if (expandedSlave2 === 0) {
      await page.getByText('从机-T1').click();
      await page.waitForTimeout(1000);
    }

    // 打开保持寄存器标签
    await page.locator('div', { hasText: '保持寄存器' }).first().click();
    await expect(page.getByText('40001')).toBeVisible({ timeout: 10000 });

    // 打开输入寄存器标签
    await page.locator('div', { hasText: '输入寄存器' }).first().click();
    await expect(page.getByText('30001')).toBeVisible({ timeout: 10000 });

    // 切换标签（应能看到 Tab 1/2）
    await page.getByText('Tab 1').click();
    await expect(page.getByText('40001')).toBeVisible({ timeout: 10000 });
    await page.getByText('Tab 2').click();
    await expect(page.getByText('30001')).toBeVisible({ timeout: 10000 });

    // 修改输入寄存器 hex -> 保存修改
    const hexCell = page.locator('td', { hasText: '1234' }).first();
    await hexCell.click();
    const input = page.locator('input').first();
    await input.fill('5678');
    await input.press('Enter');
    await expect(page.getByRole('button', { name: '保存修改' })).toBeVisible({ timeout: 10000 });
    await page.getByRole('button', { name: '保存修改' }).click();

    // Wait for save to complete
    await page.waitForTimeout(2000);

    // 关闭一个标签
    await page.getByText('Tab 1').locator('..').getByRole('button').click();

    // 清理：删连接
    await connRow.click();
    await page.getByRole('menuitem', { name: '删除连接' }).click();
    await expect(page.locator('[data-testid^="connection-actions-"]')).toHaveCount(0, { timeout: 15000 });
    await expect(page.getByText(connName)).toHaveCount(0, { timeout: 5000 });
  });
});


