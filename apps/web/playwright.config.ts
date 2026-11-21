import { defineConfig } from '@playwright/test';

// Suite simulada (API con page.route): la que ejecuta comando_test.
// La suite contra el stack real vive en e2e-stack/ con playwright.stack.config.ts.
export default defineConfig({
  testDir: 'e2e',
});
