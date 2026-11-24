import { urlApi } from '../api/cliente';
import type { Identidad } from './api';
import estilos from './EncabezadoMarca.module.css';

type Props = { identidad: Identidad; nombreUsuario: string; onCerrarSesion(): void };

export function EncabezadoMarca({ identidad, nombreUsuario, onCerrarSesion }: Props) {
  return (
    <header className={estilos.encabezado}>
      <div className={estilos.marca}>
        {identidad.logoUrl && (
          <img className={estilos.logo} src={urlApi(identidad.logoUrl)} alt={`Logo de ${identidad.nombre}`} />
        )}
        <span className={estilos.nombre}>{identidad.nombre}</span>
      </div>
      <div className={estilos.usuario}>
        <span>Hola, {nombreUsuario}</span>
        <button type="button" className={estilos.salir} onClick={onCerrarSesion}>
          Cerrar sesión
        </button>
      </div>
    </header>
  );
}
