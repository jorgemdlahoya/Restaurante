import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it } from 'vitest';
import { borrarSesion, guardarSesion, type Sesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { RutaProtegida } from './RutaProtegida';

const futuro = () => new Date(Date.now() + 3_600_000).toISOString();

function sesion(extra: Partial<Sesion>): Sesion {
  return { token: 't', expiraEn: futuro(), nombreUsuario: 'ana', rol: 'cajero', slug: 'b', ...extra };
}

function montar(ruta: string) {
  render(
    <ProveedorSesion>
      <MemoryRouter initialEntries={[ruta]}>
        <Routes>
          <Route path="/plataforma/login" element={<p>login plataforma</p>} />
          <Route path="/r/:slug/login" element={<p>login restaurante</p>} />
          <Route
            path="/plataforma/inicio"
            element={
              <RutaProtegida roles={['administrador_plataforma']}>
                <p>contenido plataforma</p>
              </RutaProtegida>
            }
          />
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
}

describe('RutaProtegida', () => {
  beforeEach(() => {
    borrarSesion();
    window.sessionStorage.clear();
  });

  it('002/RF-21: sin sesión, una ruta de plataforma lleva a /plataforma/login', () => {
    montar('/plataforma/inicio');
    expect(screen.getByText('login plataforma')).toBeInTheDocument();
  });

  it('002/RF-21: sin sesión o con sesión de otro restaurante, /r/b/inicio lleva a /r/b/login', () => {
    montar('/r/b/inicio');
    expect(screen.getByText('login restaurante')).toBeInTheDocument();
    document.body.innerHTML = '';
    guardarSesion(sesion({ slug: 'a' }));
    montar('/r/b/inicio');
    expect(screen.getByText('login restaurante')).toBeInTheDocument();
  });

  it('002/RF-21: con el rol requerido muestra el contenido protegido', () => {
    guardarSesion(sesion({}));
    montar('/r/b/inicio');
    expect(screen.getByText('contenido restaurante')).toBeInTheDocument();
  });

  it('003/RF-20: un rol no permitido con sesión válida va a la ruta indicada', () => {
    guardarSesion(sesion({ rol: 'cajero' }));
    render(
      <ProveedorSesion>
        <MemoryRouter initialEntries={['/r/b/sucursales']}>
          <Routes>
            <Route path="/r/b/login" element={<p>login restaurante</p>} />
            <Route path="/r/b/inicio" element={<p>inicio restaurante</p>} />
            <Route
              path="/r/b/sucursales"
              element={
                <RutaProtegida roles={['administrador_general']} slug="b" redirigirSinRol="/r/b/inicio">
                  <p>sucursales</p>
                </RutaProtegida>
              }
            />
          </Routes>
        </MemoryRouter>
      </ProveedorSesion>,
    );
    expect(screen.getByText('inicio restaurante')).toBeInTheDocument();
  });
});
