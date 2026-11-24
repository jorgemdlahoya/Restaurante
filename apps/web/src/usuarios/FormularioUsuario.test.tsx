import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Rol } from '../api/auth';
import { borrarSesion, guardarSesion } from '../sesion/almacenSesion';
import { ProveedorSesion } from '../sesion/ProveedorSesion';
import type { Usuario } from './api';
import { FormularioUsuario } from './FormularioUsuario';

const CENTRO = { slug: 'centro', nombre: 'Centro' };
const NORTE = { slug: 'norte', nombre: 'Norte' };

function montar(rolActual: Rol, opciones: { usuario?: Usuario; sucursales?: typeof CENTRO[]; onGuardado?: () => void } = {}) {
  render(
    <ProveedorSesion>
      <FormularioUsuario
        slugRestaurante="la-esquina"
        rolActual={rolActual}
        sucursalesDisponibles={opciones.sucursales ?? [CENTRO, NORTE]}
        usuario={opciones.usuario}
        onGuardado={opciones.onGuardado ?? vi.fn()}
        onCancelar={vi.fn()}
      />
    </ProveedorSesion>,
  );
}

function responder(status: number, cuerpo: unknown) {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(cuerpo), { status, headers: { 'Content-Type': 'application/json' } }),
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
  vi.restoreAllMocks();
});

describe('FormularioUsuario', () => {
  it('004/RF-18: el administrador general elige entre los cuatro roles y cualquier sucursal', () => {
    montar('administrador_general');
    const opciones = within(screen.getByLabelText('Rol')).getAllByRole('option').map((o) => o.textContent);
    expect(opciones).toEqual(['Administrador general', 'Administrador de sucursal', 'Cajero', 'Cocinero']);
    const grupo = screen.getByRole('group', { name: 'Sucursales' });
    expect(within(grupo).getByLabelText('Centro')).toBeEnabled();
    expect(within(grupo).getByLabelText('Norte')).not.toBeChecked();
  });

  it('004/RF-18: con rol administrador general no se piden sucursales y se envía una lista vacía', async () => {
    const fetch = responder(201, { id: 'u9' });
    const onGuardado = vi.fn();
    montar('administrador_general', { onGuardado });
    await userEvent.type(screen.getByLabelText('Usuario'), 'lola');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'Clave-123');
    await userEvent.selectOptions(screen.getByLabelText('Rol'), 'administrador_general');

    expect(screen.queryByRole('group', { name: 'Sucursales' })).toBeNull();
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    expect(JSON.parse(fetch.mock.calls[0][1]!.body as string)).toEqual({
      nombreUsuario: 'lola',
      contrasena: 'Clave-123',
      rol: 'administrador_general',
      sucursales: [],
    });
    expect(onGuardado).toHaveBeenCalled();
  });

  it('004/RF-19: el administrador de sucursal sólo ofrece cajero y cocinero con su sucursal fija', () => {
    montar('administrador_sucursal', { sucursales: [CENTRO] });
    const opciones = within(screen.getByLabelText('Rol')).getAllByRole('option').map((o) => o.textContent);
    expect(opciones).toEqual(['Cajero', 'Cocinero']);
    const centro = within(screen.getByRole('group', { name: 'Sucursales' })).getByLabelText('Centro');
    expect(centro).toBeChecked();
    expect(centro).toBeDisabled();
  });

  it('004/RF-18: la edición bloquea el nombre, no pide contraseña y muestra también sucursales asignadas fuera de la sesión', () => {
    montar('administrador_general', {
      sucursales: [CENTRO],
      usuario: {
        id: 'u2',
        nombreUsuario: 'maria',
        rol: 'cajero',
        activo: true,
        gestionable: true,
        sucursales: [{ slug: 'sur', nombre: 'Sur', activa: false }],
      },
    });
    expect(screen.getByRole('form', { name: 'Editar maria' })).toBeInTheDocument();
    expect(screen.getByLabelText('Usuario')).toBeDisabled();
    expect(screen.queryByLabelText('Contraseña')).toBeNull();
    expect(within(screen.getByRole('group', { name: 'Sucursales' })).getByLabelText('Sur')).toBeChecked();
  });

  it('004/RF-18: los errores 400 del servidor se muestran junto a su campo', async () => {
    responder(400, {
      status: 400,
      title: 'Datos inválidos',
      errors: { Sucursales: ['Un cajero debe tener al menos una sucursal.'], NombreUsuario: ['Ya existe.'] },
    });
    montar('administrador_general');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    expect(await screen.findByText('Un cajero debe tener al menos una sucursal.')).toBeInTheDocument();
    expect(screen.getByLabelText('Usuario')).toHaveAccessibleDescription('Ya existe.');
  });

  it('004/RF-18: un 403 muestra que no hay permiso', async () => {
    responder(403, { status: 403, title: 'Acceso denegado', detail: 'No tiene permisos para esta operación.' });
    montar('administrador_sucursal', { sucursales: [CENTRO] });
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('No tienes permiso para esta acción.');
  });
});
