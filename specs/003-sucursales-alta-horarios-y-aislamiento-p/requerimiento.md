---
feature: 003-sucursales-alta-horarios-y-aislamiento-p
estado: borrador
---
# Requerimiento: Sucursales: alta, horarios y aislamiento por sucursal (HT-0, SUC-01)

## Contexto y propósito

La constitución organiza la plataforma en Plataforma → Restaurantes → **Sucursales** (Art. 12):
cada restaurante opera una o varias sucursales y toda la información operativa (pedidos,
disponibilidad, impresoras, sesiones del chatbot) se diferencia por sucursal. La feature 002 creó
los restaurantes y el aislamiento por restaurante. Esta feature crea las **sucursales** de cada
restaurante, su **horario de atención** y sus **cierres temporales**, y la base técnica del
**aislamiento por sucursal** sobre la que se apoyarán el menú, los pedidos y los canales.

Implementa del backlog (`docs/backlog/BACKLOG.md`, Art. 14):
- **SUC-01** (gestión de sucursales y horarios): CA1, CA2, CA3, CA5, CA6; reglas RN-S1, RN-S2
  (parte web/API), RN-S4 (cálculo de "abierta/cerrada" y su mensaje) y RN-S5; parte de CA4
  (selector de sucursal del administrador general).
- **HT-0** (base del dominio y aislamiento por sucursal): sólo la parte de **sucursal**: entidad,
  `RestaurantId`/`BranchId` en las entidades de sucursal, filtro global por sucursal y pruebas
  "la sucursal A no ve ni modifica datos de la B" (Art. 12.5).

Lo demás de HT-0 (entidades del menú, `BranchItemAvailability`) y de SUC-01 que depende de
features futuras queda en "Fuera de alcance".

## Actores

- **Administrador general**: gestiona las sucursales de su restaurante, su horario y sus cierres
  temporales, y elige la sucursal activa en el backoffice.
- **Canales y clientes (anónimos)**: consultan si una sucursal está abierta (lo usarán la web y el
  chatbot en features posteriores).
- **Administrador de la plataforma**: no gestiona sucursales (Art. 9).

## Historias de usuario

### HU-01: Gestión de sucursales
Como administrador general quiero dar de alta y administrar las sucursales de mi restaurante para
que cada una opere con sus propios datos.

- **RF-01**: Cuando el administrador general envía nombre, dirección, teléfono, `slug`, número de WhatsApp, `phone_number_id` (opcional), prefijo de número de orden y zona horaria, el sistema deberá crear la sucursal activa en su restaurante, con todos los días cerrados, y responder 201.
- **RF-02**: Si el nombre está vacío o supera 80 caracteres, la dirección está vacía o supera 200, el teléfono o el WhatsApp no son un número mexicano (`+52` seguido de 10 dígitos), el `slug` no cumple `^[a-z0-9-]{3,40}$`, el `phone_number_id` no son sólo dígitos (1–30), el prefijo no cumple `^[A-Z]{1,3}$` o la zona horaria no es una de las admitidas, el sistema deberá responder 400 por campo.
- **RF-03**: Si el nombre, el `slug` o el prefijo ya existen en otra sucursal del mismo restaurante, o el número de WhatsApp o el `phone_number_id` ya existen en cualquier sucursal de la plataforma, el sistema deberá responder 409 indicando el dato repetido.
- **RF-04**: Cuando el administrador general consulta las sucursales, el sistema deberá devolver de cada sucursal de su restaurante su nombre, dirección, `slug`, prefijo y estado (activa o inactiva).
- **RF-05**: Cuando el administrador general edita una sucursal, el sistema deberá guardar los cambios de todos sus datos salvo el `slug`, que no se puede modificar, aplicando las mismas validaciones y unicidades.
- **RF-06**: Cuando el administrador general desactiva una sucursal, el sistema deberá marcarla inactiva sin borrarla; al reactivarla, todos sus datos (horario y cierres) deberán seguir intactos.
- **RF-07**: Si un usuario sin rol `administrador_general` del restaurante intenta gestionar sucursales, su horario o sus cierres, el sistema deberá responder 403.

### HU-02: Horario de atención
Como administrador general quiero configurar el horario semanal de cada sucursal para que los
canales sepan cuándo aceptan pedidos.

- **RF-08**: Cuando el administrador general guarda el horario de una sucursal, el sistema deberá guardar, para cada uno de los siete días, si está cerrado o su hora de apertura y de cierre (`HH:mm`, 24 h), reemplazando el horario anterior completo.
- **RF-09**: Si un día abierto tiene la misma hora de apertura y de cierre, una hora no válida o faltan días, el sistema deberá responder 400; una hora de cierre menor que la de apertura deberá aceptarse y significar que el turno termina al día siguiente.

### HU-03: Cierres temporales
Como administrador general quiero cerrar temporalmente una sucursal (por ejemplo, un día festivo)
para que deje de aceptar pedidos de inmediato.

- **RF-10**: Cuando el administrador general crea un cierre temporal con inicio (por defecto, ahora), fin y motivo opcional (hasta 120 caracteres), el sistema deberá guardarlo y aplicarlo desde su inicio; si el fin no es posterior al inicio o ya pasó, deberá responder 400.
- **RF-11**: Cuando el administrador general consulta o cancela los cierres de una sucursal, el sistema deberá listar los cierres vigentes y futuros y, al cancelar uno, dejar de aplicarlo de inmediato.

### HU-04: Estado de atención
Como canal de pedidos quiero saber si una sucursal está abierta para aceptar o rechazar pedidos.

- **RF-12**: Cuando se consulta sin autenticación el estado de una sucursal activa, el sistema deberá responder si está abierta en ese instante según su zona horaria, su horario semanal y sus cierres temporales.
- **RF-13**: Si la sucursal está cerrada, el sistema deberá devolver el mensaje "Estamos cerrados, abrimos {día} a las {hora}", donde `{día}` es "hoy", "mañana" o el nombre del día de la semana y `{hora}` la próxima apertura en `HH:mm` dentro de los próximos 14 días; si no hay ninguna, el mensaje deberá ser "Estamos cerrados por el momento".
- **RF-14**: Si la sucursal no existe en ese restaurante o está inactiva, el sistema deberá responder 404 con el título "Sucursal no disponible".

### HU-05: Aislamiento por sucursal
Como dueño de un restaurante quiero que los datos de cada sucursal no se mezclen con los de otra.

- **RF-15**: Si desde la dirección de la sucursal A se consulta o modifica un dato de la sucursal B (o de una sucursal de otro restaurante), el sistema deberá responder 404.
- **RF-16**: Cuando se consulta cualquier entidad de sucursal, el sistema deberá devolver sólo las de la sucursal y el restaurante del contexto, y ninguna si no hay sucursal en el contexto.

### HU-06: Sucursales en el backoffice
Como administrador general quiero pantallas para gestionar mis sucursales.

- **RF-17**: Cuando el administrador general entra en `/r/{slug}/sucursales`, el sistema deberá mostrar la lista de sucursales con nombre, dirección y estado, y permitir crearlas, editarlas y activarlas o desactivarlas, pidiendo confirmación para desactivar.
- **RF-18**: Cuando el administrador general abre `/r/{slug}/sucursales/{sucursal}`, el sistema deberá mostrar si está "Abierta ahora" o el mensaje de cerrada, el editor del horario semanal y la lista de cierres temporales con alta y cancelación.
- **RF-19**: Cuando el administrador general elige una sucursal en el selector del encabezado, el sistema deberá guardarla como sucursal activa de la sesión, mostrarla en el encabezado y llevarlo a su detalle; el selector sólo lista sucursales activas.
- **RF-20**: Cuando un usuario entra en `/r/{slug}/inicio`, el sistema deberá mostrar el acceso "Sucursales" sólo si su rol es `administrador_general`; si otro rol abre una pantalla de sucursales, deberá llevarlo a `/r/{slug}/inicio`.

## Requisitos no funcionales

- **RNF-01**: Las horas del horario y de los cierres se interpretan en la zona horaria de la sucursal; zonas admitidas: `America/Mexico_City` (por defecto), `America/Cancun`, `America/Merida`, `America/Monterrey`, `America/Matamoros`, `America/Chihuahua`, `America/Ciudad_Juarez`, `America/Ojinaga`, `America/Mazatlan`, `America/Bahia_Banderas`, `America/Hermosillo`, `America/Tijuana`.
- **RNF-02**: Los números de teléfono y WhatsApp se guardan en formato E.164 (`+52XXXXXXXXXX`).
- **RNF-03**: Las pantallas funcionan en escritorio (1440 px) y móvil (390 px) y usan sólo variables de tema (Art. 15.3).
- **RNF-04**: El cálculo del estado de atención es determinista para un instante dado (depende de `IReloj`), de modo que se puede probar sin esperar.

## Fuera de alcance

- Entidades del menú (categorías, platillos, variantes, modificadores, ingredientes) y la disponibilidad por sucursal (`BranchItemAvailability`): feature del menú (HU-1 a HU-6 del backlog).
- Asignación de usuarios a sucursales y alcance del administrador de sucursal (que sólo vea la suya y gestione sus cierres): feature 004 (AUTH-01).
- RN-S3 (no desactivar con pedidos en curso): se añadirá con la feature de pedidos.
- Usar el `phone_number_id` para enrutar mensajes de WhatsApp y el mensaje "sucursal no disponible" del chatbot: HT-WA.
- Directorio web de sucursales (WEB-00) y numeración diaria de pedidos con el prefijo.
- Turnos partidos (más de un intervalo por día) y festivos recurrentes.

## Preguntas abiertas

- Ninguna
