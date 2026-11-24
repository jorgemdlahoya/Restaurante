import { test, expect, type Browser, type BrowserContext, type Page } from '@playwright/test';

// Flujo real de la feature 004 contra el sistema levantado con `docker compose up`:
// el administrador general crea un cajero con dos sucursales (RF-01), el cajero entra y elige
// sucursal (RF-20) y, al desactivarlo, su sesión abierta vuelve al login con el aviso (RF-13).
// Dos contextos de navegador: uno para el administrador y otro para el cajero.

const AVISO = 'Tu sesión terminó. Vuelve a iniciar sesión.';

const marca = Date.now();
const slug = `e2e-usu-${marca}`;
const nombreRestaurante = `Restaurante Usuarios ${marca}`;
const admin = { usuario: `admin.${marca}`, clave: `Clave-${marca}` };
const cajero = { usuario: `cajero.${marca}`, clave: `Cajero-${marca}` };
// El WhatsApp es único en toda la plataforma: +52 y 10 dígitos derivados de la marca de tiempo.
// La sucursal Norte usa el siguiente número para no chocar con Centro.
const digitos = String(marca).slice(-10);
const whatsAppCentro = `+52${digitos}`;
const whatsAppNorte = `+52${String((Number(digitos) + 1) % 10_000_000_000).padStart(10, '0')}`;

let contextoAdmin: BrowserContext;
let contextoCajero: BrowserContext;
let paginaAdmin: Page;
let paginaCajero: Page;

async function iniciarSesionRestaurante(page: Page, usuario: string, clave: string) {
  await page.goto(`/r/${slug}/login`);
  await page.getByLabel('Usuario', { exact: true }).fill(usuario);
  await page.getByLabel('Contraseña', { exact: true }).fill(clave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
}

async function crearSucursal(page: Page, nombre: string, slugSucursal: string, whatsApp: string, prefijo: string) {
  await page.getByRole('button', { name: 'Nueva sucursal' }).click();
  const formulario = page.getByRole('form', { name: 'Nueva sucursal' });
  await formulario.getByLabel('Nombre', { exact: true }).fill(nombre);
  await formulario.getByLabel('Dirección').fill(`Calle 1 #10, ${nombre}`);
  await formulario.getByLabel('Teléfono').fill('+525512345678');
  await formulario.getByLabel('Slug').fill(slugSucursal);
  await formulario.getByLabel('WhatsApp', { exact: true }).fill(whatsApp);
  await formulario.getByLabel('Prefijo').fill(prefijo);
  await formulario.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByRole('row', { name: new RegExp(nombre) })).toBeVisible();
}

async function prepararRestaurante(browser: Browser) {
  const adminUsuario = process.env.ADMIN_USERNAME;
  const adminClave = process.env.ADMIN_PASSWORD;
  if (!adminUsuario || !adminClave) {
    throw new Error('Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)');
  }

  contextoAdmin = await browser.newContext();
  contextoCajero = await browser.newContext();
  paginaAdmin = await contextoAdmin.newPage();
  paginaCajero = await contextoCajero.newPage();
  const page = paginaAdmin;

  // La plataforma crea el restaurante con su administrador general.
  await page.goto('/plataforma/login');
  await page.getByLabel('Usuario', { exact: true }).fill(adminUsuario);
  await page.getByLabel('Contraseña', { exact: true }).fill(adminClave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/plataforma\/restaurantes$/);
  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();
  const alta = page.getByRole('form', { name: 'Nuevo restaurante' });
  await alta.getByLabel('Nombre', { exact: true }).fill(nombreRestaurante);
  await alta.getByLabel('Slug').fill(slug);
  await alta.getByLabel('Usuario administrador').fill(admin.usuario);
  await alta.getByLabel('Contraseña del administrador').fill(admin.clave);
  await alta.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByText(nombreRestaurante)).toBeVisible();

  // El administrador general da de alta las sucursales Centro y Norte.
  await iniciarSesionRestaurante(page, admin.usuario, admin.clave);
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/inicio$`));
  await page.getByRole('link', { name: 'Sucursales' }).first().click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/sucursales$`));
  await crearSucursal(page, 'Centro', 'centro', whatsAppCentro, 'A');
  await crearSucursal(page, 'Norte', 'norte', whatsAppNorte, 'B');
}

test.describe.serial('004: usuarios, roles y asignación a sucursales', () => {
  test.beforeAll(async ({ browser }) => {
    test.setTimeout(60_000);
    await prepararRestaurante(browser);
  });

  test.afterAll(async () => {
    await contextoCajero?.close();
    await contextoAdmin?.close();
  });

  test('004/RF-01: el administrador general crea un cajero con dos sucursales', async () => {
    const page = paginaAdmin;
    await page.goto(`/r/${slug}/usuarios`);
    await expect(page.getByRole('heading', { name: 'Usuarios' })).toBeVisible();

    await page.getByRole('button', { name: 'Nuevo usuario' }).click();
    const formulario = page.getByRole('form', { name: 'Nuevo usuario' });
    await formulario.getByLabel('Usuario', { exact: true }).fill(cajero.usuario);
    await formulario.getByLabel('Contraseña', { exact: true }).fill(cajero.clave);
    await formulario.getByLabel('Rol').selectOption({ label: 'Cajero' });
    const sucursales = formulario.getByRole('group', { name: 'Sucursales' });
    await sucursales.getByLabel('Centro').check();
    await sucursales.getByLabel('Norte').check();
    await formulario.getByRole('button', { name: 'Guardar' }).click();

    await expect(formulario).toBeHidden();
    const fila = page.getByRole('row', { name: new RegExp(cajero.usuario) });
    await expect(fila).toContainText('Cajero');
    await expect(fila).toContainText('Centro, Norte');
    await expect(fila).toContainText('Activo');
  });

  test('004/RF-20: el cajero inicia sesión y elige sucursal', async () => {
    const page = paginaCajero;
    await iniciarSesionRestaurante(page, cajero.usuario, cajero.clave);

    await expect(page).toHaveURL(new RegExp(`/r/${slug}/elegir-sucursal$`));
    await expect(page.getByRole('heading', { name: 'Elige tu sucursal' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Centro' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Norte' })).toBeVisible();

    await page.getByRole('button', { name: 'Norte' }).click();

    await expect(page).toHaveURL(new RegExp(`/r/${slug}/inicio$`));
    const selector = page.getByLabel('Sucursal', { exact: true });
    await expect(selector).toBeVisible();
    await expect(selector).toHaveValue('norte');
  });

  test('004/RF-13: al desactivar al cajero, su sesión vuelve al login', async () => {
    // El administrador desactiva al cajero desde la pantalla de usuarios.
    const pantallaAdmin = paginaAdmin;
    const fila = pantallaAdmin.getByRole('row', { name: new RegExp(cajero.usuario) });
    await fila.getByRole('button', { name: 'Desactivar' }).click();
    const dialogo = pantallaAdmin.getByRole('dialog', { name: `¿Desactivar a ${cajero.usuario}?` });
    await expect(dialogo).toBeVisible();
    const cambio = pantallaAdmin.waitForResponse(
      (r) => r.request().method() !== 'GET' && r.url().includes(`/api/r/${slug}/usuarios/`),
    );
    await dialogo.getByRole('button', { name: 'Desactivar' }).click();
    expect((await cambio).ok()).toBe(true);
    await expect(fila).toContainText('Inactivo');

    // En el contexto del cajero, la siguiente petición autenticada (GET mi-sesion al recargar el
    // inicio) recibe 401 "Sesión no válida" y la aplicación vuelve al login con el aviso.
    const page = paginaCajero;
    const rechazo = page.waitForResponse((r) => r.url().includes(`/api/r/${slug}/mi-sesion`));
    await page.reload();
    expect((await rechazo).status()).toBe(401);

    await expect(page).toHaveURL(new RegExp(`/r/${slug}/login$`));
    await expect(page.getByText(AVISO)).toBeVisible();

    // El cajero desactivado tampoco puede volver a entrar.
    await page.getByLabel('Usuario', { exact: true }).fill(cajero.usuario);
    await page.getByLabel('Contraseña', { exact: true }).fill(cajero.clave);
    await page.getByRole('button', { name: 'Iniciar sesión' }).click();
    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/r/${slug}/login$`));
  });
});
