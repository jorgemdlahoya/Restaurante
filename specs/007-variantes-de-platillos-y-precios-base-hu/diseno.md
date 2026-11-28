---
feature: 007-variantes-de-platillos-y-precios-base-hu
estado: borrador
---
# Diseño: Variantes de platillos y precios base (HU-3)

## Enfoque elegido y alternativas descartadas

- **Elegido (decisión del usuario, enfoque A): variantes como entidad propia, con su pantalla por
  platillo.** `Variante` pertenece al restaurante (`IEntidadDeRestaurante`), tiene borrado lógico
  (`IEliminable`) y devuelve un 409 por campo, como categorías y platillos. API del backlog:
  `GET`/`POST /platillos/{id}/variantes`, `PUT`/`estado`/`DELETE /variantes/{id}`. Pantalla
  `/menu/platillos/{id}/variantes`, abierta con la acción "Variantes" de cada platillo.
- **Descartado (B): variantes dentro del formulario del platillo.** Guardar exigiría comparar listas
  y mezclaría los conflictos de variantes con los del platillo; la HU-4 necesita que cada variante
  tenga su propia identidad.
- **Descartado (C): variantes desplegadas en la tabla de platillos.** Una tabla dentro de otra es
  difícil de usar y de probar a 390 px.
- **Decisiones del usuario:**
  - El alta de platillo pide el precio y crea la variante "Regular" con él.
  - Los platillos que ya existen reciben, en la migración, una "Regular" inactiva con $0.00.
  - Las variantes se ordenan por precio y después por nombre (sin campo de orden).
- **Llamadas del agente:**
  - Precio de 0.00 a 99,999.99 con hasta 2 decimales, IVA incluido; número JSON estricto
    (`JsonNumberHandling.Strict`, como `orden` en 005/006); `decimal` en C#, `numeric(10,2)` en
    PostgreSQL. Un precio con más de 2 decimales se rechaza, no se redondea.
  - SKU opcional, `^[A-Za-z0-9_-]{1,40}$` tras recortar, único en el restaurante sin distinguir
    mayúsculas (se guarda tal cual y se compara por su forma en mayúsculas).
  - Nombre de variante único dentro del platillo, con la normalización de 005/006
    (`Trim().ToLowerInvariant()`).
  - Se puede desactivar la última variante activa; la pantalla lo avisa. No se puede eliminar la
    última no eliminada (409).
  - Eliminar un platillo elimina también sus variantes (borrado lógico), para liberar sus SKU.
  - La lista de variantes no se pagina: un platillo tiene pocas.
  - El estado de la variante es `activa` (femenino), como `Categoria`.
  - RN2.3 queda como función pura del dominio (`VariantesALaVenta`) con sus pruebas; la usarán
    WEB-01 y US-01.

## Arquitectura

Sobre la Clean Architecture de 001–006:

- **Domain**: `Variante` (agregado, `IEntidadDeRestaurante`, `IEliminable`), `PrecioVariante`
  (validación del precio) y `VariantesALaVenta` (función pura de RF-14).
- **Application**: casos de uso `CrearVariante`, `ListarVariantes`, `EditarVariante`,
  `CambiarEstadoVariante` y `EliminarVariante`; validador común; `IVarianteRepository`. Cambian dos
  de 006: `CrearPlatillo` (exige `precio` y crea la "Regular") y `EliminarPlatillo` (elimina también
  las variantes).
- **Infrastructure**: tabla `variantes`, configuración EF, repositorio y migración `Variantes` (con
  el relleno de las "Regular").
- **WebApi**: `VariantesController` con `[RequiereRol(Rol.AdministradorGeneral)]`.
- **Frontend**: `apps/web/src/menu/` (página de variantes, formulario, API, formato de precio); el
  formulario de alta de platillo añade "Precio" y la tabla de platillos la acción "Variantes".

## Componentes

- **`Variante`** (Domain): `Id`, `RestauranteId`, `PlatilloId`, `Nombre`, `NombreNormalizado`,
  `Precio` (`decimal`), `Sku?`, `SkuNormalizado?`, `Activa`, `CreadaEn`, `EliminadaEn?`. Métodos
  `Crear(restauranteId, platilloId, datos, ahora)`, `Regular(restauranteId, platilloId, precio, ahora)`,
  `Editar(datos)`, `Activar()`, `Desactivar()` y `Eliminar(ahora)`. Recorta, convierte el SKU vacío en
  ausente y lanza `ArgumentException` si un formato no se cumple. Cubre: RF-01, RF-02, RF-03, RF-07,
  RF-08, RF-09
- **`PrecioVariante`** (Domain, puro): `Errores(decimal? precio)` con los mensajes de RF-03 (falta,
  negativo, mayor que 99,999.99, más de 2 decimales). Única fuente de la regla para el dominio y los
  validadores de variante y de alta de platillo. Cubre: RF-01, RF-03
- **`VariantesALaVenta.De(categoria, platillo, variantes)`** (Domain, puro): devuelve las variantes
  activas y no eliminadas, o ninguna si el platillo está inactivo o eliminado o la categoría
  inactiva. Cubre: RF-14
- **Validador de variante** (Application): común a crear y editar, con los mensajes por campo de
  RF-03. Un precio como texto o con formato no numérico llega como 400 en `precio` por el enlace del
  modelo (JSON estricto). Cubre: RF-03
- **`CrearVariante`** (Application + WebApi): carga el platillo de la ruta (404 "Platillo no
  encontrado" si no está en el contexto), comprueba unicidad de nombre en el platillo y de SKU en el
  restaurante (409 con `errors` por campo, `ConflictoCamposException`) y crea la variante.
  Cubre: RF-02, RF-04, RF-06
- **`ListarVariantes`** (Application + WebApi): 404 si el platillo no está en el contexto; lista
  completa ordenada por `Precio` y `Nombre`. Cubre: RF-05, RF-06
- **`EditarVariante`** (Application + WebApi): 400 (validador) → 404 "Variante no encontrada" (también
  si su platillo está eliminado) → 409 (excluyendo su id). Cubre: RF-07, RF-11
- **`CambiarEstadoVariante`** (Application + WebApi): 404 como arriba; sin restricción por ser la
  última activa. Cubre: RF-08, RF-11
- **`EliminarVariante`** (Application + WebApi): 404 como arriba; si es la única no eliminada del
  platillo lanza un conflicto que el filtro traduce a 409 "Conflicto" con el detalle de RF-10;
  si no, la marca. Cubre: RF-09, RF-10, RF-11
- **`VariantesController`** (WebApi): `[RequiereRol(Rol.AdministradorGeneral)]` en la clase; cuerpos
  con propiedades anulables y `precio` con `JsonNumberHandling.Strict`. Cubre: RF-12
- **`CrearPlatillo` ampliado** (Application + WebApi, 006): el cuerpo añade `precio`, validado con
  `PrecioVariante`; el handler agrega la "Regular" y guarda platillo y variante en una sola unidad de
  trabajo (mismo `DbContext`). La respuesta de 006 no cambia. Cubre: RF-01
- **`EliminarPlatillo` ampliado** (Application, 006): marca como eliminadas las variantes no
  eliminadas del platillo en el mismo guardado. Cubre: RF-13
- **Pantalla de variantes** (Frontend `menu/VariantesPagina`, `FormularioVariante`,
  `menu/apiVariantes.ts`, `menu/precio.ts`): tabla con columnas Nombre, Precio, SKU, Estado y
  Acciones (Editar, Activar/Desactivar, Eliminar); "Nueva variante"; avisos de RF-16; formulario con
  contador "N/24", precio validado en el cliente y errores por campo; confirmación de eliminación;
  un 409 de la única variante como aviso general. `precio.ts` formatea con
  `Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })` y valida el texto escrito.
  El enlace "Platillos de {categoría}" usa `GET /platillos/{id}` (006). Cubre: RF-16, RF-17, RF-18
- **Precio en el alta de platillo** (Frontend `FormularioPlatillo`, 006): campo "Precio" sólo en alta,
  con la validación de `precio.ts`; el cuerpo del POST incluye `precio`. Cubre: RF-15
- **Navegación** (Frontend `PlatillosPagina`, `App.tsx`): acción "Variantes" por fila; ruta hija de
  `/r/:slug` protegida con `redirigirSinRol` al inicio; un 404 del platillo muestra "Platillo no
  encontrado" con el enlace "Categorías". Cubre: RF-16, RF-19

## Datos

- `variantes (id uuid PK, restaurante_id uuid not null FK restaurantes, platillo_id uuid not null FK
  platillos, nombre varchar(24) not null, nombre_normalizado varchar(24) not null, precio
  numeric(10,2) not null check (precio >= 0 and precio <= 99999.99), sku varchar(40) null,
  sku_normalizado varchar(40) null, activa bool not null, creada_en timestamptz not null,
  eliminada_en timestamptz null)`.
- Índices únicos parciales: `(platillo_id, nombre_normalizado) where eliminada_en is null` y
  `(restaurante_id, sku_normalizado) where eliminada_en is null and sku is not null`. Índice
  `(platillo_id, precio, nombre)` para la lista.
- Filtro global por convención de 003/005 (restaurante + no eliminadas): no cambia.
- Migración `Variantes`, aditiva: crea la tabla y, con SQL en la propia migración, inserta para cada
  platillo con `eliminada_en is null` una fila `nombre = 'Regular'`, `precio = 0.00`,
  `activa = false`, `sku = null`, `creada_en = now()` e `id = gen_random_uuid()`.

## Manejo de errores

- 400 `ValidationProblemDetails`: `nombre`, `precio`, `sku`, `activa`; y `precio` en el alta de
  platillo.
- 403 "Acceso denegado": rol distinto de administrador general.
- 404 "Platillo no encontrado" (alta y lista de variantes) y "Variante no encontrada" (edición,
  estado y eliminación); nunca 403 por otro restaurante.
- 409 "Conflicto" con `errors` por campo: `nombre` ("Ya existe una variante con ese nombre en este
  platillo") y `sku` ("Ya existe una variante con ese SKU").
- 409 "Conflicto" sin `errors` al eliminar la única variante: "No se puede eliminar la única variante
  del platillo".
- Frontend: errores por campo en el formulario; precio inválido se detiene en el cliente; un 409 sin
  campo se muestra como aviso general; 403 → "No tienes permiso para esta acción." (`mensajeError`).

## Estrategia de pruebas

- **Domain (xUnit)**: formatos de `Variante` y `PrecioVariante` (0, 99,999.99, -0.01, 100,000, 1.005,
  SKU con espacio o de 41 caracteres) (`007/RF-03`); "Regular" (`007/RF-01`); eliminar marca la fecha
  (`007/RF-09`); `VariantesALaVenta` en tabla de casos (`007/RF-14`).
- **Application (xUnit)**: handlers con dobles en memoria: unicidad de nombre por platillo y de SKU
  por restaurante, la única variante, el alta de platillo con "Regular" y la eliminación en cascada
  (`007/RF-01`, `RF-04`, `RF-10`, `RF-13`).
- **WebApi (xUnit + `FabricaApi`)**: 201/200/204/400/403/404/409 por endpoint, orden por precio,
  `precio` como texto o con 3 decimales → 400, aislamiento A/B (RNF-01), y las pruebas de alta de
  platillo de 006 actualizadas con `precio`.
- **Integración (PostgreSQL)**: `numeric(10,2)` conserva 45.50 exacto; índices únicos parciales;
  filtro global sobre `variantes`; el relleno de la migración (una "Regular" inactiva por platillo
  existente y ninguna para los eliminados).
- **Frontend (Vitest)**: `precio.ts` (formato y validación), formulario de variante (contador,
  precio, errores 400/409, aviso de la única variante) y el campo "Precio" del alta de platillo.
- **Playwright simulada (`apps/web/e2e/`, 1440 y 390)**: acción "Variantes", lista ordenada por
  precio, avisos, alta y edición con precio, activar/desactivar, eliminar con confirmación, el 409
  de la única variante, el precio en el alta de platillo, redirección de otros roles y platillo
  inexistente (`007/RF-15`–`RF-19`). Las pruebas de alta de platillo de 006 se actualizan con el
  campo "Precio".
- **Playwright e2e-stack**: el administrador general crea un platillo con precio, ve su "Regular",
  agrega "Grande", intenta eliminar la última tras eliminar una y ve el aviso; la prueba de
  platillos de 006 se actualiza con el precio.
- Todos los nombres llevan el prefijo `007/RF-NN:`.
