import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import { simularSucursales, sucursal } from './apiSimuladaSucursales';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };

const CENTRO = sucursal({ id: 's1', nombre: 'Centro', slug: 'centro', direccion: 'Av. Juárez 1', prefijo: 'A' });
const NORTE = sucursal({
  id: 's2',
  nombre: 'Norte',
  slug: 'norte',
  direccion: 'Calle 5 de Mayo 20',
  prefijo: 'N',
  whatsApp: '+525512345670',
  activa: false,
});

async function entrar(page: Page, rol = 'administrador_general') {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', { nombreUsuario: 'ana', contrasena: 'Clave-123', rol });
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
}

function fila(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

test('003/RF-17: el administrador general ve la lista de sucursales con su estado', async ({ page }) => {
  await simularSucursales(page, 'la-esquina', [CENTRO, NORTE]);
  await entrar(page);

  await page.getByRole('link', { name: 'Sucursales' }).click();

  await expect(page).toHaveURL(/\/r\/la-esquina\/sucursales$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Sucursales' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Dirección' })).toBeVisible();
  await expect(fila(page, 'Centro')).toContainText('Av. Juárez 1');
  await expect(fila(page, 'Centro')).toContainText('Activa');
  await expect(fila(page, 'Norte')).toContainText('Inactiva');
  await expect(page.getByRole('button', { name: 'Nueva sucursal' })).toBeVisible();
});

test('003/RF-17: crear una sucursal la añade a la lista', async ({ page }) => {
  const api = await simularSucursales(page, 'la-esquina', [CENTRO]);
  await entrar(page);
  await page.goto('/r/la-esquina/sucursales');

  await page.getByRole('button', { name: 'Nueva sucursal' }).click();
  const formulario = page.getByRole('form', { name: 'Nueva sucursal' });
  await formulario.getByLabel('Nombre').fill('Sur');
  await formulario.getByLabel('Dirección').fill('Av. Sur 9');
  await formulario.getByLabel('Teléfono').fill('+52 55 1111 2222');
  await formulario.getByLabel('Slug').fill('sur');
  await formulario.getByLabel('WhatsApp').fill('+52 55 1111 3333');
  await formulario.getByLabel('Prefijo').fill('b');
  await expect(formulario.getByLabel('Prefijo')).toHaveValue('B');
  await expect(formulario.getByLabel('Zona horaria')).toHaveValue('America/Mexico_City');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Nueva sucursal' })).toHaveCount(0);
  await expect(fila(page, 'Sur')).toContainText('Activa');
  expect(api.altas).toEqual([
    expect.objectContaining({
      nombre: 'Sur',
      slug: 'sur',
      prefijo: 'B',
      zonaHoraria: 'America/Mexico_City',
      telefono: '+52 55 1111 2222',
    }),
  ]);
});

test('003/RF-17: los errores 400 se muestran junto a su campo', async ({ page }) => {
  const mensaje = 'El prefijo debe tener de 1 a 3 letras mayúsculas';
  await simularSucursales(page, 'la-esquina', [CENTRO], { erroresAlta: { prefijo: [mensaje] } });
  await entrar(page);
  await page.goto('/r/la-esquina/sucursales');

  await page.getByRole('button', { name: 'Nueva sucursal' }).click();
  const formulario = page.getByRole('form', { name: 'Nueva sucursal' });
  await formulario.getByLabel('Nombre').fill('Sur');
  await formulario.getByLabel('Dirección').fill('Av. Sur 9');
  await formulario.getByLabel('Teléfono').fill('+525511112222');
  await formulario.getByLabel('Slug').fill('sur');
  await formulario.getByLabel('WhatsApp').fill('+525511113333');
  await formulario.getByLabel('Prefijo').fill('ABCD');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(formulario.getByLabel('Prefijo')).toHaveAccessibleDescription(mensaje);
  await expect(formulario.getByLabel('Prefijo')).toHaveAttribute('aria-invalid', 'true');
});

test('003/RF-17: editar bloquea el slug y guarda los cambios', async ({ page }) => {
  const api = await simularSucursales(page, 'la-esquina', [CENTRO]);
  await entrar(page);
  await page.goto('/r/la-esquina/sucursales');

  await fila(page, 'Centro').getByRole('button', { name: 'Editar' }).click();
  const formulario = page.getByRole('form', { name: 'Editar Centro' });
  await expect(formulario.getByLabel('Slug')).toBeDisabled();
  await formulario.getByLabel('Dirección').fill('Av. Juárez 100');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Editar Centro' })).toHaveCount(0);
  await expect(fila(page, 'Centro')).toContainText('Av. Juárez 100');
  expect(api.ediciones).toEqual([{ id: 's1', cuerpo: expect.objectContaining({ direccion: 'Av. Juárez 100' }) }]);
});

test('003/RF-17: desactivar pide confirmación y cambia el estado', async ({ page }) => {
  const api = await simularSucursales(page, 'la-esquina', [CENTRO]);
  await entrar(page);
  await page.goto('/r/la-esquina/sucursales');

  await fila(page, 'Centro').getByRole('button', { name: 'Desactivar' }).click();
  const dialogo = page.getByRole('dialog');
  await expect(dialogo).toContainText('¿Desactivar Centro?');
  await expect(dialogo).toContainText('Dejará de aparecer en los canales hasta que la reactives.');
  await dialogo.getByRole('button', { name: 'Desactivar' }).click();

  await expect(fila(page, 'Centro')).toContainText('Inactiva');
  expect(api.cambiosDeEstado).toEqual([{ id: 's1', cuerpo: { activa: false } }]);
});

test('003/RF-20: el inicio muestra Sucursales sólo al administrador general', async ({ page }) => {
  await simularSucursales(page, 'la-esquina', [CENTRO]);
  await entrar(page, 'cajero');

  await expect(page.getByRole('link', { name: 'Sucursales' })).toHaveCount(0);

  await page.goto('/r/la-esquina/sucursales');
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
});
