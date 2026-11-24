import type { Page, Route } from '@playwright/test';

// Ayudas de la suite simulada para las sucursales de un restaurante (feature 003).

export type SucursalSimulada = {
  id: string;
  nombre: string;
  direccion: string;
  telefono: string;
  slug: string;
  whatsApp: string;
  phoneNumberId: string | null;
  prefijo: string;
  zonaHoraria: string;
  activa: boolean;
};

type OpcionesSucursales = {
  /** Si se indica, el alta (POST) responde 400 con estos errores por campo. */
  erroresAlta?: Record<string, string[]>;
};

function json(route: Route, status: number, cuerpo: unknown) {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(cuerpo) });
}

/**
 * Simula `/api/r/{slug}/sucursales` con estado en memoria: GET lista, POST alta, PUT /{id} edición y
 * PUT /{id}/estado cambio de estado. Devuelve los registros de peticiones para afirmar sobre su cuerpo.
 */
export async function simularSucursales(
  page: Page,
  slugRestaurante: string,
  iniciales: SucursalSimulada[],
  opciones: OpcionesSucursales = {},
) {
  const sucursales = iniciales.map((s) => ({ ...s }));
  const altas: Record<string, unknown>[] = [];
  const ediciones: { id: string; cuerpo: Record<string, unknown> }[] = [];
  const cambiosDeEstado: { id: string; cuerpo: unknown }[] = [];
  const base = `/api/r/${slugRestaurante}/sucursales`;

  await page.route(`**${base}`, (route) => {
    const metodo = route.request().method();
    if (metodo === 'GET') return json(route, 200, sucursales);
    if (metodo !== 'POST') return route.fallback();

    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
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
    const nueva: SucursalSimulada = {
      id: `suc-nueva-${altas.length}`,
      nombre: String(cuerpo.nombre),
      direccion: String(cuerpo.direccion),
      telefono: String(cuerpo.telefono),
      slug: String(cuerpo.slug),
      whatsApp: String(cuerpo.whatsApp),
      phoneNumberId: (cuerpo.phoneNumberId as string | null) ?? null,
      prefijo: String(cuerpo.prefijo),
      zonaHoraria: String(cuerpo.zonaHoraria ?? 'America/Mexico_City'),
      activa: true,
    };
    sucursales.push(nueva);
    return json(route, 201, nueva);
  });

  await page.route(new RegExp(`${base}/[^/]+$`), (route) => {
    if (route.request().method() !== 'PUT') return route.fallback();
    const id = new URL(route.request().url()).pathname.split('/').at(-1)!;
    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
    ediciones.push({ id, cuerpo });
    const sucursal = sucursales.find((s) => s.id === id);
    if (!sucursal) return json(route, 404, { status: 404, title: 'No encontrado', detail: 'Sucursal no encontrada' });
    Object.assign(sucursal, { ...cuerpo, slug: sucursal.slug });
    return json(route, 200, sucursal);
  });

  await page.route(`**${base}/*/estado`, (route) => {
    const id = new URL(route.request().url()).pathname.split('/').at(-2)!;
    const cuerpo = route.request().postDataJSON() as { activa: boolean };
    cambiosDeEstado.push({ id, cuerpo });
    const sucursal = sucursales.find((s) => s.id === id);
    if (!sucursal) return json(route, 404, { status: 404, title: 'No encontrado', detail: 'Sucursal no encontrada' });
    sucursal.activa = cuerpo.activa;
    return json(route, 200, sucursal);
  });

  return { sucursales, altas, ediciones, cambiosDeEstado };
}

export function sucursal(parcial: Partial<SucursalSimulada> & Pick<SucursalSimulada, 'id' | 'nombre' | 'slug'>): SucursalSimulada {
  return {
    direccion: 'Av. Juárez 1',
    telefono: '+525512345678',
    whatsApp: '+525512345679',
    phoneNumberId: null,
    prefijo: 'A',
    zonaHoraria: 'America/Mexico_City',
    activa: true,
    ...parcial,
  };
}
