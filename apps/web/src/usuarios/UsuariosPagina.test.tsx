import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Rol } from '../api/auth';
import { borrarSesion, guardarSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { UsuariosPagina } from './UsuariosPagina';

const IDENTIDAD = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };

function montar() {
  const enrutador = createMemoryRouter(
    [
      {
        path: '/r/:slug',
        element: <Outlet context={IDENTIDAD} />,
        children: [
          { path: 'usuarios', element: <UsuariosPagina /> },
          { path: 'inicio', element: <p>Pantalla de inicio</p> },
        ],
      },
    ],
    { initialEntries: ['/r/la-esquina/usuarios'] },
  );
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <ProveedorSesion>
        <RouterProvider router={enrutador} />
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

function sesion(rol: Rol) {
  guardarSesion({
    token: 't',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    nombreUsuario: 'ana',
    rol,
    slug: 'la-esquina',
  });
}

function simularApi(usuarios: { status: number; cuerpo: unknown }) {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
    const url = String(entrada);
    const [status, cuerpo] = url.endsWith('/usuarios')
      ? [usuarios.status, usuarios.cuerpo]
      : url.endsWith('/mi-sesion')
        ? [200, { nombreUsuario: 'ana', rol: 'administrador_sucursal', sucursales: [{ slug: 'centro', nombre: 'Centro' }] }]
        : [404, { status: 404, title: 'No encontrado' }];
    return new Response(JSON.stringify(cuerpo), { status, headers: { 'Content-Type': 'application/json' } });
  });
}

beforeEach(() => sesion('administrador_sucursal'));

afterEach(() => {
  borrarSesion();
  vi.restoreAllMocks();
});

describe('UsuariosPagina', () => {
  it('004/RF-18: un 403 al cargar la lista muestra que no hay permiso', async () => {
    simularApi({ status: 403, cuerpo: { status: 403, title: 'Acceso denegado' } });
    montar();
    expect(await screen.findByRole('alert')).toHaveTextContent('No tienes permiso para esta acción.');
  });

  it('004/RF-18: muestra sucursales inactivas marcadas y sin acciones a quien no es gestionable', async () => {
    simularApi({
      status: 200,
      cuerpo: [
        {
          id: 'u3',
          nombreUsuario: 'pedro',
          rol: 'cocinero',
          activo: true,
          gestionable: false,
          sucursales: [
            { slug: 'centro', nombre: 'Centro', activa: true },
            { slug: 'sur', nombre: 'Sur', activa: false },
          ],
        },
      ],
    });
    montar();
    const fila = (await screen.findByRole('cell', { name: 'pedro' })).closest('tr')!;
    expect(fila).toHaveTextContent('Centro, Sur (inactiva)');
    expect(within(fila).queryAllByRole('button')).toHaveLength(0);
  });

  it('004/RF-22: un cajero vuelve al inicio', async () => {
    sesion('cajero');
    simularApi({ status: 200, cuerpo: [] });
    montar();
    expect(await screen.findByText('Pantalla de inicio')).toBeInTheDocument();
  });
});
