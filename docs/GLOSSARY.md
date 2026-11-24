# Glosario

## Rol

Perfil de permisos de un usuario del sistema (los cinco roles de la constitución). Valores:
`AdministradorPlataforma`, `AdministradorGeneral`, `AdministradorSucursal`, `Cajero` y `Cocinero`.
En la API, el JWT y la base de datos se serializa en snake_case (`administrador_plataforma`,
`administrador_general`, `administrador_sucursal`, `cajero`, `cocinero`); la coincidencia es exacta.
`POST /api/r/{slug}/usuarios` y `PUT /api/r/{slug}/usuarios/{id}/rol` aceptan todos salvo `administrador_plataforma`.

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

## JWT (emisor y audiencia)

Los tokens llevan `iss = restaurante-api` y `aud = restaurante-web`, y los claims `sub` (id del
usuario), `unique_name` (nombre de usuario), `role` (rol en minúsculas) y, si el usuario tiene
restaurante, `restaurant_id`. Se validan firma
(HS256 con `JWT_SIGNING_KEY`), emisor, audiencia y expiración, sin tolerancia de reloj
(`ClockSkew = 0`).

## UsuarioDto

Representación pública de un usuario en la API: `{ id, nombreUsuario, rol, activo }`. Nunca incluye
la contraseña ni su hash. Es la respuesta de `GET /api/r/{slug}/usuarios` (lista, sólo los del
restaurante), `POST /api/r/{slug}/usuarios` (201, `Location: /api/r/{slug}/usuarios/{id}`),
`PUT /api/r/{slug}/usuarios/{id}/estado` y `PUT /api/r/{slug}/usuarios/{id}/rol`. Un usuario de otro
restaurante da 404.

## CrearUsuarioPeticion

Cuerpo de `POST /api/r/{slug}/usuarios`: `{ nombreUsuario, contrasena, rol }`. El rol va en minúsculas.
El nombre es único sólo dentro del restaurante.

## CambiarEstadoPeticion

Cuerpo de `PUT /api/r/{slug}/usuarios/{id}/estado`: `{ activo }` (booleano).

## CambiarRolPeticion

Cuerpo de `PUT /api/r/{slug}/usuarios/{id}/rol`: `{ rol }`, en minúsculas.

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

## IEntidadDeRestaurante / IEntidadDeSucursal

Contratos del dominio que marcan las entidades con `RestauranteId` y, además, `SucursalId`, para aplicar el
aislamiento por restaurante y por sucursal.
