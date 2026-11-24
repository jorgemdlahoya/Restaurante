import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import { semanaCerrada, simularDetalleSucursal, simularSucursales, sucursal } from './apiSimuladaSucursales';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };
const CENTRO = sucursal({ id: 's1', nombre: 'Centro', slug: 'centro' });

async function abrirDetalle(page: Page) {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', {
    nombreUsuario: 'ana',
    contrasena: 'Clave-123',
    rol: 'administrador_general',
  });
  await simularSucursales(page, 'la-esquina', [CENTRO]);
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
  await page.goto('/r/la-esquina/sucursales');
  await page
    .getByRole('row')
    .filter({ has: page.getByRole('cell', { name: 'Centro', exact: true }) })
    .getByRole('link', { name: 'Ver horario' })
    .click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/sucursales\/centro$/);
}

test('003/RF-18: el detalle muestra el estado y el horario de la sucursal', async ({ page }) => {
  const horario = semanaCerrada();
  horario[0] = { dia: 'lunes', cerrado: false, apertura: '09:00', cierre: '18:00' };
  const datos = await simularDetalleSucursal(page, 'la-esquina', 'centro', {
    horario,
    estado: { abierta: false, mensaje: 'Estamos cerrados, abrimos mañana a las 09:00' },
  });

  await abrirDetalle(page);

  await expect(page.getByRole('heading', { level: 1, name: 'Centro' })).toBeVisible();
  await expect(page.getByRole('status')).toHaveText('Estamos cerrados, abrimos mañana a las 09:00');
  await expect(page.getByLabel('Apertura del lunes')).toHaveValue('09:00');
  await expect(page.getByLabel('Cierre del lunes')).toHaveValue('18:00');
  await expect(page.getByLabel('Cerrado el martes')).toBeChecked();
  await expect(page.getByLabel('Apertura del martes')).toBeDisabled();
  await expect(page.getByRole('link', { name: 'Volver a sucursales' })).toBeVisible();
  // El estado es público: nunca se envía el token.
  expect(datos.peticionesEstado.every((p) => p.autorizacion === undefined)).toBe(true);
});

test('003/RF-18: guardar el horario envía los siete días', async ({ page }) => {
  const datos = await simularDetalleSucursal(page, 'la-esquina', 'centro');
  await abrirDetalle(page);

  await page.getByLabel('Cerrado el lunes').uncheck();
  await page.getByLabel('Apertura del lunes').fill('09:00');
  await page.getByLabel('Cierre del lunes').fill('18:00');
  await page.getByLabel('Cerrado el domingo').uncheck();
  await page.getByLabel('Apertura del domingo').fill('18:00');
  await page.getByLabel('Cierre del domingo').fill('02:00');
  await expect(page.getByText('El turno termina al día siguiente')).toBeVisible();
  await page.getByRole('button', { name: 'Guardar horario' }).click();

  await expect.poll(() => datos.guardadosHorario.length).toBe(1);
  const { dias } = datos.guardadosHorario[0] as { dias: { dia: string; cerrado: boolean; apertura: string | null }[] };
  expect(dias).toHaveLength(7);
  expect(dias[0]).toEqual({ dia: 'lunes', cerrado: false, apertura: '09:00', cierre: '18:00' });
  expect(dias[6]).toEqual({ dia: 'domingo', cerrado: false, apertura: '18:00', cierre: '02:00' });
  expect(dias[1]).toEqual(expect.objectContaining({ dia: 'martes', cerrado: true }));
});

test('003/RF-18: crear y cancelar un cierre temporal actualiza la lista y el estado', async ({ page }) => {
  const datos = await simularDetalleSucursal(page, 'la-esquina', 'centro', { estado: { abierta: true, mensaje: null } });
  await abrirDetalle(page);
  await expect(page.getByRole('status')).toHaveText('Abierta ahora');

  datos.estado = { abierta: false, mensaje: 'Estamos cerrados, abrimos jueves a las 09:00' };
  await page.getByLabel('Fin').fill('2027-01-06T23:00');
  await page.getByLabel('Motivo').fill('Inventario');
  await page.getByRole('button', { name: 'Cerrar temporalmente' }).click();

  await expect(page.getByRole('listitem').filter({ hasText: 'Inventario' })).toBeVisible();
  await expect(page.getByRole('status')).toHaveText('Estamos cerrados, abrimos jueves a las 09:00');
  expect(datos.altasCierre).toEqual([expect.objectContaining({ fin: '2027-01-06T23:00', motivo: 'Inventario' })]);
  expect(datos.altasCierre[0].inicio ?? null).toBeNull();

  datos.estado = { abierta: true, mensaje: null };
  await page.getByRole('listitem').filter({ hasText: 'Inventario' }).getByRole('button', { name: 'Cancelar cierre' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Cancelar cierre' }).click();

  await expect(page.getByRole('listitem').filter({ hasText: 'Inventario' })).toHaveCount(0);
  await expect(page.getByRole('status')).toHaveText('Abierta ahora');
  expect(datos.cancelaciones).toEqual(['cierre-1']);
});
