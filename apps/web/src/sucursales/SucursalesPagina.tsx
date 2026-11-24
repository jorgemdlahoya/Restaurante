import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Link, useOutletContext } from 'react-router';
import { DialogoConfirmacion } from '../componentes/DialogoConfirmacion';
import type { Identidad } from '../restaurante/api';
import { MarcoRestaurante } from '../restaurante/MarcoRestaurante';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { useSesion } from '../sesion/ProveedorSesion';
import { cambiarEstadoSucursal, claveSucursales, listarSucursales, type Sucursal } from './api';
import { FormularioSucursal } from './FormularioSucursal';
import estilos from './SucursalesPagina.module.css';

type Edicion = { modo: 'alta' } | { modo: 'editar'; sucursal: Sucursal } | null;

/** `/r/{slug}/sucursales`: sólo para el administrador general; otros roles vuelven al inicio. */
export function SucursalesPagina() {
  const identidad = useOutletContext<Identidad>();
  return (
    <RutaProtegida roles={['administrador_general']} slug={identidad.slug} redirigirSinRol={`/r/${identidad.slug}/inicio`}>
      <MarcoRestaurante identidad={identidad}>
        <ListaSucursales slugRestaurante={identidad.slug} />
      </MarcoRestaurante>
    </RutaProtegida>
  );
}

function ListaSucursales({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const consultas = useQueryClient();
  const clave = claveSucursales(slugRestaurante);
  const [edicion, setEdicion] = useState<Edicion>(null);
  const [porDesactivar, setPorDesactivar] = useState<Sucursal | null>(null);

  const lista = useQuery({ queryKey: clave, queryFn: () => listarSucursales(token, slugRestaurante) });
  const cambiarEstado = useMutation({
    mutationFn: ({ id, activa }: { id: string; activa: boolean }) =>
      cambiarEstadoSucursal(token, slugRestaurante, id, activa),
    onSettled: () => consultas.invalidateQueries({ queryKey: clave }),
  });

  function cerrarFormulario() {
    setEdicion(null);
    consultas.invalidateQueries({ queryKey: clave });
  }

  function confirmarDesactivacion() {
    if (porDesactivar) cambiarEstado.mutate({ id: porDesactivar.id, activa: false });
    setPorDesactivar(null);
  }

  return (
    <>
      <div className={estilos.cabecera}>
        <h1 className={estilos.titulo}>Sucursales</h1>
        <button type="button" className={estilos.principal} onClick={() => setEdicion({ modo: 'alta' })}>
          Nueva sucursal
        </button>
      </div>

      {edicion && (
        <FormularioSucursal
          key={edicion.modo === 'editar' ? edicion.sucursal.id : 'alta'}
          slugRestaurante={slugRestaurante}
          sucursal={edicion.modo === 'editar' ? edicion.sucursal : undefined}
          onGuardado={cerrarFormulario}
          onCancelar={() => setEdicion(null)}
        />
      )}

      {lista.isPending && <p>Cargando…</p>}
      {lista.isError && <p role="alert">No se pudo cargar la lista de sucursales.</p>}
      {cambiarEstado.isError && <p role="alert">No se pudo cambiar el estado de la sucursal.</p>}

      {lista.data && (
        <div className={estilos.tablaContenedor}>
          <table className={estilos.tabla}>
            <thead>
              <tr>
                <th scope="col">Nombre</th>
                <th scope="col">Dirección</th>
                <th scope="col">Estado</th>
                <th scope="col">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {lista.data.map((s) => (
                <tr key={s.id}>
                  <td>{s.nombre}</td>
                  <td>{s.direccion}</td>
                  <td>
                    <span className={s.activa ? estilos.activa : estilos.inactiva}>{s.activa ? 'Activa' : 'Inactiva'}</span>
                  </td>
                  <td className={estilos.acciones}>
                    <button type="button" className={estilos.secundario} onClick={() => setEdicion({ modo: 'editar', sucursal: s })}>
                      Editar
                    </button>
                    <Link className={estilos.secundario} to={`/r/${slugRestaurante}/sucursales/${s.slug}`}>
                      Ver horario
                    </Link>
                    {s.activa ? (
                      <button type="button" className={estilos.secundario} onClick={() => setPorDesactivar(s)}>
                        Desactivar
                      </button>
                    ) : (
                      <button
                        type="button"
                        className={estilos.secundario}
                        onClick={() => cambiarEstado.mutate({ id: s.id, activa: true })}
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

      {porDesactivar && (
        <DialogoConfirmacion
          titulo={`¿Desactivar ${porDesactivar.nombre}?`}
          mensaje="Dejará de aparecer en los canales hasta que la reactives."
          confirmar="Desactivar"
          onConfirmar={confirmarDesactivacion}
          onCancelar={() => setPorDesactivar(null)}
        />
      )}
    </>
  );
}
