import { ErrorApi } from '../api/cliente';

export const MENSAJE_SIN_PERMISO = 'No tienes permiso para esta acción.';

/** Un 403 siempre dice que no hay permiso; cualquier otro error usa el texto `alternativo`. */
export function mensajeDeError(error: unknown, alternativo: string): string {
  if (error instanceof ErrorApi && error.estado === 403) return MENSAJE_SIN_PERMISO;
  return alternativo;
}
