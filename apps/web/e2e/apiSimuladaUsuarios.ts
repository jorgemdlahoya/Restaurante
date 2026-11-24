import type { Page, Route } from '@playwright/test';

// Ayudas de la suite simulada para los usuarios de un restaurante y la sesión (feature 004).

export type SucursalAsignadaSimulada = { slug: string; nombre: string; activa: boolean };

export type UsuarioSimulado = {
  id: string;
  nombreUsuario: string;
  rol: string;
  activo: boolean;
  sucursales: SucursalAsignadaSimulada[];
  gestionable: boolean;
};

export type MiSesionSimulada = {
  nombreUsuario: string;
  rol: string;
  sucursales: { slug: string; nombre: string }[];
};

function json(route: Route, status: number, cuerpo: unknown) {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(cuerpo) });
}

/** Simula `GET /api/r/{slug}/mi-sesion`. Devuelve el objeto para poder cambiarlo durante la prueba. */
export async function simularMiSesion(page: Page, slugRestaurante: string, sesion: MiSesionSimulada) {
  const datos = { ...sesion, sucursales: [...sesion.sucursales] };
  await page.route(`**/api/r/${slugRestaurante}/mi-sesion`, (route) => json(route, 200, datos));
  return datos;
}

/**
 * Simula `/api/r/{slug}/usuarios` con estado en memoria: GET lista, POST alta, PUT /{id} edición de rol
 * y sucursales, PUT /{id}/estado y PUT /{id}/contrasena (204). Las sucursales de la petición llegan como
 * slugs y se resuelven con `catalogo`. Devuelve los registros de peticiones.
 */
export async function simularUsuarios(
  page: Page,
  slugRestaurante: string,
  iniciales: UsuarioSimulado[],
  catalogo: SucursalAsignadaSimulada[],
) {
  const usuarios = iniciales.map((u) => ({ ...u, sucursales: [...u.sucursales] }));
  const altas: Record<string, unknown>[] = [];
  const ediciones: { id: string; cuerpo: Record<string, unknown> }[] = [];
  const cambiosDeEstado: { id: string; cuerpo: unknown }[] = [];
  const contrasenas: { id: string; cuerpo: unknown }[] = [];
  const base = `/api/r/${slugRestaurante}/usuarios`;

  const resolver = (slugs: unknown) =>
    ((slugs as string[] | undefined) ?? []).map((s) => catalogo.find((c) => c.slug === s)!).filter(Boolean);

  await page.route(`**${base}`, (route) => {
    const metodo = route.request().method();
    if (metodo === 'GET') return json(route, 200, usuarios);
    if (metodo !== 'POST') return route.fallback();
    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
    altas.push(cuerpo);
    const nuevo: UsuarioSimulado = {
      id: `u-nuevo-${altas.length}`,
      nombreUsuario: String(cuerpo.nombreUsuario),
      rol: String(cuerpo.rol),
      activo: true,
      sucursales: resolver(cuerpo.sucursales),
      gestionable: true,
    };
    usuarios.push(nuevo);
    return json(route, 201, nuevo);
  });

  await page.route(new RegExp(`${base}/[^/]+$`), (route) => {
    if (route.request().method() !== 'PUT') return route.fallback();
    const id = new URL(route.request().url()).pathname.split('/').at(-1)!;
    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
    ediciones.push({ id, cuerpo });
    const usuario = usuarios.find((u) => u.id === id)!;
    usuario.rol = String(cuerpo.rol);
    usuario.sucursales = resolver(cuerpo.sucursales);
    return json(route, 200, usuario);
  });

  await page.route(`**${base}/*/estado`, (route) => {
    const id = new URL(route.request().url()).pathname.split('/').at(-2)!;
    const cuerpo = route.request().postDataJSON() as { activo: boolean };
    cambiosDeEstado.push({ id, cuerpo });
    const usuario = usuarios.find((u) => u.id === id)!;
    usuario.activo = cuerpo.activo;
    return json(route, 200, usuario);
  });

  await page.route(`**${base}/*/contrasena`, (route) => {
    const id = new URL(route.request().url()).pathname.split('/').at(-2)!;
    contrasenas.push({ id, cuerpo: route.request().postDataJSON() });
    return route.fulfill({ status: 204 });
  });

  return { usuarios, altas, ediciones, cambiosDeEstado, contrasenas };
}

export const CENTRO_ASIGNADA: SucursalAsignadaSimulada = { slug: 'centro', nombre: 'Centro', activa: true };
export const NORTE_ASIGNADA: SucursalAsignadaSimulada = { slug: 'norte', nombre: 'Norte', activa: true };

export function usuario(parcial: Partial<UsuarioSimulado> & Pick<UsuarioSimulado, 'id' | 'nombreUsuario' | 'rol'>): UsuarioSimulado {
  return { activo: true, sucursales: [], gestionable: true, ...parcial };
}
