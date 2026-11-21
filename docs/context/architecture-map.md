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
