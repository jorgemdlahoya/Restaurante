---
feature: 001-plataforma-base
estado: borrador
---
# Diseño: Plataforma base — backend

## Enfoque elegido y alternativas descartadas

- **Elegido:** casos de uso como Commands/Queries de MediatR en Application; autorización con un
  filtro MVC propio sobre el esquema JwtBearer; persistencia con un `DbContext` de EF Core y
  repositorios tras interfaces; controllers REST delgados. Sigue la constitución
  (Art. 3, 6, 7, 9, 10, 11).
- **Descartado:** ASP.NET Core Identity completo (tablas y flujos innecesarios; sólo se usa su
  `PasswordHasher`); sólo `[Authorize(Roles = …)]` (el usuario pidió filtros MVC y el filtro
  propio unifica las respuestas en `ProblemDetails`); Minimal APIs (la constitución define
  controllers).
- **Decisiones del usuario en este análisis:** dividir la plataforma en backend (001) y
  frontend (002); administrador inicial desde `ADMIN_USERNAME`/`ADMIN_PASSWORD`; gestión mínima
  de usuarios para el administrador; separar las pruebas con Testcontainers del comando de
  tests (aplicado en la constitución); las reglas de negocio de HU-04.

## Arquitectura

Solución en `apps/api/` con la estructura de la constitución:

```
apps/api/
  Restaurante.sln
  Dockerfile
  src/  Restaurante.Domain  Restaurante.Application  Restaurante.Infrastructure  Restaurante.WebApi
  tests/ Restaurante.Domain.Tests  Restaurante.Application.Tests
         Restaurante.Infrastructure.Tests  Restaurante.WebApi.Tests  Restaurante.Architecture.Tests
```

Dependencias: `WebApi → Application, Infrastructure`; `Infrastructure → Application`;
`Application → Domain`; `Domain` sin dependencias. Cada capa expone su `AddXxxServices()` y
`Program.cs` sólo los encadena.

Arranque de la API, en orden:
1. `ConfiguracionEntorno.Cargar()` valida las variables obligatorias (RF-22); si falla, registra
   la variable y el motivo y termina con código ≠ 0.
2. Se construye el host con los `AddXxxServices()`.
3. Si `APPLY_MIGRATIONS=true`, se aplican las migraciones (RF-26).
4. `InicializadorAdministrador` crea el administrador si no hay usuarios (RF-08 a RF-10).
5. La API empieza a atender peticiones.

## Componentes

**Domain**
- **`Usuario`** (entidad): `Id` (Guid), `NombreUsuario`, `NombreUsuarioNormalizado`
  (minúsculas invariantes), `HashContrasena`, `Rol`, `Activo`, `CreadoEn`. Métodos
  `Desactivar()`, `Reactivar()`, `CambiarRol(Rol)`. Cubre: RF-11, RF-15, RF-16
- **`Rol`** (enum): `Mesero`, `Cocina`, `Caja`, `Administrador`; en la API y en el JWT se
  serializa en minúsculas (`mesero`, `cocina`, `caja`, `administrador`). Cubre: RF-13

**Application**
- **Interfaces:** `IUsuarioRepository` (`ObtenerPorIdAsync`, `ObtenerPorNombreNormalizadoAsync`,
  `ListarAsync`, `ContarAdministradoresActivosAsync`, `ExisteAlgunoAsync`, `Agregar`,
  `GuardarCambiosAsync`), `IHasherContrasenas`, `IEmisorTokens`, `IUsuarioActual` (id del
  usuario del JWT), `IReloj`.
- **`IniciarSesionCommand`** + Validator + Handler: busca por nombre normalizado, verifica el
  hash y el estado, emite el token; cualquier fallo de credenciales o usuario inactivo lanza
  `CredencialesInvalidasException` con un único mensaje. Cubre: RF-01, RF-02, RF-03, RF-04
- **`CrearUsuarioCommand`** + Validator (formato del nombre, contraseña ≥ 8, rol válido) +
  Handler (unicidad sin distinguir mayúsculas → `ConflictoException`). Cubre: RF-11, RF-12, RF-13
- **`ListarUsuariosQuery`** + Handler → `UsuarioDto[]` (id, nombreUsuario, rol, activo).
  Cubre: RF-14
- **`CambiarEstadoUsuarioCommand`** + Handler: no encontrado → `NoEncontradoException`; propio
  usuario desactivándose → `ConflictoException`; desactivar al último administrador activo →
  `ConflictoException`. Cubre: RF-15, RF-17, RF-18, RF-19
- **`CambiarRolUsuarioCommand`** + Validator + Handler: no encontrado → `NoEncontradoException`;
  quitar el rol al último administrador activo → `ConflictoException`. Cubre: RF-16, RF-17, RF-19
- **`ValidacionBehavior`**: ejecuta los validators del request; si fallan, lanza
  `ValidationException` de FluentValidation. Cubre: RF-04, RF-13
- **`LoggingBehavior`**: registra nombre del request, duración (ms) y resultado; nunca el
  contenido del request. Cubre: RF-21
- **Excepciones de aplicación:** `CredencialesInvalidasException` (→ 401),
  `NoEncontradoException` (→ 404), `ConflictoException` (→ 409).
- **`UsuarioDto`**, `LoginRespuestaDto` (token, expiraEn, nombreUsuario, rol) y los DTOs de
  entrada; mapeo a mano.

**Infrastructure**
- **`RestauranteDbContext`** + `UsuarioConfiguration` (índice único sobre
  `NombreUsuarioNormalizado`; `Rol` como texto) + primera migración en
  `Persistencia/Migraciones/`. Cubre: RF-12, RF-26
- **`UsuarioRepository`**: implementa `IUsuarioRepository` sobre el `DbContext`. Cubre: RF-11,
  RF-14, RF-15, RF-16
- **`HasherContrasenas`**: envuelve `PasswordHasher<Usuario>` de ASP.NET Core Identity.
  Cubre: RF-01, RF-08
- **`EmisorTokensJwt`**: HS256 con `JWT_SIGNING_KEY`; claims `sub`, `unique_name`, `role`;
  expiración = `IReloj.Ahora + 8 h`. Cubre: RF-01
- **`InicializadorAdministrador`**: si `ExisteAlgunoAsync()` es falso, exige `ADMIN_USERNAME` y
  `ADMIN_PASSWORD` (si falta alguna, registra cuál y la API termina) y crea el administrador; si
  hay usuarios, no hace nada. Cubre: RF-08, RF-09, RF-10
- **`AplicadorMigraciones`**: aplica `Database.MigrateAsync()` sólo con `APPLY_MIGRATIONS=true`.
  Cubre: RF-26
- **`Reloj`** (implementa `IReloj` con `TimeProvider`).

**WebApi**
- **`ConfiguracionEntorno`**: lee y valida `DB_CONNECTION_STRING`, `JWT_SIGNING_KEY` (≥ 32
  caracteres) y `CORS_ALLOWED_ORIGINS` (lista separada por comas); ante un fallo registra la
  variable y el motivo, sin valores. Cubre: RF-22
- **`AuthController`**: `POST /api/auth/login` (anónimo) → `IniciarSesionCommand`.
  Cubre: RF-01, RF-07
- **`UsuariosController`**: `[RequiereRol(Rol.Administrador)]`; `GET /api/usuarios`,
  `POST /api/usuarios` (201 + `Location`), `PUT /api/usuarios/{id}/estado`
  (`{ "activo": bool }`), `PUT /api/usuarios/{id}/rol` (`{ "rol": "caja" }`).
  Cubre: RF-11, RF-14, RF-15, RF-16
- **`RequiereRolAttribute`** (filtro de autorización MVC): sin usuario autenticado (sin token,
  firma inválida o expirado) → 401 `ProblemDetails`; rol fuera de los permitidos → 403
  `ProblemDetails`. El esquema JwtBearer valida firma, emisor, audiencia y expiración con
  tolerancia de reloj cero. Cubre: RF-05, RF-06
- **`FiltroExcepciones`** (filtro de excepciones MVC): `ValidationException` → 400
  `ValidationProblemDetails`; `CredencialesInvalidasException` → 401; `NoEncontradoException` →
  404; `ConflictoException` → 409; cualquier otra → 500 con `traceId` y sin detalle, registrando
  la excepción con ese `traceId`. Cubre: RF-02, RF-03, RF-04, RF-12, RF-13, RF-17, RF-18, RF-19, RF-20
- **`UsuarioActual`**: implementa `IUsuarioActual` leyendo `sub` del `HttpContext`. Cubre: RF-18
- **Salud:** `GET /health` (anónimo) con el health check de EF Core sobre el `DbContext`;
  200 si conecta, 503 si no. Cubre: RF-07, RF-23
- **CORS:** política con los orígenes de `CORS_ALLOWED_ORIGINS`; nunca `AllowAnyOrigin`.
  Cubre: RF-24
- **Swagger:** Swashbuckle con el esquema Bearer, registrado sólo en `Development`.
  Cubre: RF-25

## Datos

Tabla `usuarios` (primera migración):

| Columna | Tipo | Notas |
|---|---|---|
| `id` | uuid | PK |
| `nombre_usuario` | varchar(50) | tal como se escribió |
| `nombre_usuario_normalizado` | varchar(50) | minúsculas; índice único |
| `hash_contrasena` | text | `PasswordHasher` |
| `rol` | varchar(20) | `mesero`, `cocina`, `caja`, `administrador` |
| `activo` | boolean | |
| `creado_en` | timestamptz | |

Los usuarios nunca se borran; sólo se desactivan.

## Manejo de errores

| Situación | Respuesta |
|---|---|
| Validación de entrada | 400 `ValidationProblemDetails` con errores por campo |
| Credenciales inválidas o usuario inactivo | 401 `ProblemDetails`, mensaje único "Usuario o contraseña incorrectos" |
| Sin token, token inválido o expirado | 401 `ProblemDetails` |
| Rol no autorizado | 403 `ProblemDetails` |
| Usuario inexistente | 404 `ProblemDetails` |
| Nombre duplicado, último administrador, autodesactivación | 409 `ProblemDetails` con el motivo |
| Excepción no controlada | 500 `ProblemDetails` con `traceId`, sin detalle interno |

Los mensajes de error para el cliente están en español.

## Despliegue

- `apps/api/Dockerfile` multi-etapa (`mcr.microsoft.com/dotnet/sdk:10.0` para compilar,
  `mcr.microsoft.com/dotnet/aspnet:10.0` para ejecutar), escuchando en el puerto 8080.
- `docker-compose.yml` en la raíz con `db` (`postgres:16`, volumen `datos-db`, health check
  `pg_isready`) y `api` (build de `apps/api`, `depends_on: db` con `condition: service_healthy`,
  puerto `5000:8080`, `APPLY_MIGRATIONS=true`).
- `.env.example` con *placeholders* para `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`,
  `DB_CONNECTION_STRING`, `JWT_SIGNING_KEY`, `CORS_ALLOWED_ORIGINS`
  (`http://localhost:5173,http://localhost:8080`), `ADMIN_USERNAME`, `ADMIN_PASSWORD`,
  `ASPNETCORE_ENVIRONMENT`. `.env` está en `.gitignore`.

## Documentación (constitución, Art. 5)

- `docs/context/http/auth.http` y `docs/context/http/usuarios.http`, con una variable `@token`
  del formato `.http` y credenciales de ejemplo como *placeholders*.
- `docs/context/glossary.md` (términos de dominio: usuario, rol, administrador inicial),
  `docs/GLOSSARY.md` (`Rol`, DTOs), `docs/context/architecture-map.md` (capas y dependencias) y
  `ONBOARDING.md` (cómo levantar el sistema con `docker compose` y ejecutar las pruebas).

## Estrategia de pruebas

Todas las pruebas con `[Fact(DisplayName = "001/RF-NN: …")]` (o `[Theory]`).

- **Domain.Tests:** reglas de `Usuario` (desactivar, reactivar, cambiar rol).
- **Application.Tests:** handlers con dobles de las interfaces (login, creación, unicidad,
  último administrador, autodesactivación, no encontrado), validators y los dos behaviours
  (el de logging comprueba que no registra el contenido del request).
- **WebApi.Tests:** `WebApplicationFactory` con `IUsuarioRepository` reemplazado por un doble en
  memoria y el health check reemplazable, sin Docker: códigos 200/201/400/401/403/404/409/500,
  forma de los `ProblemDetails`, CORS, Swagger sólo en `Development`, rutas anónimas, fallos
  de configuración al arrancar.
- **Infrastructure.Tests** (`[Trait("Category", "Integracion")]`, Testcontainers con
  `postgres:16`): migración aplicada, índice único sin distinguir mayúsculas, repositorio,
  inicializador del administrador (RF-08/RF-09) y `GET /health` 200 contra PostgreSQL real.
- **Architecture.Tests:** NetArchTest para RNF-02.
- `comando_test` ejecuta todo menos `Category=Integracion`; `comando_e2e_stack` ejecuta las de
  integración (y la suite completa de Playwright, vacía hasta la feature 002).
