import { test, expect, type Page } from '@playwright/test';

// Flujo real de la feature 005 contra el sistema levantado con `docker compose up`:
// el administrador general crea, reordena y elimina categorías y reutiliza el nombre eliminado.

// Fila de la tabla cuyo celda Nombre (no el Título en WhatsApp) es exactamente `nombre`.
function filaDe(page: Page, nombre: string) {
  return page.getByRole('row').filter({ has: page.getByRole('cell', { name: nombre, exact: true }) });
}

async function crearCategoria(page: Page, nombre: string, orden: string) {
  await page.getByRole('button', { name: 'Nueva categoría' }).click();
  const formulario = page.getByRole('form', { name: 'Nueva categoría' });
  await formulario.getByLabel('Nombre', { exact: true }).fill(nombre);
  await formulario.getByLabel('Orden').fill(orden);
  await formulario.getByRole('button', { name: 'Guardar' }).click();
}

test('005/RF-07: el administrador general crea, reordena y elimina categorías y reutiliza el nombre', async ({ page }) => {
  const adminUsuario = process.env.ADMIN_USERNAME;
  const adminClave = process.env.ADMIN_PASSWORD;
  if (!adminUsuario || !adminClave) {
    throw new Error('Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)');
  }

  const marca = Date.now();
  const slug = `e2e-cat-${marca}`;
  const nombreRestaurante = `Restaurante Categorias ${marca}`;
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

  // Alta de Bebidas (orden 1) y Pozoles (orden 2).
  await crearCategoria(page, 'Bebidas', '1');
  await expect(filaDe(page, 'Bebidas')).toBeVisible();
  await crearCategoria(page, 'Pozoles', '2');
  await expect(filaDe(page, 'Pozoles')).toBeVisible();

  // Edita Bebidas a orden 3: Pozoles queda en la primera fila de datos.
  await filaDe(page, 'Bebidas').getByRole('button', { name: 'Editar' }).click();
  const edicion = page.getByRole('form', { name: 'Editar Bebidas' });
  await edicion.getByLabel('Orden').fill('3');
  await edicion.getByRole('button', { name: 'Guardar' }).click();
  await expect(page.getByRole('row').nth(1).getByRole('cell').nth(1)).toHaveText('Pozoles');

  // Elimina Bebidas confirmando el diálogo.
  await filaDe(page, 'Bebidas').getByRole('button', { name: 'Eliminar' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Eliminar' }).click();
  await expect(filaDe(page, 'Bebidas')).toHaveCount(0);

  // El nombre se reutiliza: otra Bebidas (orden 2) aparece en la tabla.
  await crearCategoria(page, 'Bebidas', '2');
  await expect(filaDe(page, 'Bebidas')).toBeVisible();
  await expect(filaDe(page, 'Pozoles')).toBeVisible();
});
