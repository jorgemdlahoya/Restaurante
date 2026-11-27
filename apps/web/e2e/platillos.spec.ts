import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import { categoria, simularCategorias } from './apiSimuladaCategorias';
import { platillo, simularPlatillos, variosPlatillos } from './apiSimuladaPlatillos';
import { simularSucursales, sucursal } from './apiSimuladaSucursales';
import { simularMiSesion } from './apiSimuladaUsuarios';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };
const CENTRO = sucursal({ id: 's1', nombre: 'Centro', slug: 'centro' });

const POZOLES = categoria({ id: 'c1', nombre: 'Pozoles', orden: 1 });
const BEBIDAS = categoria({ id: 'c2', nombre: 'Bebidas', orden: 2 });
const POSTRES_INACTIVA = categoria({ id: 'c3', nombre: 'Postres', orden: 3, activa: false });

const POZOLE_ROJO = platillo({
  id: 'p1',
  categoriaId: 'c1',
  nombre: 'Pozole rojo',
  orden: 1,
  ingredientes: ['Lechuga', 'Rábano'],
});

async function entrar(page: Page, rol = 'administrador_general') {
  await simularIdentidad(page, LA_ESQUINA);
  await simularLoginRestaurante(page, 'la-esquina', { nombreUsuario: 'ana', contrasena: 'Clave-123', rol });
  await simularMiSesion(page, 'la-esquina', {
    nombreUsuario: 'ana',
    rol,
    sucursales: [{ slug: 'centro', nombre: 'Centro' }],
  });
  await simularSucursales(page, 'la-esquina', [CENTRO]);
  await page.goto('/r/la-esquina/login');
  await page.getByLabel('Usuario').fill('ana');
  await page.getByLabel('Contraseña').fill('Clave-123');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
}

/** Simula categorías y platillos; devuelve los registros de ambos. */
async function simularMenu(page: Page, categorias = [POZOLES, BEBIDAS, POSTRES_INACTIVA], platillos = [POZOLE_ROJO]) {
  const apiCategorias = await simularCategorias(page, 'la-esquina', categorias);
  const apiPlatillos = await simularPlatillos(page, 'la-esquina', apiCategorias.categorias, platillos);
  return { apiCategorias, apiPlatillos };
}

function fila(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

function filasDeDatos(page: Page) {
  return page.getByRole('row').filter({ has: page.getByRole('cell') });
}

test('006/RF-13: desde categorías se abre la lista de platillos de la categoría', async ({ page }) => {
  await simularMenu(page, [POZOLES, BEBIDAS], variosPlatillos('c1', 22));
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias');

  await fila(page, 'Pozoles').getByRole('link', { name: 'Platillos' }).click();

  await expect(page).toHaveURL(/\/r\/la-esquina\/menu\/categorias\/c1\/platillos$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Platillos de Pozoles' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Categorías' })).toBeVisible();
  for (const columna of ['Orden', 'Nombre', 'Título en WhatsApp', 'Ingredientes', 'Estado']) {
    await expect(page.getByRole('columnheader', { name: columna, exact: true })).toBeVisible();
  }
  await expect(page.getByRole('button', { name: 'Nuevo platillo' })).toBeEnabled();
  await expect(filasDeDatos(page)).toHaveCount(20);
  await expect(fila(page, 'Platillo 01')).toContainText('Activo');

  await page.getByRole('button', { name: 'Siguiente' }).click();
  await expect(filasDeDatos(page)).toHaveCount(2);
  await expect(fila(page, 'Platillo 22')).toBeVisible();
});

test('006/RF-13: una categoría inactiva muestra el aviso y no deja crear', async ({ page }) => {
  await simularMenu(page, [POZOLES, POSTRES_INACTIVA], [platillo({ id: 'p9', categoriaId: 'c3', nombre: 'Flan' })]);
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias/c3/platillos');

  await expect(page.getByRole('heading', { level: 1, name: 'Platillos de Postres' })).toBeVisible();
  await expect(page.getByText('Esta categoría está inactiva. Sus platillos no se muestran a los clientes.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Nuevo platillo' })).toBeDisabled();
  await expect(fila(page, 'Flan')).toBeVisible();
});

test('006/RF-14: el alta con ingredientes crea el platillo', async ({ page }) => {
  const { apiPlatillos } = await simularMenu(page);
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias/c1/platillos');

  await page.getByRole('button', { name: 'Nuevo platillo' }).click();
  const formulario = page.getByRole('form', { name: 'Nuevo platillo' });
  await formulario.getByLabel('Nombre', { exact: true }).fill('Pozole verde');
  await formulario.getByLabel('Orden').fill('3');
  const ingrediente = formulario.getByLabel('Ingredientes incluidos');
  await ingrediente.fill('Crema');
  await formulario.getByRole('button', { name: 'Agregar ingrediente' }).click();
  await ingrediente.fill('Cebolla');
  await ingrediente.press('Enter');
  await expect(page.getByRole('form', { name: 'Nuevo platillo' })).toBeVisible();
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Nuevo platillo' })).toHaveCount(0);
  const nueva = fila(page, 'Pozole verde');
  await expect(nueva.getByRole('cell', { name: '3', exact: true })).toBeVisible();
  await expect(nueva.getByRole('cell', { name: '2', exact: true })).toBeVisible();
  await expect(nueva).toContainText('Activo');
  expect(apiPlatillos.altas).toEqual([
    {
      categoriaId: 'c1',
      cuerpo: { nombre: 'Pozole verde', nombreCorto: null, descripcion: null, orden: 3, ingredientes: ['Crema', 'Cebolla'] },
    },
  ]);
});

test('006/RF-14: editar mueve el platillo a otra categoría', async ({ page }) => {
  const { apiPlatillos } = await simularMenu(page);
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias/c1/platillos');

  await fila(page, 'Pozole rojo').getByRole('button', { name: 'Editar' }).click();
  const formulario = page.getByRole('form', { name: 'Editar Pozole rojo' });
  await expect(formulario.getByRole('listitem')).toHaveCount(2);
  await formulario.getByLabel('Categoría').selectOption({ label: 'Bebidas' });
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(formulario).toHaveCount(0);
  await expect(fila(page, 'Pozole rojo')).toHaveCount(0);
  expect(apiPlatillos.ediciones).toEqual([
    { id: 'p1', cuerpo: expect.objectContaining({ categoriaId: 'c2', ingredientes: ['Lechuga', 'Rábano'] }) },
  ]);
});

test('006/RF-18: otro rol vuelve al inicio', async ({ page }) => {
  const { apiPlatillos } = await simularMenu(page);
  await entrar(page, 'cajero');
  await page.goto('/r/la-esquina/menu/categorias/c1/platillos');

  await expect(page).toHaveURL(/\/r\/la-esquina\/inicio$/);
  expect(apiPlatillos.altas).toEqual([]);
});

test('006/RF-18: una categoría que no existe muestra Categoría no encontrada', async ({ page }) => {
  await simularMenu(page);
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias/no-existe/platillos');

  await expect(page.getByText('Categoría no encontrada')).toBeVisible();
  await page.getByRole('link', { name: 'Categorías' }).click();
  await expect(page).toHaveURL(/\/r\/la-esquina\/menu\/categorias$/);
});
