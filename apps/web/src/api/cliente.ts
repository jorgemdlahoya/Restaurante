const BASE = import.meta.env.VITE_API_URL ?? '';

export class ErrorApi extends Error {
  estado: number;
  titulo: string;
  detalle?: string;
  errores: Record<string, string[]>;
  /** Resto de campos del ProblemDetails (p. ej. `platillos` en el 409 de una categoría con platillos). */
  extensiones: Record<string, unknown>;

  constructor(
    estado: number,
    titulo: string,
    detalle?: string,
    errores: Record<string, string[]> = {},
    extensiones: Record<string, unknown> = {},
  ) {
    super(detalle ?? titulo);
    this.name = 'ErrorApi';
    this.estado = estado;
    this.titulo = titulo;
    this.detalle = detalle;
    this.errores = errores;
    this.extensiones = extensiones;
  }
}

/** URL absoluta de una ruta de la API (p. ej. el logoUrl relativo que devuelve la identidad). */
export function urlApi(ruta: string): string {
  return `${BASE}${ruta}`;
}

type OpcionesApi = RequestInit & { token?: string };

/** Título del 401 con que la API rechaza un token cuyo usuario cambió de acceso (feature 004, RF-13/RF-14). */
export const TITULO_SESION_INVALIDA = 'Sesión no válida';

const manejadoresSesionInvalida = new Set<() => void>();

/**
 * Suscribe un manejador que se llama cuando una petición **con token** recibe 401 "Sesión no válida".
 * Devuelve la función para cancelar la suscripción.
 */
export function alSesionInvalida(manejador: () => void): () => void {
  manejadoresSesionInvalida.add(manejador);
  return () => {
    manejadoresSesionInvalida.delete(manejador);
  };
}

// El token se añade sólo si se pasa explícitamente: un token de restaurante enviado a otro
// restaurante devuelve 404, así que nunca se toma de la sesión de forma automática.
export async function pedirApi<T>(ruta: string, opciones: OpcionesApi = {}): Promise<T> {
  const { token, headers: cabecerasIniciales, ...resto } = opciones;
  const headers = new Headers(cabecerasIniciales);
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const respuesta = await fetch(`${BASE}${ruta}`, { ...resto, headers });

  if (respuesta.status === 204 || respuesta.status === 304) return undefined as T;

  if (!respuesta.ok) {
    let problema: { title?: string; detail?: string; errors?: Record<string, string[]> } & Record<string, unknown> = {};
    try {
      problema = await respuesta.json();
    } catch {
      // Cuerpo vacío o no JSON: se usa el estado HTTP.
    }
    if (respuesta.status === 401 && token && problema.title === TITULO_SESION_INVALIDA) {
      manejadoresSesionInvalida.forEach((m) => m());
    }
    const { title, detail, errors, ...extensiones } = problema;
    throw new ErrorApi(respuesta.status, title ?? respuesta.statusText ?? 'Error', detail, errors ?? {}, extensiones);
  }

  return (await respuesta.json()) as T;
}
