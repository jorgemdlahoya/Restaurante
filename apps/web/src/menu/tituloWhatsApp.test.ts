import { describe, expect, it } from 'vitest';
import { LIMITE_TITULO_WHATSAPP, seRecorta, tituloWhatsApp } from './tituloWhatsApp';

const rep = (s: string, n: number) => s.repeat(n);

// Misma tabla que TituloWhatsAppTests.cs (RNF-02).
const casos: [string, string | null, string, boolean][] = [
  ['Pozoles tradicionales de la casa', 'Pozoles', 'Pozoles', false],
  [rep('a', 24), null, rep('a', 24), false],
  [rep('a', 25), null, rep('a', 23) + '…', true],
  ['Pozoles tradicionales de la casa', null, 'Pozoles tradicionales d…', true],
  [rep('🌮', 12), null, rep('🌮', 12), false],
  [rep('🌮', 13), null, rep('🌮', 11) + '…', true],
  [rep('a', 21) + '🌮' + 'bbb', null, rep('a', 21) + '🌮' + '…', true],
];

describe('tituloWhatsApp', () => {
  it('005/RF-11: el límite es de 24 unidades', () => {
    expect(LIMITE_TITULO_WHATSAPP).toBe(24);
  });

  it.each(casos)('005/RF-11: calcula el título en WhatsApp (%#)', (nombre, corto, titulo, recorta) => {
    expect(tituloWhatsApp(nombre, corto)).toBe(titulo);
    expect(seRecorta(nombre, corto)).toBe(recorta);
  });

  it('005/RF-11: recorta nombre y nombre corto, y un nombre corto en blanco cuenta como ausente', () => {
    expect(tituloWhatsApp('  Bebidas  ', '   ')).toBe('Bebidas');
    expect(tituloWhatsApp('Bebidas', '  Beb  ')).toBe('Beb');
    expect(tituloWhatsApp(rep('a', 24) + '   ', null)).toBe(rep('a', 24));
    expect(seRecorta(rep('a', 24) + '   ', '  ')).toBe(false);
  });
});
