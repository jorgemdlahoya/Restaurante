import { useLayoutEffect, type ReactNode } from 'react';
import { aplicarPaleta, PALETA_POR_DEFECTO, resolverPaleta, type PaletaParcial } from './paleta';

type Props = { paleta?: PaletaParcial | null; children: ReactNode };

export function ProveedorTema({ paleta, children }: Props) {
  const { principal, acento, oscuro, fondo, texto } = resolverPaleta(paleta);

  useLayoutEffect(() => {
    aplicarPaleta({ principal, acento, oscuro, fondo, texto });
  }, [principal, acento, oscuro, fondo, texto]);

  useLayoutEffect(() => () => aplicarPaleta(PALETA_POR_DEFECTO), []);

  return <>{children}</>;
}
