import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion } from '../sesion/almacenSesion';
import { montarRestaurante, respuestaJson, simularFetch } from './pruebasRestaurante';

const identidad = {
  nombre: 'La Esquina',
  slug: 'la-esquina',
  paleta: { principal: '#123456' },
  logoUrl: '/api/r/la-esquina/logo?v=abc',
};

afterEach(() => {
  vi.unstubAllGlobals();
  borrarSesion();
});

describe('LoginRestaurante', () => {
  it('002/RF-19: el login muestra nombre y logo del restaurante', async () => {
    simularFetch(() => respuestaJson(200, identidad));

    montarRestaurante('/r/la-esquina/login');

    expect(await screen.findByRole('heading', { level: 1, name: 'La Esquina' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Logo de La Esquina' })).toHaveAttribute(
      'src',
      '/api/r/la-esquina/logo?v=abc',
    );
    expect(document.documentElement.style.getPropertyValue('--color-principal')).toBe('#123456');
  });

  it('002/RF-19: el login no envía token ni a la identidad ni al iniciar sesión', async () => {
    const espia = simularFetch((url) =>
      url.endsWith('/auth/login')
        ? respuestaJson(200, {
            token: 't',
            expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
            nombreUsuario: 'ana',
            rol: 'cajero',
          })
        : url.endsWith('/mi-sesion')
          ? respuestaJson(200, { nombreUsuario: 'ana', rol: 'cajero', sucursales: [{ slug: 'centro', nombre: 'Centro' }] })
          : respuestaJson(200, identidad),
    );

    montarRestaurante('/r/la-esquina/login');
    await userEvent.type(await screen.findByLabelText('Usuario'), 'ana');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Clave-123');
    await userEvent.click(screen.getByRole('button', { name: 'Iniciar sesión' }));

    expect(await screen.findByText('Hola, ana')).toBeInTheDocument();
    // Tras iniciar sesión, `mi-sesion` (feature 004) sí va autenticada; la identidad y el login, nunca.
    const publicas = espia.mock.calls.filter(([url]) => !String(url).endsWith('/mi-sesion'));
    expect(publicas.length).toBeGreaterThan(0);
    for (const [, init] of publicas) {
      expect(new Headers(init?.headers).has('Authorization')).toBe(false);
    }
  });
});
