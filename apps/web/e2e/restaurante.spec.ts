import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante, simularRestauranteNoDisponible } from './apiSimulada';

const LA_ESQUINA = {
  nombre: 'La Esquina',
  slug: 'la-esquina',
  paleta: { principal: '#123456', acento: null, oscuro: null, fondo: null, texto: null },
  logoUrl: '/api/r/la-esquina/logo?v=abc',
};

function variableDeTema(page: Page, nombre: string) {
  return page.evaluate(
    (n) => getComputedStyle(document.documentElement).getPropertyValue(n).trim(),
    nombre,
  );
}

async function iniciarSesion(page: Page, usuario: string, contrasena: string) {
  await page.getByLabel('Usuario').fill(usuario);
  await page.getByLabel('Contraseña').fill(contrasena);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
}

test.beforeEach(async ({ page }) => {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', { nombreUsuario: 'ana', contrasena: 'Clave-123' });
});

test('002/RF-19: login con marca lleva a inicio con Hola y Cerrar sesión', async ({ page }) => {
  await page.goto('/r/la-esquina/login');

  await expect(page.getByRole('heading', { level: 1, name: 'La Esquina' })).toBeVisible();
  await expect(page.getByRole('img', { name: 'Logo de La Esquina' })).toBeVisible();
  await expect.poll(() => variableDeTema(page, '--color-principal')).toBe('#123456');

  await iniciarSesion(page, 'ana', 'Clave-123');

  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
  await expect(page.getByText('Hola, ana')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Cerrar sesión' })).toBeVisible();
  await expect.poll(() => variableDeTema(page, '--color-principal')).toBe('#123456');
});

test('002/RF-16: sin paleta se aplican los colores por defecto', async ({ page }) => {
  await page.unroute('**/api/r/la-esquina/identidad');
  await simularIdentidad(page, { ...LA_ESQUINA, paleta: {}, logoUrl: null });

  await page.goto('/r/la-esquina/login');

  await expect(page.getByRole('heading', { level: 1, name: 'La Esquina' })).toBeVisible();
  await expect.poll(() => variableDeTema(page, '--color-principal')).toBe('#F97316');
  await expect(page.getByRole('img', { name: 'Logo de La Esquina' })).toHaveCount(0);
});

test('002/RF-19: credenciales incorrectas muestran el mensaje del servidor', async ({ page }) => {
  await page.goto('/r/la-esquina/login');

  await iniciarSesion(page, 'ana', 'equivocada');

  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos');
  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
});

test('002/RF-20: un slug inexistente muestra Restaurante no disponible', async ({ page }) => {
  await simularRestauranteNoDisponible(page, 'no-existe');

  await page.goto('/r/no-existe/login');

  await expect(page.getByRole('heading', { level: 1, name: 'Restaurante no disponible' })).toBeVisible();
  await expect(page.getByLabel('Usuario')).toHaveCount(0);
});

test('002/RF-21: /r/la-esquina/inicio sin sesión lleva a /r/la-esquina/login', async ({ page }) => {
  await page.goto('/r/la-esquina/inicio');

  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
  await expect(page.getByRole('button', { name: 'Iniciar sesión' })).toBeVisible();
});

test('002/RF-19: Cerrar sesión vuelve al login', async ({ page }) => {
  await page.goto('/r/la-esquina/login');
  await iniciarSesion(page, 'ana', 'Clave-123');
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);

  await page.getByRole('button', { name: 'Cerrar sesión' }).click();

  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
  await page.goto('/r/la-esquina/inicio');
  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
});
