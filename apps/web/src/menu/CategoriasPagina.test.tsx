import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion, guardarSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { CategoriasPagina } from './CategoriasPagina';

const IDENTIDAD = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };

function montar() {
  const enrutador = createMemoryRouter(
    [
      {
        path: '/r/:slug',
        element: <Outlet context={IDENTIDAD} />,
        children: [{ path: 'menu/categorias', element: <CategoriasPagina /> }],
      },
    ],
    { initialEntries: ['/r/la-esquina/menu/categorias'] },
  );
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <ProveedorSesion>
        <RouterProvider router={enrutador} />
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

/** 21 categorías: la página 1 tiene 20 y la 2 una. */
function simularApi() {
  const todas = Array.from({ length: 21 }, (_, i) => ({
    id: `c${i + 1}`,
    nombre: `Categoría ${String(i + 1).padStart(2, '0')}`,
    nombreCorto: null,
    orden: i + 1,
    activa: true,
  }));
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
    const url = new URL(String(entrada), 'http://localhost');
    let cuerpo: unknown = { status: 404, title: 'No encontrado' };
    let status = 404;
    if (url.pathname.endsWith('/categorias')) {
      const pagina = Number(url.searchParams.get('pagina'));
      status = 200;
      cuerpo = { elementos: todas.slice((pagina - 1) * 20, pagina * 20), total: todas.length, pagina, tamano: 20 };
    } else if (url.pathname.endsWith('/sucursales')) {
      status = 200;
      cuerpo = [];
    }
    return new Response(JSON.stringify(cuerpo), { status, headers: { 'Content-Type': 'application/json' } });
  });
}

describe('CategoriasPagina', () => {
  beforeEach(() => {
    guardarSesion({
      token: 't',
      expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
      nombreUsuario: 'ana',
      rol: 'administrador_general',
      slug: 'la-esquina',
    });
    simularApi();
  });

  afterEach(() => {
    borrarSesion();
    vi.restoreAllMocks();
  });

  it('005/RF-10: Anterior deshabilitado en la primera página y Siguiente en la última', async () => {
    montar();

    expect(await screen.findAllByRole('cell', { name: 'Categoría 01' })).not.toHaveLength(0);
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Siguiente' })).toBeEnabled();

    await userEvent.click(screen.getByRole('button', { name: 'Siguiente' }));

    expect(await screen.findAllByRole('cell', { name: 'Categoría 21' })).not.toHaveLength(0);
    expect(screen.getByRole('button', { name: 'Siguiente' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeEnabled();
  });
});
