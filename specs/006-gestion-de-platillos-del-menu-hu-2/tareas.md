---
feature: 006-gestion-de-platillos-del-menu-hu-2
estado: borrador
---
# Tareas: Gestión de platillos del menú (HU-2)

> **Para agentes:** se ejecuta con `/implementar`, tarea por tarea, con TDD. Cada tarea se cierra
> con `python3 .claude/scripts/completar-tarea.py T-NN` (sólo con `comando_test` en verde). Los
> pasos usan casillas (`- [ ]`).

**Objetivo:** que el administrador general agregue, ordene, edite, mueva, desactive y elimine los
platillos de cada categoría, con descripción e ingredientes incluidos, y que no pueda eliminar una
categoría que todavía tiene platillos.

**Arquitectura:** entidad `Platillo` del restaurante (`IEntidadDeRestaurante`, `IEliminable`) que
recibe el filtro global de 003/005 sin cambios; ingredientes como `text[]` en la misma fila.
Endpoints CQRS en `PlatillosController` (`/categorias/{id}/platillos` y `/platillos/{id}`), sólo para
`administrador_general`; `EliminarCategoria` de 005 cuenta los platillos antes de eliminar. Pantalla
`/r/{slug}/menu/categorias/{categoriaId}/platillos` con el patrón de la de categorías.

**Stack:** .NET 10, MediatR 12.x, FluentValidation, EF Core + Npgsql, xUnit, Testcontainers;
React + TypeScript + Vite, TanStack Query, Vitest + Testing Library, Playwright.

**Spec:** `specs/006-gestion-de-platillos-del-menu-hu-2/requerimiento.md` y `diseno.md`;
constitución en `specs/000-constitucion/`. El código de 005 (`Categoria`, `CategoriaRepository`,
`CategoriasController`, `ConflictoCamposException`, `CategoriasPagina`, `FormularioCategoria`,
`VistaPreviaWhatsApp`, `mensajeError.ts`, `e2e/apiSimuladaCategorias.ts`) es la base: seguir sus
patrones.

## Restricciones globales

- Tests con el RF calificado: `[Fact(DisplayName = "006/RF-NN: …")]`, `it("006/RF-NN: …")`, `test("006/RF-NN: …")`.
- Integración con PostgreSQL: `[Trait("Category", "Integracion")]` + `[Collection(PostgresCollection.Nombre)]`; se ejecuta a mano en la tarea que la crea (Docker encendido; `dockerd` si hace falta).
- `dotnet`: anteponer `PATH=/root/.dotnet:$PATH`. EF: `dotnet tool run dotnet-ef -- migrations add <Nombre> --project src/Restaurante.Infrastructure --startup-project src/Restaurante.Infrastructure --output-dir Persistencia/Migraciones` desde `apps/api`.
- Formatos (RF-02), en unidades UTF-16 tras `Trim()`: nombre 1–60; nombre corto ≤ 24, vacío → `null`; descripción ≤ 300, vacía → `null`; orden entero ≥ 1 (JSON estricto: `[JsonNumberHandling(Strict)]` en la propiedad, como en 005); ingredientes ≤ 30, cada uno 1–40, sin repetidos según `Trim().ToLowerInvariant()`, guardados recortados y en su orden; ausentes → lista vacía.
- Normalización para unicidad de nombre y nombre corto: `Trim().ToLowerInvariant()`, entre los platillos no eliminados del restaurante.
- Mensajes 400: `El nombre es obligatorio`, `El nombre no puede superar 60 caracteres`, `El nombre corto no puede superar 24 caracteres`, `La descripción no puede superar 300 caracteres`, `El orden es obligatorio`, `El orden debe ser un entero mayor o igual que 1`, `No puede haber más de 30 ingredientes`, `El ingrediente {n} está vacío`, `El ingrediente {n} no puede superar 40 caracteres`, `El ingrediente {n} está repetido` (`{n}` empieza en 1; clave `ingredientes`), `La categoría es obligatoria`, `La categoría no existe`, `La categoría está inactiva` (clave `categoriaId`), `El estado es obligatorio` (clave `activo`), y los de `pagina`/`tamano` de 005.
- 409 por campo: `nombre: ["Ya existe un platillo con ese nombre"]`, `nombreCorto: ["Ya existe un platillo con ese nombre corto"]` (con `ConflictoCamposException` de 005).
- 409 de categoría con platillos: título `Conflicto`, detalle `Esta categoría tiene N platillos` (`Esta categoría tiene 1 platillo` si N = 1), extensión `platillos: N`.
- 404: título `No encontrado`, detalle `Platillo no encontrado` o `Categoría no encontrada`; nunca 403 por otro restaurante. 403: `Acceso denegado` de `RequiereRol`.
- API: el estado del platillo es `activo` (masculino); respuesta `PlatilloDto { id, categoriaId, categoriaNombre, nombre, nombreCorto, descripcion, orden, ingredientes, activo }`; lista `{ elementos, total, pagina, tamano }` con `tamano` 20 por defecto.
- Textos de pantalla: acción de categoría `Platillos`; título `Platillos de {categoría}`; enlace `Categorías`; `Nuevo platillo`; columnas `Orden`, `Nombre`, `Título en WhatsApp`, `Ingredientes`, `Estado`, `Acciones`; estados `Activo`/`Inactivo`; acciones `Editar`, `Desactivar`/`Activar`, `Eliminar`; `Anterior`/`Siguiente`; aviso `Esta categoría está inactiva. Sus platillos no se muestran a los clientes.`; campos `Nombre`, `Nombre corto (opcional)`, `Descripción (opcional)`, `Orden`, `Categoría`, `Ingredientes incluidos`; botones `Agregar ingrediente`, `Quitar`, `Guardar`, `Cancelar`; avisos del editor `Escribe un ingrediente`, `Ese ingrediente ya está en la lista`, `No puede haber más de 30 ingredientes`; confirmación `¿Eliminar {nombre}?` / `No se podrá recuperar. Si sólo quieres ocultarlo, desactívalo.`; aviso de categoría con platillos: título `No se puede eliminar {categoría}`, mensaje `Esta categoría tiene N platillos`, botones `Desactivar categoría` (sólo si está activa) y `Cancelar`; `Categoría no encontrada`; opción del selector `{nombre} (inactiva)`; 403 → `No tienes permiso para esta acción.` (`mensajeDeError` de 005).
- RNF-02: el título en WhatsApp usa `TituloWhatsApp` (C#) y `tituloWhatsApp` (TS) de 005; no se copian.
- Art. 5: todo endpoint nuevo o cambiado actualiza `docs/context/http/platillos.http` (nuevo) o `categorias.http`, y los glosarios (`docs/GLOSSARY.md`, `docs/context/glossary.md`) en la misma tarea; los cambios de esquema y de pantallas, `docs/context/architecture-map.md`.
- Frontend: sólo variables de tema; escritorio 1440 px y móvil 390 px; el token se pasa siempre explícito a `pedirApi`.

## Foco de revisión

1. **Editar un platillo cuya categoría está inactiva sin moverlo:** 200; moverlo a otra categoría inactiva: 400 en `categoriaId` (T-05).
2. **Ingredientes `" crema "` y `"Crema"` en la misma lista:** 400 `El ingrediente 2 está repetido` en el servidor y aviso `Ese ingrediente ya está en la lista` en el editor (T-01, T-07).
3. **Eliminar el único platillo de una categoría y después la categoría:** 204; los platillos eliminados no cuentan (T-06).
4. **Un platillo con el mismo nombre que una categoría** ("Pozoles"): se acepta, la unicidad es entre platillos (T-03).
5. **Abrir la pantalla de platillos de una categoría de otro restaurante o eliminada:** `Categoría no encontrada` con el enlace `Categorías`, sin error en blanco (T-08).

---

### T-01 [RF-02, RF-09]: Entidad Platillo e ingredientes

**Archivos:**
- Crear: `apps/api/src/Restaurante.Domain/Menu/Platillo.cs`, `DatosPlatillo.cs`, `IngredientesPlatillo.cs`
- Test: `apps/api/tests/Restaurante.Domain.Tests/Menu/PlatilloTests.cs`, `IngredientesPlatilloTests.cs`
- Docs: glosarios (Platillo, ingredientes incluidos)

**Interfaces:**
- Consume: `IEntidadDeRestaurante`, `IEliminable` (`EliminadaEn`), `Categoria.Normalizar` (005).
- Produce: `sealed record DatosPlatillo(string Nombre, string? NombreCorto, string? Descripcion, int Orden, IReadOnlyList<string> Ingredientes)`.
- Produce: `static class IngredientesPlatillo { const int MaximoElementos = 30; const int LongitudMaxima = 40; static IReadOnlyList<string> Errores(IReadOnlyList<string?>? ingredientes); static IReadOnlyList<string> Normalizar(IReadOnlyList<string?>? ingredientes); }`. `Errores` devuelve los mensajes de las restricciones globales en orden (vacía si todo está bien); `Normalizar` recorta y lanza `ArgumentException` si `Errores` no está vacía. Es la única fuente de esas reglas: el validador de T-03 la usa.
- Produce: `Platillo : IEntidadDeRestaurante, IEliminable` con `const int LongitudMaximaNombre = 60`, `LongitudMaximaNombreCorto = 24`, `LongitudMaximaDescripcion = 300`; `static Platillo Crear(Guid restauranteId, Guid categoriaId, DatosPlatillo datos, DateTimeOffset creadoEn)`, `void Editar(Guid categoriaId, DatosPlatillo datos)`, `void Activar()`, `void Desactivar()`, `void Eliminar(DateTimeOffset ahora)`. Propiedades `Id`, `RestauranteId`, `CategoriaId`, `Nombre`, `NombreNormalizado`, `NombreCorto`, `NombreCortoNormalizado`, `Descripcion`, `Orden`, `Ingredientes` (`IReadOnlyList<string>` sobre un `List<string>` privado), `Activo` (empieza en `true`), `CreadoEn`, `EliminadaEn`. Valida todo antes de asignar (una edición inválida no deja el platillo a medias). Constructor privado para EF.

- [ ] **Paso 1: Pruebas que fallan**
  - `"006/RF-02: un platillo nuevo está activo y con datos recortados"` → `Crear(rid, cid, new("  Pozole rojo ", "  ", "  ", 2, [" Crema ", "Cebolla"]), t)` da `Nombre == "Pozole rojo"`, `NombreNormalizado == "pozole rojo"`, `NombreCorto == null`, `Descripcion == null`, `Ingredientes == ["Crema", "Cebolla"]`, `Activo`, `CategoriaId == cid`.
  - `"006/RF-02: Crear y Editar rechazan formatos inválidos"` (Theory) → `ArgumentException` con nombre `""`/61 × `"a"`, nombre corto 25 × `"a"`, descripción 301 × `"a"`, orden `0`/`-1`; acepta 60, 24 y 300.
  - `"006/RF-02: Editar inválido no cambia el platillo"`.
  - `"006/RF-02: Editar cambia la categoría y los ingredientes"`.
  - `IngredientesPlatilloTests` `"006/RF-02: valida los ingredientes"` (Theory) → `[]` y `null` → sin errores; 31 elementos → `No puede haber más de 30 ingredientes`; `["Crema", "  "]` → `El ingrediente 2 está vacío`; `["a", 41 × "b"]` → `El ingrediente 2 no puede superar 40 caracteres`; `[" crema ", "Crema"]` → `El ingrediente 2 está repetido` (Foco 2); 30 elementos de 40 → sin errores.
  - `"006/RF-02: Normalizar recorta y conserva el orden"` → `[" Queso ", "Crema"]` → `["Queso", "Crema"]`.
  - `"006/RF-09: eliminar marca la fecha y conserva los datos"`.
- [ ] **Paso 2:** `PATH=/root/.dotnet:$PATH dotnet test apps/api/tests/Restaurante.Domain.Tests --filter "DisplayName~006/"` → no compila.
- [ ] **Paso 3:** implementar en `Restaurante.Domain` (sin paquetes externos).
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): entidad Platillo e ingredientes (T-01)"`.

### T-02 [RF-04, RF-05, RF-09, RF-10, RF-12]: Persistencia de platillos

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Abstracciones/IPlatilloRepository.cs`, `apps/api/src/Restaurante.Application/Features/Platillos/MensajesPlatillo.cs`
- Crear: `apps/api/src/Restaurante.Infrastructure/Persistencia/PlatilloConfiguration.cs`, `PlatilloRepository.cs`
- Modificar: `RestauranteDbContext.cs` (`DbSet<Platillo>`), `apps/api/src/Restaurante.Infrastructure/DependencyInjection.cs`
- Crear: migración `Platillos`
- Test: `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/PlatillosIntegracionTests.cs`
- Docs: `docs/context/architecture-map.md` (tabla `platillos`)

**Interfaces:**
- Consume: `Platillo` (T-01), `ConflictoCamposException` (005), filtro global (005).
- Produce: `static class MensajesPlatillo { NombreRepetido = "Ya existe un platillo con ese nombre"; NombreCortoRepetido = "Ya existe un platillo con ese nombre corto"; NoEncontrado = "Platillo no encontrado"; }`.
- Produce (`IPlatilloRepository`, todo dentro del restaurante del contexto y sin eliminados, por el filtro global):
  ```csharp
  Task<Platillo?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
  Task<(IReadOnlyList<Platillo> Elementos, int Total)> ListarPorCategoriaAsync(Guid categoriaId, int pagina, int tamano, CancellationToken ct); // Orden, Nombre, Id; desplazamiento seguro como 005
  Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluirId, CancellationToken ct);
  Task<bool> ExisteNombreCortoAsync(string nombreCortoNormalizado, Guid? excluirId, CancellationToken ct);
  Task<int> ContarPorCategoriaAsync(Guid categoriaId, CancellationToken ct);
  void Agregar(Platillo platillo);
  Task GuardarCambiosAsync(CancellationToken ct);
  ```
- Produce el esquema del diseño: tabla `platillos` (FK a `restaurantes` y `categorias`), `ingredientes text[] not null default '{}'`, `ck_platillos_orden`, únicos parciales `ix_platillos_restaurante_nombre` y `ix_platillos_restaurante_nombre_corto`, índice `ix_platillos_categoria_orden_nombre`. `GuardarCambiosAsync` traduce `UniqueViolation` por `ConstraintName` a `ConflictoCamposException` (`nombreCorto` para su índice; cualquier otro → `nombre`).

- [ ] **Paso 1: Pruebas de integración que fallan**
  - `"006/RF-05: los ingredientes se guardan en su orden y la lista pagina por orden y nombre"` → ingredientes `["Queso", "Crema", "Cebolla"]` vuelven igual; tres platillos (orden 2 "B", 1 "Z", 1 "A") → `ListarPorCategoriaAsync(c, 1, 2)` = `["A", "Z"]`, `Total == 3`; otra categoría no aparece.
  - `"006/RF-04: los índices únicos traducen a ConflictoCamposException por campo"` → nombre repetido con otra capitalización en otra categoría → `nombre`; nombre corto repetido → `nombreCorto`; dos sin nombre corto → se guardan.
  - `"006/RF-09: un platillo eliminado no se ve, no cuenta y libera su nombre"`.
  - `"006/RF-10: los platillos de otro restaurante no se ven"`.
  - `"006/RF-12: ContarPorCategoriaAsync cuenta activos e inactivos no eliminados"`.
  - Las pruebas de integración de 003–005 siguen en verde.
- [ ] **Paso 2:** `dotnet test apps/api --filter "Category=Integracion&DisplayName~006/"` → no compila.
- [ ] **Paso 3:** implementar; `dotnet tool run dotnet-ef -- migrations add Platillos …`; registrar el repositorio.
- [ ] **Paso 4:** `dotnet test apps/api --filter "Category=Integracion"` → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): persistencia de platillos (T-02)"`.

### T-03 [RF-01, RF-02, RF-03, RF-04, RF-11]: Alta de platillo

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Platillos/PlatilloDto.cs`, `DatosPlatilloDto.cs`, `ValidadorDatosPlatillo.cs`, `ComprobadorUnicidadPlatillo.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Platillos/CrearPlatillo/CrearPlatilloCommand.cs`, `CrearPlatilloHandler.cs`, `CrearPlatilloValidator.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/PlatillosController.cs`
- Crear: dobles `PlatilloRepositoryEnMemoria` en `apps/api/tests/Restaurante.Application.Tests/Dobles/` y `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/` (filtra por el restaurante de la petición y `EliminadaEn == null`; `FabricaApi.Platillos` como almacén compartido)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Platillos/CrearPlatilloTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Platillos/PlatillosEndpointsTests.cs`
- Docs: `docs/context/http/platillos.http` (nuevo), glosarios (`PlatilloDto`)

**Interfaces:**
- Consume: `Platillo`, `DatosPlatillo`, `IngredientesPlatillo` (T-01); `IPlatilloRepository`, `MensajesPlatillo` (T-02); `ICategoriaRepository`, `MensajesCategoria.NoEncontrada`, `ConflictoCamposException`, `NoEncontradoException` (005); `IContextoRestaurante`, `IReloj`.
- Produce: `sealed record DatosPlatilloDto(string? Nombre, string? NombreCorto, string? Descripcion, int? Orden, IReadOnlyList<string?>? Ingredientes)` con `DatosPlatillo ADominio()`; `ValidadorDatosPlatillo : AbstractValidator<DatosPlatilloDto>` (claves sin prefijo; para `ingredientes` usa `IngredientesPlatillo.Errores`).
- Produce: `sealed record PlatilloDto(Guid Id, Guid CategoriaId, string CategoriaNombre, string Nombre, string? NombreCorto, string? Descripcion, int Orden, IReadOnlyList<string> Ingredientes, bool Activo)` con `static Desde(Platillo, Categoria)`.
- Produce: `ComprobadorUnicidadPlatillo.ComprobarAsync(string nombre, string? nombreCorto, Guid? excluirId, CancellationToken ct)` (una sola `ConflictoCamposException` con todos los campos).
- Produce: `CrearPlatilloCommand(Guid CategoriaId, DatosPlatilloDto Datos) : IRequest<PlatilloDto>`; handler: categoría no encontrada → `NoEncontradoException(MensajesCategoria.NoEncontrada)`; inactiva → `ValidationException` en `categoriaId` con `La categoría está inactiva`; luego unicidad; luego alta.
- Produce: `PlatillosController` con `[Route("api/r/{slug}")]` y `[RequiereRol(Rol.AdministradorGeneral)]` en la clase; `POST categorias/{categoriaId:guid}/platillos` (`CrearPlatilloPeticion(string? Nombre, string? NombreCorto, string? Descripcion, [JsonNumberHandling(Strict)] int? Orden, IReadOnlyList<string?>? Ingredientes)`) → 201, `Location: /api/r/{slug}/platillos/{id}`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"006/RF-01: crea el platillo activo en la categoría"` (handler) → restaurante del contexto, `CreadoEn` del reloj, un guardado.
  - `"006/RF-01: POST devuelve 201 con Location y el platillo"` → cuerpo `{ nombre: "Pozole rojo", nombreCorto: "Pozole", descripcion: "Con maciza", orden: 1, ingredientes: ["Crema", "Cebolla"] }`; respuesta con `categoriaNombre`, `ingredientes` en orden y `activo: true`.
  - `"006/RF-01: sin ingredientes se guarda una lista vacía"`.
  - `"006/RF-02: datos inválidos dan 400 por campo"` (Theory, `AssertValidacionAsync`) → `nombre`, `nombreCorto`, `descripcion`, `orden` (`0`, `1.5`, `"3"`), `ingredientes` (31, vacío, 41, repetido).
  - `"006/RF-03: una categoría inexistente, eliminada o de otro restaurante da 404"` (Theory) → `Categoría no encontrada`.
  - `"006/RF-03: una categoría inactiva da 400 en categoriaId"`.
  - `"006/RF-04: nombre y nombre corto repetidos dan 409 con los dos campos"`; `"006/RF-04: un platillo puede llamarse como una categoría"` (Foco 4); `"006/RF-04: el mismo nombre en otro restaurante se acepta"` (RNF-01).
  - `"006/RF-11: un rol distinto de administrador_general recibe 403"` (Theory: `cajero`, `cocinero`, `administrador_sucursal`, token de plataforma).
- [ ] **Paso 2:** filtro `DisplayName~006/RF-01|DisplayName~006/RF-02|DisplayName~006/RF-03|DisplayName~006/RF-04|DisplayName~006/RF-11` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): alta de platillo (T-03)"`.

### T-04 [RF-05, RF-06, RF-10, RF-11]: Lista y detalle de platillos, y detalle de categoría

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Platillos/ListarPlatillos/*` (`ListarPlatillosQuery`, handler, validador, `PaginaPlatillosDto`), `ObtenerPlatillo/*`
- Crear: `apps/api/src/Restaurante.Application/Features/Categorias/ObtenerCategoria/ObtenerCategoriaQuery.cs`, `ObtenerCategoriaHandler.cs`
- Modificar: `PlatillosController.cs`, `CategoriasController.cs` (`GET {id:guid}`)
- Test: `apps/api/tests/Restaurante.WebApi.Tests/Platillos/PlatillosEndpointsTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Categorias/CategoriasEndpointsTests.cs`
- Docs: `platillos.http`, `categorias.http`

**Interfaces:**
- Consume: `PlatilloDto` (T-03), `IPlatilloRepository` (T-02), `CategoriaDto` (005).
- Produce: `ListarPlatillosQuery(Guid CategoriaId, int Pagina, int Tamano) : IRequest<PaginaPlatillosDto>`; `sealed record PaginaPlatillosDto(IReadOnlyList<PlatilloDto> Elementos, int Total, int Pagina, int Tamano)`; `GET categorias/{categoriaId:guid}/platillos?pagina=&tamano=` (nulos → 1 y 20; mismas reglas y claves que 005).
- Produce: `ObtenerPlatilloQuery(Guid Id) : IRequest<PlatilloDto>` → `GET platillos/{id:guid}`; 404 `Platillo no encontrado`.
- Produce: `ObtenerCategoriaQuery(Guid Id) : IRequest<CategoriaDto>` → `GET /api/r/{slug}/categorias/{id:guid}`; 404 `Categoría no encontrada`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"006/RF-05: GET lista los platillos no eliminados de la categoría por orden y nombre"` → activos e inactivos; no trae eliminados ni de otra categoría; 25 → 20 en la página 1 y `total: 25`.
  - `"006/RF-05: una categoría inactiva lista sus platillos"`; `"006/RF-05: categoría inexistente, eliminada o de otro restaurante da 404"`; `"006/RF-05: parámetros fuera de rango dan 400"` (`pagina=0`, `tamano=101`, `pagina=abc`).
  - `"006/RF-05: GET de una categoría devuelve sus datos y su estado"` y su 404.
  - `"006/RF-06: GET de un platillo devuelve su categoría e ingredientes en orden"`.
  - `"006/RF-10: un platillo inexistente, eliminado o de otro restaurante da 404"`.
  - `"006/RF-11: listar y consultar exigen administrador_general"` → 403 con `cajero`.
- [ ] **Paso 2:** filtro `DisplayName~006/RF-05|DisplayName~006/RF-06|DisplayName~006/RF-10|DisplayName~006/RF-11` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): lista y detalle de platillos (T-04)"`.

### T-05 [RF-07, RF-08, RF-10]: Editar, mover y activar o desactivar platillos

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Platillos/EditarPlatillo/*`, `CambiarEstadoPlatillo/*`
- Modificar: `PlatillosController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Platillos/EditarPlatilloTests.cs`, `PlatillosEndpointsTests.cs`
- Docs: `platillos.http`

**Interfaces:**
- Consume: `ValidadorDatosPlatillo`, `ComprobadorUnicidadPlatillo`, `PlatilloDto` (T-03).
- Produce: `EditarPlatilloCommand(Guid Id, Guid? CategoriaId, DatosPlatilloDto Datos) : IRequest<PlatilloDto>` → `PUT platillos/{id:guid}` (`EditarPlatilloPeticion` = campos de alta + `Guid? CategoriaId`); orden de errores: 400 validador (`La categoría es obligatoria` si falta) → 404 platillo → 400 `categoriaId` (`La categoría no existe` / `La categoría está inactiva`, salvo que sea la actual) → 409 (excluyendo su id).
- Produce: `CambiarEstadoPlatilloCommand(Guid Id, bool? Activo) : IRequest<PlatilloDto>` → `PUT platillos/{id:guid}/estado` con `{ activo }`; sin `activo` → 400 `El estado es obligatorio`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"006/RF-07: editar cambia los datos y los ingredientes"`; `"006/RF-07: renombrar cambiando sólo mayúsculas no choca consigo mismo"`.
  - `"006/RF-07: mover a otra categoría activa cambia categoriaId y categoriaNombre"`.
  - `"006/RF-07: mover a una categoría inactiva da 400 y quedarse en la actual inactiva da 200"` (Foco 1).
  - `"006/RF-07: una categoría inexistente o de otro restaurante da 400 La categoría no existe"`; `"006/RF-07: sin categoría da 400 La categoría es obligatoria"`.
  - `"006/RF-07: nombre de otro platillo da 409 en nombre"`.
  - `"006/RF-08: desactivar lo mantiene en la lista y con el nombre reservado"`; `"006/RF-08: estado sin activo o no booleano da 400 en activo"`.
  - `"006/RF-10: editar o cambiar estado de un platillo inexistente, eliminado o de otro restaurante da 404"`.
- [ ] **Paso 2:** filtro `DisplayName~006/RF-07|DisplayName~006/RF-08|DisplayName~006/RF-10` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): editar, mover y cambiar estado de platillos (T-05)"`.

### T-06 [RF-09, RF-10, RF-12]: Eliminar platillos y bloquear categorías con platillos

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Platillos/EliminarPlatillo/*`
- Crear: `apps/api/src/Restaurante.Application/Comun/Excepciones/CategoriaConPlatillosException.cs`
- Modificar: `apps/api/src/Restaurante.Application/Features/Categorias/EliminarCategoria/EliminarCategoriaHandler.cs`, `apps/api/src/Restaurante.WebApi/Filtros/FiltroExcepciones.cs`, `PlatillosController.cs`, el doble `CategoriaRepositoryEnMemoria` o la `FabricaApi` si hace falta para sembrar platillos
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Platillos/EliminarPlatilloTests.cs`, `apps/api/tests/Restaurante.Application.Tests/Features/Categorias/EliminarCategoriaTests.cs`, `PlatillosEndpointsTests.cs`, `CategoriasEndpointsTests.cs`
- Docs: `platillos.http`, `categorias.http` (el 409), glosarios

**Interfaces:**
- Consume: `IPlatilloRepository.ContarPorCategoriaAsync` (T-02), `MensajesPlatillo.NoEncontrado`.
- Produce: `EliminarPlatilloCommand(Guid Id) : IRequest` → `DELETE platillos/{id:guid}` → 204.
- Produce: `sealed class CategoriaConPlatillosException(int cantidad) : Exception` con `Cantidad` y `Message` = `Esta categoría tiene N platillos` / `Esta categoría tiene 1 platillo`; el filtro la traduce a 409 `Conflicto` con `Extensions["platillos"] = Cantidad`.
- `EliminarCategoriaHandler`: 404 como en 005 → si `ContarPorCategoriaAsync > 0` lanza `CategoriaConPlatillosException` → si no, elimina.

- [ ] **Paso 1: Pruebas que fallan**
  - `"006/RF-09: eliminar responde 204, lo quita de la lista y libera sus nombres"`.
  - `"006/RF-10: eliminar un platillo inexistente o ya eliminado da 404"`.
  - `"006/RF-12: eliminar una categoría con platillos da 409 con la cantidad"` → dos platillos (uno inactivo) → `detail: "Esta categoría tiene 2 platillos"`, `platillos: 2`, la categoría sigue en la lista; con uno → `"Esta categoría tiene 1 platillo"`.
  - `"006/RF-12: tras eliminar su único platillo la categoría se puede eliminar"` (Foco 3) → 204.
  - Las pruebas de 005 de eliminar categoría (sin platillos) siguen en verde.
- [ ] **Paso 2:** filtro `DisplayName~006/RF-09|DisplayName~006/RF-10|DisplayName~006/RF-12` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): eliminar platillos y bloquear categorías con platillos (T-06)"`.

### T-07 [RF-14, RF-15]: Formulario de platillo con editor de ingredientes

**Archivos:**
- Crear: `apps/web/src/menu/apiPlatillos.ts`, `EditorIngredientes.tsx`, `FormularioPlatillo.tsx` (+ `.module.css`)
- Modificar: `apps/web/src/menu/api.ts` (`obtenerCategoria`, `listarCategoriasActivas`)
- Test: `apps/web/src/menu/EditorIngredientes.test.tsx`, `FormularioPlatillo.test.tsx`

**Interfaces:**
- Consume: `pedirApi`, `ErrorApi`, `CampoFormulario`, `VistaPreviaWhatsApp`, `mensajeDeError`, `Categoria` (005).
- Produce (`apiPlatillos.ts`): `type Platillo = { id: string; categoriaId: string; categoriaNombre: string; nombre: string; nombreCorto: string | null; descripcion: string | null; orden: number; ingredientes: string[]; activo: boolean }`; `type DatosPlatillo = { nombre: string; nombreCorto: string | null; descripcion: string | null; orden: number; ingredientes: string[] }`; `type PaginaPlatillos = { elementos: Platillo[]; total: number; pagina: number; tamano: number }`; `clavePlatillos(slug: string, categoriaId: string, pagina?: number)`; `listarPlatillos(token, slug, categoriaId, pagina)` (`tamano=20`); `crearPlatillo(token, slug, categoriaId, datos)`; `editarPlatillo(token, slug, id, datos & { categoriaId: string })`; `cambiarEstadoPlatillo(token, slug, id, activo)`; `eliminarPlatillo(token, slug, id): Promise<void>`.
- Produce (`api.ts`): `obtenerCategoria(token, slug, id): Promise<Categoria>`; `listarCategoriasActivas(token, slug): Promise<Categoria[]>` (`?pagina=1&tamano=100`, filtra `activa`).
- Produce: `EditorIngredientes({ ingredientes: string[], onCambio(lista: string[]): void, errores?: string[] })` con campo `Ingredientes incluidos`, botón `Agregar ingrediente` (también con Enter, sin enviar el formulario) y `Quitar` por elemento; avisos `Escribe un ingrediente`, `Ese ingrediente ya está en la lista` (comparando `trim().toLowerCase()`), `No puede haber más de 30 ingredientes`.
- Produce: `FormularioPlatillo({ slugRestaurante, token, categoriaId, platillo?, categorias?: Categoria[], onGuardado(p: Platillo): void, onCancelar(): void })`: en edición muestra el selector `Categoría` con `categorias` más la actual (`{nombre} (inactiva)` si lo está); errores 400/409 por campo (claves desconocidas → aviso general, como en 005); 403 → `mensajeDeError`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"006/RF-14: agrega ingredientes con el botón y con Enter y los quita"`.
  - `"006/RF-14: un ingrediente vacío o repetido no se agrega y avisa"` (`" crema "` tras `"Crema"`, Foco 2); `"006/RF-14: no deja pasar de 30 ingredientes"`.
  - `"006/RF-14: la vista previa de WhatsApp sigue al nombre y al nombre corto"`.
  - `"006/RF-14: en edición el selector ofrece las activas y la actual inactiva"` → opción `Pozoles (inactiva)` seleccionada; el PUT lleva `categoriaId`.
  - `"006/RF-15: un orden inválido se marca sin llamar a la API"` (`""`, `"0"`, `"1.5"`, `"abc"`).
  - `"006/RF-15: los errores 400 y 409 del servidor se muestran junto a su campo"` (`nombre`, `ingredientes`, `categoriaId`).
  - `"006/RF-15: el alta envía descripción y nombre corto en blanco como null"`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run src/menu)` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo comando → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): formulario de platillo con editor de ingredientes (T-07)"`.

### T-08 [RF-13, RF-14, RF-18]: Pantalla de platillos: acceso, lista, alta y edición
UI: sí

**Archivos:**
- Crear: `apps/web/src/menu/PlatillosPagina.tsx` (+ `.module.css`), `apps/web/e2e/apiSimuladaPlatillos.ts`, `apps/web/e2e/platillos.spec.ts`
- Modificar: `apps/web/src/menu/CategoriasPagina.tsx` (acción `Platillos` por fila), `apps/web/src/App.tsx` (ruta `menu/categorias/:categoriaId/platillos`)
- Modificar si hace falta: `apps/web/e2e/apiSimuladaCategorias.ts` (GET `/categorias/{id}`)
- Test: `apps/web/src/menu/PlatillosPagina.test.tsx`
- Docs: `docs/context/architecture-map.md` (ruta y componentes)

**Interfaces:**
- Consume: `apiPlatillos.ts`, `obtenerCategoria`, `listarCategoriasActivas`, `FormularioPlatillo` (T-07), `tituloWhatsApp`, `MarcoRestaurante`, `RutaProtegida`.
- Produce: `PlatillosPagina` (sólo `administrador_general`, `redirigirSinRol` → inicio): título `Platillos de {categoría}`, enlace `Categorías`, aviso de categoría inactiva con `Nuevo platillo` deshabilitado, tabla paginada (columna `Ingredientes` con la cantidad), formulario de alta y edición; 404 de la categoría → `Categoría no encontrada` con enlace `Categorías`.
- Produce (`apiSimuladaPlatillos.ts`): `type PlatilloSimulado`, `platillo(parcial)`, `simularPlatillos(page, slug, categorias, iniciales, opciones?)` que simula GET de categoría, GET paginado por categoría, POST, GET/PUT `/platillos/{id}`, PUT `/estado` y DELETE en memoria, con registros de peticiones.

- [ ] **Paso 1: Pruebas Playwright que fallan (commit sola)**
  - `test("006/RF-13: desde categorías se abre la lista de platillos de la categoría")` → `Platillos` en la fila; título `Platillos de Pozoles`; columnas; 22 platillos → 20 y `Siguiente`.
  - `test("006/RF-13: una categoría inactiva muestra el aviso y no deja crear")`.
  - `test("006/RF-14: el alta con ingredientes crea el platillo")` → la fila muestra `2` en `Ingredientes`; el POST lleva `["Crema", "Cebolla"]`.
  - `test("006/RF-14: editar mueve el platillo a otra categoría")` → desaparece de la lista; el PUT lleva el nuevo `categoriaId`.
  - `test("006/RF-18: otro rol vuelve al inicio")`; `test("006/RF-18: una categoría que no existe muestra Categoría no encontrada")` (Foco 5).
  - Vitest `PlatillosPagina.test.tsx`: `"006/RF-13: Anterior y Siguiente se deshabilitan en los extremos"`.
- [ ] **Paso 2:** `(cd apps/web && npx playwright test e2e/platillos.spec.ts)` → FAIL; commit de las pruebas solas.
- [ ] **Paso 3:** Ralph Loop (`/implementar`).
- [ ] **Paso 4:** las pruebas → PASS en `escritorio` y `movil`; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): pantalla de platillos con lista, alta y edición (T-08)"`.

### T-09 [RF-16, RF-17]: Estado y eliminación de platillos, y aviso de categoría con platillos
UI: sí

**Archivos:**
- Modificar: `apps/web/src/menu/PlatillosPagina.tsx`, `apps/web/src/menu/CategoriasPagina.tsx`, `apps/web/src/componentes/DialogoConfirmacion.tsx` (`confirmar` opcional: sin él no hay botón de confirmar)
- Modificar: `apps/web/e2e/platillos.spec.ts`, `apps/web/e2e/apiSimuladaCategorias.ts` (opción para que DELETE responda 409 con `platillos`)
- Test: `apps/web/src/menu/PlatillosPagina.test.tsx`, `CategoriasPagina.test.tsx`

**Interfaces:**
- Consume: `cambiarEstadoPlatillo`, `eliminarPlatillo` (T-07), `simularPlatillos` (T-08), `cambiarEstadoCategoria` (005), `ErrorApi` (con el cuerpo del problema: si hace falta leer `platillos`, exponer la extensión en `ErrorApi` sin romper sus usos).
- Produce: acciones por fila `Desactivar`/`Activar` (sin confirmación) y `Eliminar` (confirmación de las restricciones globales; volver a la página anterior si se borra la última fila, como 005); en `CategoriasPagina`, un 409 con `platillos` al eliminar abre el aviso `No se puede eliminar {categoría}` con `Esta categoría tiene N platillos`, `Desactivar categoría` (sólo si está activa) y `Cancelar`.

- [ ] **Paso 1: Pruebas Playwright que fallan (commit sola)**
  - `test("006/RF-16: eliminar un platillo pide confirmación y quita la fila")`.
  - `test("006/RF-16: desactivar y activar un platillo no piden confirmación")` → `Inactivo` y vuelta a `Activo`.
  - `test("006/RF-17: eliminar una categoría con platillos muestra el aviso y deja desactivarla")` → aviso con `Esta categoría tiene 2 platillos`; `Desactivar categoría` → la fila queda `Inactiva`, sin DELETE posterior.
  - `test("006/RF-17: con la categoría ya inactiva el aviso sólo ofrece Cancelar")`.
  - Vitest: `"006/RF-17: Cancelar cierra el aviso sin cambiar la categoría"`.
- [ ] **Paso 2:** `(cd apps/web && npx playwright test e2e/platillos.spec.ts e2e/categorias.spec.ts)` → FAIL en las nuevas; commit de las pruebas solas.
- [ ] **Paso 3:** Ralph Loop (`/implementar`).
- [ ] **Paso 4:** las pruebas → PASS en `escritorio` y `movil` (las de 005 siguen en verde); `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(006): estado y eliminación de platillos y aviso de categoría con platillos (T-09)"`.

### T-10 [RF-01, RF-09, RF-12]: Prueba de punta a punta de platillos

**Archivos:**
- Crear: `apps/web/e2e-stack/platillos.spec.ts`
- Docs: `docs/ONBOARDING.md` (qué cubre la suite completa)

**Interfaces:**
- Consume: todo lo anterior y el arranque de `e2e-stack/categorias.spec.ts` (restaurante `e2e-pla-<marca de tiempo>`, `ADMIN_USERNAME`/`ADMIN_PASSWORD` con `"Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)"`, `E2E_BASE_URL`).

- [ ] **Paso 1: Prueba que falla**
  - `test("006/RF-12: el administrador general crea un platillo, no puede eliminar su categoría y la elimina tras quitarlo")`. Flujo: entra, `Menú`, crea `Pozoles`, pulsa `Platillos`, crea `Pozole rojo` con ingredientes `Crema` y `Cebolla` y ve `2` en `Ingredientes`; vuelve a `Categorías`, pulsa `Eliminar` en `Pozoles`, confirma y ve `Esta categoría tiene 1 platillo`; cancela; entra a `Platillos`, elimina `Pozole rojo` confirmando; vuelve y elimina `Pozoles`, que desaparece.
- [ ] **Paso 2:** `docker compose up -d --build`, `set -a; . ./.env; set +a` y `(cd apps/web && npx playwright test --config playwright.stack.config.ts e2e-stack/platillos.spec.ts)` → FAIL antes de tener todo desplegado (si el contenedor no alcanza NuGet, se valida en la aceptación en el servidor).
- [ ] **Paso 3:** ajustar lo necesario (sólo si la prueba revela un fallo real del sistema).
- [ ] **Paso 4:** el mismo comando → PASS; `comando_e2e_stack` completo → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "test(006): prueba de punta a punta de platillos (T-10)"`.

## Cobertura RF → tareas

| RF | Tareas | RF | Tareas | RF | Tareas |
|---|---|---|---|---|---|
| RF-01 | T-03, T-10 | RF-07 | T-05 | RF-13 | T-08 |
| RF-02 | T-01, T-03 | RF-08 | T-05 | RF-14 | T-07, T-08 |
| RF-03 | T-03 | RF-09 | T-01, T-02, T-06, T-10 | RF-15 | T-07 |
| RF-04 | T-02, T-03 | RF-10 | T-02, T-04, T-05, T-06 | RF-16 | T-09 |
| RF-05 | T-02, T-04 | RF-11 | T-03, T-04 | RF-17 | T-09 |
| RF-06 | T-04 | RF-12 | T-02, T-06, T-10 | RF-18 | T-08 |

RNF-01 → T-02, T-03, T-04, T-05 · RNF-02 → T-07 (`VistaPreviaWhatsApp`) y T-08 (columna con `tituloWhatsApp`) · RNF-03 → proyectos `escritorio`/`movil` (T-08, T-09) · RNF-04 → migración `Platillos` (T-02) y pruebas de 005 en verde (T-06, T-09).
