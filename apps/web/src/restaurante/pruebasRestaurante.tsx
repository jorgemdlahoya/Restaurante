import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { vi } from 'vitest';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { DisenoRestaurante } from './DisenoRestaurante';
import { InicioRestaurante } from './InicioRestaurante';
import { LoginRestaurante } from './LoginRestaurante';

// Ayudas compartidas por las pruebas de las pantallas del restaurante.

export function respuestaJson(estado: number, cuerpo: unknown, tipo = 'application/json') {
  return new Response(JSON.stringify(cuerpo), { status: estado, headers: { 'Content-Type': tipo } });
}

export function simularFetch(manejador: (url: string, init?: RequestInit) => Response) {
  const espia = vi.fn(async (entrada: RequestInfo | URL, init?: RequestInit) => manejador(String(entrada), init));
  vi.stubGlobal('fetch', espia);
  return espia;
}

export function montarRestaurante(ruta: string) {
  const consultas = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={consultas}>
      <ProveedorSesion>
        <MemoryRouter initialEntries={[ruta]}>
          <Routes>
            <Route path="/r/:slug" element={<DisenoRestaurante />}>
              <Route path="login" element={<LoginRestaurante />} />
              <Route path="inicio" element={<InicioRestaurante />} />
            </Route>
          </Routes>
        </MemoryRouter>
      </ProveedorSesion>
    </QueryClientProvider>,
  );
}
