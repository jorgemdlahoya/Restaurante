import { pedirApi } from '../api/cliente';
import type { PaletaParcial } from '../tema/paleta';

export type Restaurante = { id: string; nombre: string; slug: string; activo: boolean; paleta: PaletaParcial };

export type DatosAlta = {
  nombre: string;
  slug: string;
  paleta: PaletaParcial;
  administrador: { nombreUsuario: string; contrasena: string };
};

export type DatosEdicion = { nombre: string; paleta: PaletaParcial };

const BASE = '/api/plataforma/restaurantes';

function json(metodo: string, cuerpo: unknown, token: string): RequestInit & { token: string } {
  return {
    method: metodo,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(cuerpo),
    token,
  };
}

export function listarRestaurantes(token: string): Promise<Restaurante[]> {
  return pedirApi<Restaurante[]>(BASE, { token });
}

export function crearRestaurante(token: string, datos: DatosAlta): Promise<Restaurante> {
  return pedirApi<Restaurante>(BASE, json('POST', datos, token));
}

export function editarRestaurante(token: string, id: string, datos: DatosEdicion): Promise<Restaurante> {
  return pedirApi<Restaurante>(`${BASE}/${id}`, json('PUT', datos, token));
}

export function cambiarEstadoRestaurante(token: string, id: string, activo: boolean): Promise<Restaurante> {
  return pedirApi<Restaurante>(`${BASE}/${id}/estado`, json('PUT', { activo }, token));
}

export function subirLogo(token: string, id: string, archivo: File): Promise<void> {
  const formulario = new FormData();
  formulario.append('logo', archivo);
  return pedirApi<void>(`${BASE}/${id}/logo`, { method: 'PUT', body: formulario, token });
}
