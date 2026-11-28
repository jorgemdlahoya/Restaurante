import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion, guardarSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { PlatillosPagina } from './PlatillosPagina';

const IDENTIDAD = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };

function montar() {
  const enrutador = createMemoryRouter(
    [
      {
        path: '/r/:slug',
        element: <Outlet context={IDENTIDAD} />,
        children: [{ path: 'menu/categorias/:categoriaId/platillos', element: <PlatillosPagina /> }],
      },
    ],
    { initialEntries: ['/r/la-esquina/menu/categorias/c1/platillos'] },
  );
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <ProveedorSesion>
        <RouterProvider router={enrutador} />
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

/** Categoría c1 con 21 platillos: la página 1 tiene 20 y la 2 uno. */
function simularApi() {
  const todos = Array.from({ length: 21 }, (_, i) => ({
    id: `p${i + 1}`,
    categoriaId: 'c1',
    categoriaNombre: 'Pozoles',
    nombre: `Platillo ${String(i + 1).padStart(2, '0')}`,
    nombreCorto: null,
    descripcion: null,
    orden: i + 1,
    ingredientes: [],
    activo: true,
  }));
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
    const url = new URL(String(entrada), 'http://localhost');
    let status = 404;
    let cuerpo: unknown = { status: 404, title: 'No encontrado' };
    if (url.pathname.endsWith('/categorias/c1')) {
      status = 200;
      cuerpo = { id: 'c1', nombre: 'Pozoles', nombreCorto: null, orden: 1, activa: true };
    } else if (url.pathname.endsWith('/categorias/c1/platillos')) {
      const pagina = Number(url.searchParams.get('pagina'));
      status = 200;
      cuerpo = { elementos: todos.slice((pagina - 1) * 20, pagina * 20), total: todos.length, pagina, tamano: 20 };
    } else if (url.pathname.endsWith('/sucursales')) {
      status = 200;
      cuerpo = [];
    }
    return new Response(JSON.stringify(cuerpo), { status, headers: { 'Content-Type': 'application/json' } });
  });
}

describe('PlatillosPagina', () => {
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

  it('006/RF-13: Anterior y Siguiente se deshabilitan en los extremos', async () => {
    montar();

    expect(await screen.findByRole('heading', { level: 1, name: 'Platillos de Pozoles' })).toBeInTheDocument();
    expect(await screen.findAllByRole('cell', { name: 'Platillo 01' })).not.toHaveLength(0);
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Siguiente' })).toBeEnabled();

    await userEvent.click(screen.getByRole('button', { name: 'Siguiente' }));

    expect(await screen.findAllByRole('cell', { name: 'Platillo 21' })).not.toHaveLength(0);
    expect(screen.getByRole('button', { name: 'Siguiente' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeEnabled();
  });

  it('006/RF-14: si fallan las categorías activas, Editar muestra un aviso', async () => {
    montar();

    await userEvent.click((await screen.findAllByRole('button', { name: 'Editar' }))[0]);

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudieron cargar las categorías.');
    expect(screen.queryByRole('form', { name: /^Editar / })).not.toBeInTheDocument();
  });
});
