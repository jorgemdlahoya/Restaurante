# Glosario

## Rol

Perfil de permisos de un usuario del sistema. Valores: `Mesero`, `Cocina`, `Caja` y
`Administrador`. En la API y en el JWT se serializa en minúsculas (`mesero`, `cocina`, `caja`,
`administrador`).

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
