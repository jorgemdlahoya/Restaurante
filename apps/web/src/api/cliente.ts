const BASE = import.meta.env.VITE_API_URL ?? '';

export class ErrorApi extends Error {
  estado: number;
  titulo: string;
  detalle?: string;
  errores: Record<string, string[]>;

  constructor(estado: number, titulo: string, detalle?: string, errores: Record<string, string[]> = {}) {
    super(detalle ?? titulo);
    this.name = 'ErrorApi';
    this.estado = estado;
    this.titulo = titulo;
    this.detalle = detalle;
    this.errores = errores;
  }
}

type OpcionesApi = RequestInit & { token?: string };

// El token se añade sólo si se pasa explícitamente: un token de restaurante enviado a otro
// restaurante devuelve 404, así que nunca se toma de la sesión de forma automática.
export async function pedirApi<T>(ruta: string, opciones: OpcionesApi = {}): Promise<T> {
  const { token, headers: cabecerasIniciales, ...resto } = opciones;
  const headers = new Headers(cabecerasIniciales);
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const respuesta = await fetch(`${BASE}${ruta}`, { ...resto, headers });

  if (respuesta.status === 204 || respuesta.status === 304) return undefined as T;

  if (!respuesta.ok) {
    let problema: { title?: string; detail?: string; errors?: Record<string, string[]> } = {};
    try {
      problema = await respuesta.json();
    } catch {
      // Cuerpo vacío o no JSON: se usa el estado HTTP.
    }
    throw new ErrorApi(
      respuesta.status,
      problema.title ?? respuesta.statusText ?? 'Error',
      problema.detail,
      problema.errors ?? {},
    );
  }

  return (await respuesta.json()) as T;
}
