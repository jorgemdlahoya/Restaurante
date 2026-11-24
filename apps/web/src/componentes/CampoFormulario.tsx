import { useId, type InputHTMLAttributes, type SelectHTMLAttributes } from 'react';
import estilos from './CampoFormulario.module.css';

type Comunes = { etiqueta: string; errores?: string[] };

type PropsEntrada = Comunes & InputHTMLAttributes<HTMLInputElement> & { opciones?: undefined };
type PropsSelector = Comunes &
  SelectHTMLAttributes<HTMLSelectElement> & { opciones: { valor: string; etiqueta: string }[] };

/**
 * Campo de formulario con etiqueta y, si hay errores del servidor, un mensaje asociado por
 * aria-describedby (fuera del <label>, para no ensuciar el nombre accesible). Con `opciones`
 * se dibuja un <select>; sin ellas, un <input>.
 */
export function CampoFormulario(props: PropsEntrada | PropsSelector) {
  const idCampo = useId();
  const idError = useId();
  const { etiqueta, errores } = props;
  const conError = !!errores?.length;
  const accesibilidad = {
    id: idCampo,
    'aria-invalid': conError || undefined,
    'aria-describedby': conError ? idError : undefined,
  };

  let control;
  if (props.opciones) {
    const { etiqueta: _e, errores: _r, opciones, ...select } = props;
    control = (
      <select {...select} {...accesibilidad}>
        {opciones.map((o) => (
          <option key={o.valor} value={o.valor}>
            {o.etiqueta}
          </option>
        ))}
      </select>
    );
  } else {
    const { etiqueta: _e, errores: _r, opciones: _o, ...input } = props;
    control = <input {...input} {...accesibilidad} />;
  }

  return (
    <div className={estilos.campo}>
      <label htmlFor={idCampo}>{etiqueta}</label>
      {control}
      {conError && (
        <span id={idError} className={estilos.error}>
          {errores.join(' ')}
        </span>
      )}
    </div>
  );
}
