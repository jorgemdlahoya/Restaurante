---
feature: 001-plataforma-base
estado: borrador
---
# Tareas: Plataforma base — backend

> **Para agentes:** ejecuta con `/implementar` (TDD por tarea). Cada tarea se cierra con
> `python3 .claude/scripts/completar-tarea.py T-NN`. Pasos con casillas (`- [ ]`).

**Objetivo:** construir el backend .NET 10 de la plataforma: login JWT con roles, gestión mínima
de usuarios, errores uniformes, logging, configuración por entorno, salud, CORS, Swagger y
despliegue con `docker-compose`.

**Arquitectura:** Clean Architecture en 4 capas (`apps/api/src/Restaurante.{Domain,Application,Infrastructure,WebApi}`),
CQRS con MediatR 12.x y behaviours de validación y logging, filtros MVC de autorización y
excepciones, EF Core + PostgreSQL tras repositorios.

**Stack:** .NET 10, ASP.NET Core, MediatR 12.x, FluentValidation, EF Core + Npgsql, JwtBearer,
Swashbuckle, xUnit, NetArchTest, Testcontainers, `Microsoft.AspNetCore.Mvc.Testing`.

**Spec:** `specs/001-plataforma-base/requerimiento.md` y `diseno.md`; principios en
`specs/000-constitucion/constitucion.md`.

## Restricciones globales

- **Requisito de entorno:** SDK de .NET 10 y Node 18+ con npm. En el entorno en la nube hay que
  permitir el host `builds.dotnet.microsoft.com` para instalar el SDK; Docker sólo es necesario
  para las pruebas `Integracion` (cierre de la feature).
- Proyectos `net10.0`, `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  en `apps/api/Directory.Build.props`; versiones centralizadas en `apps/api/Directory.Packages.props`.
- MediatR fijado en **12.x** (la última 12.x disponible); nunca 13+.
- Todas las pruebas: `[Fact(DisplayName = "001/RF-NN: …")]` o `[Theory(DisplayName = …)]`.
  Las que usan Testcontainers llevan además `[Trait("Category", "Integracion")]`.
- Dobles de prueba escritos a mano (sin librerías de mocking).
- Identificadores de dominio en español; sufijos técnicos en inglés (`Command`, `Handler`,
  `Validator`, `Controller`, `Repository`, `Dto`).
- Mensajes al cliente en español. Mensaje único de credenciales: `"Usuario o contraseña incorrectos"`.
- Variables: `DB_CONNECTION_STRING`, `JWT_SIGNING_KEY` (≥ 32 caracteres), `CORS_ALLOWED_ORIGINS`
  (separadas por comas), `ADMIN_USERNAME`, `ADMIN_PASSWORD`, `APPLY_MIGRATIONS`.
- JWT: HS256, emisor `restaurante-api`, audiencia `restaurante-web`, expiración 8 h, tolerancia
  de reloj cero; claims `sub`, `unique_name`, `role`.
- Constitución, Art. 5: cada endpoint creado o tocado añade su `.http` en `docs/context/http/`
  en la **misma** tarea.
- Comprobar cada tarea con `dotnet test apps/api --filter "Category!=Integracion"` y cerrarla con
  `completar-tarea.py`; las pruebas `Integracion` se ejecutan con
  `dotnet test apps/api --filter "Category=Integracion"` cuando hay Docker.

## Puntos de revisión

1. Login con distinta capitalización (`ADMIN` frente a `admin`) → entra igual (búsqueda por nombre normalizado). (T-04)
2. Login con espacios al principio o al final del nombre → se recortan antes de buscar. (T-04)
3. Dos altas simultáneas con el mismo nombre → la segunda recibe 409, no 500 (el repositorio traduce la violación del índice único a `ConflictoException`). (T-07)
4. JSON mal formado o rol con mayúsculas (`"Caja"`) → 400 `ValidationProblemDetails`, no 500. (T-11)
5. `{id}` que no es un GUID en `/api/usuarios/{id}/…` → 404, no 500 (restricción de ruta `{id:guid}`). (T-11)

---

### T-01 [RF-22]: Solución .NET, test de arquitectura y validación de configuración

**Archivos:**
- Crear: `apps/api/Restaurante.sln`, `apps/api/Directory.Build.props`,
  `apps/api/Directory.Packages.props`, `apps/api/global.json` (SDK 10.0)
- Crear: los 4 proyectos `src/` y los 5 de `tests/` con las referencias de la arquitectura
- Crear: `apps/api/src/Restaurante.WebApi/Configuracion/ConfiguracionEntorno.cs`
- Crear: `apps/web/package.json` mínimo (sólo `devDependencies`: `vitest`, `@playwright/test`;
  scripts vacíos) y su `package-lock.json`, para que `comando_test` funcione hasta la feature 002
- Crear: `docs/context/architecture-map.md` (capas y dependencias)
- Test: `tests/Restaurante.Architecture.Tests/ReglasDeCapasTests.cs`,
  `tests/Restaurante.WebApi.Tests/Configuracion/ConfiguracionEntornoTests.cs`

**Interfaces:**
- Produce: `ConfiguracionEntorno.Cargar(IReadOnlyDictionary<string, string?> entorno) -> ResultadoConfiguracion`
  con `bool EsValida`, `IReadOnlyList<string> Errores` y, si es válida, `string CadenaConexion`,
  `string ClaveJwt`, `IReadOnlyList<string> OrigenesCors`, `bool AplicarMigraciones`.

- [ ] **Paso 1: tests que fallan**
  - `001/RF-22: falta una variable obligatoria` (`[Theory]` con `DB_CONNECTION_STRING`,
    `JWT_SIGNING_KEY`, `CORS_ALLOWED_ORIGINS`): `EsValida == false` y algún error contiene el
    nombre de la variable.
  - `001/RF-22: clave JWT corta`: clave de 31 caracteres → error que contiene `JWT_SIGNING_KEY`
    y `32`.
  - `001/RF-22: los errores no muestran valores`: ningún error contiene el valor de una variable
    presente.
  - `001/RF-22: orígenes CORS separados por comas`: `"http://a, http://b"` → `["http://a", "http://b"]`.
  - Arquitectura (NetArchTest, `DisplayName = "001/RNF-02: …"`): `Domain` no depende de
    `Application`, `Infrastructure`, `WebApi`, `MediatR` ni `Microsoft.EntityFrameworkCore`;
    `Application` no depende de `Infrastructure` ni `WebApi`.
- [ ] **Paso 2:** `dotnet test apps/api --filter "Category!=Integracion"` → falla (tipos inexistentes).
- [ ] **Paso 3:** crear la solución y `ConfiguracionEntorno`; `npm --prefix apps/web install`.
- [ ] **Paso 4:** `dotnet test apps/api --filter "Category!=Integracion"` → pasa; el `comando_test`
  completo de `harness.config.json` → código 0.
- [ ] **Paso 5:** commit `feat(api): solución base, test de arquitectura y configuración por entorno`.

### T-02 [RF-11, RF-15, RF-16]: Dominio `Usuario` y `Rol`

**Archivos:**
- Crear: `src/Restaurante.Domain/Usuarios/Usuario.cs`, `src/Restaurante.Domain/Usuarios/Rol.cs`
- Crear: `docs/context/glossary.md` (usuario, rol, administrador inicial) y `docs/GLOSSARY.md` (`Rol`)
- Test: `tests/Restaurante.Domain.Tests/Usuarios/UsuarioTests.cs`

**Interfaces:**
- Produce: `enum Rol { Mesero, Cocina, Caja, Administrador }`;
  `Usuario.Crear(string nombreUsuario, string hashContrasena, Rol rol, DateTimeOffset creadoEn) -> Usuario`
  (activo, `Id` nuevo, `NombreUsuarioNormalizado = nombreUsuario.ToLowerInvariant()`);
  `void Desactivar()`, `void Reactivar()`, `void CambiarRol(Rol nuevo)`; propiedades de sólo lectura.

- [ ] **Paso 1: tests que fallan:** `001/RF-11: un usuario nuevo está activo y normaliza el nombre`
  (`"Ana.Mesera"` → `"ana.mesera"`); `001/RF-15: desactivar y reactivar`; `001/RF-16: cambiar rol`.
- [ ] **Paso 2:** ejecutar → falla. **Paso 3:** implementar. **Paso 4:** ejecutar → pasa.
- [ ] **Paso 5:** commit `feat(domain): entidad Usuario y Rol`.

### T-03 [RF-04, RF-13, RF-21]: Pipeline de Application (validación y logging)

**Archivos:**
- Crear: `src/Restaurante.Application/Comun/Behaviors/ValidacionBehavior.cs`,
  `.../LoggingBehavior.cs`, `src/Restaurante.Application/DependencyInjection.cs`
- Test: `tests/Restaurante.Application.Tests/Comun/ValidacionBehaviorTests.cs`, `.../LoggingBehaviorTests.cs`

**Interfaces:**
- Produce: `IServiceCollection AddApplicationServices(this IServiceCollection)` que registra MediatR
  (ensamblado de Application), los validators y los dos behaviours (validación primero).
- `ValidacionBehavior<TRequest,TResponse>` lanza `FluentValidation.ValidationException` con todos
  los errores; `LoggingBehavior` registra `"{Request} terminó en {Ms} ms: {Resultado}"` con
  `Resultado` = `ok` o `error` y relanza la excepción.

- [ ] **Paso 1: tests que fallan** (request de prueba con validator y logger de prueba):
  `001/RF-04: un request inválido no llega al handler y lanza ValidationException`;
  `001/RF-13: se informan todos los errores por campo`;
  `001/RF-21: registra nombre, duración y resultado`;
  `001/RF-21: no registra el contenido del request` (el request lleva `Contrasena = "secreta123"`
  y ningún mensaje registrado la contiene);
  `001/RF-21: registra error y relanza la excepción`.
- [ ] **Pasos 2–4:** falla → implementar → pasa. **Paso 5:** commit `feat(app): behaviours de validación y logging`.

### T-04 [RF-01, RF-02, RF-03, RF-04]: Caso de uso `IniciarSesion`

**Archivos:**
- Crear: `src/Restaurante.Application/Abstracciones/{IUsuarioRepository,IHasherContrasenas,IEmisorTokens,IReloj}.cs`
- Crear: `src/Restaurante.Application/Comun/Excepciones/{CredencialesInvalidasException,NoEncontradoException,ConflictoException}.cs`
- Crear: `src/Restaurante.Application/Features/Auth/IniciarSesion/{IniciarSesionCommand,IniciarSesionValidator,IniciarSesionHandler,LoginRespuestaDto}.cs`
- Test: `tests/Restaurante.Application.Tests/Features/Auth/IniciarSesionTests.cs` y dobles en
  `tests/Restaurante.Application.Tests/Dobles/`

**Interfaces:**
- Produce:
  - `IUsuarioRepository`: `Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct)`,
    `Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string nombreNormalizado, CancellationToken ct)`,
    `Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct)`,
    `Task<int> ContarAdministradoresActivosAsync(CancellationToken ct)`,
    `Task<bool> ExisteAlgunoAsync(CancellationToken ct)`, `void Agregar(Usuario usuario)`,
    `Task GuardarCambiosAsync(CancellationToken ct)`.
  - `IHasherContrasenas`: `string Hashear(string contrasena)`, `bool Verificar(string hash, string contrasena)`.
  - `IEmisorTokens`: `TokenEmitido Emitir(Usuario usuario)` con `record TokenEmitido(string Token, DateTimeOffset ExpiraEn)`.
  - `IReloj`: `DateTimeOffset Ahora { get; }`.
  - `record IniciarSesionCommand(string NombreUsuario, string Contrasena) : IRequest<LoginRespuestaDto>`;
    `record LoginRespuestaDto(string Token, DateTimeOffset ExpiraEn, string NombreUsuario, string Rol)`
    (rol en minúsculas).
  - Las tres excepciones con constructor `(string mensaje)`; `CredencialesInvalidasException`
    usa siempre `"Usuario o contraseña incorrectos"`.

- [ ] **Paso 1: tests que fallan:** `001/RF-01: credenciales válidas devuelven token y rol`;
  `001/RF-01: el nombre se busca sin distinguir mayúsculas` (*punto de revisión 1*);
  `001/RF-01: el nombre se recorta antes de buscar` (*punto de revisión 2*);
  `001/RF-02: usuario inexistente y contraseña incorrecta dan la misma excepción y mensaje`;
  `001/RF-03: usuario desactivado da CredencialesInvalidasException`;
  `001/RF-04: nombre o contraseña vacíos no superan el validator`.
- [ ] **Pasos 2–4:** falla → implementar → pasa. **Paso 5:** commit `feat(app): caso de uso IniciarSesion`.

### T-05 [RF-11, RF-12, RF-13, RF-14]: Casos de uso `CrearUsuario` y `ListarUsuarios`

**Archivos:**
- Crear: `src/Restaurante.Application/Features/Usuarios/UsuarioDto.cs`,
  `.../CrearUsuario/{CrearUsuarioCommand,CrearUsuarioValidator,CrearUsuarioHandler}.cs`,
  `.../ListarUsuarios/{ListarUsuariosQuery,ListarUsuariosHandler}.cs`
- Test: `tests/Restaurante.Application.Tests/Features/Usuarios/CrearUsuarioTests.cs`, `.../ListarUsuariosTests.cs`

**Interfaces:**
- Consume: abstracciones de T-04, `Usuario`/`Rol` de T-02.
- Produce: `record UsuarioDto(Guid Id, string NombreUsuario, string Rol, bool Activo)` (rol en
  minúsculas); `record CrearUsuarioCommand(string NombreUsuario, string Contrasena, string Rol) : IRequest<UsuarioDto>`;
  `record ListarUsuariosQuery : IRequest<IReadOnlyList<UsuarioDto>>` (ordenado por nombre).
- Validator: nombre `^[A-Za-z0-9._-]{3,50}$`; contraseña ≥ 8; rol ∈ {`mesero`, `cocina`, `caja`,
  `administrador`} exacto (minúsculas).

- [ ] **Paso 1: tests que fallan:** `001/RF-11: crea usuario activo con hash y devuelve DTO sin contraseña`;
  `001/RF-12: nombre duplicado sin distinguir mayúsculas lanza ConflictoException`;
  `001/RF-13: valida formato de nombre, longitud de contraseña y rol` (`[Theory]`: `"ab"`,
  `"con espacio"`, contraseña de 7, `"Caja"`, `"chef"`);
  `001/RF-14: lista usuarios ordenados sin datos sensibles`.
- [ ] **Pasos 2–4:** falla → implementar → pasa. **Paso 5:** commit `feat(app): crear y listar usuarios`.

### T-06 [RF-15, RF-16, RF-17, RF-18, RF-19]: Casos de uso de estado y rol

**Archivos:**
- Crear: `src/Restaurante.Application/Abstracciones/IUsuarioActual.cs`,
  `.../Features/Usuarios/CambiarEstadoUsuario/{CambiarEstadoUsuarioCommand,CambiarEstadoUsuarioHandler}.cs`,
  `.../Features/Usuarios/CambiarRolUsuario/{CambiarRolUsuarioCommand,CambiarRolUsuarioValidator,CambiarRolUsuarioHandler}.cs`
- Test: `tests/Restaurante.Application.Tests/Features/Usuarios/CambiarEstadoUsuarioTests.cs`, `.../CambiarRolUsuarioTests.cs`

**Interfaces:**
- Produce: `IUsuarioActual { Guid Id { get; } }`;
  `record CambiarEstadoUsuarioCommand(Guid Id, bool Activo) : IRequest<UsuarioDto>`;
  `record CambiarRolUsuarioCommand(Guid Id, string Rol) : IRequest<UsuarioDto>`.

- [ ] **Paso 1: tests que fallan:** `001/RF-15: desactiva y reactiva`; `001/RF-16: cambia el rol`;
  `001/RF-17: no desactiva al último administrador activo`;
  `001/RF-17: no quita el rol al último administrador activo`;
  `001/RF-17: sí desactiva a un administrador si queda otro activo`;
  `001/RF-18: un administrador no puede desactivarse a sí mismo`;
  `001/RF-19: id inexistente lanza NoEncontradoException` (ambos comandos).
- [ ] **Pasos 2–4:** falla → implementar → pasa. **Paso 5:** commit `feat(app): cambiar estado y rol de usuarios`.

### T-07 [RF-01, RF-08, RF-12, RF-26]: Persistencia y servicios de Infrastructure

**Archivos:**
- Crear: `src/Restaurante.Infrastructure/Persistencia/{RestauranteDbContext,UsuarioConfiguration,UsuarioRepository,AplicadorMigraciones}.cs`,
  migración inicial en `Persistencia/Migraciones/`
- Crear: `src/Restaurante.Infrastructure/Seguridad/{HasherContrasenas,EmisorTokensJwt}.cs`,
  `src/Restaurante.Infrastructure/Reloj.cs`, `src/Restaurante.Infrastructure/DependencyInjection.cs`
- Test: `tests/Restaurante.Infrastructure.Tests/Seguridad/*Tests.cs` (sin Docker) y
  `tests/Restaurante.Infrastructure.Tests/Persistencia/*Tests.cs` (`Integracion`, Testcontainers `postgres:16`)

**Interfaces:**
- Consume: abstracciones de T-04/T-06, `ResultadoConfiguracion` de T-01.
- Produce: `IServiceCollection AddInfrastructureServices(this IServiceCollection, ResultadoConfiguracion config)`;
  `AplicadorMigraciones.AplicarAsync(bool aplicar, CancellationToken ct)`; tabla y columnas según
  `diseno.md` (§ Datos); `UsuarioRepository.GuardarCambiosAsync` traduce la violación del índice
  único (`PostgresErrorCodes.UniqueViolation`) a `ConflictoException`.

- [ ] **Paso 1: tests que fallan:**
  - Sin Docker: `001/RF-01: el token lleva sub, unique_name, role y expira a las 8 h`
    (`IReloj` fijo; decodificar con `JwtSecurityTokenHandler`);
    `001/RF-08: el hash verifica la contraseña y no la contiene`.
  - `Integracion`: `001/RF-26: aplicar migraciones crea la tabla usuarios`;
    `001/RF-26: con aplicar=false no crea la tabla`;
    `001/RF-12: el índice único no distingue mayúsculas` y
    `001/RF-12: alta duplicada concurrente lanza ConflictoException` (*punto de revisión 3*).
- [ ] **Paso 2:** ejecutar ambos filtros → falla. **Paso 3:** implementar y generar la migración
  (`dotnet ef migrations add Inicial`). **Paso 4:** ambos filtros → pasan (el de `Integracion`
  sólo donde haya Docker; si no, anotar en el commit que queda para el cierre).
- [ ] **Paso 5:** commit `feat(infra): persistencia, hash de contraseñas y emisión de JWT`.

### T-08 [RF-08, RF-09, RF-10]: Administrador inicial

**Archivos:**
- Crear: `src/Restaurante.Infrastructure/Arranque/InicializadorAdministrador.cs`
- Test: `tests/Restaurante.Infrastructure.Tests/Arranque/InicializadorAdministradorTests.cs` (dobles)
  y un caso `Integracion` contra PostgreSQL

**Interfaces:**
- Produce: `Task<ResultadoInicializacion> InicializarAsync(IReadOnlyDictionary<string, string?> entorno, CancellationToken ct)`
  con `bool Ok` y `string? Error` (nombre de la variable que falta, sin valores).

- [ ] **Paso 1: tests que fallan:** `001/RF-08: sin usuarios crea un administrador activo con hash`;
  `001/RF-09: con usuarios no crea ni modifica nada`;
  `001/RF-10: sin usuarios y sin ADMIN_PASSWORD devuelve error con el nombre de la variable`
  (y lo mismo con `ADMIN_USERNAME`); `Integracion`: `001/RF-08: crea el administrador en PostgreSQL`.
- [ ] **Pasos 2–4:** falla → implementar → pasa. **Paso 5:** commit `feat(infra): administrador inicial desde el entorno`.

### T-09 [RF-05, RF-06, RF-07, RF-20, RF-22]: Host WebApi, autenticación y filtros

**Archivos:**
- Crear: `src/Restaurante.WebApi/Program.cs`, `.../DependencyInjection.cs`,
  `.../Filtros/{RequiereRolAttribute,FiltroExcepciones}.cs`, `.../Seguridad/UsuarioActual.cs`
- Crear: `tests/Restaurante.WebApi.Tests/Infraestructura/FabricaApi.cs` (`WebApplicationFactory<Program>`
  con entorno de prueba, `IUsuarioRepository` en memoria, health check reemplazable y un
  controller de prueba con `[RequiereRol(Rol.Caja)]` y otro que lanza una excepción)
- Test: `tests/Restaurante.WebApi.Tests/Seguridad/AutorizacionTests.cs`, `.../Errores/FiltroExcepcionesTests.cs`,
  `.../Configuracion/ArranqueTests.cs`

**Interfaces:**
- Consume: `AddApplicationServices`, `AddInfrastructureServices`, `ConfiguracionEntorno`,
  `InicializadorAdministrador`, `AplicadorMigraciones`.
- Produce: `public partial class Program` (para la fábrica); `[RequiereRol(params Rol[] roles)]`;
  orden de arranque de `diseno.md` (§ Arquitectura); si la configuración o el inicializador fallan,
  registra el error y termina con código ≠ 0 sin construir la API.

- [ ] **Paso 1: tests que fallan:** `001/RF-05: sin token responde 401 ProblemDetails`;
  `001/RF-05: token con otra clave responde 401`; `001/RF-05: token expirado responde 401`;
  `001/RF-06: rol no autorizado responde 403 ProblemDetails`;
  `001/RF-07: el endpoint de prueba anónimo no exige token`;
  `001/RF-20: excepción no controlada responde 500 con traceId y sin detalle`
  (el cuerpo no contiene el mensaje de la excepción);
  `001/RF-22: la API no arranca sin JWT_SIGNING_KEY` (la fábrica falla al iniciar).
- [ ] **Pasos 2–4:** falla → implementar → pasa. **Paso 5:** commit `feat(api): host, JWT y filtros de autorización y errores`.

### T-10 [RF-01, RF-02, RF-03, RF-04, RF-07]: Endpoint de login

**Archivos:**
- Crear: `src/Restaurante.WebApi/Controllers/AuthController.cs`, `docs/context/http/auth.http`
- Test: `tests/Restaurante.WebApi.Tests/Auth/LoginTests.cs`

**Interfaces:**
- Produce: `POST /api/auth/login` con cuerpo `{ "nombreUsuario", "contrasena" }` → 200
  `LoginRespuestaDto` (JSON camelCase).

- [ ] **Paso 1: tests que fallan:** `001/RF-01: login correcto devuelve token utilizable en un endpoint protegido`;
  `001/RF-02: credenciales incorrectas → 401 con "Usuario o contraseña incorrectos"`;
  `001/RF-03: usuario desactivado → 401 con el mismo mensaje`;
  `001/RF-04: cuerpo sin contraseña → 400 ValidationProblemDetails con el campo`;
  `001/RF-07: login no exige token`.
- [ ] **Pasos 2–4:** falla → implementar (y `auth.http`) → pasa. **Paso 5:** commit `feat(api): endpoint de login`.

### T-11 [RF-11, RF-12, RF-13, RF-14, RF-15, RF-16, RF-17, RF-18, RF-19]: Endpoints de usuarios

**Archivos:**
- Crear: `src/Restaurante.WebApi/Controllers/UsuariosController.cs`, `docs/context/http/usuarios.http`,
  actualizar `docs/GLOSSARY.md` (DTOs)
- Test: `tests/Restaurante.WebApi.Tests/Usuarios/UsuariosEndpointsTests.cs`

**Interfaces:**
- Produce: `[ApiController]`, `[RequiereRol(Rol.Administrador)]`; `GET /api/usuarios`;
  `POST /api/usuarios` (201 + `Location: /api/usuarios/{id}`); `PUT /api/usuarios/{id:guid}/estado`
  (`{ "activo": bool }`); `PUT /api/usuarios/{id:guid}/rol` (`{ "rol": string }`).

- [ ] **Paso 1: tests que fallan:** `001/RF-11: alta → 201 con Location y sin contraseña`;
  `001/RF-12: duplicado → 409`; `001/RF-13: datos inválidos → 400 por campo`;
  `001/RF-13: JSON mal formado y rol "Caja" → 400` (*punto de revisión 4*);
  `001/RF-14: listado → 200`; `001/RF-15: desactivar → 200 con activo=false`;
  `001/RF-16: cambiar rol → 200`; `001/RF-17: último administrador → 409`;
  `001/RF-18: autodesactivación → 409`; `001/RF-19: id inexistente → 404`;
  `001/RF-19: id que no es GUID → 404` (*punto de revisión 5*);
  y un usuario `mesero` recibe 403 en `GET /api/usuarios`.
- [ ] **Pasos 2–4:** falla → implementar (y `usuarios.http`) → pasa. **Paso 5:** commit `feat(api): endpoints de gestión de usuarios`.

### T-12 [RF-23, RF-24, RF-25]: Salud, CORS y Swagger

**Archivos:**
- Modificar: `src/Restaurante.WebApi/DependencyInjection.cs`, `Program.cs`
- Crear: `docs/context/http/health.http`
- Test: `tests/Restaurante.WebApi.Tests/Plataforma/{SaludTests,CorsTests,SwaggerTests}.cs`

- [ ] **Paso 1: tests que fallan:** `001/RF-23: /health 200 con base accesible` y
  `001/RF-23: /health 503 con base inaccesible` (health check de prueba);
  `001/RF-24: origen permitido recibe Access-Control-Allow-Origin` y
  `001/RF-24: origen no listado no la recibe`;
  `001/RF-25: Swagger disponible en Development con esquema Bearer` y
  `001/RF-25: Swagger no disponible en Production` (404 en `/swagger/v1/swagger.json`).
- [ ] **Pasos 2–4:** falla → implementar → pasa. **Paso 5:** commit `feat(api): salud, CORS y Swagger`.

### T-13 [RF-23, RF-26]: Despliegue con Docker y onboarding

**Archivos:**
- Crear: `apps/api/Dockerfile`, `apps/api/.dockerignore`, `docker-compose.yml` (servicios `db` y
  `api`), `.env.example`; añadir `.env` a `.gitignore`; crear `ONBOARDING.md`
- Test: `tests/Restaurante.Infrastructure.Tests/Despliegue/SaludConPostgresTests.cs` (`Integracion`)

- [ ] **Paso 1: test que falla** (`Integracion`): `001/RF-23: /health responde 200 contra PostgreSQL real`
  (API en `WebApplicationFactory` con la cadena del contenedor y `APPLY_MIGRATIONS=true`) y
  `001/RF-26: la API arranca con migraciones aplicadas`.
- [ ] **Paso 2:** ejecutar con Docker → falla (o anotar que queda para el cierre si no hay Docker).
- [ ] **Paso 3:** crear `Dockerfile`, `docker-compose.yml` y `.env.example` según `diseno.md`
  (§ Despliegue) con *placeholders*; `ONBOARDING.md` con `cp .env.example .env`,
  `docker compose up` y cómo ejecutar las dos suites.
- [ ] **Paso 4:** donde haya Docker: `docker compose up -d` y `curl -s localhost:5000/health` → 200
  (RNF-03); `comando_test` → código 0.
- [ ] **Paso 5:** commit `feat(deploy): Dockerfile, docker-compose y onboarding`.
