import { Link, useOutletContext } from 'react-router';
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
  return (
    <MarcoRestaurante identidad={identidad}>
      <h1 className={estilos.titulo}>Inicio</h1>
      {sesion?.rol === 'administrador_general' && (
        <nav aria-label="Secciones" className={estilos.secciones}>
          <Link className={estilos.enlace} to={`/r/${identidad.slug}/sucursales`}>
            Sucursales
          </Link>
        </nav>
      )}
    </MarcoRestaurante>
  );
}
