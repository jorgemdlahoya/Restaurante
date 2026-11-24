import { useNavigate } from 'react-router';
import { iniciarSesionPlataforma } from '../api/auth';
import { FormularioLogin } from '../componentes/FormularioLogin';
import { useSesion } from '../sesion/ProveedorSesion';
import estilos from './LoginPlataforma.module.css';

export function LoginPlataforma() {
  const { iniciar } = useSesion();
  const navegar = useNavigate();

  async function entrar(nombreUsuario: string, contrasena: string) {
    const respuesta = await iniciarSesionPlataforma(nombreUsuario, contrasena);
    iniciar({ ...respuesta, slug: null });
    navegar('/plataforma/restaurantes', { replace: true });
  }

  return (
    <main className={estilos.pagina}>
      <div className={estilos.tarjeta}>
        <h1 className={estilos.titulo}>Plataforma</h1>
        <FormularioLogin titulo="Iniciar sesión en la plataforma" onEnviar={entrar} />
      </div>
    </main>
  );
}
