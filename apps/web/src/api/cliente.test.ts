import { afterEach, describe, expect, it, vi } from 'vitest';
import { iniciarSesionRestaurante } from './auth';
import { ErrorApi, pedirApi } from './cliente';

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('pedirApi', () => {
  it('002/RF-21: pedirApi lanza ErrorApi con estado y errores por campo', async () => {
    const cuerpo = { status: 400, title: 'Datos inválidos', detail: 'Revisa', errors: { nombre: ['Requerido'] } };
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify(cuerpo), {
          status: 400,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
      ),
    );
    const error = await pedirApi('/api/x').catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ErrorApi);
    const e = error as ErrorApi;
    expect(e.estado).toBe(400);
    expect(e.titulo).toBe('Datos inválidos');
    expect(e.detalle).toBe('Revisa');
    expect(e.errores).toEqual({ nombre: ['Requerido'] });
  });

  it('002/RF-21: pedirApi añade Authorization sólo si se pasa un token y devuelve undefined con 204', async () => {
    const fetchSimulado = vi.fn().mockImplementation(() => Promise.resolve(new Response(null, { status: 204 })));
    vi.stubGlobal('fetch', fetchSimulado);
    expect(await pedirApi('/api/x', { token: 'abc' })).toBeUndefined();
    await pedirApi('/api/y');
    const con = new Headers((fetchSimulado.mock.calls[0][1] as RequestInit).headers);
    const sin = new Headers((fetchSimulado.mock.calls[1][1] as RequestInit).headers);
    expect(con.get('Authorization')).toBe('Bearer abc');
    expect(sin.get('Authorization')).toBeNull();
  });

  it('002/RF-21: el login no envía token y devuelve la respuesta', async () => {
    const respuesta = { token: 'j', expiraEn: '2030-01-01T00:00:00Z', nombreUsuario: 'ana', rol: 'cajero' };
    const fetchSimulado = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(respuesta), { status: 200, headers: { 'Content-Type': 'application/json' } }),
    );
    vi.stubGlobal('fetch', fetchSimulado);
    expect(await iniciarSesionRestaurante('resto', 'ana', 'x')).toEqual(respuesta);
    expect(fetchSimulado.mock.calls[0][0]).toBe('/api/r/resto/auth/login');
    const init = fetchSimulado.mock.calls[0][1] as RequestInit;
    expect(new Headers(init.headers).get('Authorization')).toBeNull();
    expect(JSON.parse(init.body as string)).toEqual({ nombreUsuario: 'ana', contrasena: 'x' });
  });
});
