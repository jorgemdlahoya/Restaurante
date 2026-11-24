import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { borrarSesion, guardarSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import type { Sucursal } from './api';
import { FormularioSucursal } from './FormularioSucursal';

const centro: Sucursal = {
  id: 's1',
  nombre: 'Centro',
  direccion: 'Av. Juárez 1',
  telefono: '+525512345678',
  slug: 'centro',
  whatsApp: '+525512345679',
  phoneNumberId: null,
  prefijo: 'A',
  zonaHoraria: 'America/Tijuana',
  activa: true,
};

function montar(sucursal?: Sucursal) {
  render(
    <ProveedorSesion>
      <FormularioSucursal slugRestaurante="la-esquina" sucursal={sucursal} onGuardado={vi.fn()} onCancelar={vi.fn()} />
    </ProveedorSesion>,
  );
}

beforeEach(() => {
  guardarSesion({
    token: 't',
    expiraEn: new Date(Date.now() + 3_600_000).toISOString(),
    nombreUsuario: 'ana',
    rol: 'administrador_general',
    slug: 'la-esquina',
  });
});

afterEach(() => {
  borrarSesion();
});

describe('FormularioSucursal', () => {
  it('003/RF-17: el prefijo se escribe en mayúsculas y el slug se bloquea al editar', async () => {
    montar();
    await userEvent.type(screen.getByLabelText('Prefijo'), 'ab');
    expect(screen.getByLabelText('Prefijo')).toHaveValue('AB');
    expect(screen.getByLabelText('Slug')).toBeEnabled();
    expect(screen.getByLabelText('Zona horaria')).toHaveValue('America/Mexico_City');
  });

  it('003/RF-17: al editar muestra los datos actuales con el slug bloqueado', () => {
    montar(centro);
    expect(screen.getByRole('form', { name: 'Editar Centro' })).toBeInTheDocument();
    expect(screen.getByLabelText('Slug')).toBeDisabled();
    expect(screen.getByLabelText('Slug')).toHaveValue('centro');
    expect(screen.getByLabelText('Zona horaria')).toHaveValue('America/Tijuana');
  });
});
