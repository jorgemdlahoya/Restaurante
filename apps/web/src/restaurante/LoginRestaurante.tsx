import { useNavigate, useOutletContext } from 'react-router';
import { iniciarSesionRestaurante } from '../api/auth';
import { urlApi } from '../api/cliente';
import { FormularioLogin } from '../componentes/FormularioLogin';
import { useSesion } from '../sesion/ProveedorSesion';
import type { Identidad } from './api';
import estilos from './LoginRestaurante.module.css';

export function LoginRestaurante() {
  const identidad = useOutletContext<Identidad>();
  const { iniciar } = useSesion();
  const navegar = useNavigate();

  async function entrar(nombreUsuario: string, contrasena: string) {
    const respuesta = await iniciarSesionRestaurante(identidad.slug, nombreUsuario, contrasena);
    iniciar({ ...respuesta, slug: identidad.slug });
    navegar(`/r/${identidad.slug}/inicio`, { replace: true });
  }

  return (
    <main className={estilos.pagina}>
      <div className={estilos.tarjeta}>
        {identidad.logoUrl && (
          <img className={estilos.logo} src={urlApi(identidad.logoUrl)} alt={`Logo de ${identidad.nombre}`} />
        )}
        <h1 className={estilos.nombre}>{identidad.nombre}</h1>
        <FormularioLogin titulo={`Iniciar sesión en ${identidad.nombre}`} onEnviar={entrar} />
      </div>
    </main>
  );
}
