import { useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import { CampoFormulario } from '../componentes/CampoFormulario';
import type { Categoria } from './api';
import { crearPlatillo, editarPlatillo, type Platillo } from './apiPlatillos';
import { EditorIngredientes } from './EditorIngredientes';
import estilos from './FormularioPlatillo.module.css';
import { mensajeDeError } from './mensajeError';
import { VistaPreviaWhatsApp } from './VistaPreviaWhatsApp';

type Props = {
  slugRestaurante: string;
  token: string;
  categoriaId: string;
  platillo?: Platillo;
  categorias?: Categoria[];
  onGuardado(platillo: Platillo): void;
  onCancelar(): void;
};

const CAMPOS_CONOCIDOS = ['nombre', 'nombreCorto', 'descripcion', 'orden', 'ingredientes', 'categoriaId'];
const MENSAJE_ORDEN = 'El orden debe ser un entero mayor o igual que 1';

function leerOrden(texto: string): number | null {
  const limpio = texto.trim();
  if (!/^\d+$/.test(limpio)) return null;
  const orden = Number(limpio);
  return Number.isSafeInteger(orden) && orden >= 1 ? orden : null;
}

const aNulo = (texto: string) => (texto.trim() === '' ? null : texto.trim());

export function FormularioPlatillo({
  slugRestaurante,
  token,
  categoriaId,
  platillo,
  categorias = [],
  onGuardado,
  onCancelar,
}: Props) {
  const [nombre, setNombre] = useState(platillo?.nombre ?? '');
  const [nombreCorto, setNombreCorto] = useState(platillo?.nombreCorto ?? '');
  const [descripcion, setDescripcion] = useState(platillo?.descripcion ?? '');
  const [orden, setOrden] = useState(platillo ? String(platillo.orden) : '');
  const [ingredientes, setIngredientes] = useState<string[]>(platillo?.ingredientes ?? []);
  const [categoriaElegida, setCategoriaElegida] = useState(platillo?.categoriaId ?? categoriaId);
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const titulo = platillo ? `Editar ${platillo.nombre}` : 'Nuevo platillo';

  const opciones = categorias.map((c) => ({ valor: c.id, etiqueta: c.nombre }));
  if (platillo && !categorias.some((c) => c.id === platillo.categoriaId)) {
    opciones.push({ valor: platillo.categoriaId, etiqueta: `${platillo.categoriaNombre} (inactiva)` });
  }

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
    const datos = {
      nombre: nombre.trim(),
      nombreCorto: aNulo(nombreCorto),
      descripcion: aNulo(descripcion),
      orden: valor,
      ingredientes,
    };
    try {
      const guardado = platillo
        ? await editarPlatillo(token, slugRestaurante, platillo.id, { ...datos, categoriaId: categoriaElegida })
        : await crearPlatillo(token, slugRestaurante, categoriaId, datos);
      onGuardado(guardado);
    } catch (e) {
      // Al crear no hay campo Categoría: su error va al aviso general.
      const camposVisibles = platillo ? CAMPOS_CONOCIDOS : CAMPOS_CONOCIDOS.filter((campo) => campo !== 'categoriaId');
      const hayCampoConocido =
        e instanceof ErrorApi && Object.keys(e.errores).some((campo) => camposVisibles.includes(campo));
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
          etiqueta="Descripción (opcional)"
          errores={errores.descripcion}
          value={descripcion}
          onChange={(e) => setDescripcion(e.target.value)}
        />
        <CampoFormulario
          etiqueta="Orden"
          errores={errores.orden}
          inputMode="numeric"
          value={orden}
          onChange={(e) => setOrden(e.target.value)}
        />
        {platillo && (
          <CampoFormulario
            etiqueta="Categoría"
            errores={errores.categoriaId}
            opciones={opciones}
            value={categoriaElegida}
            onChange={(e) => setCategoriaElegida(e.target.value)}
          />
        )}
      </div>
      <EditorIngredientes ingredientes={ingredientes} onCambio={setIngredientes} errores={errores.ingredientes} />
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
