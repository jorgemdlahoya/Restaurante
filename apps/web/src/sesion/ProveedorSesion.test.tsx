import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { pedirApi, TITULO_SESION_INVALIDA } from '../api/cliente';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { borrarSesion, guardarSesion } from './almacenSesion';
import { ProveedorSesion, useSesion } from './ProveedorSesion';

describe('ProveedorSesion', () => {
  beforeEach(() => {
    borrarSesion();
    window.sessionStorage.clear();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it('002/RF-21: al expirar con la pantalla abierta vuelve al login', () => {
    vi.useFakeTimers();
    const expiraEn = new Date(Date.now() + 60_000).toISOString();
    guardarSesion({ token: 't', expiraEn, nombreUsuario: 'ana', rol: 'cajero', slug: 'b' });
    render(
      <ProveedorSesion>
        <MemoryRouter initialEntries={['/r/b/inicio']}>
          <Routes>
            <Route path="/r/:slug/login" element={<p>login restaurante</p>} />
            <Route
              path="/r/b/inicio"
              element={
                <RutaProtegida roles={['cajero']} slug="b">
                  <p>contenido restaurante</p>
                </RutaProtegida>
              }
            />
          </Routes>
        </MemoryRouter>
      </ProveedorSesion>,
    );
    expect(screen.getByText('contenido restaurante')).toBeInTheDocument();
    act(() => {
      vi.advanceTimersByTime(60_001);
    });
    expect(screen.getByText('login restaurante')).toBeInTheDocument();
  });

  it('004/RF-23: cerrar la sesión o recibir "Sesión no válida" vacía la caché de consultas', async () => {
    const consultas = new QueryClient();
    let contexto: ReturnType<typeof useSesion> | null = null;
    function Sonda() {
      contexto = useSesion();
      return null;
    }
    const sesionDePrueba = {
      token: 't',
      expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
      nombreUsuario: 'ana',
      rol: 'administrador_general' as const,
      slug: 'b',
    };
    guardarSesion(sesionDePrueba);
    render(
      <QueryClientProvider client={consultas}>
        <ProveedorSesion>
          <Sonda />
        </ProveedorSesion>
      </QueryClientProvider>,
    );

    consultas.setQueryData(['usuarios', 'b'], [{ id: 'u1' }]);
    act(() => contexto!.cerrar());
    expect(consultas.getQueryData(['usuarios', 'b'])).toBeUndefined();

    act(() => contexto!.iniciar(sesionDePrueba));
    consultas.setQueryData(['usuarios', 'b'], [{ id: 'u1' }]);
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 401, title: TITULO_SESION_INVALIDA }), {
        status: 401,
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    await act(() => pedirApi('/api/r/b/usuarios', { token: 't' }).catch(() => undefined));
    expect(consultas.getQueryData(['usuarios', 'b'])).toBeUndefined();
    expect(contexto!.sesionTerminada).toBe(true);
    vi.restoreAllMocks();
  });

  it('002/RF-21: useSesion falla con un mensaje claro fuera de ProveedorSesion', () => {
    function Sonda() {
      useSesion();
      return null;
    }
    const consola = vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => render(<Sonda />)).toThrow('useSesion debe usarse dentro de ProveedorSesion');
    consola.mockRestore();
  });
});
