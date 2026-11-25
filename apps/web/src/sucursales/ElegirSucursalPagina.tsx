import { useQuery } from '@tanstack/react-query';
import { useNavigate, useOutletContext } from 'react-router';
import type { Identidad } from '../restaurante/api';
import { ROLES_RESTAURANTE } from '../restaurante/InicioRestaurante';
import { MarcoRestaurante } from '../restaurante/MarcoRestaurante';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { claveMiSesion, obtenerMiSesion } from '../sesion/miSesion';
import { useSesion } from '../sesion/ProveedorSesion';
import estilos from './ElegirSucursalPagina.module.css';

/** `/r/{slug}/elegir-sucursal`: tras iniciar sesión, quien trabaja en varias sucursales elige una (RF-20). */
export function ElegirSucursalPagina() {
  const identidad = useOutletContext<Identidad>();
  return (
    <RutaProtegida roles={ROLES_RESTAURANTE} slug={identidad.slug}>
      <MarcoRestaurante identidad={identidad}>
        <Eleccion slugRestaurante={identidad.slug} />
      </MarcoRestaurante>
    </RutaProtegida>
  );
}

function Eleccion({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion, elegirSucursal } = useSesion();
  const token = sesion?.token ?? '';
  const navegar = useNavigate();
  const miSesion = useQuery({
    queryKey: claveMiSesion(slugRestaurante),
    queryFn: () => obtenerMiSesion(token, slugRestaurante),
  });

  function elegir(slug: string) {
    elegirSucursal(slug);
    navegar(`/r/${slugRestaurante}/inicio`, { replace: true });
  }

  return (
    <>
      <h1 className={estilos.titulo}>Elige tu sucursal</h1>
      {miSesion.isPending && <p>Cargando…</p>}
      {miSesion.isError && <p role="alert">No se pudieron cargar tus sucursales.</p>}
      {miSesion.data && miSesion.data.sucursales.length === 0 && (
        <p>No tienes sucursales activas. Habla con tu administrador.</p>
      )}
      {miSesion.data && (
        <ul className={estilos.lista}>
          {miSesion.data.sucursales.map((s) => (
            <li key={s.slug}>
              <button type="button" className={estilos.opcion} onClick={() => elegir(s.slug)}>
                {s.nombre}
              </button>
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
