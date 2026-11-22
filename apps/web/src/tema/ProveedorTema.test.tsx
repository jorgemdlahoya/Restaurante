import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ProveedorTema } from './ProveedorTema';

describe('ProveedorTema', () => {
  it('002/RF-16: ProveedorTema escribe la paleta en variables de :root', () => {
    const { unmount } = render(
      <ProveedorTema paleta={{ principal: '#123456' }}>
        <p>hola</p>
      </ProveedorTema>,
    );
    const estilo = document.documentElement.style;
    expect(estilo.getPropertyValue('--color-principal')).toBe('#123456');
    expect(estilo.getPropertyValue('--color-fondo')).toBe('#F8F9FA');
    unmount();
    expect(estilo.getPropertyValue('--color-principal')).toBe('#F97316');
  });
});
