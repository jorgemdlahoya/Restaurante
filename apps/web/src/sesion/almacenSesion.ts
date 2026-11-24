import type { LoginRespuesta } from '../api/auth';

// slug null = plataforma; sucursalActiva = slug de la sucursal elegida en el selector (feature 003).
export type Sesion = LoginRespuesta & { slug: string | null; sucursalActiva?: string | null };

const CLAVE = 'restaurante.sesion';
let enMemoria: Sesion | null = null;

export function guardarSesion(s: Sesion): void {
  enMemoria = s;
  try {
    window.sessionStorage.setItem(CLAVE, JSON.stringify(s));
  } catch {
    // Sin sessionStorage la sesión vive sólo en memoria.
  }
}

export function borrarSesion(): void {
  enMemoria = null;
  try {
    window.sessionStorage.removeItem(CLAVE);
  } catch {
    // Nada que limpiar.
  }
}

function leerAlmacenada(): Sesion | null {
  if (enMemoria) return enMemoria;
  try {
    const crudo = window.sessionStorage.getItem(CLAVE);
    return crudo ? (JSON.parse(crudo) as Sesion) : null;
  } catch {
    return null;
  }
}

export function leerSesion(ahora: number = Date.now()): Sesion | null {
  const s = leerAlmacenada();
  if (!s) return null;
  if (Date.parse(s.expiraEn) <= ahora) {
    borrarSesion();
    return null;
  }
  enMemoria = s;
  return s;
}
