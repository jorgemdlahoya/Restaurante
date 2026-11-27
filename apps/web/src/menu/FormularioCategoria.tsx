import { useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import { CampoFormulario } from '../componentes/CampoFormulario';
import { crearCategoria, editarCategoria, type Categoria } from './api';
import estilos from './FormularioCategoria.module.css';
import { mensajeDeError } from './mensajeError';
import { VistaPreviaWhatsApp } from './VistaPreviaWhatsApp';

type Props = {
  slugRestaurante: string;
  token: string;
  categoria?: Categoria;
  onGuardada(categoria: Categoria): void;
  onCancelar(): void;
};

const CAMPOS_CONOCIDOS = ['nombre', 'nombreCorto', 'orden'];
const MENSAJE_ORDEN = 'El orden debe ser un entero mayor o igual que 1';

function leerOrden(texto: string): number | null {
  const limpio = texto.trim();
  if (!/^\d+$/.test(limpio)) return null;
  const orden = Number(limpio);
  return Number.isSafeInteger(orden) && orden >= 1 ? orden : null;
}

export function FormularioCategoria({ slugRestaurante, token, categoria, onGuardada, onCancelar }: Props) {
  const [nombre, setNombre] = useState(categoria?.nombre ?? '');
  const [nombreCorto, setNombreCorto] = useState(categoria?.nombreCorto ?? '');
  const [orden, setOrden] = useState(categoria ? String(categoria.orden) : '');
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const titulo = categoria ? `Editar ${categoria.nombre}` : 'Nueva categoría';

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setErrorGeneral(null);
    const valor = leerOrden(orden);
    if (valor === null) {
      setErrores({ orden: [MENSAJE_ORDEN] });
      return;
    }
    setErrores({});
    setGuardando(true);
    const datos = { nombre: nombre.trim(), nombreCorto: nombreCorto.trim() === '' ? null : nombreCorto.trim(), orden: valor };
    try {
      const guardada = categoria
        ? await editarCategoria(token, slugRestaurante, categoria.id, datos)
        : await crearCategoria(token, slugRestaurante, datos);
      onGuardada(guardada);
    } catch (e) {
      const hayCampoConocido =
        e instanceof ErrorApi && Object.keys(e.errores).some((campo) => CAMPOS_CONOCIDOS.includes(campo));
      if (e instanceof ErrorApi && (e.estado === 400 || e.estado === 409) && hayCampoConocido) {
        setErrores(e.errores);
      } else {
        const alternativo = e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo guardar. Inténtalo de nuevo.';
        setErrorGeneral(mensajeDeError(e, alternativo));
      }
    } finally {
      setGuardando(false);
    }
  }

  return (
    <form className={estilos.formulario} aria-label={titulo} onSubmit={guardar} noValidate>
      <h2 className={estilos.titulo}>{titulo}</h2>
      {errorGeneral && (
        <p role="alert" className={estilos.alerta}>
          {errorGeneral}
        </p>
      )}
      <div className={estilos.campos}>
        <CampoFormulario etiqueta="Nombre" errores={errores.nombre} value={nombre} onChange={(e) => setNombre(e.target.value)} />
        <CampoFormulario
          etiqueta="Nombre corto (opcional)"
          errores={errores.nombreCorto}
          value={nombreCorto}
          onChange={(e) => setNombreCorto(e.target.value)}
        />
        <CampoFormulario
          etiqueta="Orden"
          errores={errores.orden}
          inputMode="numeric"
          value={orden}
          onChange={(e) => setOrden(e.target.value)}
        />
      </div>
      <VistaPreviaWhatsApp nombre={nombre} nombreCorto={nombreCorto} />
      <div className={estilos.acciones}>
        <button type="button" className={estilos.secundario} onClick={onCancelar}>
          Cancelar
        </button>
        <button type="submit" className={estilos.principal} disabled={guardando}>
          Guardar
        </button>
      </div>
    </form>
  );
}
