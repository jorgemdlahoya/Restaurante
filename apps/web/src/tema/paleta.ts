export type Paleta = { principal: string; acento: string; oscuro: string; fondo: string; texto: string };
export type PaletaParcial = Partial<Record<keyof Paleta, string | null>>;

// Paleta por defecto (Art. 15 de la constitución).
export const PALETA_POR_DEFECTO: Paleta = {
  principal: '#F97316',
  acento: '#FF6600',
  oscuro: '#111111',
  fondo: '#F8F9FA',
  texto: '#1F2937',
};

export function resolverPaleta(p?: PaletaParcial | null): Paleta {
  const resultado: Paleta = { ...PALETA_POR_DEFECTO };
  if (!p) return resultado;
  for (const clave of Object.keys(PALETA_POR_DEFECTO) as (keyof Paleta)[]) {
    const valor = p[clave];
    if (valor) resultado[clave] = valor;
  }
  return resultado;
}

export function aplicarPaleta(p: Paleta, elemento: HTMLElement = document.documentElement): void {
  for (const clave of Object.keys(p) as (keyof Paleta)[]) {
    elemento.style.setProperty(`--color-${clave}`, p[clave]);
  }
}
