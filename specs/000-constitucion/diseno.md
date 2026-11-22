---
feature: 000-constitucion
estado: borrador
---
# Diseño: Constitución del proyecto

## Enfoque elegido y alternativas descartadas

- **Elegido:** la constitución recoge sólo decisiones (stack, arquitectura, patrones, datos,
  autenticación, despliegue, pruebas y principios). La plataforma base se construye en la
  feature `001-plataforma-base` por el flujo normal (análisis, plan, TDD).
- **Descartado:** construir el esqueleto dentro de la constitución (el harness no permite
  código en ella y sería código sin pruebas) y empezar por una funcionalidad de negocio (cada
  feature tendría que montar su parte de la plataforma).
- Decisiones del backend tomadas por el usuario: .NET 10 / ASP.NET Core, MediatR 12.x,
  FluentValidation, RestSharp/HttpClient, Swagger; Clean Architecture en 4 capas; CQRS +
  Mediator; behaviours de validación y logging; filtros MVC de autorización y errores; DTOs;
  contenedor de DI nativo con `AddXxxServices()` por capa; PostgreSQL + EF Core; JWT propio
  con roles; Docker + `docker-compose`.

## Frontend (fijo del harness)

- **React 18+ con TypeScript**, empaquetado con **Vite**. Código en `apps/web/`.
- **Vitest + Testing Library + jsdom** para componentes y lógica (TDD rápido). La configuración
  de Vitest excluye `e2e/` y `e2e-stack/`, que son de Playwright.
- **Playwright** (`@playwright/test`) para los flujos de punta a punta, en dos suites:
  - `apps/web/e2e/`: la app servida por Vite con la API simulada (`page.route`). Forma parte
    de `comando_test` y bloquea cada tarea; toda tarea con flujo visible tiene aquí su prueba.
  - `apps/web/e2e-stack/`: contra el sistema completo levantado con `docker compose`
    (`E2E_BASE_URL`, por defecto `http://localhost:8080`, el servicio `web`), sin
    simulaciones. Se ejecuta con `comando_e2e_stack` y bloquea el cierre de la feature.
- Cada test lleva el RF calificado en su nombre: `it("NNN/RF-NN: …")` en Vitest y
  `test("NNN/RF-NN: …")` en Playwright.
- En las tareas con flujo visible (`UI: sí`), la fase verde se itera con **Ralph Loop**.
- La URL base del API llega en tiempo de build por `VITE_API_URL`. En producción `nginx` sirve
  los estáticos.

## Backend

Microservicio **.NET 10 / ASP.NET Core** con **base de datos propia** (PostgreSQL), en
`apps/api/`. Paquetes: **MediatR 12.x** (CQRS/Mediator; se fija la rama 12.x, la última con
licencia Apache 2.0, y no se actualiza a 13+ sin decisión explícita), FluentValidation, Entity Framework Core con
Npgsql, RestSharp/HttpClient (servicios externos), Swashbuckle (Swagger) y
`Microsoft.AspNetCore.Authentication.JwtBearer`. Mapeo entre entidades y DTOs a mano (sin
librerías de mapeo).

## Arquitectura

Clean Architecture en 4 capas, con las dependencias apuntando hacia adentro:

```
WebApi ──► Application ──► Domain
   │            ▲
   └──► Infrastructure ──┘
```

- **Domain**: entidades, value objects y reglas de negocio. Sin referencias a paquetes externos.
- **Application**: casos de uso como Commands/Queries de MediatR con sus Handlers y Validators;
  interfaces de repositorios, de servicios externos y de reloj/usuario actual; DTOs; pipeline
  behaviours. Depende sólo de Domain.
- **Infrastructure**: `DbContext` de EF Core, migraciones, repositorios, hashing de contraseñas,
  emisión de JWT y clientes HTTP. Implementa las interfaces de Application.
- **WebApi**: controllers delgados, filtros MVC, configuración de autenticación, CORS, Swagger y
  arranque. Es la raíz de composición.

Inyección de dependencias con el contenedor nativo: cada capa expone `AddDomainServices()` (si
hace falta), `AddApplicationServices()`, `AddInfrastructureServices(config)` y
`AddWebApiServices()`, y `Program.cs` sólo los encadena.

**Excepciones (constitución, Art. 8):** los clientes HTTP hacia servicios externos se crean con
`new` dentro de Infrastructure (una instancia reutilizable por servicio, detrás de una
interfaz), y la configuración se lee de variables de entorno.

Estructura de la solución:

```
apps/api/
  Restaurante.sln
  src/  Restaurante.Domain  Restaurante.Application  Restaurante.Infrastructure  Restaurante.WebApi
  tests/ Restaurante.Domain.Tests  Restaurante.Application.Tests
         Restaurante.Infrastructure.Tests  Restaurante.WebApi.Tests  Restaurante.Architecture.Tests
```

## Componentes

- **Autenticación JWT** (Infrastructure + WebApi): command `IniciarSesion` valida usuario y
  contraseña contra el hash guardado y emite un JWT con `sub`, `role` y `exp` (8 h), firmado con
  la clave de `JWT_SIGNING_KEY`. Credenciales inválidas → 401 genérico. Cubre: RF-01, RF-02
- **Autorización por rol** (WebApi): esquema JwtBearer + filtro MVC que exige el rol declarado
  en cada acción (`administrador_plataforma`, `administrador_general`, `administrador_sucursal`,
  `cajero`, `cocinero`; ver
  constitución Art. 9); sin token o token expirado → 401,
  rol no permitido → 403. Sólo login y salud son anónimos. Cubre: RF-03, RF-04
- **Behaviour de validación** (Application): ejecuta los validators de FluentValidation del
  request y, si fallan, lanza una excepción de validación con los errores por campo.
  Cubre: RF-05
- **Filtro de excepciones** (WebApi): convierte la excepción de validación en 400
  `ValidationProblemDetails` y cualquier otra en 500 `ProblemDetails` con `traceId`, sin
  detalle interno; registra la excepción completa en el log. Cubre: RF-05, RF-06
- **Behaviour de logging** (Application): registra nombre del request, duración y resultado,
  sin volcar el contenido del request. Cubre: RF-07
- **Validación de configuración** (WebApi): al arrancar comprueba `DB_CONNECTION_STRING`,
  `JWT_SIGNING_KEY` y `CORS_ALLOWED_ORIGINS`; si falta alguna, registra su nombre y termina.
  Cubre: RF-08
- **Endpoint de salud** (WebApi): `GET /health` con el health check de EF Core/PostgreSQL;
  200 si conecta, 503 si no. Cubre: RF-09
- **Política CORS** (WebApi): sólo los orígenes de `CORS_ALLOWED_ORIGINS` (separados por coma);
  nunca `AllowAnyOrigin`. Cubre: RF-10
- **Swagger** (WebApi): Swashbuckle con el esquema Bearer, activo sólo en `Development`.
  Cubre: RF-11

## Datos

- PostgreSQL 16+ accedido con EF Core (proveedor Npgsql); un `DbContext` en Infrastructure.
- El esquema cambia sólo con migraciones de EF Core versionadas en
  `apps/api/src/Restaurante.Infrastructure/Persistencia/Migraciones/`.
- La API aplica las migraciones pendientes al arrancar sólo si `APPLY_MIGRATIONS=true`
  (`docker-compose` lo activa en desarrollo).
- Acceso a datos siempre mediante repositorios definidos como interfaces en Application.
- Multi-restaurante y multi-sucursal (constitución Art. 12): `RestaurantId` en toda entidad de
  restaurante y `BranchId` en las operativas, con filtros globales de EF Core por el contexto
  (usuario autenticado o canal); menú global dentro del restaurante.
- Identidad visual por restaurante (constitución Art. 15): nombre, logo y paleta configurables;
  el frontend usa tokens de tema con los valores por defecto de Nunito y negro/naranja.
- Borrado lógico (constitución Art. 13): `IsActive` / `DeletedAt` con filtro global; snapshot de
  nombre y precio en las líneas de pedido.
- Tabla inicial de usuarios (en 001): identificador, nombre de usuario único, hash de
  contraseña, rol y activo.

## Manejo de errores

- Errores de validación → 400 `ValidationProblemDetails` (RF-05).
- No autenticado → 401; sin permiso → 403 (RF-03, RF-04).
- Recurso inexistente → 404 `ProblemDetails` (excepción de dominio "no encontrado").
- Cualquier otra excepción → 500 `ProblemDetails` con `traceId` y sin detalle interno (RF-06).
- Toda excepción se registra en el log con su `traceId`; nunca con datos sensibles.

## Despliegue

- `docker-compose.yml` en la raíz con tres servicios: `web` (build de `apps/web`, servido por
  `nginx`), `api` (imagen de `apps/api`) y `db` (`postgres:16`, volumen persistente).
- Variables en un `.env` fuera del repo; se versiona sólo `.env.example` con *placeholders*.
- Variables: `DB_CONNECTION_STRING`, `JWT_SIGNING_KEY`, `CORS_ALLOWED_ORIGINS`, `E2E_BASE_URL`,
  `APPLY_MIGRATIONS`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `VITE_API_URL`, y las
  URLs/credenciales de servicios externos que añadan las features.

## Convenciones del proyecto

- Identificadores del dominio y de los casos de uso en español (`Pedido`, `IniciarSesion`,
  `Persistencia`); sufijos y términos técnicos del framework en inglés (`Command`, `Query`,
  `Handler`, `Validator`, `Controller`, `Repository`, `Dto`). Textos de la interfaz,
  documentación y specs en español.
- Un archivo por clase; carpetas por caso de uso en Application
  (`Features/<Área>/<CasoDeUso>/`: Command/Query, Handler, Validator, DTO).
- Controllers sin lógica: reciben el DTO, envían a MediatR y devuelven la respuesta.
- Las entidades de dominio nunca salen por la API: siempre DTOs.
- Cada endpoint nuevo o tocado tiene su archivo `.http` en `docs/context/http/`.

## Estrategia de pruebas

- **Frontend:** Vitest + Testing Library (`it("NNN/RF-NN: …")`) para componentes y lógica;
  Playwright (`test("NNN/RF-NN: …")`) con la suite simulada en `apps/web/e2e/` en cada tarea con
  flujo visible y la suite completa en `apps/web/e2e-stack/` al cerrar la feature.
- **Backend:** xUnit, con el RF en el nombre visible:
  `[Fact(DisplayName = "NNN/RF-NN: …")]`.
  - Domain y Application: pruebas unitarias (handlers con dobles de las interfaces).
  - Infrastructure: pruebas de integración contra PostgreSQL real con Testcontainers, marcadas
    con `[Trait("Category", "Integracion")]` porque necesitan Docker. No entran en
    `comando_test`; se ejecutan con `comando_e2e_stack` al cerrar la feature.
  - WebApi: pruebas de integración con `WebApplicationFactory` (autenticación, autorización,
    errores, CORS, salud).
  - Architecture: NetArchTest verifica la regla de dependencias entre capas.
- `comando_test` ejecuta Vitest, la suite simulada de Playwright y las pruebas del backend que no
  necesitan Docker (`--filter "Category!=Integracion"`); debe quedar en verde para cerrar
  cualquier tarea.
- `comando_e2e_stack` ejecuta las pruebas de integración del backend (Testcontainers) y la suite
  completa de Playwright con el sistema levantado; debe pasar para aceptar la última HU de una
  feature. Requiere Docker.

## Configuración del harness

```harness-config
{ "stack": "React + TypeScript + Vite + Vitest + Playwright; backend: .NET 10 / ASP.NET Core + PostgreSQL (EF Core)",
  "frontend": "react-ts-vite-vitest-playwright",
  "rutas_codigo": ["apps/web/", "apps/api/", "docker-compose.yml", ".env.example"],
  "rutas_libres": ["README.md", ".gitignore", ".editorconfig"],
  "comando_test": "(cd apps/web && npx vitest run --passWithNoTests && npx playwright test --pass-with-no-tests) && dotnet test apps/api --filter \"Category!=Integracion\"",
  "comando_e2e_stack": "dotnet test apps/api --filter \"Category=Integracion\" && cd apps/web && npx playwright test --config playwright.stack.config.ts" }
```
