import { test, expect, type Page } from '@playwright/test';

// Flujo real de la feature 006 contra el sistema levantado con `docker compose up`:
// el administrador general crea un platillo con ingredientes, no puede eliminar su categoría
// mientras lo tenga y la elimina después de quitarlo.

// Fila de la tabla cuya celda Nombre (puede coincidir con el Título en WhatsApp) es exactamente `nombre`.
function filaDe(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

test('006/RF-12: el administrador general crea un platillo, no puede eliminar su categoría y la elimina tras quitarlo', async ({ page }) => {
  const adminUsuario = process.env.ADMIN_USERNAME;
  const adminClave = process.env.ADMIN_PASSWORD;
  if (!adminUsuario || !adminClave) {
    throw new Error('Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)');
  }

  const marca = Date.now();
  const slug = `e2e-pla-${marca}`;
  const nombreRestaurante = `Restaurante Platillos ${marca}`;
  const usuario = `admin.${marca}`;
  const clave = `Clave-${marca}`;

  // La plataforma crea el restaurante con su administrador.
  await page.goto('/plataforma/login');
  await page.getByLabel('Usuario').fill(adminUsuario);
  await page.getByLabel('Contraseña').fill(adminClave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(/\/plataforma\/restaurantes$/);
  await page.getByRole('button', { name: 'Nuevo restaurante' }).click();

  const alta = page.getByRole('form', { name: 'Nuevo restaurante' });
  await alta.getByLabel('Nombre', { exact: true }).fill(nombreRestaurante);
  await alta.getByLabel('Slug').fill(slug);
  await alta.getByLabel('Usuario administrador').fill(usuario);
  await alta.getByLabel('Contraseña del administrador').fill(clave);
  await alta.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByText(nombreRestaurante)).toBeVisible();

  // El administrador entra en su restaurante y abre Menú.
  await page.goto(`/r/${slug}/login`);
  await page.getByLabel('Usuario').fill(usuario);
  await page.getByLabel('Contraseña').fill(clave);
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/inicio$`));

  await page.getByRole('link', { name: 'Menú' }).first().click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/menu/categorias$`));

  // Crea la categoría Pozoles.
  await page.getByRole('button', { name: 'Nueva categoría' }).click();
  const nuevaCategoria = page.getByRole('form', { name: 'Nueva categoría' });
  await nuevaCategoria.getByLabel('Nombre', { exact: true }).fill('Pozoles');
  await nuevaCategoria.getByLabel('Orden').fill('1');
  await nuevaCategoria.getByRole('button', { name: 'Guardar' }).click();
  await expect(filaDe(page, 'Pozoles')).toBeVisible();

  // Abre sus platillos y crea Pozole rojo con dos ingredientes.
  await filaDe(page, 'Pozoles').getByRole('link', { name: 'Platillos' }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Platillos de Pozoles' })).toBeVisible();
  await page.getByRole('button', { name: 'Nuevo platillo' }).click();
  const formulario = page.getByRole('form', { name: 'Nuevo platillo' });
  await formulario.getByLabel('Nombre', { exact: true }).fill('Pozole rojo');
  await formulario.getByLabel('Orden').fill('1');
  const ingrediente = formulario.getByLabel('Ingredientes incluidos');
  await ingrediente.fill('Crema');
  await formulario.getByRole('button', { name: 'Agregar ingrediente' }).click();
  await ingrediente.fill('Cebolla');
  await ingrediente.press('Enter');
  await formulario.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByRole('form', { name: 'Nuevo platillo' })).toHaveCount(0);
  await expect(filaDe(page, 'Pozole rojo')).toBeVisible();
  await expect(filaDe(page, 'Pozole rojo').getByRole('cell', { name: '2', exact: true })).toBeVisible();

  // Con un platillo, la categoría no se puede eliminar.
  await page.getByRole('link', { name: 'Categorías' }).first().click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/menu/categorias$`));
  await filaDe(page, 'Pozoles').getByRole('button', { name: 'Eliminar' }).click();
  await page.getByRole('dialog', { name: '¿Eliminar Pozoles?' }).getByRole('button', { name: 'Eliminar' }).click();
  const aviso = page.getByRole('dialog', { name: 'No se puede eliminar Pozoles' });
  await expect(aviso).toContainText('Esta categoría tiene 1 platillo');
  await aviso.getByRole('button', { name: 'Cancelar' }).click();
  await expect(aviso).toHaveCount(0);
  await expect(filaDe(page, 'Pozoles')).toBeVisible();

  // Elimina el platillo confirmando.
  await filaDe(page, 'Pozoles').getByRole('link', { name: 'Platillos' }).click();
  await filaDe(page, 'Pozole rojo').getByRole('button', { name: 'Eliminar' }).click();
  await page.getByRole('dialog', { name: '¿Eliminar Pozole rojo?' }).getByRole('button', { name: 'Eliminar' }).click();
  await expect(filaDe(page, 'Pozole rojo')).toHaveCount(0);

  // Ahora la categoría sí se elimina.
  await page.getByRole('link', { name: 'Categorías' }).first().click();
  await expect(page).toHaveURL(new RegExp(`/r/${slug}/menu/categorias$`));
  await filaDe(page, 'Pozoles').getByRole('button', { name: 'Eliminar' }).click();
  await page.getByRole('dialog', { name: '¿Eliminar Pozoles?' }).getByRole('button', { name: 'Eliminar' }).click();
  await expect(filaDe(page, 'Pozoles')).toHaveCount(0);
});
