import { pedirApi } from '../api/cliente';
import type { PaletaParcial } from '../tema/paleta';

export type Identidad = { nombre: string; slug: string; paleta: PaletaParcial; logoUrl: string | null };

// Endpoint anónimo: nunca se envía token (un token de otro restaurante daría 404).
export function obtenerIdentidad(slug: string): Promise<Identidad> {
  return pedirApi<Identidad>(`/api/r/${encodeURIComponent(slug)}/identidad`);
}
