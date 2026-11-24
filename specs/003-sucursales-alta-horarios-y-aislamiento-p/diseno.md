---
feature: 003-sucursales-alta-horarios-y-aislamiento-p
estado: borrador
---
# Diseño: Sucursales: alta, horarios y aislamiento por sucursal (HT-0, SUC-01)

## Enfoque elegido y alternativas descartadas

- **Elegido: sucursal en la ruta + filtro global genérico.** Las operaciones de una sucursal viven
  bajo `/api/r/{slug}/s/{sucursal}/…`; el middleware de 002 se amplía para resolver la sucursal
  dentro del restaurante del contexto y fijar `IContextoSucursal`. Toda entidad que implemente
  `IEntidadDeSucursal` recibe un filtro global de EF Core por restaurante **y** sucursal, aplicado
  por convención en `OnModelCreating`, de modo que una entidad nueva queda aislada sin código
  extra. Sin sucursal en el contexto, esas entidades no devuelven nada (seguro por defecto).
- **Descartado: sucursal en una cabecera (`X-Sucursal`).** Invisible en la URL, fácil de olvidar
  en el cliente y difícil de documentar en los `.http`.
- **Descartado: filtrar a mano en cada repositorio.** Repite la regla y deja huecos; contradice el
  Art. 12.3 (filtros globales).
- **Alcance (llamada del agente):** sólo sucursal de HT-0; el menú es otro subsistema y la
  constitución lo declara global al restaurante. Las reglas que dependen de pedidos, WhatsApp o
  asignación de usuarios se dejan para sus features (ver requerimiento).
- `slug` de sucursal inmutable, igual que el del restaurante (es parte de direcciones públicas).

## Arquitectura

Sobre la Clean Architecture de 001/002:

- **Domain**: `Sucursal` (agregado) con `HorarioSemanal` (value object de 7 `HorarioDia`),
  `CierreTemporal` (entidad de sucursal) y `ZonasHorarias` (lista admitida). Interfaces de marca
  `IEntidadDeRestaurante` (`RestauranteId`) e `IEntidadDeSucursal` (`RestauranteId`, `SucursalId`).
- **Application**: casos de uso de sucursales, horario, cierres y estado de atención;
  `IContextoSucursal`; servicio puro `CalculadoraEstadoAtencion`.
- **Infrastructure**: tablas `sucursales`, `horarios_sucursal`, `cierres_temporales`; filtros
  globales por convención; repositorios; migración `Sucursales`.
- **WebApi**: `SucursalesController` (`/api/r/{slug}/sucursales`) y `SucursalController`
  (`/api/r/{slug}/s/{sucursal}/…`); el middleware resuelve la sucursal.
- **Frontend**: pantallas en `apps/web/src/sucursales/` dentro del diseño del restaurante.

## Componentes

- **`Sucursal`** (Domain): nombre, dirección, teléfono, `slug` (inmutable), WhatsApp,
  `PhoneNumberId?`, prefijo, zona horaria, activa, `RestauranteId`; `Crear`, `Editar`,
  `Activar`, `Desactivar`, `CambiarHorario`. Al crearse, los siete días quedan cerrados.
  Cubre: RF-01, RF-05, RF-06
- **Validación de sucursal** (Application): validator común a crear y editar con los formatos de
  RF-02 (`+52` y 10 dígitos, normalizado a E.164; prefijo `^[A-Z]{1,3}$` anclado con `\z`; zonas
  de RNF-01). Cubre: RF-02
- **Crear, listar, editar y cambiar estado** (Application + WebApi): `CrearSucursal`,
  `ListarSucursales`, `EditarSucursal`, `CambiarEstadoSucursal` bajo `/api/r/{slug}/sucursales`
  con `[RequiereRol(Rol.AdministradorGeneral)]`; unicidades comprobadas en el handler y por índices
  únicos (carreras → 409). Cubre: RF-01, RF-03, RF-04, RF-05, RF-06, RF-07
- **Horario** (Domain + Application): `HorarioDia(DiaSemana, Cerrado, Apertura?, Cierre?)`;
  `GuardarHorario` y `ObtenerHorario` en `/api/r/{slug}/s/{sucursal}/horario`; reemplazo completo
  de los 7 días; cierre < apertura = turno que cruza la medianoche. Cubre: RF-08, RF-09
- **Cierres temporales** (Domain + Application): `CierreTemporal(Inicio, Fin, Motivo?)` en UTC;
  `CrearCierre`, `ListarCierres` (vigentes y futuros), `CancelarCierre` en
  `/api/r/{slug}/s/{sucursal}/cierres`. Cancelar no borra (Art. 13): fija `Fin` en el instante
  actual (o `Inicio = Fin` si aún no había empezado) y marca `CanceladoEn`, así deja de aplicar de
  inmediato y se conserva el histórico. Cubre: RF-10, RF-11
- **`CalculadoraEstadoAtencion`** (Application, pura): dado un instante UTC, la zona, el horario y
  los cierres, decide abierta/cerrada y busca la próxima apertura en 14 días (considerando turnos
  que cruzan la medianoche y cierres que tapan aperturas); arma el mensaje con "hoy", "mañana" o
  el nombre del día en español. Cubre: RF-12, RF-13
- **Estado público** (Application + WebApi): `ObtenerEstadoAtencion` en
  `GET /api/r/{slug}/s/{sucursal}/estado`, `[EndpointAnonimo]`, que usa `IReloj`; sucursal
  inexistente o inactiva → 404 "Sucursal no disponible". Cubre: RF-12, RF-13, RF-14
- **Contexto de sucursal** (WebApi): el middleware, tras fijar el restaurante, reconoce
  `^/api/r/[^/]+/s/([^/]+)`, busca la sucursal por (restaurante, `slug` en minúsculas) y fija
  `IContextoSucursal`; si no existe → 404 "Sucursal no disponible". Las sucursales inactivas se
  resuelven igual (el administrador puede editar su horario); el estado público las rechaza.
  Cubre: RF-14, RF-15
- **Filtros globales por convención** (Infrastructure): en `OnModelCreating`, para cada tipo que
  implemente `IEntidadDeRestaurante` se añade `RestauranteId == RestauranteActualId`, y para
  `IEntidadDeSucursal` además `SucursalId == SucursalActualId` (propiedades de instancia del
  `DbContext`, parametrizadas por EF). `Sucursal` es entidad de restaurante; `CierreTemporal` y
  la fila de horario son entidades de sucursal. Cubre: RF-15, RF-16
- **Pantallas de sucursales** (frontend): `/r/{slug}/sucursales` (tabla con estado, formulario de
  alta y edición, desactivar con diálogo de confirmación) y `/r/{slug}/sucursales/{sucursal}`
  (indicador de estado, editor de 7 días con casilla "Cerrado" y dos campos de hora, cierres
  temporales). Reutilizan `DialogoConfirmacion`, el patrón de `FormularioRestaurante` y
  `RutaProtegida`. Cubre: RF-17, RF-18
- **Selector de sucursal** (frontend): en `EncabezadoMarca`, para `administrador_general`, lista
  las sucursales activas; la elegida se guarda en la sesión (`sucursalActiva` en el almacén de
  002) y navega a su detalle. Cubre: RF-19
- **Acceso desde inicio y protección por rol** (frontend): enlace "Sucursales" en
  `InicioRestaurante` sólo para `administrador_general`; `RutaProtegida` gana una ruta de
  redirección para "rol no permitido con sesión válida" que lleva a `/r/{slug}/inicio`.
  Cubre: RF-20

## Datos

| Tabla | Columnas |
|---|---|
| `sucursales` | `id` uuid, `restaurante_id` uuid FK, `nombre` varchar(80), `direccion` varchar(200), `telefono` varchar(13), `slug` varchar(40), `whatsapp` varchar(13), `phone_number_id` varchar(30) nulo, `prefijo` varchar(3), `zona_horaria` varchar(40), `activa` bool, `creada_en` timestamptz. Únicos: (`restaurante_id`, `nombre`), (`restaurante_id`, `slug`), (`restaurante_id`, `prefijo`), `whatsapp`, `phone_number_id` (si no es nulo) |
| `horarios_sucursal` | PK (`sucursal_id`, `dia_semana` smallint 0–6, lunes = 0), `restaurante_id`, `cerrado` bool, `apertura` time nulo, `cierre` time nulo |
| `cierres_temporales` | `id` uuid, `restaurante_id`, `sucursal_id` FK, `inicio` timestamptz, `fin` timestamptz, `motivo` varchar(120) nulo, `creado_en` timestamptz, `cancelado_en` timestamptz nulo; índice (`sucursal_id`, `fin`) |

Migración `Sucursales` (aditiva; no toca datos de 002). La imagen de la API debe incluir datos de
zonas horarias (`tzdata`) para resolver los identificadores IANA; se verifica en una prueba de
integración.

## Manejo de errores

- Validación → 400 por campo (formatos de 001/002). Datos repetidos → 409 con el dato en el detalle
  (p. ej. "Ya existe una sucursal con ese prefijo").
- Sucursal inexistente en la ruta, o inactiva en el estado público → 404 "Sucursal no disponible".
- Datos de otra sucursal u otro restaurante → 404 (nunca 403).
- Rol distinto de `administrador_general` → 403 (incluido `administrador_plataforma`).
- Zona horaria no resoluble en el servidor → 500 genérico registrado en el log (no debería ocurrir:
  la lista es cerrada y se prueba).

## Estrategia de pruebas

- **Domain** (xUnit, `003/RF-NN`): `Sucursal` (alta con días cerrados, slug inmutable, activar y
  desactivar), `HorarioDia` (cruce de medianoche, apertura = cierre inválido), `CierreTemporal`.
- **Application**: validadores (formatos, `\z`), handlers con dobles, y la
  `CalculadoraEstadoAtencion` con casos de tabla: abierta, cerrada antes de abrir ("hoy"), después
  de cerrar ("mañana"), fin de semana cerrado (nombre del día), turno que cruza la medianoche,
  cierre temporal vigente que tapa la próxima apertura, sin aperturas en 14 días.
- **WebApi** (`WebApplicationFactory`): CRUD, 400/409, 403 por rol, estado público 200 y 404, y
  las pruebas de aislamiento del Art. 12.5: la sucursal A no ve ni modifica horario ni cierres de
  la B (404) y una sucursal de otro restaurante → 404.
- **Integración** (`Category=Integracion`, PostgreSQL): filtro global por convención (A no ve
  cierres de B, sin contexto no ve ninguno), índices únicos y zonas horarias resolubles.
- **Frontend**: Vitest para el editor de horario y el selector; Playwright (suite simulada,
  `test("003/RF-NN: …")`) para la lista y el formulario, el detalle con horario y cierres, el
  selector y el acceso por rol. Esas tareas llevan `UI: sí`.
- **Suite completa (`e2e-stack`)**: alta de una sucursal desde el backoffice y consulta de su
  estado público.
