# Glosario de contexto

- **Usuario**: persona que accede al sistema con un nombre de usuario y una contraseña. Tiene un
  rol, puede estar activo o inactivo y su nombre es único sin distinguir mayúsculas
  (`NombreUsuarioNormalizado`, en minúsculas invariantes).
- **Rol**: perfil de permisos de un usuario: `AdministradorPlataforma`, `AdministradorGeneral`,
  `AdministradorSucursal`, `Cajero` o `Cocinero`. En la API, el JWT y la base de datos se serializa
  en snake_case (`administrador_plataforma`, `administrador_general`, ...), con coincidencia exacta.
- **Administrador inicial**: primer usuario con rol `AdministradorPlataforma`, creado al arrancar la API
  cuando no existen usuarios, a partir de `ADMIN_USERNAME` y `ADMIN_PASSWORD`.
