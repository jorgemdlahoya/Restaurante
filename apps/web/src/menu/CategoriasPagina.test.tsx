import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
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
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
    const url = new URL(String(entrada), 'http://localhost');
    let cuerpo: unknown = { status: 404, title: 'No encontrado' };
    let status = 404;
    if (init?.method === 'DELETE') {
      status = 403;
      cuerpo = { status: 403, title: 'Acceso denegado' };
    } else if (url.pathname.endsWith('/categorias')) {
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

  it('005/RF-13: un 403 al eliminar muestra que no hay permiso', async () => {
    montar();

    const filaPrimera = (await screen.findAllByRole('row')).find((f) => f.textContent?.includes('Categoría 01'))!;
    await userEvent.click(within(filaPrimera).getByRole('button', { name: 'Eliminar' }));
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('No tienes permiso para esta acción.');
  });

  it('005/RF-10: un 403 al cargar la lista muestra que no hay permiso', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(
      async () =>
        new Response(JSON.stringify({ status: 403, title: 'Acceso denegado' }), {
          status: 403,
          headers: { 'Content-Type': 'application/json' },
        }),
    );
    montar();

    expect(await screen.findByRole('alert')).toHaveTextContent(/^No tienes permiso para esta acción\.$/);
  });

  it('006/RF-17: Cancelar cierra el aviso sin cambiar la categoría', async () => {
    const peticiones: string[] = [];
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = new URL(String(entrada), 'http://localhost');
      peticiones.push(`${init?.method ?? 'GET'} ${url.pathname}`);
      const [status, cuerpo] =
        init?.method === 'DELETE'
          ? [409, { status: 409, title: 'Conflicto', detail: 'Esta categoría tiene 2 platillos', platillos: 2 }]
          : url.pathname.endsWith('/categorias')
            ? [200, { elementos: [{ id: 'c1', nombre: 'Pozoles', nombreCorto: null, orden: 1, activa: true }], total: 1, pagina: 1, tamano: 20 }]
            : [200, []];
      return new Response(JSON.stringify(cuerpo), { status, headers: { 'Content-Type': 'application/json' } });
    });
    montar();

    const filaPozoles = (await screen.findAllByRole('row')).find((f) => f.textContent?.includes('Pozoles'))!;
    await userEvent.click(within(filaPozoles).getByRole('button', { name: 'Eliminar' }));
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Eliminar' }));

    const aviso = await screen.findByRole('dialog', { name: 'No se puede eliminar Pozoles' });
    expect(aviso).toHaveTextContent('Esta categoría tiene 2 platillos');
    await userEvent.click(within(aviso).getByRole('button', { name: 'Cancelar' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(peticiones.some((p) => p.startsWith('PUT'))).toBe(false);
  });
});
