import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const raizSrc = join(dirname(fileURLToPath(import.meta.url)), '..');
const EXCLUIDOS = ['paleta.ts', 'tema.css'];

function archivos(dir: string): string[] {
  return readdirSync(dir).flatMap((nombre) => {
    const ruta = join(dir, nombre);
    if (statSync(ruta).isDirectory()) return archivos(ruta);
    return [ruta];
  });
}

describe('sin colores literales', () => {
  it('002/RF-16: ningún componente usa colores literales (RNF-02)', () => {
    const candidatos = archivos(raizSrc).filter(
      (ruta) =>
        /\.(tsx|module\.css)$/.test(ruta) &&
        !/\.test\./.test(ruta) &&
        !EXCLUIDOS.some((e) => ruta.endsWith(`/${e}`)),
    );
    const patron = /#[0-9a-fA-F]{3,6}\b|rgba?\(|hsla?\(/;
    const ofensores = candidatos.filter((ruta) => patron.test(readFileSync(ruta, 'utf8')));
    expect(ofensores).toEqual([]);
  });
});
