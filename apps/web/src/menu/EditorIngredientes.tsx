import { useState, type KeyboardEvent } from 'react';
import { CampoFormulario } from '../componentes/CampoFormulario';
import estilos from './EditorIngredientes.module.css';

type Props = { ingredientes: string[]; onCambio(lista: string[]): void; errores?: string[] };

export const MAXIMO_INGREDIENTES = 30;

const clave = (texto: string) => texto.trim().toLowerCase();

export function EditorIngredientes({ ingredientes, onCambio, errores }: Props) {
  const [texto, setTexto] = useState('');
  const [aviso, setAviso] = useState<string | null>(null);
  const lleno = ingredientes.length >= MAXIMO_INGREDIENTES;

  function agregar() {
    const limpio = texto.trim();
    if (limpio === '') {
      setAviso('Escribe un ingrediente');
      return;
    }
    if (ingredientes.some((i) => clave(i) === clave(limpio))) {
      setAviso('Ese ingrediente ya está en la lista');
      return;
    }
    setAviso(null);
    setTexto('');
    onCambio([...ingredientes, limpio]);
  }

  function alPulsar(evento: KeyboardEvent<HTMLInputElement>) {
    if (evento.key !== 'Enter') return;
    evento.preventDefault();
    if (!lleno) agregar();
  }

  return (
    <div className={estilos.editor}>
      <div className={estilos.entrada}>
        <CampoFormulario
          etiqueta="Ingredientes incluidos"
          errores={errores}
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          onKeyDown={alPulsar}
        />
        <button type="button" className={estilos.agregar} onClick={agregar} disabled={lleno}>
          Agregar ingrediente
        </button>
      </div>
      {aviso && !lleno && (
        <p role="status" className={estilos.aviso}>
          {aviso}
        </p>
      )}
      {lleno && (
        <p role="status" className={estilos.aviso}>
          No puede haber más de 30 ingredientes
        </p>
      )}
      <ul className={estilos.lista}>
        {ingredientes.map((ingrediente, indice) => (
          <li key={ingrediente} className={estilos.elemento}>
            <span>{ingrediente}</span>
            <button
              type="button"
              className={estilos.quitar}
              aria-label={`Quitar ${ingrediente}`}
              onClick={() => onCambio(ingredientes.filter((_, i) => i !== indice))}
            >
              Quitar
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
