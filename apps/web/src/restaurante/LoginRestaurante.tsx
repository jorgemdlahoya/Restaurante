import { useQueryClient } from '@tanstack/react-query';
import { useNavigate, useOutletContext } from 'react-router';
import { iniciarSesionRestaurante } from '../api/auth';
import { ErrorApi, urlApi } from '../api/cliente';
import { FormularioLogin } from '../componentes/FormularioLogin';
import { claveMiSesion, obtenerMiSesion } from '../sesion/miSesion';
import { useSesion } from '../sesion/ProveedorSesion';
import type { Identidad } from './api';
import estilos from './LoginRestaurante.module.css';

export const MENSAJE_SIN_SUCURSALES = 'No tienes sucursales activas. Habla con tu administrador.';
export const MENSAJE_SESION_TERMINADA = 'Tu sesión terminó. Vuelve a iniciar sesión.';

export function LoginRestaurante() {
  const identidad = useOutletContext<Identidad>();
  const { iniciar, elegirSucursal, sesionTerminada } = useSesion();
  const consultas = useQueryClient();
  const navegar = useNavigate();
  const slug = identidad.slug;

  async function entrar(nombreUsuario: string, contrasena: string) {
    let respuesta;
    try {
      respuesta = await iniciarSesionRestaurante(slug, nombreUsuario, contrasena);
    } catch (e) {
      if (e instanceof ErrorApi && e.estado === 403 && e.titulo === 'Sin sucursales activas') {
        throw new ErrorApi(403, e.titulo, MENSAJE_SIN_SUCURSALES);
      }
      throw e;
    }
    iniciar({ ...respuesta, slug, sucursalActiva: null });

    // El administrador general trabaja en todas las sucursales y elige desde el encabezado (feature 003).
    if (respuesta.rol !== 'administrador_general') {
      try {
        const miSesion = await obtenerMiSesion(respuesta.token, slug);
        consultas.setQueryData(claveMiSesion(slug), miSesion);
        if (miSesion.sucursales.length > 1) {
          navegar(`/r/${slug}/elegir-sucursal`, { replace: true });
          return;
        }
        if (miSesion.sucursales.length === 1) elegirSucursal(miSesion.sucursales[0].slug);
      } catch {
        // Sin `mi-sesion` se entra al inicio; el encabezado y el inicio lo volverán a pedir.
      }
    }
    navegar(`/r/${slug}/inicio`, { replace: true });
  }

  return (
    <main className={estilos.pagina}>
      <div className={estilos.tarjeta}>
        {identidad.logoUrl && (
          <img className={estilos.logo} src={urlApi(identidad.logoUrl)} alt={`Logo de ${identidad.nombre}`} />
        )}
        <h1 className={estilos.nombre}>{identidad.nombre}</h1>
        {sesionTerminada && (
          <p role="status" className={estilos.aviso}>
            {MENSAJE_SESION_TERMINADA}
          </p>
        )}
        <FormularioLogin titulo={`Iniciar sesión en ${identidad.nombre}`} onEnviar={entrar} />
      </div>
    </main>
  );
}
