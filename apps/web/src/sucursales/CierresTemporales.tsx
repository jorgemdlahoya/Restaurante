import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import { CampoFormulario } from '../componentes/CampoFormulario';
import { DialogoConfirmacion } from '../componentes/DialogoConfirmacion';
import { useSesion } from '../sesion/ProveedorSesion';
import { cancelarCierre, claveEstado, crearCierre, listarCierres, type Cierre } from './api';
import estilos from './CierresTemporales.module.css';

type Props = { slugRestaurante: string; slugSucursal: string };

const legible = (fecha: string) => fecha.replace('T', ' ');

/**
 * Cierres temporales vigentes y futuros de la sucursal. Las fechas viajan tal cual (hora local de la
 * sucursal, sin zona): la API las interpreta con la zona horaria de la sucursal.
 */
export function CierresTemporales({ slugRestaurante, slugSucursal }: Props) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const consultas = useQueryClient();
  const clave = ['cierres', slugRestaurante, slugSucursal];

  const [inicio, setInicio] = useState('');
  const [fin, setFin] = useState('');
  const [motivo, setMotivo] = useState('');
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [porCancelar, setPorCancelar] = useState<Cierre | null>(null);

  const cierres = useQuery({ queryKey: clave, queryFn: () => listarCierres(token, slugRestaurante, slugSucursal) });

  function refrescar() {
    consultas.invalidateQueries({ queryKey: clave });
    consultas.invalidateQueries({ queryKey: claveEstado(slugRestaurante, slugSucursal) });
  }

  const crear = useMutation({
    mutationFn: () =>
      crearCierre(token, slugRestaurante, slugSucursal, {
        inicio: inicio === '' ? null : inicio,
        fin,
        motivo: motivo.trim() === '' ? null : motivo.trim(),
      }),
    onSuccess: () => {
      setInicio('');
      setFin('');
      setMotivo('');
      refrescar();
    },
    onError: (e) => {
      if (e instanceof ErrorApi && e.estado === 400 && Object.keys(e.errores).length > 0) setErrores(e.errores);
      else setErrorGeneral(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo crear el cierre.');
    },
  });

  const cancelar = useMutation({
    mutationFn: (id: string) => cancelarCierre(token, slugRestaurante, slugSucursal, id),
    onSettled: refrescar,
  });

  function enviar(evento: FormEvent) {
    evento.preventDefault();
    setErrores({});
    setErrorGeneral(null);
    crear.mutate();
  }

  function confirmarCancelacion() {
    if (porCancelar) cancelar.mutate(porCancelar.id);
    setPorCancelar(null);
  }

  return (
    <section aria-label="Cierres temporales" className={estilos.seccion}>
      <h2 className={estilos.titulo}>Cierres temporales</h2>

      {cierres.data && cierres.data.length === 0 && <p>No hay cierres programados.</p>}
      {cierres.data && cierres.data.length > 0 && (
        <ul className={estilos.lista}>
          {cierres.data.map((c) => (
            <li key={c.id} className={estilos.cierre}>
              <span>
                Del {legible(c.inicio)} al {legible(c.fin)}
                {c.motivo && <> · {c.motivo}</>}
              </span>
              <button type="button" className={estilos.secundario} onClick={() => setPorCancelar(c)}>
                Cancelar cierre
              </button>
            </li>
          ))}
        </ul>
      )}
      {cancelar.isError && <p role="alert">No se pudo cancelar el cierre.</p>}

      <form className={estilos.formulario} aria-label="Nuevo cierre temporal" onSubmit={enviar} noValidate>
        {errorGeneral && (
          <p role="alert" className={estilos.alerta}>
            {errorGeneral}
          </p>
        )}
        <CampoFormulario
          etiqueta="Inicio"
          errores={errores.inicio}
          type="datetime-local"
          value={inicio}
          onChange={(e) => setInicio(e.target.value)}
        />
        <CampoFormulario
          etiqueta="Fin"
          errores={errores.fin}
          type="datetime-local"
          value={fin}
          onChange={(e) => setFin(e.target.value)}
        />
        <CampoFormulario
          etiqueta="Motivo"
          errores={errores.motivo}
          maxLength={120}
          value={motivo}
          onChange={(e) => setMotivo(e.target.value)}
        />
        <p className={estilos.ayuda}>Deja el inicio vacío para cerrar desde ahora. Las horas son las de la sucursal.</p>
        <button type="submit" className={estilos.principal} disabled={crear.isPending}>
          Cerrar temporalmente
        </button>
      </form>

      {porCancelar && (
        <DialogoConfirmacion
          titulo="¿Cancelar el cierre?"
          mensaje="La sucursal volverá a abrir según su horario."
          confirmar="Cancelar cierre"
          onConfirmar={confirmarCancelacion}
          onCancelar={() => setPorCancelar(null)}
        />
      )}
    </section>
  );
}
