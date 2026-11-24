import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion, guardarSesion, leerSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { ElegirSucursalPagina } from './ElegirSucursalPagina';
import { SelectorSucursal } from './SelectorSucursal';

const IDENTIDAD = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };

function simularMiSesion(sucursales: { slug: string; nombre: string }[]) {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
    const ok = String(entrada).endsWith('/mi-sesion');
    return new Response(JSON.stringify(ok ? { nombreUsuario: 'ana', rol: 'cajero', sucursales } : {}), {
      status: ok ? 200 : 404,
      headers: { 'Content-Type': 'application/json' },
    });
  });
}

function montar(inicial: string) {
  const enrutador = createMemoryRouter(
    [
      {
        path: '/r/:slug',
        element: <Outlet context={IDENTIDAD} />,
        children: [
          { path: 'elegir-sucursal', element: <ElegirSucursalPagina /> },
          {
            path: 'inicio',
            element: (
              <>
                <p>Pantalla de inicio</p>
                <SelectorSucursal slugRestaurante="la-esquina" />
              </>
            ),
          },
        ],
      },
    ],
    { initialEntries: [inicial] },
  );
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <ProveedorSesion>
        <RouterProvider router={enrutador} />
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

beforeEach(() =>
  guardarSesion({
    token: 't',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    nombreUsuario: 'ana',
    rol: 'cajero',
    slug: 'la-esquina',
    sucursalActiva: null,
  }),
);

afterEach(() => {
  borrarSesion();
  vi.restoreAllMocks();
});

describe('ElegirSucursalPagina', () => {
  it('004/RF-20: elegir una sucursal la guarda en la sesión y lleva al inicio', async () => {
    simularMiSesion([
      { slug: 'centro', nombre: 'Centro' },
      { slug: 'norte', nombre: 'Norte' },
    ]);
    montar('/r/la-esquina/elegir-sucursal');

    await userEvent.click(await screen.findByRole('button', { name: 'Norte' }));

    expect(await screen.findByText('Pantalla de inicio')).toBeInTheDocument();
    expect(leerSesion()?.sucursalActiva).toBe('norte');
  });

  it('004/RF-27: sin sucursales activas muestra el aviso', async () => {
    simularMiSesion([]);
    montar('/r/la-esquina/elegir-sucursal');
    expect(await screen.findByText('No tienes sucursales activas. Habla con tu administrador.')).toBeInTheDocument();
  });
});

describe('SelectorSucursal del personal', () => {
  it('004/RF-21: con una sola sucursal muestra su nombre sin selector y la deja activa', async () => {
    simularMiSesion([{ slug: 'centro', nombre: 'Centro' }]);
    montar('/r/la-esquina/inicio');

    expect(await screen.findByText('Sucursal: Centro')).toBeInTheDocument();
    expect(screen.queryByRole('combobox')).toBeNull();
    await waitFor(() => expect(leerSesion()?.sucursalActiva).toBe('centro'));
  });

  it('004/RF-21: descarta una sucursal guardada que ya no está disponible', async () => {
    guardarSesion({ ...leerSesion()!, sucursalActiva: 'sur' });
    simularMiSesion([
      { slug: 'centro', nombre: 'Centro' },
      { slug: 'norte', nombre: 'Norte' },
    ]);
    montar('/r/la-esquina/inicio');

    const selector = await screen.findByLabelText('Sucursal');
    await waitFor(() => expect(leerSesion()?.sucursalActiva).toBeNull());
    expect(selector).toHaveValue('');
  });

  it('004/RF-21: sin sucursales no muestra nada', async () => {
    simularMiSesion([]);
    montar('/r/la-esquina/inicio');
    await screen.findByText('Pantalla de inicio');
    await waitFor(() => expect(globalThis.fetch).toHaveBeenCalled());
    expect(screen.queryByLabelText('Sucursal')).toBeNull();
    expect(screen.queryByText(/Sucursal:/)).toBeNull();
  });
});
