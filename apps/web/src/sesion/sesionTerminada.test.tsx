import { act, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { alSesionInvalida, pedirApi } from '../api/cliente';
import { RutaProtegida } from '../rutas/RutaProtegida';
import { borrarSesion, guardarSesion, leerSesion } from './almacenSesion';
import { ProveedorSesion, useSesion } from './ProveedorSesion';

function responder401(titulo: string) {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockImplementation(() =>
      Promise.resolve(
        new Response(JSON.stringify({ status: 401, title: titulo, detail: 'x' }), {
          status: 401,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
      ),
    ),
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
  borrarSesion();
});

describe('pedirApi y la sesión no válida', () => {
  it('004/RF-23: un 401 "Sesión no válida" con token avisa a los suscriptores', async () => {
    const manejador = vi.fn();
    const cancelar = alSesionInvalida(manejador);
    responder401('Sesión no válida');

    await expect(pedirApi('/api/x', { token: 't' })).rejects.toMatchObject({ estado: 401, titulo: 'Sesión no válida' });
    expect(manejador).toHaveBeenCalledTimes(1);
    cancelar();
  });

  it('004/RF-23: un 401 con otro título o sin token no avisa', async () => {
    const manejador = vi.fn();
    const cancelar = alSesionInvalida(manejador);

    responder401('No autenticado');
    await pedirApi('/api/x', { token: 't' }).catch(() => undefined);
    responder401('Sesión no válida');
    await pedirApi('/api/r/b/auth/login').catch(() => undefined);

    expect(manejador).not.toHaveBeenCalled();
    cancelar();
  });

  it('004/RF-23: tras cancelar la suscripción ya no se avisa', async () => {
    const manejador = vi.fn();
    alSesionInvalida(manejador)();
    responder401('Sesión no válida');
    await pedirApi('/api/x', { token: 't' }).catch(() => undefined);
    expect(manejador).not.toHaveBeenCalled();
  });
});

describe('ProveedorSesion ante una sesión no válida', () => {
  beforeEach(() => {
    guardarSesion({
      token: 't',
      expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
      nombreUsuario: 'ana',
      rol: 'cajero',
      slug: 'b',
    });
  });

  it('004/RF-23: cierra la sesión local, marca el aviso y la ruta protegida vuelve al login', async () => {
    function Login() {
      const { sesionTerminada } = useSesion();
      return <p>{sesionTerminada ? 'login con aviso' : 'login sin aviso'}</p>;
    }
    render(
      <ProveedorSesion>
        <MemoryRouter initialEntries={['/r/b/inicio']}>
          <Routes>
            <Route path="/r/:slug/login" element={<Login />} />
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

    responder401('Sesión no válida');
    await act(async () => {
      await pedirApi('/api/x', { token: 't' }).catch(() => undefined);
    });

    expect(screen.getByText('login con aviso')).toBeInTheDocument();
    expect(leerSesion()).toBeNull();
  });
});
