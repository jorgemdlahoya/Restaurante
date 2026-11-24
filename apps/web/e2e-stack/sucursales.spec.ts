import { test, expect } from '@playwright/test';

const DIAS = ['lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado', 'domingo'];

test('003/RF-01: el administrador general da de alta una sucursal y su estado público responde', async ({ page, request }) => {
  const adminUsuario = process.env.ADMIN_USERNAME;
  const adminClave = process.env.ADMIN_PASSWORD;
  if (!adminUsuario || !adminClave) {
    throw new Error('Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)');
  }

  const marca = Date.now();
  const slug = `e2e-suc-${marca}`;
  const nombre = `Restaurante Sucursales ${marca}`;
  const usuario = `admin.${marca}`;
  const clave = `Clave-${marca}`;
  // El WhatsApp es único en toda la plataforma: +52 y 10 dígitos derivados de la marca de tiempo.
  const whatsApp = `+52${String(marca).slice(-10)}`;

  // La plataforma crea el restaurante con su administrador.
  await page.goto('/plataforma/login');
  await page.getByLabel('Usuario').fill(adminUsuario);
  await page.getByLabel('Contraseña').fill(adminClave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/plataforma\/restaurantes$/);
  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();

  const alta = page.getByRole('form', { name: 'Nuevo restaurante' });
  await alta.getByLabel('Nombre', { exact: true }).fill(nombre);
  await alta.getByLabel('Slug').fill(slug);
  await alta.getByLabel('Usuario administrador').fill(usuario);
  await alta.getByLabel('Contraseña del administrador').fill(clave);
  await alta.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByText(nombre)).toBeVisible();

  // El administrador entra en su restaurante y abre Sucursales.
  await page.goto(`/r/${slug}/login`);
  await page.getByLabel('Usuario').fill(usuario);
  await page.getByLabel('Contraseña').fill(clave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/inicio$`));

  await page.getByRole('link', { name: 'Sucursales' }).first().click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/sucursales$`));

  // Alta de la sucursal Centro.
  await page.getByRole('button', { name: 'Nueva sucursal' }).click();
  const formulario = page.getByRole('form', { name: 'Nueva sucursal' });
  await formulario.getByLabel('Nombre', { exact: true }).fill('Centro');
  await formulario.getByLabel('Dirección').fill('Calle 1 #10, Centro');
  await formulario.getByLabel('Teléfono').fill('+525512345678');
  await formulario.getByLabel('Slug').fill('centro');
  await formulario.getByLabel('WhatsApp', { exact: true }).fill(whatsApp);
  await formulario.getByLabel('Prefijo').fill('A');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  const fila = page.getByRole('row', { name: /Centro/ });
  await expect(fila).toBeVisible();

  // Detalle: horario de lunes a domingo 00:00–23:59.
  await fila.getByRole('link', { name: 'Ver horario' }).click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/sucursales/centro$`));
  for (const dia of DIAS) {
    await page.getByLabel(`Cerrado el ${dia}`).setChecked(false);
    await page.getByLabel(`Apertura del ${dia}`).fill('00:00');
    await page.getByLabel(`Cierre del ${dia}`).fill('23:59');
  }
  const guardado = page.waitForResponse((r) => r.request().method() === 'PUT' && r.url().includes('/horario'));
  await page.getByRole('button', { name: 'Guardar horario' }).click();
  expect((await guardado).ok()).toBe(true);
  await expect(page.getByText('Horario guardado.')).toBeVisible();

  // El estado público (sin token) responde y resuelve la zona horaria (tzdata en la imagen).
  // Se consulta por la interfaz (nginx proxifica /api), como lo hace un canal real.
  const respuesta = await request.get(`/api/r/${slug}/s/centro/estado`);
  expect(respuesta.status()).toBe(200);
  const cuerpo = await respuesta.json();
  expect(typeof cuerpo.abierta).toBe('boolean');
  // 00:00–23:59 todos los días: abierta salvo el minuto 23:59→00:00 local, que da el mensaje de cerrada.
  expect(cuerpo.abierta === true || String(cuerpo.mensaje).startsWith('Estamos cerrados, abrimos')).toBe(true);
});
