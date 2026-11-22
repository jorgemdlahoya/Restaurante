import type { Page, Route } from '@playwright/test';

// Ayudas de la suite simulada: responden a la API con page.route, sin backend real.

export type IdentidadSimulada = {
  nombre: string;
  slug: string;
  paleta: Partial<Record<'principal' | 'acento' | 'oscuro' | 'fondo' | 'texto', string | null>>;
  logoUrl: string | null;
};

// PNG transparente de 1×1 px.
const PNG_1PX = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
  'base64',
);

function problema(route: Route, status: number, title: string, detail: string) {
  return route.fulfill({
    status,
    contentType: 'application/problem+json',
    body: JSON.stringify({ status, title, detail }),
  });
}

export async function simularIdentidad(page: Page, identidad: IdentidadSimulada) {
  await page.route(`**/api/r/${identidad.slug}/identidad`, (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(identidad) }),
  );
  await page.route(`**/api/r/${identidad.slug}/logo**`, (route) =>
    route.fulfill({ status: 200, contentType: 'image/png', body: PNG_1PX }),
  );
}

export async function simularRestauranteNoDisponible(page: Page, slug: string) {
  await page.route(`**/api/r/${slug}/**`, (route) =>
    problema(route, 404, 'Restaurante no disponible', 'La dirección no corresponde a un restaurante activo.'),
  );
}

export async function simularLoginRestaurante(
  page: Page,
  slug: string,
  credenciales: { nombreUsuario: string; contrasena: string; rol?: string },
) {
  await page.route(`**/api/r/${slug}/auth/login`, async (route) => {
    const cuerpo = route.request().postDataJSON() as { nombreUsuario?: string; contrasena?: string };
    if (cuerpo.nombreUsuario === credenciales.nombreUsuario && cuerpo.contrasena === credenciales.contrasena) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'token-de-prueba',
          expiraEn: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
          nombreUsuario: credenciales.nombreUsuario,
          rol: credenciales.rol ?? 'cajero',
        }),
      });
    }
    return problema(route, 401, 'No autenticado', 'Usuario o contraseña incorrectos');
  });
}
