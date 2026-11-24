import { useNavigate, useOutletContext } from 'react-router';
import type { Rol } from '../api/auth';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { useSesion } from '../sesion/ProveedorSesion';
import type { Identidad } from './api';
import { EncabezadoMarca } from './EncabezadoMarca';
import estilos from './InicioRestaurante.module.css';

const ROLES_RESTAURANTE: Rol[] = ['administrador_general', 'administrador_sucursal', 'cajero', 'cocinero'];

export function InicioRestaurante() {
  const identidad = useOutletContext<Identidad>();
  return (
    <RutaProtegida roles={ROLES_RESTAURANTE} slug={identidad.slug}>
      <ContenidoInicio identidad={identidad} />
    </RutaProtegida>
  );
}

function ContenidoInicio({ identidad }: { identidad: Identidad }) {
  const { sesion, cerrar } = useSesion();
  const navegar = useNavigate();

  function cerrarSesion() {
    cerrar();
    navegar(`/r/${identidad.slug}/login`, { replace: true });
  }

  return (
    <>
      <EncabezadoMarca identidad={identidad} nombreUsuario={sesion?.nombreUsuario ?? ''} onCerrarSesion={cerrarSesion} />
      <main className={estilos.contenido}>
        <h1 className={estilos.titulo}>Inicio</h1>
      </main>
    </>
  );
}
