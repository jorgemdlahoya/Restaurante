import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { rutas } from './App';
import { respuestaJson, simularFetch } from './restaurante/pruebasRestaurante';
import { ProveedorSesion } from './sesion/ProveedorSesion';

afterEach(() => {
  vi.unstubAllGlobals();
});

function montar(ruta: string) {
  const consultas = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={consultas}>
      <ProveedorSesion>
        <RouterProvider router={createMemoryRouter(rutas, { initialEntries: [ruta] })} />
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

describe('App', () => {
  it('002/RF-20: una dirección desconocida muestra Página no encontrada', async () => {
    montar('/cualquier-cosa');

    expect(await screen.findByRole('heading', { level: 1, name: 'Página no encontrada' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /plataforma/i })).toHaveAttribute('href', '/plataforma/login');
  });

  it('002/RF-20: una subruta desconocida de un restaurante muestra Página no encontrada', async () => {
    simularFetch(() => respuestaJson(200, { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null }));

    montar('/r/la-esquina/xyz');

    expect(await screen.findByRole('heading', { level: 1, name: 'Página no encontrada' })).toBeInTheDocument();
  });
});
