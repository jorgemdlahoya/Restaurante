import { useQuery } from '@tanstack/react-query';
import { claveEstado, obtenerEstado } from './api';
import estilos from './EstadoSucursal.module.css';

/** "Abierta ahora" o el mensaje de cerrada que devuelve el estado público de la sucursal. */
export function EstadoSucursal({ slugRestaurante, slugSucursal }: { slugRestaurante: string; slugSucursal: string }) {
  const estado = useQuery({
    queryKey: claveEstado(slugRestaurante, slugSucursal),
    queryFn: () => obtenerEstado(slugRestaurante, slugSucursal),
  });

  let texto = 'Consultando el estado…';
  if (estado.isError) texto = 'No se pudo consultar el estado.';
  if (estado.data) texto = estado.data.abierta ? 'Abierta ahora' : (estado.data.mensaje ?? 'Cerrada');

  const clase = estado.data ? (estado.data.abierta ? estilos.abierta : estilos.cerrada) : estilos.estado;
  return (
    <p role="status" className={clase}>
      {texto}
    </p>
  );
}
