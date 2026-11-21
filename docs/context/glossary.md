# Glosario de contexto

- **Usuario**: persona que accede al sistema con un nombre de usuario y una contraseña. Tiene un
  rol, puede estar activo o inactivo y su nombre es único sin distinguir mayúsculas
  (`NombreUsuarioNormalizado`, en minúsculas invariantes).
- **Rol**: perfil de permisos de un usuario: `Mesero`, `Cocina`, `Caja` o `Administrador`. En la
  API y en el JWT se serializa en minúsculas.
- **Administrador inicial**: primer usuario con rol `Administrador`, creado al arrancar la API
  cuando no existen usuarios, a partir de `ADMIN_USERNAME` y `ADMIN_PASSWORD`.
