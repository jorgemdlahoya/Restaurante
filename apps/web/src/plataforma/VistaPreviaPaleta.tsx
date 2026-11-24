import { useLayoutEffect, useRef } from 'react';
import { aplicarPaleta, type Paleta } from '../tema/paleta';
import estilos from './VistaPreviaPaleta.module.css';

/** Muestra un encabezado y un botón de ejemplo con la paleta, fijando los tokens sólo en su propio elemento. */
export function VistaPreviaPaleta({ paleta }: { paleta: Paleta }) {
  const ref = useRef<HTMLElement>(null);

  useLayoutEffect(() => {
    if (ref.current) aplicarPaleta(paleta, ref.current);
  }, [paleta]);

  return (
    <section ref={ref} aria-label="Vista previa" className={estilos.vista}>
      <div className={estilos.encabezado}>Encabezado de ejemplo</div>
      <div className={estilos.cuerpo}>
        <p className={estilos.texto}>Así verá el personal sus pantallas.</p>
        <span className={estilos.boton}>Botón</span>
        <span className={estilos.acento}>Acento</span>
      </div>
    </section>
  );
}
