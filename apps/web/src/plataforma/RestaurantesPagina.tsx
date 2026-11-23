import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useSesion } from '../sesion/ProveedorSesion';
import { cambiarEstadoRestaurante, listarRestaurantes, type Restaurante } from './api';
import { DialogoConfirmacion } from './DialogoConfirmacion';
import estilos from './RestaurantesPagina.module.css';

const CLAVE = ['restaurantes'];

export function RestaurantesPagina() {
  const { sesion, cerrar } = useSesion();
  const token = sesion?.token ?? '';
  const navegar = useNavigate();
  const consultas = useQueryClient();
  const [porDesactivar, setPorDesactivar] = useState<Restaurante | null>(null);

  const lista = useQuery({ queryKey: CLAVE, queryFn: () => listarRestaurantes(token) });

  const cambiarEstado = useMutation({
    mutationFn: ({ id, activo }: { id: string; activo: boolean }) => cambiarEstadoRestaurante(token, id, activo),
    onSettled: () => consultas.invalidateQueries({ queryKey: CLAVE }),
  });

  function cerrarSesion() {
    cerrar();
    navegar('/plataforma/login', { replace: true });
  }

  function confirmarDesactivacion() {
    if (porDesactivar) cambiarEstado.mutate({ id: porDesactivar.id, activo: false });
    setPorDesactivar(null);
  }

  return (
    <div className={estilos.pagina}>
      <header className={estilos.encabezado}>
        <span className={estilos.marca}>Plataforma</span>
        <button type="button" className={estilos.secundario} onClick={cerrarSesion}>
          Cerrar sesión
        </button>
      </header>

      <main className={estilos.contenido}>
        <div className={estilos.cabecera}>
          <h1 className={estilos.titulo}>Restaurantes</h1>
          <button type="button" className={estilos.principal}>
            Nuevo restaurante
          </button>
        </div>

        {lista.isPending && <p>Cargando…</p>}
        {lista.isError && <p role="alert">No se pudo cargar la lista de restaurantes.</p>}
        {cambiarEstado.isError && <p role="alert">No se pudo cambiar el estado del restaurante.</p>}

        {lista.data && (
          <div className={estilos.tablaContenedor}>
            <table className={estilos.tabla}>
              <thead>
                <tr>
                  <th scope="col">Nombre</th>
                  <th scope="col">Slug</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {lista.data.map((r) => (
                  <tr key={r.id}>
                    <td>{r.nombre}</td>
                    <td>{r.slug}</td>
                    <td>
                      <span className={r.activo ? estilos.activo : estilos.inactivo}>
                        {r.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className={estilos.acciones}>
                      <button type="button" className={estilos.secundario}>
                        Editar
                      </button>
                      {r.activo ? (
                        <button type="button" className={estilos.secundario} onClick={() => setPorDesactivar(r)}>
                          Desactivar
                        </button>
                      ) : (
                        <button
                          type="button"
                          className={estilos.secundario}
                          onClick={() => cambiarEstado.mutate({ id: r.id, activo: true })}
                        >
                          Activar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </main>

      {porDesactivar && (
        <DialogoConfirmacion
          titulo={`¿Desactivar ${porDesactivar.nombre}?`}
          mensaje="Su personal no podrá iniciar sesión hasta que lo reactives."
          confirmar="Desactivar"
          onConfirmar={confirmarDesactivacion}
          onCancelar={() => setPorDesactivar(null)}
        />
      )}
    </div>
  );
}
