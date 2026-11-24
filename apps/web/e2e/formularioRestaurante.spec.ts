import { expect, test, type Page } from '@playwright/test';
import { simularLoginPlataforma, simularRestaurantes, type RestauranteSimulado } from './apiSimulada';

const LA_ESQUINA: RestauranteSimulado = {
  id: '11111111-1111-1111-1111-111111111111',
  nombre: 'La Esquina',
  slug: 'la-esquina',
  activo: true,
  paleta: { principal: '#F97316', acento: null, oscuro: null, fondo: null, texto: null },
};

// PNG transparente de 1×1 px.
const PNG_1PX = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
  'base64',
);

async function entrarComoPlataforma(page: Page) {
  await simularLoginPlataforma(page, { nombreUsuario: 'root', contrasena: 'Clave-root-1' });
  await page.goto('/plataforma/login');
  await page.getByLabel('Usuario').fill('root');
  await page.getByLabel('Contraseña').fill('Clave-root-1');
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/plataforma\/restaurantes$/);
}

function fila(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

test('002/RF-18: crear un restaurante con logo lo añade a la lista', async ({ page }) => {
  const api = await simularRestaurantes(page, [LA_ESQUINA]);
  await entrarComoPlataforma(page);

  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();
  const formulario = page.getByRole('form', { name: 'Nuevo restaurante' });
  await formulario.getByLabel('Nombre').fill('El Puerto');
  await formulario.getByLabel('Slug').fill('el-puerto');
  await formulario.getByLabel('Color principal').fill('#123456');
  await formulario.getByLabel('Logo').setInputFiles({ name: 'logo.png', mimeType: 'image/png', buffer: PNG_1PX });
  await formulario.getByLabel('Usuario administrador').fill('admin-puerto');
  await formulario.getByLabel('Contraseña del administrador').fill('Clave-puerto-1');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Nuevo restaurante' })).toHaveCount(0);
  await expect(fila(page, 'El Puerto')).toContainText('Activo');
  expect(api.altas).toEqual([
    expect.objectContaining({
      nombre: 'El Puerto',
      slug: 'el-puerto',
      paleta: expect.objectContaining({ principal: '#123456' }),
      administrador: { nombreUsuario: 'admin-puerto', contrasena: 'Clave-puerto-1' },
    }),
  ]);
  expect(api.logos).toHaveLength(1);
  expect(api.logos[0].id).toBe('nuevo-1');
  expect(api.logos[0].tipoContenido).toContain('multipart/form-data');
  expect(api.logos[0].tamano).toBeGreaterThan(PNG_1PX.length);
});

test('002/RF-18: los errores 400 del servidor se muestran junto a su campo', async ({ page }) => {
  const mensaje = 'El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones';
  await simularRestaurantes(page, [LA_ESQUINA], { erroresAlta: { slug: [mensaje] } });
  await entrarComoPlataforma(page);

  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();
  const formulario = page.getByRole('form', { name: 'Nuevo restaurante' });
  await formulario.getByLabel('Nombre').fill('El Puerto');
  await formulario.getByLabel('Slug').fill('El Puerto');
  await formulario.getByLabel('Usuario administrador').fill('admin-puerto');
  await formulario.getByLabel('Contraseña del administrador').fill('Clave-puerto-1');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(formulario.getByLabel('Slug')).toHaveAccessibleDescription(mensaje);
  await expect(formulario.getByLabel('Slug')).toHaveAttribute('aria-invalid', 'true');
  await expect(page.getByRole('form', { name: 'Nuevo restaurante' })).toBeVisible();
});

test('002/RF-18: editar nombre y paleta actualiza la fila', async ({ page }) => {
  const api = await simularRestaurantes(page, [LA_ESQUINA]);
  await entrarComoPlataforma(page);

  await fila(page, 'La Esquina').getByRole('button', { name: 'Editar' }).click();
  const formulario = page.getByRole('form', { name: 'Editar La Esquina' });
  await expect(formulario.getByLabel('Slug')).toBeDisabled();
  await expect(formulario.getByLabel('Slug')).toHaveValue('la-esquina');
  await expect(formulario.getByLabel('Usuario administrador')).toHaveCount(0);
  await formulario.getByLabel('Nombre').fill('La Esquina Centro');
  await formulario.getByLabel('Color de acento').fill('#00aa55');
  await formulario.getByRole('button', { name: 'Guardar' }).click();

  await expect(page.getByRole('form', { name: 'Editar La Esquina' })).toHaveCount(0);
  await expect(fila(page, 'La Esquina Centro')).toContainText('la-esquina');
  expect(api.ediciones).toEqual([
    {
      id: LA_ESQUINA.id,
      cuerpo: expect.objectContaining({
        nombre: 'La Esquina Centro',
        paleta: expect.objectContaining({ acento: '#00aa55' }),
      }),
    },
  ]);
  expect(api.logos).toEqual([]);
});

test('002/RF-18: la vista previa muestra los colores elegidos', async ({ page }) => {
  await simularRestaurantes(page, [LA_ESQUINA]);
  await entrarComoPlataforma(page);

  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();
  const formulario = page.getByRole('form', { name: 'Nuevo restaurante' });
  await formulario.getByLabel('Color principal').fill('#123456');

  const vistaPrevia = formulario.getByRole('region', { name: 'Vista previa' });
  await expect(vistaPrevia).toBeVisible();
  await expect
    .poll(() => vistaPrevia.evaluate((el) => getComputedStyle(el).getPropertyValue('--color-principal').trim()))
    .toBe('#123456');
});

test('002/RF-18: Cancelar cierra el formulario sin llamar a la API', async ({ page }) => {
  const api = await simularRestaurantes(page, [LA_ESQUINA]);
  await entrarComoPlataforma(page);

  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();
  await page.getByRole('form', { name: 'Nuevo restaurante' }).getByRole('button', { name: 'Cancelar' }).click();

  await expect(page.getByRole('form', { name: 'Nuevo restaurante' })).toHaveCount(0);
  expect(api.altas).toEqual([]);
});
