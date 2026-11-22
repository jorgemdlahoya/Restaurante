# Mapa de arquitectura

Solución .NET en `apps/api/` (`Restaurante.sln`), proyectos `net10.0`.

## Capas

| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| Domain | `src/Restaurante.Domain` | Entidades y reglas de negocio. Sin dependencias. |
| Application | `src/Restaurante.Application` | Casos de uso (commands, handlers, validators) y puertos. |
| Infrastructure | `src/Restaurante.Infrastructure` | Persistencia y servicios externos. |
| WebApi | `src/Restaurante.WebApi` | Controllers, configuración por entorno y `Program.cs`. |

## Dependencias permitidas

```
WebApi → Application, Infrastructure
Infrastructure → Application
Application → Domain
Domain → (nada)
```

Reglas verificadas por `tests/Restaurante.Architecture.Tests` (001/RNF-02):
`Domain` no depende de `Application`, `Infrastructure`, `WebApi`, `MediatR` ni
`Microsoft.EntityFrameworkCore`; `Application` no depende de `Infrastructure` ni `WebApi`.

## Filtros y seguridad (WebApi)

En `src/Restaurante.WebApi/Filtros/` y `Seguridad/`:

- `RequiereRol(...)`: filtro de autorización por rol (claim `role`). Sin token válido responde
  401 y con otro rol 403, ambos como `ProblemDetails`. Todo endpoint lo exige salvo los marcados
  con `EndpointAnonimo` (login; `/health` no es un controller).
- `EndpointAnonimo`: marca explícita de endpoint sin autenticación; una prueba de guarda
  (`GuardaEndpointsTests`) falla si un endpoint no declara ninguna de las dos cosas.
- `FiltroExcepciones`: traduce excepciones de aplicación a `ProblemDetails` (validación 400,
  credenciales 401, no encontrado 404, conflicto 409, resto 500 sin detalles internos).
- `RespuestasProblema`: construye los 400 de validación con una única forma (también para los
  fallos de enlace del modelo).
- `UsuarioActual`: expone el usuario autenticado (id, rol) a la capa de aplicación.
- `ContextoRestaurante` (scoped, implementa `IContextoRestaurante`) y `MiddlewareContextoRestaurante`
  (en `Program.cs` justo después de `UseAuthentication()`). Reglas en orden: (1) `/api/plataforma…` no fija
  contexto aunque llegue un token de restaurante; (2) `/api/r/{slug}…` busca el restaurante por el `slug` en
  minúsculas y responde 404 `Restaurante no disponible` (con `RespuestasProblema.EscribirAsync`) si no
  existe o está inactivo; si no, `Establecer(id)`; (3) en el resto, provisionalmente, `Establecer` con el
  claim `restaurant_id` del token (T-08 completa la regla del token: 401 si el restaurante está inactivo y
  404 si no coincide con el `slug`).
- Logins: `AuthController` (`POST /api/r/{slug}/auth/login`) y `PlataformaAuthController`
  (`POST /api/plataforma/auth/login`) envían el mismo `IniciarSesionCommand`; el filtro global decide qué
  usuarios existen en cada ámbito.

JWT: HS256, emisor `restaurante-api`, audiencia `restaurante-web`, `ClockSkew = 0`.

## Arranque

Orden en `Program.cs`: **configuración** (`ConfiguracionEntorno.Cargar`, falla sin construir el
host si es inválida) → **host** (servicios y pipeline) → **migraciones** (sólo con
`APPLY_MIGRATIONS=true`) → **administrador inicial** (sólo si no hay usuarios; `ADMIN_USERNAME` /
`ADMIN_PASSWORD`) → **servir peticiones**. Los pasos 3 y 4 los orquesta `SecuenciaArranque`; si
fallan, el proceso termina con código 1.

Variables de entorno: `DB_CONNECTION_STRING`, `JWT_SIGNING_KEY` (>= 32 caracteres),
`CORS_ALLOWED_ORIGINS`, `APPLY_MIGRATIONS`, `ADMIN_USERNAME`, `ADMIN_PASSWORD`. Los errores de
configuración nombran la variable y el motivo, nunca el valor.

## Salud, CORS y Swagger

- `GET /health`: anónimo; 200 si la base de datos responde, 503 si no.
- CORS: política `OrigenesPermitidos` con los orígenes de `CORS_ALLOWED_ORIGINS` (lista separada
  por comas de orígenes `http(s)://host[:puerto]` sin ruta ni barra final). Se rechaza `*`
  (Art. 1.4: nunca `AllowAnyOrigin`).
- Swagger/OpenAPI con esquema Bearer: sólo en el entorno `Development`.

## Despliegue (Docker)

`docker-compose.yml` define `db` (PostgreSQL 16, volumen `datos-db`, healthcheck) y `api`
(`apps/api/Dockerfile`, puerto 5000 -> 8080, `APPLY_MIGRATIONS=true`). La configuración llega por
`.env` (plantilla `.env.example`, sin secretos). Ver `docs/ONBOARDING.md`.

## Pruebas

Un proyecto de pruebas por capa (`Domain`, `Application`, `Infrastructure`, `WebApi`) más
`Architecture`. Las pruebas con Testcontainers llevan `[Trait("Category", "Integracion")]`.

## Tablas de restaurantes (feature 002)

Persistencia en `Restaurante.Infrastructure/Persistencia` (`RestauranteRepository`, migración `Restaurantes`):

- `restaurantes`: `id`, `nombre` varchar(80), `slug` varchar(40), `activo`, `color_principal`,
  `color_acento`, `color_oscuro`, `color_fondo`, `color_texto` (`char(7)`, nulos; la `Paleta` es una
  `ComplexProperty`), `creado_en`. Índices únicos `ix_restaurantes_nombre` e `ix_restaurantes_slug`.
- `restaurantes_logo`: `restaurante_id` (PK y FK a `restaurantes`, borrado en cascada), `contenido`
  bytea, `tipo` varchar(20), `etag` varchar(64), `actualizado_en`.
- `GuardarCambiosAsync` traduce la violación de unicidad a `ConflictoException` según el índice
  (nombre, slug u otro).

## Usuarios por restaurante y filtro global (feature 002)

- `usuarios.restaurante_id` (uuid, nulo sólo para el administrador de plataforma) con FK a `restaurantes`
  (`Restrict`). El índice único `ix_usuarios_restaurante_nombre` (`restaurante_id`,
  `nombre_usuario_normalizado`, `NULLS NOT DISTINCT`) sustituye a `ix_usuarios_nombre_usuario_normalizado`
  (migración `UsuariosPorRestaurante`).
- `RestauranteDbContext(options, IContextoRestaurante)` aplica a `Usuario` el filtro global
  `RestauranteId == RestauranteActualId`, con `RestauranteActualId` como propiedad de instancia para que EF
  la parametrice por contexto. Todas las consultas de `UsuarioRepository` (listar, buscar, contar
  administradores, `ExisteAlgunoAsync`) quedan acotadas al restaurante; con contexto `null` sólo ven a
  los administradores de plataforma (así el `InicializadorAdministrador` comprueba "ya hay uno").
- `CrearUsuarioHandler` crea el usuario en `IContextoRestaurante.RestauranteId` y lanza
  `InvalidOperationException` si es `null`.
- Registro: `AddInfrastructureServices` no registra `IContextoRestaurante`; lo hace `AddWebApiServices`
  con el `ContextoRestaurante` por petición. La fábrica de diseño (`dotnet ef`) y las pruebas de
  integración usan `ContextoRestauranteFijo`. Las pruebas de la API usan el contexto y el middleware reales
  con un `UsuarioRepositoryEnMemoria` scoped que filtra el almacén compartido `FabricaApi.Usuarios`.
