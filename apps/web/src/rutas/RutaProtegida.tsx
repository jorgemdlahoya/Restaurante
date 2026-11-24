import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import type { Rol } from '../api/auth';
import { useSesion } from '../sesion/ProveedorSesion';

type Props = { roles: Rol[]; slug?: string; children: ReactNode };

export function RutaProtegida({ roles, slug, children }: Props) {
  const { sesion } = useSesion();
  const permitida = sesion !== null && sesion.slug === (slug ?? null) && roles.includes(sesion.rol);
  if (!permitida) {
    return <Navigate replace to={slug ? `/r/${slug}/login` : '/plataforma/login'} />;
  }
  return <>{children}</>;
}
