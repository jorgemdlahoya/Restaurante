# Glosario

## Rol

Perfil de permisos de un usuario del sistema (los cinco roles de la constitución). Valores:
`AdministradorPlataforma`, `AdministradorGeneral`, `AdministradorSucursal`, `Cajero` y `Cocinero`.
En la API, el JWT y la base de datos se serializa en snake_case (`administrador_plataforma`,
`administrador_general`, `administrador_sucursal`, `cajero`, `cocinero`); la coincidencia es exacta.
`POST /api/usuarios` y `PUT /api/usuarios/{id}/rol` aceptan todos salvo `administrador_plataforma`.

## LoginPeticion

Cuerpo de `POST /api/auth/login`: `{ nombreUsuario, contrasena }`. Endpoint anónimo. Los campos
vacíos o ausentes dan 400 (validador); credenciales incorrectas o usuario desactivado dan 401 con
el mismo mensaje.

## LoginRespuestaDto

Respuesta 200 de `POST /api/auth/login`: `{ token, expiraEn, nombreUsuario, rol }`. `token` es el
JWT (HS256), `expiraEn` su instante de expiración y `rol` va en minúsculas.

## JWT (emisor y audiencia)

Los tokens llevan `iss = restaurante-api` y `aud = restaurante-web`, y los claims `sub` (id del
usuario), `unique_name` (nombre de usuario) y `role` (rol en minúsculas). Se validan firma
(HS256 con `JWT_SIGNING_KEY`), emisor, audiencia y expiración, sin tolerancia de reloj
(`ClockSkew = 0`).

## UsuarioDto

Representación pública de un usuario en la API: `{ id, nombreUsuario, rol, activo }`. Nunca incluye
la contraseña ni su hash. Es la respuesta de `GET /api/usuarios` (lista), `POST /api/usuarios`
(201), `PUT /api/usuarios/{id}/estado` y `PUT /api/usuarios/{id}/rol`.

## CrearUsuarioPeticion

Cuerpo de `POST /api/usuarios`: `{ nombreUsuario, contrasena, rol }`. El rol va en minúsculas.

## CambiarEstadoPeticion

Cuerpo de `PUT /api/usuarios/{id}/estado`: `{ activo }` (booleano).

## CambiarRolPeticion

Cuerpo de `PUT /api/usuarios/{id}/rol`: `{ rol }`, en minúsculas.
