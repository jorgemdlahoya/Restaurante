import type { Rol } from '../api/auth';
import { pedirApi } from '../api/cliente';

export type SucursalAsignada = { slug: string; nombre: string; activa: boolean };

export type Usuario = {
  id: string;
  nombreUsuario: string;
  rol: Rol;
  activo: boolean;
  sucursales: SucursalAsignada[];
  /** Si el usuario actual puede editarlo, asignarle contraseña o cambiar su estado. */
  gestionable: boolean;
};

export type RolRestaurante = Exclude<Rol, 'administrador_plataforma'>;

export const NOMBRES_ROL: Record<RolRestaurante, string> = {
  administrador_general: 'Administrador general',
  administrador_sucursal: 'Administrador de sucursal',
  cajero: 'Cajero',
  cocinero: 'Cocinero',
};

export type DatosAlta = { nombreUsuario: string; contrasena: string; rol: RolRestaurante; sucursales: string[] };
export type DatosEdicion = { rol: RolRestaurante; sucursales: string[] };

export const claveUsuarios = (slugRestaurante: string) => ['usuarios', slugRestaurante] as const;

const base = (slugRestaurante: string) => `/api/r/${encodeURIComponent(slugRestaurante)}/usuarios`;

function json(metodo: string, cuerpo: unknown, token: string): RequestInit & { token: string } {
  return { method: metodo, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cuerpo), token };
}

export function listarUsuarios(token: string, slugRestaurante: string): Promise<Usuario[]> {
  return pedirApi<Usuario[]>(base(slugRestaurante), { token });
}

export function crearUsuario(token: string, slugRestaurante: string, datos: DatosAlta): Promise<Usuario> {
  return pedirApi<Usuario>(base(slugRestaurante), json('POST', datos, token));
}

export function editarUsuario(token: string, slugRestaurante: string, id: string, datos: DatosEdicion): Promise<Usuario> {
  return pedirApi<Usuario>(`${base(slugRestaurante)}/${id}`, json('PUT', datos, token));
}

export function cambiarEstadoUsuario(token: string, slugRestaurante: string, id: string, activo: boolean): Promise<Usuario> {
  return pedirApi<Usuario>(`${base(slugRestaurante)}/${id}/estado`, json('PUT', { activo }, token));
}

export function cambiarContrasena(token: string, slugRestaurante: string, id: string, contrasena: string): Promise<void> {
  return pedirApi<void>(`${base(slugRestaurante)}/${id}/contrasena`, json('PUT', { contrasena }, token));
}
