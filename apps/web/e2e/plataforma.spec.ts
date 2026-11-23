import { expect, test, type Page } from '@playwright/test';
import { simularLoginPlataforma, simularRestaurantes, type RestauranteSimulado } from './apiSimulada';

const RESTAURANTES: RestauranteSimulado[] = [
  { id: '11111111-1111-1111-1111-111111111111', nombre: 'La Esquina', slug: 'la-esquina', activo: true, paleta: {} },
  { id: '22222222-2222-2222-2222-222222222222', nombre: 'El Puerto', slug: 'el-puerto', activo: false, paleta: {} },
];

async function entrarComoPlataforma(page: Page) {
  await page.goto('/plataforma/login');
  await page.getByLabel('Usuario').fill('root');
  await page.getByLabel('Contraseña').fill('Clave-root-1');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/plataforma\/restaurantes$/);
}

function fila(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

test.beforeEach(async ({ page }) => {
  await simularLoginPlataforma(page, { nombreUsuario: 'root', contrasena: 'Clave-root-1' });
});

test('002/RF-18: el administrador entra y ve la lista con estado', async ({ page }) => {
  await simularRestaurantes(page, RESTAURANTES);

  await entrarComoPlataforma(page);

  await expect(page.getByRole('columnheader', { name: 'Nombre' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Estado' })).toBeVisible();
  await expect(fila(page, 'La Esquina')).toContainText('Activo');
  await expect(fila(page, 'La Esquina')).toContainText('la-esquina');
  await expect(fila(page, 'El Puerto')).toContainText('Inactivo');
  await expect(page.getByRole('button', { name: 'Nuevo restaurante' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Cerrar sesión' })).toBeVisible();
});

test('002/RF-18: desactivar con confirmación cambia el estado a Inactivo', async ({ page }) => {
  const api = await simularRestaurantes(page, RESTAURANTES);
  await entrarComoPlataforma(page);

  await fila(page, 'La Esquina').getByRole('button', { name: 'Desactivar' }).click();

  const dialogo = page.getByRole('dialog');
  await expect(dialogo).toContainText('¿Desactivar La Esquina?');
  await expect(dialogo).toContainText('Su personal no podrá iniciar sesión hasta que lo reactives.');
  await dialogo.getByRole('button', { name: 'Desactivar' }).click();

  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(fila(page, 'La Esquina')).toContainText('Inactivo');
  expect(api.cambiosDeEstado).toEqual([{ id: RESTAURANTES[0].id, cuerpo: { activo: false } }]);
});

test('002/RF-18: cancelar la confirmación no cambia nada', async ({ page }) => {
  const api = await simularRestaurantes(page, RESTAURANTES);
  await entrarComoPlataforma(page);

  await fila(page, 'La Esquina').getByRole('button', { name: 'Desactivar' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Cancelar' }).click();

  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(fila(page, 'La Esquina')).toContainText('Activo');
  expect(api.cambiosDeEstado).toEqual([]);
});

test('002/RF-18: activar un restaurante inactivo no pide confirmación', async ({ page }) => {
  const api = await simularRestaurantes(page, RESTAURANTES);
  await entrarComoPlataforma(page);

  await fila(page, 'El Puerto').getByRole('button', { name: 'Activar' }).click();

  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(fila(page, 'El Puerto')).toContainText('Activo');
  expect(api.cambiosDeEstado).toEqual([{ id: RESTAURANTES[1].id, cuerpo: { activo: true } }]);
});

test('002/RF-21: /plataforma/restaurantes sin sesión lleva a /plataforma/login', async ({ page }) => {
  await page.goto('/plataforma/restaurantes');

  await expect(page).toHaveURL(/\/plataforma\/login$/);
  await expect(page.getByRole('button', { name: 'Iniciar sesión' })).toBeVisible();
});

test('002/RF-18: Cerrar sesión vuelve al login de plataforma', async ({ page }) => {
  await simularRestaurantes(page, RESTAURANTES);
  await entrarComoPlataforma(page);

  await page.getByRole('button', { name: 'Cerrar sesión' }).click();

  await expect(page).toHaveURL(/\/plataforma\/login$/);
});
