import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion, guardarSesion, leerSesion, type Sesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import type { Sucursal } from './api';
import { SelectorSucursal } from './SelectorSucursal';

const suc = (id: string, nombre: string, slug: string, activa = true): Sucursal => ({
  id,
  nombre,
  direccion: 'Av. 1',
  telefono: '+525512345678',
  slug,
  whatsApp: '+525512345679',
  phoneNumberId: null,
  prefijo: 'A',
  zonaHoraria: 'America/Mexico_City',
  activa,
});

function sesion(extra: Partial<Sesion> = {}): Sesion {
  return {
    token: 't',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    nombreUsuario: 'ana',
    rol: 'administrador_general',
    slug: 'la-esquina',
    ...extra,
  };
}

function montar(sucursales: Sucursal[]) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async () => new Response(JSON.stringify(sucursales), { status: 200, headers: { 'Content-Type': 'application/json' } })),
  );
  const consultas = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={consultas}>
      <ProveedorSesion>
        <MemoryRouter initialEntries={['/r/la-esquina/inicio']}>
          <Routes>
            <Route path="/r/la-esquina/inicio" element={<SelectorSucursal slugRestaurante="la-esquina" />} />
            <Route path="/r/la-esquina/sucursales/:sucursal" element={<p>detalle</p>} />
          </Routes>
        </MemoryRouter>
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

beforeEach(() => borrarSesion());
afterEach(() => {
  vi.unstubAllGlobals();
  borrarSesion();
});

describe('SelectorSucursal', () => {
  it('003/RF-19: el selector sólo lista sucursales activas y recuerda la elegida', async () => {
    guardarSesion(sesion());
    montar([suc('1', 'Centro', 'centro'), suc('2', 'Norte', 'norte'), suc('3', 'Sur', 'sur', false)]);

    await screen.findByRole('option', { name: 'Centro' });
    expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual(['Elegir sucursal', 'Centro', 'Norte']);

    await userEvent.selectOptions(screen.getByLabelText('Sucursal'), 'norte');

    expect(await screen.findByText('detalle')).toBeInTheDocument();
    expect(leerSesion()?.sucursalActiva).toBe('norte');
  });

  it('003/RF-19: si la sucursal activa se desactiva, deja de ser la activa', async () => {
    guardarSesion(sesion({ sucursalActiva: 'sur' }));
    montar([suc('1', 'Centro', 'centro'), suc('3', 'Sur', 'sur', false)]);

    await screen.findByRole('option', { name: 'Centro' });
    await waitFor(() => expect(leerSesion()?.sucursalActiva ?? null).toBeNull());
    expect(screen.getByLabelText('Sucursal')).toHaveValue('');
  });
});
