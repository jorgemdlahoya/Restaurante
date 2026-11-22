import { beforeEach, describe, expect, it } from 'vitest';
import { borrarSesion, guardarSesion, leerSesion, type Sesion } from './almacenSesion';

const CLAVE = 'restaurante.sesion';

function sesion(expiraEn: string): Sesion {
  return { token: 't', expiraEn, nombreUsuario: 'ana', rol: 'cajero', slug: 'resto' };
}

describe('almacenSesion', () => {
  beforeEach(() => {
    borrarSesion();
    window.sessionStorage.clear();
    window.localStorage.clear();
  });

  it('002/RF-21: la sesión se guarda en sessionStorage y nunca en localStorage (RNF-01)', () => {
    const s = sesion(new Date(Date.now() + 3_600_000).toISOString());
    guardarSesion(s);
    expect(JSON.parse(window.sessionStorage.getItem(CLAVE)!)).toEqual(s);
    expect(window.localStorage.length).toBe(0);
    expect(leerSesion()).toEqual(s);
  });

  it('002/RF-21: una sesión expirada se descarta al leerla', () => {
    guardarSesion(sesion(new Date(Date.now() - 1000).toISOString()));
    expect(leerSesion()).toBeNull();
    expect(window.sessionStorage.getItem(CLAVE)).toBeNull();
  });
});
