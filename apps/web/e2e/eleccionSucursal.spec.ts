import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import { simularMiSesion } from './apiSimuladaUsuarios';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };
const CENTRO = { slug: 'centro', nombre: 'Centro' };
const NORTE = { slug: 'norte', nombre: 'Norte' };

async function iniciarSesion(page: Page, rol: string, sucursales: { slug: string; nombre: string }[]) {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', { nombreUsuario: 'ana', contrasena: 'Clave-123', rol });
  const sesion = await simularMiSesion(page, 'la-esquina', { nombreUsuario: 'ana', rol, sucursales });
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  return sesion;
}

test('004/RF-20: un cajero con dos sucursales elige una al iniciar sesión', async ({ page }) => {
  await iniciarSesion(page, 'cajero', [CENTRO, NORTE]);

  await expect(page).toHaveURL(/\/r\/la-esquina\/elegir-sucursal$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Elige tu sucursal' })).toBeVisible();
  await page.getByRole('button', { name: 'Norte' }).click();

  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
  await expect(page.getByRole('banner').getByLabel('Sucursal')).toHaveValue('norte');
});

test('004/RF-20: con una sola sucursal entra directo al inicio', async ({ page }) => {
  await iniciarSesion(page, 'cocinero', [CENTRO]);

  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
  await expect(page.getByRole('banner')).toContainText('Centro');
});

test('004/RF-21: el selector muestra las sucursales del cajero y con una sola sólo su nombre', async ({ page }) => {
  const sesion = await iniciarSesion(page, 'cajero', [CENTRO, NORTE]);
  await page.getByRole('button', { name: 'Centro' }).click();

  const selector = page.getByRole('banner').getByLabel('Sucursal');
  await expect(selector.locator('option')).toHaveText(['Centro', 'Norte']);
  await selector.selectOption({ label: 'Norte' });
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
  await expect(selector).toHaveValue('norte');

  // Norte deja de estar disponible: al recargar, se descarta y queda la única sucursal, sin selector.
  sesion.sucursales.splice(1, 1);
  await page.reload();
  await expect(page.getByRole('banner')).toContainText('Centro');
  await expect(page.getByRole('banner').getByRole('combobox')).toHaveCount(0);
});

test('004/RF-24: el login muestra el aviso sin sucursales activas', async ({ page }) => {
  await simularIdentidad(page, LA_ESQUINA);
  await page.route('**/api/r/la-esquina/auth/login', (route) =>
    route.fulfill({
      status: 403,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 403, title: 'Sin sucursales activas', detail: 'Detalle del servidor' }),
    }),
  );
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();

  await expect(page.getByRole('alert')).toHaveText('No tienes sucursales activas. Habla con tu administrador.');
  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
});

test('004/RF-27: con la sesión abierta y sin sucursales activas el inicio muestra el aviso', async ({ page }) => {
  await iniciarSesion(page, 'cajero', []);

  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
  await expect(page.getByText('No tienes sucursales activas. Habla con tu administrador.')).toBeVisible();
  await expect(page.getByRole('banner').getByRole('combobox')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Cerrar sesión' })).toBeVisible();
});
