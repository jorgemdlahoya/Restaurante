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
  el filtro global de la base de datos. En la API lo fija `MiddlewareContextoRestaurante` por petición
  (`ContextoRestaurante.Establecer`): por el claim `restaurant_id` del token o, sin él, por el `slug` de
  `/api/r/{slug}/…`; nunca en `/api/plataforma/…`.
- **Restaurante no disponible**: 404 que responde `/api/r/{slug}/…` cuando el `slug` no existe, el
  restaurante está inactivo o el token es de otro restaurante (detalle
  `La dirección no corresponde a un restaurante activo.`).
- **Restaurante no activo (token)**: 401 `No autenticado` / `El restaurante no está activo.` (con
  `WWW-Authenticate: Bearer`) para un token cuyo `restaurant_id` no existe o está desactivado; el token
  vuelve a valer al reactivar el restaurante.
- **LoginPeticion**: cuerpo `{ nombreUsuario, contrasena }` de `POST /api/r/{slug}/auth/login` y
  `POST /api/plataforma/auth/login`.
- **restaurant_id**: claim del JWT con el restaurante del usuario; ausente en los tokens de plataforma.
- **Sello de sesión**: `SelloSesion` (Guid) del usuario, en el token como claim `sello`; se renueva al
  desactivar, cambiar contraseña, rol o sucursales. `MiddlewareSesionUsuario` responde 401 `Sesión no válida` /
  `Vuelve a iniciar sesión.` (`WWW-Authenticate: Bearer`) si falta, no coincide o el usuario no existe o está inactivo.
- **Cardinalidad rol ↔ sucursales** (004/RF-02): `administrador_general`/`administrador_plataforma` 0,
  `administrador_sucursal` exactamente 1, `cajero`/`cocinero` ≥ 1, sin repetir (`Usuario.CardinalidadValida`;
  `Usuario.Crear` la exige). La API indica las sucursales por `slug`; error 400 en `sucursales`.
- **UsuarioDto**: `{ id, nombreUsuario, rol, activo, sucursales: [{ slug, nombre, activa }], gestionable }`;
  sucursales por nombre; `gestionable` según el alcance de quien consulta.
- **ResolutorActor**: arma el `ActorUsuarios` desde `IUsuarioActual` (403 si no puede operar), resuelve slugs de
  sucursal en el restaurante (400 si no existe; 403 para el administrador de sucursal con un slug ajeno) y proyecta
  `UsuarioDto`.
- **PaletaDto**: paleta de un restaurante en la API (`principal`, `acento`, `oscuro`, `fondo`, `texto`),
  cada color `#RRGGBB` o `null`.
- **IdentidadDto**: `{ nombre, slug, paleta, logoUrl }`; respuesta anónima de
  `GET /api/r/{slug}/identidad` (`logoUrl` es `null` sin logo).
- **LogoDto**: `{ contenido, tipo, etag }`; logo servido por `GET /api/r/{slug}/logo` (ETag, 304) y subido
  por `PUT /api/plataforma/restaurantes/{id}/logo` (multipart `logo`, PNG/JPG/WebP, ≤ 1 MB).
- **RestauranteDto**: `{ id, nombre, slug, activo, paleta }`; respuesta de
  `POST /api/plataforma/restaurantes`.
- **CrearRestaurantePeticion**: cuerpo de `POST /api/plataforma/restaurantes`:
  `{ nombre, slug, paleta?, administrador: { nombreUsuario, contrasena } }`. Crea el restaurante y su
  primer `administrador_general` en una sola transacción; sólo `administrador_plataforma`. El nombre es
  único de forma exacta (distingue mayúsculas, como el índice de la base).
- **EditarRestaurantePeticion**: cuerpo `{ nombre, paleta? }` de `PUT /api/plataforma/restaurantes/{id}`;
  `paleta` nula borra la paleta.
- **Sesion** (frontend): `{ token, expiraEn, nombreUsuario, rol, slug }` en `ProveedorSesion` (memoria +
  `sessionStorage`, clave `restaurante.sesion`); caduca sola en `expiraEn`.
- **ErrorApi** (frontend): excepción de `pedirApi` con `estado`, título, detalle y `errores` por campo.
- **RutaProtegida** (frontend): ruta que exige sesión con un rol (y slug) válidos o redirige al login.
- **ProveedorTema / paleta** (frontend): escribe `--color-principal|acento|oscuro|fondo|texto` en `:root`
  (por defecto en `tema.css`); sólo se usan `var(--color-*)`.
- **Identidad** (frontend): `{ nombre, slug, paleta, logoUrl }` cargada por `DisenoRestaurante`.
- **VistaPreviaPaleta** (frontend): vista previa de la paleta con variables locales a su elemento.
- **FormularioRestaurante** (frontend): alta y edición de restaurantes en plataforma.
- **Sucursal** (003): local de un restaurante; `slug` inmutable, teléfono/WhatsApp E.164 (`TelefonoMexico`),
  prefijo `^[A-Z]{1,3}\z`, zona horaria admitida (`ZonasHorarias`), `phone_number_id` opcional; nace activa.
- **Prefijo**: 1-3 letras mayúsculas de la sucursal.
- **Zona horaria**: una de las 12 zonas mexicanas admitidas; por defecto `America/Mexico_City`.
- **phone_number_id**: identificador numérico (1-30 dígitos) del número de WhatsApp Business; opcional.
- **DatosSucursalDto**: `{ nombre, direccion, telefono, whatsApp, phoneNumberId?, prefijo, zonaHoraria? }`; zona
  ausente = `America/Mexico_City`, `phoneNumberId` vacío = ausente; validado por `ValidadorDatosSucursal`.
- **SucursalDto**: `{ id, nombre, direccion, telefono, slug, whatsApp, phoneNumberId, prefijo, zonaHoraria, activa }`;
  respuesta de `POST /api/r/{slug}/sucursales` (201, 409 por duplicados; sólo `administrador_general`).
- **HorarioDiaDto**: `{ dia, cerrado, apertura?, cierre? }` (día en español, horas `HH:mm`); `GET`/`PUT
  /api/r/{slug}/s/{sucursal}/horario` (siete días lunes→domingo; cierre < apertura = turno nocturno; sólo `administrador_general`).
- **CierreTemporalDto**: `{ id, inicio, fin, motivo? }` con fechas locales de la sucursal `yyyy-MM-ddTHH:mm` (UTC en BD);
  `GET`/`POST /api/r/{slug}/s/{sucursal}/cierres` (vigentes y futuros / crear, `inicio` opcional = ahora) y
  `POST …/cierres/{id}/cancelar` (404 si es de otra sucursal, ya cancelado o ya terminado; sólo `administrador_general`).
- **Estado de atención / EstadoAtencionDto**: `{ abierta, mensaje? }` (horario + cierres + zona de la sucursal);
  `GET /api/r/{slug}/s/{sucursal}/estado` público sin token; 404 `Sucursal no disponible` si no existe o está inactiva.
- **IEntidadDeRestaurante / IEntidadDeSucursal**: contratos de entidades con `RestauranteId` (y `SucursalId`).
