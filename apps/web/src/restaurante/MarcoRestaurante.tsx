import type { ReactNode } from 'react';
import { useNavigate } from 'react-router';
import { useSesion } from '../sesion/ProveedorSesion';
import type { Identidad } from './api';
import { EncabezadoMarca } from './EncabezadoMarca';
import estilos from './MarcoRestaurante.module.css';

/** Pantallas autenticadas del restaurante: encabezado de marca con "Cerrar sesión" y el contenido. */
export function MarcoRestaurante({ identidad, children }: { identidad: Identidad; children: ReactNode }) {
  const { sesion, cerrar } = useSesion();
  const navegar = useNavigate();

  function cerrarSesion() {
    cerrar();
    navegar(`/r/${identidad.slug}/login`, { replace: true });
  }

  return (
    <>
      <EncabezadoMarca identidad={identidad} nombreUsuario={sesion?.nombreUsuario ?? ''} onCerrarSesion={cerrarSesion} />
      <main className={estilos.contenido}>{children}</main>
    </>
  );
}
