import { useQuery } from '@tanstack/react-query';
import { Outlet, useParams } from 'react-router';
import { ErrorApi } from '../api/cliente';
import { ProveedorTema } from '../tema/ProveedorTema';
import { obtenerIdentidad } from './api';
import estilos from './DisenoRestaurante.module.css';
import { RestauranteNoDisponible } from './RestauranteNoDisponible';

const esNoDisponible = (error: unknown) => error instanceof ErrorApi && error.estado === 404;

/** Carga la identidad del restaurante de la ruta y aplica su marca a las pantallas hijas. */
export function DisenoRestaurante() {
  const { slug = '' } = useParams();
  const consulta = useQuery({
    queryKey: ['identidad', slug],
    queryFn: () => obtenerIdentidad(slug),
    // Un 404 es definitivo: no se reintenta.
    retry: (intentos, error) => !esNoDisponible(error) && intentos < 2,
  });

  if (consulta.isPending) {
    return <p className={estilos.cargando}>Cargando…</p>;
  }

  if (consulta.isError) {
    if (esNoDisponible(consulta.error)) {
      return <RestauranteNoDisponible />;
    }
    return (
      <p role="alert" className={estilos.cargando}>
        No se pudo cargar el restaurante. Inténtalo de nuevo más tarde.
      </p>
    );
  }

  return (
    <ProveedorTema paleta={consulta.data.paleta}>
      <Outlet context={consulta.data} />
    </ProveedorTema>
  );
}
