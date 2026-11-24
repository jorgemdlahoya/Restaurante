import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { HorarioDia } from './api';
import { EditorHorario } from './EditorHorario';

const DIAS = ['lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado', 'domingo'];
const semana = (): HorarioDia[] => DIAS.map((dia) => ({ dia, cerrado: true, apertura: null, cierre: null }));

describe('EditorHorario', () => {
  it('003/RF-18: marcar Cerrado deshabilita las horas y un cierre menor avisa del turno nocturno', async () => {
    render(<EditorHorario horario={semana()} onGuardar={vi.fn(async () => {})} />);

    expect(screen.getByLabelText('Apertura del lunes')).toBeDisabled();
    await userEvent.click(screen.getByLabelText('Cerrado el lunes'));
    expect(screen.getByLabelText('Apertura del lunes')).toBeEnabled();

    fireEvent.change(screen.getByLabelText('Apertura del lunes'), { target: { value: '18:00' } });
    fireEvent.change(screen.getByLabelText('Cierre del lunes'), { target: { value: '02:00' } });
    expect(screen.getByText('El turno termina al día siguiente')).toBeInTheDocument();

    await userEvent.click(screen.getByLabelText('Cerrado el lunes'));
    expect(screen.getByLabelText('Apertura del lunes')).toBeDisabled();
    expect(screen.queryByText('El turno termina al día siguiente')).not.toBeInTheDocument();
  });

  it('003/RF-18: guardar envía los siete días con horas nulas en los cerrados', async () => {
    const onGuardar = vi.fn(async (_dias: HorarioDia[]) => {});
    render(<EditorHorario horario={semana()} onGuardar={onGuardar} />);

    await userEvent.click(screen.getByLabelText('Cerrado el martes'));
    fireEvent.change(screen.getByLabelText('Apertura del martes'), { target: { value: '09:00' } });
    fireEvent.change(screen.getByLabelText('Cierre del martes'), { target: { value: '17:00' } });
    await userEvent.click(screen.getByRole('button', { name: 'Guardar horario' }));

    const dias = onGuardar.mock.calls[0][0];
    expect(dias).toHaveLength(7);
    expect(dias[1]).toEqual({ dia: 'martes', cerrado: false, apertura: '09:00', cierre: '17:00' });
    expect(dias[0]).toEqual({ dia: 'lunes', cerrado: true, apertura: null, cierre: null });
  });
});
