// Debug script to test delete operation
const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  
  // Enable console logging
  page.on('console', msg => {
    console.log(`[BROWSER] ${msg.type()}: ${msg.text()}`);
  });
  
  page.on('pageerror', err => {
    console.error(`[BROWSER ERROR] ${err.message}`);
  });

  await page.goto('http://localhost:3000');
  
  console.log('=== INITIAL LOAD ===');
  await page.waitForTimeout(3000);
  
  // Check initial connections
  let connections = await page.$$eval('[data-testid^="connection-actions-"]', elements => elements.length);
  console.log(`Initial connections: ${connections}`);
  
  if (connections > 0) {
    console.log('=== TESTING DELETE ===');
    
    // Setup dialog handler
    page.on('dialog', async (dialog) => {
      console.log(`Dialog appeared: ${dialog.message()}`);
      await dialog.accept();
    });

    // Find and click the action button
    const actionBtn = page.locator('[data-testid^="connection-actions-"]').first();
    console.log('Clicking action button...');
    await actionBtn.click();
    
    // Click delete from menu
    console.log('Clicking delete menu item...');
    await page.getByRole('menuitem', { name: '删除连接' }).click();
    
    console.log('Waiting for deletion to complete...');
    await page.waitForTimeout(2000);
    
    // Check connections after delete
    connections = await page.$$eval('[data-testid^="connection-actions-"]', elements => elements.length);
    console.log(`Connections after delete: ${connections}`);
    
    // Check if connection text still exists
    const connectionTexts = await page.getByText('E2E连接').count();
    console.log(`Connection text count: ${connectionTexts}`);
    
    await page.waitForTimeout(3000);
  }
  
  await page.screenshot({ path: 'debug-after-delete.png' });
  console.log('Keeping browser open for inspection...');
  await page.waitForTimeout(10000);
  
  await browser.close();
})();