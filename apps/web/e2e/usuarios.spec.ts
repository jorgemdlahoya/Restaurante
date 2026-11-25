import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import {
  CENTRO_ASIGNADA,
  NORTE_ASIGNADA,
  simularMiSesion,
  simularUsuarios,
  usuario,
} from './apiSimuladaUsuarios';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };
const CATALOGO = [CENTRO_ASIGNADA, NORTE_ASIGNADA];

const JEFA = usuario({ id: 'u1', nombreUsuario: 'jefa', rol: 'administrador_general' });
const MARIA = usuario({ id: 'u2', nombreUsuario: 'maria', rol: 'cajero', sucursales: [CENTRO_ASIGNADA] });
const PEDRO = usuario({
  id: 'u3',
  nombreUsuario: 'pedro',
  rol: 'cocinero',
  sucursales: [CENTRO_ASIGNADA, NORTE_ASIGNADA],
  gestionable: false,
});

async function entrar(page: Page, rol: string, sucursales = [CENTRO_ASIGNADA, NORTE_ASIGNADA]) {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', { nombreUsuario: 'ana', contrasena: 'Clave-123', rol });
  await simularMiSesion(page, 'la-esquina', {
    nombreUsuario: 'ana',
    rol,
    sucursales: sucursales.map(({ slug, nombre }) => ({ slug, nombre })),
  });
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
}

function fila(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

test('004/RF-18: el administrador general crea un cajero con dos sucursales y lo ve en la lista', async ({ page }) => {
  const api = await simularUsuarios(page, 'la-esquina', [JEFA, MARIA], CATALOGO);
  await entrar(page, 'administrador_general');

  await page.getByRole('link', { name: 'Usuarios' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/usuarios$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Usuarios' })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Sucursales' })).toBeVisible();
  await expect(fila(page, 'maria')).toContainText('Cajero');
  await expect(fila(page, 'maria')).toContainText('Centro');
  await expect(fila(page, 'maria')).toContainText('Activo');

  await page.getByRole('button', { name: 'Nuevo usuario' }).click();
  const formulario = page.getByRole('form', { name: 'Nuevo usuario' });
  await formulario.getByLabel('Usuario').fill('luis');
  await formulario.getByLabel('Contraseña').fill('Clave-456');
  await formulario.getByLabel('Rol').selectOption({ label: 'Cajero' });
  const sucursales = formulario.getByRole('group', { name: 'Sucursales' });
  await sucursales.getByLabel('Centro').check();
  await sucursales.getByLabel('Norte').check();
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Nuevo usuario' })).toHaveCount(0);
  await expect(fila(page, 'luis')).toContainText('Centro');
  await expect(fila(page, 'luis')).toContainText('Norte');
  expect(api.altas).toEqual([
    { nombreUsuario: 'luis', contrasena: 'Clave-456', rol: 'cajero', sucursales: ['centro', 'norte'] },
  ]);
});

test('004/RF-18: desactivar pide confirmación; asignar contraseña envía la nueva', async ({ page }) => {
  const api = await simularUsuarios(page, 'la-esquina', [JEFA, MARIA], CATALOGO);
  await entrar(page, 'administrador_general');
  await page.goto('/r/la-esquina/usuarios');

  await fila(page, 'maria').getByRole('button', { name: 'Desactivar' }).click();
  const dialogo = page.getByRole('dialog');
  await expect(dialogo).toContainText('¿Desactivar a maria?');
  await dialogo.getByRole('button', { name: 'Desactivar' }).click();
  await expect(fila(page, 'maria')).toContainText('Inactivo');
  expect(api.cambiosDeEstado).toEqual([{ id: 'u2', cuerpo: { activo: false } }]);

  await fila(page, 'maria').getByRole('button', { name: 'Asignar contraseña' }).click();
  const contrasena = page.getByRole('dialog', { name: 'Asignar contraseña a maria' });
  await contrasena.getByLabel('Contraseña nueva').fill('Nueva-789');
  await contrasena.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByText('Contraseña asignada a maria.')).toBeVisible();
  expect(api.contrasenas).toEqual([{ id: 'u2', cuerpo: { contrasena: 'Nueva-789' } }]);
});

test('004/RF-18: editar cambia rol y sucursales', async ({ page }) => {
  const api = await simularUsuarios(page, 'la-esquina', [JEFA, MARIA], CATALOGO);
  await entrar(page, 'administrador_general');
  await page.goto('/r/la-esquina/usuarios');

  await fila(page, 'maria').getByRole('button', { name: 'Editar' }).click();
  const formulario = page.getByRole('form', { name: 'Editar maria' });
  await expect(formulario.getByLabel('Usuario')).toBeDisabled();
  await expect(formulario.getByLabel('Contraseña')).toHaveCount(0);
  await formulario.getByLabel('Rol').selectOption({ label: 'Cocinero' });
  await formulario.getByRole('group', { name: 'Sucursales' }).getByLabel('Norte').check();
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Editar maria' })).toHaveCount(0);
  await expect(fila(page, 'maria')).toContainText('Cocinero');
  expect(api.ediciones).toEqual([{ id: 'u2', cuerpo: { rol: 'cocinero', sucursales: ['centro', 'norte'] } }]);
});

test('004/RF-18: el personal no gestionable se muestra sin acciones', async ({ page }) => {
  await simularUsuarios(page, 'la-esquina', [MARIA, PEDRO], CATALOGO);
  await entrar(page, 'administrador_sucursal', [CENTRO_ASIGNADA]);
  await page.goto('/r/la-esquina/usuarios');

  await expect(fila(page, 'pedro')).toContainText('Norte');
  await expect(fila(page, 'pedro').getByRole('button')).toHaveCount(0);
  await expect(fila(page, 'maria').getByRole('button', { name: 'Editar' })).toBeVisible();
});

test('004/RF-19: el administrador de sucursal sólo ofrece cajero y cocinero con su sucursal fija', async ({ page }) => {
  const api = await simularUsuarios(page, 'la-esquina', [MARIA], CATALOGO);
  await entrar(page, 'administrador_sucursal', [CENTRO_ASIGNADA]);
  await page.goto('/r/la-esquina/usuarios');

  await page.getByRole('button', { name: 'Nuevo usuario' }).click();
  const formulario = page.getByRole('form', { name: 'Nuevo usuario' });
  await expect(formulario.getByLabel('Rol').getByRole('option')).toHaveText(['Cajero', 'Cocinero']);
  const centro = formulario.getByRole('group', { name: 'Sucursales' }).getByLabel('Centro');
  await expect(centro).toBeChecked();
  await expect(centro).toBeDisabled();
  await expect(formulario.getByRole('group', { name: 'Sucursales' }).getByRole('checkbox')).toHaveCount(1);

  await formulario.getByLabel('Usuario').fill('rosa');
  await formulario.getByLabel('Contraseña').fill('Clave-456');
  await formulario.getByLabel('Rol').selectOption({ label: 'Cocinero' });
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(fila(page, 'rosa')).toContainText('Cocinero');
  expect(api.altas).toEqual([{ nombreUsuario: 'rosa', contrasena: 'Clave-456', rol: 'cocinero', sucursales: ['centro'] }]);
});

test('004/RF-22: el inicio muestra Usuarios a los administradores y un cajero que abre /usuarios vuelve al inicio', async ({
  page,
}) => {
  await simularUsuarios(page, 'la-esquina', [MARIA], CATALOGO);
  await entrar(page, 'administrador_sucursal', [CENTRO_ASIGNADA]);
  await expect(page.getByRole('link', { name: 'Usuarios' })).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar sesión' }).click();

  await entrar(page, 'cajero', [CENTRO_ASIGNADA]);
  await expect(page.getByRole('link', { name: 'Usuarios' })).toHaveCount(0);
  await page.goto('/r/la-esquina/usuarios');
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
});
