import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useOutletContext } from 'react-router';
import type { Rol } from '../api/auth';
import { ErrorApi } from '../api/cliente';
import { DialogoConfirmacion } from '../componentes/DialogoConfirmacion';
import type { Identidad } from '../restaurante/api';
import { MarcoRestaurante } from '../restaurante/MarcoRestaurante';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { claveMiSesion, obtenerMiSesion } from '../sesion/miSesion';
import { useSesion } from '../sesion/ProveedorSesion';
import { cambiarEstadoUsuario, claveUsuarios, listarUsuarios, NOMBRES_ROL, type RolRestaurante, type Usuario } from './api';
import { DialogoContrasena } from './DialogoContrasena';
import { FormularioUsuario, MENSAJE_SIN_PERMISO } from './FormularioUsuario';
import estilos from './UsuariosPagina.module.css';

type Edicion = { modo: 'alta' } | { modo: 'editar'; usuario: Usuario } | null;

export const ROLES_GESTION_USUARIOS: Rol[] = ['administrador_general', 'administrador_sucursal'];

/** `/r/{slug}/usuarios`: para los administradores general y de sucursal; otros roles vuelven al inicio. */
export function UsuariosPagina() {
  const identidad = useOutletContext<Identidad>();
  return (
    <RutaProtegida roles={ROLES_GESTION_USUARIOS} slug={identidad.slug} redirigirSinRol={`/r/${identidad.slug}/inicio`}>
      <MarcoRestaurante identidad={identidad}>
        <ListaUsuarios slugRestaurante={identidad.slug} />
      </MarcoRestaurante>
    </RutaProtegida>
  );
}

function ListaUsuarios({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const rolActual = sesion?.rol ?? 'cajero';
  const consultas = useQueryClient();
  const clave = claveUsuarios(slugRestaurante);
  const [edicion, setEdicion] = useState<Edicion>(null);
  const [porDesactivar, setPorDesactivar] = useState<Usuario | null>(null);
  const [porContrasena, setPorContrasena] = useState<Usuario | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);

  const lista = useQuery({ queryKey: clave, queryFn: () => listarUsuarios(token, slugRestaurante) });
  const miSesion = useQuery({
    queryKey: claveMiSesion(slugRestaurante),
    queryFn: () => obtenerMiSesion(token, slugRestaurante),
  });
  const cambiarEstado = useMutation({
    mutationFn: ({ id, activo }: { id: string; activo: boolean }) => cambiarEstadoUsuario(token, slugRestaurante, id, activo),
    onSettled: () => consultas.invalidateQueries({ queryKey: clave }),
  });

  function cerrarFormulario() {
    setEdicion(null);
    consultas.invalidateQueries({ queryKey: clave });
  }

  function confirmarDesactivacion() {
    if (porDesactivar) cambiarEstado.mutate({ id: porDesactivar.id, activo: false });
    setPorDesactivar(null);
  }

  function contrasenaAsignada() {
    if (porContrasena) setAviso(`Contraseña asignada a ${porContrasena.nombreUsuario}.`);
    setPorContrasena(null);
    // Si la contraseña era la propia, la API ya rechaza el token: al recargar, la sesión vuelve al login (RF-23).
    consultas.invalidateQueries({ queryKey: clave });
  }

  const errorEstado = mensajeErrorEstado(cambiarEstado.error);
  // El formulario necesita las sucursales de `mi-sesion` (el administrador de sucursal las envía fijas).
  const sinMiSesion = !miSesion.data;

  return (
    <>
      <div className={estilos.cabecera}>
        <h1 className={estilos.titulo}>Usuarios</h1>
        <button
          type="button"
          className={estilos.principal}
          disabled={sinMiSesion}
          onClick={() => setEdicion({ modo: 'alta' })}
        >
          Nuevo usuario
        </button>
      </div>

      {aviso && (
        <p role="status" className={estilos.aviso}>
          {aviso}
        </p>
      )}

      {edicion && (
        <FormularioUsuario
          key={edicion.modo === 'editar' ? edicion.usuario.id : 'alta'}
          slugRestaurante={slugRestaurante}
          rolActual={rolActual}
          sucursalesDisponibles={miSesion.data?.sucursales ?? []}
          usuario={edicion.modo === 'editar' ? edicion.usuario : undefined}
          onGuardado={cerrarFormulario}
          onCancelar={() => setEdicion(null)}
        />
      )}

      {lista.isPending && <p>Cargando…</p>}
      {lista.isError && (
        <p role="alert" className={estilos.alerta}>
          {lista.error instanceof ErrorApi && lista.error.estado === 403
            ? MENSAJE_SIN_PERMISO
            : 'No se pudo cargar la lista de usuarios.'}
        </p>
      )}
      {cambiarEstado.isError && (
        <p role="alert" className={estilos.alerta}>
          {errorEstado}
        </p>
      )}

      {lista.data && (
        <div className={estilos.tablaContenedor}>
          <table className={estilos.tabla}>
            <thead>
              <tr>
                <th scope="col">Usuario</th>
                <th scope="col">Rol</th>
                <th scope="col">Sucursales</th>
                <th scope="col">Estado</th>
                <th scope="col">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {lista.data.map((u) => (
                <tr key={u.id}>
                  <td>{u.nombreUsuario}</td>
                  <td>{NOMBRES_ROL[u.rol as RolRestaurante] ?? u.rol}</td>
                  <td>{u.sucursales.map((s) => (s.activa ? s.nombre : `${s.nombre} (inactiva)`)).join(', ') || '—'}</td>
                  <td>
                    <span className={u.activo ? estilos.activa : estilos.inactiva}>{u.activo ? 'Activo' : 'Inactivo'}</span>
                  </td>
                  <td className={estilos.acciones}>
                    {u.gestionable && (
                      <>
                        <button
                          type="button"
                          className={estilos.secundario}
                          disabled={sinMiSesion}
                          onClick={() => setEdicion({ modo: 'editar', usuario: u })}
                        >
                          Editar
                        </button>
                        <button type="button" className={estilos.secundario} onClick={() => setPorContrasena(u)}>
                          Asignar contraseña
                        </button>
                        {u.activo ? (
                          <button type="button" className={estilos.secundario} onClick={() => setPorDesactivar(u)}>
                            Desactivar
                          </button>
                        ) : (
                          <button
                            type="button"
                            className={estilos.secundario}
                            onClick={() => cambiarEstado.mutate({ id: u.id, activo: true })}
                          >
                            Activar
                          </button>
                        )}
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {porDesactivar && (
        <DialogoConfirmacion
          titulo={`¿Desactivar a ${porDesactivar.nombreUsuario}?`}
          mensaje="No podrá iniciar sesión y su sesión abierta se cerrará."
          confirmar="Desactivar"
          onConfirmar={confirmarDesactivacion}
          onCancelar={() => setPorDesactivar(null)}
        />
      )}

      {porContrasena && (
        <DialogoContrasena
          slugRestaurante={slugRestaurante}
          usuario={porContrasena}
          onAsignada={contrasenaAsignada}
          onCancelar={() => setPorContrasena(null)}
        />
      )}
    </>
  );
}

/** Mensaje al fallar activar/desactivar: 403 sin permiso; 409 con el motivo de la API (p. ej. el propio usuario). */
function mensajeErrorEstado(error: unknown): string {
  if (error instanceof ErrorApi && error.estado === 403) return MENSAJE_SIN_PERMISO;
  if (error instanceof ErrorApi && error.estado === 409) return error.detalle ?? error.titulo;
  return 'No se pudo cambiar el estado del usuario.';
}
