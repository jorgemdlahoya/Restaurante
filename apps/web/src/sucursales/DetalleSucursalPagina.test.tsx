import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Link, MemoryRouter, Outlet, Route, Routes } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Identidad } from '../restaurante/api';
import { borrarSesion, guardarSesion, leerSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import type { HorarioDia, Sucursal } from './api';
import { DetalleSucursalPagina } from './DetalleSucursalPagina';

const DIAS = ['lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado', 'domingo'];
const semana = (apertura: string, cierre: string): HorarioDia[] =>
  DIAS.map((dia) => ({ dia, cerrado: false, apertura, cierre }));

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

const identidad = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null } as unknown as Identidad;

const respuesta = (cuerpo: unknown) =>
  new Response(JSON.stringify(cuerpo), { status: 200, headers: { 'Content-Type': 'application/json' } });

/** API simulada con estado: el PUT del horario modifica lo que devuelven los GET siguientes. */
function simularApi(sucursales: Sucursal[]) {
  const horarios: Record<string, HorarioDia[]> = {
    centro: semana('09:00', '17:00'),
    norte: semana('10:00', '20:00'),
  };
  vi.stubGlobal(
    'fetch',
    vi.fn(async (entrada: RequestInfo | URL, init?: RequestInit) => {
      const url = String(entrada);
      const horario = /\/s\/([^/]+)\/horario$/.exec(url);
      if (horario) {
        if (init?.method === 'PUT') horarios[horario[1]] = JSON.parse(String(init.body)).dias;
        return respuesta(horarios[horario[1]]);
      }
      if (url.endsWith('/estado')) return respuesta({ abierta: true, mensaje: null });
      if (url.endsWith('/cierres')) return respuesta([]);
      return respuesta(sucursales);
    }),
  );
}

function montar(inicial: string) {
  const consultas = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={consultas}>
      <ProveedorSesion>
        <MemoryRouter initialEntries={[inicial]}>
          <Routes>
            <Route path="/r/la-esquina" element={<Outlet context={identidad} />}>
              <Route path="sucursales" element={<Link to="/r/la-esquina/sucursales/centro">ir al detalle</Link>} />
              <Route path="sucursales/:sucursal" element={<DetalleSucursalPagina />} />
              <Route path="inicio" element={<p>inicio</p>} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  borrarSesion();
  guardarSesion({
    token: 't',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    nombreUsuario: 'ana',
    rol: 'administrador_general',
    slug: 'la-esquina',
  });
});
afterEach(() => {
  vi.unstubAllGlobals();
  borrarSesion();
});

describe('DetalleSucursalPagina', () => {
  it('003/RF-18: tras guardar, al volver al detalle se ve el horario guardado (caché actualizada)', async () => {
    simularApi([suc('1', 'Centro', 'centro'), suc('2', 'Norte', 'norte')]);
    montar('/r/la-esquina/sucursales/centro');

    const apertura = await screen.findByLabelText('Apertura del lunes');
    expect(apertura).toHaveValue('09:00');
    fireEvent.change(apertura, { target: { value: '11:00' } });
    await userEvent.click(screen.getByRole('button', { name: 'Guardar horario' }));
    await screen.findByText('Horario guardado.');

    await userEvent.click(screen.getByRole('link', { name: 'Volver a sucursales' }));
    await userEvent.click(await screen.findByRole('link', { name: 'ir al detalle' }));

    expect(await screen.findByLabelText('Apertura del lunes')).toHaveValue('11:00');
  });

  it('003/RF-18: al cambiar de sucursal el editor se reinicia con el horario de la nueva sucursal', async () => {
    simularApi([suc('1', 'Centro', 'centro'), suc('2', 'Norte', 'norte')]);
    montar('/r/la-esquina/sucursales/norte');

    expect(await screen.findByLabelText('Apertura del lunes')).toHaveValue('10:00');
    await screen.findByRole('option', { name: 'Centro' });

    await userEvent.selectOptions(screen.getByLabelText('Sucursal'), 'centro');
    await waitFor(() => expect(screen.getByLabelText('Apertura del lunes')).toHaveValue('09:00'));

    // Norte ya está en caché: el editor debe mostrar sus filas, no las de Centro.
    await userEvent.selectOptions(screen.getByLabelText('Sucursal'), 'norte');
    await waitFor(() => expect(screen.getByLabelText('Apertura del lunes')).toHaveValue('10:00'));
  });

  it('003/RF-19: abrir el detalle de una sucursal activa la marca como activa', async () => {
    simularApi([suc('1', 'Centro', 'centro'), suc('2', 'Norte', 'norte')]);
    montar('/r/la-esquina/sucursales/norte');

    await waitFor(() => expect(screen.getByLabelText('Sucursal')).toHaveValue('norte'));
    expect(leerSesion()?.sucursalActiva).toBe('norte');
  });
});
