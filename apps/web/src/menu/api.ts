import { pedirApi } from '../api/cliente';

export type Categoria = { id: string; nombre: string; nombreCorto: string | null; orden: number; activa: boolean };
export type DatosCategoria = { nombre: string; nombreCorto: string | null; orden: number };
export type PaginaCategorias = { elementos: Categoria[]; total: number; pagina: number; tamano: number };

export const TAMANO_PAGINA = 20;

const base = (slugRestaurante: string) => `/api/r/${encodeURIComponent(slugRestaurante)}/categorias`;

export function json(metodo: string, cuerpo: unknown, token: string): RequestInit & { token: string } {
  return { method: metodo, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cuerpo), token };
}

/** Clave de consulta de la lista de categorías; sin página, sirve para invalidar todas. */
export const claveCategorias = (slugRestaurante: string, pagina?: number) =>
  pagina === undefined ? ['categorias', slugRestaurante] : ['categorias', slugRestaurante, pagina];

export function listarCategorias(token: string, slugRestaurante: string, pagina: number): Promise<PaginaCategorias> {
  return pedirApi<PaginaCategorias>(`${base(slugRestaurante)}?pagina=${pagina}&tamano=${TAMANO_PAGINA}`, { token });
}

export function obtenerCategoria(token: string, slugRestaurante: string, id: string): Promise<Categoria> {
  return pedirApi<Categoria>(`${base(slugRestaurante)}/${encodeURIComponent(id)}`, { token });
}

/** Categorías activas (hasta 100), para el selector de categoría de un platillo. */
export async function listarCategoriasActivas(token: string, slugRestaurante: string): Promise<Categoria[]> {
  const pagina = await pedirApi<PaginaCategorias>(`${base(slugRestaurante)}?pagina=1&tamano=100`, { token });
  return pagina.elementos.filter((c) => c.activa);
}

export function crearCategoria(token: string, slugRestaurante: string, datos: DatosCategoria): Promise<Categoria> {
  return pedirApi<Categoria>(base(slugRestaurante), json('POST', datos, token));
}

export function editarCategoria(
  token: string,
  slugRestaurante: string,
  id: string,
  datos: DatosCategoria,
): Promise<Categoria> {
  return pedirApi<Categoria>(`${base(slugRestaurante)}/${encodeURIComponent(id)}`, json('PUT', datos, token));
}

export function cambiarEstadoCategoria(
  token: string,
  slugRestaurante: string,
  id: string,
  activa: boolean,
): Promise<Categoria> {
  return pedirApi<Categoria>(`${base(slugRestaurante)}/${encodeURIComponent(id)}/estado`, json('PUT', { activa }, token));
}

export function eliminarCategoria(token: string, slugRestaurante: string, id: string): Promise<void> {
  return pedirApi<void>(`${base(slugRestaurante)}/${encodeURIComponent(id)}`, { method: 'DELETE', token });
}
