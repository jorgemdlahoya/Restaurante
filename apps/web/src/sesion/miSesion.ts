import type { Rol } from '../api/auth';
import { pedirApi } from '../api/cliente';

export type SucursalDeSesion = { slug: string; nombre: string };

/** Respuesta de `GET /api/r/{slug}/mi-sesion` (feature 004, RF-16). */
export type MiSesion = { nombreUsuario: string; rol: Rol; sucursales: SucursalDeSesion[] };

export const claveMiSesion = (slugRestaurante: string) => ['mi-sesion', slugRestaurante] as const;

export function obtenerMiSesion(token: string, slugRestaurante: string): Promise<MiSesion> {
  return pedirApi<MiSesion>(`/api/r/${encodeURIComponent(slugRestaurante)}/mi-sesion`, { token });
}
