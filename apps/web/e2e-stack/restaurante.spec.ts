import { test, expect } from '@playwright/test';

test('002/RF-01: la plataforma da de alta un restaurante y su administrador entra en su dirección', async ({ page }) => {
  const adminUsuario = process.env.ADMIN_USERNAME;
  const adminClave = process.env.ADMIN_PASSWORD;
  if (!adminUsuario || !adminClave) {
    throw new Error('Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)');
  }

  const marca = Date.now();
  const slug = `e2e-${marca}`;
  const nombre = `Restaurante E2E ${marca}`;
  const usuario = `admin.${marca}`;
  const clave = `Clave-${marca}`;

  // Alta desde la plataforma.
  await page.goto('/plataforma/login');
  await page.getByLabel('Usuario').fill(adminUsuario);
  await page.getByLabel('Contraseña').fill(adminClave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();

  await expect(page).toHaveURL(/\/plataforma\/restaurantes$/);
  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();

  const formulario = page.getByRole('form', { name: 'Nuevo restaurante' });
  await formulario.getByLabel('Nombre', { exact: true }).fill(nombre);
  await formulario.getByLabel('Slug').fill(slug);
  await formulario.getByLabel('Usuario administrador').fill(usuario);
  await formulario.getByLabel('Contraseña del administrador').fill(clave);
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByText(nombre)).toBeVisible();

  // El administrador del restaurante entra en su dirección.
  await page.goto(`/r/${slug}/login`);
  await expect(page.getByRole('heading', { level: 1, name: nombre })).toBeVisible();
  await page.getByLabel('Usuario').fill(usuario);
  await page.getByLabel('Contraseña').fill(clave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();

  await expect(page).toHaveURL(new RegExp(`/r/${slug}/inicio$`));
  await expect(page.getByText(`Hola, ${usuario}`)).toBeVisible();
});
