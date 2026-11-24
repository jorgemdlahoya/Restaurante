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
  contexto aunque llegue un token de restaurante; (2) token autenticado con `restaurant_id`:
  `ObtenerPorIdAsync`; inexistente o inactivo → 401 `No autenticado` / `El restaurante no está activo.` con
  `WWW-Authenticate: Bearer`; en `/api/r/{slug}…` con un `slug` (en minúsculas) distinto del suyo → 404
  `Restaurante no disponible`; si no, `Establecer(id)`; (3) `/api/r/{slug}…` sin restaurante en el token
  busca por el `slug` en minúsculas y responde 404 `Restaurante no disponible` si no existe o está
  inactivo; si no, `Establecer(id)`; (4) en el resto, ámbito de plataforma. Las respuestas se escriben con
  `RespuestasProblema.EscribirAsync`.
- `MiddlewareContextoSucursal` (tras el de restaurante): fija `ContextoSucursal` en
  `/api/r/{slug}/s/{sucursal}…` o responde 404 `Sucursal no disponible` (ver la sección de sucursales).
- `UsuariosController` bajo `api/r/{slug}/usuarios` (sólo `administrador_general`); el filtro global por
  contexto aísla los usuarios de cada restaurante (otro restaurante → 404).
- `IdentidadController`: `GET /api/r/{slug}/identidad` (anónimo; nombre, paleta y logo del restaurante).
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

El servicio `web` (`apps/web/Dockerfile`: etapa `node:22-alpine` con `npm ci && npm run build`, etapa
`nginx:alpine` con `dist/`; puerto 8080 -> 80, `depends_on: api`) sirve la SPA con
`apps/web/nginx.conf`: `/api/` y `/health` se reenvían a `http://api:8080` (cabeceras `Host` y
`X-Forwarded-*`), el resto cae en `index.html`, y `client_max_body_size 10m` deja que el límite de
tamaño del logo lo aplique la API (400) y no nginx (413). La interfaz y la API comparten origen
(`http://localhost:8080`), de modo que las llamadas relativas `/api/...` no necesitan CORS.

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

## Sucursales y filtro global por convención (feature 003)

Persistencia en `Restaurante.Infrastructure/Persistencia` (migración `Sucursales`, aditiva):

- `sucursales`: `id`, `restaurante_id` (FK a `restaurantes`, `Restrict`), `nombre` varchar(80),
  `direccion` varchar(200), `telefono` y `whatsapp` varchar(13) (E.164), `slug` varchar(40),
  `phone_number_id` varchar(30) nulo, `prefijo` varchar(3), `zona_horaria` varchar(40), `activa`,
  `creada_en`. Índices únicos `ix_sucursales_restaurante_nombre`, `ix_sucursales_restaurante_slug`,
  `ix_sucursales_restaurante_prefijo` (por restaurante), `ix_sucursales_whatsapp` e
  `ix_sucursales_phone_number_id` (filtrado `phone_number_id IS NOT NULL`), únicos en la plataforma.
- `horarios_sucursal`: PK (`sucursal_id`, `dia_semana` smallint 0–6, lunes = 0, con check),
  `restaurante_id`, `apertura` y `cierre` (`time`, nulos), `cerrado` (columna calculada almacenada
  `apertura IS NULL`; el dominio lo deriva igual). FK a `sucursales`.
- `cierres_temporales`: `id`, `restaurante_id`, `sucursal_id` (FK), `inicio` y `fin` (timestamptz, UTC),
  `motivo` varchar(120) nulo, `creado_en`, `cancelado_en` nulo; índice `ix_cierres_temporales_sucursal_fin`.
- `SucursalRepository` y `CierreTemporalRepository` (puertos `ISucursalRepository` e
  `ICierreTemporalRepository` en Application). `GuardarCambiosAsync` de sucursales traduce la violación de
  unicidad por `ConstraintName` a `ConflictoException` ("Ya existe una sucursal con ese nombre / slug /
  prefijo / WhatsApp / phone_number_id"). WhatsApp y `phone_number_id` se comprueban con
  `IgnoreQueryFilters` (toda la plataforma).
- `RestauranteDbContext(options, IContextoRestaurante, IContextoSucursal)`: además del filtro explícito de
  `Usuario` (sin cambios), `FiltrosGlobales.Aplicar` recorre el modelo y, por convención, filtra toda
  entidad `IEntidadDeRestaurante` (`Sucursal`) con `RestauranteId == RestauranteActualId` y toda
  `IEntidadDeSucursal` (`HorarioDia`, `CierreTemporal`) además con `SucursalId == SucursalActualId`. Las
  lambdas se construyen con `System.Linq.Expressions` sobre `Expression.Constant(contexto)` y las
  propiedades de instancia privadas del contexto, como haría una lambda de C#: EF las parametriza por
  contexto y el modelo cacheado sirve para todos. Con sucursal `null` no se ve ningún horario ni cierre;
  con restaurante `null`, ninguna sucursal. Una entidad nueva sólo tiene que implementar la interfaz.
- Registro: `AddInfrastructureServices` registra los repositorios pero no `IContextoSucursal`; lo hace
  `AddWebApiServices` con el `ContextoSucursal` por petición (scoped + reenvío a la interfaz, como
  `ContextoRestaurante`). La fábrica de diseño, `PostgresFixture.CrearContexto(cadena, restauranteId,
  sucursalId)` y las pruebas que construyen el DbContext fuera de la API usan `ContextoSucursalFijo`.
- `ContextoSucursal` (scoped, implementa `IContextoSucursal`) y `MiddlewareContextoSucursal` (en
  `Program.cs` justo después de `MiddlewareContextoRestaurante`): si la ruta cumple
  `^/api/r/[^/]+/s/([^/]+)` (sin distinguir mayúsculas) y hay restaurante en el contexto, busca
  `ObtenerPorSlugAsync(slug en minúsculas)` (el filtro de restaurante ya aplica, así que una sucursal de otro
  restaurante no se encuentra); nula → 404 `Sucursal no disponible` / `La dirección no corresponde a una
  sucursal de este restaurante.` (constantes `TituloNoDisponible` y `DetalleNoDisponible`); si existe,
  activa o no, `Establecer(id)`. Fuera de `/s/` el contexto queda en `null`.

## Frontend (`apps/web`)

React 19 + TypeScript + Vite 7, pruebas con Vitest 3 (jsdom, `globals: false`, limpieza en
`src/pruebas/setup.ts`) y Playwright (proyectos `escritorio` 1440x900 y `movil` 390x844).

- `src/main.tsx`: monta `QueryClientProvider` (TanStack Query), `ProveedorSesion` y `App`. No hay
  `ProveedorTema` global: `tema.css` ya fija la paleta por defecto en `:root` y uno externo pisaría la del
  restaurante.
- `src/App.tsx`: enrutador (`react-router`; exporta `rutas`). Rutas: `/` (redirige a `/plataforma/login`),
  `/plataforma/login`, `/plataforma/restaurantes` (`RutaProtegida`, rol `administrador_plataforma`),
  `/r/:slug` con hijas `login` e `inicio` (dentro de `DisenoRestaurante`) y una ruta `*` (también bajo
  `/r/:slug`) que muestra `PaginaNoEncontrada`.
- `src/api/`: `cliente.ts` (`pedirApi`, `ErrorApi`, `urlApi`; el token se pasa en cada llamada, nunca
  implícito) y `auth.ts` (login de plataforma y de restaurante).
- `src/sesion/`: `almacenSesion.ts` (persistencia en `sessionStorage`, clave `restaurante.sesion`) y
  `ProveedorSesion.tsx` (`useSesion`, cierre automático en `expiraEn`).
- `src/rutas/`: `RutaProtegida.tsx` (exige rol y, en restaurante, el `slug`; si no, redirige al login).
- `src/restaurante/`: `DisenoRestaurante` (layout de `/r/:slug`: carga la identidad con `api.ts`, aplica el
  tema con `ProveedorTema` y muestra `RestauranteNoDisponible` ante 404), `LoginRestaurante`,
  `InicioRestaurante`, `EncabezadoMarca`.
- `src/plataforma/`: `LoginPlataforma`, `RestaurantesPagina` (lista, activar/desactivar con
  `DialogoConfirmacion`), `FormularioRestaurante` (alta/edición con logo), `VistaPreviaPaleta` y `api.ts`.
- `src/componentes/`: piezas compartidas (`FormularioLogin`, `PaginaNoEncontrada`).
- `src/tema/`: `paleta.ts` (`Paleta`, `PALETA_POR_DEFECTO`, `resolverPaleta`, `aplicarPaleta`),
  `ProveedorTema.tsx` (escribe la paleta en variables CSS de `:root` en `useLayoutEffect` y restaura la
  paleta por defecto al desmontarse) y `tema.css` (variables `--color-principal`, `--color-acento`,
  `--color-oscuro`, `--color-fondo`, `--color-texto`, tipografía Nunito y estilos base).
- Regla RNF-02: ningún componente usa colores literales; sólo `var(--color-*)` (lo verifica
  `sinColoresLiterales.test.ts`, que excluye `paleta.ts` y `tema.css`).
- Desarrollo: `vite.config.ts` hace proxy de `/api` y `/health` a `http://localhost:5000`.
