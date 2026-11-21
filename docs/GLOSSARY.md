# Glosario

## Rol

Perfil de permisos de un usuario del sistema. Valores: `Mesero`, `Cocina`, `Caja` y
`Administrador`. En la API y en el JWT se serializa en minúsculas (`mesero`, `cocina`, `caja`,
`administrador`).

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
