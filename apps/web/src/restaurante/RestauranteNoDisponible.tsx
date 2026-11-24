import estilos from './RestauranteNoDisponible.module.css';

export function RestauranteNoDisponible() {
  return (
    <main className={estilos.pagina}>
      <h1 className={estilos.titulo}>Restaurante no disponible</h1>
      <p>La dirección no corresponde a un restaurante activo. Revisa el enlace o consulta con tu restaurante.</p>
    </main>
  );
}
