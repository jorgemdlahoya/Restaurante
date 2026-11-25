import { useQuery } from '@tanstack/react-query';
import { Link, useOutletContext } from 'react-router';
import { claveMiSesion, obtenerMiSesion } from '../sesion/miSesion';
import { MENSAJE_SIN_SUCURSALES } from './LoginRestaurante';
import type { Rol } from '../api/auth';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { useSesion } from '../sesion/ProveedorSesion';
import type { Identidad } from './api';
import estilos from './InicioRestaurante.module.css';
import { MarcoRestaurante } from './MarcoRestaurante';

export const ROLES_RESTAURANTE: Rol[] = ['administrador_general', 'administrador_sucursal', 'cajero', 'cocinero'];

export function InicioRestaurante() {
  const identidad = useOutletContext<Identidad>();
  return (
    <RutaProtegida roles={ROLES_RESTAURANTE} slug={identidad.slug}>
      <ContenidoInicio identidad={identidad} />
    </RutaProtegida>
  );
}

function ContenidoInicio({ identidad }: { identidad: Identidad }) {
  const { sesion } = useSesion();
  const esGeneral = sesion?.rol === 'administrador_general';
  const gestionaUsuarios = esGeneral || sesion?.rol === 'administrador_sucursal';
  const miSesion = useQuery({
    queryKey: claveMiSesion(identidad.slug),
    queryFn: () => obtenerMiSesion(sesion?.token ?? '', identidad.slug),
    enabled: !esGeneral,
  });
  const sinSucursales = !esGeneral && miSesion.data?.sucursales.length === 0;
  return (
    <MarcoRestaurante identidad={identidad}>
      <h1 className={estilos.titulo}>Inicio</h1>
      {sinSucursales && (
        <p role="status" className={estilos.aviso}>
          {MENSAJE_SIN_SUCURSALES}
        </p>
      )}
      {gestionaUsuarios && (
        <nav aria-label="Secciones" className={estilos.secciones}>
          {esGeneral && (
            <Link className={estilos.enlace} to={`/r/${identidad.slug}/sucursales`}>
              Sucursales
            </Link>
          )}
          <Link className={estilos.enlace} to={`/r/${identidad.slug}/usuarios`}>
            Usuarios
          </Link>
        </nav>
      )}
    </MarcoRestaurante>
  );
}
