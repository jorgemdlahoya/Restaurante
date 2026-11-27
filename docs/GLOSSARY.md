# Glosario

## Rol

Perfil de permisos de un usuario del sistema (los cinco roles de la constitución). Valores:
`AdministradorPlataforma`, `AdministradorGeneral`, `AdministradorSucursal`, `Cajero` y `Cocinero`.
En la API, el JWT y la base de datos se serializa en snake_case (`administrador_plataforma`,
`administrador_general`, `administrador_sucursal`, `cajero`, `cocinero`); la coincidencia es exacta.
`POST /api/r/{slug}/usuarios` y `PUT /api/r/{slug}/usuarios/{id}` aceptan todos salvo `administrador_plataforma`
(reglas compartidas en `ReglasRolYSucursales`).

## LoginPeticion

Cuerpo de los dos logins, `POST /api/r/{slug}/auth/login` (usuarios de ese restaurante) y
`POST /api/plataforma/auth/login` (sólo administradores de plataforma): `{ nombreUsuario, contrasena }`.
Ambos son anónimos. Los campos vacíos o ausentes dan 400 (validador); credenciales incorrectas, usuario
desactivado o usuario de otro restaurante (o de restaurante en el login de plataforma) dan 401 con el mismo
mensaje. En el login por restaurante, un `slug` inexistente o de un restaurante inactivo da 404
`Restaurante no disponible`; el `slug` no distingue mayúsculas.

## LoginRespuestaDto

Respuesta 200 de los dos logins: `{ token, expiraEn, nombreUsuario, rol }`. `token` es el
JWT (HS256), `expiraEn` su instante de expiración y `rol` va en minúsculas.

## restaurant_id (claim)

Claim del JWT (`EmisorTokensJwt.ClaimRestaurante`) con el id del restaurante del usuario. Sólo se emite si
el usuario tiene restaurante; los tokens de `administrador_plataforma` no lo llevan. Fuera de
`/api/plataforma/…` fija el contexto de restaurante de la petición: si el restaurante no existe o está
inactivo, 401 `No autenticado` / `El restaurante no está activo.` (con `WWW-Authenticate: Bearer`; el token
vuelve a valer al reactivarlo); en `/api/r/{slug}/…` con el `slug` de otro restaurante, 404
`Restaurante no disponible`.

## Sello de sesión

`SelloSesion` (Guid) de cada usuario, que viaja en el token como claim `sello` (`EmisorTokensJwt.ClaimSello`,
formato `D`). Se renueva al desactivar al usuario, al cambiar su contraseña y al cambiar su rol o su conjunto de
sucursales (una edición sin cambios y la reactivación no lo renuevan). `MiddlewareSesionUsuario` lo compara en
toda petición autenticada, incluidas las de `/api/plataforma/…`: si el token no lo trae, el usuario no existe,
está inactivo o el sello no coincide, responde 401 `Sesión no válida` / `Vuelve a iniciar sesión.` con
`WWW-Authenticate: Bearer`. Así se cortan al instante los tokens emitidos antes del cambio (004/RF-13, RF-14).

## JWT (emisor y audiencia)

Los tokens llevan `iss = restaurante-api` y `aud = restaurante-web`, y los claims `sub` (id del
usuario), `unique_name` (nombre de usuario), `role` (rol en minúsculas), `sello` (sello de sesión) y, si el
usuario tiene restaurante, `restaurant_id`. Se validan firma
(HS256 con `JWT_SIGNING_KEY`), emisor, audiencia y expiración, sin tolerancia de reloj
(`ClockSkew = 0`).

## UsuarioDto

Representación pública de un usuario en la API: `{ id, nombreUsuario, rol, activo, sucursales, gestionable }`.
`sucursales` son sus `SucursalAsignadaDto` ordenadas por nombre (activas o no); `gestionable` indica si quien
consulta puede modificarlo (`AlcanceGestionUsuarios.PuedeGestionar`). Nunca incluye
la contraseña ni su hash. Es la respuesta de `GET /api/r/{slug}/usuarios` (lista, sólo los del
restaurante; el administrador de sucursal sólo ve a quien tiene su sucursal, sin administradores generales),
`POST /api/r/{slug}/usuarios` (201, `Location: /api/r/{slug}/usuarios/{id}`),
`PUT /api/r/{slug}/usuarios/{id}/estado` y `PUT /api/r/{slug}/usuarios/{id}`. Un usuario de otro
restaurante da 404. Los endpoints de usuarios admiten `administrador_general` y `administrador_sucursal`;
cajero y cocinero reciben 403 (004/RF-11).

## SucursalAsignadaDto

Sucursal asignada a un usuario: `{ slug, nombre, activa }` (004/RF-01, RF-03).

## ResolutorActor

Servicio scoped de Application que construye el `ActorUsuarios` de la petición desde `IUsuarioActual` (datos de
base de datos) y la sucursal propia del administrador de sucursal (con su `Activa`, de `ISucursalRepository`);
lanza `ProhibidoException` (403 `Acceso denegado`) si el actor no puede operar. También resuelve slugs de sucursal
dentro del restaurante (400 en `sucursales` si alguno no existe; para el administrador de sucursal, 403 ante
cualquier slug que no sea el suyo, exista o no) y proyecta `UsuarioDto`.

## CrearUsuarioPeticion

Cuerpo de `POST /api/r/{slug}/usuarios`: `{ nombreUsuario, contrasena, rol, sucursales }`. El rol va en
minúsculas; `sucursales` son slugs (ausente = ninguna) con la cardinalidad de 004/RF-02: `administrador_general`
ninguna, `administrador_sucursal` exactamente una, `cajero` y `cocinero` al menos una, sin repetir (error 400 en
`sucursales`). El nombre es único sólo dentro del restaurante. Orden de errores: 400 (validador) → 403 si el
administrador de sucursal tiene su sucursal desactivada (004/RF-26) → 403 slug ajeno (administrador de sucursal,
exista o no) / 400 slug inexistente (administrador general) → 403 si no puede asignar ese rol y sucursales → 409
nombre.

## CambiarEstadoPeticion

Cuerpo de `PUT /api/r/{slug}/usuarios/{id}/estado`: `{ activo }` (booleano). Orden de errores: 400 (validador) →
403 si el administrador de sucursal tiene su sucursal desactivada (004/RF-26) → 404 (otro restaurante) → 403 si no
puede gestionarlo → 409 (desactivarse a sí mismo o al último administrador general activo). Desactivar renueva el sello de
sesión; reactivar no.

## CambiarContrasenaPeticion

Cuerpo de `PUT /api/r/{slug}/usuarios/{id}/contrasena` (004/RF-06): `{ contrasena }` (mínimo 8 caracteres, la misma
constante que el alta). Lo atiende `CambiarContrasenaUsuarioCommand(Id, Contrasena)` → 204. Orden de errores: 400 (validador) →
403 si el administrador de sucursal tiene su sucursal desactivada (004/RF-26) → 404 (otro restaurante) → 403 si no
puede gestionarlo. El administrador general puede cambiar la suya (su token anterior da 401 `Sesión no
válida`); el de sucursal sólo la de cajeros y cocineros exclusivos de su sucursal. Guarda sólo el hash y renueva el
sello de sesión (004/RF-13).

## EditarUsuarioPeticion

Cuerpo de `PUT /api/r/{slug}/usuarios/{id}` (004/RF-04; sustituye a `PUT /{id}/rol` de 001): `{ rol, sucursales }`,
con las mismas reglas que `CrearUsuarioPeticion` (rol en minúsculas, sucursales por slug con la cardinalidad de
004/RF-02). El nombre de usuario no se edita (un `nombreUsuario` en el cuerpo se ignora). Lo atiende
`EditarUsuarioCommand(Id, Rol, Sucursales)` → `UsuarioDto`. Orden de errores: 400 (validador) → 404 (usuario de
otro restaurante) → 403 si no puede gestionarlo (el administrador de sucursal sólo edita cajeros y cocineros
exclusivos de su sucursal) → 403 slug ajeno (administrador de sucursal, exista o no) / 400 slug inexistente
(administrador general) → 403 si no puede asignar ese rol y sucursales (el de sucursal sólo asigna cajero o cocinero
con la suya) → 409 (quitarse a sí mismo el rol de administrador general o dejar el restaurante sin administradores
generales activos). Un administrador de sucursal cuya sucursal está desactivada recibe 403 antes que todo lo demás
salvo el 400 del validador (004/RF-26). Sólo si cambian el rol o el conjunto de sucursales se
renueva el sello de sesión del usuario (004/RF-13).

## Restaurante

Entidad de dominio (`Restaurante.Domain.Restaurantes.Restaurante`) que representa un restaurante del
sistema multi-restaurante: `Id`, `Nombre`, `Slug`, `Activo`, `Paleta` y `CreadoEn`. El nombre se guarda
recortado (`Trim()`) y tiene entre 1 y 80 caracteres. Nace activo; `Desactivar()` y `Activar()` sólo
cambian el estado. `CambiarNombre` y `CambiarPaleta` no tocan el `Slug`.

## Slug

Identificador de un restaurante en direcciones (`/r/{slug}`, `/api/r/{slug}/...`). Cumple
`^[a-z0-9-]{3,40}$` y es inmutable: la entidad no ofrece ningún método para cambiarlo.

## Paleta

Registro de dominio con los cinco colores de tema de un restaurante: `Principal`, `Acento`, `Oscuro`,
`Fondo` y `Texto`. Cada color es `#RRGGBB` (hexadecimal, sin distinguir mayúsculas) o ausente (`null`,
se usa el valor por defecto). Un color con otro formato lanza `ArgumentException`. `Paleta.Vacia` no
define ningún color y `Paleta.EsColorHex(color)` valida el formato.

## Logo

Entidad `LogoRestaurante`: imagen del restaurante (una por restaurante, `RestauranteId`) con `Contenido`
(bytes), `Tipo` (tipo MIME), `ETag` y `ActualizadoEn`. `Reemplazar` sustituye contenido, tipo y ETag.

## Contexto de restaurante

Puerto `IContextoRestaurante` (`Restaurante.Application.Abstracciones`) con `Guid? RestauranteId`: el
restaurante en cuyo ámbito se atiende la petición. `null` es el ámbito de plataforma. El
`RestauranteDbContext` lo usa en un filtro global sobre `usuarios` (`RestauranteId == RestauranteActualId`):
con un restaurante sólo se ven sus usuarios; con `null`, sólo los administradores de plataforma.
`ContextoRestauranteFijo` es la implementación de valor fijo (fábrica de diseño y pruebas). En la API lo
resuelve `MiddlewareContextoRestaurante` por petición: el claim `restaurant_id` del token, si lo hay; si no,
el `slug` de `/api/r/{slug}/…`; nunca en `/api/plataforma/…`.

## Usuario por restaurante

Todo `Usuario` tiene `RestauranteId` salvo el administrador de plataforma, que no tiene ninguno:
`Usuario.Crear(restauranteId, ...)` lanza `ArgumentException` si no se cumple. El nombre de usuario es
único dentro de cada restaurante (índice `ix_usuarios_restaurante_nombre`, `NULLS NOT DISTINCT`), y la
regla del último administrador activo se cuenta por restaurante.

## PaletaDto

Paleta de un restaurante en la API: `{ principal, acento, oscuro, fondo, texto }`. Cada color es
`#RRGGBB` o ausente (`null`); el validador `ValidadorPaleta` responde `El color debe tener el formato #RRGGBB`.

## IdentidadDto

Identidad pública de un restaurante: `{ nombre, slug, paleta, logoUrl }`. Es la respuesta anónima de
`GET /api/r/{slug}/identidad`; `logoUrl` es `/api/r/{slug}/logo?v={etag}` o `null` si no hay logo.

## LogoDto

Logo de un restaurante: `{ contenido, tipo, etag }` (bytes, `image/png|jpeg|webp` y SHA-256 en hexadecimal).
Lo devuelve `ObtenerLogoQuery` y `GET /api/r/{slug}/logo` lo sirve con `ETag`, `Cache-Control: public, no-cache`
y 304 si `If-None-Match` coincide. Se sube con `PUT /api/plataforma/restaurantes/{id}/logo` (multipart, campo
`logo`, ≤ 1 MB, tipo detectado por firma con `DetectorTipoImagen`; SVG rechazado).

## RestauranteDto

Representación pública de un restaurante en la API de plataforma: `{ id, nombre, slug, activo, paleta }`.
Es la respuesta de `POST /api/plataforma/restaurantes` (201).

## CrearRestaurantePeticion

Cuerpo de `POST /api/plataforma/restaurantes` (sólo `administrador_plataforma`):
`{ nombre, slug, paleta?, administrador: { nombreUsuario, contrasena } }`. Crea el restaurante activo y
su primer `administrador_general` en una sola transacción. Nombre o slug repetidos dan 409 (el nombre se
compara recortado y distinguiendo mayúsculas, igual que el índice único de la base); los campos inválidos
dan 400 como `slug`, `nombre`, `paleta.principal` o `administrador.contrasena`.

## EditarRestaurantePeticion

Cuerpo de `PUT /api/plataforma/restaurantes/{id}` (sólo `administrador_plataforma`): `{ nombre, paleta? }`.
El nombre se recorta y no puede repetirse (409, exacto); el `slug` no se edita. Una `paleta` ausente o `null`
borra la paleta del restaurante (vuelve a los colores por defecto). Responde un `RestauranteDto`.

## Sesion (frontend)

Datos de la sesión iniciada: `{ token, expiraEn, nombreUsuario, rol, slug }` (`slug` sólo en usuarios de
restaurante). `ProveedorSesion` la guarda en memoria y en `sessionStorage` (clave `restaurante.sesion`) y la
cierra sola al llegar `expiraEn`. El token nunca se envía de forma implícita: cada llamada lo pasa a `pedirApi`.
También guarda `sucursalActiva` (slug elegido con `elegirSucursal`) y, desde 004, expone `sesionTerminada`. Al cerrar la
sesión (`cerrar` o "Sesión no válida") vacía la caché de TanStack Query, para que quien entre después en la misma
pestaña no vea las listas del usuario anterior.

## TITULO_SESION_INVALIDA / alSesionInvalida (frontend)

`TITULO_SESION_INVALIDA` (`'Sesión no válida'`, en `api/cliente.ts`) es el título del 401 con que la API rechaza un
token cuyo usuario cambió de acceso (004/RF-13, RF-14). Cuando una petición **con token** lo recibe, `pedirApi` avisa
a los manejadores suscritos con `alSesionInvalida(manejador)` (devuelve la función para cancelar la suscripción).
`ProveedorSesion` se suscribe: borra la sesión, vacía la caché de consultas y marca `sesionTerminada`.

## sesionTerminada / MENSAJE_SESION_TERMINADA (frontend)

`sesionTerminada` (en `useSesion()`) indica que la API rechazó la sesión con "Sesión no válida"; dura hasta el
siguiente `iniciar`. Mientras vale `true`, `LoginRestaurante` muestra `MENSAJE_SESION_TERMINADA`:
"Tu sesión terminó. Vuelve a iniciar sesión." (004/RF-23).

## MENSAJE_SIN_SUCURSALES (frontend)

"No tienes sucursales activas. Habla con tu administrador." (`restaurante/LoginRestaurante.tsx`): lo muestra el login
ante el 403 `Sin sucursales activas` (004/RF-24) y el inicio cuando `mi-sesion` no trae sucursales (004/RF-27).

## MENSAJE_SIN_PERMISO (frontend)

"No tienes permiso para esta acción." (`usuarios/FormularioUsuario.tsx`): texto de la gestión de usuarios ante un 403
(lista, formulario, asignación de contraseña y cambio de estado). Un 409 al cambiar el estado muestra en cambio el detalle de la API (p. ej.
"No puedes desactivar tu propio usuario").

## ROLES_GESTION_USUARIOS / NOMBRES_ROL (frontend)

`ROLES_GESTION_USUARIOS` (`usuarios/UsuariosPagina.tsx`): roles que pueden abrir `/r/{slug}/usuarios`
(`administrador_general` y `administrador_sucursal`); el resto vuelve a `/r/{slug}/inicio` (004/RF-22).
`NOMBRES_ROL` (`usuarios/api.ts`): nombre visible de cada rol de restaurante ("Administrador general",
"Administrador de sucursal", "Cajero", "Cocinero").

## LIMITE_TITULO_WHATSAPP (frontend)

`LIMITE_TITULO_WHATSAPP` (`menu/tituloWhatsApp.ts`) = 24: largo máximo del título de una categoría en WhatsApp; espejo de
`TituloWhatsApp.Limite` del dominio. `tituloWhatsApp` y `seRecorta` alimentan la tabla y `VistaPreviaWhatsApp` (005/RF-11).
`menu/mensajeError.ts` muestra "No tienes permiso para esta acción." ante todo 403 de la gestión de categorías.

## MensajesCategoria / ICategoriaRepository / ListarCategoriasValidator.TamanoMaximo

`MensajesCategoria` (Application): textos de validación y conflicto de categorías. `ICategoriaRepository` (puerto en
Application; implementado por `CategoriaRepository`): obtener, listar paginado, unicidad y persistencia.
`ListarCategoriasValidator.TamanoMaximo` = 100: tope de `tamano` en `GET /api/r/{slug}/categorias` (005/RF-04).

## claveMiSesion / mi-sesion (frontend)

`sesion/miSesion.ts`: `obtenerMiSesion(token, slug)` pide `GET /api/r/{slug}/mi-sesion` (`MiSesion`: nombre, rol y
`SucursalDeSesion[]`) y `claveMiSesion(slug)` es su clave de TanStack Query (`['mi-sesion', slug]`). Lo usan el
login y la elección de sucursal, el inicio (aviso sin sucursales), el selector del encabezado del personal y la
gestión de usuarios (las sucursales fijas del
administrador de sucursal: "Nuevo usuario" y "Editar" esperan a que lleguen).

## ErrorApi (frontend)

Excepción de `pedirApi` ante una respuesta no exitosa: lleva el `estado` HTTP, el título y el detalle del
`problem+json` y, en validaciones, `errores` por campo (claves en camelCase, p. ej. `administrador.nombreUsuario`).

## RutaProtegida (frontend)

Componente de ruta que exige una sesión vigente con uno de los `roles` indicados (y el `slug` de la URL en los
usuarios de restaurante); si no, redirige al login que corresponde (`/plataforma/login` o `/r/{slug}/login`).

## ProveedorTema y paleta (frontend)

`ProveedorTema` escribe la `Paleta` del restaurante en las variables CSS `--color-principal`, `--color-acento`,
`--color-oscuro`, `--color-fondo` y `--color-texto` de `:root`, rellenando con `PALETA_POR_DEFECTO` los colores
ausentes, y las restaura al desmontarse. Ningún componente usa colores literales: sólo `var(--color-*)` (RNF-02).

## Identidad (frontend)

Datos públicos del restaurante (`nombre`, `slug`, `paleta`, `logoUrl`) que `DisenoRestaurante` carga de
`GET /api/r/{slug}/identidad` para pintar el encabezado de marca y aplicar el tema; un 404 muestra
`Restaurante no disponible`.

## VistaPreviaPaleta (frontend)

Muestra en el formulario de restaurantes cómo se verá la paleta elegida, fijando las variables `--color-*` sólo
en su propio elemento (no toca el tema de la pantalla).

## FormularioRestaurante (frontend)

Formulario de plataforma para dar de alta (nombre, slug, administrador inicial, paleta y logo) o editar
(nombre, paleta y logo) un restaurante; enlaza los errores 400 del servidor a cada campo.

## Sucursal (feature 003)

Local físico de un restaurante (`Restaurante.Domain.Sucursales.Sucursal`, implementa `IEntidadDeRestaurante`).
Tiene nombre (1-80, recortado), dirección (1-200), teléfono y WhatsApp en E.164 (`+52XXXXXXXXXX`; al guardar
se quitan espacios y guiones con `TelefonoMexico.Normalizar`), `slug` inmutable (`^[a-z0-9-]{3,40}\z`),
`phone_number_id`, prefijo, zona horaria y estado activa/inactiva; nace activa. `Editar` cambia todo menos el
`slug`; `Activar`/`Desactivar` sólo cambian el estado. Los formatos inválidos lanzan `ArgumentException`.

## Prefijo (sucursal)

Una a tres letras mayúsculas (`^[A-Z]{1,3}\z`) que identifican a la sucursal (p. ej. `A`, `BC`).

## Zona horaria (sucursal)

Zona IANA en que se interpretan los horarios y cierres de la sucursal. Sólo se admiten las 12 de
`ZonasHorarias.Admitidas` (México); por defecto `America/Mexico_City`.

## phone_number_id

Identificador numérico (`^[0-9]{1,30}\z`, sólo dígitos ASCII) del número de WhatsApp Business de la sucursal; es opcional (puede ser
ausente).

## DatosSucursalDto

Datos editables de una sucursal tal como llegan por la API:
`{ nombre, direccion, telefono, whatsApp, phoneNumberId?, prefijo, zonaHoraria? }`. Una `zonaHoraria` ausente
usa `America/Mexico_City`; un `phoneNumberId` vacío se trata como ausente. `ValidadorDatosSucursal` aplica los
formatos (nombre 1-80, dirección 1-200, teléfonos `+52` y 10 dígitos ASCII, prefijo, zona admitida) y lo
reutilizan el alta y la edición.

## SucursalDto

Representación pública de una sucursal: `{ id, nombre, direccion, telefono, slug, whatsApp, phoneNumberId,
prefijo, zonaHoraria, activa }`. Es la respuesta de `POST /api/r/{slug}/sucursales` (201,
`Location: /api/r/{slug}/sucursales/{id}`; sólo `administrador_general`). Nombre, slug y prefijo son únicos
dentro del restaurante; WhatsApp y `phone_number_id`, en toda la plataforma (409 si se repiten).

## HorarioDiaDto

Horario de un día de la semana: `{ dia, cerrado, apertura?, cierre? }`, con `dia` en español (`lunes` … `domingo`)
y horas `HH:mm` de 24 h. `GET /api/r/{slug}/s/{sucursal}/horario` devuelve los siete días de lunes a domingo y
`PUT` los reemplaza con `{ dias: [...] }` (exactamente siete días distintos; en un día `cerrado` las horas se
ignoran). Un cierre anterior a la apertura es un turno nocturno (`18:00`–`02:00`); apertura y cierre iguales se
rechazan. Los errores salen en las claves `dias` y `dias[i].apertura` / `dias[i].cierre`. Sólo `administrador_general`.

## CierreTemporalDto

Cierre temporal de una sucursal: `{ id, inicio, fin, motivo? }`, con `inicio` y `fin` en la hora local de la sucursal
sin zona (`yyyy-MM-ddTHH:mm`); en BD se guardan en UTC. `GET /api/r/{slug}/s/{sucursal}/cierres` lista los vigentes y
futuros (`fin` posterior a ahora); `POST` crea uno (201; `inicio` opcional, vacío = ahora; `fin` posterior al inicio y
no pasado; `motivo` hasta 120 caracteres); `POST …/cierres/{id}/cancelar` lo cancela (200). Un cierre inexistente, de
otra sucursal, ya cancelado o ya terminado responde 404 `Cierre no encontrado`. Los errores de fecha salen en las
claves `inicio` y `fin`. Sólo `administrador_general`.

## Estado de atención / EstadoAtencionDto

Estado de atención: si una sucursal acepta pedidos en este momento, según su horario semanal, sus cierres temporales
y su zona horaria. `EstadoAtencionDto` es `{ abierta, mensaje }`: `mensaje` es `null` si está abierta; si no, `Estamos
cerrados, abrimos {hoy|mañana|día} a las HH:mm` o, sin apertura en 14 días, `Estamos cerrados por el momento`.
`GET /api/r/{slug}/s/{sucursal}/estado` es público (sin token); una sucursal inexistente o inactiva responde 404
`Sucursal no disponible`. La imagen de la API incluye `tzdata` para resolver las zonas horarias.

## IEntidadDeRestaurante / IEntidadDeSucursal

Contratos del dominio que marcan las entidades con `RestauranteId` y, además, `SucursalId`, para aplicar el
aislamiento por restaurante y por sucursal.

## MiSesionDto

Respuesta de `GET /api/r/{slug}/mi-sesion` (004/RF-16): `{ nombreUsuario, rol, sucursales }`, con `sucursales` como lista de
`SucursalDeSesionDto` `{ slug, nombre }` ordenadas por nombre en el orden que da `ISucursalRepository.ListarAsync`
(intercalación de la base de datos, el mismo que `UsuarioDto`; el handler no reordena). El nombre sale de la base de datos, no del token. El
administrador general recibe todas las sucursales activas del restaurante; el resto, las asignadas y activas. Lo
atiende `ObtenerMiSesionHandler`; cualquier rol de restaurante puede consultarlo.

## SinSucursalesActivasException

Excepción de Application que `IniciarSesionHandler` lanza (004/RF-17) cuando las credenciales son válidas, el usuario
está activo y es administrador de sucursal, cajero o cocinero con todas sus sucursales desactivadas. `FiltroExcepciones`
la convierte en 403 `Sin sucursales activas` (ProblemDetails) y no se emite token. Se evalúa después de verificar la
contraseña y `Activo`, por lo que una contraseña incorrecta sigue dando 401. El administrador general y el de
plataforma quedan fuera de la regla.

## Categoría

Agrupación del menú de un restaurante (`Restaurante.Domain.Menu.Categoria`, 005/RF-02). Tiene `Nombre` (1–60 tras
`Trim()`), `NombreCorto` opcional (≤ 24; en blanco se guarda como `null`), `Orden` (entero ≥ 1) y `Activa`
(empieza en `true`). `NombreNormalizado` y `NombreCortoNormalizado` (`Trim().ToLowerInvariant()`, "Café" ≠ "Cafe")
sirven para la unicidad. Implementa `IEntidadDeRestaurante` e `IEliminable`.

## DatosCategoriaDto

Datos editables de una categoría tal como llegan por la API: `{ nombre?, nombreCorto?, orden? }` (todo anulable para
que los faltantes los valide `ValidadorDatosCategoria`, 005/RF-02: nombre 1–60 y nombre corto ≤ 24 tras `Trim()`, orden
entero ≥ 1; claves de error `nombre`, `nombreCorto`, `orden`, sin prefijo). Lo reutilizan el alta y la edición.

## CategoriaDto

Representación pública de una categoría: `{ id, nombre, nombreCorto, orden, activa }`. Es la respuesta de
`POST /api/r/{slug}/categorias` (201, `Location: /api/r/{slug}/categorias/{id}`; sólo `administrador_general`, 403 para
otro rol). El nombre y el nombre corto son únicos en el restaurante sin distinguir mayúsculas ni espacios
(`ComprobadorUnicidadCategoria`): si se repiten, 409 `Conflicto` con `errors.nombre` y/o `errors.nombreCorto`
(`ConflictoCamposException`). También es la respuesta de `PUT /api/r/{slug}/categorias/{id}` (editar, 005/RF-05; el
conflicto excluye a la propia categoría, así que cambiar sólo mayúsculas da 200) y de
`PUT /api/r/{slug}/categorias/{id}/estado` con `{ activa }` (005/RF-06; sin `activa` o no booleano, 400 en `activa`).
Una categoría desactivada sigue en la lista y conserva su nombre reservado. Otro restaurante, inexistente o eliminada:
404 `Categoría no encontrada` (005/RF-08).

## PaginaCategoriasDto

Respuesta de `GET /api/r/{slug}/categorias?pagina=&tamano=` (005/RF-04, sólo `administrador_general`):
`{ elementos: CategoriaDto[], total, pagina, tamano }`. Lista las categorías no eliminadas (activas e inactivas) del
restaurante por `orden` y nombre. Por defecto `pagina=1`, `tamano=20`; `pagina >= 1` y `tamano` entre 1 y 100
(`ListarCategoriasValidator`); un parámetro mal formado (`?pagina=abc`) o fuera de rango da 400 con `errors.pagina` /
`errors.tamano`.

## Nombre corto y título en WhatsApp

`TituloWhatsApp.Calcular(nombre, nombreCorto)` (005/RF-11): el nombre corto si existe; si no, el nombre cuando mide
24 unidades UTF-16 o menos; si no, los primeros 23 caracteres más `…`, sin partir un par sustituto (si la unidad 23
es un sustituto alto se corta en 22). `SeRecorta` indica cuándo se avisa "Este nombre se verá recortado en WhatsApp".

## Borrado lógico (IEliminable)

Contrato `IEliminable { DateTimeOffset? EliminadaEn }`: eliminar (`Categoria.Eliminar`, 005/RF-07) sólo marca la fecha
y conserva los datos; las consultas excluyen las entidades eliminadas. Se expone como
`DELETE /api/r/{slug}/categorias/{id}` (`EliminarCategoriaCommand`): 204 sin cuerpo, sólo `administrador_general`
(403 para otro rol); la categoría sale de la lista y libera su nombre y nombre corto. Otro restaurante, inexistente o
ya eliminada (un segundo DELETE): 404 `Categoría no encontrada` (005/RF-07, RF-08, RF-09).

## Platillo

Plato del menú de un restaurante dentro de una categoría (`Restaurante.Domain.Menu.Platillo`, 006/RF-02). Tiene
`Nombre` (1–60 tras `Trim()`), `NombreCorto` (≤ 24, vacío → `null`), `Descripcion` (≤ 300, vacía → `null`), `Orden`
(≥ 1), `Ingredientes` y `Activo` (empieza en `true`). Se crea con `DatosPlatillo` y se edita con `Editar(categoriaId,
datos)`, que valida todo antes de asignar. `Eliminar` sólo marca `EliminadaEn` y conserva los datos (006/RF-09).

## Ingredientes incluidos (IngredientesPlatillo)

Lista de ingredientes de un platillo (006/RF-02): hasta 30 (`MaximoElementos`), cada uno de 1–40 caracteres
(`LongitudMaxima`) tras `Trim()`, sin repetidos según `Trim().ToLowerInvariant()`; se guardan recortados y en su orden.
`IngredientesPlatillo.Errores` devuelve los mensajes (`El ingrediente {n} está vacío`, `... no puede superar 40
caracteres`, `... está repetido`, `No puede haber más de 30 ingredientes`; `{n}` desde 1) y es la única fuente de las
reglas; `Normalizar` recorta y lanza `ArgumentException` si hay errores.

## DatosPlatilloDto

Datos editables de un platillo tal como llegan por la API: `{ nombre?, nombreCorto?, descripcion?, orden?, ingredientes? }`
(todo anulable para que los faltantes los valide `ValidadorDatosPlatillo`, 006/RF-02; claves de error `nombre`,
`nombreCorto`, `descripcion`, `orden`, `ingredientes`, sin prefijo; la clave `ingredientes` recoge los mensajes de
`IngredientesPlatillo.Errores`). `ADominio()` produce el `DatosPlatillo` del dominio. Lo reutilizan el alta y la edición.

## PlatilloDto

Representación pública de un platillo: `{ id, categoriaId, categoriaNombre, nombre, nombreCorto, descripcion, orden,
ingredientes, activo }`. Es la respuesta de `POST /api/r/{slug}/categorias/{categoriaId}/platillos` (201,
`Location: /api/r/{slug}/platillos/{id}`; sólo `administrador_general`, 403 para otro rol; 006/RF-01, RF-11). Orden de
comprobación del alta: categoría inexistente, eliminada o de otro restaurante, 404 `Categoría no encontrada`; categoría
inactiva, 400 en `categoriaId` (`La categoría está inactiva`); nombre o nombre corto repetidos entre los platillos del
restaurante (`ComprobadorUnicidadPlatillo`), 409 `Conflicto` con `errors.nombre` y/o `errors.nombreCorto` (006/RF-03,
RF-04). Un platillo puede llamarse como una categoría: la unicidad es sólo entre platillos.

## PaginaPlatillosDto

Respuesta de `GET /api/r/{slug}/categorias/{categoriaId}/platillos?pagina=&tamano=` (006/RF-05, RF-11, sólo
`administrador_general`): `{ elementos: PlatilloDto[], total, pagina, tamano }`. Lista los platillos no eliminados de la
categoría (activos e inactivos; también si la categoría está inactiva) por `orden`, nombre e id. Mismos parámetros y claves de
error que `PaginaCategoriasDto` (defecto 1 y 20; `tamano` 1–100). Categoría inexistente, eliminada o ajena: 404
`Categoría no encontrada`. El detalle `GET /api/r/{slug}/platillos/{id}` devuelve un `PlatilloDto` (006/RF-06; 404 `Platillo
no encontrado`, RF-10) y `GET /api/r/{slug}/categorias/{id}` devuelve un `CategoriaDto` (006/RF-05).
