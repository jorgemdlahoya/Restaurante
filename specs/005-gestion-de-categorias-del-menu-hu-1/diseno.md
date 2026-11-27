---
feature: 005-gestion-de-categorias-del-menu-hu-1
estado: borrador
---
# Diseño: Gestión de categorías del menú (HU-1)

## Enfoque elegido y alternativas descartadas

- **Elegido (decisión del usuario): seguir el patrón de las sucursales de la feature 003.** Entidad
  `Categoria` del restaurante (`IEntidadDeRestaurante`, filtro global por convención) con borrado
  lógico, endpoints CQRS bajo `/api/r/{slug}/categorias` sólo para el administrador general, y una
  pantalla de tabla + formulario como la de sucursales.
- **Descartado: ordenar arrastrando.** Incómodo en móvil y difícil de probar; el backlog fija el orden
  como número entero positivo con sus pruebas (0, negativo, decimal → 400).
- **Descartado: categorías y platillos en una misma feature.** HU-2 es una historia propia; juntarlas
  duplicaría el tamaño.
- **Decisión del usuario: eliminar libera el nombre.** Una categoría eliminada desaparece de la
  pantalla, no se recupera desde ella y su nombre y nombre corto quedan libres; se conserva en la base
  como histórico (Art. 13). Para retirarla temporalmente está "Desactivar".
- **Llamada del agente: repetidos → 409**, como en 002 y 003 (el backlog dice 400; se mantiene la
  convención del proyecto: 400 formato, 409 conflicto con otro registro).
- **Llamada del agente: orden repetido permitido**; empate → por nombre.
- **Llamada del agente: RN1.2 (platillos activos) se aplaza a la HU-2**, porque hoy no existen platillos
  y una comprobación sin datos no se puede probar.
- **Llamada del agente: ruta de pantalla `/r/{slug}/menu/categorias`**, para agrupar ahí platillos y
  variantes después.
- **Llamada del agente: normalización = `Trim().ToLowerInvariant()`** (como el nombre de usuario de
  002): "Café" y "Cafe" son distintos; "Bebidas" y " bebidas " son el mismo.
- **Llamada del agente: el 409 dice qué campo choca.** Respuesta `ProblemDetails` con estado 409,
  título "Conflicto" y extensión `errors` igual que el 400 (`{ "nombre": [...], "nombreCorto": [...] }`),
  con todos los campos repetidos a la vez; en una carrera, la violación se traduce por el nombre del
  índice (si no se reconoce, se atribuye a `nombre`). El frontend pinta 400 y 409 igual, por campo.
- **Llamada del agente: longitudes en unidades UTF-16** (`string.Length` en C# y en JS), con los mismos
  casos de prueba en ambos lados; los emojis cuentan como 2.
- **Llamada del agente: borrado lógico en el filtro global** mediante una interfaz de marca
  (p. ej. `IEliminable { DateTimeOffset? EliminadaEn }`) que la convención de `FiltrosGlobales`
  combina con el filtro de restaurante en una sola expresión; el diseño exacto queda al constructor
  siempre que 003/004 no cambien de comportamiento.

## Arquitectura

Sobre la Clean Architecture de 001–004:

- **Domain**: `Categoria` (agregado, `IEntidadDeRestaurante`) y `TituloWhatsApp` (función pura).
- **Application**: casos de uso `CrearCategoria`, `ListarCategorias`, `EditarCategoria`,
  `CambiarEstadoCategoria`, `EliminarCategoria`; validador común; `ICategoriaRepository`.
- **Infrastructure**: tabla `categorias`, configuración EF, repositorio, migración `Categorias`.
- **WebApi**: `CategoriasController` con `[RequiereRol(Rol.AdministradorGeneral)]`.
- **Frontend**: `apps/web/src/menu/` (página, formulario, API, vista previa); la página usa
  `MarcoRestaurante` y se registra como hija de `/r/:slug` (`DisenoRestaurante`) en `App.tsx`.

## Componentes

- **`Categoria`** (Domain): `Id`, `RestauranteId`, `Nombre`, `NombreCorto?`, `Orden`, `Activa`,
  `CreadaEn`, `EliminadaEn?`; `Crear(restauranteId, datos, ahora)`, `Editar(datos)`, `Activar()`,
  `Desactivar()`, `Eliminar(ahora)`. Recorta espacios, convierte nombre corto vacío en ausente y lanza
  `ArgumentException` si un formato de RF-02 no se cumple. Cubre: RF-01, RF-02, RF-05, RF-06, RF-07
- **`TituloWhatsApp.Calcular(nombre, nombreCorto)`** (Domain, puro): nombre corto si existe; si no,
  el nombre si tiene ≤ 24 caracteres; si no, 23 caracteres + "…"; y `SeRecorta(nombre, nombreCorto)`.
  Cubre: RF-11
- **Validador de categoría** (Application): común a crear y editar; nombre 1–60, nombre corto ≤ 24,
  orden entero ≥ 1. El cuerpo llega con `orden` como número JSON; un decimal, texto o ausente da 400 en
  `orden` (no 500 ni error genérico de deserialización). Cubre: RF-02
- **`CrearCategoria` / `EditarCategoria`** (Application + WebApi): comprueban unicidad de nombre y
  nombre corto entre las no eliminadas del restaurante (comparación por la forma normalizada: recortada
  y en minúsculas) y responden 409 con título "Conflicto" y detalle "Ya existe una categoría con ese
  nombre" o "…con ese nombre corto"; los índices únicos parciales cubren las carreras (violación → 409).
  Cubre: RF-01, RF-03, RF-05
- **`ListarCategorias`** (Application + WebApi): paginado en la consulta (`Skip/Take`), orden por
  `Orden` y `Nombre`; respuesta `{ elementos, total, pagina, tamano }`; parámetros fuera de rango →
  400. Cubre: RF-04
- **`CambiarEstadoCategoria` / `EliminarCategoria`** (Application + WebApi): 404 "Categoría no
  encontrada" si no existe en el contexto (filtro global de restaurante + filtro de no eliminadas);
  eliminar → 204. Cubre: RF-06, RF-07, RF-08
- **`CategoriasController`** (WebApi): `[RequiereRol(Rol.AdministradorGeneral)]` en la clase; cuerpos
  con propiedades anulables para que FluentValidation responda por campo. Cubre: RF-09
- **Pantalla de categorías** (Frontend `menu/CategoriasPagina`, `FormularioCategoria`,
  `VistaPreviaWhatsApp`, `menu/api.ts`, `menu/tituloWhatsApp.ts`): tabla con columnas Orden, Nombre,
  Título en WhatsApp, Estado y Acciones (Editar, Activar/Desactivar, Eliminar); paginación; formulario
  con vista previa en vivo y validación del orden en el cliente; `DialogoConfirmacion` para eliminar.
  Cubre: RF-10, RF-11, RF-12, RF-13
- **Navegación** (Frontend `InicioRestaurante`, `App.tsx`): enlace "Menú" para el administrador
  general; ruta protegida con `redirigirSinRol` al inicio. Cubre: RF-14

## Datos

- `categorias (id uuid PK, restaurante_id uuid not null FK restaurantes, nombre varchar(60) not null,
  nombre_normalizado varchar(60) not null, nombre_corto varchar(24) null, nombre_corto_normalizado
  varchar(24) null, orden int not null check (orden >= 1), activa bool not null, creada_en timestamptz
  not null, eliminada_en timestamptz null)`.
- Índices únicos parciales: `(restaurante_id, nombre_normalizado) where eliminada_en is null` y
  `(restaurante_id, nombre_corto_normalizado) where eliminada_en is null and nombre_corto is not null`.
  Índice `(restaurante_id, orden, nombre)` para la lista.
- Filtro global: restaurante del contexto (convención de 003) **y** `eliminada_en is null`, combinados
  en un solo filtro de la entidad.
- Migración `Categorias`, aditiva.

## Manejo de errores

- 400 `ValidationProblemDetails`: `nombre`, `nombreCorto`, `orden`, `pagina`, `tamano` y `activa`
  (ausente o no booleano en el cambio de estado).
- 403 "Acceso denegado": rol distinto de administrador general.
- 404 "Categoría no encontrada": id inexistente, eliminado o de otro restaurante (nunca 403).
- 409 "Conflicto" con `errors` por campo: nombre o nombre corto repetidos (ambos si chocan los dos).
- Frontend: errores por campo en el formulario; orden inválido se detiene en el cliente; 403 →
  "No tienes permiso para esta acción."

## Estrategia de pruebas

- **Domain (xUnit)**: formatos de `Categoria` (`005/RF-02`), eliminar marca la fecha (`005/RF-07`),
  `TituloWhatsApp` en tabla de casos: con nombre corto, nombre de 24, de 25 y de 30 sin nombre corto
  (`005/RF-11`).
- **Application (xUnit)**: handlers con dobles en memoria, incluida la unicidad sin mayúsculas y la
  liberación del nombre tras eliminar (`005/RF-03`, `RF-07`).
- **WebApi (xUnit + `FabricaApi`)**: 201/200/204/400/403/404/409 por endpoint; `orden` decimal, 0,
  negativo y texto → 400 en `orden`; paginación y orden de la lista; aislamiento A/B (RNF-01).
- **Integración (PostgreSQL)**: índices únicos parciales (repetido → violación; eliminado libera) y
  `check (orden >= 1)`.
- **Frontend (Vitest)**: `tituloWhatsApp` con los mismos casos que el dominio (RNF-02); formulario
  (vista previa, aviso, validación del orden, errores 400/409).
- **Playwright simulada (`apps/web/e2e/`, 1440 y 390)**: lista y paginación, alta con vista previa,
  edición, activar/desactivar, eliminar con confirmación, enlace "Menú" y redirección de otros roles
  (`005/RF-10`–`RF-14`).
- **Playwright e2e-stack**: el administrador general crea dos categorías, las reordena, elimina una y
  crea otra con el mismo nombre.
- Todos los nombres llevan el prefijo `005/RF-NN:`.
