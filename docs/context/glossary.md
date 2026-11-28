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
  `Usuario.Crear` y `Usuario.AsignarRolYSucursales` la exigen). La API indica las sucursales por `slug`; error 400
  en `sucursales`. Alta y edición validan rol y sucursales con las mismas reglas (`ReglasRolYSucursales`).
- **EditarUsuarioPeticion**: cuerpo `{ rol, sucursales }` de `PUT /api/r/{slug}/usuarios/{id}` (004/RF-04; sustituye
  a `PUT /{id}/rol`). El nombre de usuario no se edita. Errores: 400 validador → 404 usuario de otro restaurante →
  403 si no puede gestionarlo → 403 slug ajeno (administrador de sucursal) / 400 slug inexistente (administrador
  general) → 403 si no puede asignar → 409 (quitarse a sí mismo el rol de administrador general o dejar el
  restaurante sin ninguno activo). Un administrador de sucursal con su sucursal inactiva recibe 403 primero, tras el
  400 del validador (004/RF-26). Sólo un cambio
  real de rol o del conjunto de sucursales renueva el sello de sesión (`AsignarRolYSucursales` devuelve si cambió).
- **CambiarContrasenaPeticion**: cuerpo `{ contrasena }` de `PUT /api/r/{slug}/usuarios/{id}/contrasena` (004/RF-06, 204);
  mínimo 8 caracteres. Errores: 400 validador → 403 sucursal propia inactiva (004/RF-26) → 404 otro restaurante → 403
  si no puede gestionarlo. El general puede cambiar la suya (su token anterior da 401);
  renueva el sello de sesión.
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
- **Sesion** (frontend): `{ token, expiraEn, nombreUsuario, rol, slug, sucursalActiva? }` en `ProveedorSesion`
  (memoria + `sessionStorage`, clave `restaurante.sesion`); caduca sola en `expiraEn`. Al cerrarse (`cerrar` o
  "Sesión no válida") vacía la caché de TanStack Query.
- **TITULO_SESION_INVALIDA / alSesionInvalida** (frontend, `api/cliente.ts`): título `'Sesión no válida'` del 401 de
  004/RF-13/RF-14; `pedirApi` avisa a los suscritos de `alSesionInvalida` si la petición llevaba token.
- **sesionTerminada / MENSAJE_SESION_TERMINADA** (frontend): la API rechazó la sesión; el login muestra "Tu sesión
  terminó. Vuelve a iniciar sesión." hasta el siguiente inicio (004/RF-23).
- **MENSAJE_SIN_SUCURSALES** (frontend): "No tienes sucursales activas. Habla con tu administrador." en el login (403
  `Sin sucursales activas`, 004/RF-24) y en el inicio si `mi-sesion` no trae sucursales (004/RF-27).
- **MENSAJE_SIN_PERMISO** (frontend): "No tienes permiso para esta acción." ante un 403 en la gestión de usuarios; un
  409 al cambiar el estado muestra el detalle de la API.
- **ROLES_GESTION_USUARIOS / NOMBRES_ROL** (frontend): roles que abren `/r/{slug}/usuarios` (los dos administradores;
  el resto vuelve al inicio, 004/RF-22) y nombre visible de cada rol de restaurante.
- **LIMITE_TITULO_WHATSAPP** (frontend, `menu/tituloWhatsApp.ts`): 24, espejo de `TituloWhatsApp.Limite`; `menu/mensajeError.ts`
  muestra "No tienes permiso para esta acción." ante cualquier 403 de categorías.
- **MensajesCategoria / ICategoriaRepository / ListarCategoriasValidator.TamanoMaximo**: textos de categorías, puerto del
  repositorio en Application y tope 100 de `tamano` del listado (005/RF-04).
- **claveMiSesion / mi-sesion** (frontend, `sesion/miSesion.ts`): `obtenerMiSesion` pide `GET /api/r/{slug}/mi-sesion`;
  `claveMiSesion(slug)` = `['mi-sesion', slug]`. Alimenta la elección de sucursal, el selector del personal, el aviso
  sin sucursales del inicio y las sucursales fijas del formulario de usuarios.
- **ErrorApi** (frontend): excepción de `pedirApi` con `estado`, título, detalle, `errores` por campo y `extensiones`
  (resto del ProblemDetails, p. ej. `platillos`).
- **MensajesPlatillo / IPlatilloRepository**: textos de platillos y puerto del repositorio en Application (006).
- **EditarPlatilloCommand / CambiarEstadoPlatilloCommand**: `PUT /api/r/{slug}/platillos/{id}` (400 en `categoriaId`:
  `La categoría es obligatoria` / `no existe` / `está inactiva`; 006/RF-07) y `PUT .../platillos/{id}/estado` con
  `{ activo }` (006/RF-08).
- **MAXIMO_INGREDIENTES / AVISO_CATEGORIA_INACTIVA / TAMANO_PAGINA / json / DialogoConfirmacion.confirmar** (frontend):
  constantes y utilidades de `menu/`; sin `confirmar` el diálogo sólo ofrece "Cancelar".
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
- **MiSesionDto / SucursalDeSesionDto**: `{ nombreUsuario, rol, sucursales: [{ slug, nombre }] }`; respuesta de
  `GET /api/r/{slug}/mi-sesion` (004/RF-16; cualquier rol de restaurante). General: todas las sucursales activas; resto:
  asignadas y activas, en el orden por nombre del repositorio (como `UsuarioDto`); el nombre sale de la base de datos.
- **SinSucursalesActivasException**: login con credenciales válidas y usuario activo, pero con todas sus sucursales
  desactivadas (no aplica al administrador general): 403 `Sin sucursales activas` y ningún token (004/RF-17).
- **Categoría**: agrupación del menú de un restaurante (`Domain.Menu.Categoria`, 005/RF-02): nombre 1–60, nombre
  corto opcional ≤ 24 (en blanco → `null`), orden ≥ 1, activa por defecto; normalizados con `Trim().ToLowerInvariant()`.
- **DatosCategoriaDto**: `{ nombre?, nombreCorto?, orden? }` validado por `ValidadorDatosCategoria` (claves `nombre`,
  `nombreCorto`, `orden` sin prefijo; 005/RF-02).
- **CategoriaDto**: `{ id, nombre, nombreCorto, orden, activa }`; respuesta de `POST /api/r/{slug}/categorias` (201,
  400 por campo, 409 con `errors` por nombre/nombre corto repetidos vía `ConflictoCamposException`; sólo `administrador_general`). También responde `PUT .../categorias/{id}` (editar; excluye su propio id en la unicidad,
  005/RF-05) y `PUT .../categorias/{id}/estado` con `{ activa }` (400 en `activa` si falta o no es booleano, 005/RF-06);
  404 `Categoría no encontrada` si es de otro restaurante, inexistente o eliminada (005/RF-08).
- **PaginaCategoriasDto**: `{ elementos, total, pagina, tamano }`; respuesta de `GET /api/r/{slug}/categorias?pagina=&tamano=`
  (defecto 1 y 20; pagina ≥ 1, tamano 1–100; 400 con `errors.pagina`/`errors.tamano`; 005/RF-04).
- **Nombre corto / título en WhatsApp**: `TituloWhatsApp.Calcular` (005/RF-11): nombre corto, o el nombre si ≤ 24, o
  23 caracteres + `…` sin partir pares sustitutos; `SeRecorta` dispara el aviso de recorte.
- **Borrado lógico / IEliminable**: `EliminadaEn` marca la eliminación y conserva los datos (005/RF-07). Endpoint
  `DELETE /api/r/{slug}/categorias/{id}`: 204 sin cuerpo, sólo `administrador_general` (403 otro rol); libera nombre y
  nombre corto; 404 `Categoría no encontrada` si es de otro restaurante, inexistente o ya eliminada (005/RF-08, RF-09).
- **Platillo / DatosPlatillo**: `Restaurante.Domain.Menu.Platillo` (006/RF-02): nombre 1–60, nombre corto ≤ 24 y
  descripción ≤ 300 (vacíos → `null`), orden ≥ 1, `Activo` inicia en `true`; `Editar(categoriaId, datos)` valida antes
  de asignar. `Eliminar` marca `EliminadaEn` y conserva los datos (006/RF-09).
- **Ingredientes incluidos / IngredientesPlatillo**: ≤ 30 ingredientes de 1–40 caracteres, sin repetidos según
  `Trim().ToLowerInvariant()`, guardados recortados y en orden (006/RF-02). `Errores` es la única fuente de los mensajes
  (`El ingrediente {n} está vacío|no puede superar 40 caracteres|está repetido`, `{n}` desde 1); `Normalizar` lanza si hay.
- **DatosPlatilloDto**: `{ nombre?, nombreCorto?, descripcion?, orden?, ingredientes? }` validado por
  `ValidadorDatosPlatillo` (claves sin prefijo; `ingredientes` usa `IngredientesPlatillo.Errores`; 006/RF-02).
- **PlatilloDto**: `{ id, categoriaId, categoriaNombre, nombre, nombreCorto, descripcion, orden, ingredientes, activo }`;
  respuesta de `POST /api/r/{slug}/categorias/{categoriaId}/platillos` (201 con `Location`, 400 por campo, 404 categoría no
  encontrada, 400 en `categoriaId` si está inactiva, 409 por nombre/nombre corto vía `ComprobadorUnicidadPlatillo`; sólo
  `administrador_general`; 006/RF-01, RF-03, RF-04, RF-11).
- **PaginaPlatillosDto**: `{ elementos, total, pagina, tamano }`; respuesta de
  `GET /api/r/{slug}/categorias/{categoriaId}/platillos?pagina=&tamano=` (defecto 1 y 20; 404 si la categoría no existe;
  006/RF-05). Detalles: `GET .../platillos/{id}` (PlatilloDto, 404 `Platillo no encontrado`; RF-06, RF-10) y
  `GET .../categorias/{id}` (CategoriaDto; RF-05). `DELETE .../platillos/{id}` elimina (borrado lógico, 204; 404; RF-09, RF-10).
- **CategoriaConPlatillosException**: `Cantidad` + mensaje `Esta categoría tiene N platillos` / `... 1 platillo`; la lanza
  `EliminarCategoriaHandler` si la categoría tiene platillos no eliminados y el filtro responde 409 `Conflicto` con
  `platillos: N` (006/RF-12).
- **Variante / DatosVariante** (007/RF-03): `Restaurante.Domain.Menu.Variante` es una presentación con precio de un platillo
  (`PlatilloId`): nombre 1–24 tras `Trim()` (`NombreNormalizado` = `Trim().ToLowerInvariant()`), `Precio` y `Sku` opcional
  (vacío → `null`, patrón `^[A-Za-z0-9_-]{1,40}\z`, `SkuNormalizado` = `Trim().ToUpperInvariant()`); `Activa` inicia en `true`;
  `Editar` valida antes de asignar; `Eliminar` marca `EliminadaEn` y conserva los datos (RF-09).
- **Precio base / PrecioVariante**: `decimal` de 0.00 a 99,999.99 (`Maximo`) con hasta 2 decimales (`decimal.Round(p, 2) == p`).
  `PrecioVariante.Errores(decimal?)` es la única fuente de los mensajes (`El precio es obligatorio|no puede ser negativo|no puede
  superar 99,999.99|debe tener como máximo 2 decimales`).
- **Variante Regular**: variante automática `Variante.Regular(...)` de nombre `Regular`, sin SKU, con el precio base del platillo (RF-01).
- **Variantes a la venta** (`VariantesALaVenta.De`, RF-14): variantes activas y no eliminadas del platillo, si categoría y platillo
  están activos y no eliminados; ordenadas por precio, nombre (ordinal) e id.
