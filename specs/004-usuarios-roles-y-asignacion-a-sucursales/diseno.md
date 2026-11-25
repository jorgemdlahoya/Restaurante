---
feature: 004-usuarios-roles-y-asignacion-a-sucursales
estado: borrador
---
# Diseño: Usuarios, roles y asignación a sucursales (AUTH-01)

## Enfoque elegido y alternativas descartadas

- **Elegido (decisión del usuario): revisar al usuario en cada petición con un sello de sesión.**
  Cada usuario tiene un `SelloSesion` (Guid) que viaja en el token como claim `sello`. Un
  middleware, tras la autenticación, carga al usuario del token (una consulta por clave primaria
  con sus sucursales) y rechaza con 401 si no existe, está inactivo o su sello no coincide.
  Desactivar, cambiar contraseña, rol o sucursales renueva el sello. El corte es inmediato y
  reutiliza el patrón que ya aplica 002 al restaurante desactivado.
- **Descartado: tokens cortos con renovación.** El corte no es inmediato (dura lo que dure el
  token) y exige un flujo de renovación en el frontend.
- **Descartado: lista negra de tokens.** Obliga a registrar cada token emitido; en memoria se
  pierde al reiniciar y en base de datos cuesta lo mismo que el sello con más piezas.
- **Decisión del usuario: personal compartido.** El administrador de sucursal sólo gestiona a
  cajeros y cocineros asignados *únicamente* a su sucursal; al personal compartido sólo lo
  gestiona el administrador general.
- **Decisión del usuario: contraseña nueva.** Quien gestiona a un usuario puede asignarle una
  contraseña nueva; hacerlo corta su sesión.
- **Llamada del agente: se mantiene el hash y el JWT propios de 001/002** en lugar de ASP.NET Core
  Identity (que menciona el backlog): ya cumplen el Art. 9.2 y migrar no aporta nada a esta
  feature.
- **Llamada del agente: la sucursal activa no viaja en el token.** La sucursal ya va en la ruta
  (`/s/{sucursal}`, feature 003); el frontend recuerda la elegida en la sesión local. Así cambiar
  de sucursal no requiere token nuevo.
- **Llamada del agente: sucursal ajena → 404**, no 403, para no revelar qué sucursales existen
  (Art. 12.5). Las operaciones sobre usuarios fuera del alcance del administrador de sucursal,
  dentro de su restaurante, dan 403 (prueba clave del backlog).
- **Llamada del agente: el sello sólo se renueva si algo cambia.** `AsignarRolYSucursales` compara
  rol y conjunto de sucursales antes de renovarlo; contraseña nueva y desactivación siempre lo
  renuevan.
- **Llamada del agente: autoservicio.** El administrador general puede asignarse contraseña a sí
  mismo (corta su propia sesión; el frontend lo lleva al login con el aviso). El administrador de
  sucursal no se gestiona a sí mismo: su fila sale con `gestionable = false`.
- **Llamada del agente: orden 400 → 403.** El validador del pipeline (formato y cardinalidad)
  responde antes; el alcance se comprueba en el handler. Para el administrador de sucursal toda
  sucursal pedida que no sea la suya da 403, exista o no.
- **Llamada del agente: sucursal desactivada.** No corta sesiones ni toca el handler de 003: el
  middleware de sucursal rechaza con 404 la sucursal inactiva para quien no es administrador
  general, `mi-sesion` deja de ofrecerla, y el administrador de sucursal con su sucursal inactiva
  pierde la gestión de usuarios (403). El inicio muestra un estado vacío si no queda ninguna.
- **Llamada del agente: `PUT /usuarios/{id}` sustituye a `PUT /usuarios/{id}/rol` de 002**, porque
  rol y sucursales se validan juntos (RF-02). Ningún cliente usa el endpoint viejo (no hay
  pantallas de usuarios).

## Arquitectura

Sobre la Clean Architecture de 001–003:

- **Domain**: `Usuario` gana `Sucursales` (ids asignados), `SelloSesion`, `AsignarSucursales`,
  `CambiarContrasena` y la regla de cardinalidad rol ↔ sucursales.
- **Application**: casos de uso de usuarios ampliados con un **alcance de gestión**
  (`AlcanceGestionUsuarios`) que decide, según el usuario actual, qué usuarios ve y cuáles puede
  modificar; `ObtenerMiSesion`; el login comprueba sucursales activas y emite el sello.
- **Infrastructure**: tabla `usuarios_sucursales`, columna `sello_sesion`, migración
  `UsuariosSucursales`; el emisor de tokens añade el claim `sello`.
- **WebApi**: `MiddlewareSesionUsuario` (sello y estado, tras la autenticación y antes del
  contexto de restaurante), `MiddlewareContextoSucursal` comprueba la asignación,
  `UsuariosController` ampliado y `MiSesionController`.
- **Frontend**: `apps/web/src/usuarios/` (lista y formulario), pantalla de elección de sucursal,
  selector del encabezado basado en `mi-sesion` y manejo global del 401 "Sesión no válida".

Orden del pipeline: `UseAuthentication` → `MiddlewareSesionUsuario` →
`MiddlewareContextoRestaurante` → `MiddlewareContextoSucursal` → controladores.

## Componentes

- **`Usuario`** (Domain): `IReadOnlyCollection<Guid> Sucursales`; `SelloSesion` (nuevo Guid al
  crear); `Crear(..., sucursales)`; `AsignarRolYSucursales(rol, sucursales)`,
  `CambiarContrasena(hash)` y `Desactivar()` renuevan el sello (`Reactivar` no lo necesita: el
  usuario inactivo no tiene tokens válidos). `ValidarSucursales(rol, cantidad)` aplica la
  cardinalidad: administrador general 0, administrador de sucursal 1, cajero y cocinero ≥ 1,
  administrador de plataforma 0. Cubre: RF-01, RF-02, RF-04, RF-06, RF-13
- **Validación de usuario** (Application): validator común de alta y edición (formato de 002 para
  nombre y contraseña, rol válido, cardinalidad de RF-02 en el campo `sucursales`, sin
  duplicados); el handler resuelve los `slug` de sucursal dentro del restaurante (filtro global) y
  responde 400 en `sucursales` si alguno no existe. Cubre: RF-02, RF-06
- **`AlcanceGestionUsuarios`** (Application, puro): dado el usuario actual (rol y sucursal) y un
  usuario objetivo, responde `PuedeVer` y `PuedeGestionar`; y valida que el rol y las sucursales
  pedidos estén dentro del alcance. Administrador general: todo su restaurante.
  Administrador de sucursal: ve a quien tenga su sucursal asignada salvo administradores
  generales; gestiona sólo cajeros/cocineros cuya única sucursal es la suya (nunca a sí mismo);
  sólo puede pedir rol cajero/cocinero con su sucursal; si su sucursal está inactiva, nada (403).
  Cualquier otro rol: nada (403). Cubre: RF-07, RF-08, RF-09, RF-10, RF-11, RF-26
- **Crear, listar, editar, contraseña y estado** (Application + WebApi): `CrearUsuario` (con
  sucursales), `ListarUsuarios` (filtrado por alcance, ordenado por nombre, con `gestionable`),
  `EditarUsuario` (`PUT /{id}`: rol y sucursales; reemplaza a `CambiarRolUsuario`),
  `CambiarContrasenaUsuario` (`PUT /{id}/contrasena`, 204) y `CambiarEstadoUsuario` existente,
  todos consultando el alcance. `[RequiereRol(AdministradorGeneral, AdministradorSucursal)]` en el
  controlador. Un usuario fuera de su restaurante no existe para el filtro global → 404.
  `UsuarioDto` añade `sucursales: [{slug, nombre}]` y `gestionable`. Cubre: RF-01, RF-03, RF-04,
  RF-06, RF-07, RF-08, RF-09, RF-10, RF-11
- **Reglas de último administrador y autoprotección** (Application): las de 002 se trasladan a
  `EditarUsuario` y `CambiarEstadoUsuario`: 409 si quedaría sin administrador general activo, si
  se desactiva a sí mismo o si se quita el rol de administrador general. Cubre: RF-05
- **Login** (Application `IniciarSesionHandler`): usuario inactivo → 401 genérico (ya existe);
  rol distinto de administrador general sin ninguna sucursal asignada activa → 403 "Sin sucursales
  activas"; emite el token con `sello`. Cubre: RF-12, RF-17
- **Emisor de tokens** (Infrastructure): añade el claim `sello` = `SelloSesion` del usuario.
  Cubre: RF-13, RF-14
- **`MiddlewareSesionUsuario`** (WebApi): para toda petición autenticada carga al usuario por
  `sub` (sin filtro de restaurante, por clave primaria) y responde 401 "Sesión no válida" con
  `WWW-Authenticate: Bearer` si falta el claim `sello`, el usuario no existe, está inactivo o el
  sello no coincide. Deja al usuario cargado en `IUsuarioActual` (id, rol y sucursales vigentes
  en base de datos) para el alcance y para el middleware de sucursal: las decisiones de
  autorización usan los datos actuales, no los del token. Cubre: RF-13, RF-14
- **`MiddlewareContextoSucursal`** (WebApi, ampliado): tras resolver la sucursal, si hay usuario
  autenticado que no es administrador general y la sucursal no está entre las suyas o está
  inactiva → 404 "Sucursal no disponible". Sin autenticar no cambia (estado público).
  Cubre: RF-15, RF-25
- **`ObtenerMiSesion`** (Application + WebApi `GET /api/r/{slug}/mi-sesion`, cualquier rol de
  restaurante): nombre, rol y sucursales activas permitidas ordenadas por nombre. Cubre: RF-16
- **Pantalla de usuarios** (Frontend `usuarios/UsuariosPagina`, `FormularioUsuario`,
  `DialogoContrasena`): lista con acciones sólo en los `gestionable`; formulario con rol y
  casillas de sucursales (para el administrador de sucursal, roles cajero/cocinero y su sucursal
  fija); confirmación al desactivar con `DialogoConfirmacion`. Ruta protegida para ambos
  administradores con `redirigirSinRol` al inicio; enlace "Usuarios" en el inicio. Cubre: RF-18,
  RF-19, RF-22
- **Elección de sucursal** (Frontend `sucursales/ElegirSucursalPagina`, login y
  `SelectorSucursal`): tras el login se consulta `mi-sesion`; con varias sucursales (y rol no
  administrador general) se va a `/r/{slug}/elegir-sucursal`, con una se fija y se va al inicio.
  El selector del encabezado usa `mi-sesion` para todos los roles y se muestra con dos o más
  sucursales; con una, sólo su nombre. El login muestra el mensaje de 403 "Sin sucursales
  activas"; con sesión abierta y lista vacía, el inicio muestra el mismo aviso sin selector.
  Cubre: RF-20, RF-21, RF-24, RF-27
- **Sesión expirada** (Frontend `api/cliente`, `ProveedorSesion`): un 401 con título "Sesión no
  válida" en cualquier petición autenticada cierra la sesión local y navega al login del
  restaurante con el aviso. Cubre: RF-23

## Datos

- `usuarios`: nueva columna `sello_sesion uuid not null` (la migración da a cada fila un valor
  nuevo con `gen_random_uuid()`, por eso los tokens previos dejan de valer; RNF-04).
- `usuarios_sucursales (usuario_id uuid FK usuarios on delete cascade, sucursal_id uuid FK
  sucursales on delete restrict, restaurante_id uuid not null)`, clave primaria
  `(usuario_id, sucursal_id)`, índice por `sucursal_id`. Implementa `IEntidadDeRestaurante`
  para el filtro global. No se borra físicamente ninguna fila al desactivar (Art. 13): sólo cambia
  al reasignar sucursales, que es reemplazo de la asignación, no borrado de una entidad de negocio.
- Migración `UsuariosSucursales`, aditiva. Los usuarios existentes quedan sin sucursales.
- Consulta de sesión: `usuarios` por PK con `Include` de `usuarios_sucursales`, ignorando filtros
  globales (el middleware de sesión corre antes de fijar el restaurante).

## Manejo de errores

- 400 `ValidationProblemDetails`: formato de nombre, contraseña, rol y `sucursales` (cardinalidad,
  inexistente, duplicada).
- 401 "Sesión no válida" (`ProblemDetails`, `WWW-Authenticate: Bearer`): sello ausente o distinto,
  usuario inexistente o inactivo. 401 genérico de 002 para credenciales incorrectas o usuario
  inactivo en el login.
- 403 "Acceso denegado": rol sin permiso o operación fuera del alcance del administrador de
  sucursal. 403 "Sin sucursales activas" en el login.
- 404: usuario de otro restaurante ("Usuario no encontrado"), sucursal no asignada o inexistente
  ("Sucursal no disponible").
- 409: último administrador general, desactivarse a sí mismo, quitarse el rol, nombre de usuario
  repetido (002).
- Frontend: errores por campo en el formulario; 401 "Sesión no válida" → login con aviso; 403 en
  acciones → mensaje "No tienes permiso para esta acción.".

## Estrategia de pruebas

- **Domain (xUnit)**: cardinalidad rol ↔ sucursales y renovación del sello en cada cambio
  (`004/RF-02`, `004/RF-13`).
- **Application (xUnit)**: `AlcanceGestionUsuarios` en tabla de casos (general, sucursal propia
  exclusiva, compartido, otro rol, otra sucursal) (`004/RF-07`–`RF-11`); handlers con dobles en
  memoria, incluido último administrador (`004/RF-05`) y login sin sucursales activas
  (`004/RF-17`).
- **WebApi (xUnit + `FabricaApi`)**: endpoints de usuarios por rol (200/201/204/400/403/404/409),
  `mi-sesion`, sello: token viejo → 401 tras desactivar, cambiar contraseña, rol y sucursales, y
  token nuevo → 200 (`004/RF-13`, `RF-14`); acceso a `/s/{sucursal}` no asignada → 404
  (`004/RF-15`) y asignada pero inactiva → 404 (`004/RF-25`); administrador de sucursal con su
  sucursal inactiva → 403 (`004/RF-26`); aislamiento A/B de restaurante y sucursal (RNF-02).
- **Integración (Testcontainers, PostgreSQL)**: migración con usuarios existentes (sello nuevo,
  sin sucursales), FK y clave primaria de `usuarios_sucursales`, y tiempo de la consulta de sesión
  (RNF-01).
- **Frontend (Vitest + Testing Library)**: formulario de usuario por rol, lista con y sin
  acciones, manejo del 401 "Sesión no válida".
- **Playwright simulada (`apps/web/e2e/`, 1440 y 390)**: gestión de usuarios por el
  administrador general y por el de sucursal, elección de sucursal al iniciar sesión, selector,
  aviso de sesión terminada y mensaje sin sucursales (`004/RF-18`–`RF-24`, `004/RF-27`).
- **Playwright e2e-stack**: el administrador general crea un cajero con dos sucursales, el cajero
  inicia sesión y elige sucursal; tras desactivarlo, su sesión vuelve al login.
- Todos los nombres llevan el prefijo `004/RF-NN:`.
