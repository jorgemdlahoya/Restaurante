import { useQuery } from '@tanstack/react-query';
import { useEffect, useId } from 'react';
import { useNavigate } from 'react-router';
import { claveMiSesion, obtenerMiSesion } from '../sesion/miSesion';
import { useSesion } from '../sesion/ProveedorSesion';
import { claveSucursales, listarSucursales } from './api';
import estilos from './SelectorSucursal.module.css';

type Opcion = { slug: string; nombre: string };

/**
 * Selector de la sucursal activa en el encabezado. El administrador general elige entre todas las
 * sucursales activas y va a su detalle (feature 003). El resto del personal elige entre sus sucursales
 * activas de `mi-sesion` y se queda en el inicio; con una sola se muestra su nombre (feature 004).
 */
export function SelectorSucursal({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion } = useSesion();
  return sesion?.rol === 'administrador_general' ? (
    <SelectorGeneral slugRestaurante={slugRestaurante} />
  ) : (
    <SelectorPersonal slugRestaurante={slugRestaurante} />
  );
}

function SelectorGeneral({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion, elegirSucursal } = useSesion();
  const token = sesion?.token ?? '';
  const navegar = useNavigate();
  const sucursales = useQuery({
    queryKey: claveSucursales(slugRestaurante),
    queryFn: () => listarSucursales(token, slugRestaurante),
  });
  const activas = sucursales.data?.filter((s) => s.activa);

  useDescartarNoDisponible(activas, false);

  return (
    <Lista
      opciones={activas ?? []}
      conVacia
      onCambiar={(slug) => {
        elegirSucursal(slug);
        navegar(`/r/${slugRestaurante}/sucursales/${slug}`);
      }}
    />
  );
}

function SelectorPersonal({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion, elegirSucursal } = useSesion();
  const token = sesion?.token ?? '';
  const navegar = useNavigate();
  const miSesion = useQuery({
    queryKey: claveMiSesion(slugRestaurante),
    queryFn: () => obtenerMiSesion(token, slugRestaurante),
  });
  const opciones = miSesion.data?.sucursales;

  useDescartarNoDisponible(opciones, true);

  if (!opciones || opciones.length === 0) return null;
  if (opciones.length === 1) {
    return <span className={estilos.selector}>Sucursal: {opciones[0].nombre}</span>;
  }
  return (
    <Lista
      opciones={opciones}
      conVacia={!opciones.some((s) => s.slug === sesion?.sucursalActiva)}
      onCambiar={(slug) => {
        elegirSucursal(slug);
        navegar(`/r/${slugRestaurante}/inicio`);
      }}
    />
  );
}

/**
 * Si la sucursal guardada en la sesión ya no está entre las disponibles, la quita; con `fijarUnica`,
 * cuando sólo queda una, la deja como activa.
 */
function useDescartarNoDisponible(opciones: Opcion[] | undefined, fijarUnica: boolean) {
  const { sesion, elegirSucursal } = useSesion();
  const activa = sesion?.sucursalActiva ?? null;
  useEffect(() => {
    if (!opciones) return;
    if (fijarUnica && opciones.length === 1) {
      if (activa !== opciones[0].slug) elegirSucursal(opciones[0].slug);
      return;
    }
    if (activa !== null && !opciones.some((s) => s.slug === activa)) elegirSucursal(null);
  }, [opciones, activa, fijarUnica, elegirSucursal]);
}

function Lista({ opciones, conVacia, onCambiar }: { opciones: Opcion[]; conVacia: boolean; onCambiar(slug: string): void }) {
  const { sesion } = useSesion();
  const id = useId();
  const activa = sesion?.sucursalActiva ?? null;
  const vigente = activa !== null && opciones.some((s) => s.slug === activa);

  return (
    <div className={estilos.selector}>
      <label htmlFor={id}>Sucursal</label>
      <select id={id} value={vigente ? activa : ''} onChange={(e) => e.target.value !== '' && onCambiar(e.target.value)}>
        {conVacia && <option value="">Elegir sucursal</option>}
        {opciones.map((s) => (
          <option key={s.slug} value={s.slug}>
            {s.nombre}
          </option>
        ))}
      </select>
    </div>
  );
}
