import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion, guardarSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import { FormularioRestaurante } from './FormularioRestaurante';
import type { Restaurante } from './api';

const laEsquina: Restaurante = {
  id: 'r1',
  nombre: 'La Esquina',
  slug: 'la-esquina',
  activo: true,
  paleta: { principal: '#abcdef' },
};

function montar(restaurante?: Restaurante) {
  render(
    <ProveedorSesion>
      <FormularioRestaurante restaurante={restaurante} onGuardado={vi.fn()} onCancelar={vi.fn()} />
    </ProveedorSesion>,
  );
}

beforeEach(() => {
  guardarSesion({
    token: 't',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    nombreUsuario: 'root',
    rol: 'administrador_plataforma',
    slug: null,
  });
});

afterEach(() => {
  borrarSesion();
});

describe('FormularioRestaurante', () => {
  it('002/RF-18: la vista previa refleja el color elegido', () => {
    montar();

    fireEvent.change(screen.getByLabelText('Color principal'), { target: { value: '#123456' } });

    const vistaPrevia = screen.getByRole('region', { name: 'Vista previa' });
    expect(vistaPrevia.style.getPropertyValue('--color-principal')).toBe('#123456');
    expect(vistaPrevia.style.getPropertyValue('--color-fondo')).toBe('#f8f9fa');
  });

  it('002/RF-18: al editar, el slug no se puede cambiar y no se pide administrador', () => {
    montar(laEsquina);

    expect(screen.getByRole('form', { name: 'Editar La Esquina' })).toBeInTheDocument();
    expect(screen.getByLabelText('Slug')).toBeDisabled();
    expect(screen.getByLabelText('Slug')).toHaveValue('la-esquina');
    expect(screen.getByLabelText('Color principal')).toHaveValue('#abcdef');
    expect(screen.queryByLabelText('Usuario administrador')).not.toBeInTheDocument();
  });

  it('002/RF-18: en el alta pide el administrador inicial y el slug es editable', () => {
    montar();

    expect(screen.getByRole('form', { name: 'Nuevo restaurante' })).toBeInTheDocument();
    expect(screen.getByLabelText('Slug')).toBeEnabled();
    expect(screen.getByLabelText('Usuario administrador')).toBeInTheDocument();
    expect(screen.getByLabelText('Contraseña del administrador')).toHaveAttribute('type', 'password');
  });
});
