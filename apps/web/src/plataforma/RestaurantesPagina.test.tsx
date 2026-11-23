import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion, guardarSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { RestaurantesPagina } from './RestaurantesPagina';

const restaurantes = [
  { id: 'r1', nombre: 'La Esquina', slug: 'la-esquina', activo: true, paleta: {} },
  { id: 'r2', nombre: 'El Puerto', slug: 'el-puerto', activo: false, paleta: {} },
];

function montar() {
  const consultas = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={consultas}>
      <ProveedorSesion>
        <MemoryRouter>
          <RestaurantesPagina />
        </MemoryRouter>
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

let espia: ReturnType<typeof vi.fn>;

beforeEach(() => {
  guardarSesion({
    token: 'token-plataforma',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    nombreUsuario: 'root',
    rol: 'administrador_plataforma',
    slug: null,
  });
  espia = vi.fn(async () =>
    new Response(JSON.stringify(restaurantes), { status: 200, headers: { 'Content-Type': 'application/json' } }),
  );
  vi.stubGlobal('fetch', espia);
});

afterEach(() => {
  vi.unstubAllGlobals();
  borrarSesion();
});

describe('RestaurantesPagina', () => {
  it('002/RF-18: Desactivar pide confirmación y Cancelar no llama a la API', async () => {
    montar();
    const filaEsquina = (await screen.findByRole('cell', { name: 'La Esquina' })).closest('tr')!;

    await userEvent.click(within(filaEsquina).getByRole('button', { name: 'Desactivar' }));

    const dialogo = screen.getByRole('dialog');
    expect(dialogo).toHaveTextContent('¿Desactivar La Esquina?');
    await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    const metodos = espia.mock.calls.map(([, init]) => (init as RequestInit | undefined)?.method ?? 'GET');
    expect(metodos.every((m) => m === 'GET')).toBe(true);
  });

  it('002/RF-18: la lista se pide con el token de la sesión de plataforma', async () => {
    montar();
    await screen.findByRole('cell', { name: 'El Puerto' });

    const [url, init] = espia.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/api/plataforma/restaurantes');
    expect(new Headers(init.headers).get('Authorization')).toBe('Bearer token-plataforma');
    expect(screen.getByRole('cell', { name: 'Inactivo' })).toBeInTheDocument();
  });
});
