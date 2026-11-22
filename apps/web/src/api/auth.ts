import { pedirApi } from './cliente';

export type Rol =
  | 'administrador_plataforma'
  | 'administrador_general'
  | 'administrador_sucursal'
  | 'cajero'
  | 'cocinero';

export type LoginRespuesta = { token: string; expiraEn: string; nombreUsuario: string; rol: Rol };

function enviarLogin(ruta: string, nombreUsuario: string, contrasena: string) {
  return pedirApi<LoginRespuesta>(ruta, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario, contrasena }),
  });
}

export function iniciarSesionRestaurante(slug: string, nombreUsuario: string, contrasena: string) {
  return enviarLogin(`/api/r/${encodeURIComponent(slug)}/auth/login`, nombreUsuario, contrasena);
}

export function iniciarSesionPlataforma(nombreUsuario: string, contrasena: string) {
  return enviarLogin('/api/plataforma/auth/login', nombreUsuario, contrasena);
}
