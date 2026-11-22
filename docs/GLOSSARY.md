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

## RestauranteDto

Representación pública de un restaurante en la API de plataforma: `{ id, nombre, slug, activo, paleta }`.
Es la respuesta de `POST /api/plataforma/restaurantes` (201).

## CrearRestaurantePeticion

Cuerpo de `POST /api/plataforma/restaurantes` (sólo `administrador_plataforma`):
`{ nombre, slug, paleta?, administrador: { nombreUsuario, contrasena } }`. Crea el restaurante activo y
su primer `administrador_general` en una sola transacción. Nombre o slug repetidos dan 409 (el nombre se
compara recortado y sin distinguir mayúsculas); los campos inválidos dan 400 como `slug`, `nombre`,
`paleta.principal` o `administrador.contrasena`.
