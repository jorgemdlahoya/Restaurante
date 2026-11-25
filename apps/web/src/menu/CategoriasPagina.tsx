import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useOutletContext } from 'react-router';
import { ErrorApi } from '../api/cliente';
import { DialogoConfirmacion } from '../componentes/DialogoConfirmacion';
import type { Identidad } from '../restaurante/api';
import { MarcoRestaurante } from '../restaurante/MarcoRestaurante';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { useSesion } from '../sesion/ProveedorSesion';
import { cambiarEstadoCategoria, type Categoria, claveCategorias, eliminarCategoria, listarCategorias } from './api';
import estilos from './CategoriasPagina.module.css';
import { FormularioCategoria } from './FormularioCategoria';
import { tituloWhatsApp } from './tituloWhatsApp';

type Edicion = { modo: 'alta' } | { modo: 'editar'; categoria: Categoria } | null;

/** `/r/{slug}/menu/categorias`: sólo para el administrador general; otros roles vuelven al inicio. */
export function CategoriasPagina() {
  const identidad = useOutletContext<Identidad>();
  return (
    <RutaProtegida roles={['administrador_general']} slug={identidad.slug} redirigirSinRol={`/r/${identidad.slug}/inicio`}>
      <MarcoRestaurante identidad={identidad}>
        <ListaCategorias slugRestaurante={identidad.slug} />
      </MarcoRestaurante>
    </RutaProtegida>
  );
}

function ListaCategorias({ slugRestaurante }: { slugRestaurante: string }) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const consultas = useQueryClient();
  const [pagina, setPagina] = useState(1);
  const [edicion, setEdicion] = useState<Edicion>(null);
  const [porEliminar, setPorEliminar] = useState<Categoria | null>(null);

  const lista = useQuery({
    queryKey: claveCategorias(slugRestaurante, pagina),
    queryFn: () => listarCategorias(token, slugRestaurante, pagina),
    placeholderData: keepPreviousData,
  });

  function refrescar() {
    return consultas.invalidateQueries({ queryKey: claveCategorias(slugRestaurante) });
  }

  function alGuardar() {
    setEdicion(null);
    refrescar();
  }

  const cambiarEstado = useMutation({
    mutationFn: ({ id, activa }: { id: string; activa: boolean }) =>
      cambiarEstadoCategoria(token, slugRestaurante, id, activa),
    onSettled: refrescar,
  });

  const eliminar = useMutation({
    mutationFn: (id: string) => eliminarCategoria(token, slugRestaurante, id),
    onSuccess: () => {
      // Si se borró la única fila de una página que no es la primera, se vuelve a la anterior.
      if (pagina > 1 && lista.data?.elementos.length === 1) setPagina((p) => p - 1);
    },
    onSettled: refrescar,
  });

  function confirmarEliminacion() {
    if (porEliminar) eliminar.mutate(porEliminar.id);
    setPorEliminar(null);
  }

  const errorAccion = cambiarEstado.error ?? eliminar.error;
  const datos = lista.data;
  const hayAnterior = pagina > 1;
  const haySiguiente = datos ? pagina * datos.tamano < datos.total : false;

  return (
    <>
      <div className={estilos.cabecera}>
        <h1 className={estilos.titulo}>Categorías</h1>
        <button type="button" className={estilos.principal} onClick={() => setEdicion({ modo: 'alta' })}>
          Nueva categoría
        </button>
      </div>

      {edicion && (
        <FormularioCategoria
          key={edicion.modo === 'editar' ? edicion.categoria.id : 'alta'}
          slugRestaurante={slugRestaurante}
          token={token}
          categoria={edicion.modo === 'editar' ? edicion.categoria : undefined}
          onGuardada={alGuardar}
          onCancelar={() => setEdicion(null)}
        />
      )}

      {lista.isPending && <p>Cargando…</p>}
      {lista.isError && <p role="alert">No se pudo cargar la lista de categorías.</p>}
      {errorAccion && <p role="alert">{mensajeDeError(errorAccion)}</p>}

      {datos && (
        <>
          <div className={estilos.tablaContenedor}>
            <table className={estilos.tabla}>
              <thead>
                <tr>
                  <th scope="col">Orden</th>
                  <th scope="col">Nombre</th>
                  <th scope="col">Título en WhatsApp</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {datos.elementos.map((c) => (
                  <tr key={c.id}>
                    <td>{c.orden}</td>
                    <td>{c.nombre}</td>
                    <td>{tituloWhatsApp(c.nombre, c.nombreCorto)}</td>
                    <td>
                      <span className={c.activa ? estilos.activa : estilos.inactiva}>{c.activa ? 'Activa' : 'Inactiva'}</span>
                    </td>
                    <td className={estilos.acciones}>
                      <button type="button" className={estilos.secundario} onClick={() => setEdicion({ modo: 'editar', categoria: c })}>
                        Editar
                      </button>
                      <button
                        type="button"
                        className={estilos.secundario}
                        onClick={() => cambiarEstado.mutate({ id: c.id, activa: !c.activa })}
                      >
                        {c.activa ? 'Desactivar' : 'Activar'}
                      </button>
                      <button type="button" className={estilos.peligro} onClick={() => setPorEliminar(c)}>
                        Eliminar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {datos.elementos.length === 0 && <p>Todavía no hay categorías.</p>}
          <nav className={estilos.paginacion} aria-label="Paginación">
            <button type="button" className={estilos.secundario} disabled={!hayAnterior} onClick={() => setPagina((p) => p - 1)}>
              Anterior
            </button>
            <span>Página {pagina}</span>
            <button type="button" className={estilos.secundario} disabled={!haySiguiente} onClick={() => setPagina((p) => p + 1)}>
              Siguiente
            </button>
          </nav>
        </>
      )}

      {porEliminar && (
        <DialogoConfirmacion
          titulo={`¿Eliminar ${porEliminar.nombre}?`}
          mensaje="No se podrá recuperar. Si sólo quieres ocultarla, desactívala."
          confirmar="Eliminar"
          onConfirmar={confirmarEliminacion}
          onCancelar={() => setPorEliminar(null)}
        />
      )}
    </>
  );
}

function mensajeDeError(error: unknown) {
  if (error instanceof ErrorApi && error.estado === 403) return 'No tienes permiso para esta acción.';
  return 'No se pudo completar la acción. Inténtalo de nuevo.';
}
