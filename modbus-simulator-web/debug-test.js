// Debug script to test data loading
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
  
  // Wait a bit for initial load
  await page.waitForTimeout(3000);
  
  // Check if data loaded
  console.log('Checking for connections...');
  const connections = await page.$$eval('[data-testid^="connection-actions-"]', elements => elements.length);
  console.log(`Found ${connections} connections`);
  
  // Take screenshot
  await page.screenshot({ path: 'debug-screenshot.png' });
  
  console.log('Debug complete. Keeping browser open for 10 seconds...');
  await page.waitForTimeout(10000);
  
  await browser.close();
})();