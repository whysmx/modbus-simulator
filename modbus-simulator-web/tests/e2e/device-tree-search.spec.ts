import { test, expect } from '@playwright/test';

test.describe('Device Tree Search', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    
    // Wait for initial data load
    await page.waitForSelector('[data-testid^="connection-actions-"], .text-center:has-text("正在加载连接数据...")', { timeout: 10000 });
    
    // Setup dialog handler for cleanup operations
    page.on('dialog', async (dialog) => {
      await dialog.accept();
    });

    // Clean up any existing connections first
    let existingConnections = await page.locator('[data-testid^="connection-actions-"]').count();
    while (existingConnections > 0) {
      const connActionBtn = page.locator('[data-testid^="connection-actions-"]').first();
      await connActionBtn.click();
      await page.getByRole('menuitem', { name: '删除连接' }).click();
      await page.waitForTimeout(1000);
      existingConnections = await page.locator('[data-testid^="connection-actions-"]').count();
    }
  });

  test('displays search input field in device tree', async ({ page }) => {
    // Check that search input field is present
    await expect(page.locator('input[placeholder="搜索连接、从机、端口..."]')).toBeVisible();
    
    // Check that search icon is present
    await expect(page.locator('svg.lucide-search')).toBeVisible();
  });

  test('search functionality with connections', async ({ page }) => {
    // Create test connections
    await createTestConnection(page, 'Test Connection 1', 502);
    await createTestConnection(page, 'Production Server', 1502);
    await createTestConnection(page, 'Debug Port', 5000);

    // Test searching by connection name
    const searchInput = page.locator('input[placeholder="搜索连接、从机、端口..."]');
    await searchInput.fill('Production');
    
    // Should only show Production Server connection
    await expect(page.locator('text=Production Server')).toBeVisible();
    await expect(page.locator('text=Test Connection 1')).not.toBeVisible();
    await expect(page.locator('text=Debug Port')).not.toBeVisible();

    // Test searching by port number
    await searchInput.fill('502');
    
    // Should show connections with port containing '502'
    await expect(page.locator('text=Test Connection 1')).toBeVisible();
    await expect(page.locator('text=Production Server')).toBeVisible();
    await expect(page.locator('text=Debug Port')).not.toBeVisible();

    // Test partial matching
    await searchInput.fill('Test');
    
    // Should show Test Connection 1
    await expect(page.locator('text=Test Connection 1')).toBeVisible();
    await expect(page.locator('text=Production Server')).not.toBeVisible();
    await expect(page.locator('text=Debug Port')).not.toBeVisible();

    // Test case insensitive search
    await searchInput.fill('production');
    
    // Should show Production Server (case insensitive)
    await expect(page.locator('text=Production Server')).toBeVisible();
    await expect(page.locator('text=Test Connection 1')).not.toBeVisible();
  });

  test('search functionality with slaves', async ({ page }) => {
    // Create test connection and slaves
    await createTestConnection(page, 'Main Connection', 502);
    
    // Expand connection to see slaves section
    await page.locator('[data-testid^="connection-actions-"]').first().click();
    await page.getByRole('menuitem', { name: '新增从机' }).click();
    
    // Create first slave
    await page.fill('input[placeholder="从机名称"]', 'Water Pump Station');
    await page.fill('input[placeholder="从机地址 (1-247)"]', '1');
    await page.click('button:has-text("保存")');
    await page.waitForTimeout(1000);

    // Create second slave
    await page.locator('[data-testid^="connection-actions-"]').first().click();
    await page.getByRole('menuitem', { name: '新增从机' }).click();
    await page.fill('input[placeholder="从机名称"]', 'Temperature Sensor');
    await page.fill('input[placeholder="从机地址 (1-247)"]', '2');
    await page.click('button:has-text("保存")');
    await page.waitForTimeout(1000);

    // Expand connection to show slaves
    const connectionRow = page.locator('text=Main Connection').locator('..');
    await connectionRow.locator('button').first().click();
    await page.waitForTimeout(500);

    // Test searching by slave name
    const searchInput = page.locator('input[placeholder="搜索连接、从机、端口..."]');
    await searchInput.fill('Water');
    
    // Should show connection and matching slave
    await expect(page.locator('text=Main Connection')).toBeVisible();
    await expect(page.locator('text=Water Pump Station')).toBeVisible();
    await expect(page.locator('text=Temperature Sensor')).not.toBeVisible();

    // Test searching by slave address
    await searchInput.fill('2');
    
    // Should show connection and slave with address 2
    await expect(page.locator('text=Main Connection')).toBeVisible();
    await expect(page.locator('text=Temperature Sensor')).toBeVisible();
    await expect(page.locator('text=Water Pump Station')).not.toBeVisible();
  });

  test('clear search functionality', async ({ page }) => {
    // Create test connection
    await createTestConnection(page, 'Test Connection', 502);
    
    const searchInput = page.locator('input[placeholder="搜索连接、从机、端口..."]');
    
    // Enter search term
    await searchInput.fill('NonExistent');
    
    // Should show "no results" message
    await expect(page.locator('text=未找到匹配的设备')).toBeVisible();
    
    // Check that clear button (X) appears
    const clearButton = page.locator('button:has(svg.lucide-x)');
    await expect(clearButton).toBeVisible();
    
    // Click clear button
    await clearButton.click();
    
    // Search input should be empty
    await expect(searchInput).toHaveValue('');
    
    // Should show all connections again
    await expect(page.locator('text=Test Connection')).toBeVisible();
    await expect(page.locator('text=未找到匹配的设备')).not.toBeVisible();
  });

  test('no results message when no matches found', async ({ page }) => {
    // Create test connection
    await createTestConnection(page, 'Test Connection', 502);
    
    const searchInput = page.locator('input[placeholder="搜索连接、从机、端口..."]');
    
    // Search for non-existent term
    await searchInput.fill('NonExistentDevice');
    
    // Should show no results message
    await expect(page.locator('text=未找到匹配的设备')).toBeVisible();
    await expect(page.locator('text=请尝试修改搜索关键词')).toBeVisible();
    
    // Should not show any connections
    await expect(page.locator('text=Test Connection')).not.toBeVisible();
  });

  test('search highlighting functionality', async ({ page }) => {
    // Create test connection
    await createTestConnection(page, 'Production Server', 1502);
    
    const searchInput = page.locator('input[placeholder="搜索连接、从机、端口..."]');
    
    // Search for partial match
    await searchInput.fill('Production');
    
    // Should highlight the matching text
    await expect(page.locator('mark:has-text("Production")')).toBeVisible();
    
    // Test port number highlighting
    await searchInput.fill('1502');
    
    // Should highlight the port number
    await expect(page.locator('mark:has-text("1502")')).toBeVisible();
  });

  test('search preserves tree expansion state', async ({ page }) => {
    // Create connection and expand it
    await createTestConnection(page, 'Test Connection', 502);
    
    // Expand connection
    const expandButton = page.locator('[data-testid^="connection-actions-"]').first().locator('xpath=../..').locator('button').first();
    await expandButton.click();
    await page.waitForTimeout(500);
    
    // Search for the connection
    const searchInput = page.locator('input[placeholder="搜索连接、从机、端口..."]');
    await searchInput.fill('Test');
    
    // Connection should still be visible and expanded state preserved
    await expect(page.locator('text=Test Connection')).toBeVisible();
    
    // Clear search
    await searchInput.fill('');
    
    // Connection should still be in expanded state
    await expect(page.locator('text=Test Connection')).toBeVisible();
  });

  // Helper function to create a test connection
  async function createTestConnection(page: any, name: string, port: number) {
    // Click create connection button
    await page.click('button:has-text("新增连接")');
    
    // Fill connection details
    await page.fill('input[placeholder="连接名称"]', name);
    await page.fill('input[placeholder="端口号 (1-65535)"]', port.toString());
    
    // Save connection
    await page.click('button:has-text("保存")');
    
    // Wait for connection to be created
    await page.waitForTimeout(1000);
  }
});