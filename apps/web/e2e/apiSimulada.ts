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

export type RestauranteSimulado = {
  id: string;
  nombre: string;
  slug: string;
  activo: boolean;
  paleta: IdentidadSimulada['paleta'];
};

export async function simularLoginPlataforma(page: Page, credenciales: { nombreUsuario: string; contrasena: string }) {
  await page.route('**/api/plataforma/auth/login', async (route) => {
    const cuerpo = route.request().postDataJSON() as { nombreUsuario?: string; contrasena?: string };
    if (cuerpo.nombreUsuario === credenciales.nombreUsuario && cuerpo.contrasena === credenciales.contrasena) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          token: 'token-plataforma',
          expiraEn: new Date(Date.now() + 8 * 60 * 60 * 1000).toISOString(),
          nombreUsuario: credenciales.nombreUsuario,
          rol: 'administrador_plataforma',
        }),
      });
    }
    return problema(route, 401, 'No autenticado', 'Usuario o contraseña incorrectos');
  });
}

type OpcionesRestaurantes = {
  /** Si se indica, el alta (POST) responde 400 con estos errores por campo. */
  erroresAlta?: Record<string, string[]>;
};

/**
 * Simula la gestión de restaurantes con estado en memoria: GET lista, POST alta, PUT /{id} edición,
 * PUT …/estado cambio de estado y PUT …/logo subida del logo (multipart).
 * Devuelve los registros de peticiones para poder afirmar sobre su cuerpo.
 */
export async function simularRestaurantes(
  page: Page,
  iniciales: RestauranteSimulado[],
  opciones: OpcionesRestaurantes = {},
) {
  const restaurantes = iniciales.map((r) => ({ ...r }));
  const cambiosDeEstado: { id: string; cuerpo: unknown }[] = [];
  const altas: unknown[] = [];
  const ediciones: { id: string; cuerpo: unknown }[] = [];
  const logos: { id: string; tipoContenido: string; tamano: number }[] = [];

  await page.route('**/api/plataforma/restaurantes', (route) => {
    const metodo = route.request().method();
    if (metodo === 'GET') {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(restaurantes) });
    }
    if (metodo !== 'POST') return route.fallback();

    const cuerpo = route.request().postDataJSON() as { nombre: string; slug: string; paleta?: RestauranteSimulado['paleta'] };
    altas.push(cuerpo);
    if (opciones.erroresAlta) {
      return route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          status: 400,
          title: 'Datos inválidos',
          detail: 'Uno o más campos no son válidos.',
          errors: opciones.erroresAlta,
        }),
      });
    }
    const nuevo: RestauranteSimulado = {
      id: `nuevo-${altas.length}`,
      nombre: cuerpo.nombre,
      slug: cuerpo.slug,
      activo: true,
      paleta: cuerpo.paleta ?? {},
    };
    restaurantes.push(nuevo);
    return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(nuevo) });
  });

  await page.route(/\/api\/plataforma\/restaurantes\/[^/]+$/, (route) => {
    if (route.request().method() !== 'PUT') return route.fallback();
    const id = new URL(route.request().url()).pathname.split('/').at(-1)!;
    const cuerpo = route.request().postDataJSON() as { nombre: string; paleta?: RestauranteSimulado['paleta'] };
    ediciones.push({ id, cuerpo });
    const restaurante = restaurantes.find((r) => r.id === id);
    if (!restaurante) return problema(route, 404, 'No encontrado', 'Restaurante no encontrado');
    restaurante.nombre = cuerpo.nombre;
    restaurante.paleta = cuerpo.paleta ?? {};
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(restaurante) });
  });

  await page.route('**/api/plataforma/restaurantes/*/logo', (route) => {
    const id = new URL(route.request().url()).pathname.split('/').at(-2)!;
    logos.push({
      id,
      tipoContenido: route.request().headers()['content-type'] ?? '',
      tamano: route.request().postDataBuffer()?.length ?? 0,
    });
    return route.fulfill({ status: 204 });
  });

  await page.route('**/api/plataforma/restaurantes/*/estado', (route) => {
    const id = new URL(route.request().url()).pathname.split('/').at(-2)!;
    const cuerpo = route.request().postDataJSON() as { activo: boolean };
    cambiosDeEstado.push({ id, cuerpo });
    const restaurante = restaurantes.find((r) => r.id === id);
    if (!restaurante) return problema(route, 404, 'No encontrado', 'Restaurante no encontrado');
    restaurante.activo = cuerpo.activo;
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(restaurante) });
  });

  return { restaurantes, cambiosDeEstado, altas, ediciones, logos };
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
