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
  `ConflictoCamposException` (005) da 409 `Conflicto` con `errors` por campo en camelCase
  (`RespuestasProblema.ConClavesCamelCase`).
- `RespuestasProblema`: construye los 400 de validación con una única forma (también para los
  fallos de enlace del modelo). Un fallo de enlace de un parámetro de consulta (`?pagina=abc`) conserva su nombre
  como clave (`errors.pagina`); los de cuerpo siguen como `x` (de `$.x`) o `cuerpo`.
- Orden del pipeline: `UseExceptionHandler` → `UseStatusCodePages` → (Swagger en Development) → `UseCors` →
  `UseAuthentication` → `MiddlewareSesionUsuario` → `MiddlewareContextoRestaurante` →
  `MiddlewareContextoSucursal` → controllers.
- `UsuarioActual` (scoped, implementa `IUsuarioActual`: `Id`, `Rol`, `Sucursales`): contenedor por petición que
  fija `MiddlewareSesionUsuario` con `Establecer(usuario)`; lleva los datos vigentes en base de datos, no los
  del token. Leerlo sin usuario fijado lanza `InvalidOperationException`.
- `MiddlewareSesionUsuario` (en `Program.cs` justo después de `UseAuthentication()`, antes del de restaurante y
  también en `/api/plataforma…`; 004/RF-13, RF-14): sólo actúa si la petición está autenticada. Carga al
  usuario del claim `sub` con `IUsuarioRepository.ObtenerParaSesionAsync` (por clave primaria, sin filtro de
  restaurante) y responde 401 `Sesión no válida` / `Vuelve a iniciar sesión.` (constantes
  `TituloSesionInvalida` y `DetalleSesionInvalida`, con `WWW-Authenticate: Bearer`) si falta el claim `sello` o
  no es un Guid, el usuario no existe, está inactivo o su `SelloSesion` es otro. Si todo cuadra,
  `UsuarioActual.Establecer(usuario)`.
- `ContextoRestaurante` (scoped, implementa `IContextoRestaurante`) y `MiddlewareContextoRestaurante`
  (en `Program.cs` justo después de `MiddlewareSesionUsuario`). Reglas en orden: (1) `/api/plataforma…` no fija
  contexto aunque llegue un token de restaurante; (2) token autenticado con `restaurant_id`:
  `ObtenerPorIdAsync`; inexistente o inactivo → 401 `No autenticado` / `El restaurante no está activo.` con
  `WWW-Authenticate: Bearer`; en `/api/r/{slug}…` con un `slug` (en minúsculas) distinto del suyo → 404
  `Restaurante no disponible`; si no, `Establecer(id)`; (3) `/api/r/{slug}…` sin restaurante en el token
  busca por el `slug` en minúsculas y responde 404 `Restaurante no disponible` si no existe o está
  inactivo; si no, `Establecer(id)`; (4) en el resto, ámbito de plataforma. Las respuestas se escriben con
  `RespuestasProblema.EscribirAsync`.
- `MiddlewareContextoSucursal` (tras el de restaurante): fija `ContextoSucursal` en
  `/api/r/{slug}/s/{sucursal}…` o responde 404 `Sucursal no disponible` (ver la sección de sucursales).
- `UsuariosController` bajo `api/r/{slug}/usuarios` (`administrador_general` y `administrador_sucursal`; cajero y
  cocinero → 403). El alcance fino lo deciden los handlers con `ResolutorActor` y `AlcanceGestionUsuarios` (ver la
  sección de la feature 004); el filtro global por contexto aísla los usuarios de cada restaurante (otro
  restaurante → 404).
- `MiSesionController`: `GET /api/r/{slug}/mi-sesion` (cualquier rol de restaurante; 004/RF-16).
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
`.env` (plantilla `.env.example`, sin secretos). Ver `docs/ONBOARDING.md`. La imagen de la API instala
`tzdata` (la imagen base no lo trae): las zonas horarias de las sucursales (`America/Mexico_City`, etc.)
dependen de ella para resolver el horario y el estado de atención.

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
  `FabricaApi.EmitirToken` firma tokens de usuarios de ese almacén con su sello (siembra uno si no se pasa
  `usuarioId`); `FabricaApi.FirmarToken` firma claims a mano (p. ej. sin `sello`).

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
  sucursal de este restaurante.` (constantes `TituloNoDisponible` y `DetalleNoDisponible`). Desde 004
  (RF-15, RF-25), con sesión, todo rol salvo el administrador general recibe ese mismo 404 si la sucursal no está
  entre sus asignadas (`IUsuarioActual.Sucursales`) o está inactiva (nunca 403). Sin sesión (p. ej. el `estado`
  público) no cambia nada: si existe, activa o no, `Establecer(id)`; el administrador general también entra a
  cualquiera, activa o no. Fuera de `/s/` el contexto queda en `null`.

## Usuarios, roles y sucursales asignadas (feature 004)

Persistencia (migración `UsuariosSucursales`, aditiva):

- `usuarios.sello_sesion` (uuid, no nulo): sello de sesión del usuario; la migración da a cada usuario existente
  uno propio (`gen_random_uuid()`), así que los tokens anteriores (sin claim `sello`) dejan de valer.
- `usuarios_sucursales`: PK (`usuario_id`, `sucursal_id`), `restaurante_id`; FK a `usuarios` (cascada) y a
  `sucursales` (`Restrict`); índice `ix_usuarios_sucursales_sucursal`. Es la colección `_sucursales` de `Usuario`
  (`Usuario.Sucursales`); `UsuarioRepository` la incluye al leer (`ObtenerPorIdAsync`, `ListarAsync`,
  `ObtenerParaSesionAsync`, éste sin filtro de restaurante y sin seguimiento).
- `Usuario.AsignarRolYSucursales`, `CambiarContrasena` y `Desactivar` renuevan el sello (el primero sólo si cambia el
  rol o el conjunto de sucursales); `Reactivar` no.

Application (`Features/Usuarios`):

- `AlcanceGestionUsuarios`: reglas puras sobre un `ActorUsuarios` (`PuedeOperar`, `PuedeVer`, `PuedeGestionar`,
  `PuedeAsignar`; 004/RF-07..RF-11, RF-26). El general opera siempre; el de sucursal, sólo con su sucursal activa:
  ve a quien tiene su sucursal (sin administradores generales) y sólo gestiona cajeros y cocineros exclusivos de ella.
- `ResolutorActor` (scoped): arma el actor desde `IUsuarioActual` (403 si no puede operar), resuelve slugs de
  sucursal (403 slug ajeno para el de sucursal, 400 inexistente para el general) y proyecta `UsuarioDto` (sucursales
  en el orden de `ISucursalRepository.ListarAsync`). Los handlers de usuarios lo usan en este orden: actor → usuario
  (404) → `PuedeGestionar` (403) → slugs → `PuedeAsignar` (403) → reglas de conflicto (409).
- `ObtenerMiSesionHandler` (`Features/Auth/MiSesion`): nombre desde la base de datos y sucursales activas (todas
  para el general; las asignadas para el resto) en el orden del repositorio.
- `IniciarSesionHandler` lanza `SinSucursalesActivasException` (403 `Sin sucursales activas`) si un usuario que no es
  general no tiene ninguna sucursal activa (004/RF-17); el token lleva el claim `sello`.

## Categorías del menú y borrado lógico en el filtro global (feature 005)

Persistencia en `Restaurante.Infrastructure/Persistencia` (migración `Categorias`, aditiva):

- `categorias`: `id`, `restaurante_id` (FK a `restaurantes`, `Restrict`), `nombre` varchar(60),
  `nombre_normalizado` varchar(60), `nombre_corto` varchar(24) nulo, `nombre_corto_normalizado` nulo, `orden`
  int con check `ck_categorias_orden` (`orden >= 1`), `activa`, `creada_en`, `eliminada_en` nulo (borrado lógico).
- Índices únicos parciales (`eliminada_en IS NULL`): `ix_categorias_restaurante_nombre`
  (`restaurante_id`, `nombre_normalizado`) e `ix_categorias_restaurante_nombre_corto`
  (`restaurante_id`, `nombre_corto_normalizado`, además `nombre_corto IS NOT NULL`): eliminar una categoría
  libera su nombre y su nombre corto. Índice no único `ix_categorias_restaurante_orden_nombre`.
- `FiltrosGlobales.Aplicar` también reconoce `IEliminable` (`Restaurante.Domain.Comun`): a toda entidad que lo
  implemente le añade `e.EliminadaEn == null`, combinado con `AND` al filtro de restaurante/sucursal en la misma
  expresión (cada tipo tiene un único `HasQueryFilter`). `Sucursal`, `HorarioDia` y `CierreTemporal` no cambian.
- `CategoriaRepository` (puerto `ICategoriaRepository` en Application): obtener, listar paginado (`Skip/Take`
  ordenado por `Orden`, `Nombre`), existencia de nombre y de nombre corto normalizados (con `excluirId`), agregar
  y guardar. `GuardarCambiosAsync` traduce `UniqueViolation` por `ConstraintName` a `ConflictoCamposException`
  (`Application/Comun/Excepciones`, con `Errores` por campo: `nombreCorto` para el índice del nombre corto, cualquier
  otro `nombre`); los textos están en `MensajesCategoria`. Application no referencia Npgsql ni EF.

## Platillos del menú (feature 006)

Persistencia en `Restaurante.Infrastructure/Persistencia` (migración `Platillos`, aditiva):

- `platillos`: `id`, `restaurante_id` (FK a `restaurantes`, `Restrict`), `categoria_id` (FK a `categorias`,
  `Restrict`), `nombre` varchar(60), `nombre_normalizado`, `nombre_corto` varchar(24) nulo,
  `nombre_corto_normalizado` nulo, `descripcion` varchar(300) nula, `orden` int con check `ck_platillos_orden`
  (`orden >= 1`), `ingredientes` `text[]` `not null default '{}'` (mapeado sobre el campo de respaldo
  `_ingredientes`; conserva el orden y EF detecta el cambio al reemplazar la lista), `activo`, `creado_en`,
  `eliminada_en` nulo (borrado lógico, filtrado por `FiltrosGlobales` como en categorías).
- Índices únicos parciales (`eliminada_en IS NULL`): `ix_platillos_restaurante_nombre` e
  `ix_platillos_restaurante_nombre_corto` (además `nombre_corto IS NOT NULL`); índice no único
  `ix_platillos_categoria_orden_nombre` (`categoria_id`, `orden`, `nombre`).
- `PlatilloRepository` (puerto `IPlatilloRepository` en Application): obtener, listar por categoría paginado
  (`Orden`, `Nombre`, `Id`; desplazamiento en `long`), existencia de nombre y de nombre corto, contar por categoría
  (activos e inactivos no eliminados), agregar y guardar; `UniqueViolation` → `ConflictoCamposException`
  (`nombreCorto` o `nombre`); textos en `MensajesPlatillo`.

## Frontend (`apps/web`)

React 19 + TypeScript + Vite 7, pruebas con Vitest 3 (jsdom, `globals: false`, limpieza en
`src/pruebas/setup.ts`) y Playwright (proyectos `escritorio` 1440x900 y `movil` 390x844).

- `src/main.tsx`: monta `QueryClientProvider` (TanStack Query), `ProveedorSesion` (dentro del primero) y `App`.
  Las consultas no se reintentan ante un `ErrorApi` con estado < 500 (un 403 se muestra en seguida); el resto,
  hasta 3 veces. No hay `ProveedorTema` global: `tema.css` ya fija la paleta por defecto en `:root` y uno externo
  pisaría la del restaurante.
- `src/App.tsx`: enrutador (`react-router`; exporta `rutas`). Rutas: `/` (redirige a `/plataforma/login`),
  `/plataforma/login`, `/plataforma/restaurantes` (`RutaProtegida`, rol `administrador_plataforma`),
  `/r/:slug` con hijas `login`, `inicio`, `elegir-sucursal`, `sucursales`, `sucursales/:sucursal`, `usuarios` y `menu/categorias`
  (dentro de `DisenoRestaurante`) y una ruta `*` (también bajo `/r/:slug`) que muestra `PaginaNoEncontrada`.
- `src/api/`: `cliente.ts` (`pedirApi`, `ErrorApi`, `urlApi`; el token se pasa en cada llamada, nunca
  implícito; `TITULO_SESION_INVALIDA` y `alSesionInvalida`, que avisa a los suscritos cuando una petición con
  token recibe 401 "Sesión no válida") y `auth.ts` (login de plataforma y de restaurante).
- `src/sesion/`: `almacenSesion.ts` (persistencia en `sessionStorage`, clave `restaurante.sesion`),
  `ProveedorSesion.tsx` (`useSesion`, cierre automático en `expiraEn`, `elegirSucursal`; se suscribe a
  `alSesionInvalida` y expone `sesionTerminada` para el aviso del login; al cerrar la sesión vacía la caché de
  TanStack Query, leída con `QueryClientContext` para no fallar sin proveedor) y `miSesion.ts` (`obtenerMiSesion`,
  `claveMiSesion`: `GET /api/r/{slug}/mi-sesion`).
- `src/sucursales/`: gestión de sucursales (feature 003), `ElegirSucursalPagina` (`/r/{slug}/elegir-sucursal`, tras
  el login de quien tiene varias sucursales; 004/RF-20) y `SelectorSucursal`, el selector del encabezado para todo
  el personal: el administrador general lo llena con la lista de sucursales activas y va a su detalle; el resto,
  con `mi-sesion` (con una sola sucursal se muestra su nombre; 004/RF-21).
- `src/usuarios/`: `UsuariosPagina` (`/r/{slug}/usuarios`, `ROLES_GESTION_USUARIOS`; lista, activar/desactivar,
  "Nuevo usuario" y "Editar" deshabilitados hasta tener `mi-sesion`), `FormularioUsuario` (alta/edición; para el
  administrador de sucursal, sólo cajero/cocinero y su sucursal fija, tomada de las props al enviar),
  `DialogoContrasena` y `api.ts` (`NOMBRES_ROL`, `claveUsuarios`).
- `src/menu/`: gestión de categorías del menú (feature 005): `CategoriasPagina` (`/r/{slug}/menu/categorias`, sólo
  `administrador_general`; lista paginada, alta/edición, activar/desactivar y eliminar con `DialogoConfirmacion`),
  `FormularioCategoria` (alta/edición con errores por campo; un error sin campo conocido se muestra como aviso
  general), `VistaPreviaWhatsApp`, `api.ts` (`claveCategorias`, tamaño de página 20), `tituloWhatsApp.ts`
  (`LIMITE_TITULO_WHATSAPP`, espejo de `Domain.Menu.TituloWhatsApp`) y `mensajeError.ts` (403 → "No tienes permiso
  para esta acción."). `InicioRestaurante` enlaza a la página con el enlace "Menú".
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
