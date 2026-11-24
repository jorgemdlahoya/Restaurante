import { defineConfig } from '@playwright/test';

// Suite simulada (API con page.route): la que ejecuta comando_test.
// La suite contra el stack real vive en e2e-stack/ con playwright.stack.config.ts.
export default defineConfig({
  testDir: 'e2e',
  use: {
    baseURL: 'http://localhost:5173',
  },
  webServer: {
    command: 'npx vite --port 5173 --strictPort',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
  },
  projects: [
    { name: 'escritorio', use: { viewport: { width: 1440, height: 900 } } },
    { name: 'movil', use: { viewport: { width: 390, height: 844 } } },
  ],
});
