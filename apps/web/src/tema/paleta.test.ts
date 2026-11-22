import { describe, expect, it } from 'vitest';
import { resolverPaleta } from './paleta';

describe('paleta', () => {
  it('002/RF-16: resolverPaleta completa los colores que faltan con los de la constitución', () => {
    const p = resolverPaleta({ principal: '#123456', acento: null });
    expect(p.principal).toBe('#123456');
    expect(p.acento).toBe('#FF6600');
    expect(p.oscuro).toBe('#111111');
    expect(p.fondo).toBe('#F8F9FA');
    expect(p.texto).toBe('#1F2937');
    expect(resolverPaleta(null).principal).toBe('#F97316');
  });
});
