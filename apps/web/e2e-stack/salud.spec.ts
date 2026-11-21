import { test, expect } from '@playwright/test';

const apiUrl = process.env.E2E_API_URL ?? 'http://localhost:5000';

test('001/RF-23: el sistema levantado con docker compose responde /health 200', async ({ request }) => {
  const respuesta = await request.get(`${apiUrl}/health`);

  expect(respuesta.status()).toBe(200);
});
