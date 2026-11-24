import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import { simularDetalleSucursal, simularSucursales, sucursal } from './apiSimuladaSucursales';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };
const CENTRO = sucursal({ id: 's1', nombre: 'Centro', slug: 'centro' });
const NORTE = sucursal({ id: 's2', nombre: 'Norte', slug: 'norte', prefijo: 'N', whatsApp: '+525512345670' });
const SUR = sucursal({ id: 's3', nombre: 'Sur', slug: 'sur', prefijo: 'S', whatsApp: '+525512345671', activa: false });

async function entrar(page: Page, rol: string) {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', { nombreUsuario: 'ana', contrasena: 'Clave-123', rol });
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
}

test('003/RF-19: elegir una sucursal la muestra en el encabezado y lleva a su detalle', async ({ page }) => {
  await simularSucursales(page, 'la-esquina', [CENTRO, NORTE, SUR]);
  await simularDetalleSucursal(page, 'la-esquina', 'norte');
  await entrar(page, 'administrador_general');

  const selector = page.getByRole('banner').getByLabel('Sucursal');
  await expect(selector).toBeVisible();
  // Sólo las sucursales activas (más la opción vacía).
  await expect(selector.locator('option')).toHaveText(['Elegir sucursal', 'Centro', 'Norte']);

  await selector.selectOption({ label: 'Norte' });

  await expect(page).toHaveURL(/\/r\/la-esquina\/sucursales\/norte$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Norte' })).toBeVisible();
  await expect(page.getByRole('banner').getByLabel('Sucursal')).toHaveValue('norte');

  // La sucursal activa se recuerda en la sesión al volver al inicio.
  await page.goto('/r/la-esquina/inicio');
  await expect(page.getByRole('banner').getByLabel('Sucursal')).toHaveValue('norte');
});

test('003/RF-19: el selector no aparece para un cajero', async ({ page }) => {
  await simularSucursales(page, 'la-esquina', [CENTRO, NORTE]);
  await entrar(page, 'cajero');

  await expect(page.getByRole('banner')).toBeVisible();
  await expect(page.getByRole('banner').getByLabel('Sucursal')).toHaveCount(0);
});
