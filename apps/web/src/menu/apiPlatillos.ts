import { pedirApi } from '../api/cliente';
import { json, TAMANO_PAGINA } from './api';

export type Platillo = {
  id: string;
  categoriaId: string;
  categoriaNombre: string;
  nombre: string;
  nombreCorto: string | null;
  descripcion: string | null;
  orden: number;
  ingredientes: string[];
  activo: boolean;
};
export type DatosPlatillo = {
  nombre: string;
  nombreCorto: string | null;
  descripcion: string | null;
  orden: number;
  ingredientes: string[];
};
export type PaginaPlatillos = { elementos: Platillo[]; total: number; pagina: number; tamano: number };

const raiz = (slugRestaurante: string) => `/api/r/${encodeURIComponent(slugRestaurante)}`;

/** Clave de consulta de los platillos de una categoría; sin página, sirve para invalidar todas. */
export const clavePlatillos = (slugRestaurante: string, categoriaId: string, pagina?: number) =>
  pagina === undefined
    ? ['platillos', slugRestaurante, categoriaId]
    : ['platillos', slugRestaurante, categoriaId, pagina];

export function listarPlatillos(
  token: string,
  slugRestaurante: string,
  categoriaId: string,
  pagina: number,
): Promise<PaginaPlatillos> {
  return pedirApi<PaginaPlatillos>(
    `${raiz(slugRestaurante)}/categorias/${encodeURIComponent(categoriaId)}/platillos?pagina=${pagina}&tamano=${TAMANO_PAGINA}`,
    { token },
  );
}

export function crearPlatillo(
  token: string,
  slugRestaurante: string,
  categoriaId: string,
  datos: DatosPlatillo,
): Promise<Platillo> {
  return pedirApi<Platillo>(`${raiz(slugRestaurante)}/categorias/${encodeURIComponent(categoriaId)}/platillos`, json('POST', datos, token));
}

export function editarPlatillo(
  token: string,
  slugRestaurante: string,
  id: string,
  datos: DatosPlatillo & { categoriaId: string },
): Promise<Platillo> {
  return pedirApi<Platillo>(`${raiz(slugRestaurante)}/platillos/${encodeURIComponent(id)}`, json('PUT', datos, token));
}

export function cambiarEstadoPlatillo(
  token: string,
  slugRestaurante: string,
  id: string,
  activo: boolean,
): Promise<Platillo> {
  return pedirApi<Platillo>(`${raiz(slugRestaurante)}/platillos/${encodeURIComponent(id)}/estado`, json('PUT', { activo }, token));
}

export function eliminarPlatillo(token: string, slugRestaurante: string, id: string): Promise<void> {
  return pedirApi<void>(`${raiz(slugRestaurante)}/platillos/${encodeURIComponent(id)}`, { method: 'DELETE', token });
}
