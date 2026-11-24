import type { ReactNode } from 'react';
import { urlApi } from '../api/cliente';
import type { Identidad } from './api';
import estilos from './EncabezadoMarca.module.css';

type Props = {
  identidad: Identidad;
  nombreUsuario: string;
  onCerrarSesion(): void;
  /** Contenido opcional junto al usuario (p. ej. el selector de sucursal). */
  extra?: ReactNode;
};

export function EncabezadoMarca({ identidad, nombreUsuario, onCerrarSesion, extra }: Props) {
  return (
    <header className={estilos.encabezado}>
      <div className={estilos.marca}>
        {identidad.logoUrl && (
          <img className={estilos.logo} src={urlApi(identidad.logoUrl)} alt={`Logo de ${identidad.nombre}`} />
        )}
        <span className={estilos.nombre}>{identidad.nombre}</span>
      </div>
      <div className={estilos.usuario}>
        {extra}
        <span>Hola, {nombreUsuario}</span>
        <button type="button" className={estilos.salir} onClick={onCerrarSesion}>
          Cerrar sesión
        </button>
      </div>
    </header>
  );
}
