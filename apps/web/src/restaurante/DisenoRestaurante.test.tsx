import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { montarRestaurante, respuestaJson, simularFetch } from './pruebasRestaurante';

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('DisenoRestaurante', () => {
  it('002/RF-20: con 404 muestra Restaurante no disponible', async () => {
    simularFetch(() =>
      respuestaJson(
        404,
        { status: 404, title: 'Restaurante no disponible', detail: 'La dirección no corresponde a un restaurante activo.' },
        'application/problem+json',
      ),
    );

    montarRestaurante('/r/no-existe/login');

    expect(await screen.findByRole('heading', { level: 1, name: 'Restaurante no disponible' })).toBeInTheDocument();
    expect(screen.queryByLabelText('Usuario')).not.toBeInTheDocument();
  });

  it('002/RF-16: sin paleta aplica los colores por defecto', async () => {
    simularFetch(() => respuestaJson(200, { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null }));

    montarRestaurante('/r/la-esquina/login');

    await screen.findByRole('heading', { level: 1, name: 'La Esquina' });
    expect(document.documentElement.style.getPropertyValue('--color-principal')).toBe('#F97316');
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });
});
