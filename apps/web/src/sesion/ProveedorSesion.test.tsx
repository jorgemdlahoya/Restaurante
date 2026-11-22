import { act, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
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
