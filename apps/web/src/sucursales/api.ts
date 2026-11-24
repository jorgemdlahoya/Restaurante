import { pedirApi } from '../api/cliente';

export type Sucursal = {
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

/** Datos editables; el `slug` sólo se envía en el alta (es inmutable). */
export type DatosSucursal = Omit<Sucursal, 'id' | 'activa' | 'slug'> & { slug?: string };

/** Zonas horarias admitidas (RNF-01 de la feature 003). */
export const ZONAS_HORARIAS = [
  'America/Mexico_City',
  'America/Cancun',
  'America/Merida',
  'America/Monterrey',
  'America/Matamoros',
  'America/Chihuahua',
  'America/Ciudad_Juarez',
  'America/Ojinaga',
  'America/Mazatlan',
  'America/Bahia_Banderas',
  'America/Hermosillo',
  'America/Tijuana',
] as const;

export const ZONA_POR_DEFECTO = 'America/Mexico_City';

const base = (slugRestaurante: string) => `/api/r/${encodeURIComponent(slugRestaurante)}/sucursales`;

function json(metodo: string, cuerpo: unknown, token: string): RequestInit & { token: string } {
  return { method: metodo, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cuerpo), token };
}

export function listarSucursales(token: string, slugRestaurante: string): Promise<Sucursal[]> {
  return pedirApi<Sucursal[]>(base(slugRestaurante), { token });
}

export function crearSucursal(token: string, slugRestaurante: string, datos: DatosSucursal): Promise<Sucursal> {
  return pedirApi<Sucursal>(base(slugRestaurante), json('POST', datos, token));
}

export function editarSucursal(
  token: string,
  slugRestaurante: string,
  id: string,
  datos: DatosSucursal,
): Promise<Sucursal> {
  return pedirApi<Sucursal>(`${base(slugRestaurante)}/${id}`, json('PUT', datos, token));
}

export function cambiarEstadoSucursal(
  token: string,
  slugRestaurante: string,
  id: string,
  activa: boolean,
): Promise<Sucursal> {
  return pedirApi<Sucursal>(`${base(slugRestaurante)}/${id}/estado`, json('PUT', { activa }, token));
}

/** Clave de consulta compartida por la lista y el selector de sucursal. */
export const claveSucursales = (slugRestaurante: string) => ['sucursales', slugRestaurante];
