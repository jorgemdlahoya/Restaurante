import { Link } from 'react-router';
import estilos from './PaginaNoEncontrada.module.css';

export function PaginaNoEncontrada() {
  return (
    <main className={estilos.pagina}>
      <h1 className={estilos.titulo}>Página no encontrada</h1>
      <p>La dirección no existe. Revisa el enlace o vuelve al inicio.</p>
      <Link className={estilos.enlace} to="/plataforma/login">
        Ir al acceso de la plataforma
      </Link>
    </main>
  );
}
