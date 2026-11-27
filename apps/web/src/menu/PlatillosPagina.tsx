import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Link, useOutletContext, useParams } from 'react-router';
import { ErrorApi } from '../api/cliente';
import type { Identidad } from '../restaurante/api';
import { MarcoRestaurante } from '../restaurante/MarcoRestaurante';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { useSesion } from '../sesion/ProveedorSesion';
import { listarCategoriasActivas, obtenerCategoria } from './api';
import { clavePlatillos, listarPlatillos, type Platillo } from './apiPlatillos';
import { FormularioPlatillo } from './FormularioPlatillo';
import { mensajeDeError } from './mensajeError';
import estilos from './PlatillosPagina.module.css';
import { tituloWhatsApp } from './tituloWhatsApp';

type Edicion = { modo: 'alta' } | { modo: 'editar'; platillo: Platillo } | null;

export const AVISO_CATEGORIA_INACTIVA = 'Esta categoría está inactiva. Sus platillos no se muestran a los clientes.';

/** `/r/{slug}/menu/categorias/{categoriaId}/platillos`: sólo para el administrador general. */
export function PlatillosPagina() {
  const identidad = useOutletContext<Identidad>();
  const { categoriaId = '' } = useParams();
  return (
    <RutaProtegida roles={['administrador_general']} slug={identidad.slug} redirigirSinRol={`/r/${identidad.slug}/inicio`}>
      <MarcoRestaurante identidad={identidad}>
        <ListaPlatillos slugRestaurante={identidad.slug} categoriaId={categoriaId} />
      </MarcoRestaurante>
    </RutaProtegida>
  );
}

function ListaPlatillos({ slugRestaurante, categoriaId }: { slugRestaurante: string; categoriaId: string }) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const consultas = useQueryClient();
  const [pagina, setPagina] = useState(1);
  const [edicion, setEdicion] = useState<Edicion>(null);
  const enlaceCategorias = `/r/${slugRestaurante}/menu/categorias`;

  const categoria = useQuery({
    queryKey: ['categoria', slugRestaurante, categoriaId],
    queryFn: () => obtenerCategoria(token, slugRestaurante, categoriaId),
  });
  const lista = useQuery({
    queryKey: clavePlatillos(slugRestaurante, categoriaId, pagina),
    queryFn: () => listarPlatillos(token, slugRestaurante, categoriaId, pagina),
    placeholderData: keepPreviousData,
    enabled: categoria.isSuccess,
  });
  // Sólo hace falta para el selector de categoría al editar.
  const activas = useQuery({
    queryKey: ['categorias-activas', slugRestaurante],
    queryFn: () => listarCategoriasActivas(token, slugRestaurante),
    enabled: edicion?.modo === 'editar',
  });

  function alGuardar() {
    setEdicion(null);
    consultas.invalidateQueries({ queryKey: clavePlatillos(slugRestaurante, categoriaId) });
  }

  if (categoria.isPending) return <p>Cargando…</p>;
  if (categoria.isError) {
    const noEncontrada = categoria.error instanceof ErrorApi && categoria.error.estado === 404;
    return (
      <>
        <p role="alert">
          {noEncontrada ? 'Categoría no encontrada' : mensajeDeError(categoria.error, 'No se pudo cargar la categoría.')}
        </p>
        <Link className={estilos.enlace} to={enlaceCategorias}>
          Categorías
        </Link>
      </>
    );
  }

  const cat = categoria.data;
  const datos = lista.data;
  const hayAnterior = pagina > 1;
  const haySiguiente = datos ? pagina * datos.tamano < datos.total : false;
  // En edición el formulario espera la lista de activas cargada para no marcar mal la actual como inactiva.
  const listoParaEditar = edicion?.modo !== 'editar' || activas.isSuccess;

  return (
    <>
      <Link className={estilos.enlace} to={enlaceCategorias}>
        Categorías
      </Link>
      <div className={estilos.cabecera}>
        <h1 className={estilos.titulo}>Platillos de {cat.nombre}</h1>
        <button
          type="button"
          className={estilos.principal}
          disabled={!cat.activa}
          onClick={() => setEdicion({ modo: 'alta' })}
        >
          Nuevo platillo
        </button>
      </div>
      {!cat.activa && (
        <p role="status" className={estilos.aviso}>
          {AVISO_CATEGORIA_INACTIVA}
        </p>
      )}

      {edicion && listoParaEditar && (
        <FormularioPlatillo
          key={edicion.modo === 'editar' ? edicion.platillo.id : 'alta'}
          slugRestaurante={slugRestaurante}
          token={token}
          categoriaId={categoriaId}
          platillo={edicion.modo === 'editar' ? edicion.platillo : undefined}
          categorias={activas.data}
          onGuardado={alGuardar}
          onCancelar={() => setEdicion(null)}
        />
      )}

      {lista.isPending && <p>Cargando…</p>}
      {lista.isError && <p role="alert">{mensajeDeError(lista.error, 'No se pudo cargar la lista de platillos.')}</p>}

      {datos && (
        <>
          <div className={estilos.tablaContenedor}>
            <table className={estilos.tabla}>
              <thead>
                <tr>
                  <th scope="col">Orden</th>
                  <th scope="col">Nombre</th>
                  <th scope="col">Título en WhatsApp</th>
                  <th scope="col">Ingredientes</th>
                  <th scope="col">Estado</th>
                  <th scope="col">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {datos.elementos.map((p) => (
                  <tr key={p.id}>
                    <td>{p.orden}</td>
                    <td>{p.nombre}</td>
                    <td>{tituloWhatsApp(p.nombre, p.nombreCorto)}</td>
                    <td>{p.ingredientes.length}</td>
                    <td>
                      <span className={p.activo ? estilos.activa : estilos.inactiva}>{p.activo ? 'Activo' : 'Inactivo'}</span>
                    </td>
                    <td className={estilos.acciones}>
                      <button type="button" className={estilos.secundario} onClick={() => setEdicion({ modo: 'editar', platillo: p })}>
                        Editar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {datos.elementos.length === 0 && <p>Todavía no hay platillos en esta categoría.</p>}
          <nav className={estilos.paginacion} aria-label="Paginación">
            <button type="button" className={estilos.secundario} disabled={!hayAnterior} onClick={() => setPagina((n) => n - 1)}>
              Anterior
            </button>
            <span>Página {pagina}</span>
            <button type="button" className={estilos.secundario} disabled={!haySiguiente} onClick={() => setPagina((n) => n + 1)}>
              Siguiente
            </button>
          </nav>
        </>
      )}
    </>
  );
}
