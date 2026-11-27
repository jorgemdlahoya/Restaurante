---
feature: 006-gestion-de-platillos-del-menu-hu-2
estado: borrador
---
# Diseño: Gestión de platillos del menú (HU-2)

## Enfoque elegido y alternativas descartadas

- **Elegido (decisión del usuario, enfoque A): platillos dentro de cada categoría, con los
  ingredientes en una columna de lista.** La entidad `Platillo` sigue el patrón de `Categoria`
  (005): pertenece al restaurante (`IEntidadDeRestaurante`), tiene borrado lógico (`IEliminable`)
  y devuelve un 409 por campo. Los ingredientes se guardan como `text[]` en la misma fila, en el
  orden en que se escriben. La API va bajo `/categorias/{id}/platillos` (alta y lista) y
  `/platillos/{id}` (detalle, edición, estado y eliminación), como pide el backlog. La pantalla es
  `/menu/categorias/{id}/platillos`.
- **Descartado (B): ingredientes en una tabla aparte.** Sólo serviría si se ligaran a modificadores
  u otras reglas, y el backlog lo descarta (P-34). Cuesta más código y pruebas para ordenar.
- **Descartado (C): una sola pantalla con todos los platillos y filtro.** El backlog pide crearlos
  "desde una categoría", y el orden numérico sólo tiene sentido dentro de cada una.
- **Decisiones del usuario:**
  - Orden numérico dentro de la categoría; empate → por nombre.
  - Una categoría con platillos no eliminados (activos o inactivos) no se puede eliminar: 409 con
    "Esta categoría tiene N platillos" y el botón "Desactivar categoría".
- **Llamadas del agente:**
  - Nombre y nombre corto únicos en todo el restaurante, para que la cocina y el ticket no vean dos
    platillos con el mismo nombre; normalización `Trim().ToLowerInvariant()` como en 005.
  - Descripción ≤ 300 caracteres. Ingredientes: hasta 30, cada uno de 1 a 40 caracteres tras
    recortar, sin repetidos sin distinguir mayúsculas; se guardan recortados y en su orden.
  - Desactivar una categoría no toca sus platillos: dejan de verse (RN3.5, en WEB-01/US-01) y vuelven
    al reactivarla.
  - Al editar se puede cambiar de categoría, pero sólo a una activa del restaurante. Un platillo cuya
    categoría está inactiva se puede seguir editando sin moverlo.
  - Categoría inactiva al crear → 400 en `categoriaId` (el backlog dice 400; no es un conflicto con
    otro registro). Categoría inexistente en la ruta → 404; en el cuerpo de la edición → 400 en
    `categoriaId`, porque el recurso de la ruta (el platillo) sí existe.
  - El borrado lógico usa la misma interfaz `IEliminable` y su propiedad `EliminadaEn` (columna
    `eliminada_en`), para que el filtro global de 005 lo cubra sin cambios.
  - RN2.3 se aplaza a la HU-3 y la HU-4, igual que se aplazó RN1.2 en 005.
  - Estado del platillo en masculino: `activo` en la API y "Activo"/"Inactivo" en pantalla.

## Arquitectura

Sobre la Clean Architecture de 001–005:

- **Domain**: `Platillo` (agregado, `IEntidadDeRestaurante`, `IEliminable`) e `IngredientesPlatillo`
  (normaliza y valida la lista).
- **Application**: casos de uso `CrearPlatillo`, `ListarPlatillos`, `ObtenerPlatillo`,
  `EditarPlatillo`, `CambiarEstadoPlatillo` y `EliminarPlatillo`; validador común; `IPlatilloRepository`;
  `EliminarCategoria` (005) pasa a contar los platillos antes de eliminar.
- **Infrastructure**: tabla `platillos`, configuración EF, repositorio y migración `Platillos`.
- **WebApi**: `PlatillosController` con `[RequiereRol(Rol.AdministradorGeneral)]` y las dos rutas
  base.
- **Frontend**: `apps/web/src/menu/` (página de platillos, formulario, editor de ingredientes, API)
  reutilizando `VistaPreviaWhatsApp`, `tituloWhatsApp` y `mensajeError`; la pantalla de categorías
  añade la acción "Platillos" y el aviso de categoría con platillos.

## Componentes

- **`Platillo`** (Domain): `Id`, `RestauranteId`, `CategoriaId`, `Nombre`, `NombreNormalizado`,
  `NombreCorto?`, `NombreCortoNormalizado?`, `Descripcion?`, `Orden`, `Ingredientes`
  (lista de solo lectura), `Activo`, `CreadoEn`, `EliminadaEn?`. Métodos `Crear(restauranteId,
  categoriaId, datos, ahora)`, `Editar(categoriaId, datos)`, `Activar()`, `Desactivar()` y
  `Eliminar(ahora)`. Recorta espacios, convierte el nombre corto o la descripción vacíos en ausentes
  y lanza `ArgumentException` si un formato no se cumple. Cubre: RF-01, RF-02, RF-07, RF-08, RF-09
- **`IngredientesPlatillo.Normalizar(lista)`** (Domain, puro): recorta cada uno, conserva el orden y
  rechaza vacíos, más de 40 caracteres, más de 30 elementos y repetidos sin distinguir mayúsculas.
  Cubre: RF-02
- **Validador de platillo** (Application): común a crear y editar, con los mensajes por campo de
  RF-02. `orden` llega como número JSON estricto (`JsonNumberHandling.Strict`, como en 005). Los
  errores de ingredientes van en la clave `ingredientes` con el número del elemento en el mensaje
  (p. ej. "El ingrediente 3 está repetido"). Cubre: RF-02
- **`CrearPlatillo`** (Application + WebApi): carga la categoría de la ruta (404 si no está en el
  contexto; 400 en `categoriaId` si está inactiva), comprueba la unicidad (409 con `errors` por campo,
  reutilizando `ConflictoCamposException`) y crea el platillo. Cubre: RF-01, RF-03, RF-04
- **`ListarPlatillos`** (Application + WebApi): 404 si la categoría no está en el contexto; paginado
  en la consulta por `Orden` y `Nombre`, con el desplazamiento seguro de 005; respuesta
  `{ elementos, total, pagina, tamano }`. Cubre: RF-05
- **`ObtenerCategoria`** (Application + WebApi, ruta de 005): `GET /categorias/{id}` devuelve
  `CategoriaDto` o 404 "Categoría no encontrada". La pantalla de platillos lo usa para el título, el
  aviso de categoría inactiva y el caso "Categoría no encontrada" (llamada del agente: endpoint
  propio en vez de añadir la categoría a la respuesta de la lista). Cubre: RF-05, RF-13, RF-18
- **`ObtenerPlatillo`** (Application + WebApi): detalle con `categoriaId` y `categoriaNombre`.
  Cubre: RF-06, RF-10
- **`EditarPlatillo`** (Application + WebApi): orden de errores 400 (validador) → 404 platillo →
  400 categoría destino (inexistente o inactiva, salvo que sea la actual) → 409 (excluyendo su id).
  Cubre: RF-07, RF-10
- **`CambiarEstadoPlatillo` / `EliminarPlatillo`** (Application + WebApi): 404 "Platillo no
  encontrado"; eliminar → 204. Cubre: RF-08, RF-09, RF-10
- **`PlatillosController`** (WebApi): `[RequiereRol(Rol.AdministradorGeneral)]` en la clase; cuerpos
  con propiedades anulables para que FluentValidation responda por campo. Cubre: RF-11
- **`EliminarCategoria` ampliado** (Application, 005): antes de marcarla, cuenta los platillos no
  eliminados de la categoría con `IPlatilloRepository.ContarPorCategoriaAsync`; si hay alguno, lanza
  una excepción de conflicto que el filtro traduce a 409 "Conflicto" con el detalle de RF-12 y la
  extensión `platillos: N`. Cubre: RF-12
- **Pantalla de platillos** (Frontend `menu/PlatillosPagina`, `FormularioPlatillo`,
  `EditorIngredientes`, `menu/apiPlatillos.ts`): tabla con columnas Orden, Nombre, Título en WhatsApp,
  Ingredientes (cuántos), Estado y Acciones (Editar, Activar/Desactivar, Eliminar); paginación; aviso
  de categoría inactiva. El formulario reutiliza `VistaPreviaWhatsApp` y, en edición, ofrece un
  selector de categoría con las activas más la actual del platillo (con "(inactiva)" si lo está,
  para poder guardar sin moverlo). Para el selector, la lista de categorías se pide con
  `tamano=100` y se filtra a las activas: llamada del agente, suficiente mientras un restaurante
  tenga menos de 100 categorías. Cubre: RF-13, RF-14, RF-15, RF-16
- **Aviso de categoría con platillos** (Frontend `CategoriasPagina`): al recibir 409 con `platillos`
  en la eliminación, muestra el diálogo de RF-17; "Desactivar categoría" llama al cambio de estado de
  005 y sólo aparece si la categoría está activa. Cubre: RF-17
- **Navegación** (Frontend `CategoriasPagina`, `App.tsx`): acción "Platillos" por fila; ruta hija de
  `/r/:slug` protegida con `redirigirSinRol` al inicio; una categoría inexistente (404) muestra
  "Categoría no encontrada" con el enlace "Categorías". Cubre: RF-13, RF-18

## Datos

- `platillos (id uuid PK, restaurante_id uuid not null FK restaurantes, categoria_id uuid not null
  FK categorias, nombre varchar(60) not null, nombre_normalizado varchar(60) not null, nombre_corto
  varchar(24) null, nombre_corto_normalizado varchar(24) null, descripcion varchar(300) null,
  orden int not null check (orden >= 1), ingredientes text[] not null default '{}', activo bool
  not null, creado_en timestamptz not null, eliminada_en timestamptz null)`.
- Índices únicos parciales: `(restaurante_id, nombre_normalizado) where eliminada_en is null` y
  `(restaurante_id, nombre_corto_normalizado) where eliminada_en is null and nombre_corto is not
  null`. Índice `(categoria_id, orden, nombre)` para la lista.
- Filtro global por convención de 003/005 (restaurante + no eliminados): no cambia, `Platillo` lo
  recibe por implementar las dos interfaces.
- Migración `Platillos`, aditiva.

## Manejo de errores

- 400 `ValidationProblemDetails`: `nombre`, `nombreCorto`, `descripcion`, `orden`, `ingredientes`,
  `categoriaId` (inactiva o inexistente en la edición), `pagina`, `tamano` y `activo`.
- 403 "Acceso denegado": rol distinto de administrador general.
- 404 "Categoría no encontrada" (ruta de alta y lista) y "Platillo no encontrado" (detalle, edición,
  estado y eliminación); nunca 403 por otro restaurante.
- 409 "Conflicto" con `errors` por campo: nombre o nombre corto repetidos.
- 409 "Conflicto" al eliminar una categoría con platillos: detalle "Esta categoría tiene N platillos"
  y extensión `platillos`.
- Frontend: errores por campo en el formulario; orden inválido se detiene en el cliente; 403 →
  "No tienes permiso para esta acción." (`mensajeError` de 005).

## Estrategia de pruebas

- **Domain (xUnit)**: formatos de `Platillo` e `IngredientesPlatillo` (vacío, 41 caracteres, 31
  elementos, repetidos con otra capitalización, orden conservado) (`006/RF-02`); eliminar marca la
  fecha (`006/RF-09`).
- **Application (xUnit)**: handlers con dobles en memoria: categoría inactiva o de otro restaurante,
  unicidad sin mayúsculas, cambio de categoría, liberación del nombre tras eliminar y conteo en la
  eliminación de categoría (`006/RF-03`, `RF-04`, `RF-07`, `RF-09`, `RF-12`).
- **WebApi (xUnit + `FabricaApi`)**: 201/200/204/400/403/404/409 por endpoint, paginación y orden,
  aislamiento A/B (RNF-01), y eliminar categoría con platillos → 409 y sin platillos → 204.
- **Integración (PostgreSQL)**: `text[]` conserva el orden; índices únicos parciales; filtro global
  sobre `platillos`; `check (orden >= 1)`; conteo de platillos por categoría.
- **Frontend (Vitest)**: editor de ingredientes (agregar con Enter y botón, quitar, vacío y
  repetido), formulario (vista previa, orden, errores 400/409, selector de categorías activas).
- **Playwright simulada (`apps/web/e2e/`, 1440 y 390)**: acción "Platillos", lista y paginación,
  aviso de categoría inactiva, alta con ingredientes, edición con cambio de categoría,
  activar/desactivar, eliminar con confirmación, aviso de categoría con platillos con "Desactivar
  categoría", redirección de otros roles y categoría inexistente (`006/RF-13`–`RF-18`).
- **Playwright e2e-stack**: el administrador general crea una categoría, le agrega un platillo con
  ingredientes, intenta eliminar la categoría (aviso), elimina el platillo y después la categoría.
- Todos los nombres llevan el prefijo `006/RF-NN:`.
