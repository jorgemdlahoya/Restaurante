import { configDefaults, defineConfig } from 'vitest/config';

// Las suites de Playwright (e2e/ y e2e-stack/) no las ejecuta Vitest.
export default defineConfig({
  test: {
    exclude: [...configDefaults.exclude, 'e2e/**', 'e2e-stack/**'],
  },
});
