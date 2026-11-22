---
feature: 002-base-multisucursal-y-sucursales
estado: borrador
---
# Tareas: base multi-restaurante

> **Para agentes:** se ejecuta con `/implementar`, tarea por tarea, con TDD. Cada tarea se cierra
> con `python3 .claude/scripts/completar-tarea.py T-NN` (sólo con `comando_test` en verde). Los
> pasos usan casillas (`- [ ]`).

**Objetivo:** convertir la plataforma de 001 en multi-restaurante (entidad restaurante, cinco roles,
login por restaurante, aislamiento con filtro global, identidad visual) y montar el backoffice React.

**Arquitectura:** el restaurante del contexto (`IContextoRestaurante`) lo fija un middleware a
partir del claim `restaurant_id` del token o del `slug` de la ruta `/api/r/{slug}/…`; un filtro
global de EF Core sobre `Usuario` usa ese contexto, así que el login, la unicidad de nombres y la
regla del último administrador quedan acotados al restaurante sin lógica extra en los handlers.
El frontend es una SPA (React Router + TanStack Query) que vuelca la paleta en variables CSS.

**Stack:** .NET 10, MediatR 12.x, FluentValidation, EF Core + Npgsql, xUnit, Testcontainers;
React + TypeScript + Vite, Vitest + Testing Library, Playwright.

**Spec:** `specs/002-base-multisucursal-y-sucursales/requerimiento.md` y `diseno.md`;
constitución en `specs/000-constitucion/`.

## Restricciones globales

- Tests con el RF calificado: `[Fact(DisplayName = "002/RF-NN: …")]`, `it("002/RF-NN: …")`, `test("002/RF-NN: …")`.
- Pruebas que necesitan PostgreSQL: `[Trait("Category", "Integracion")]` + `[Collection(PostgresCollection.Nombre)]`. No entran en `comando_test`; se ejecutan a mano en la tarea que las crea (Docker encendido: `dockerd` en segundo plano).
- `dotnet` en esta máquina: anteponer `PATH=/root/.dotnet:$PATH` a cada comando.
- Roles (texto en API, JWT y BD): `administrador_plataforma`, `administrador_general`, `administrador_sucursal`, `cajero`, `cocinero`.
- `slug`: `^[a-z0-9-]{3,40}$`, inmutable. Nombre de restaurante: 1–80 caracteres tras `Trim()`. Colores: `^#[0-9A-Fa-f]{6}$` o ausentes.
- Logo: PNG, JPG o WebP detectados por firma; ≤ 1 048 576 bytes; SVG rechazado.
- Paleta por defecto (Art. 15): `principal #F97316`, `acento #FF6600`, `oscuro #111111`, `fondo #F8F9FA`, `texto #1F2937`; tipografía Nunito.
- Variables CSS de tema: `--color-principal`, `--color-acento`, `--color-oscuro`, `--color-fondo`, `--color-texto`. Ningún componente usa colores literales (RNF-02).
- Sesión del frontend en memoria + `sessionStorage` (clave `restaurante.sesion`), nunca `localStorage`; expira con `expiraEn` (8 h).
- Datos de otro restaurante → 404, nunca 403. Restaurante inexistente o inactivo en `/api/r/{slug}/…` → 404 con título `Restaurante no disponible`. Token de restaurante inactivo → 401.
- Art. 5: todo endpoint nuevo o tocado actualiza su `.http` en `docs/context/http/`, y todo término o DTO nuevo se refleja en `docs/GLOSSARY.md` y `docs/context/glossary.md` en la misma tarea; los cambios de capas o entorno actualizan `docs/context/architecture-map.md` y `docs/ONBOARDING.md`.
- Mensajes de error y textos de la interfaz en español, tal como los fija cada tarea.

## Foco de revisión

Casos que la spec no menciona pero que un usuario encontrará; cada uno tiene su prueba en la tarea indicada.

1. **`slug` con mayúsculas en la dirección** (`/r/MiResto/login`): debe funcionar igual que en minúsculas. El middleware compara el `slug` en minúsculas (T-07).
2. **Token de restaurante enviado al login de plataforma**: no debe permitir obtener un token de restaurante por `/api/plataforma/auth/login`. El middleware ignora el token en `/api/plataforma/…` (T-07).
3. **Logo de 3 MB**: responde 400 por campo `logo`, no 413 (límite de petición 10 MB) (T-10).
4. **Nombre con espacios alrededor** (`"  La Esquina "` frente a `"La Esquina"`): se guarda recortado y cuenta como duplicado → 409 (T-05).
5. **Sesión que expira con la pantalla abierta**: al llegar `expiraEn`, la pantalla protegida vuelve al login sin recargar (T-12).

---

### T-01 [RF-17]: Cinco roles de la constitución y administrador inicial de plataforma

**Archivos:**
- Modificar: `apps/api/src/Restaurante.Domain/Usuarios/Rol.cs`
- Modificar: `apps/api/src/Restaurante.Application/Comun/RolExtensiones.cs`
- Modificar: `apps/api/src/Restaurante.Application/Features/Usuarios/CrearUsuario/CrearUsuarioValidator.cs`, `CrearUsuarioHandler.cs`
- Modificar: `apps/api/src/Restaurante.Application/Features/Usuarios/CambiarRolUsuario/CambiarRolUsuarioValidator.cs`, `CambiarRolUsuarioHandler.cs`
- Modificar: `apps/api/src/Restaurante.Application/Features/Usuarios/CambiarEstadoUsuario/CambiarEstadoUsuarioHandler.cs`
- Modificar: `apps/api/src/Restaurante.Infrastructure/Persistencia/UsuarioConfiguration.cs`
- Modificar: `apps/api/src/Restaurante.Infrastructure/Persistencia/UsuarioRepository.cs` (`ContarAdministradoresActivosAsync`)
- Modificar: `apps/api/src/Restaurante.Infrastructure/Arranque/InicializadorAdministrador.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/UsuariosController.cs`
- Crear: migración `RolesPlataforma` en `apps/api/src/Restaurante.Infrastructure/Persistencia/Migraciones/`
- Modificar: `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/FabricaApi.cs` y las pruebas de 001 que usan los roles antiguos
- Test: `apps/api/tests/Restaurante.Application.Tests/Comun/RolExtensionesTests.cs`, `apps/api/tests/Restaurante.Infrastructure.Tests/Arranque/InicializadorAdministradorTests.cs`, `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/RolesIntegracionTests.cs`
- Docs: `docs/GLOSSARY.md` (Rol), `docs/context/glossary.md` (Rol, Administrador inicial), `docs/context/http/usuarios.http` (valores de rol)

**Interfaces:**
- Produce: `enum Rol { AdministradorPlataforma, AdministradorGeneral, AdministradorSucursal, Cajero, Cocinero }`.
- Produce: `RolExtensiones.ComoTexto(this Rol rol) : string` (snake_case), `RolExtensiones.TryDesdeTexto(string? texto, out Rol rol) : bool` (coincidencia exacta), `RolExtensiones.DesdeTexto(string texto) : Rol` (lanza `ArgumentException`).
- Produce: `CrearUsuarioValidator.RolesValidos = ["administrador_general", "administrador_sucursal", "cajero", "cocinero"]`, con el mensaje `"El rol debe ser administrador_general, administrador_sucursal, cajero o cocinero"`. `CambiarRolUsuarioValidator` usa la misma lista.
- Produce: la regla del último administrador y `[RequiereRol]` de `UsuariosController` pasan a `Rol.AdministradorGeneral`.

- [ ] **Paso 1: Escribir las pruebas que fallan**
  - `"002/RF-17: cada rol se expone con su texto de la constitución"` (Theory, 5 casos) → `Assert.Equal("administrador_plataforma", Rol.AdministradorPlataforma.ComoTexto())`, … `"cocinero"`; y `RolExtensiones.DesdeTexto(texto)` devuelve el mismo rol.
  - `"002/RF-17: los textos antiguos o con otra grafía no son roles"` → `TryDesdeTexto` es `false` para `"administrador"`, `"mesero"`, `"Cajero"`, `"administradorplataforma"`, `null`.
  - `"002/RF-17: el administrador inicial se crea como administrador_plataforma"` → tras `InicializarAsync`, el usuario creado tiene `Rol.AdministradorPlataforma`.
  - Integración `"002/RF-17: la migración guarda y lee administrador_plataforma"` → aplicar migraciones, guardar un usuario `AdministradorPlataforma` y leerlo con un contexto nuevo; y `"002/RF-17: la migración RolesPlataforma vacía la tabla usuarios"` → insertar con SQL una fila con rol `administrador`, aplicar la migración y comprobar `COUNT(*) = 0` (RNF-04).
- [ ] **Paso 2: Ejecutar y ver que fallan.** `PATH=/root/.dotnet:$PATH dotnet test apps/api --filter "DisplayName~002/RF-17"` → no compila (`Rol.AdministradorPlataforma` no existe).
- [ ] **Paso 3: Implementar.** `ComoTexto` con un `switch` explícito y `TryDesdeTexto` con un diccionario inverso. Sustituir `Enum.Parse<Rol>` por `RolExtensiones.DesdeTexto` en los handlers. El `ValueConverter` de `UsuarioConfiguration` usa `ComoTexto`/`DesdeTexto`, y la columna `rol` pasa a `HasMaxLength(30)`. Generar la migración: `PATH=/root/.dotnet:$PATH dotnet tool restore --tool-manifest apps/api/.config/dotnet-tools.json` y `dotnet ef migrations add RolesPlataforma --project apps/api/src/Restaurante.Infrastructure --output-dir Persistencia/Migraciones`. Después añadir `migrationBuilder.Sql("DELETE FROM usuarios;")` como primera instrucción de `Up` (RNF-04). `FabricaApi.EmitirToken` usa `rol.ComoTexto()`. Las pruebas de 001 cambian `Mesero/Cocina/Caja/Administrador` por `Cajero/Cocinero/AdministradorGeneral`, sin cambiar lo que comprueban.
- [ ] **Paso 4: Ejecutar y ver que pasan.** El mismo filtro → PASS. La suite completa `comando_test` → PASS. Integración: `dotnet test apps/api --filter "Category=Integracion"` → PASS.
- [ ] **Paso 5: Docs y commit.** `git commit -m "feat(002): cinco roles de la constitución y administrador de plataforma (T-01)"`.

### T-02 [RF-01, RF-04, RF-06]: Entidades Restaurante, Paleta y LogoRestaurante

**Archivos:**
- Crear: `apps/api/src/Restaurante.Domain/Restaurantes/Restaurante.cs`, `Paleta.cs`, `LogoRestaurante.cs`
- Test: `apps/api/tests/Restaurante.Domain.Tests/Restaurantes/RestauranteTests.cs`, `PaletaTests.cs`
- Docs: glosarios (Restaurante, Slug, Paleta, Logo)

**Interfaces:**
- Produce: `sealed record Paleta(string? Principal, string? Acento, string? Oscuro, string? Fondo, string? Texto)`. El constructor lanza `ArgumentException` si un color no nulo no cumple `^#[0-9A-Fa-f]{6}$`. También `static Paleta Vacia` y `static bool EsColorHex(string color)`.
- Produce: `Restaurante`, con `const string PatronSlug = "^[a-z0-9-]{3,40}$"`, `const int LongitudMaximaNombre = 80` y `static Restaurante Crear(string nombre, string slug, Paleta paleta, DateTimeOffset creadoEn)` (recorta el nombre; lanza `ArgumentException` si el slug o el nombre no son válidos). Propiedades `Id`, `Nombre`, `Slug` (sin método que lo cambie), `Activo` (empieza en `true`), `Paleta` y `CreadoEn`. Métodos `CambiarNombre(string)`, `CambiarPaleta(Paleta)`, `Activar()` y `Desactivar()`.
- Produce: `LogoRestaurante`, con `static LogoRestaurante Crear(Guid restauranteId, byte[] contenido, string tipo, string etag, DateTimeOffset ahora)` y `Reemplazar(byte[] contenido, string tipo, string etag, DateTimeOffset ahora)`. Propiedades `RestauranteId`, `Contenido`, `Tipo`, `ETag` y `ActualizadoEn`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-01: un restaurante nuevo está activo con nombre recortado, slug y paleta"` → `Crear("  La Esquina ", "la-esquina", paleta, t)` da `Nombre == "La Esquina"`, `Activo` y la paleta recibida.
  - `"002/RF-01: Crear rechaza slug o nombre inválidos"` (Theory: `"AB"`, `"con espacio"`, `"ab"`, slug de 41 caracteres, nombre `""`, nombre de 81 caracteres) → `ArgumentException`.
  - `"002/RF-04: cambiar nombre y paleta conserva el slug"` → tras `CambiarNombre` y `CambiarPaleta`, `Slug` es el original.
  - `"002/RF-06: desactivar y reactivar cambia sólo el estado"` → `Desactivar()` da `Activo == false`, y `Activar()` lo devuelve a `true` con los mismos datos.
  - `"002/RF-04: Paleta acepta colores #RRGGBB o nulos y rechaza otros formatos"` → acepta `"#f97316"` y `null`; lanza con `"F97316"`, `"#FFF"`, `"#GGGGGG"`.
- [ ] **Paso 2:** `dotnet test apps/api --filter "DisplayName~002/RF-0"` → no compila.
- [ ] **Paso 3:** implementar las tres clases en `Restaurante.Domain.Restaurantes` (sin paquetes externos, Art. 6.2).
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): entidades Restaurante, Paleta y LogoRestaurante (T-02)"`.

### T-03 [RF-03, RF-05]: Persistencia de restaurantes y logos

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Abstracciones/IRestauranteRepository.cs`
- Crear: `apps/api/src/Restaurante.Infrastructure/Persistencia/RestauranteConfiguration.cs`, `LogoRestauranteConfiguration.cs`, `RestauranteRepository.cs`
- Modificar: `apps/api/src/Restaurante.Infrastructure/Persistencia/RestauranteDbContext.cs` (`DbSet<Restaurante> Restaurantes`, `DbSet<LogoRestaurante> Logos`), `apps/api/src/Restaurante.Infrastructure/DependencyInjection.cs`
- Crear: migración `Restaurantes`
- Test: `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/RestaurantesIntegracionTests.cs`
- Docs: `docs/context/architecture-map.md` (tablas)

**Interfaces:**
- Produce (en `IRestauranteRepository`):
  ```csharp
  Task<Restaurante?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
  Task<Restaurante?> ObtenerPorSlugAsync(string slug, CancellationToken ct);
  Task<IReadOnlyList<Restaurante>> ListarAsync(CancellationToken ct);          // ordenados por Nombre
  Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct);
  Task<bool> ExisteSlugAsync(string slug, CancellationToken ct);
  void Agregar(Restaurante restaurante);
  Task<LogoRestaurante?> ObtenerLogoAsync(Guid restauranteId, CancellationToken ct);
  Task<string?> ObtenerEtagLogoAsync(Guid restauranteId, CancellationToken ct); // sin cargar bytes
  void AgregarLogo(LogoRestaurante logo);
  Task GuardarCambiosAsync(CancellationToken ct); // guarda todo lo pendiente del DbContext compartido
  ```
- Produce el esquema: `restaurantes` (`id`, `nombre` varchar(80), `slug` varchar(40), `activo`, `color_principal` … `color_texto` char(7) nulos, `creado_en`), con índices únicos `ix_restaurantes_nombre` e `ix_restaurantes_slug`. `Paleta` se mapea como `ComplexProperty`. `restaurantes_logo` (`restaurante_id` PK/FK con borrado en cascada, `contenido` bytea, `tipo` varchar(20), `etag` varchar(64), `actualizado_en`).
- Produce: `GuardarCambiosAsync` traduce `UniqueViolation` según `ConstraintName`: `ix_restaurantes_nombre` → `ConflictoException("Ya existe un restaurante con ese nombre")`, `ix_restaurantes_slug` → `ConflictoException("Ya existe un restaurante con ese slug")` y cualquier otra → `ConflictoException("Ya existe un usuario con ese nombre")`.

- [ ] **Paso 1: Pruebas de integración que fallan**
  - `"002/RF-03: guarda un restaurante con paleta parcial y lo lista ordenado por nombre"` → dos restaurantes, uno con `Paleta.Vacia`; `ListarAsync` devuelve `["Alfa", "Beta"]` con la paleta nula intacta.
  - `"002/RF-03: un slug repetido al guardar lanza ConflictoException"` → mensaje `"Ya existe un restaurante con ese slug"`.
  - `"002/RF-05: el logo se guarda en bytea y se lee igual"` → `ObtenerLogoAsync` devuelve los mismos bytes, el tipo y el etag; `ObtenerEtagLogoAsync` devuelve el etag, y `null` si el restaurante no tiene logo.
- [ ] **Paso 2:** `dotnet test apps/api --filter "Category=Integracion&DisplayName~002/RF-0"` → no compila.
- [ ] **Paso 3:** configuraciones, repositorio y registro `AddScoped<IRestauranteRepository, RestauranteRepository>()`. Después `dotnet ef migrations add Restaurantes …` (mismos parámetros que en T-01).
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): persistencia de restaurantes y logos (T-03)"`.

### T-04 [RF-12, RF-13]: Usuarios por restaurante con filtro global

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Abstracciones/IContextoRestaurante.cs`
- Crear: `apps/api/src/Restaurante.Infrastructure/Persistencia/ContextoRestauranteFijo.cs`
- Modificar: `apps/api/src/Restaurante.Domain/Usuarios/Usuario.cs`, `apps/api/src/Restaurante.Infrastructure/Persistencia/RestauranteDbContext.cs`, `UsuarioConfiguration.cs`, `RestauranteDbContextFactory.cs`, `apps/api/src/Restaurante.Infrastructure/Arranque/InicializadorAdministrador.cs`
- Modificar: `apps/api/src/Restaurante.Application/Features/Usuarios/CrearUsuario/CrearUsuarioHandler.cs`
- Modificar: `apps/api/tests/Restaurante.Application.Tests/Dobles/UsuarioRepositoryEnMemoria.cs` (filtra por contexto), `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/PostgresFixture.cs` (`CrearContexto(string cadena, Guid? restauranteId = null)`)
- Crear: `apps/api/tests/Restaurante.Application.Tests/Dobles/ContextoRestauranteFalso.cs`
- Crear: migración `UsuariosPorRestaurante`
- Test: `apps/api/tests/Restaurante.Domain.Tests/Usuarios/UsuarioTests.cs`, `apps/api/tests/Restaurante.Application.Tests/Features/Usuarios/UsuariosPorRestauranteTests.cs`, `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/AislamientoIntegracionTests.cs`
- Docs: glosarios (Contexto de restaurante), `docs/context/architecture-map.md` (filtro global)

**Interfaces:**
- Produce: `interface IContextoRestaurante { Guid? RestauranteId { get; } }`. `null` es el ámbito de plataforma.
- Produce: `Usuario.Crear(Guid? restauranteId, string nombreUsuario, string hashContrasena, Rol rol, DateTimeOffset creadoEn)`. Lanza `ArgumentException` si `(rol == Rol.AdministradorPlataforma) != (restauranteId is null)`. Nueva propiedad `Guid? RestauranteId`.
- Produce: `RestauranteDbContext(DbContextOptions<RestauranteDbContext> options, IContextoRestaurante contexto)`, con el filtro `HasQueryFilter(u => u.RestauranteId == RestauranteActualId)`, donde `private Guid? RestauranteActualId => contexto.RestauranteId;` (propiedad de instancia para que EF la parametrice).
- Produce: `sealed class ContextoRestauranteFijo(Guid? restauranteId) : IContextoRestaurante` (fábrica de diseño y pruebas).
- Produce el esquema: `usuarios.restaurante_id` uuid nulo con FK a `restaurantes` (`Restrict`) y el índice único `ix_usuarios_restaurante_nombre` sobre (`restaurante_id`, `nombre_usuario_normalizado`) con `AreNullsDistinct(false)`, que sustituye a `ix_usuarios_nombre_usuario_normalizado`.
- Produce: `CrearUsuarioHandler(IUsuarioRepository, IHasherContrasenas, IReloj, IContextoRestaurante)` crea el usuario en `contexto.RestauranteId` y lanza `InvalidOperationException` si es `null`. `InicializadorAdministrador` crea `Usuario.Crear(null, …, Rol.AdministradorPlataforma, …)`; su `ExisteAlgunoAsync`, filtrado con contexto nulo, significa "ya hay un administrador de plataforma".

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-13: sólo el administrador de plataforma carece de restaurante"` → `Crear(null, …, Cajero, …)` y `Crear(Guid.NewGuid(), …, AdministradorPlataforma, …)` lanzan `ArgumentException`.
  - `"002/RF-13: el mismo nombre puede existir en otro restaurante"` (handler con contexto A y repositorio con `maria` en B) → crea `maria` en A, con `RestauranteId == A`.
  - `"002/RF-13: nombre repetido dentro del restaurante da conflicto"` → `ConflictoException`.
  - `"002/RF-13: la regla del último administrador se cuenta por restaurante"` → A y B tienen un `AdministradorGeneral` activo cada uno; desactivar el de A con contexto A lanza `ConflictoException`.
  - Integración `"002/RF-12: el filtro global sólo devuelve usuarios del restaurante del contexto"` → `ListarAsync` y `ObtenerPorIdAsync` con contexto A no ven el usuario de B (`null`); con contexto `null` sólo se ve el administrador de plataforma.
  - Integración `"002/RF-13: el índice permite el mismo nombre en dos restaurantes y lo impide en uno"` → la segunda inserción en el mismo restaurante lanza `ConflictoException`. Dos administradores de plataforma con el mismo nombre también lo lanzan (`NULLS NOT DISTINCT`).
- [ ] **Paso 2:** `dotnet test apps/api --filter "DisplayName~002/RF-1"` → no compila.
- [ ] **Paso 3:** implementar. Registrar `IContextoRestaurante` en las pruebas con el doble; en la API lo registra T-07 (hasta entonces, `AddInfrastructureServices` registra `TryAddScoped<IContextoRestaurante>(_ => new ContextoRestauranteFijo(null))`). `dotnet ef migrations add UsuariosPorRestaurante …`. Actualizar los usos de `Usuario.Crear` en las pruebas existentes (`null` sólo para plataforma).
- [ ] **Paso 4:** filtro unitario e integración → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): usuarios por restaurante con filtro global (T-04)"`.

### T-05 [RF-01, RF-02, RF-07]: Alta de restaurante con su primer administrador

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/PaletaDto.cs`, `RestauranteDto.cs`, `ValidadorPaleta.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/CrearRestaurante/CrearRestauranteCommand.cs`, `CrearRestauranteHandler.cs`, `CrearRestauranteValidator.cs`, `AdministradorInicialDto.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/RestaurantesController.cs`
- Crear: `apps/api/tests/Restaurante.Application.Tests/Dobles/RestauranteRepositoryEnMemoria.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/RestauranteRepositoryEnMemoria.cs`
- Modificar: `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/FabricaApi.cs` (propiedad `Restaurantes`, registrada como singleton)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Restaurantes/CrearRestauranteTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Restaurantes/RestaurantesEndpointsTests.cs`
- Docs: `docs/context/http/restaurantes.http` (nuevo), glosarios (`RestauranteDto`, `PaletaDto`, `CrearRestaurantePeticion`)

**Interfaces:**
- Consume: `IRestauranteRepository` (T-03), `IUsuarioRepository`, `Usuario.Crear` (T-04), `Restaurante.Crear` y `Paleta` (T-02).
- Produce: `sealed record PaletaDto(string? Principal, string? Acento, string? Oscuro, string? Fondo, string? Texto)`, con `static PaletaDto Desde(Paleta)` y `Paleta ADominio()`. `ValidadorPaleta : AbstractValidator<PaletaDto>` exige que cada color sea nulo o `Paleta.EsColorHex`, con el mensaje `"El color debe tener el formato #RRGGBB"`.
- Produce: `sealed record RestauranteDto(Guid Id, string Nombre, string Slug, bool Activo, PaletaDto Paleta)`, con `static Desde(Restaurante)`.
- Produce: `sealed record AdministradorInicialDto(string NombreUsuario, string Contrasena)` y `sealed record CrearRestauranteCommand(string Nombre, string Slug, PaletaDto? Paleta, AdministradorInicialDto Administrador) : IRequest<RestauranteDto>`.
- Produce: mensajes del validador: `"El nombre es obligatorio"`, `"El nombre no puede superar 80 caracteres"`, `"El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones"`. Los campos del administrador usan los patrones y mensajes de `CrearUsuarioValidator` y salen como `administrador.nombreUsuario` y `administrador.contrasena`. Los colores salen como `paleta.principal`, etc.
- Produce: el handler recorta el nombre, comprueba `ExisteNombreAsync`/`ExisteSlugAsync` (409 con los mensajes de T-03), agrega el restaurante y su `AdministradorGeneral` y llama una sola vez a `restaurantes.GuardarCambiosAsync` (mismo DbContext ⇒ una transacción).
- Produce: `POST /api/plataforma/restaurantes`, con el cuerpo `{ nombre, slug, paleta?, administrador: { nombreUsuario, contrasena } }` → 201, `Location: /api/plataforma/restaurantes/{id}` y un `RestauranteDto`. El controller lleva `[RequiereRol(Rol.AdministradorPlataforma)]` a nivel de clase.

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-01: crea el restaurante activo y su administrador general en un solo guardado"` → un restaurante, un usuario `AdministradorGeneral` con `RestauranteId` igual al del restaurante y la contraseña hasheada, y `GuardadosLlamados == 1`.
  - `"002/RF-01: POST devuelve 201 con Location y el restaurante"` → `201`, `Location` que termina en el id, `activo == true`.
  - `"002/RF-02: datos inválidos dan 400 por campo"` (Theory) → `AssertValidacionAsync` con `slug`, `nombre`, `paleta.principal` (`"rojo"`) y `administrador.contrasena` (`"corta"`).
  - `"002/RF-02: nombre o slug repetidos dan 409"` → `409` con `"Ya existe un restaurante con ese nombre"` y con `"... ese slug"`.
  - `"002/RF-02: el nombre se recorta y '  La Esquina ' duplica 'La Esquina'"` → `409` (Foco 4).
  - `"002/RF-07: un rol distinto de administrador_plataforma recibe 403"` (Theory con los cuatro roles de restaurante) → `403`; sin token → `401`.
- [ ] **Paso 2:** `dotnet test apps/api --filter "DisplayName~002/RF-01|DisplayName~002/RF-02|DisplayName~002/RF-07"` → FAIL.
- [ ] **Paso 3:** implementar el command, el validador, el handler y la acción `Crear` del controller (petición `CrearRestaurantePeticion(string? Nombre, string? Slug, PaletaDto? Paleta, AdministradorInicialDto? Administrador)`, cuyos nulos se convierten en cadenas vacías como en `UsuariosController`).
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS (incluida la guarda de endpoints de 001).
- [ ] **Paso 5:** `git commit -m "feat(002): alta de restaurante con su primer administrador (T-05)"`.

### T-06 [RF-03, RF-04, RF-06, RF-07]: Listar, editar y activar o desactivar restaurantes

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/ListarRestaurantes/ListarRestaurantesQuery.cs`, `ListarRestaurantesHandler.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/EditarRestaurante/EditarRestauranteCommand.cs`, `EditarRestauranteHandler.cs`, `EditarRestauranteValidator.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/CambiarEstadoRestaurante/CambiarEstadoRestauranteCommand.cs`, `CambiarEstadoRestauranteHandler.cs`, `CambiarEstadoRestauranteValidator.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/RestaurantesController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Restaurantes/EditarRestauranteTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Restaurantes/RestaurantesEndpointsTests.cs`
- Docs: `docs/context/http/restaurantes.http`

**Interfaces:**
- Produce: `ListarRestaurantesQuery() : IRequest<IReadOnlyList<RestauranteDto>>` → `GET /api/plataforma/restaurantes`.
- Produce: `EditarRestauranteCommand(Guid Id, string Nombre, PaletaDto? Paleta) : IRequest<RestauranteDto>` → `PUT /api/plataforma/restaurantes/{id}` con `{ nombre, paleta }`. `Paleta == null` ⇒ `Paleta.Vacia`. Un id inexistente da `NoEncontradoException("Restaurante no encontrado")` (404), y un nombre de otro restaurante da 409 (`ExisteNombreAsync(nombre, excluirId: id)`).
- Produce: `CambiarEstadoRestauranteCommand(Guid Id, bool? Activo) : IRequest<RestauranteDto>` → `PUT /api/plataforma/restaurantes/{id}/estado` con `{ activo }` (reutiliza `CambiarEstadoPeticion`). Sin `activo` da 400 `"El estado es obligatorio"`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-03: GET lista nombre, slug, estado y paleta de cada restaurante"` → un arreglo con `nombre`, `slug`, `activo` y `paleta.principal` (nulo en el que no la tiene).
  - `"002/RF-04: editar cambia nombre y paleta y no el slug"` → con el cuerpo `{ nombre, paleta, slug: "otro" }`, la respuesta y el repositorio conservan el slug original.
  - `"002/RF-04: editar un id inexistente da 404 y un nombre ajeno da 409"`.
  - `"002/RF-06: PUT estado desactiva y reactiva sin perder datos"` → `activo == false`, luego `true`, con el mismo nombre, slug y paleta.
  - `"002/RF-07: listar, editar y cambiar estado exigen administrador_plataforma"` → `403` con un token `AdministradorGeneral`.
- [ ] **Paso 2:** filtro `DisplayName~002/RF-03|DisplayName~002/RF-04|DisplayName~002/RF-06|DisplayName~002/RF-07` → FAIL.
- [ ] **Paso 3:** implementar los tres casos de uso y sus acciones.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): listar, editar y cambiar estado de restaurantes (T-06)"`.

### T-07 [RF-08, RF-09, RF-10, RF-11]: Contexto por slug y login por restaurante y de plataforma

**Archivos:**
- Crear: `apps/api/src/Restaurante.WebApi/Seguridad/ContextoRestaurante.cs`, `apps/api/src/Restaurante.WebApi/Seguridad/MiddlewareContextoRestaurante.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/PlataformaAuthController.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/AuthController.cs` (ruta `api/r/{slug}/auth`), `apps/api/src/Restaurante.WebApi/DependencyInjection.cs`, `apps/api/src/Restaurante.WebApi/Program.cs`, `apps/api/src/Restaurante.WebApi/Filtros/RespuestasProblema.cs`, `apps/api/src/Restaurante.Infrastructure/Seguridad/EmisorTokensJwt.cs`, `apps/api/src/Restaurante.Infrastructure/DependencyInjection.cs` (quitar el `TryAddScoped` provisional de T-04)
- Modificar: `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/UsuarioRepositoryEnMemoria.cs` (filtra por `IContextoRestaurante`; `FabricaApi` lo registra scoped sobre un almacén compartido `Usuarios`), `FabricaApi.EmitirToken(..., Guid? restauranteId = null)`, `apps/api/tests/Restaurante.WebApi.Tests/Auth/LoginTests.cs` (rutas nuevas)
- Test: `apps/api/tests/Restaurante.WebApi.Tests/Auth/LoginPorRestauranteTests.cs`, `apps/api/tests/Restaurante.Infrastructure.Tests/Seguridad/EmisorTokensJwtTests.cs`
- Docs: `docs/context/http/auth.http` (dos logins), `docs/GLOSSARY.md` (`LoginPeticion`, claim `restaurant_id`), `docs/context/architecture-map.md` (middleware)

**Interfaces:**
- Consume: `IRestauranteRepository.ObtenerPorIdAsync/ObtenerPorSlugAsync` (T-03), el filtro de T-04 y `IniciarSesionCommand`/`Handler` de 001 sin cambios (el filtro decide qué usuarios existen).
- Produce: `EmisorTokensJwt.ClaimRestaurante = "restaurant_id"`, emitido sólo si `usuario.RestauranteId` tiene valor.
- Produce: `sealed class ContextoRestaurante : IContextoRestaurante` con `void Establecer(Guid restauranteId)`, registrado `AddScoped<ContextoRestaurante>()` y `AddScoped<IContextoRestaurante>(sp => sp.GetRequiredService<ContextoRestaurante>())`.
- Produce: `MiddlewareContextoRestaurante.InvokeAsync(HttpContext, ContextoRestaurante, IRestauranteRepository)`, después de `UseAuthentication()`. Reglas, en este orden:
  1. Ruta que empieza por `/api/plataforma` → no fija contexto (aunque venga un token de restaurante).
  2. Token con `restaurant_id` → regla de T-08.
  3. Ruta `^/api/r/([^/]+)` → `ObtenerPorSlugAsync(slug.ToLowerInvariant())`. Si es nulo o inactivo, 404 con título `"Restaurante no disponible"` y detalle `"La dirección no corresponde a un restaurante activo."`. Si existe y está activo, `Establecer(id)`.
- Produce: `RespuestasProblema.EscribirAsync(HttpContext contexto, int estado, string titulo, string detalle) : Task` (problem+json fuera de MVC).
- Produce: `POST /api/r/{slug}/auth/login` y `POST /api/plataforma/auth/login`, ambos `[EndpointAnonimo]` con `LoginPeticion`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-08: login en su restaurante devuelve JWT con sub, role y restaurant_id"` → decodificar con `JwtSecurityTokenHandler`: `role == "cajero"` y `restaurant_id == id`.
  - `"002/RF-08: el emisor incluye restaurant_id sólo si el usuario tiene restaurante"` (en `EmisorTokensJwtTests`).
  - `"002/RF-09: usuario de otro restaurante recibe 401 con el mismo mensaje"` → el cuerpo es igual al de una contraseña incorrecta (`"Usuario o contraseña incorrectos"`).
  - `"002/RF-10: slug inexistente o restaurante inactivo dan 404 Restaurante no disponible"` (Theory: `"no-existe"`, slug de un restaurante inactivo) → `404` y `title == "Restaurante no disponible"`.
  - `"002/RF-10: el slug de la ruta no distingue mayúsculas"` → `POST /api/r/La-Esquina/auth/login` funciona como `la-esquina` (Foco 1).
  - `"002/RF-11: login de plataforma devuelve rol administrador_plataforma sin restaurant_id"`.
  - `"002/RF-11: un usuario de restaurante no puede entrar por el login de plataforma"` → `401`, también enviando un `Authorization` con su token de restaurante (Foco 2).
- [ ] **Paso 2:** filtro `DisplayName~002/RF-08|DisplayName~002/RF-09|DisplayName~002/RF-10|DisplayName~002/RF-11` → FAIL.
- [ ] **Paso 3:** implementar. Actualizar `LoginTests` de 001 a `/api/r/{slug}/auth/login`, sembrando un restaurante. El texto de Swagger pasa a `"Token JWT obtenido en POST /api/r/{slug}/auth/login o /api/plataforma/auth/login."`.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): login por restaurante y de plataforma con contexto por slug (T-07)"`.

### T-08 [RF-06, RF-12, RF-13]: Contexto desde el token y usuarios bajo /api/r/{slug}

**Archivos:**
- Modificar: `apps/api/src/Restaurante.WebApi/Seguridad/MiddlewareContextoRestaurante.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/UsuariosController.cs` (ruta `api/r/{slug}/usuarios`, `Location: /api/r/{slug}/usuarios/{id}`)
- Modificar: `apps/api/tests/Restaurante.WebApi.Tests/Usuarios/UsuariosEndpointsTests.cs` (rutas nuevas y token con restaurante)
- Test: `apps/api/tests/Restaurante.WebApi.Tests/Restaurantes/AislamientoTests.cs`
- Docs: `docs/context/http/usuarios.http`

**Interfaces:**
- Produce (regla 2 del middleware): si el usuario está autenticado y tiene `restaurant_id`, se busca con `ObtenerPorIdAsync`. Si es nulo o inactivo → 401, título `"No autenticado"`, detalle `"El restaurante no está activo."` y cabecera `WWW-Authenticate: Bearer`. Si la ruta es `/api/r/{slug}` y `slug.ToLowerInvariant() != restaurante.Slug` → 404 `"Restaurante no disponible"`. En otro caso, `Establecer(id)`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-06: tokens de un restaurante desactivado reciben 401 y vuelven a valer al reactivarlo"` → `GET /api/r/a/usuarios` da 401 tras `Desactivar()` y 200 tras `Activar()`, con los mismos usuarios.
  - `"002/RF-06: token con restaurant_id desconocido recibe 401"`.
  - `"002/RF-12: token del restaurante A en la ruta del B da 404"` → `GET /api/r/b/usuarios` con el token de A da `404`.
  - `"002/RF-12: A no ve ni modifica un usuario de B por id"` → `PUT /api/r/a/usuarios/{idDeB}/estado` y `/rol` dan `404`, y el usuario de B no cambia.
  - `"002/RF-13: el administrador general crea usuarios en su restaurante aunque el nombre exista en otro"` → `201`; `GET` de A lo lista, `GET` de B no.
  - `"002/RF-12: un token de plataforma en /api/r/{slug}/usuarios recibe 403"`.
- [ ] **Paso 2:** filtro `DisplayName~002/RF-06|DisplayName~002/RF-12|DisplayName~002/RF-13` → FAIL.
- [ ] **Paso 3:** implementar la regla y mover la ruta.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): aislamiento por restaurante desde el token (T-08)"`.

### T-09 [RF-14, RF-10]: Identidad pública del restaurante

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/ObtenerIdentidad/ObtenerIdentidadQuery.cs`, `ObtenerIdentidadHandler.cs`, `IdentidadDto.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/IdentidadController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Restaurantes/ObtenerIdentidadTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Restaurantes/IdentidadTests.cs`
- Docs: `docs/context/http/identidad.http` (nuevo), glosarios (`IdentidadDto`)

**Interfaces:**
- Consume: `IContextoRestaurante` (fijado por el slug en T-07), `IRestauranteRepository.ObtenerPorIdAsync` y `ObtenerEtagLogoAsync`.
- Produce: `ObtenerIdentidadQuery() : IRequest<IdentidadDto>`; `sealed record IdentidadDto(string Nombre, string Slug, PaletaDto Paleta, string? LogoUrl)`, con `LogoUrl = $"/api/r/{slug}/logo?v={etag}"` o `null`.
- Produce: `GET /api/r/{slug}/identidad`, `[EndpointAnonimo]`, en `IdentidadController` (ruta `api/r/{slug}`).

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-14: identidad devuelve nombre, paleta y logoUrl con el etag"` (handler) → `LogoUrl == "/api/r/la-esquina/logo?v=abc"`; sin logo, `null`.
  - `"002/RF-14: GET identidad responde 200 sin token"` → `nombre`, `paleta.principal` y `logoUrl` nulo.
  - `"002/RF-10: identidad de un restaurante inactivo da 404 Restaurante no disponible"`.
- [ ] **Paso 2:** filtro `DisplayName~002/RF-14|DisplayName~002/RF-10` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): identidad pública del restaurante (T-09)"`.

### T-10 [RF-05, RF-15]: Subir y servir el logo

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/SubirLogo/SubirLogoCommand.cs`, `SubirLogoHandler.cs`, `SubirLogoValidator.cs`, `DetectorTipoImagen.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Restaurantes/ObtenerLogo/ObtenerLogoQuery.cs`, `ObtenerLogoHandler.cs`, `LogoDto.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/RestaurantesController.cs` (acción `SubirLogo`), `IdentidadController.cs` (acción `Logo`)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Restaurantes/SubirLogoTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Restaurantes/LogoTests.cs`
- Docs: `restaurantes.http` y `identidad.http`, glosarios (`LogoDto`)

**Interfaces:**
- Produce: `static class DetectorTipoImagen { static string? Detectar(ReadOnlySpan<byte> bytes); }`. Devuelve `"image/png"` si empieza por `89 50 4E 47 0D 0A 1A 0A`, `"image/jpeg"` si empieza por `FF D8 FF`, `"image/webp"` si contiene `"RIFF"` en 0–3 y `"WEBP"` en 8–11, y `null` en otro caso.
- Produce: `SubirLogoCommand(Guid RestauranteId, byte[] Logo) : IRequest<Unit>`. El validador (campo `logo`) usa `"El logo es obligatorio"` (vacío), `"El logo no puede superar 1 MB"` (> 1 048 576) y `"El logo debe ser PNG, JPG o WebP"`. El handler da 404 si el restaurante no existe, calcula `etag = Convert.ToHexStringLower(SHA256.HashData(logo))` y crea o reemplaza el logo.
- Produce: `PUT /api/plataforma/restaurantes/{id}/logo` multipart con el campo `logo` (`IFormFile?`), `[RequestSizeLimit(10 * 1024 * 1024)]` → 204.
- Produce: `ObtenerLogoQuery() : IRequest<LogoDto>`; `sealed record LogoDto(byte[] Contenido, string Tipo, string ETag)`. Sin logo, `NoEncontradoException("El restaurante no tiene logo")`.
- Produce: `GET /api/r/{slug}/logo`, `[EndpointAnonimo]`. Responde 200 con `Content-Type` igual al tipo, `ETag: "<etag>"` y `Cache-Control: public, no-cache`. Si `If-None-Match` coincide, responde 304 sin cuerpo.

- [ ] **Paso 1: Pruebas que fallan**
  - `"002/RF-05: detecta PNG, JPG y WebP por firma y rechaza SVG y texto"` (Theory sobre `DetectorTipoImagen`).
  - `"002/RF-05: subir un PNG válido guarda contenido, tipo y etag SHA-256"` (handler).
  - `"002/RF-05: SVG, archivo de 1 MB + 1 byte o de 3 MB dan 400 en logo"` (endpoint; el de 3 MB cubre el Foco 3) → `AssertValidacionAsync(r, "logo", …)`.
  - `"002/RF-05: subir logo exige administrador_plataforma"` → `403` con un token `AdministradorGeneral`.
  - `"002/RF-15: GET logo devuelve la imagen con su tipo y ETag"` → `image/png`, el `ETag` entre comillas y los bytes iguales.
  - `"002/RF-15: con If-None-Match igual responde 304"` y `"002/RF-15: sin logo responde 404"`.
- [ ] **Paso 2:** filtro `DisplayName~002/RF-05|DisplayName~002/RF-15` → FAIL.
- [ ] **Paso 3:** implementar. El controller lee `IFormFile` en un `byte[]`; si falta el archivo, envía un arreglo vacío para que responda el validador.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS; integración completa → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): subir y servir el logo del restaurante (T-10)"`.

### T-11 [RF-16]: App React con tema configurable

**Archivos:**
- Modificar: `apps/web/package.json`. Dependencias: `react`, `react-dom`, `react-router`, `@tanstack/react-query`, `@fontsource/nunito`. Desarrollo: `typescript`, `vite` y `@vitejs/plugin-react` en versiones compatibles con `vitest` 3, más `@types/react`, `@types/react-dom`, `jsdom`, `@testing-library/react`, `@testing-library/user-event` y `@testing-library/jest-dom`. Scripts: `dev`, `build` (`tsc --noEmit && vite build`) y `preview`.
- Crear: `apps/web/index.html`, `apps/web/vite.config.ts` (plugin React; `server.proxy['/api']` y `['/health']` → `http://localhost:5000`), `apps/web/tsconfig.json`, `apps/web/src/main.tsx`, `apps/web/src/App.tsx` (enrutador vacío de momento), `apps/web/src/vite-env.d.ts`, `apps/web/src/pruebas/setup.ts`
- Modificar: `apps/web/vitest.config.ts` (`environment: 'jsdom'`, `setupFiles`, `globals: false`)
- Modificar: `apps/web/playwright.config.ts`: `use.baseURL = 'http://localhost:5173'`; `webServer = { command: 'npx vite --port 5173 --strictPort', url: 'http://localhost:5173', reuseExistingServer: true }`; proyectos `escritorio` (1440×900) y `movil` (390×844) (RNF-03).
- Crear: `apps/web/src/tema/paleta.ts`, `apps/web/src/tema/ProveedorTema.tsx`, `apps/web/src/tema/tema.css`
- Test: `apps/web/src/tema/paleta.test.ts`, `apps/web/src/tema/ProveedorTema.test.tsx`, `apps/web/src/tema/sinColoresLiterales.test.ts`
- Docs: `docs/ONBOARDING.md` (levantar el frontend con `npm run dev`), `docs/context/architecture-map.md` (sección frontend)

**Interfaces:**
- Produce (`paleta.ts`):
  ```ts
  export type Paleta = { principal: string; acento: string; oscuro: string; fondo: string; texto: string };
  export type PaletaParcial = Partial<Record<keyof Paleta, string | null>>;
  export const PALETA_POR_DEFECTO: Paleta; // valores de Restricciones globales
  export function resolverPaleta(p?: PaletaParcial | null): Paleta;            // null/ausente → defecto
  export function aplicarPaleta(p: Paleta, elemento: HTMLElement = document.documentElement): void;
  ```
- Produce: `ProveedorTema({ paleta?: PaletaParcial | null, children })`. Aplica `resolverPaleta(paleta)` en `useLayoutEffect` y al desmontarse restaura `PALETA_POR_DEFECTO`.
- Produce: `tema.css` define las cinco variables con los valores por defecto en `:root`, `font-family: 'Nunito', system-ui, sans-serif` y los estilos base (`body` con `var(--color-fondo)` y `var(--color-texto)`).

- [ ] **Paso 1: Pruebas que fallan**
  - `it("002/RF-16: resolverPaleta completa los colores que faltan con los de la constitución")` → `resolverPaleta({ principal: '#123456', acento: null })` da `principal '#123456'`, `acento '#FF6600'` y el resto por defecto.
  - `it("002/RF-16: ProveedorTema escribe la paleta en variables de :root")` → `getPropertyValue('--color-principal') === '#123456'` y `--color-fondo === '#F8F9FA'`.
  - `it("002/RF-16: ningún componente usa colores literales (RNF-02)")` → recorre `src/**/*.{tsx,module.css}` con `fs` y no encuentra `#[0-9a-fA-F]{3,6}\b`, `rgb(` ni `hsl(`. Excluye `paleta.ts`, `tema.css` y los `*.test.*`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "002/RF-16")` → FAIL.
- [ ] **Paso 3:** instalar las dependencias (`npm install --prefix apps/web …`) e implementar.
- [ ] **Paso 4:** el mismo comando → PASS; `(cd apps/web && npm run build)` → OK; `comando_test` → PASS (Playwright sin pruebas aún).
- [ ] **Paso 5:** `git commit -m "feat(002): app React con tema configurable (T-11)"`.

### T-12 [RF-21]: Sesión, cliente de API y rutas protegidas

**Archivos:**
- Crear: `apps/web/src/api/cliente.ts`, `apps/web/src/api/auth.ts`
- Crear: `apps/web/src/sesion/almacenSesion.ts`, `apps/web/src/sesion/ProveedorSesion.tsx`
- Crear: `apps/web/src/rutas/RutaProtegida.tsx`
- Test: `apps/web/src/sesion/almacenSesion.test.ts`, `apps/web/src/sesion/ProveedorSesion.test.tsx`, `apps/web/src/rutas/RutaProtegida.test.tsx`, `apps/web/src/api/cliente.test.ts`

**Interfaces:**
- Produce (`cliente.ts`): `class ErrorApi extends Error { estado: number; titulo: string; detalle?: string; errores: Record<string, string[]> }` y `pedirApi<T>(ruta: string, opciones?: RequestInit & { token?: string }): Promise<T>`, con base `import.meta.env.VITE_API_URL ?? ''`. Con un token añade `Authorization: Bearer`. Con 204/304 devuelve `undefined`. Si la respuesta no es `ok`, lanza `ErrorApi` leyendo el problem+json.
- Produce (`auth.ts`): `type Rol = 'administrador_plataforma' | 'administrador_general' | 'administrador_sucursal' | 'cajero' | 'cocinero'`; `type LoginRespuesta = { token: string; expiraEn: string; nombreUsuario: string; rol: Rol }`; `iniciarSesionRestaurante(slug, nombreUsuario, contrasena)` y `iniciarSesionPlataforma(nombreUsuario, contrasena)`, ambas `Promise<LoginRespuesta>`.
- Produce (`almacenSesion.ts`): `type Sesion = LoginRespuesta & { slug: string | null }` (`null` = plataforma); `guardarSesion(s)`, `leerSesion(ahora = Date.now()): Sesion | null` (si ha expirado, la borra y devuelve `null`) y `borrarSesion()`. Usa memoria + `sessionStorage['restaurante.sesion']`.
- Produce (`ProveedorSesion.tsx`): `ProveedorSesion` y `useSesion(): { sesion: Sesion | null; iniciar(s: Sesion): void; cerrar(): void }`, con un temporizador que llama a `cerrar()` al llegar `expiraEn`.
- Produce: `RutaProtegida({ roles: Rol[]; slug?: string; children })`. Si no hay sesión, o `sesion.slug !== (slug ?? null)`, o el rol no está en `roles`, hace `<Navigate replace>` a `/r/${slug}/login` o a `/plataforma/login`.

- [ ] **Paso 1: Pruebas que fallan**
  - `it("002/RF-21: sin sesión, una ruta de plataforma lleva a /plataforma/login")` (con `MemoryRouter`).
  - `it("002/RF-21: sin sesión o con sesión de otro restaurante, /r/b/inicio lleva a /r/b/login")`.
  - `it("002/RF-21: con el rol requerido muestra el contenido protegido")`.
  - `it("002/RF-21: la sesión se guarda en sessionStorage y nunca en localStorage (RNF-01)")`.
  - `it("002/RF-21: una sesión expirada se descarta al leerla")`.
  - `it("002/RF-21: al expirar con la pantalla abierta vuelve al login")` → con `vi.useFakeTimers()` se avanza hasta `expiraEn` y aparece el login (Foco 5).
  - `it("002/RF-21: pedirApi lanza ErrorApi con estado y errores por campo")` (`fetch` simulado).
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "002/RF-21")` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo comando → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): sesión, cliente de API y rutas protegidas (T-12)"`.

### T-13 [RF-19, RF-20, RF-16, RF-21]: Pantallas del restaurante con su marca
UI: sí

**Archivos:**
- Crear: `apps/web/src/restaurante/api.ts`, `DisenoRestaurante.tsx`, `LoginRestaurante.tsx`, `InicioRestaurante.tsx`, `EncabezadoMarca.tsx`, `RestauranteNoDisponible.tsx` y sus `*.module.css` en `apps/web/src/restaurante/`
- Crear: `apps/web/src/componentes/FormularioLogin.tsx` (+ `.module.css`)
- Modificar: `apps/web/src/App.tsx` (rutas `/r/:slug` → `DisenoRestaurante` con hijas `login` e `inicio`; el índice redirige a `login`)
- Crear: `apps/web/e2e/apiSimulada.ts` (ayudas `page.route` para identidad, logo y login)
- Test: `apps/web/src/restaurante/LoginRestaurante.test.tsx`, `apps/web/src/restaurante/DisenoRestaurante.test.tsx`, `apps/web/e2e/restaurante.spec.ts`

**Interfaces:**
- Consume: `ProveedorTema` (T-11), `pedirApi`, `iniciarSesionRestaurante`, `useSesion` y `RutaProtegida` (T-12).
- Produce: `type Identidad = { nombre: string; slug: string; paleta: PaletaParcial; logoUrl: string | null }` y `obtenerIdentidad(slug): Promise<Identidad>`.
- Produce: `FormularioLogin({ titulo: string; onEnviar(nombreUsuario, contrasena): Promise<void> })`, con los textos `Usuario`, `Contraseña` y el botón `Iniciar sesión`. Si `onEnviar` lanza `ErrorApi`, muestra su `detalle` con `role="alert"`.
- Produce: `DisenoRestaurante` (TanStack Query, clave `['identidad', slug]`). Mientras carga muestra `Cargando…`. Con `ErrorApi` 404 muestra `RestauranteNoDisponible` (h1 `Restaurante no disponible`). Si no, hace `<ProveedorTema paleta={identidad.paleta}>` y un `<Outlet context={identidad}>`.
- Produce: `LoginRestaurante` muestra `<img alt="Logo de {nombre}">` (si hay `logoUrl`), un h1 con el nombre y `FormularioLogin`; al entrar navega a `/r/{slug}/inicio`. `InicioRestaurante` (dentro de `RutaProtegida` con los cuatro roles de restaurante) muestra `EncabezadoMarca`: logo, nombre, `Hola, {nombreUsuario}` y el botón `Cerrar sesión`, que cierra la sesión y lleva a `/r/{slug}/login`.

- [ ] **Paso 1: Pruebas que fallan**
  - Vitest `it("002/RF-19: el login muestra nombre y logo del restaurante")` y `it("002/RF-20: con 404 muestra Restaurante no disponible")`.
  - Playwright `test("002/RF-19: login con marca lleva a inicio con Hola y Cerrar sesión")` → identidad simulada con `principal '#123456'`; el h1 tiene el nombre; `getComputedStyle(document.documentElement).getPropertyValue('--color-principal')` vale `#123456`; tras entrar la URL es `/r/la-esquina/inicio` y se ven `Hola, ana` y `Cerrar sesión`.
  - Playwright `test("002/RF-16: sin paleta se aplican los colores por defecto")` → `--color-principal` vale `#F97316`.
  - Playwright `test("002/RF-19: credenciales incorrectas muestran el mensaje del servidor")`.
  - Playwright `test("002/RF-20: un slug inexistente muestra Restaurante no disponible")`.
  - Playwright `test("002/RF-21: /r/la-esquina/inicio sin sesión lleva a /r/la-esquina/login")`.
  - Playwright `test("002/RF-19: Cerrar sesión vuelve al login")`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "002/RF-19|002/RF-20" && npx playwright test e2e/restaurante.spec.ts)` → FAIL.
- [ ] **Paso 3:** fase verde con Ralph Loop (`/implementar`; máximo 8 iteraciones; promesa `T-13 VERDE`; sin tocar las pruebas). Estilos sólo con `var(--color-*)`.
- [ ] **Paso 4:** el mismo comando → PASS en `escritorio` y `movil`; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): pantallas del restaurante con su marca (T-13)"`.

### T-14 [RF-18, RF-21]: Login de plataforma y lista de restaurantes
UI: sí

**Archivos:**
- Crear: `apps/web/src/plataforma/api.ts`, `LoginPlataforma.tsx`, `RestaurantesPagina.tsx`, `DialogoConfirmacion.tsx` y sus `*.module.css` en `apps/web/src/plataforma/`
- Modificar: `apps/web/src/App.tsx` (`/plataforma/login`; `/plataforma/restaurantes` dentro de `RutaProtegida roles={['administrador_plataforma']}`; `/` redirige a `/plataforma/login`)
- Modificar: `apps/web/e2e/apiSimulada.ts` (restaurantes y login de plataforma)
- Test: `apps/web/src/plataforma/RestaurantesPagina.test.tsx`, `apps/web/e2e/plataforma.spec.ts`

**Interfaces:**
- Consume: `FormularioLogin` (T-13), `iniciarSesionPlataforma`, `useSesion` y `RutaProtegida` (T-12).
- Produce (`plataforma/api.ts`): `type Restaurante = { id: string; nombre: string; slug: string; activo: boolean; paleta: PaletaParcial }`; `listarRestaurantes(token)`, `crearRestaurante(token, datos)`, `editarRestaurante(token, id, datos)`, `cambiarEstadoRestaurante(token, id, activo)` y `subirLogo(token, id, archivo: File)` (`FormData` con el campo `logo`).
- Produce: `RestaurantesPagina`, una tabla con las columnas `Nombre`, `Slug`, `Estado` (`Activo`/`Inactivo`) y `Acciones` (`Editar`; `Desactivar` o `Activar`), y los botones `Nuevo restaurante` y `Cerrar sesión`. Tras cada cambio invalida `['restaurantes']`.
- Produce: `DialogoConfirmacion({ titulo, mensaje, confirmar, onConfirmar, onCancelar })` con `role="dialog"`. Para desactivar: título `¿Desactivar {nombre}?`, mensaje `Su personal no podrá iniciar sesión hasta que lo reactives.`, botones `Desactivar` y `Cancelar`. `Activar` no pide confirmación.

- [ ] **Paso 1: Pruebas que fallan**
  - Vitest `it("002/RF-18: Desactivar pide confirmación y Cancelar no llama a la API")`.
  - Playwright `test("002/RF-18: el administrador entra y ve la lista con estado")` → tras `/plataforma/login` la URL es `/plataforma/restaurantes` y las filas muestran `Activo` e `Inactivo`.
  - Playwright `test("002/RF-18: desactivar con confirmación cambia el estado a Inactivo")` → la petición `PUT …/estado` lleva `{ activo: false }`.
  - Playwright `test("002/RF-18: activar un restaurante inactivo no pide confirmación")`.
  - Playwright `test("002/RF-21: /plataforma/restaurantes sin sesión lleva a /plataforma/login")`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "002/RF-18" && npx playwright test e2e/plataforma.spec.ts)` → FAIL.
- [ ] **Paso 3:** fase verde con Ralph Loop (promesa `T-14 VERDE`).
- [ ] **Paso 4:** el mismo comando → PASS en ambos proyectos; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): login de plataforma y lista de restaurantes (T-14)"`.

### T-15 [RF-18]: Alta y edición de restaurante con vista previa y logo
UI: sí

**Archivos:**
- Crear: `apps/web/src/plataforma/FormularioRestaurante.tsx`, `VistaPreviaPaleta.tsx` y sus `*.module.css`
- Modificar: `apps/web/src/plataforma/RestaurantesPagina.tsx` (abre el formulario en `Nuevo restaurante` y `Editar`)
- Test: `apps/web/src/plataforma/FormularioRestaurante.test.tsx`, `apps/web/e2e/formularioRestaurante.spec.ts`

**Interfaces:**
- Consume: `crearRestaurante`, `editarRestaurante` y `subirLogo` (T-14), `resolverPaleta` (T-11).
- Produce: `FormularioRestaurante({ restaurante?: Restaurante; onGuardado(): void; onCancelar(): void })`. Etiquetas: `Nombre`, `Slug` (deshabilitado al editar), `Color principal`, `Color de acento`, `Color oscuro`, `Color de fondo`, `Color de texto` (`input type="color"`, iniciados con `resolverPaleta(restaurante?.paleta)`), `Logo` (`accept="image/png,image/jpeg,image/webp"`) y, sólo en el alta, `Usuario administrador` y `Contraseña del administrador`. Botones `Guardar` y `Cancelar`. Los errores 400 se muestran junto a su campo (`nombre`, `slug`, `paleta.*`, `administrador.*`, `logo`) y el 409 como alerta general con su `detalle`. Al guardar, primero crea o edita y después, si hay archivo, llama a `subirLogo`. Si el logo falla, el restaurante queda guardado y se muestra el error del campo `logo`.
- Produce: `VistaPreviaPaleta({ paleta: Paleta })`, una región `aria-label="Vista previa"` que fija las cinco variables CSS en su propio elemento (con la misma lógica que `aplicarPaleta`) y muestra un encabezado y un botón de ejemplo con esos tokens.

- [ ] **Paso 1: Pruebas que fallan**
  - Vitest `it("002/RF-18: la vista previa refleja el color elegido")` → al cambiar `Color principal` a `#123456`, la región `Vista previa` tiene `--color-principal: #123456`.
  - Vitest `it("002/RF-18: al editar, el slug no se puede cambiar")`.
  - Playwright `test("002/RF-18: crear un restaurante con logo lo añade a la lista")` → se envía `POST` con `slug` y `administrador`, y luego `PUT …/logo` multipart; la fila nueva aparece como `Activo`.
  - Playwright `test("002/RF-18: los errores 400 del servidor se muestran junto a su campo")` → `slug` inválido simulado; el texto del error aparece junto a `Slug`.
  - Playwright `test("002/RF-18: editar nombre y paleta actualiza la fila")`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "002/RF-18" && npx playwright test e2e/formularioRestaurante.spec.ts)` → FAIL.
- [ ] **Paso 3:** fase verde con Ralph Loop (promesa `T-15 VERDE`).
- [ ] **Paso 4:** el mismo comando → PASS en ambos proyectos; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): alta y edición de restaurante con vista previa y logo (T-15)"`.

### T-16 [RF-01, RF-08, RF-19]: Servicio web en docker compose y prueba de punta a punta

**Archivos:**
- Crear: `apps/web/Dockerfile` (etapa `node` con `npm ci && npm run build`; etapa `nginx:alpine` con `dist/`), `apps/web/nginx.conf` (`/api/` y `/health` → `http://api:8080`; resto → `index.html`), `apps/web/.dockerignore` (`node_modules`, `test-results`)
- Modificar: `docker-compose.yml` (servicio `web`: `build: ./apps/web`, `ports: "8080:80"`, `depends_on: api`)
- Modificar: `apps/web/playwright.stack.config.ts` (`baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:8080'`; `salud.spec.ts` sigue usando su propia URL)
- Test: `apps/web/e2e-stack/restaurante.spec.ts`
- Docs: `docs/ONBOARDING.md` (servicio `web`, exportar `.env` antes de `comando_e2e_stack`: `set -a; . ./.env; set +a`), `docs/context/architecture-map.md` (despliegue)

**Interfaces:**
- Consume: todo lo anterior y las variables `ADMIN_USERNAME`/`ADMIN_PASSWORD` del entorno (la prueba falla con el mensaje `"Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)"` si faltan).

- [ ] **Paso 1: Prueba que falla**
  - `test("002/RF-01: la plataforma da de alta un restaurante y su administrador entra en su dirección")`, en un solo flujo contra el sistema levantado. Entra en `/plataforma/login` con el administrador inicial, crea un restaurante con el slug `e2e-<marca de tiempo>` y un administrador. Abre `/r/<slug>/login`, ve el nombre (RF-19), inicia sesión con ese administrador y termina en `/r/<slug>/inicio` con `Hola, <usuario>` (RF-08).
- [ ] **Paso 2:** `docker compose up -d --build`, luego `set -a; . ./.env; set +a` y `(cd apps/web && npx playwright test --config playwright.stack.config.ts)` → FAIL (no hay servicio `web`).
- [ ] **Paso 3:** implementar el Dockerfile, `nginx.conf` y el servicio.
- [ ] **Paso 4:** el mismo comando → PASS; `comando_e2e_stack` completo → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(002): servicio web en compose y prueba de punta a punta (T-16)"`.

## Cobertura RF → tareas

| RF | Tareas | RF | Tareas | RF | Tareas |
|---|---|---|---|---|---|
| RF-01 | T-02, T-05, T-16 | RF-08 | T-07, T-16 | RF-15 | T-10 |
| RF-02 | T-05 | RF-09 | T-07 | RF-16 | T-11, T-13 |
| RF-03 | T-03, T-06 | RF-10 | T-07, T-09 | RF-17 | T-01 |
| RF-04 | T-02, T-06 | RF-11 | T-07 | RF-18 | T-14, T-15 |
| RF-05 | T-03, T-10 | RF-12 | T-04, T-08 | RF-19 | T-13, T-16 |
| RF-06 | T-02, T-06, T-08 | RF-13 | T-04, T-08 | RF-20 | T-13 |
| RF-07 | T-05, T-06 | RF-14 | T-09 | RF-21 | T-12, T-13, T-14 |

RNF-01 → T-12 · RNF-02 → T-11 · RNF-03 → proyectos `escritorio`/`movil` de Playwright (T-11 a T-15) · RNF-04 → migración `RolesPlataforma` (T-01).
