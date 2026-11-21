---
feature: 002-base-multisucursal-y-sucursales
estado: borrador
---
# Diseño: Base multi-restaurante

## Enfoque elegido y alternativas descartadas

- **Login por dirección del restaurante** (`/r/{slug}/…`), elegido por el usuario. Descartados:
  usuarios únicos en toda la plataforma (sin marca en el login) y subdominios (DNS comodín).
- **Logo en PostgreSQL**, elegido por el usuario. Descartados: volumen de Docker (otro respaldo) y
  almacenamiento de objetos (otro servicio y credenciales).
- **Frontend:** CSS Modules + variables CSS de tema, React Router y TanStack Query (llamada del
  agente). Descartado Tailwind: la paleta en tiempo de ejecución exige variables de todos modos.
- La gestión de usuarios de 001 se conserva y se acota al restaurante del token.

## Arquitectura

Backend (Clean Architecture de la constitución), cambios sobre 001:

- **Domain**: entidad `Restaurante` y value object `Paleta`; `Usuario` gana `RestauranteId`
  (nulo sólo para `administrador_plataforma`); `Rol` pasa a los cinco roles de la constitución.
- **Application**: casos de uso de restaurantes (crear, listar, editar, cambiar estado, subir logo,
  obtener identidad, obtener logo); `IContextoRestaurante` (restaurante del token o de la ruta);
  login por restaurante y login de plataforma.
- **Infrastructure**: tablas `restaurantes` y `restaurantes_logo`, columna `restaurante_id` en
  `usuarios`, filtro global de EF Core por restaurante, nueva migración que recrea el esquema de
  usuarios desde cero (RNF-04).
- **WebApi**: rutas `/api/plataforma/…` y `/api/r/{slug}/…`; middleware que resuelve el
  restaurante desde el token o desde el `slug`, y rechaza tokens de restaurantes inactivos.

Frontend en `apps/web/` (React + TS + Vite): React Router, TanStack Query, CSS Modules y un
`ProveedorTema` que vuelca la paleta en variables CSS.

## Componentes

- **`Restaurante` + `Paleta`** (Domain): nombre, `slug` inmutable, activo, paleta con cinco colores
  (`principal`, `acento`, `oscuro`, `fondo`, `texto`) validados como `#RRGGBB`; métodos
  `Activar()`, `Desactivar()`, `CambiarNombre()`, `CambiarPaleta()`. Cubre: RF-01, RF-04, RF-06
- **Crear restaurante** (Application): command `CrearRestaurante` con su validator (`slug`
  `^[a-z0-9-]{3,40}$`, nombre 1–80, colores hex) que crea el restaurante y su primer
  `administrador_general` en una sola transacción; nombre o `slug` duplicados → conflicto.
  Cubre: RF-01, RF-02
- **Listar, editar y cambiar estado** (Application): `ListarRestaurantes`, `EditarRestaurante`,
  `CambiarEstadoRestaurante`. Cubre: RF-03, RF-04, RF-06
- **Logo** (Application + Infrastructure): `SubirLogo` valida tipo por firma del archivo (PNG, JPG,
  WebP; rechaza SVG) y tamaño ≤ 1 MB; se guarda en `restaurantes_logo` con su `ETag` (hash).
  `ObtenerLogo` lo sirve con `ETag`/304. Cubre: RF-05, RF-15
- **Identidad pública** (Application + WebApi): `GET /api/r/{slug}/identidad`, anónimo, con nombre,
  paleta y URL del logo; restaurante inexistente o inactivo → 404 "Restaurante no disponible".
  Cubre: RF-10, RF-14
- **Login por restaurante y de plataforma** (Application + WebApi): `IniciarSesion` recibe el
  restaurante resuelto por el `slug`; el usuario se busca por (restaurante, nombre normalizado);
  el token lleva `restaurant_id`. `POST /api/plataforma/auth/login` sólo acepta
  `administrador_plataforma`. Credenciales o restaurante equivocado → 401 genérico.
  Cubre: RF-08, RF-09, RF-10, RF-11
- **Contexto y aislamiento** (WebApi + Infrastructure): middleware que fija `IContextoRestaurante`
  desde el claim `restaurant_id`; si el restaurante está inactivo → 401. Filtro global de EF Core
  por `RestauranteId` en `Usuario` (y en toda entidad futura del restaurante). Acceder a datos de
  otro restaurante → 404. Cubre: RF-06, RF-12
- **Usuarios acotados al restaurante** (Application): `CrearUsuario` y las reglas de 001 operan
  dentro del restaurante del contexto; unicidad por (restaurante, nombre normalizado); último
  administrador por restaurante. Cubre: RF-13
- **Autorización de plataforma** (WebApi): `RestaurantesController` con
  `[RequiereRol(Rol.AdministradorPlataforma)]`. Cubre: RF-07
- **Roles y migración** (Domain + Infrastructure): `Rol` con los cinco roles y su texto
  (`administrador_plataforma`, …); el inicializador crea el administrador inicial como
  `administrador_plataforma`. Cubre: RF-17
- **`ProveedorTema`** (frontend): carga la identidad y escribe `--color-principal`,
  `--color-acento`, `--color-oscuro`, `--color-fondo`, `--color-texto` en `:root`, con los valores
  por defecto de la constitución si falta alguno; tipografía Nunito. Cubre: RF-16
- **Pantallas de plataforma** (frontend): `/plataforma/login` y `/plataforma/restaurantes` (tabla con
  estado; formulario de alta y edición con vista previa de la paleta y carga del logo; activar y
  desactivar con diálogo de confirmación). Cubre: RF-18
- **Pantallas de restaurante** (frontend): `/r/{slug}/login` con marca, `/r/{slug}/inicio` con
  encabezado fijo (logo, nombre, "Hola, {usuario}", "Cerrar sesión"), y "Restaurante no
  disponible". Cubre: RF-19, RF-20
- **Rutas protegidas** (frontend): `RutaProtegida` por rol; sin sesión → login correspondiente.
  Sesión en memoria + `sessionStorage`. Cubre: RF-21

## Datos

| Tabla | Columnas |
|---|---|
| `restaurantes` | `id` uuid, `nombre` varchar(80) único, `slug` varchar(40) único, `activo` bool, `color_principal`, `color_acento`, `color_oscuro`, `color_fondo`, `color_texto` char(7), `creado_en` timestamptz |
| `restaurantes_logo` | `restaurante_id` uuid PK/FK, `contenido` bytea, `tipo` varchar(20), `etag` varchar(64), `actualizado_en` timestamptz |
| `usuarios` (cambia) | + `restaurante_id` uuid nulo (sólo nulo para `administrador_plataforma`); índice único (`restaurante_id`, `nombre_usuario_normalizado`) |

## Manejo de errores

- Restaurante inexistente o inactivo en rutas públicas → 404 "Restaurante no disponible".
- Token de restaurante inactivo → 401.
- Datos de otro restaurante → 404 (nunca 403, para no revelar que existen).
- Logo inválido → 400 por campo `logo`.
- Resto según la constitución (Art. 11) y los formatos de 001.

## Estrategia de pruebas

- Backend: xUnit `001`-style con `002/RF-NN` (unitarias, WebApi con dobles, `Integracion` con
  PostgreSQL para el filtro global, la unicidad por restaurante y el logo en `bytea`), incluidas
  las pruebas de aislamiento de la constitución (Art. 12.5).
- Frontend: Vitest + Testing Library para `ProveedorTema`, `RutaProtegida` y formularios.
- Playwright (suite simulada, `test("002/RF-NN: …")`) para cada pantalla: login de plataforma,
  alta y desactivación de restaurante, login con marca, inicio y "Restaurante no disponible". Estas
  tareas llevan `UI: sí` y se implementan con Ralph Loop.
- Suite completa (`e2e-stack`): alta de un restaurante y login en su dirección contra el sistema
  levantado.
