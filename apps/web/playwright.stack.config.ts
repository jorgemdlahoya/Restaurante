import { defineConfig } from '@playwright/test';

// Suite completa contra el sistema levantado con `docker compose up` (sin webServer).
// La URL de la API se toma de E2E_API_URL (por defecto http://localhost:5000).
export default defineConfig({
  testDir: 'e2e-stack',
  use: {
    baseURL: process.env.E2E_API_URL ?? 'http://localhost:5000',
  },
});
