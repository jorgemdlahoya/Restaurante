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

export type HorarioDia = { dia: string; cerrado: boolean; apertura: string | null; cierre: string | null };
/** Fechas en hora local de la sucursal, sin zona (`yyyy-MM-ddTHH:mm`). */
export type Cierre = { id: string; inicio: string; fin: string; motivo: string | null };
export type EstadoAtencion = { abierta: boolean; mensaje: string | null };
export type DatosCierre = { inicio: string | null; fin: string; motivo: string | null };

const baseSucursal = (slugRestaurante: string, slugSucursal: string) =>
  `/api/r/${encodeURIComponent(slugRestaurante)}/s/${encodeURIComponent(slugSucursal)}`;

export function obtenerHorario(token: string, slugRestaurante: string, slugSucursal: string): Promise<HorarioDia[]> {
  return pedirApi<HorarioDia[]>(`${baseSucursal(slugRestaurante, slugSucursal)}/horario`, { token });
}

export function guardarHorario(
  token: string,
  slugRestaurante: string,
  slugSucursal: string,
  dias: HorarioDia[],
): Promise<HorarioDia[]> {
  return pedirApi<HorarioDia[]>(`${baseSucursal(slugRestaurante, slugSucursal)}/horario`, json('PUT', { dias }, token));
}

export function listarCierres(token: string, slugRestaurante: string, slugSucursal: string): Promise<Cierre[]> {
  return pedirApi<Cierre[]>(`${baseSucursal(slugRestaurante, slugSucursal)}/cierres`, { token });
}

export function crearCierre(
  token: string,
  slugRestaurante: string,
  slugSucursal: string,
  datos: DatosCierre,
): Promise<Cierre> {
  return pedirApi<Cierre>(`${baseSucursal(slugRestaurante, slugSucursal)}/cierres`, json('POST', datos, token));
}

export function cancelarCierre(token: string, slugRestaurante: string, slugSucursal: string, id: string): Promise<Cierre> {
  return pedirApi<Cierre>(`${baseSucursal(slugRestaurante, slugSucursal)}/cierres/${id}/cancelar`, {
    method: 'POST',
    token,
  });
}

/** Estado público: nunca lleva token (un token de otro restaurante daría 404). */
export function obtenerEstado(slugRestaurante: string, slugSucursal: string): Promise<EstadoAtencion> {
  return pedirApi<EstadoAtencion>(`${baseSucursal(slugRestaurante, slugSucursal)}/estado`);
}

export const claveEstado = (slugRestaurante: string, slugSucursal: string) => ['estado', slugRestaurante, slugSucursal];

/** Clave de consulta compartida por la lista y el selector de sucursal. */
export const claveSucursales = (slugRestaurante: string) => ['sucursales', slugRestaurante];
