---
feature: 007-variantes-de-platillos-y-precios-base-hu
estado: borrador
---
# Tareas: Variantes de platillos y precios base (HU-3)

> **Para agentes:** se ejecuta con `/implementar`, tarea por tarea, con TDD. Cada tarea se cierra
> con `python3 .claude/scripts/completar-tarea.py T-NN` (sólo con `comando_test` en verde). Los
> pasos usan casillas (`- [ ]`).

**Objetivo:** que cada platillo tenga variantes de venta con nombre, precio y SKU opcional; que el
alta de platillo fije el precio de su variante "Regular"; y que no se pueda dejar un platillo sin
variantes.

**Arquitectura:** entidad `Variante` del restaurante (`IEntidadDeRestaurante`, `IEliminable`) con el
filtro global de 003/005 sin cambios. Endpoints CQRS en `VariantesController`
(`/platillos/{id}/variantes` y `/variantes/{id}`), sólo para `administrador_general`. `CrearPlatillo`
y `EliminarPlatillo` de 006 crean la "Regular" y eliminan en cascada en el mismo guardado (mismo
`DbContext`). Pantalla `/r/{slug}/menu/platillos/{platilloId}/variantes` con el patrón de las de
categorías y platillos.

**Stack:** .NET 10, MediatR 12.x, FluentValidation, EF Core + Npgsql, xUnit, Testcontainers;
React + TypeScript + Vite, TanStack Query, Vitest + Testing Library, Playwright.

**Spec:** `specs/007-variantes-de-platillos-y-precios-base-hu/requerimiento.md` y `diseno.md`;
constitución en `specs/000-constitucion/`. El código de 006 (`Platillo`, `PlatilloRepository`,
`PlatillosController`, `CrearPlatillo`, `EliminarPlatillo`, `PlatillosPagina`, `FormularioPlatillo`,
`e2e/apiSimuladaPlatillos.ts`) y de 005 (`ConflictoCamposException`, `mensajeError.ts`,
`DialogoConfirmacion`) es la base: seguir sus patrones.

## Restricciones globales

- Tests con el RF calificado: `[Fact(DisplayName = "007/RF-NN: …")]`, `it("007/RF-NN: …")`, `test("007/RF-NN: …")`. Las pruebas de 005/006 que haya que tocar conservan su prefijo.
- Integración con PostgreSQL: `[Trait("Category", "Integracion")]` + `[Collection(PostgresCollection.Nombre)]`; se ejecuta a mano en la tarea que la crea (Docker encendido; `dockerd` si hace falta).
- `dotnet`: anteponer `PATH=/root/.dotnet:$PATH`. EF: `dotnet tool run dotnet-ef -- migrations add <Nombre> --project src/Restaurante.Infrastructure --startup-project src/Restaurante.Infrastructure --output-dir Persistencia/Migraciones` desde `apps/api`.
- Formatos (RF-03): nombre 1–24 (UTF-16) tras `Trim()`; precio `decimal` de 0.00 a 99,999.99 con hasta 2 decimales (`decimal.Round(p, 2) == p`), número JSON estricto (`[JsonNumberHandling(Strict)]`); SKU tras `Trim()` con `^[A-Za-z0-9_-]{1,40}\z`, vacío → `null`.
- Unicidad entre variantes no eliminadas: nombre por platillo con `Trim().ToLowerInvariant()`; SKU por restaurante con `Trim().ToUpperInvariant()`.
- Mensajes 400: `El nombre es obligatorio`, `El nombre no puede superar 24 caracteres`, `El precio es obligatorio`, `El precio no puede ser negativo`, `El precio no puede superar 99,999.99`, `El precio debe tener como máximo 2 decimales`, `El SKU sólo admite letras, números, guiones y guion bajo (hasta 40)`, `El estado es obligatorio` (clave `activa`). Un precio como texto o no numérico → 400 en `precio` con `RespuestasProblema.MensajeCuerpoInvalido`.
- 409 por campo: `nombre: ["Ya existe una variante con ese nombre en este platillo"]`, `sku: ["Ya existe una variante con ese SKU"]` (`ConflictoCamposException`). 409 sin campo: título `Conflicto`, detalle `No se puede eliminar la única variante del platillo`.
- 404: título `No encontrado`, detalle `Platillo no encontrado` o `Variante no encontrada`; nunca 403 por otro restaurante. 403: `Acceso denegado`.
- API: `VarianteDto { id, platilloId, nombre, precio, sku, activa }`; lista sin paginar ordenada por `precio`, `nombre`, `id`. Variante automática: nombre `Regular`, sin SKU.
- Textos de pantalla: acción de platillo `Variantes`; título `Variantes de {platillo}`; enlace `Platillos de {categoría}`; `Nueva variante`; columnas `Nombre`, `Precio`, `SKU`, `Estado`, `Acciones`; estados `Activa`/`Inactiva`; acciones `Editar`, `Desactivar`/`Activar`, `Eliminar`; aviso `Este platillo no tiene variantes activas: no se muestra a los clientes.`; campos `Nombre`, `Precio`, `SKU (opcional)`; contador `N/24`; error de cliente `El precio debe ser un número de 0 a 99,999.99 con hasta 2 decimales`; confirmación `¿Eliminar {nombre}?` / `No se podrá recuperar. Si sólo quieres ocultarla, desactívala.`; `Platillo no encontrado` con enlace `Categorías`; en el alta de platillo, campo `Precio`; precios mostrados con `Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })` (`$1,234.50`); 403 → `No tienes permiso para esta acción.`
- Art. 5: todo endpoint nuevo o cambiado actualiza `docs/context/http/variantes.http` (nuevo) o `platillos.http`, y los glosarios (`docs/GLOSSARY.md`, `docs/context/glossary.md`) en la misma tarea; esquema y pantallas, `docs/context/architecture-map.md`.
- Frontend: sólo variables de tema; escritorio 1440 px y móvil 390 px; el token se pasa siempre explícito a `pedirApi`; ids en URLs con `encodeURIComponent`.

## Foco de revisión

1. **Precio `45.5`**: se guarda como 45.50 exacto y se muestra `$45.50` (T-02, T-08).
2. **Precio con 3 decimales (`1.005`)**: 400 en el servidor y error en el cliente; nunca se redondea (T-01, T-03, T-07).
3. **Desactivar la única variante activa**: se permite y la pantalla muestra el aviso de RF-16 (T-04, T-09).
4. **SKU `abc-1` en un platillo y `ABC-1` en otro del mismo restaurante**: 409 en `sku` (T-03).
5. **Eliminar un platillo y reutilizar el SKU de una de sus variantes en otro platillo**: se acepta (T-05).

---

### T-01 [RF-01, RF-03, RF-09, RF-14]: Entidad Variante, precio y variantes a la venta

**Archivos:**
- Crear: `apps/api/src/Restaurante.Domain/Menu/Variante.cs`, `DatosVariante.cs`, `PrecioVariante.cs`, `VariantesALaVenta.cs`
- Test: `apps/api/tests/Restaurante.Domain.Tests/Menu/VarianteTests.cs`, `PrecioVarianteTests.cs`, `VariantesALaVentaTests.cs`
- Docs: glosarios (Variante, precio base, SKU, variante Regular, variantes a la venta)

**Interfaces:**
- Consume: `IEntidadDeRestaurante`, `IEliminable` (`EliminadaEn`), `Categoria`, `Platillo` (005/006).
- Produce: `sealed record DatosVariante(string Nombre, decimal Precio, string? Sku)`.
- Produce: `static class PrecioVariante { const decimal Maximo = 99_999.99m; static IReadOnlyList<string> Errores(decimal? precio); }` con los mensajes de precio de las restricciones globales (falta → `El precio es obligatorio`). Única fuente de la regla.
- Produce: `Variante : IEntidadDeRestaurante, IEliminable` con `const int LongitudMaximaNombre = 24`, `const string NombreRegular = "Regular"`, `const string PatronSku = @"^[A-Za-z0-9_-]{1,40}\z"`; `static Variante Crear(Guid restauranteId, Guid platilloId, DatosVariante datos, DateTimeOffset creadaEn)`, `static Variante Regular(Guid restauranteId, Guid platilloId, decimal precio, DateTimeOffset creadaEn)`, `void Editar(DatosVariante datos)`, `void Activar()`, `void Desactivar()`, `void Eliminar(DateTimeOffset ahora)`; `static string NormalizarNombre(string)` (`Trim().ToLowerInvariant()`), `static string NormalizarSku(string)` (`Trim().ToUpperInvariant()`). Propiedades `Id`, `RestauranteId`, `PlatilloId`, `Nombre`, `NombreNormalizado`, `Precio`, `Sku`, `SkuNormalizado`, `Activa` (empieza en `true`), `CreadaEn`, `EliminadaEn`. Valida todo antes de asignar; lanza `ArgumentException`. Constructor privado para EF.
- Produce: `static class VariantesALaVenta { static IReadOnlyList<Variante> De(Categoria categoria, Platillo platillo, IEnumerable<Variante> variantes); }` (RF-14), ordenadas por `Precio` y `Nombre`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"007/RF-03: una variante nueva está activa y con datos recortados"` → `Crear(rid, pid, new("  1 litro ", 45.5m, "  "), t)` da `Nombre == "1 litro"`, `NombreNormalizado == "1 litro"`, `Precio == 45.50m`, `Sku == null`, `Activa`.
  - `"007/RF-03: Crear y Editar rechazan formatos inválidos"` (Theory) → `ArgumentException` con nombre `""`/25 × `"a"`, precio `-0.01m`/`100_000m`/`1.005m`, SKU `"ab c"`/41 × `"a"`/`"á1"`; acepta nombre de 24, precio `0m` y `99_999.99m`, SKU `"TAC-01_b"`.
  - `"007/RF-03: Editar inválido no cambia la variante"`.
  - `PrecioVarianteTests` `"007/RF-03: valida el precio"` (Theory) → `null` → `El precio es obligatorio`; `-1m` → `El precio no puede ser negativo`; `100_000m` → `El precio no puede superar 99,999.99`; `1.005m` → `El precio debe tener como máximo 2 decimales`; `0m`, `45.5m`, `99_999.99m` → sin errores (Foco 2).
  - `"007/RF-01: la variante Regular se llama Regular, está activa y no tiene SKU"`.
  - `"007/RF-09: eliminar marca la fecha y conserva los datos"`.
  - `VariantesALaVentaTests` `"007/RF-14: calcula las variantes a la venta"` (Theory sobre: todo activo → las activas no eliminadas, ordenadas por precio; una inactiva y una eliminada → no salen; platillo inactivo → ninguna; platillo eliminado → ninguna; categoría inactiva → ninguna).
- [ ] **Paso 2:** `PATH=/root/.dotnet:$PATH dotnet test apps/api/tests/Restaurante.Domain.Tests --filter "DisplayName~007/"` → no compila.
- [ ] **Paso 3:** implementar en `Restaurante.Domain`.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): entidad Variante, precio y variantes a la venta (T-01)"`.

### T-02 [RF-04, RF-05, RF-09, RF-11]: Persistencia de variantes y relleno de "Regular"

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Abstracciones/IVarianteRepository.cs`, `apps/api/src/Restaurante.Application/Features/Variantes/MensajesVariante.cs`
- Crear: `apps/api/src/Restaurante.Infrastructure/Persistencia/VarianteConfiguration.cs`, `VarianteRepository.cs`
- Modificar: `RestauranteDbContext.cs` (`DbSet<Variante>`), `apps/api/src/Restaurante.Infrastructure/DependencyInjection.cs`
- Crear: migración `Variantes` (tabla + `migrationBuilder.Sql` con el relleno)
- Test: `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/VariantesIntegracionTests.cs`
- Docs: `docs/context/architecture-map.md` (tabla `variantes` y relleno)

**Interfaces:**
- Consume: `Variante` (T-01), `ConflictoCamposException`.
- Produce: `static class MensajesVariante { NombreRepetido = "Ya existe una variante con ese nombre en este platillo"; SkuRepetido = "Ya existe una variante con ese SKU"; NoEncontrada = "Variante no encontrada"; UnicaVariante = "No se puede eliminar la única variante del platillo"; }`.
- Produce (`IVarianteRepository`, todo dentro del restaurante y sin eliminadas, por el filtro global):
  ```csharp
  Task<Variante?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
  Task<IReadOnlyList<Variante>> ListarPorPlatilloAsync(Guid platilloId, CancellationToken ct); // Precio, Nombre, Id
  Task<bool> ExisteNombreAsync(Guid platilloId, string nombreNormalizado, Guid? excluirId, CancellationToken ct);
  Task<bool> ExisteSkuAsync(string skuNormalizado, Guid? excluirId, CancellationToken ct);
  Task<int> ContarPorPlatilloAsync(Guid platilloId, CancellationToken ct);
  void Agregar(Variante variante);
  Task GuardarCambiosAsync(CancellationToken ct);
  ```
- Produce el esquema del diseño: `precio numeric(10,2)` con `ck_variantes_precio` (`precio >= 0 and precio <= 99999.99`), únicos parciales `ix_variantes_platillo_nombre` y `ix_variantes_restaurante_sku`, índice `ix_variantes_platillo_precio_nombre`; FKs `Restrict`. `GuardarCambiosAsync` traduce `UniqueViolation` por `ConstraintName` (`sku` para su índice; otro → `nombre`).
- Relleno: `insert into variantes (id, restaurante_id, platillo_id, nombre, nombre_normalizado, precio, sku, sku_normalizado, activa, creada_en, eliminada_en) select gen_random_uuid(), p.restaurante_id, p.id, 'Regular', 'regular', 0.00, null, null, false, now(), null from platillos p where p.eliminada_en is null`. `Down` borra la tabla.

- [ ] **Paso 1: Pruebas de integración que fallan**
  - `"007/RF-05: el precio se guarda exacto y la lista ordena por precio y nombre"` → 45.5 vuelve como `45.50m`; variantes 20 "B", 10 "Z", 10 "A" → `["A", "Z", "B"]` (Foco 1).
  - `"007/RF-04: los índices únicos traducen a ConflictoCamposException por campo"` → nombre repetido en el mismo platillo → `nombre`; mismo nombre en otro platillo → se guarda; SKU repetido con otra capitalización en otro platillo → `sku`; dos sin SKU → se guardan.
  - `"007/RF-09: una variante eliminada no se ve, no cuenta y libera nombre y SKU"`.
  - `"007/RF-11: las variantes de otro restaurante no se ven"`.
  - `"007/RF-05: la base rechaza precios fuera de rango"` → SQL directo con `-1` y `100000` falla por `ck_variantes_precio`.
  - `"007/RF-01: la migración crea una Regular inactiva por platillo existente"` → con la base migrada hasta `Platillos`, insertar dos platillos (uno eliminado), aplicar `Variantes` y comprobar una sola fila `Regular`, `precio 0.00`, `activa false` para el no eliminado (usar `IMigrator.MigrateAsync("<migración previa>")` y luego la última).
  - Las pruebas de integración de 003–006 siguen en verde.
- [ ] **Paso 2:** `dotnet test apps/api --filter "Category=Integracion&DisplayName~007/"` → no compila.
- [ ] **Paso 3:** implementar; `dotnet tool run dotnet-ef -- migrations add Variantes …`; añadir el relleno en `Up`; registrar el repositorio.
- [ ] **Paso 4:** `dotnet test apps/api --filter "Category=Integracion"` → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): persistencia de variantes y relleno de Regular (T-02)"`.

### T-03 [RF-02, RF-03, RF-04, RF-06, RF-12]: Alta de variante

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Variantes/VarianteDto.cs`, `DatosVarianteDto.cs`, `ValidadorDatosVariante.cs`, `ComprobadorUnicidadVariante.cs`, `CrearVariante/*`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/VariantesController.cs`
- Crear: dobles `VarianteRepositoryEnMemoria` en `apps/api/tests/Restaurante.Application.Tests/Dobles/` y `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/` (filtra por restaurante y `EliminadaEn == null`; `FabricaApi.Variantes`)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Variantes/CrearVarianteTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Variantes/VariantesEndpointsTests.cs`
- Docs: `docs/context/http/variantes.http` (nuevo), glosarios (`VarianteDto`)

**Interfaces:**
- Consume: `Variante`, `DatosVariante`, `PrecioVariante` (T-01); `IVarianteRepository`, `MensajesVariante` (T-02); `IPlatilloRepository`, `MensajesPlatillo.NoEncontrado` (006); `ConflictoCamposException`, `NoEncontradoException`; `IContextoRestaurante`, `IReloj`.
- Produce: `sealed record DatosVarianteDto(string? Nombre, decimal? Precio, string? Sku)` con `DatosVariante ADominio()`; `ValidadorDatosVariante : AbstractValidator<DatosVarianteDto>` (claves `nombre`, `precio`, `sku` sin prefijo; precio con `PrecioVariante.Errores`).
- Produce: `sealed record VarianteDto(Guid Id, Guid PlatilloId, string Nombre, decimal Precio, string? Sku, bool Activa)` con `static Desde(Variante)`.
- Produce: `ComprobadorUnicidadVariante.ComprobarAsync(Guid platilloId, string nombre, string? sku, Guid? excluirId, CancellationToken ct)` (una sola `ConflictoCamposException` con todos los campos).
- Produce: `CrearVarianteCommand(Guid PlatilloId, DatosVarianteDto Datos) : IRequest<VarianteDto>`; `VariantesController` con `[Route("api/r/{slug}")]` y `[RequiereRol(Rol.AdministradorGeneral)]` en la clase; `POST platillos/{platilloId:guid}/variantes` (`CrearVariantePeticion(string? Nombre, [JsonNumberHandling(Strict)] decimal? Precio, string? Sku)`) → 201, `Location: /api/r/{slug}/variantes/{id}`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"007/RF-02: crea la variante activa en el platillo"` (handler) → restaurante del contexto, `CreadaEn` del reloj, un guardado.
  - `"007/RF-02: POST devuelve 201 con Location y la variante"` → `{ nombre: "1 litro", precio: 45.5, sku: "AGU-1L" }` → `precio: 45.5`, `activa: true`.
  - `"007/RF-03: datos inválidos dan 400 por campo"` (Theory, `AssertValidacionAsync`) → `nombre` (`""`, 25), `precio` (ausente, `-1`, `100000`, `1.005`, `"45"`, `"abc"`), `sku` (`"ab c"`, 41) (Foco 2).
  - `"007/RF-03: un SKU en blanco se guarda como ausente"`.
  - `"007/RF-04: nombre repetido en el mismo platillo da 409 y en otro platillo se acepta"`; `"007/RF-04: un SKU de otra variante del restaurante da 409 sin distinguir mayúsculas"` (Foco 4); `"007/RF-04: el mismo SKU en otro restaurante se acepta"` (RNF-01).
  - `"007/RF-06: un platillo inexistente, eliminado o de otro restaurante da 404"` (Theory).
  - `"007/RF-12: un rol distinto de administrador_general recibe 403"` (Theory: `cajero`, `cocinero`, `administrador_sucursal`, token de plataforma).
- [ ] **Paso 2:** filtro `DisplayName~007/RF-02|DisplayName~007/RF-03|DisplayName~007/RF-04|DisplayName~007/RF-06|DisplayName~007/RF-12` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): alta de variante (T-03)"`.

### T-04 [RF-05, RF-06, RF-07, RF-08, RF-11]: Lista, edición y estado de variantes

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Variantes/ListarVariantes/*`, `EditarVariante/*`, `CambiarEstadoVariante/*`
- Modificar: `VariantesController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Variantes/EditarVarianteTests.cs`, `VariantesEndpointsTests.cs`
- Docs: `variantes.http`

**Interfaces:**
- Consume: `VarianteDto`, `DatosVarianteDto`, `ValidadorDatosVariante`, `ComprobadorUnicidadVariante` (T-03).
- Produce: `ListarVariantesQuery(Guid PlatilloId) : IRequest<IReadOnlyList<VarianteDto>>` → `GET platillos/{platilloId:guid}/variantes`; 404 `Platillo no encontrado`.
- Produce: `EditarVarianteCommand(Guid Id, DatosVarianteDto Datos) : IRequest<VarianteDto>` → `PUT variantes/{id:guid}` (`EditarVariantePeticion` = campos de alta); orden: 400 → 404 `Variante no encontrada` (también si su platillo no está en el contexto) → 409 (excluyendo su id).
- Produce: `CambiarEstadoVarianteCommand(Guid Id, bool? Activa) : IRequest<VarianteDto>` → `PUT variantes/{id:guid}/estado` con `{ activa }`; sin `activa` → 400 `El estado es obligatorio`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"007/RF-05: GET lista las variantes no eliminadas por precio y nombre"` → activas e inactivas; no trae eliminadas ni de otro platillo.
  - `"007/RF-06: la lista de un platillo inexistente, eliminado o de otro restaurante da 404"`.
  - `"007/RF-07: editar cambia nombre, precio y SKU"`; `"007/RF-07: cambiar sólo mayúsculas no choca consigo misma"`; `"007/RF-07: el nombre de otra variante del platillo da 409"`.
  - `"007/RF-08: desactivar la única variante activa se permite"` (Foco 3); `"007/RF-08: una inactiva sigue en la lista y con su SKU reservado"`; `"007/RF-08: estado sin activa o no booleano da 400 en activa"`.
  - `"007/RF-11: editar o cambiar estado de una variante inexistente, eliminada, de otro restaurante o de un platillo eliminado da 404"` (Theory).
- [ ] **Paso 2:** filtro `DisplayName~007/RF-05|DisplayName~007/RF-06|DisplayName~007/RF-07|DisplayName~007/RF-08|DisplayName~007/RF-11` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): lista, edición y estado de variantes (T-04)"`.

### T-05 [RF-09, RF-10, RF-11, RF-13]: Eliminar variantes y eliminar en cascada

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Variantes/EliminarVariante/*`, `apps/api/src/Restaurante.Application/Comun/Excepciones/UnicaVarianteException.cs`
- Modificar: `apps/api/src/Restaurante.Application/Features/Platillos/EliminarPlatillo/EliminarPlatilloHandler.cs` (006), `apps/api/src/Restaurante.WebApi/Filtros/FiltroExcepciones.cs`, `VariantesController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Variantes/EliminarVarianteTests.cs`, `apps/api/tests/Restaurante.Application.Tests/Features/Platillos/EliminarPlatilloTests.cs`, `VariantesEndpointsTests.cs`
- Docs: `variantes.http`, `platillos.http` (el DELETE también elimina variantes), glosarios

**Interfaces:**
- Consume: `IVarianteRepository.ContarPorPlatilloAsync`, `ListarPorPlatilloAsync` (T-02).
- Produce: `EliminarVarianteCommand(Guid Id) : IRequest` → `DELETE variantes/{id:guid}` → 204; si `ContarPorPlatilloAsync == 1` lanza `UnicaVarianteException` → 409 `Conflicto` con el detalle `No se puede eliminar la única variante del platillo` (sin `errors`).
- `EliminarPlatilloHandler`: tras marcar el platillo, marca cada variante de `ListarPorPlatilloAsync` con la misma fecha y guarda una vez (mismo `DbContext`); los tests de 006 se construyen con un doble de variantes.

- [ ] **Paso 1: Pruebas que fallan**
  - `"007/RF-09: eliminar responde 204, la quita de la lista y libera nombre y SKU"`.
  - `"007/RF-10: eliminar la única variante da 409 y no la elimina"`; `"007/RF-10: con una inactiva y otra activa se puede eliminar cualquiera"`.
  - `"007/RF-11: eliminar una variante inexistente o ya eliminada da 404"`.
  - `"007/RF-13: eliminar un platillo elimina sus variantes"` (handler y WebApi); `"007/RF-13: tras eliminar el platillo su SKU se puede usar en otro"` (Foco 5).
  - Las pruebas de 006 de eliminar platillo siguen en verde.
- [ ] **Paso 2:** filtro `DisplayName~007/RF-09|DisplayName~007/RF-10|DisplayName~007/RF-11|DisplayName~007/RF-13` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): eliminar variantes y eliminar en cascada (T-05)"`.

### T-06 [RF-01]: Precio en el alta de platillo y variante Regular

**Archivos:**
- Modificar: `apps/api/src/Restaurante.Application/Features/Platillos/CrearPlatillo/CrearPlatilloCommand.cs`, `CrearPlatilloHandler.cs`, `CrearPlatilloValidator.cs` (006), `apps/api/src/Restaurante.WebApi/Controllers/PlatillosController.cs` (`CrearPlatilloPeticion` con `[JsonNumberHandling(Strict)] decimal? Precio`)
- Modificar: las pruebas de alta de platillo de 006 (`CrearPlatilloTests.cs`, `PlatillosEndpointsTests.cs` y cualquier siembra por POST) para enviar `precio`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Platillos/CrearPlatilloTests.cs`, `PlatillosEndpointsTests.cs`, `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/VariantesIntegracionTests.cs`
- Docs: `platillos.http`, glosarios

**Interfaces:**
- Consume: `Variante.Regular`, `PrecioVariante` (T-01), `IVarianteRepository.Agregar` (T-02).
- Produce: `CrearPlatilloCommand(Guid CategoriaId, DatosPlatilloDto Datos, decimal? Precio)`; el validador añade `precio` con `PrecioVariante.Errores`; el handler agrega el platillo y `Variante.Regular(...)` y guarda una vez. La respuesta (`PlatilloDto`) no cambia.

- [ ] **Paso 1: Pruebas que fallan**
  - `"007/RF-01: crear un platillo crea su variante Regular con el precio"` (handler) → un platillo, una variante `Regular` activa con el precio, un guardado.
  - `"007/RF-01: POST de platillo sin precio o con precio inválido da 400 en precio"` (Theory: ausente, `-1`, `1.005`, `"45"`).
  - `"007/RF-01: tras crear el platillo su lista de variantes tiene la Regular"` (WebApi, con `GET /platillos/{id}/variantes`).
  - `"007/RF-01: platillo y Regular se guardan en el mismo guardado"` (integración: con repositorios reales sobre el mismo contexto, un solo `GuardarCambiosAsync` deja las dos filas).
- [ ] **Paso 2:** filtro `DisplayName~007/RF-01|DisplayName~006/` → FAIL en las nuevas.
- [ ] **Paso 3:** implementar y actualizar las pruebas de 006 que crean platillos.
- [ ] **Paso 4:** `DisplayName~006/|DisplayName~007/` → PASS; integración `DisplayName~007/` → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): precio en el alta de platillo y variante Regular (T-06)"`.

### T-07 [RF-15, RF-17]: Formulario de variante, formato de precio y precio en el alta de platillo

**Archivos:**
- Crear: `apps/web/src/menu/precio.ts`, `apiVariantes.ts`, `FormularioVariante.tsx` (+ `.module.css`)
- Modificar: `apps/web/src/menu/FormularioPlatillo.tsx` (campo `Precio` sólo en alta), `apps/web/src/menu/apiPlatillos.ts` (`DatosPlatillo` del alta con `precio`; `obtenerPlatillo`)
- Modificar: `apps/web/src/menu/FormularioPlatillo.test.tsx` (las pruebas de alta de 006 envían `precio`)
- Test: `apps/web/src/menu/precio.test.ts`, `FormularioVariante.test.tsx`, `FormularioPlatillo.test.tsx`

**Interfaces:**
- Consume: `pedirApi`, `ErrorApi`, `CampoFormulario`, `mensajeDeError`, `json` (005/006).
- Produce (`precio.ts`): `export const MENSAJE_PRECIO = 'El precio debe ser un número de 0 a 99,999.99 con hasta 2 decimales'; export function leerPrecio(texto: string): number | null;` (acepta `^\d{1,5}(\.\d{1,2})?$` tras recortar); `export function formatearPrecio(precio: number): string;` (`Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })`).
- Produce (`apiVariantes.ts`): `type Variante = { id: string; platilloId: string; nombre: string; precio: number; sku: string | null; activa: boolean }`; `type DatosVariante = { nombre: string; precio: number; sku: string | null }`; `claveVariantes(slug: string, platilloId: string)`; `listarVariantes(token, slug, platilloId): Promise<Variante[]>`; `crearVariante(token, slug, platilloId, datos)`; `editarVariante(token, slug, id, datos)`; `cambiarEstadoVariante(token, slug, id, activa)`; `eliminarVariante(token, slug, id): Promise<void>`.
- Produce (`apiPlatillos.ts`): `obtenerPlatillo(token, slug, id): Promise<Platillo>`; `crearPlatillo(token, slug, categoriaId, datos & { precio: number })`.
- Produce: `FormularioVariante({ slugRestaurante, token, platilloId, variante?, onGuardada(v: Variante): void, onCancelar(): void })` con campos `Nombre` (contador `N/24`), `Precio`, `SKU (opcional)`; errores 400/409 por campo; un 409 sin `errors` o con claves desconocidas como aviso general; 403 → `mensajeDeError`.

- [ ] **Paso 1: Pruebas que fallan**
  - `precio.test.ts` `"007/RF-17: lee y formatea precios"` (`it.each`) → `leerPrecio`: `"45"` → 45, `" 45.5 "` → 45.5, `"0"` → 0, `"99999.99"` → 99999.99, `"1.005"`/`"-1"`/`"100000"`/`"abc"`/`""`/`"45,50"` → `null`; `formatearPrecio(1234.5)` → `"$1,234.50"`, `formatearPrecio(45.5)` → `"$45.50"`.
  - `"007/RF-17: el contador muestra N/24 al escribir el nombre"`.
  - `"007/RF-17: un precio inválido se marca sin llamar a la API"` (`""`, `"1.005"`, `"-1"`, `"abc"`) → `MENSAJE_PRECIO`.
  - `"007/RF-17: los errores 400 y 409 del servidor se muestran junto a su campo"` (`nombre`, `precio`, `sku`).
  - `"007/RF-17: en edición envía PUT con SKU en blanco como null"`.
  - `"007/RF-15: el alta de platillo pide el precio y lo envía"`; `"007/RF-15: la edición de platillo no muestra Precio"`; `"007/RF-15: un precio inválido en el alta se marca sin llamar a la API"`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run src/menu)` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo comando → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): formulario de variante y precio en el alta de platillo (T-07)"`.

### T-08 [RF-15, RF-16, RF-19]: Pantalla de variantes: acceso, lista, alta y edición
UI: sí

**Archivos:**
- Crear: `apps/web/src/menu/VariantesPagina.tsx` (+ `.module.css`), `apps/web/e2e/apiSimuladaVariantes.ts`, `apps/web/e2e/variantes.spec.ts`
- Modificar: `apps/web/src/menu/PlatillosPagina.tsx` (acción `Variantes` por fila), `apps/web/src/App.tsx` (ruta `menu/platillos/:platilloId/variantes`)
- Modificar: `apps/web/e2e/platillos.spec.ts` (el alta de 006 llena `Precio` y espera `precio` en el cuerpo) y `apps/web/e2e/apiSimuladaPlatillos.ts` si hace falta
- Test: `apps/web/src/menu/VariantesPagina.test.tsx`
- Docs: `docs/context/architecture-map.md` (ruta y componentes)

**Interfaces:**
- Consume: `apiVariantes.ts`, `precio.ts`, `obtenerPlatillo`, `FormularioVariante` (T-07), `MarcoRestaurante`, `RutaProtegida`.
- Produce: `VariantesPagina` (sólo `administrador_general`, `redirigirSinRol` → inicio): título `Variantes de {platillo}`, enlace `Platillos de {categoría}` a `/r/{slug}/menu/categorias/{categoriaId}/platillos`, aviso de RF-16 si no hay variantes activas, tabla con precio formateado, formulario de alta y edición; 404 del platillo → `Platillo no encontrado` con enlace `Categorías`.
- Produce (`apiSimuladaVariantes.ts`): `type VarianteSimulada`, `variante(parcial)`, `simularVariantes(page, slug, platillos, iniciales, opciones?: { eliminarUnica?: boolean })` que simula GET/POST `/platillos/{id}/variantes` (orden por precio y nombre), PUT `/variantes/{id}`, PUT `/estado` y DELETE (409 si es la única cuando `eliminarUnica`), con registros de peticiones. Registrar después de `simularPlatillos`.

- [ ] **Paso 1: Pruebas Playwright que fallan (commit sola)**
  - `test("007/RF-16: desde platillos se abre la lista de variantes ordenada por precio")` → `Variantes` en la fila; título `Variantes de Agua de horchata`; enlace `Platillos de Bebidas`; filas `1/2 litro` `$25.00` y `1 litro` `$45.50` en ese orden (Foco 1).
  - `test("007/RF-16: sin variantes activas se muestra el aviso")`.
  - `test("007/RF-16: el alta crea la variante con precio y SKU")` → el POST lleva `{ nombre: "Jarra", precio: 90, sku: "AGU-JARRA" }`.
  - `test("007/RF-16: editar cambia el precio")`.
  - `test("007/RF-15: el alta de platillo pide el precio")` → el POST de platillo lleva `precio: 85`.
  - `test("007/RF-19: otro rol vuelve al inicio")`; `test("007/RF-19: un platillo que no existe muestra Platillo no encontrado")`.
  - Vitest `VariantesPagina.test.tsx`: `"007/RF-16: muestra los precios con formato de pesos"`.
- [ ] **Paso 2:** `(cd apps/web && npx playwright test e2e/variantes.spec.ts e2e/platillos.spec.ts)` → FAIL en las nuevas y en el alta de 006; commit de las pruebas solas.
- [ ] **Paso 3:** Ralph Loop (`/implementar`).
- [ ] **Paso 4:** las pruebas → PASS en `escritorio` y `movil`; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): pantalla de variantes con lista, alta y edición (T-08)"`.

### T-09 [RF-08, RF-10, RF-17, RF-18]: Estado y eliminación de variantes
UI: sí

**Archivos:**
- Modificar: `apps/web/src/menu/VariantesPagina.tsx`, `apps/web/e2e/variantes.spec.ts`
- Test: `apps/web/src/menu/VariantesPagina.test.tsx`

**Interfaces:**
- Consume: `cambiarEstadoVariante`, `eliminarVariante` (T-07), `simularVariantes` (T-08), `DialogoConfirmacion`, `mensajeDeError`.
- Produce: acciones por fila `Desactivar`/`Activar` (sin confirmación) y `Eliminar` (con la confirmación de las restricciones globales); un 409 de la única variante se muestra como aviso con su detalle; 403 → `No tienes permiso para esta acción.`

- [ ] **Paso 1: Pruebas Playwright que fallan (commit sola)**
  - `test("007/RF-18: eliminar una variante pide confirmación y quita la fila")`.
  - `test("007/RF-18: desactivar y activar no piden confirmación")`.
  - `test("007/RF-08: desactivar la única variante activa muestra el aviso")` (Foco 3).
  - `test("007/RF-10: eliminar la única variante muestra que no se puede")` → aviso `No se puede eliminar la única variante del platillo`; la fila sigue.
  - Vitest: `"007/RF-17: un 403 al eliminar muestra que no hay permiso"`.
- [ ] **Paso 2:** `(cd apps/web && npx playwright test e2e/variantes.spec.ts)` → FAIL en las nuevas; commit de las pruebas solas.
- [ ] **Paso 3:** Ralph Loop (`/implementar`).
- [ ] **Paso 4:** las pruebas → PASS en `escritorio` y `movil`; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(007): estado y eliminación de variantes (T-09)"`.

### T-10 [RF-01, RF-10]: Prueba de punta a punta de variantes

**Archivos:**
- Crear: `apps/web/e2e-stack/variantes.spec.ts`
- Modificar: `apps/web/e2e-stack/platillos.spec.ts` (006: el alta llena `Precio`)
- Docs: `docs/ONBOARDING.md` (qué cubre la suite completa)

**Interfaces:**
- Consume: todo lo anterior y el arranque de `e2e-stack/platillos.spec.ts` (restaurante `e2e-var-<marca de tiempo>`, `ADMIN_USERNAME`/`ADMIN_PASSWORD` con `"Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)"`, `E2E_BASE_URL`).

- [ ] **Paso 1: Prueba que falla**
  - `test("007/RF-10: el administrador general crea un platillo con precio, agrega una variante y no puede eliminar la última")`. Flujo: entra, `Menú`, crea `Bebidas`, entra a `Platillos`, crea `Agua de horchata` con `Precio` `25` y abre `Variantes`; ve `Regular` con `$25.00`; agrega `1 litro` a `45.50` y la ve después de `Regular`; elimina `Regular` confirmando; intenta eliminar `1 litro` y ve `No se puede eliminar la única variante del platillo`.
- [ ] **Paso 2:** `docker compose up -d --build`, `set -a; . ./.env; set +a` y `(cd apps/web && npx playwright test --config playwright.stack.config.ts e2e-stack/variantes.spec.ts e2e-stack/platillos.spec.ts)` → FAIL antes de tener todo desplegado (si el contenedor no alcanza NuGet, se valida en la aceptación en el servidor).
- [ ] **Paso 3:** ajustar lo necesario (sólo si la prueba revela un fallo real del sistema).
- [ ] **Paso 4:** el mismo comando → PASS; `comando_e2e_stack` completo → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "test(007): prueba de punta a punta de variantes (T-10)"`.

## Cobertura RF → tareas

| RF | Tareas | RF | Tareas | RF | Tareas |
|---|---|---|---|---|---|
| RF-01 | T-01, T-06, T-10 | RF-08 | T-04, T-09 | RF-15 | T-07, T-08 |
| RF-02 | T-03 | RF-09 | T-01, T-02, T-05 | RF-16 | T-08 |
| RF-03 | T-01, T-03 | RF-10 | T-05, T-09, T-10 | RF-17 | T-07, T-09 |
| RF-04 | T-02, T-03 | RF-11 | T-02, T-04, T-05 | RF-18 | T-09 |
| RF-05 | T-02, T-04 | RF-12 | T-03 | RF-19 | T-08 |
| RF-06 | T-03, T-04 | RF-13 | T-05 | | |
| RF-07 | T-04 | RF-14 | T-01 | | |

RNF-01 → T-02, T-03, T-04 · RNF-02 → T-02 (`numeric(10,2)`) y T-03 (JSON estricto) · RNF-03 → proyectos `escritorio`/`movil` (T-08, T-09) · RNF-04 → migración `Variantes` con relleno (T-02) y pruebas de 005/006 en verde (T-05, T-06, T-08).
