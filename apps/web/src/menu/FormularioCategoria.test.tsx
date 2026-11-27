import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { respuestaJson, simularFetch } from '../restaurante/pruebasRestaurante';
import type { Categoria } from './api';
import { FormularioCategoria } from './FormularioCategoria';

const categoria: Categoria = { id: 'c1', nombre: 'Bebidas', nombreCorto: 'Beb', orden: 2, activa: true };

function montar(cat?: Categoria) {
  const onGuardada = vi.fn();
  render(
    <FormularioCategoria
      slugRestaurante="la-esquina"
      token="t"
      categoria={cat}
      onGuardada={onGuardada}
      onCancelar={vi.fn()}
    />,
  );
  return onGuardada;
}

afterEach(() => vi.unstubAllGlobals());

describe('FormularioCategoria', () => {
  it('005/RF-11: la vista previa sigue lo que se escribe y avisa del recorte', async () => {
    montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Pozoles tradicionales de la casa');
    expect(screen.getByText('Así se verá en WhatsApp: Pozoles tradicionales d…')).toBeInTheDocument();
    expect(screen.getByText('Este nombre se verá recortado en WhatsApp')).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText('Nombre corto (opcional)'), 'Pozoles');
    expect(screen.getByText('Así se verá en WhatsApp: Pozoles')).toBeInTheDocument();
    expect(screen.queryByText('Este nombre se verá recortado en WhatsApp')).not.toBeInTheDocument();

    await userEvent.clear(screen.getByLabelText('Nombre corto (opcional)'));
    await userEvent.type(screen.getByLabelText('Nombre corto (opcional)'), '   ');
    expect(screen.getByText('Así se verá en WhatsApp: Pozoles tradicionales d…')).toBeInTheDocument();
    expect(screen.getByText('Este nombre se verá recortado en WhatsApp')).toBeInTheDocument();
  });

  it.each(['', '0', '-1', '1.5', 'abc'])(
    '005/RF-12: un orden inválido se marca junto al campo sin llamar a la API (%j)',
    async (orden) => {
      const espia = simularFetch(() => respuestaJson(201, {}));
      montar();
      await userEvent.type(screen.getByLabelText('Nombre'), 'Bebidas');
      if (orden) await userEvent.type(screen.getByLabelText('Orden'), orden);
      await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
      expect(await screen.findByText('El orden debe ser un entero mayor o igual que 1')).toBeInTheDocument();
      expect(screen.getByLabelText('Orden')).toBeInvalid();
      expect(espia).not.toHaveBeenCalled();
    },
  );

  it.each([
    [400, 'One or more validation errors occurred.'],
    [409, 'Conflicto'],
  ])('005/RF-12: los errores %i del servidor se muestran junto a su campo', async (estado, titulo) => {
    simularFetch(() =>
      respuestaJson(estado, {
        title: titulo,
        errors: {
          nombre: ['Ya existe una categoría con ese nombre'],
          nombreCorto: ['Ya existe una categoría con ese nombre corto'],
        },
      }),
    );
    const onGuardada = montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Bebidas');
    await userEvent.type(screen.getByLabelText('Orden'), '1');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByText('Ya existe una categoría con ese nombre')).toBeInTheDocument();
    expect(screen.getByText('Ya existe una categoría con ese nombre corto')).toBeInTheDocument();
    expect(screen.getByLabelText('Nombre')).toBeInvalid();
    expect(screen.getByLabelText('Nombre corto (opcional)')).toBeInvalid();
    expect(onGuardada).not.toHaveBeenCalled();
  });

  it('005/RF-12: un error del servidor sin campo conocido se muestra como aviso general', async () => {
    simularFetch(() =>
      respuestaJson(400, {
        title: 'One or more validation errors occurred.',
        detail: 'El cuerpo de la petición no es válido',
        errors: { cuerpo: ['El cuerpo no es válido'] },
      }),
    );
    const onGuardada = montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Bebidas');
    await userEvent.type(screen.getByLabelText('Orden'), '1');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('El cuerpo de la petición no es válido');
    expect(onGuardada).not.toHaveBeenCalled();
  });

  it('005/RF-12: un 403 al guardar muestra que no hay permiso', async () => {
    simularFetch(() => respuestaJson(403, { title: 'Acceso denegado', detail: 'Otro texto' }));
    montar();
    await userEvent.type(screen.getByLabelText('Nombre'), 'Bebidas');
    await userEvent.type(screen.getByLabelText('Orden'), '1');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(/^No tienes permiso para esta acción\.$/);
  });

  it('005/RF-12: en edición envía PUT con los datos y nombre corto en blanco como null', async () => {
    const espia = simularFetch(() => respuestaJson(200, { ...categoria, nombreCorto: null }));
    const onGuardada = montar(categoria);
    expect(screen.getByLabelText('Nombre')).toHaveValue('Bebidas');
    expect(screen.getByLabelText('Orden')).toHaveValue('2');
    await userEvent.clear(screen.getByLabelText('Nombre corto (opcional)'));
    await userEvent.type(screen.getByLabelText('Nombre corto (opcional)'), '   ');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    await vi.waitFor(() => expect(onGuardada).toHaveBeenCalled());
    const [url, init] = espia.mock.calls[0];
    expect(String(url)).toBe('/api/r/la-esquina/categorias/c1');
    expect(init?.method).toBe('PUT');
    expect(JSON.parse(String(init?.body))).toEqual({ nombre: 'Bebidas', nombreCorto: null, orden: 2 });
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer t');
  });

  it('005/RF-12: en alta envía POST con el nombre recortado', async () => {
    const espia = simularFetch(() => respuestaJson(201, { ...categoria, id: 'c9' }));
    const onGuardada = montar();
    await userEvent.type(screen.getByLabelText('Nombre'), '  Postres ');
    await userEvent.type(screen.getByLabelText('Orden'), '3');
    await userEvent.click(screen.getByRole('button', { name: 'Guardar' }));
    await vi.waitFor(() => expect(onGuardada).toHaveBeenCalled());
    const [url, init] = espia.mock.calls[0];
    expect(String(url)).toBe('/api/r/la-esquina/categorias');
    expect(init?.method).toBe('POST');
    expect(JSON.parse(String(init?.body))).toEqual({ nombre: 'Postres', nombreCorto: null, orden: 3 });
  });
});
