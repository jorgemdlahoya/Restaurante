import { useQuery } from '@tanstack/react-query';
import { useEffect, useId } from 'react';
import { useNavigate } from 'react-router';
import { useSesion } from '../sesion/ProveedorSesion';
import { claveSucursales, listarSucursales } from './api';
import estilos from './SelectorSucursal.module.css';

/**
 * Selector de la sucursal activa del administrador general (en el encabezado). Sólo lista sucursales
 * activas; si la guardada en la sesión dejó de estar activa, la quita.
 */
export function SelectorSucursal({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion, elegirSucursal } = useSesion();
  const token = sesion?.token ?? '';
  const navegar = useNavigate();
  const id = useId();

  const sucursales = useQuery({
    queryKey: claveSucursales(slugRestaurante),
    queryFn: () => listarSucursales(token, slugRestaurante),
  });
  const activas = (sucursales.data ?? []).filter((s) => s.activa);
  const activa = sesion?.sucursalActiva ?? null;
  const vigente = activa !== null && activas.some((s) => s.slug === activa);

  useEffect(() => {
    if (sucursales.data && activa !== null && !sucursales.data.some((s) => s.activa && s.slug === activa)) {
      elegirSucursal(null);
    }
  }, [sucursales.data, activa, elegirSucursal]);

  function cambiar(slug: string) {
    if (slug === '') return;
    elegirSucursal(slug);
    navegar(`/r/${slugRestaurante}/sucursales/${slug}`);
  }

  return (
    <div className={estilos.selector}>
      <label htmlFor={id}>Sucursal</label>
      <select id={id} value={vigente ? activa : ''} onChange={(e) => cambiar(e.target.value)}>
        <option value="">Elegir sucursal</option>
        {activas.map((s) => (
          <option key={s.id} value={s.slug}>
            {s.nombre}
          </option>
        ))}
      </select>
    </div>
  );
}
