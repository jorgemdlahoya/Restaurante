import { expect, test, type Page } from '@playwright/test';
import { simularIdentidad, simularLoginRestaurante } from './apiSimulada';
import { categoria, simularCategorias, variasCategorias } from './apiSimuladaCategorias';
import { simularSucursales, sucursal } from './apiSimuladaSucursales';
import { simularMiSesion } from './apiSimuladaUsuarios';

const LA_ESQUINA = { nombre: 'La Esquina', slug: 'la-esquina', paleta: {}, logoUrl: null };
const CENTRO = sucursal({ id: 's1', nombre: 'Centro', slug: 'centro' });

const BEBIDAS = categoria({ id: 'c1', nombre: 'Bebidas', orden: 1 });
const POZOLES = categoria({ id: 'c2', nombre: 'Pozoles tradicionales de la casa', nombreCorto: 'Pozoles', orden: 2 });

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

function fila(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

function filasDeDatos(page: Page) {
  return page.getByRole('row').filter({ has: page.getByRole('cell') });
}

test('005/RF-10: el administrador general ve la tabla de categorías paginada', async ({ page }) => {
  const api = await simularCategorias(page, 'la-esquina', variasCategorias(22));
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias');

  await expect(page.getByRole('heading', { level: 1, name: 'Categorías' })).toBeVisible();
  for (const columna of ['Orden', 'Nombre', 'Título en WhatsApp', 'Estado']) {
    await expect(page.getByRole('columnheader', { name: columna, exact: true })).toBeVisible();
  }
  await expect(page.getByRole('button', { name: 'Nueva categoría' })).toBeVisible();
  await expect(filasDeDatos(page)).toHaveCount(20);
  await expect(fila(page, 'Categoría 01')).toContainText('Activa');
  await expect(fila(page, 'Categoría 21')).toHaveCount(0);

  await page.getByRole('button', { name: 'Siguiente' }).click();
  await expect(filasDeDatos(page)).toHaveCount(2);
  await expect(fila(page, 'Categoría 21')).toBeVisible();
  await expect(fila(page, 'Categoría 01')).toHaveCount(0);

  await page.getByRole('button', { name: 'Anterior' }).click();
  await expect(fila(page, 'Categoría 01')).toBeVisible();
  expect(api.consultas).toContainEqual({ pagina: 2, tamano: 20 });
});

test('005/RF-11: el alta muestra la vista previa y crea la categoría', async ({ page }) => {
  const api = await simularCategorias(page, 'la-esquina', [BEBIDAS]);
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias');

  await page.getByRole('button', { name: 'Nueva categoría' }).click();
  const formulario = page.getByRole('form', { name: 'Nueva categoría' });
  await formulario.getByLabel('Nombre', { exact: true }).fill('Chilaquiles verdes y rojos con pollo');
  await expect(formulario).toContainText('Así se verá en WhatsApp: Chilaquiles verdes y ro…');
  await expect(formulario).toContainText('Este nombre se verá recortado en WhatsApp');
  await formulario.getByLabel('Orden').fill('3');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Nueva categoría' })).toHaveCount(0);
  const nueva = fila(page, 'Chilaquiles verdes y rojos con pollo');
  await expect(nueva).toContainText('Chilaquiles verdes y ro…');
  await expect(nueva).toContainText('Activa');
  expect(api.altas).toEqual([{ nombre: 'Chilaquiles verdes y rojos con pollo', nombreCorto: null, orden: 3 }]);
});

test('005/RF-12: un 409 del servidor se muestra junto al nombre', async ({ page }) => {
  const mensaje = 'Ya existe una categoría con ese nombre';
  await simularCategorias(page, 'la-esquina', [BEBIDAS], { erroresAlta: { estado: 409, errors: { nombre: [mensaje] } } });
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias');

  await page.getByRole('button', { name: 'Nueva categoría' }).click();
  const formulario = page.getByRole('form', { name: 'Nueva categoría' });
  await formulario.getByLabel('Nombre', { exact: true }).fill('bebidas');
  await formulario.getByLabel('Orden').fill('2');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  const nombre = formulario.getByLabel('Nombre', { exact: true });
  await expect(nombre).toHaveAccessibleDescription(mensaje);
  await expect(nombre).toHaveAttribute('aria-invalid', 'true');
});

test('005/RF-11: editar cambia el nombre corto y el título', async ({ page }) => {
  const api = await simularCategorias(page, 'la-esquina', [BEBIDAS, POZOLES]);
  await entrar(page);
  await page.goto('/r/la-esquina/menu/categorias');

  await expect(fila(page, 'Pozoles tradicionales de la casa')).toContainText('Pozoles');
  await fila(page, 'Pozoles tradicionales de la casa').getByRole('button', { name: 'Editar' }).click();
  const formulario = page.getByRole('form', { name: 'Editar Pozoles tradicionales de la casa' });
  await expect(formulario.getByLabel('Nombre corto (opcional)')).toHaveValue('Pozoles');
  await formulario.getByLabel('Nombre corto (opcional)').fill('Pozole');
  await expect(formulario).toContainText('Así se verá en WhatsApp: Pozole');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(formulario).toHaveCount(0);
  await expect(fila(page, 'Pozoles tradicionales de la casa').getByRole('cell', { name: 'Pozole', exact: true })).toBeVisible();
  expect(api.ediciones).toEqual([
    { id: 'c2', cuerpo: { nombre: 'Pozoles tradicionales de la casa', nombreCorto: 'Pozole', orden: 2 } },
  ]);
});
