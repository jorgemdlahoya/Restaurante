import type { Page, Route } from '@playwright/test';
import type { CategoriaSimulada } from './apiSimuladaCategorias';

// Ayudas de la suite simulada para los platillos del menú (feature 006).

export type PlatilloSimulado = {
  id: string;
  categoriaId: string;
  nombre: string;
  nombreCorto: string | null;
  descripcion: string | null;
  orden: number;
  ingredientes: string[];
  activo: boolean;
};

function json(route: Route, status: number, cuerpo: unknown) {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(cuerpo) });
}

function noEncontrado(route: Route, detail: string) {
  return json(route, 404, { status: 404, title: 'No encontrado', detail });
}

/**
 * Simula los endpoints de platillos con estado en memoria: GET `/categorias/{id}` (usa la lista viva de
 * `categorias`, la misma que devuelve `simularCategorias`), GET y POST `/categorias/{id}/platillos`
 * (paginado por `orden` y `nombre`), GET y PUT `/platillos/{id}`, PUT `/platillos/{id}/estado` y DELETE
 * `/platillos/{id}`. Registrar después de `simularCategorias`: lo que no reconoce pasa a sus rutas.
 */
export async function simularPlatillos(
  page: Page,
  slugRestaurante: string,
  categorias: CategoriaSimulada[],
  iniciales: PlatilloSimulado[],
) {
  const platillos = iniciales.map((p) => ({ ...p, ingredientes: [...p.ingredientes] }));
  const altas: { categoriaId: string; cuerpo: Record<string, unknown> }[] = [];
  const ediciones: { id: string; cuerpo: Record<string, unknown> }[] = [];
  const cambiosDeEstado: { id: string; cuerpo: unknown }[] = [];
  const eliminaciones: string[] = [];
  const base = `/api/r/${slugRestaurante}`;

  const dto = (p: PlatilloSimulado) => ({
    ...p,
    categoriaNombre: categorias.find((c) => c.id === p.categoriaId)?.nombre ?? '',
  });

  await page.route(new RegExp(`${base}/categorias/[^/?]+$`), (route) => {
    if (route.request().method() !== 'GET') return route.fallback();
    const id = new URL(route.request().url()).pathname.split('/').at(-1)!;
    const categoria = categorias.find((c) => c.id === id);
    return categoria ? json(route, 200, categoria) : noEncontrado(route, 'Categoría no encontrada');
  });

  await page.route(new RegExp(`${base}/categorias/[^/]+/platillos(\\?.*)?$`), (route) => {
    const url = new URL(route.request().url());
    const categoriaId = url.pathname.split('/').at(-2)!;
    const categoria = categorias.find((c) => c.id === categoriaId);
    if (!categoria) return noEncontrado(route, 'Categoría no encontrada');

    if (route.request().method() === 'GET') {
      const pagina = Number(url.searchParams.get('pagina') ?? '1');
      const tamano = Number(url.searchParams.get('tamano') ?? '20');
      const todos = platillos
        .filter((p) => p.categoriaId === categoriaId)
        .sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre));
      const elementos = todos.slice((pagina - 1) * tamano, pagina * tamano).map(dto);
      return json(route, 200, { elementos, total: todos.length, pagina, tamano });
    }

    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
    altas.push({ categoriaId, cuerpo });
    const nuevo: PlatilloSimulado = {
      id: `pla-nuevo-${altas.length}`,
      categoriaId,
      nombre: String(cuerpo.nombre).trim(),
      nombreCorto: (cuerpo.nombreCorto as string | null) ?? null,
      descripcion: (cuerpo.descripcion as string | null) ?? null,
      orden: Number(cuerpo.orden),
      ingredientes: (cuerpo.ingredientes as string[] | undefined) ?? [],
      activo: true,
    };
    platillos.push(nuevo);
    return json(route, 201, dto(nuevo));
  });

  await page.route(new RegExp(`${base}/platillos/[^/?]+$`), (route) => {
    const metodo = route.request().method();
    const id = new URL(route.request().url()).pathname.split('/').at(-1)!;
    const indice = platillos.findIndex((p) => p.id === id);
    if (indice < 0) {
      if (metodo === 'DELETE') eliminaciones.push(id);
      return noEncontrado(route, 'Platillo no encontrado');
    }
    if (metodo === 'GET') return json(route, 200, dto(platillos[indice]));
    if (metodo === 'DELETE') {
      eliminaciones.push(id);
      platillos.splice(indice, 1);
      return route.fulfill({ status: 204 });
    }
    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
    ediciones.push({ id, cuerpo });
    Object.assign(platillos[indice], {
      categoriaId: String(cuerpo.categoriaId),
      nombre: String(cuerpo.nombre).trim(),
      nombreCorto: (cuerpo.nombreCorto as string | null) ?? null,
      descripcion: (cuerpo.descripcion as string | null) ?? null,
      orden: Number(cuerpo.orden),
      ingredientes: (cuerpo.ingredientes as string[] | undefined) ?? [],
    });
    return json(route, 200, dto(platillos[indice]));
  });

  await page.route(new RegExp(`${base}/platillos/[^/]+/estado$`), (route) => {
    const id = new URL(route.request().url()).pathname.split('/').at(-2)!;
    const cuerpo = route.request().postDataJSON() as { activo: boolean };
    cambiosDeEstado.push({ id, cuerpo });
    const platillo = platillos.find((p) => p.id === id);
    if (!platillo) return noEncontrado(route, 'Platillo no encontrado');
    platillo.activo = cuerpo.activo;
    return json(route, 200, dto(platillo));
  });

  return { platillos, altas, ediciones, cambiosDeEstado, eliminaciones };
}

export function platillo(
  parcial: Partial<PlatilloSimulado> & Pick<PlatilloSimulado, 'id' | 'nombre' | 'categoriaId'>,
): PlatilloSimulado {
  return { nombreCorto: null, descripcion: null, orden: 1, ingredientes: [], activo: true, ...parcial };
}

/** `n` platillos `Platillo 01`…`Platillo n` de la categoría, con orden 1…n. */
export function variosPlatillos(categoriaId: string, n: number): PlatilloSimulado[] {
  return Array.from({ length: n }, (_, i) => {
    const numero = String(i + 1).padStart(2, '0');
    return platillo({ id: `p${numero}`, categoriaId, nombre: `Platillo ${numero}`, orden: i + 1 });
  });
}
