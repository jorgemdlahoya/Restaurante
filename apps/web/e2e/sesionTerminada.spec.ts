import { expect, test, type Page, type Route } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import { CENTRO_ASIGNADA, simularMiSesion, usuario } from './apiSimuladaUsuarios';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };
const AVISO = 'Tu sesión terminó. Vuelve a iniciar sesión.';

function sesionNoValida(route: Route) {
  return route.fulfill({
    status: 401,
    contentType: 'application/problem+json',
    headers: { 'WWW-Authenticate': 'Bearer' },
    body: JSON.stringify({ status: 401, title: 'Sesión no válida', detail: 'Vuelve a iniciar sesión.' }),
  });
}

async function entrarComoGeneral(page: Page) {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', {
    nombreUsuario: 'ana',
    contrasena: 'Clave-123',
    rol: 'administrador_general',
  });
  await simularMiSesion(page, 'la-esquina', { nombreUsuario: 'ana', rol: 'administrador_general', sucursales: [] });
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
}

test('004/RF-23: una petición con 401 Sesión no válida lleva al login con el aviso', async ({ page }) => {
  await page.route('**/api/r/la-esquina/usuarios', sesionNoValida);
  await entrarComoGeneral(page);

  await page.getByRole('link', { name: 'Usuarios' }).click();

  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
  await expect(page.getByText(AVISO)).toBeVisible();
  await expect(page.getByRole('button', { name: 'Iniciar sesión' })).toBeVisible();

  // La sesión local quedó cerrada: una pantalla protegida vuelve al login.
  await page.goto('/r/la-esquina/usuarios');
  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
});

test('004/RF-23: tras asignarse contraseña, el administrador general vuelve al login con el aviso', async ({ page }) => {
  let sesionCortada = false;
  const ana = usuario({ id: 'u1', nombreUsuario: 'ana', rol: 'administrador_general', sucursales: [] });
  const maria = usuario({ id: 'u2', nombreUsuario: 'maria', rol: 'cajero', sucursales: [CENTRO_ASIGNADA] });
  await page.route('**/api/r/la-esquina/usuarios', (route) =>
    sesionCortada
      ? sesionNoValida(route)
      : route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([ana, maria]) }),
  );
  await page.route('**/api/r/la-esquina/usuarios/u1/contrasena', (route) => {
    sesionCortada = true;
    return route.fulfill({ status: 204 });
  });
  await entrarComoGeneral(page);
  await page.goto('/r/la-esquina/usuarios');

  await page
    .getByRole('row')
    .filter({ has: page.getByRole('cell', { name: 'ana', exact: true }) })
    .getByRole('button', { name: 'Asignar contraseña' })
    .click();
  const dialogo = page.getByRole('dialog', { name: 'Asignar contraseña a ana' });
  await dialogo.getByLabel('Contraseña nueva').fill('Nueva-789');
  await dialogo.getByRole('button', { name: 'Guardar' }).click();

  await expect(page).toHaveURL(/\/r\/la-esquina\/login$/);
  await expect(page.getByText(AVISO)).toBeVisible();
});
