import { defineConfig } from '@playwright/test';

// Suite completa contra el sistema levantado con `docker compose up` (sin webServer).
// La interfaz se sirve en E2E_BASE_URL (por defecto http://localhost:8080, servicio `web`).
// salud.spec.ts usa su propia E2E_API_URL (por defecto http://localhost:5000).
export default defineConfig({
  testDir: 'e2e-stack',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8080',
  },
});
