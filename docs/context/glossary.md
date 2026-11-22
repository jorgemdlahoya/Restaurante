# Glosario de contexto

- **Usuario**: persona que accede al sistema con un nombre de usuario y una contraseña. Tiene un
  rol, puede estar activo o inactivo y pertenece a un restaurante (`RestauranteId`), salvo el
  administrador de plataforma, que no tiene ninguno. Su nombre es único dentro de su restaurante sin
  distinguir mayúsculas (`NombreUsuarioNormalizado`, en minúsculas invariantes).
- **Rol**: perfil de permisos de un usuario: `AdministradorPlataforma`, `AdministradorGeneral`,
  `AdministradorSucursal`, `Cajero` o `Cocinero`. En la API, el JWT y la base de datos se serializa
  en snake_case (`administrador_plataforma`, `administrador_general`, ...), con coincidencia exacta.
- **Administrador inicial**: primer usuario con rol `AdministradorPlataforma`, creado al arrancar la API
  cuando no existe ningún administrador de plataforma, a partir de `ADMIN_USERNAME` y `ADMIN_PASSWORD`.
- **Restaurante**: restaurante del sistema multi-restaurante (`Restaurante.Domain.Restaurantes`). Tiene
  nombre (1-80 caracteres, recortado), `Slug` inmutable, paleta y estado activo/inactivo; nace activo.
- **Slug**: identificador de un restaurante en las direcciones (`/r/{slug}`); cumple
  `^[a-z0-9-]{3,40}$` y no se puede cambiar.
- **Paleta**: los cinco colores de tema de un restaurante (`Principal`, `Acento`, `Oscuro`, `Fondo`,
  `Texto`), cada uno `#RRGGBB` o ausente.
- **Logo**: imagen de un restaurante (`LogoRestaurante`): bytes, tipo MIME, `ETag` y fecha de
  actualización; uno por restaurante.
- **Contexto de restaurante**: restaurante en cuyo ámbito se atiende una petición
  (`IContextoRestaurante.RestauranteId`); `null` es el ámbito de plataforma. Determina qué usuarios ve
  el filtro global de la base de datos.
