/** Límite de WhatsApp para el título de una fila de lista, en unidades UTF-16 (`.length`). */
export const LIMITE_TITULO_WHATSAPP = 24;

function corto(nombreCorto: string | null): string | null {
  const recortado = nombreCorto?.trim() ?? '';
  return recortado === '' ? null : recortado;
}

/**
 * Título con el que una categoría se muestra en WhatsApp (RF-11). Misma regla que
 * `TituloWhatsApp.Calcular` en C#, pero aquí nombre y nombre corto se recortan antes y un
 * nombre corto en blanco cuenta como ausente (el formulario aún no los ha normalizado).
 */
export function tituloWhatsApp(nombre: string, nombreCorto: string | null): string {
  const abreviado = corto(nombreCorto);
  if (abreviado !== null) return abreviado;

  const completo = nombre.trim();
  if (completo.length <= LIMITE_TITULO_WHATSAPP) return completo;

  // Sin partir un par sustituto: si la unidad 23 (índice 22) es sustituto alto, se corta antes.
  const unidad = completo.charCodeAt(LIMITE_TITULO_WHATSAPP - 2);
  const alto = unidad >= 0xd800 && unidad <= 0xdbff;
  return completo.slice(0, alto ? LIMITE_TITULO_WHATSAPP - 2 : LIMITE_TITULO_WHATSAPP - 1) + '…';
}

export function seRecorta(nombre: string, nombreCorto: string | null): boolean {
  return corto(nombreCorto) === null && nombre.trim().length > LIMITE_TITULO_WHATSAPP;
}
