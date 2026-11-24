import { useState, type FormEvent } from 'react';
import type { HorarioDia } from './api';
import estilos from './EditorHorario.module.css';

type Props = {
  horario: HorarioDia[];
  onGuardar(dias: HorarioDia[]): Promise<void>;
  /** Errores 400 por clave (`dias`, `dias[i].apertura`, `dias[i].cierre`). */
  errores?: Record<string, string[]>;
};

type Fila = { dia: string; cerrado: boolean; apertura: string; cierre: string };

const aFila = (d: HorarioDia): Fila => ({ dia: d.dia, cerrado: d.cerrado, apertura: d.apertura ?? '', cierre: d.cierre ?? '' });

const capitalizar = (texto: string) => texto.charAt(0).toUpperCase() + texto.slice(1);

export function EditorHorario({ horario, onGuardar, errores = {} }: Props) {
  const [filas, setFilas] = useState<Fila[]>(() => horario.map(aFila));
  const [guardando, setGuardando] = useState(false);

  function cambiar(i: number, parcial: Partial<Fila>) {
    setFilas((actual) => actual.map((f, j) => (j === i ? { ...f, ...parcial } : f)));
  }

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setGuardando(true);
    try {
      await onGuardar(
        filas.map((f) =>
          f.cerrado
            ? { dia: f.dia, cerrado: true, apertura: null, cierre: null }
            : { dia: f.dia, cerrado: false, apertura: f.apertura || null, cierre: f.cierre || null },
        ),
      );
    } finally {
      setGuardando(false);
    }
  }

  return (
    <form className={estilos.editor} aria-label="Horario semanal" onSubmit={guardar} noValidate>
      {errores.dias && (
        <p role="alert" className={estilos.error}>
          {errores.dias.join(' ')}
        </p>
      )}
      <ul className={estilos.dias}>
        {filas.map((f, i) => {
          const nocturno = !f.cerrado && f.apertura !== '' && f.cierre !== '' && f.cierre < f.apertura;
          const erroresDia = [...(errores[`dias[${i}].apertura`] ?? []), ...(errores[`dias[${i}].cierre`] ?? [])];
          return (
            <li key={f.dia} className={estilos.dia}>
              <span className={estilos.nombre}>{capitalizar(f.dia)}</span>
              <label className={estilos.casilla}>
                <input type="checkbox" checked={f.cerrado} onChange={(e) => cambiar(i, { cerrado: e.target.checked })} />
                <span>Cerrado</span>
                <span className={estilos.oculto}> el {f.dia}</span>
              </label>
              <input
                type="time"
                aria-label={`Apertura del ${f.dia}`}
                value={f.apertura}
                disabled={f.cerrado}
                onChange={(e) => cambiar(i, { apertura: e.target.value })}
              />
              <input
                type="time"
                aria-label={`Cierre del ${f.dia}`}
                value={f.cierre}
                disabled={f.cerrado}
                onChange={(e) => cambiar(i, { cierre: e.target.value })}
              />
              {nocturno && <span className={estilos.aviso}>El turno termina al día siguiente</span>}
              {erroresDia.length > 0 && <span className={estilos.error}>{erroresDia.join(' ')}</span>}
            </li>
          );
        })}
      </ul>
      <button type="submit" className={estilos.principal} disabled={guardando}>
        Guardar horario
      </button>
    </form>
  );
}
