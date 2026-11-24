import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Link, useOutletContext, useParams } from 'react-router';
import { ErrorApi } from '../api/cliente';
import type { Identidad } from '../restaurante/api';
import { MarcoRestaurante } from '../restaurante/MarcoRestaurante';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { useSesion } from '../sesion/ProveedorSesion';
import { claveEstado, claveSucursales, guardarHorario, listarSucursales, obtenerHorario, type HorarioDia } from './api';
import { CierresTemporales } from './CierresTemporales';
import estilos from './DetalleSucursalPagina.module.css';
import { EditorHorario } from './EditorHorario';
import { EstadoSucursal } from './EstadoSucursal';

/** `/r/{slug}/sucursales/{sucursal}`: estado, horario semanal y cierres temporales. */
export function DetalleSucursalPagina() {
  const identidad = useOutletContext<Identidad>();
  const { sucursal = '' } = useParams();
  return (
    <RutaProtegida roles={['administrador_general']} slug={identidad.slug} redirigirSinRol={`/r/${identidad.slug}/inicio`}>
      <MarcoRestaurante identidad={identidad}>
        <Detalle slugRestaurante={identidad.slug} slugSucursal={sucursal} />
      </MarcoRestaurante>
    </RutaProtegida>
  );
}

function Detalle({ slugRestaurante, slugSucursal }: { slugRestaurante: string; slugSucursal: string }) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const consultas = useQueryClient();
  const [erroresHorario, setErroresHorario] = useState<Record<string, string[]>>({});
  const [mensajeHorario, setMensajeHorario] = useState<string | null>(null);

  const sucursales = useQuery({
    queryKey: claveSucursales(slugRestaurante),
    queryFn: () => listarSucursales(token, slugRestaurante),
  });
  const horario = useQuery({
    queryKey: ['horario', slugRestaurante, slugSucursal],
    queryFn: () => obtenerHorario(token, slugRestaurante, slugSucursal),
  });

  const nombre = sucursales.data?.find((s) => s.slug === slugSucursal)?.nombre ?? slugSucursal;

  async function guardar(dias: HorarioDia[]) {
    setErroresHorario({});
    setMensajeHorario(null);
    try {
      await guardarHorario(token, slugRestaurante, slugSucursal, dias);
      setMensajeHorario('Horario guardado.');
      consultas.invalidateQueries({ queryKey: claveEstado(slugRestaurante, slugSucursal) });
    } catch (e) {
      if (e instanceof ErrorApi && e.estado === 400) setErroresHorario(e.errores);
      else setMensajeHorario('No se pudo guardar el horario.');
    }
  }

  return (
    <>
      <Link className={estilos.volver} to={`/r/${slugRestaurante}/sucursales`}>
        Volver a sucursales
      </Link>
      <h1 className={estilos.titulo}>{nombre}</h1>
      <EstadoSucursal slugRestaurante={slugRestaurante} slugSucursal={slugSucursal} />

      <section aria-label="Horario" className={estilos.seccion}>
        <h2 className={estilos.subtitulo}>Horario</h2>
        {horario.isPending && <p>Cargando…</p>}
        {horario.isError && <p role="alert">No se pudo cargar el horario.</p>}
        {horario.data && <EditorHorario horario={horario.data} onGuardar={guardar} errores={erroresHorario} />}
        {mensajeHorario && <p className={estilos.mensaje}>{mensajeHorario}</p>}
      </section>

      <CierresTemporales slugRestaurante={slugRestaurante} slugSucursal={slugSucursal} />
    </>
  );
}
