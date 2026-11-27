import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { EditorIngredientes } from './EditorIngredientes';

function Envoltorio({ inicial = [], alCambiar }: { inicial?: string[]; alCambiar?: (l: string[]) => void }) {
  const [lista, setLista] = useState(inicial);
  return (
    <form onSubmit={(e) => e.preventDefault()}>
      <EditorIngredientes
        ingredientes={lista}
        onCambio={(nueva) => {
          setLista(nueva);
          alCambiar?.(nueva);
        }}
      />
    </form>
  );
}

describe('EditorIngredientes', () => {
  it('006/RF-14: agrega ingredientes con el botón y con Enter y los quita', async () => {
    const alCambiar = vi.fn();
    render(<Envoltorio alCambiar={alCambiar} />);
    const campo = screen.getByLabelText('Ingredientes incluidos');
    await userEvent.type(campo, '  Maíz ');
    await userEvent.click(screen.getByRole('button', { name: 'Agregar ingrediente' }));
    expect(alCambiar).toHaveBeenLastCalledWith(['Maíz']);
    expect(campo).toHaveValue('');

    await userEvent.type(campo, 'Cerdo{Enter}');
    expect(alCambiar).toHaveBeenLastCalledWith(['Maíz', 'Cerdo']);
    expect(screen.getByText('Cerdo')).toBeInTheDocument();

    await userEvent.click(screen.getAllByRole('button', { name: /^Quitar/ })[0]);
    expect(alCambiar).toHaveBeenLastCalledWith(['Cerdo']);
    expect(screen.queryByText('Maíz')).not.toBeInTheDocument();
  });

  it('006/RF-14: un ingrediente vacío o repetido no se agrega y avisa', async () => {
    const alCambiar = vi.fn();
    render(<Envoltorio inicial={['Crema']} alCambiar={alCambiar} />);
    const campo = screen.getByLabelText('Ingredientes incluidos');
    await userEvent.type(campo, '   {Enter}');
    expect(screen.getByText('Escribe un ingrediente')).toBeInTheDocument();
    await userEvent.clear(campo);
    await userEvent.type(campo, ' crema {Enter}');
    expect(screen.getByText('Ese ingrediente ya está en la lista')).toBeInTheDocument();
    expect(alCambiar).not.toHaveBeenCalled();
  });

  it('006/RF-14: no deja pasar de 30 ingredientes', async () => {
    const treinta = Array.from({ length: 30 }, (_, i) => `Ingrediente ${i + 1}`);
    render(<Envoltorio inicial={treinta} />);
    expect(screen.getByText('No puede haber más de 30 ingredientes')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Agregar ingrediente' })).toBeDisabled();
  });

  it('006/RF-14: muestra bajo el editor los errores del servidor', () => {
    render(
      <EditorIngredientes ingredientes={['A']} onCambio={vi.fn()} errores={['El ingrediente 2 está repetido']} />,
    );
    expect(screen.getByText('El ingrediente 2 está repetido')).toBeInTheDocument();
    expect(screen.getByLabelText('Ingredientes incluidos')).toBeInvalid();
  });
});
