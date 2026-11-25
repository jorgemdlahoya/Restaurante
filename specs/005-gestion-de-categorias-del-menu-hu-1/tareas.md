---
feature: 005-gestion-de-categorias-del-menu-hu-1
estado: borrador
---
# Tareas: Gestión de categorías del menú (HU-1)

> **Para agentes:** se ejecuta con `/implementar`, tarea por tarea, con TDD. Cada tarea se cierra
> con `python3 .claude/scripts/completar-tarea.py T-NN` (sólo con `comando_test` en verde). Los
> pasos usan casillas (`- [ ]`).

**Objetivo:** que el administrador general cree, ordene, edite, desactive y elimine las categorías
del menú de su restaurante, con una pantalla que muestra cómo se verá cada título en WhatsApp.

**Arquitectura:** entidad `Categoria` del restaurante (`IEntidadDeRestaurante`) con borrado lógico
mediante una interfaz de marca `IEliminable` que `FiltrosGlobales` combina con el filtro de
restaurante. Endpoints CQRS bajo `/api/r/{slug}/categorias` sólo para `administrador_general`; el 409
lleva `errors` por campo. Pantalla `/r/{slug}/menu/categorias` con tabla paginada, formulario con
vista previa y diálogo de confirmación, siguiendo el patrón de sucursales.

**Stack:** .NET 10, MediatR 12.x, FluentValidation, EF Core + Npgsql, xUnit, Testcontainers;
React + TypeScript + Vite, TanStack Query, Vitest + Testing Library, Playwright.

**Spec:** `specs/005-gestion-de-categorias-del-menu-hu-1/requerimiento.md` y `diseno.md`;
constitución en `specs/000-constitucion/`. El código de 003 (sucursales: entidad, repositorio,
`SucursalesController`, `SucursalesPagina`, `FormularioSucursal`, `e2e/apiSimuladaSucursales.ts`) es
la base: seguir sus patrones.

## Restricciones globales

- Tests con el RF calificado: `[Fact(DisplayName = "005/RF-NN: …")]`, `it("005/RF-NN: …")`, `test("005/RF-NN: …")`.
- Integración con PostgreSQL: `[Trait("Category", "Integracion")]` + `[Collection(PostgresCollection.Nombre)]`; se ejecuta a mano en la tarea que la crea (Docker encendido).
- `dotnet`: anteponer `PATH=/root/.dotnet:$PATH`. EF: `dotnet tool run dotnet-ef -- migrations add <Nombre> --project src/Restaurante.Infrastructure --startup-project src/Restaurante.Infrastructure --output-dir Persistencia/Migraciones` desde `apps/api`.
- Formatos (RF-02): nombre 1–60 tras `Trim()`; nombre corto ≤ 24 tras `Trim()`, vacío o sólo espacios → `null`; orden entero ≥ 1. Longitudes en unidades UTF-16 (`string.Length` / `.length`).
- Normalización para unicidad: `Trim().ToLowerInvariant()` (acentos distintos: "Café" ≠ "Cafe").
- Título WhatsApp (RF-11): nombre corto si existe; si no, el nombre si `Length <= 24`; si no, los primeros 23 caracteres + `"…"`, **sin partir un par sustituto** (si la unidad 23 es un sustituto alto, se corta en 22). Aviso: `Este nombre se verá recortado en WhatsApp`.
- Mensajes de validación: `El nombre es obligatorio`, `El nombre no puede superar 60 caracteres`, `El nombre corto no puede superar 24 caracteres`, `El orden es obligatorio`, `El orden debe ser un entero mayor o igual que 1`, `La página debe ser mayor o igual que 1`, `El tamaño debe estar entre 1 y 100`, `El estado es obligatorio`. Un tipo JSON incorrecto (`orden: 1.5`, `orden: "3"`, `activa: "si"`) da 400 en su campo con `RespuestasProblema.MensajeCuerpoInvalido` (comportamiento actual del enlace del modelo).
- 409: título `Conflicto`, `errors` con `nombre: ["Ya existe una categoría con ese nombre"]` y/o `nombreCorto: ["Ya existe una categoría con ese nombre corto"]`; `detail` = los mensajes unidos por `". "`.
- 404: título `No encontrado`, detalle `Categoría no encontrada` (id inexistente, eliminado o de otro restaurante; nunca 403). 403: `Acceso denegado` de `RequiereRol`.
- Textos de pantalla: `Categorías`, `Nueva categoría`, columnas `Orden`, `Nombre`, `Título en WhatsApp`, `Estado`, `Acciones`; estados `Activa`/`Inactiva`; acciones `Editar`, `Desactivar`/`Activar`, `Eliminar`; paginación `Anterior`/`Siguiente`; confirmación título `¿Eliminar {nombre}?`, mensaje `No se podrá recuperar. Si sólo quieres ocultarla, desactívala.`, botón `Eliminar`; enlace de inicio `Menú`; 403 → `No tienes permiso para esta acción.`
- Art. 5: todo endpoint nuevo actualiza `docs/context/http/categorias.http` (nuevo) y los glosarios (`docs/GLOSSARY.md`, `docs/context/glossary.md`) en la misma tarea; los cambios de esquema o del filtro global, `docs/context/architecture-map.md`.
- Frontend: sólo variables de tema; escritorio 1440 px y móvil 390 px (proyectos de Playwright ya configurados); el token se pasa siempre explícito a `pedirApi`.

## Foco de revisión

1. **Renombrar una categoría cambiando sólo mayúsculas** ("bebidas" → "Bebidas"): no choca consigo misma, 200 (T-05).
2. **Eliminar la única categoría de la última página:** la pantalla vuelve a la página anterior en lugar de mostrar una tabla vacía con "Anterior" (T-09).
3. **`?pagina=abc`, `?tamano=0` o `?tamano=101`:** 400 en `pagina`/`tamano`, nunca 500 ni la clave `cuerpo` (T-04).
4. **Editar con nombre corto `"   "`:** se quita el nombre corto (queda `null`) y la vista previa vuelve a usar el nombre (T-01, T-05, T-07).
5. **Nombre con emojis de más de 24 unidades** (`"🌮"` × 13): el título es `"🌮"` × 11 + `"…"`, sin un carácter roto, igual en C# y en TS (T-01, T-07).

---

### T-01 [RF-02, RF-07, RF-11]: Entidad Categoria y título de WhatsApp

**Archivos:**
- Crear: `apps/api/src/Restaurante.Domain/Comun/IEliminable.cs`
- Crear: `apps/api/src/Restaurante.Domain/Menu/Categoria.cs`, `DatosCategoria.cs`, `TituloWhatsApp.cs`
- Test: `apps/api/tests/Restaurante.Domain.Tests/Menu/CategoriaTests.cs`, `TituloWhatsAppTests.cs`
- Docs: glosarios (Categoría, nombre corto, título en WhatsApp, borrado lógico)

**Interfaces:**
- Produce: `interface IEliminable { DateTimeOffset? EliminadaEn { get; } }`.
- Produce: `sealed record DatosCategoria(string Nombre, string? NombreCorto, int Orden)`.
- Produce: `Categoria : IEntidadDeRestaurante, IEliminable` con `const int LongitudMaximaNombre = 60`, `const int LongitudMaximaNombreCorto = 24`, `static string Normalizar(string texto)` (`Trim().ToLowerInvariant()`), `static Categoria Crear(Guid restauranteId, DatosCategoria datos, DateTimeOffset creadaEn)`, `void Editar(DatosCategoria datos)`, `void Activar()`, `void Desactivar()`, `void Eliminar(DateTimeOffset ahora)`. Propiedades `Id`, `RestauranteId`, `Nombre`, `NombreNormalizado`, `NombreCorto`, `NombreCortoNormalizado`, `Orden`, `Activa` (empieza en `true`), `CreadaEn`, `EliminadaEn`. `Crear`/`Editar` recortan, convierten nombre corto vacío en `null`, recalculan los normalizados y lanzan `ArgumentException` si un formato no se cumple. Constructor privado para EF.
- Produce: `static class TituloWhatsApp { const int Limite = 24; static string Calcular(string nombre, string? nombreCorto); static bool SeRecorta(string nombre, string? nombreCorto); }` (regla de las restricciones globales).

- [ ] **Paso 1: Pruebas que fallan**
  - `"005/RF-02: una categoría nueva está activa y con datos recortados"` → `Crear(rid, new("  Pozoles ", "   ", 1), t)` da `Nombre == "Pozoles"`, `NombreNormalizado == "pozoles"`, `NombreCorto == null`, `NombreCortoNormalizado == null`, `Activa`, `EliminadaEn == null`.
  - `"005/RF-02: Crear y Editar rechazan formatos inválidos"` (Theory) → `ArgumentException` con nombre `""`, `"   "`, 61 × `"a"`; nombre corto 25 × `"a"`; orden `0`, `-1`. Acepta nombre 60 × `"a"` y nombre corto 24 × `"a"`.
  - `"005/RF-02: Editar con nombre corto en blanco lo quita"` (Foco 4).
  - `"005/RF-07: eliminar marca la fecha y conserva los datos"`.
  - `TituloWhatsAppTests` `"005/RF-11: calcula el título en WhatsApp"` (Theory, la misma tabla que T-07):
    | nombre | nombreCorto | título | seRecorta |
    |---|---|---|---|
    | `"Pozoles tradicionales de la casa"` | `"Pozoles"` | `"Pozoles"` | false |
    | 24 × `"a"` | null | 24 × `"a"` | false |
    | 25 × `"a"` | null | 23 × `"a"` + `"…"` | true |
    | `"Pozoles tradicionales de la casa"` | null | `"Pozoles tradicionales d…"` | true |
    | 12 × `"🌮"` | null | 12 × `"🌮"` | false |
    | 13 × `"🌮"` | null | 11 × `"🌮"` + `"…"` | true |
- [ ] **Paso 2:** `PATH=/root/.dotnet:$PATH dotnet test apps/api/tests/Restaurante.Domain.Tests --filter "DisplayName~005/"` → no compila.
- [ ] **Paso 3:** implementar en `Restaurante.Domain` (sin paquetes externos).
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): entidad Categoria y título de WhatsApp (T-01)"`.

### T-02 [RF-03, RF-04, RF-07, RF-08]: Persistencia de categorías con borrado lógico en el filtro global

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Abstracciones/ICategoriaRepository.cs`
- Crear: `apps/api/src/Restaurante.Application/Comun/Excepciones/ConflictoCamposException.cs`
- Crear: `apps/api/src/Restaurante.Infrastructure/Persistencia/CategoriaConfiguration.cs`, `CategoriaRepository.cs`
- Modificar: `apps/api/src/Restaurante.Infrastructure/Persistencia/FiltrosGlobales.cs` (añade `&& e.EliminadaEn == null` para `IEliminable`), `RestauranteDbContext.cs` (`DbSet<Categoria>`), `apps/api/src/Restaurante.Infrastructure/DependencyInjection.cs`
- Crear: migración `Categorias`
- Test: `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/CategoriasIntegracionTests.cs`
- Docs: `docs/context/architecture-map.md` (tabla `categorias`, filtro de borrado lógico)

**Interfaces:**
- Consume: `Categoria`, `IEliminable` (T-01).
- Produce: `sealed class ConflictoCamposException(IReadOnlyDictionary<string, string[]> errores) : Exception` con `Errores` y `Message` = mensajes unidos por `". "`; `static class MensajesCategoria { const string NombreRepetido = "Ya existe una categoría con ese nombre"; const string NombreCortoRepetido = "Ya existe una categoría con ese nombre corto"; const string NoEncontrada = "Categoría no encontrada"; }` en `Application/Features/Categorias/MensajesCategoria.cs`.
- Produce (`ICategoriaRepository`, todo dentro del restaurante del contexto y sin eliminadas, por el filtro global):
  ```csharp
  Task<Categoria?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
  Task<(IReadOnlyList<Categoria> Elementos, int Total)> ListarAsync(int pagina, int tamano, CancellationToken ct); // Orden, Nombre; Skip/Take
  Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct);
  Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct);
  void Agregar(Categoria categoria);
  Task GuardarCambiosAsync(CancellationToken ct);
  ```
- Produce el esquema del diseño: tabla `categorias`, `ck_categorias_orden` (`orden >= 1`), únicos parciales `ix_categorias_restaurante_nombre` (`eliminada_en IS NULL`) y `ix_categorias_restaurante_nombre_corto` (`eliminada_en IS NULL AND nombre_corto IS NOT NULL`), índice `ix_categorias_restaurante_orden_nombre`. `GuardarCambiosAsync` traduce `UniqueViolation` por `ConstraintName` a `ConflictoCamposException` (`nombreCorto` para el índice del nombre corto; cualquier otro → `nombre`).

- [ ] **Paso 1: Pruebas de integración que fallan**
  - `"005/RF-07: una categoría eliminada no se ve y libera su nombre"` → crear "Bebidas", `Eliminar`, guardar; `ObtenerPorIdAsync` → `null`, `ListarAsync` no la trae, `ExisteNombreAsync("bebidas", null)` → `false`, y una nueva "Bebidas" se guarda (el índice parcial lo permite).
  - `"005/RF-03: los índices únicos traducen a ConflictoCamposException por campo"` → nombre repetido con otra capitalización → `Errores` con `nombre`; nombre corto repetido → `nombreCorto`; dos categorías sin nombre corto se guardan.
  - `"005/RF-08: las categorías de otro restaurante no se ven"` → con contexto de R2, `ObtenerPorIdAsync(idDeR1)` → `null` y `ListarAsync` vacío.
  - `"005/RF-04: la lista pagina y ordena por orden y nombre"` → orden 2 "B", 1 "Z", 1 "A"; `ListarAsync(1, 2)` → `["A", "Z"]`, `Total == 3`; `ListarAsync(2, 2)` → `["B"]`.
  - `"005/RF-02: la base rechaza orden menor que 1"` → insertar con SQL directo `orden = 0` falla por `ck_categorias_orden`.
  - Las pruebas de integración de 003/004 siguen en verde (el filtro de `Sucursal`/`CierreTemporal` no cambia).
- [ ] **Paso 2:** `dotnet test apps/api --filter "Category=Integracion&DisplayName~005/"` → no compila.
- [ ] **Paso 3:** implementar; `dotnet tool run dotnet-ef -- migrations add Categorias …`; registrar el repositorio.
- [ ] **Paso 4:** `dotnet test apps/api --filter "Category=Integracion"` → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): persistencia de categorías con borrado lógico (T-02)"`.

### T-03 [RF-01, RF-02, RF-03, RF-09]: Alta de categoría

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Categorias/CategoriaDto.cs`, `DatosCategoriaDto.cs`, `ValidadorDatosCategoria.cs`, `ComprobadorUnicidadCategoria.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Categorias/CrearCategoria/CrearCategoriaCommand.cs`, `CrearCategoriaHandler.cs`, `CrearCategoriaValidator.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/CategoriasController.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Filtros/FiltroExcepciones.cs` (`ConflictoCamposException` → 409 con `errors`)
- Crear: dobles `CategoriaRepositoryEnMemoria` en `apps/api/tests/Restaurante.Application.Tests/Dobles/` y `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/` (el de WebApi filtra por el `IContextoRestaurante` de la petición y por `EliminadaEn == null`; `FabricaApi.Categorias` como almacén compartido)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Categorias/CrearCategoriaTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Categorias/CategoriasEndpointsTests.cs`
- Docs: `docs/context/http/categorias.http` (nuevo), glosarios (`CategoriaDto`)

**Interfaces:**
- Consume: `Categoria`, `DatosCategoria` (T-01), `ICategoriaRepository`, `ConflictoCamposException`, `MensajesCategoria` (T-02), `IContextoRestaurante`, `IReloj`.
- Produce: `sealed record DatosCategoriaDto(string? Nombre, string? NombreCorto, int? Orden)`; `ValidadorDatosCategoria : AbstractValidator<DatosCategoriaDto>` con los mensajes de las restricciones globales (claves `nombre`, `nombreCorto`, `orden`, sin prefijo).
- Produce: `sealed record CategoriaDto(Guid Id, string Nombre, string? NombreCorto, int Orden, bool Activa)` con `static Desde(Categoria)`.
- Produce: `ComprobadorUnicidadCategoria(ICategoriaRepository repo)` con `Task ComprobarAsync(string nombre, string? nombreCorto, Guid? excluirId, CancellationToken ct)`: consulta ambas unicidades y lanza **una** `ConflictoCamposException` con todos los campos repetidos.
- Produce: `CrearCategoriaCommand(DatosCategoriaDto Datos) : IRequest<CategoriaDto>`.
- Produce: `POST /api/r/{slug}/categorias` con cuerpo `{ nombre, nombreCorto?, orden }` (`CrearCategoriaPeticion(string? Nombre, string? NombreCorto, int? Orden)`) → 201, `Location: /api/r/{slug}/categorias/{id}`; controller con `[RequiereRol(Rol.AdministradorGeneral)]` a nivel de clase. El filtro traduce `ConflictoCamposException` a `ProblemDetails` 409 con `Extensions["errors"]` (claves en camelCase).

- [ ] **Paso 1: Pruebas que fallan**
  - `"005/RF-01: crea la categoría activa en el restaurante del contexto"` (handler) → `RestauranteId` del contexto, `CreadaEn` del reloj, un guardado.
  - `"005/RF-01: POST devuelve 201 con Location y la categoría"` → cuerpo `{ nombre: "Pozoles", nombreCorto: "Pozole", orden: 1 }`; respuesta con `id`, `nombre`, `nombreCorto`, `orden`, `activa: true`.
  - `"005/RF-02: datos inválidos dan 400 por campo"` (Theory, `AssertValidacionAsync`) → `nombre` (`""`, 61 × `"a"`), `nombreCorto` (25 × `"a"`), `orden` (ausente, `0`, `-1`, `1.5`, `"3"`, `"abc"`).
  - `"005/RF-02: un nombre corto en blanco se guarda como ausente"` → `nombreCorto: "  "` → respuesta `nombreCorto: null`.
  - `"005/RF-03: nombre repetido sin distinguir mayúsculas ni espacios da 409 en nombre"` → existe "Bebidas"; alta `" bebidas "` → 409, título `Conflicto`, `errors.nombre == ["Ya existe una categoría con ese nombre"]`, sin `errors.nombreCorto`.
  - `"005/RF-03: nombre y nombre corto repetidos dan 409 con los dos campos"`.
  - `"005/RF-09: un rol distinto de administrador_general recibe 403"` (Theory: `cajero`, `cocinero`, `administrador_sucursal`, y token de plataforma).
- [ ] **Paso 2:** `dotnet test apps/api --filter "DisplayName~005/RF-01|DisplayName~005/RF-02|DisplayName~005/RF-03|DisplayName~005/RF-09"` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): alta de categoría (T-03)"`.

### T-04 [RF-04, RF-09]: Lista paginada de categorías

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Categorias/ListarCategorias/ListarCategoriasQuery.cs`, `ListarCategoriasHandler.cs`, `ListarCategoriasValidator.cs`, `PaginaCategoriasDto.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/CategoriasController.cs`, `apps/api/src/Restaurante.WebApi/Filtros/RespuestasProblema.cs` (fallo de enlace de un parámetro de consulta conserva su nombre como clave)
- Test: `apps/api/tests/Restaurante.WebApi.Tests/Categorias/CategoriasEndpointsTests.cs`
- Docs: `docs/context/http/categorias.http`

**Interfaces:**
- Consume: `CategoriaDto` (T-03), `ICategoriaRepository.ListarAsync` (T-02).
- Produce: `ListarCategoriasQuery(int Pagina, int Tamano) : IRequest<PaginaCategoriasDto>`; `sealed record PaginaCategoriasDto(IReadOnlyList<CategoriaDto> Elementos, int Total, int Pagina, int Tamano)`.
- Produce: `GET /api/r/{slug}/categorias?pagina=&tamano=` (`[FromQuery] int? pagina, int? tamano`, nulos → 1 y 20) → 200 `{ elementos, total, pagina, tamano }`.
- `RespuestasProblema.Validacion(ModelStateDictionary)`: una clave que no empieza por `$.` y no está vacía ni es el nombre del parámetro del cuerpo (p. ej. `pagina`) se conserva; las de cuerpo siguen como hoy (las pruebas de 002–004 no cambian).

- [ ] **Paso 1: Pruebas que fallan**
  - `"005/RF-04: GET devuelve las no eliminadas, activas e inactivas, por orden y nombre"` → siembra activas, una inactiva y una eliminada; la eliminada no sale y la inactiva sí.
  - `"005/RF-04: por defecto página 1 de 20 con el total"` → 25 categorías: 20 elementos, `total: 25`, `pagina: 1`, `tamano: 20`; `?pagina=2` → 5.
  - `"005/RF-04: parámetros fuera de rango dan 400"` (Theory) → `pagina=0` y `pagina=abc` → `errors.pagina`; `tamano=0`, `tamano=101`, `tamano=abc` → `errors.tamano` (Foco 3).
  - `"005/RF-04: no lista categorías de otro restaurante"` (RNF-01).
  - `"005/RF-09: listar exige administrador_general"` → 403 con `cajero`.
- [ ] **Paso 2:** filtro `DisplayName~005/RF-04|DisplayName~005/RF-09` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): lista paginada de categorías (T-04)"`.

### T-05 [RF-05, RF-06, RF-08]: Editar y activar o desactivar categorías

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Categorias/EditarCategoria/*`, `CambiarEstadoCategoria/*`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/CategoriasController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Categorias/EditarCategoriaTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Categorias/CategoriasEndpointsTests.cs`
- Docs: `docs/context/http/categorias.http`

**Interfaces:**
- Consume: `CategoriaDto`, `DatosCategoriaDto`, `ValidadorDatosCategoria`, `ComprobadorUnicidadCategoria` (T-03).
- Produce: `EditarCategoriaCommand(Guid Id, DatosCategoriaDto Datos) : IRequest<CategoriaDto>` → `PUT /api/r/{slug}/categorias/{id:guid}`; orden de errores: 400 (pipeline) → 404 `NoEncontradoException(MensajesCategoria.NoEncontrada)` → 409 (excluyendo su propio id).
- Produce: `CambiarEstadoCategoriaCommand(Guid Id, bool? Activa) : IRequest<CategoriaDto>` → `PUT /api/r/{slug}/categorias/{id:guid}/estado` con `{ activa }` (`CambiarEstadoCategoriaPeticion(bool? Activa)`); sin `activa` → 400 `El estado es obligatorio`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"005/RF-05: editar cambia nombre, nombre corto y orden"` → 200 con los nuevos valores.
  - `"005/RF-05: renombrar cambiando sólo mayúsculas no choca consigo misma"` (Foco 1).
  - `"005/RF-05: editar con el nombre de otra categoría da 409 en nombre"` y con datos inválidos → 400 por campo.
  - `"005/RF-05: editar con nombre corto en blanco lo quita"` (Foco 4).
  - `"005/RF-06: desactivar mantiene la categoría en la lista y su nombre reservado"` → tras `activa: false`, GET la trae con `activa: false` y un alta con su nombre → 409; reactivar → `activa: true`.
  - `"005/RF-06: estado sin activa o no booleano da 400 en activa"` (Theory: `{}`, `{ activa: "si" }`).
  - `"005/RF-08: editar o cambiar estado de una categoría de otro restaurante, inexistente o eliminada da 404"` (Theory).
- [ ] **Paso 2:** filtro `DisplayName~005/RF-05|DisplayName~005/RF-06|DisplayName~005/RF-08` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): editar y cambiar estado de categorías (T-05)"`.

### T-06 [RF-07, RF-08, RF-09]: Eliminar categoría

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Categorias/EliminarCategoria/EliminarCategoriaCommand.cs`, `EliminarCategoriaHandler.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/CategoriasController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Categorias/EliminarCategoriaTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Categorias/CategoriasEndpointsTests.cs`
- Docs: `docs/context/http/categorias.http`

**Interfaces:**
- Consume: `ICategoriaRepository`, `MensajesCategoria` (T-02), `IReloj`.
- Produce: `EliminarCategoriaCommand(Guid Id) : IRequest` → `DELETE /api/r/{slug}/categorias/{id:guid}` → 204 (`Categoria.Eliminar(reloj.Ahora)`, sin borrado físico).

- [ ] **Paso 1: Pruebas que fallan**
  - `"005/RF-07: eliminar responde 204, la quita de la lista y libera sus nombres"` → tras DELETE, GET no la trae y un alta con el mismo nombre y nombre corto → 201; el almacén conserva la fila con `EliminadaEn`.
  - `"005/RF-08: eliminar una categoría de otro restaurante, inexistente o ya eliminada da 404"` (Theory; un segundo DELETE → 404).
  - `"005/RF-09: eliminar exige administrador_general"` → 403 con `administrador_sucursal`.
- [ ] **Paso 2:** filtro `DisplayName~005/RF-07|DisplayName~005/RF-08|DisplayName~005/RF-09` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): eliminar categoría (T-06)"`.

### T-07 [RF-11, RF-12]: Formulario de categoría con vista previa de WhatsApp

**Archivos:**
- Crear: `apps/web/src/menu/tituloWhatsApp.ts`, `api.ts`, `VistaPreviaWhatsApp.tsx`, `FormularioCategoria.tsx` (+ `.module.css`)
- Test: `apps/web/src/menu/tituloWhatsApp.test.ts`, `FormularioCategoria.test.tsx`

**Interfaces:**
- Produce (`tituloWhatsApp.ts`): `export const LIMITE_TITULO_WHATSAPP = 24; export function tituloWhatsApp(nombre: string, nombreCorto: string | null): string; export function seRecorta(nombre: string, nombreCorto: string | null): boolean;` (misma regla que T-01; nombre y nombre corto se recortan antes, y un nombre corto en blanco cuenta como ausente).
- Produce (`api.ts`): `type Categoria = { id: string; nombre: string; nombreCorto: string | null; orden: number; activa: boolean }`; `type DatosCategoria = { nombre: string; nombreCorto: string | null; orden: number }`; `type PaginaCategorias = { elementos: Categoria[]; total: number; pagina: number; tamano: number }`; `claveCategorias(slug: string, pagina?: number)`; `listarCategorias(token, slug, pagina): Promise<PaginaCategorias>` (`tamano=20`); `crearCategoria(token, slug, datos)`, `editarCategoria(token, slug, id, datos)`, `cambiarEstadoCategoria(token, slug, id, activa)`, `eliminarCategoria(token, slug, id): Promise<void>`.
- Produce: `FormularioCategoria({ slugRestaurante, token, categoria?, onGuardada, onCancelar })` con campos `Nombre`, `Nombre corto (opcional)`, `Orden` y botón `Guardar`; `VistaPreviaWhatsApp({ nombre, nombreCorto })` con el texto `Así se verá en WhatsApp: {título}` y, si `seRecorta`, el aviso `Este nombre se verá recortado en WhatsApp`.

- [ ] **Paso 1: Pruebas que fallan**
  - `tituloWhatsApp.test.ts` `"005/RF-11: calcula el título en WhatsApp"` → `it.each` con la misma tabla de T-01 (RNF-02, Foco 5).
  - `"005/RF-11: la vista previa sigue lo que se escribe y avisa del recorte"` → escribir `Pozoles tradicionales de la casa` muestra `Pozoles tradicionales d…` y el aviso; escribir `Pozoles` en nombre corto lo cambia a `Pozoles` y quita el aviso; dejar el nombre corto en `"   "` vuelve al nombre (Foco 4).
  - `"005/RF-12: un orden inválido se marca junto al campo sin llamar a la API"` (`it.each`: `""`, `"0"`, `"-1"`, `"1.5"`, `"abc"`) → `El orden debe ser un entero mayor o igual que 1` y `fetch` no llamado.
  - `"005/RF-12: los errores 400 y 409 del servidor se muestran junto a su campo"` (`it.each` sobre 400 y 409 con `errors: { nombre: [...], nombreCorto: [...] }`).
  - `"005/RF-12: en edición envía PUT con los datos y nombre corto en blanco como null"`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run src/menu)` → FAIL.
- [ ] **Paso 3:** implementar (reusar `CampoFormulario`; errores de servidor para estado 400 **o** 409).
- [ ] **Paso 4:** el mismo comando → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): formulario de categoría con vista previa de WhatsApp (T-07)"`.

### T-08 [RF-10, RF-11, RF-12]: Pantalla de categorías: lista, paginación, alta y edición
UI: sí

**Archivos:**
- Crear: `apps/web/src/menu/CategoriasPagina.tsx` (+ `.module.css`), `apps/web/e2e/apiSimuladaCategorias.ts`, `apps/web/e2e/categorias.spec.ts`
- Modificar: `apps/web/src/App.tsx` (ruta hija `menu/categorias` de `/r/:slug`)
- Test: `apps/web/src/menu/CategoriasPagina.test.tsx`

**Interfaces:**
- Consume: `api.ts`, `FormularioCategoria`, `tituloWhatsApp` (T-07), `MarcoRestaurante`, `RutaProtegida`, `useSesion`.
- Produce: `CategoriasPagina` (sólo `administrador_general`, `redirigirSinRol` → `/r/{slug}/inicio`); estado de página en el componente, `useQuery` con `claveCategorias(slug, pagina)`; `Anterior` deshabilitado en la página 1 y `Siguiente` deshabilitado cuando `pagina * tamano >= total`.
- Produce (`apiSimuladaCategorias.ts`): `type CategoriaSimulada`, `categoria(parcial)`, `simularCategorias(page, slug, iniciales, opciones?: { erroresAlta?: { estado: 400 | 409; errors: Record<string, string[]> } })` que simula GET paginado (`pagina`, `tamano`, orden por `orden` y `nombre`), POST, PUT `/{id}`, PUT `/{id}/estado` y DELETE en memoria, y devuelve los registros de peticiones.

- [ ] **Paso 1: Pruebas Playwright que fallan (commit sola)**
  - `test("005/RF-10: el administrador general ve la tabla de categorías paginada")` → 22 categorías: tabla con columnas `Orden`, `Nombre`, `Título en WhatsApp`, `Estado`; 20 filas; `Siguiente` muestra 2; `Anterior` vuelve.
  - `test("005/RF-11: el alta muestra la vista previa y crea la categoría")` → `Nueva categoría`, escribir un nombre de 32 caracteres: aviso visible; guardar: la fila aparece con su título recortado.
  - `test("005/RF-12: un 409 del servidor se muestra junto al nombre")`.
  - `test("005/RF-11: editar cambia el nombre corto y el título")`.
  - Vitest `CategoriasPagina.test.tsx`: `"005/RF-10: Anterior deshabilitado en la primera página y Siguiente en la última"`.
- [ ] **Paso 2:** `(cd apps/web && npx playwright test e2e/categorias.spec.ts)` → FAIL; commit de las pruebas solas.
- [ ] **Paso 3:** Ralph Loop (`/implementar`).
- [ ] **Paso 4:** las pruebas → PASS en `escritorio` y `movil`; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): pantalla de categorías con lista, alta y edición (T-08)"`.

### T-09 [RF-06, RF-13, RF-14]: Activar, desactivar, eliminar con confirmación y enlace "Menú"
UI: sí

**Archivos:**
- Modificar: `apps/web/src/menu/CategoriasPagina.tsx`, `apps/web/src/restaurante/InicioRestaurante.tsx` (enlace `Menú` para `administrador_general`)
- Modificar: `apps/web/e2e/categorias.spec.ts`
- Test: `apps/web/src/menu/CategoriasPagina.test.tsx`

**Interfaces:**
- Consume: `cambiarEstadoCategoria`, `eliminarCategoria` (T-07), `simularCategorias` (T-08), `DialogoConfirmacion`.
- Produce: acciones por fila `Desactivar`/`Activar` (sin confirmación) y `Eliminar` (con `DialogoConfirmacion` y los textos de las restricciones globales); tras eliminar, si la página queda vacía y `pagina > 1`, pasa a `pagina - 1` (Foco 2); un 403 muestra `No tienes permiso para esta acción.`

- [ ] **Paso 1: Pruebas Playwright que fallan (commit sola)**
  - `test("005/RF-13: eliminar pide confirmación y quita la fila")` → `Eliminar` abre el diálogo `¿Eliminar Bebidas?` con el mensaje; `Cancelar` no llama a la API; `Eliminar` hace DELETE y la fila desaparece.
  - `test("005/RF-13: desactivar y activar no piden confirmación")` → el estado pasa a `Inactiva` y vuelve a `Activa`, sin diálogo.
  - `test("005/RF-13: eliminar la única categoría de la última página vuelve a la anterior")` (21 categorías, Foco 2).
  - `test("005/RF-14: el administrador general ve el enlace Menú en el inicio")` → lleva a `/r/la-esquina/menu/categorias`.
  - `test("005/RF-14: otro rol que abre las categorías vuelve al inicio")` (cajero) → `/r/la-esquina/inicio` sin enlace `Menú`.
- [ ] **Paso 2:** `(cd apps/web && npx playwright test e2e/categorias.spec.ts)` → FAIL en las nuevas; commit de las pruebas solas.
- [ ] **Paso 3:** Ralph Loop (`/implementar`).
- [ ] **Paso 4:** las pruebas → PASS en `escritorio` y `movil`; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(005): estado, eliminación y enlace Menú (T-09)"`.

### T-10 [RF-01, RF-05, RF-07]: Prueba de punta a punta de categorías

**Archivos:**
- Crear: `apps/web/e2e-stack/categorias.spec.ts`
- Docs: `docs/ONBOARDING.md` (qué cubre la suite completa)

**Interfaces:**
- Consume: todo lo anterior, `ADMIN_USERNAME`/`ADMIN_PASSWORD` del entorno (falla con `"Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)"` si faltan), `E2E_BASE_URL` y `E2E_API_URL`, como `e2e-stack/sucursales.spec.ts`.

- [ ] **Paso 1: Prueba que falla**
  - `test("005/RF-07: el administrador general crea, reordena y elimina categorías y reutiliza el nombre")`. Flujo: la plataforma crea un restaurante `e2e-cat-<marca de tiempo>` con su administrador; el administrador entra, pulsa `Menú`, crea `Bebidas` (orden 1) y `Pozoles` (orden 2); edita `Bebidas` a orden 3 y comprueba que `Pozoles` queda en la primera fila; elimina `Bebidas` confirmando; crea otra `Bebidas` (orden 2) y la ve en la tabla (RF-01, RF-05, RF-07).
- [ ] **Paso 2:** `docker compose up -d --build`, `set -a; . ./.env; set +a` y `(cd apps/web && npx playwright test --config playwright.stack.config.ts e2e-stack/categorias.spec.ts)` → FAIL antes de tener las pantallas desplegadas.
- [ ] **Paso 3:** ajustar lo necesario (sólo si la prueba revela un fallo real del sistema).
- [ ] **Paso 4:** el mismo comando → PASS; `comando_e2e_stack` completo → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "test(005): prueba de punta a punta de categorías (T-10)"`.

## Cobertura RF → tareas

| RF | Tareas | RF | Tareas | RF | Tareas |
|---|---|---|---|---|---|
| RF-01 | T-03, T-10 | RF-06 | T-05, T-09 | RF-11 | T-01, T-07, T-08 |
| RF-02 | T-01, T-02, T-03 | RF-07 | T-01, T-02, T-06, T-10 | RF-12 | T-07, T-08 |
| RF-03 | T-02, T-03 | RF-08 | T-02, T-05, T-06 | RF-13 | T-09 |
| RF-04 | T-02, T-04 | RF-09 | T-03, T-04, T-06 | RF-14 | T-09 |
| RF-05 | T-05, T-10 | RF-10 | T-08 | | |

RNF-01 → T-02, T-04, T-05, T-06 · RNF-02 → T-01 y T-07 (misma tabla de casos) · RNF-03 → proyectos `escritorio`/`movil` (T-08, T-09) · RNF-04 → migración `Categorias` (T-02).
