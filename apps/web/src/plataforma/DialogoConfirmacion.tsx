import { useEffect, useId, useRef } from 'react';
import estilos from './DialogoConfirmacion.module.css';

type Props = {
  titulo: string;
  mensaje: string;
  confirmar: string;
  onConfirmar(): void;
  onCancelar(): void;
};

export function DialogoConfirmacion({ titulo, mensaje, confirmar, onConfirmar, onCancelar }: Props) {
  const idTitulo = useId();
  const idMensaje = useId();
  const cancelarRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    cancelarRef.current?.focus();
    const alPulsar = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancelar();
    };
    document.addEventListener('keydown', alPulsar);
    return () => document.removeEventListener('keydown', alPulsar);
  }, [onCancelar]);

  return (
    <div className={estilos.fondo}>
      <div role="dialog" aria-modal="true" aria-labelledby={idTitulo} aria-describedby={idMensaje} className={estilos.dialogo}>
        <h2 id={idTitulo} className={estilos.titulo}>
          {titulo}
        </h2>
        <p id={idMensaje}>{mensaje}</p>
        <div className={estilos.acciones}>
          <button ref={cancelarRef} type="button" className={estilos.secundario} onClick={onCancelar}>
            Cancelar
          </button>
          <button type="button" className={estilos.peligro} onClick={onConfirmar}>
            {confirmar}
          </button>
        </div>
      </div>
    </div>
  );
}
