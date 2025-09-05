import { test, expect } from '@playwright/test';

test.describe('Registers', () => {
  test('create, edit and delete a holding register group', async ({ page }) => {
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

    // Prepare a connection and a slave
    const connName = 'E2E-Conn-Reg';
    await page.getByRole('button', { name: '新建连接' }).click();
    await page.getByLabel('连接名称').fill(connName);
    await page.getByRole('button', { name: '创建' }).click();
    await expect(page.getByText(connName)).toBeVisible({ timeout: 10000 });

    // 新增从机
    const connRow = page.locator('[data-testid^="connection-actions-"]').first();
    await connRow.click();
    await page.getByRole('menuitem', { name: '新增从机' }).click();
    await page.getByLabel('从机名称').fill('从机-R');
    await page.getByLabel('从机地址').fill('1');
    await page.getByRole('button', { name: '创建' }).click();

    // Expand connection to see slave
    const connectionText = page.locator('div', { hasText: connName }).first();
    await connectionText.click();
    await expect(page.getByText('从机-R')).toBeVisible({ timeout: 10000 });

    // 在从机菜单选择"新增寄存器"
    const slaveActionBtn = page.locator('[data-testid^="slave-actions-"]').first();
    await slaveActionBtn.click();
    await page.getByRole('menuitem', { name: '新增寄存器' }).click();

    // 新建保持寄存器（40002, ABCD）- 使用不同的地址避免约束冲突
    await page.getByLabel('起始逻辑地址').fill('40002');
    await page.getByLabel('16进制字符串').fill('ABCD');
    await page.getByRole('button', { name: '确定' }).click();

    // Wait for register creation to complete
    await page.waitForTimeout(2000);

    // 确保从机节点是展开状态 - 重新点击从机行来展开它
    const slaveRow = page.locator('[data-testid^="slave-actions-"]').first().locator('..').first();
    await slaveRow.click();
    
    // 等待更长时间让异步数据加载完成
    await page.waitForTimeout(3000);
    
    // 等待寄存器类型显示出来（等待异步加载完成）
    await expect(page.locator('div', { hasText: '保持寄存器' })).toBeVisible({ timeout: 15000 });

    // 然后点击"保持寄存器"打开标签（如果有数据就可点击）
    await page.locator('div', { hasText: '保持寄存器' }).first().click();

    // 断言表格出现 40002 地址行（以 40002 左侧地址展示）
    await expect(page.getByText('40002')).toBeVisible({ timeout: 10000 });

    // 编辑 Hex 列，将 ABCD 改为 1234，回车提交
    // 找到 Hex 单元格（蓝色样式），点击进入编辑，然后输入并回车
    const hexCell = page.locator('td', { hasText: 'ABCD' }).first();
    await hexCell.click();
    const input = page.locator('input').first();
    await input.fill('1234');
    await input.press('Enter');

    // 顶部工具栏出现"保存修改"按钮
    await expect(page.getByRole('button', { name: '保存修改' })).toBeVisible({ timeout: 10000 });

    // 点击保存修改
    await page.getByRole('button', { name: '保存修改' }).click();

    // Wait for save to complete
    await page.waitForTimeout(2000);

    // 删除连接进行清理
    await connRow.click();
    await page.getByRole('menuitem', { name: '删除连接' }).click();
    await expect(page.locator('[data-testid^="connection-actions-"]')).toHaveCount(0, { timeout: 15000 });
    await expect(page.getByText(connName)).toHaveCount(0, { timeout: 5000 });
  });
});


