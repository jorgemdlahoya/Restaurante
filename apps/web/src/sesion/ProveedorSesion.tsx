import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { borrarSesion, guardarSesion, leerSesion, type Sesion } from './almacenSesion';

type ContextoSesion = {
  sesion: Sesion | null;
  iniciar(s: Sesion): void;
  cerrar(): void;
  /** Guarda la sucursal activa (slug) en la sesión; `null` la quita. */
  elegirSucursal(slug: string | null): void;
};

const Contexto = createContext<ContextoSesion | null>(null);

// setTimeout no admite retardos mayores a 2^31-1 ms.
const RETARDO_MAXIMO = 2_147_483_647;

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<Sesion | null>(() => leerSesion());

  const iniciar = useCallback((s: Sesion) => {
    guardarSesion(s);
    setSesion(s);
  }, []);

  const cerrar = useCallback(() => {
    borrarSesion();
    setSesion(null);
  }, []);

  const expiraEn = sesion?.expiraEn;
  useEffect(() => {
    if (!expiraEn) return;
    const retardo = Math.max(0, Date.parse(expiraEn) - Date.now());
    const temporizador = setTimeout(cerrar, Math.min(retardo, RETARDO_MAXIMO));
    return () => clearTimeout(temporizador);
  }, [expiraEn, cerrar]);

  const elegirSucursal = useCallback((slug: string | null) => {
    setSesion((actual) => {
      if (!actual) return actual;
      const nueva = { ...actual, sucursalActiva: slug };
      guardarSesion(nueva);
      return nueva;
    });
  }, []);

  const valor = useMemo(
    () => ({ sesion, iniciar, cerrar, elegirSucursal }),
    [sesion, iniciar, cerrar, elegirSucursal],
  );
  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>;
}

export function useSesion(): ContextoSesion {
  const valor = useContext(Contexto);
  if (!valor) throw new Error('useSesion debe usarse dentro de ProveedorSesion');
  return valor;
}
