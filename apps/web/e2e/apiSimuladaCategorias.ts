import type { Page, Route } from '@playwright/test';

// Ayudas de la suite simulada para las categorías del menú de un restaurante (feature 005).

export type CategoriaSimulada = {
  id: string;
  nombre: string;
  nombreCorto: string | null;
  orden: number;
  activa: boolean;
};

type OpcionesCategorias = {
  /** Si se indica, el alta (POST) responde con este estado y estos errores por campo. */
  erroresAlta?: { estado: 400 | 409; errors: Record<string, string[]> };
  /** Platillos sin eliminar de cada categoría (feature 006): si hay alguno, DELETE responde 409. */
  platillosPorCategoria?: Record<string, number>;
};

function json(route: Route, status: number, cuerpo: unknown) {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(cuerpo) });
}

function noEncontrada(route: Route) {
  return json(route, 404, { status: 404, title: 'No encontrado', detail: 'Categoría no encontrada' });
}

function ordenadas(categorias: CategoriaSimulada[]) {
  return [...categorias].sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre));
}

/**
 * Simula `/api/r/{slug}/categorias` con estado en memoria: GET paginado (`pagina`, `tamano`, orden por
 * `orden` y `nombre`), POST alta, PUT /{id} edición, PUT /{id}/estado y DELETE /{id}. Devuelve los
 * registros de peticiones para afirmar sobre ellas.
 */
export async function simularCategorias(
  page: Page,
  slugRestaurante: string,
  iniciales: CategoriaSimulada[],
  opciones: OpcionesCategorias = {},
) {
  const categorias = iniciales.map((c) => ({ ...c }));
  const consultas: { pagina: number; tamano: number }[] = [];
  const altas: Record<string, unknown>[] = [];
  const ediciones: { id: string; cuerpo: Record<string, unknown> }[] = [];
  const cambiosDeEstado: { id: string; cuerpo: unknown }[] = [];
  const eliminaciones: string[] = [];
  const base = `/api/r/${slugRestaurante}/categorias`;

  await page.route(new RegExp(`${base}(\\?.*)?$`), (route) => {
    const metodo = route.request().method();
    if (metodo === 'GET') {
      const url = new URL(route.request().url());
      const pagina = Number(url.searchParams.get('pagina') ?? '1');
      const tamano = Number(url.searchParams.get('tamano') ?? '20');
      consultas.push({ pagina, tamano });
      const todas = ordenadas(categorias);
      const elementos = todas.slice((pagina - 1) * tamano, pagina * tamano);
      return json(route, 200, { elementos, total: todas.length, pagina, tamano });
    }
    if (metodo !== 'POST') return route.fallback();

    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
    altas.push(cuerpo);
    if (opciones.erroresAlta) {
      const { estado, errors } = opciones.erroresAlta;
      return route.fulfill({
        status: estado,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          status: estado,
          title: estado === 409 ? 'Conflicto' : 'Datos inválidos',
          detail: Object.values(errors).flat().join('. '),
          errors,
        }),
      });
    }
    const nueva: CategoriaSimulada = {
      id: `cat-nueva-${altas.length}`,
      nombre: String(cuerpo.nombre).trim(),
      nombreCorto: (cuerpo.nombreCorto as string | null) ?? null,
      orden: Number(cuerpo.orden),
      activa: true,
    };
    categorias.push(nueva);
    return json(route, 201, nueva);
  });

  await page.route(new RegExp(`${base}/[^/?]+$`), (route) => {
    const metodo = route.request().method();
    const id = new URL(route.request().url()).pathname.split('/').at(-1)!;
    const indice = categorias.findIndex((c) => c.id === id);
    if (metodo === 'DELETE') {
      eliminaciones.push(id);
      if (indice < 0) return noEncontrada(route);
      const conPlatillos = opciones.platillosPorCategoria?.[id] ?? 0;
      if (conPlatillos > 0) {
        return route.fulfill({
          status: 409,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            status: 409,
            title: 'Conflicto',
            detail: `Esta categoría tiene ${conPlatillos} ${conPlatillos === 1 ? 'platillo' : 'platillos'}`,
            platillos: conPlatillos,
          }),
        });
      }
      categorias.splice(indice, 1);
      return route.fulfill({ status: 204 });
    }
    if (metodo !== 'PUT') return route.fallback();
    const cuerpo = route.request().postDataJSON() as Record<string, unknown>;
    ediciones.push({ id, cuerpo });
    if (indice < 0) return noEncontrada(route);
    Object.assign(categorias[indice], {
      nombre: String(cuerpo.nombre).trim(),
      nombreCorto: (cuerpo.nombreCorto as string | null) ?? null,
      orden: Number(cuerpo.orden),
    });
    return json(route, 200, categorias[indice]);
  });

  await page.route(new RegExp(`${base}/[^/]+/estado$`), (route) => {
    const id = new URL(route.request().url()).pathname.split('/').at(-2)!;
    const cuerpo = route.request().postDataJSON() as { activa: boolean };
    cambiosDeEstado.push({ id, cuerpo });
    const categoria = categorias.find((c) => c.id === id);
    if (!categoria) return noEncontrada(route);
    categoria.activa = cuerpo.activa;
    return json(route, 200, categoria);
  });

  return { categorias, consultas, altas, ediciones, cambiosDeEstado, eliminaciones };
}

export function categoria(parcial: Partial<CategoriaSimulada> & Pick<CategoriaSimulada, 'id' | 'nombre'>): CategoriaSimulada {
  return { nombreCorto: null, orden: 1, activa: true, ...parcial };
}

/** `n` categorías `Categoría 01`…`Categoría n` con orden 1…n. */
export function variasCategorias(n: number): CategoriaSimulada[] {
  return Array.from({ length: n }, (_, i) => {
    const numero = String(i + 1).padStart(2, '0');
    return categoria({ id: `c${numero}`, nombre: `Categoría ${numero}`, orden: i + 1 });
  });
}
