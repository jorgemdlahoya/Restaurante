import { pedirApi } from '../api/cliente';

export type Categoria = { id: string; nombre: string; nombreCorto: string | null; orden: number; activa: boolean };
export type DatosCategoria = { nombre: string; nombreCorto: string | null; orden: number };
export type PaginaCategorias = { elementos: Categoria[]; total: number; pagina: number; tamano: number };

const TAMANO_PAGINA = 20;

const base = (slugRestaurante: string) => `/api/r/${encodeURIComponent(slugRestaurante)}/categorias`;

function json(metodo: string, cuerpo: unknown, token: string): RequestInit & { token: string } {
  return { method: metodo, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cuerpo), token };
}

/** Clave de consulta de la lista de categorías; sin página, sirve para invalidar todas. */
export const claveCategorias = (slugRestaurante: string, pagina?: number) =>
  pagina === undefined ? ['categorias', slugRestaurante] : ['categorias', slugRestaurante, pagina];

export function listarCategorias(token: string, slugRestaurante: string, pagina: number): Promise<PaginaCategorias> {
  return pedirApi<PaginaCategorias>(`${base(slugRestaurante)}?pagina=${pagina}&tamano=${TAMANO_PAGINA}`, { token });
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
  return pedirApi<Categoria>(`${base(slugRestaurante)}/${id}`, json('PUT', datos, token));
}

export function cambiarEstadoCategoria(
  token: string,
  slugRestaurante: string,
  id: string,
  activa: boolean,
): Promise<Categoria> {
  return pedirApi<Categoria>(`${base(slugRestaurante)}/${id}/estado`, json('PUT', { activa }, token));
}

export function eliminarCategoria(token: string, slugRestaurante: string, id: string): Promise<void> {
  return pedirApi<void>(`${base(slugRestaurante)}/${id}`, { method: 'DELETE', token });
}
