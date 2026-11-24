import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import type { Rol } from '../api/auth';
import { useSesion } from '../sesion/ProveedorSesion';

type Props = {
  roles: Rol[];
  slug?: string;
  /** A dónde llevar a quien tiene sesión válida en este ámbito pero no uno de los roles permitidos. */
  redirigirSinRol?: string;
  children: ReactNode;
};

export function RutaProtegida({ roles, slug, redirigirSinRol, children }: Props) {
  const { sesion } = useSesion();
  const login = slug ? `/r/${slug}/login` : '/plataforma/login';

  if (sesion === null || sesion.slug !== (slug ?? null)) {
    return <Navigate replace to={login} />;
  }
  if (!roles.includes(sesion.rol)) {
    return <Navigate replace to={redirigirSinRol ?? login} />;
  }
  return <>{children}</>;
}
