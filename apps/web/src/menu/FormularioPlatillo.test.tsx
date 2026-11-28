import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { respuestaJson, simularFetch } from '../restaurante/pruebasRestaurante';
import type { Categoria } from './api';
import type { Platillo } from './apiPlatillos';
import { FormularioPlatillo } from './FormularioPlatillo';

const activas: Categoria[] = [
  { id: 'c1', nombre: 'Bebidas', nombreCorto: null, orden: 1, activa: true },
  { id: 'c2', nombre: 'Postres', nombreCorto: null, orden: 2, activa: true },
];

const platillo: Platillo = {
  id: 'p1',
  categoriaId: 'c3',
  categoriaNombre: 'Pozoles',
  nombre: 'Pozole rojo',
  nombreCorto: 'Rojo',
  descripcion: 'Con cerdo',
  orden: 2,
  ingredientes: ['Maíz', 'Cerdo'],
  activo: true,
};

function montar(opciones: { platillo?: Platillo; categorias?: Categoria[] } = {}) {
  const onGuardado = vi.fn();
  render(
    <FormularioPlatillo
      slugRestaurante="la-esquina"
      token="t"
      categoriaId="c1"
      platillo={opciones.platillo}
      categorias={opciones.categorias}
      onGuardado={onGuardado}
      onCancelar={vi.fn()}
    />,
  );
  return onGuardado;
}

afterEach(() => vi.unstubAllGlobals());

describe('FormularioPlatillo', () => {
  it('006/RF-14: la vista previa de WhatsApp sigue al nombre y al nombre corto', async () => {
    montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Pozole rojo tradicional de la casa');
    expect(screen.getByText('Así se verá en WhatsApp: Pozole rojo tradicional…')).toBeInTheDocument();
    await userEvent.type(screen.getByLabelText('Nombre corto (opcional)'), 'Rojo');
    expect(screen.getByText('Así se verá en WhatsApp: Rojo')).toBeInTheDocument();
  });

  it('006/RF-14: en edición el selector ofrece las activas y la actual inactiva', async () => {
    const espia = simularFetch(() => respuestaJson(200, platillo));
    const onGuardado = montar({ platillo, categorias: activas });
    const selector = screen.getByLabelText('Categoría');
    expect(screen.getAllByRole('option').map((o) => o.textContent)).toEqual([
      'Bebidas',
      'Postres',
      'Pozoles (inactiva)',
    ]);
    expect(selector).toHaveValue('c3');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    await vi.waitFor(() => expect(onGuardado).toHaveBeenCalled());
    const [url, init] = espia.mock.calls[0];
    expect(String(url)).toBe('/api/r/la-esquina/platillos/p1');
    expect(init?.method).toBe('PUT');
    expect(JSON.parse(String(init?.body))).toEqual({
      nombre: 'Pozole rojo',
      nombreCorto: 'Rojo',
      descripcion: 'Con cerdo',
      orden: 2,
      ingredientes: ['Maíz', 'Cerdo'],
      categoriaId: 'c3',
    });
  });

  it('006/RF-14: en edición cambiar de categoría envía la elegida', async () => {
    const espia = simularFetch(() => respuestaJson(200, platillo));
    const onGuardado = montar({ platillo: { ...platillo, categoriaId: 'c1', categoriaNombre: 'Bebidas' }, categorias: activas });
    await userEvent.selectOptions(screen.getByLabelText('Categoría'), 'c2');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    await vi.waitFor(() => expect(onGuardado).toHaveBeenCalled());
    expect(JSON.parse(String(espia.mock.calls[0][1]?.body)).categoriaId).toBe('c2');
  });

  it.each(['', '0', '1.5', 'abc'])('006/RF-15: un orden inválido se marca sin llamar a la API (%j)', async (orden) => {
    const espia = simularFetch(() => respuestaJson(201, {}));
    montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Pozole');
    if (orden) await userEvent.type(screen.getByLabelText('Orden'), orden);
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByText('El orden debe ser un entero mayor o igual que 1')).toBeInTheDocument();
    expect(screen.getByLabelText('Orden')).toBeInvalid();
    expect(espia).not.toHaveBeenCalled();
  });

  it.each([
    [400, 'One or more validation errors occurred.'],
    [409, 'Conflicto'],
  ])('006/RF-15: los errores %i del servidor se muestran junto a su campo', async (estado, titulo) => {
    simularFetch(() =>
      respuestaJson(estado, {
        title: titulo,
        errors: {
          nombre: ['Ya existe un platillo con ese nombre'],
          ingredientes: ['El ingrediente 2 está repetido'],
          categoriaId: ['La categoría está inactiva'],
        },
      }),
    );
    const onGuardado = montar({ platillo, categorias: activas });
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByText('Ya existe un platillo con ese nombre')).toBeInTheDocument();
    expect(screen.getByText('El ingrediente 2 está repetido')).toBeInTheDocument();
    expect(screen.getByText('La categoría está inactiva')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toBeInvalid();
    expect(screen.getByLabelText('Ingredientes incluidos')).toBeInvalid();
    expect(screen.getByLabelText('Categoría')).toBeInvalid();
    expect(onGuardado).not.toHaveBeenCalled();
  });

  it('006/RF-15: al crear, un error en categoriaId se muestra como aviso general', async () => {
    simularFetch(() =>
      respuestaJson(400, {
        title: 'One or more validation errors occurred.',
        errors: { categoriaId: ['La categoría está inactiva'] },
      }),
    );
    montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Pozole');
    await userEvent.type(screen.getByLabelText('Orden'), '1');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByRole('alert')).toBeInTheDocument();
  });

  it('006/RF-15: un error sin campo conocido se muestra como aviso general y un 403 como falta de permiso', async () => {
    simularFetch(() =>
      respuestaJson(400, { title: 'x', detail: 'El cuerpo de la petición no es válido', errors: { cuerpo: ['x'] } }),
    );
    montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Pozole');
    await userEvent.type(screen.getByLabelText('Orden'), '1');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('El cuerpo de la petición no es válido');

    simularFetch(() => respuestaJson(403, { title: 'Acceso denegado', detail: 'Otro' }));
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    await vi.waitFor(() =>
      expect(screen.getByRole('alert')).toHaveTextContent(/^No tienes permiso para esta acción\.$/),
    );
  });

  it('006/RF-15: el alta envía descripción y nombre corto en blanco como null', async () => {
    const espia = simularFetch(() => respuestaJson(201, { ...platillo, id: 'p9' }));
    const onGuardado = montar();
    expect(screen.queryByLabelText('Categoría')).not.toBeInTheDocument();
    await userEvent.type(screen.getByLabelText('Nombre'), '  Pozole ');
    await userEvent.type(screen.getByLabelText('Nombre corto (opcional)'), '  ');
    await userEvent.type(screen.getByLabelText('Descripción (opcional)'), '   ');
    await userEvent.type(screen.getByLabelText('Orden'), '3');
    await userEvent.type(screen.getByLabelText('Ingredientes incluidos'), 'Maíz{Enter}');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    await vi.waitFor(() => expect(onGuardado).toHaveBeenCalled());
    const [url, init] = espia.mock.calls[0];
    expect(String(url)).toBe('/api/r/la-esquina/categorias/c1/platillos');
    expect(init?.method).toBe('POST');
    expect(JSON.parse(String(init?.body))).toEqual({
      nombre: 'Pozole',
      nombreCorto: null,
      descripcion: null,
      orden: 3,
      ingredientes: ['Maíz'],
    });
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer t');
  });
});
