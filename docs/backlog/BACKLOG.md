# Backlog — Plataforma de Menú y Pedidos

> **Última actualización:** 2026-11-10
> **Estimación:** Story Points (Fibonacci: 1, 2, 3, 5, 8, 13)
> **Priorización:** MoSCoW (Must / Should / Could / Won't)
> **Estados:** ⬜ Pendiente · 🔄 En progreso · 👀 En revisión · ✅ Hecho · ⛔ Bloqueado

---

## Índice

1. [Épicas](#épicas)
2. [Tablero de control](#tablero-de-control)
3. [Plan de sprints](#plan-de-sprints)
4. [Modelo de pedido unificado](#modelo-de-pedido-unificado) · [Requisitos no funcionales](#requisitos-no-funcionales)
5. [E1 · Administración del Menú](#e1--administración-del-menú-backoffice)
6. [E2 · Pedidos por Chatbot de WhatsApp](#e2--pedidos-por-chatbot-de-whatsapp)
7. [E3 · Pedidos Web y Notificaciones por WhatsApp](#e3--pedidos-web-y-notificaciones-por-whatsapp)
8. [E4 · Impresión de Tickets](#e4--impresión-de-tickets)
9. [Definición de Hecho (DoD)](#definición-de-hecho-dod)
10. [Preguntas abiertas para el Product Owner](#preguntas-abiertas-para-el-product-owner)
11. [Historial de cambios](#historial-de-cambios)

---

## Épicas

| Épica | Nombre | Historias | SP |
|---|---|---|---|
| E1 | Administración del Menú, Sucursales y Usuarios (Backoffice) | HT-0, SUC-01, AUTH-01, HU-1 a HU-6 | 55 |
| E2 | Pedidos por Chatbot de WhatsApp | HT-WA, US-01, US-02a, US-02b, US-03, US-04 | 40 |
| E3 | Pedidos Web y Notificaciones por WhatsApp | WEB-00 a WEB-06, WEB-01b | 48 |
| E4 | Impresión de Tickets | HT-PRN, PRN-01, PRN-03a/b, PRN-04 (PRN-02a/b fuera de alcance) | 28 |
| **Total** | | **28 elementos** | **171** |

> **Nomenclatura:** los IDs originales de cada épica se renombraron para evitar colisiones (p. ej. la "US-01" de impresión es `PRN-01`; la "HU-01" de pedidos web es `WEB-01`). El ID original se conserva entre paréntesis en el detalle.

---

## Tablero de control

| ID | Historia | Épica | Prioridad | SP | Depende de | Sprint | Estado |
|---|---|---|---|---|---|---|---|
| HT-0 | Base del dominio, persistencia y aislamiento por sucursal | E1 | Must | 8 | — | 1 | ⬜ |
| SUC-01 | Gestión de Sucursales y Horarios | E1 | Must | 8 | HT-0 | 1 | ⬜ |
| AUTH-01 | Inicio de sesión, usuarios y roles | E1 | Must | 5 | SUC-01 | 1 | ⬜ |
| HU-1 | Gestión de Categorías del Menú | E1 | Must | 5 | AUTH-01 | 2 | ⬜ |
| HU-2 | Gestión de Platillos Base e Ingredientes | E1 | Must | 8 | HU-1 | 2 | ⬜ |
| HU-3 | Variantes de Platillos y Precios Base | E1 | Must | 5 | HU-2 | 2 | ⬜ |
| WEB-00 | Directorio de Sucursales Activas | E3 | Must | 3 | SUC-01 | 2 | ⬜ |
| HU-4 | Grupos de Modificadores reutilizables | E1 | Must | 8 | HU-2, HU-3 | 3 | ⬜ |
| HU-6 | Marcar Platillo/Variante como Agotado por sucursal | E1 | Must | 3 | HU-2, HU-3, AUTH-01 | 3 | ⬜ |
| HT-WA | Integración base WhatsApp Business | E2 | Must | 8 | SUC-01 | 3 | ⬜ |
| HU-5 | Opciones de Modificadores y Costos Extra | E1 | Must | 5 | HU-4 | 4 | ⬜ |
| US-01 | Consultar el Menú por WhatsApp | E2 | Must | 8 | HT-WA, HU-3, HU-6 | 4 | ⬜ |
| WEB-05 | Tablero de Caja y Cocina en tiempo real | E3 | Must | 8 | AUTH-01 | 4 | ⬜ |
| US-02a | Agregar artículos y cantidades al carrito | E2 | Must | 5 | US-01 | 5 | ⬜ |
| US-02b | Variantes y modificadores en el chat | E2 | Must | 8 | US-02a, HU-5 | 5 | ⬜ |
| WEB-01 | Catálogo de Platillos y Cotizador Dinámico | E3 | Must | 5 | WEB-00, HU-3, HU-6 | 5 | ⬜ |
| HT-PRN | Cola de impresión y registro de dispositivos | E4 | Must | 5 | SUC-01 | 5 | ⬜ |
| US-03 | Cierre, Totalización e Identificación | E2 | Must | 8 | US-02b, WEB-05 | 6 | ⬜ |
| WEB-01b | Variantes y modificadores en la web | E3 | Must | 8 | WEB-01, HU-5 | 6 | ⬜ |
| PRN-01 | Generación del Formato de Ticket | E4 | Must | 5 | HT-PRN | 6 | ⬜ |
| WEB-02 | Captura de Datos, Consentimiento y Registro | E3 | Must | 8 | WEB-01b | 7 | ⬜ |
| WEB-03 | Validación de Código por WhatsApp | E3 | Must | 8 | WEB-02, HT-WA, WEB-05 | 7 | ⬜ |
| WEB-04 | Notificación de Orden Lista para Entrega | E3 | Must | 5 | WEB-05, HT-WA | 7 | ⬜ |
| WEB-06 | Cancelación de Pedidos | E3 | Must | 3 | WEB-05, HT-WA | 8 | ⬜ |
| PRN-03a | App Android: impresora Bluetooth e impresión | E4 | Must | 8 | PRN-01 | 8 | ⬜ |
| PRN-03b | App Android: segundo plano, retención y alertas | E4 | Must | 8 | PRN-03a | 8 | ⬜ |
| US-04 | Ver y editar el carrito en el chatbot | E2 | Should | 3 | US-02b | 9 | ⬜ |
| PRN-04 | Reimpresión de tickets | E4 | Should | 2 | PRN-03a, WEB-05 | 9 | ⬜ |

### Fuera de alcance

*Decisión P-47: la impresión será solo con teléfono Android e impresora Bluetooth. Se conservan como referencia por si en el futuro se usa PC.*

| ID | Historia | Épica | Prioridad | SP | Motivo |
|---|---|---|---|---|---|
| PRN-02a | Agente PC: conexión e impresión silenciosa | E4 | Won't | 8 | Sin PC en las sucursales (P-47) |
| PRN-02b | Agente PC: recuperación y alertas | E4 | Won't | 5 | Sin PC en las sucursales (P-47) |

---

## Plan de sprints

Sprints sugeridos de 2 semanas.

| Sprint | Elementos | SP | Nota |
|---|---|---|---|
| 1 | HT-0, SUC-01, AUTH-01 | 21 | Base multi-sucursal, horarios y acceso |
| 2 | HU-1, HU-2, HU-3, WEB-00 | 21 | Iniciar trámite de números y plantillas en Meta |
| 3 | HU-4, HU-6, HT-WA | 19 | |
| 4 | HU-5, US-01, WEB-05 | 21 | Backoffice completo |
| 5 | US-02a, US-02b, WEB-01, HT-PRN | 23 | Ligeramente cargado |
| 6 | US-03, WEB-01b, PRN-01 | 21 | Chatbot completo |
| 7 | WEB-02, WEB-03, WEB-04 | 21 | Canal web completo |
| 8 | PRN-03a, PRN-03b, WEB-06 | 19 | **Todo lo Must terminado (MVP)** · impresión Android |
| 9 | US-04, PRN-04 | 5 | Should. Sprint corto: puede absorberse en el 8 si hay capacidad, o usarse para estabilización |

---

## Modelo de pedido unificado

Una sola entidad `Order` con campo `Channel` (`WhatsAppBot` | `Web`) y una sola máquina de estados:

```
PendienteConfirmacion → Confirmada → EnPreparacion → ListaParaEntrega → Entregada
          ↘ Expirada                     ↘ Cancelada
```

- "Recibida/En Cocina" (US-03) ≡ `Confirmada`.
- La impresión (E4) se dispara al pasar a `Confirmada` (ver pregunta P-14).
- Transiciones validadas en dominio; una transición inválida responde HTTP 400.
- `OrderLines` guarda **snapshot** de nombre y precio al momento del pedido.
- **Carrito por líneas (P-28 ✅):** cada línea es *platillo + variante + modificadores elegidos + cantidad*. Dos líneas con exactamente la misma configuración se combinan sumando cantidades; con configuración distinta son líneas separadas (ej. "1 Pozole Grande de Puerco" y "1 Pozole Chico de Pollo"). Aplica a ambos canales.

### Reglas por convivencia de canales (P-13)

Como el chatbot (E2) y el pedido web (E3) conviven:

- **Lógica compartida:** el cálculo de precios, la validación del carrito (variantes, modificadores, mínimos, límites) y las transiciones de estado viven en servicios de dominio comunes (`PricingService`, `CartValidator`, `OrderService`). Los canales solo adaptan la entrada y la salida. *Se implementa dentro de US-03 y WEB-02; no agrega SP.*
- **Misma numeración:** ambos canales comparten la secuencia diaria de número de orden por sucursal.
- **Mismo tablero e impresión:** WEB-05 y E4 reciben pedidos de ambos canales y muestran el canal de origen (etiqueta "WhatsApp" / "Web").
- **Paridad de reglas:** un mismo pedido debe ser válido o inválido en ambos canales. Por eso P-15 (variantes en web) y P-29 (reglas del carrito) suben de importancia.

### Aislamiento por sucursal (P-02)

El sistema es **multi-sucursal**: toda la información operativa se diferencia por sucursal.

| Elemento | Regla |
|---|---|
| Menú (categorías, platillos, variantes, modificadores) | **Común para todas las sucursales** (P-38 ✅), **incluidos los precios** (P-41 ✅). Se administra una sola vez y no lleva `BranchId`. |
| Disponibilidad ("Agotado") | **Por sucursal** (P-42 ✅): una sucursal puede agotar un platillo sin afectar a las demás. Tabla `BranchItemAvailability (BranchId, MenuItemId/ItemVariantId, IsAvailable)`. |
| Pedidos | Siempre tienen `BranchId`. La numeración diaria es por sucursal (`(BranchId, Date, Number)` único). |
| Usuarios | Cada usuario está asignado a una o más sucursales y solo ve los datos de ellas (ver P-24). |
| Tablero de Caja y Cocina (WEB-05) | Muestra únicamente los pedidos de la sucursal del usuario. |
| Impresoras (E4) | Cada `PrintDevice` pertenece a una sucursal y solo recibe los tickets de ella. |
| Canal WhatsApp (E2) | **Cada sucursal tiene su propio número de WhatsApp** (P-25 ✅). La sucursal se identifica por el número que recibe el mensaje (`phone_number_id` del webhook). |
| Mensajes salientes (E2, E3) | Toda respuesta, código de verificación (WEB-03) o aviso de "orden lista" (WEB-04) se envía **desde el número de la sucursal** del pedido. |
| Sesiones del chatbot | La sesión y el carrito se identifican por **(sucursal, teléfono del cliente)**: un cliente puede tener carritos independientes en dos sucursales. |
| Canal Web (E3) | La web muestra un **directorio de sucursales activas con su número de WhatsApp** (WEB-00, P-25b ✅). El cliente elige la sucursal ahí y el pedido web queda asociado a ella (URL `/s/{sucursal}`). |

**Implementación:** filtro global de EF Core por `BranchId` del usuario o del canal, de modo que ninguna consulta pueda mezclar datos de sucursales. Las pruebas de integración de cada historia incluyen el caso "un usuario de la sucursal A no puede ver ni modificar datos de la sucursal B" (→ 404).

---

## Requisitos no funcionales

*Definidos por P-37. Aplican a todas las épicas y se verifican en Staging/QA.*

| Requisito | Valor |
|---|---|
| Actualización de precios y disponibilidad en los canales | ≤ 60 segundos después de guardar en el backoffice |
| Tiempo de respuesta del chatbot | < 3 segundos por mensaje (desde que llega el webhook hasta que se envía la respuesta) |
| Concurrencia | 50 pedidos simultáneos por sucursal sin errores ni números de orden duplicados |
| Disponibilidad | 99.5% mensual |
| Expiración | Pedido web sin verificar: 15 min · Carrito del chatbot sin actividad: 30 min · Código de verificación: 10 min |
| Teléfonos | Solo México (+52), almacenados en formato E.164 |
| Impuestos | Precios con IVA incluido; tasa configurable (16% por defecto) solo para el desglose informativo |
| Monitoreo | Logs estructurados y alertas de fallas en webhook, envío de WhatsApp e impresión |

---

## E1 · Administración del Menú (Backoffice)

### HT-0 · Base del dominio, persistencia y aislamiento por sucursal — 8 SP · Must · ⬜
*Habilitador técnico. Subió de 5 a 8 SP por el requisito multi-sucursal.*

- [ ] Entidades C#: `Branch`, `Category`, `MenuItem`, `Ingredient`, `ItemVariant`, `ModifierGroup`, `ModifierOption` y tablas de vínculo (platillo↔grupo, variante↔grupo).
- [ ] Clase base con `IsActive` / `DeletedAt` y filtro global de EF Core para soft delete.
- [ ] **Menú global** (sin `BranchId`) y tabla `BranchItemAvailability` para la disponibilidad por sucursal.
- [ ] `BranchId` en las entidades operativas (pedidos, sesiones, usuarios, impresoras) y **filtro global por sucursal** (contexto resuelto desde el usuario autenticado o desde el canal).
- [ ] Migración inicial, seed con al menos 2 sucursales de prueba y pipeline de pruebas con cobertura.
- [ ] Prueba de aislamiento: datos de la sucursal A invisibles desde la sucursal B.

---

### SUC-01 · Gestión de Sucursales y Horarios — 8 SP · Must · ⬜
*Subió de 5 a 8 SP por el horario de atención (P-26).*
*Historia agregada por el requisito multi-sucursal (P-02).*

**Como** administrador general, **quiero** crear, consultar, modificar y desactivar sucursales, **para** que cada una opere con su propio menú, pedidos, personal e impresoras.

**Criterios de aceptación**
- [ ] CA1: Listado de sucursales con Nombre, Dirección y Estado (activa/inactiva).
- [ ] CA2: Crear y editar una sucursal con: Nombre, Dirección, Teléfono, Identificador para URL (`slug`), **Número de WhatsApp (obligatorio) y su `phone_number_id` de Meta**, y Prefijo de número de orden.
- [ ] CA3: Desactivar una sucursal (no se elimina físicamente).
- [ ] CA4: El administrador general puede cambiar de sucursal activa en el backoffice; el administrador de sucursal solo ve la suya.
- [ ] CA5 *(P-26)*: Cada sucursal configura su **horario de atención por día de la semana** (hora de apertura y cierre; día cerrado).
- [ ] CA6 *(P-26)*: El administrador de sucursal puede marcar un **cierre temporal** (ej. día festivo) que se aplica de inmediato.

**Reglas de negocio**
- [ ] RN-S1: Nombre, `slug` y número de WhatsApp únicos entre sucursales.
- [ ] RN-S2: Una sucursal inactiva no aparece en la web ni responde pedidos por WhatsApp (mensaje de "sucursal no disponible").
- [ ] RN-S3: No se puede desactivar una sucursal con pedidos en curso (`Confirmada`, `EnPreparacion`, `ListaParaEntrega`).
- [ ] RN-S4 *(P-26)*: Fuera de horario (o con cierre temporal) la sucursal **no acepta pedidos** en ningún canal; responde "Estamos cerrados, abrimos {día} a las {hora}". Un pedido ya iniciado antes del cierre puede terminarse.
- [ ] RN-S5 *(P-21)*: El **prefijo** del número de orden es obligatorio y único entre sucursales (ej. `A`, `B`), para formar números diarios como `A-014`.

**Tareas técnicas**
- [ ] CRUD `/branches` restringido al rol Administrador General.
- [ ] Selector de sucursal en el encabezado del backoffice.
- [ ] Resolución de sucursal por `phone_number_id` de WhatsApp y por `slug` de URL.

**Pruebas clave:** `slug` o WhatsApp duplicado → 400; desactivar con pedidos en curso → 400; administrador de sucursal no ve otras sucursales.

---

### AUTH-01 · Inicio de sesión, usuarios y roles — 5 SP · Must · ⬜
*Historia agregada por P-24 y P-39.*

**Como** administrador general, **quiero** dar de alta usuarios con un rol y asignarlos a sucursales, **para** que cada persona acceda solo a las funciones y sucursales que le corresponden.

**Roles**

| Rol | Permisos | Alcance |
|---|---|---|
| Administrador general | Menú, precios, sucursales y usuarios | Todas las sucursales |
| Administrador de sucursal | Agotados, personal e impresoras de su sucursal | Su sucursal |
| Cajero | Tablero de caja: ver pedidos, cobrar, marcar "Entregada" | Una o varias sucursales |
| Cocinero | Tablero de cocina: ver pedidos, marcar "En preparación" y "Lista" | Una o varias sucursales |

**Criterios de aceptación**
- [ ] CA1: Inicio y cierre de sesión con usuario y contraseña.
- [ ] CA2: El administrador general crea, edita y desactiva usuarios, asignándoles rol y sucursal(es).
- [ ] CA3: El administrador de sucursal puede gestionar cajeros y cocineros solo de su sucursal.
- [ ] CA4: Un usuario con varias sucursales elige con cuál trabajar al iniciar sesión (y puede cambiarla).
- [ ] CA5: Cada pantalla y endpoint valida el rol; sin permiso → HTTP 403.

**Reglas de negocio**
- [ ] RN-A1: Correo/usuario único.
- [ ] RN-A2: Un usuario desactivado no puede iniciar sesión y sus sesiones activas se cierran.
- [ ] RN-A3: Debe existir al menos un administrador general activo.

**Tareas técnicas**
- [ ] ASP.NET Core Identity + JWT o cookie segura; contraseñas con hash.
- [ ] Políticas de autorización por rol y por sucursal (alimentan el filtro global de HT-0).
- [ ] Pantallas de login, listado y formulario de usuarios.

**Pruebas clave:** cajero intenta editar el menú → 403; administrador de sucursal A intenta crear usuario en B → 403; desactivar al último administrador general → 400.

---

### HU-1 · Gestión de Categorías del Menú — 5 SP · Must · ⬜

**Como** administrador del restaurante, **quiero** crear, consultar, modificar y eliminar las categorías del menú (ej. Pozoles, Chilaquiles, Bebidas), **para** organizar la oferta gastronómica y facilitar la navegación del cliente.

**Criterios de aceptación**
- [ ] CA1: Listado paginado de categorías con Nombre y `DisplayOrder`.
- [ ] CA2: Crear categoría ingresando Nombre y Orden.
- [ ] CA3: Editar nombre y orden de una categoría existente.
- [ ] CA4: Eliminar una categoría.
- [ ] CA5 *(P-45)*: Campo opcional **"Nombre corto para WhatsApp"** (máx. 24 caracteres), con vista previa de cómo se verá en la lista de WhatsApp.

**Reglas de negocio**
- [ ] RN1.1: Nombre obligatorio y único. *(Ajuste por P-38: el menú es común, así que la unicidad es global y no "dentro de la sucursal".)*
- [ ] RN1.2: No se puede eliminar una categoría con `MenuItems` activos; mostrar error u ofrecer "Desactivar" (soft delete). *(P-01 ✅: se hacen ambas cosas: mensaje "Esta categoría tiene N platillos activos" + botón "Desactivar categoría".)*
- [ ] RN1.3: `DisplayOrder` solo acepta enteros positivos.

**Tareas técnicas**
- [ ] `GET /categories?page&size` + tabla paginada en UI.
- [ ] `POST` / `PUT /categories/{id}` + formulario con validación.
- [ ] `DELETE /categories/{id}` con validación de platillos activos → 400.
- [ ] Índice único `(Name)`.
- [ ] Campo `ShortName` (nullable, máx. 24) con índice único cuando tenga valor. Regla de nombre corto: ver "Nombre corto para WhatsApp" en HU-2.
- [ ] Validación de `DisplayOrder` en backend y frontend.
- [ ] Solo el rol Administrador General puede modificar el menú (AUTH-01).

**Pruebas clave:** nombre duplicado → 400; eliminar con platillos activos → 400; `DisplayOrder` = 0, negativo o decimal → 400; un cambio en el menú se refleja en todas las sucursales.

---

### HU-2 · Gestión de Platillos Base e Ingredientes — 8 SP · Must · ⬜

**Como** administrador del restaurante, **quiero** registrar y administrar los platillos base asignándolos a una categoría, definiendo su descripción y los ingredientes que incluye por defecto, **para** informar al cliente qué contiene su comida antes de cualquier modificación.

**Criterios de aceptación**
- [ ] CA1: Desde una categoría, agregar un Platillo (`MenuItem`).
- [ ] CA2: Formulario con Nombre, Descripción (opcional) y listado dinámico de Ingredientes Incluidos (ej. "Crema", "Cebolla", "Queso Rayado").
- [ ] CA3: Consultar detalle, modificar o eliminar el platillo.
- [ ] CA4 *(P-45)*: Campo opcional **"Nombre corto para WhatsApp"** (máx. 24 caracteres), con vista previa de la fila de WhatsApp.
- [ ] CA5 *(P-34 ✅)*: Los ingredientes incluidos se muestran al cliente en el **detalle del platillo** (panel de la web, WEB-01b, y mensaje del bot al elegir el platillo, US-01). **No** se imprimen en el ticket y **no** se vinculan con los modificadores.

**Nombre corto para WhatsApp (aplica a categorías y platillos, P-45 ✅)**
- [ ] RN-NC1: Es opcional y tiene máximo 24 caracteres (límite del título de fila en las listas de WhatsApp).
- [ ] RN-NC2: Se usa **solo** en los títulos de las listas de WhatsApp. La web, los tickets, el tablero y las descripciones usan siempre el nombre completo.
- [ ] RN-NC3: Si no hay nombre corto y el nombre completo cabe en 24 caracteres, WhatsApp muestra el nombre completo.
- [ ] RN-NC4: Si no hay nombre corto y el nombre completo pasa de 24 caracteres, el backoffice muestra la advertencia *"Este nombre se verá recortado en WhatsApp"* y el bot lo recorta con "…".
- [ ] Tarea: campo `ShortName` (nullable, máx. 24) en `MenuItem`; función común `GetWhatsAppTitle()` usada por US-01, US-02a y US-02b.
- Pruebas: nombre de 30 caracteres sin nombre corto → advertencia y recorte; con nombre corto → WhatsApp muestra el corto y el ticket muestra el completo; nombre corto de 25 caracteres → 400.

**Reglas de negocio**
- [ ] RN2.1: Un Platillo debe pertenecer obligatoriamente a una Categoría activa.
- [ ] RN2.2: El nombre del Platillo es obligatorio.
- [ ] RN2.3: Al eliminar/desactivar un Platillo, sus Variantes y Modificadores quedan inaccesibles para la venta.

**Tareas técnicas**
- [ ] `POST /categories/{id}/menu-items`.
- [ ] `GET` / `PUT` / `DELETE /menu-items/{id}` + pantalla de detalle.
- [ ] Componente de ingredientes dinámicos (agregar/quitar).
- [ ] Validación de categoría activa → 400.
- [ ] Desactivación en cascada y filtro en consultas de venta.

**Pruebas clave:** crear en categoría inactiva → 400; sin nombre → 400; tras desactivar, sus variantes no se devuelven.

---

### HU-3 · Configuración de Variantes de Platillos y Precios Base — 5 SP · Must · ⬜

**Como** administrador del restaurante, **quiero** definir múltiples variantes de venta para un platillo (ej. Tamaño: 1/2 Litro vs 1 Litro, o Proteína: Sencillo vs Suadero vs Combinado) con un precio para cada una, **para** cobrar correctamente según la versión que elija el cliente.

**Criterios de aceptación**
- [ ] CA1: Sección "Variantes de Venta" (`ItemVariants`) en el detalle del Platillo.
- [ ] CA2: Agregar variantes con Nombre, Precio Base y SKU (opcional).
- [ ] CA3: Editar precio y nombre, o eliminar una variante.

**Reglas de negocio**
- [ ] RN3.1: Precio Base decimal ≥ $0.00.
- [ ] RN3.2: Todo Platillo debe tener al menos una Variante para publicarse/venderse (variante por defecto "Único"/"Regular").
- [ ] RN3.5 *(P-03 ✅)*: No hay un paso manual de "publicar". Un platillo es **visible para los clientes** automáticamente cuando: está activo, tiene al menos una variante activa, su categoría está activa y no está agotado en la sucursal.
- [ ] RN3.3: Nombre de variante único dentro del mismo Platillo.
- [ ] RN3.4 *(P-46)*: Nombre de variante de **máximo 24 caracteres**, porque se muestra como título de fila en las listas de WhatsApp (US-02b). El formulario muestra un contador de caracteres.

**Tareas técnicas**
- [ ] `POST /menu-items/{id}/variants`, `PUT` / `DELETE /variants/{id}`.
- [ ] Tipo `decimal(10,2)` para precios.
- [ ] Crear variante "Regular" automáticamente al dar de alta un platillo; impedir borrar la última.
- [ ] Índice único `(MenuItemId, Name)`.

**Pruebas clave:** precio negativo → 400; nombre duplicado en el mismo platillo → 400; borrar la última variante → 400; nombre de 24 caracteres → 201; nombre de 25 caracteres → 400.

---

### HU-4 · Configuración de Grupos de Modificadores (Reglas de Selección) — 8 SP · Must · ⬜

**Como** administrador del restaurante, **quiero** crear grupos de modificadores con reglas de selección mínima y máxima (ej. "Preparación", "Adicionales", "Elige 2 guisos"), **para** controlar cómo el cliente personaliza su pedido.

**Criterios de aceptación**
- [ ] CA1: Crear `ModifierGroup` con Nombre, `MinSelections` y `MaxSelections`.
- [ ] CA2: Vincular el grupo **globalmente al Platillo** (todas las variantes) o **específico a una Variante**.
- [ ] CA3: Desvincular o editar las reglas de selección.
- [ ] CA4 *(P-04)*: Los grupos forman un **catálogo reutilizable**: un mismo grupo (ej. "Picoso / No picoso") se crea una vez y se vincula a varios platillos o variantes.
- [ ] CA5 *(P-04)*: Al editar un grupo o sus opciones, el sistema advierte cuántos platillos lo usan, porque el cambio aplica a todos.
- [ ] CA6 *(P-04)*: Un grupo con vínculos activos no se elimina; se desvincula primero o se desactiva.

**Reglas de negocio**
- [ ] RN4.1: `MinSelections` ≤ `MaxSelections`.
- [ ] RN4.4 *(P-05 ✅)*: `MaxSelections` debe ser **≥ 1**.
- [ ] RN4.2: Si `MinSelections` > 0, el grupo es "Obligatorio" para agregar al carrito.
- [ ] RN4.3: Si `MinSelections` = 0, el grupo es opcional.

**Tareas técnicas**
- [ ] CRUD `/modifier-groups`.
- [ ] `POST /menu-items/{id}/modifier-groups` y `POST /variants/{id}/modifier-groups`; `DELETE` del vínculo.
- [ ] Campo calculado `IsRequired` en el DTO.

**Pruebas clave:** Min > Max → 400; valores negativos → 400; grupo de variante solo aparece al consultar esa variante; `IsRequired` correcto.

---

### HU-5 · Configuración de Opciones de Modificadores y Costos Extra — 5 SP · Must · ⬜

**Como** administrador del restaurante, **quiero** agregar opciones dentro de un grupo de modificadores y definir si tienen costo extra, **para** cobrar los ingredientes adicionales (ej. "Huevo Extra $10.00").

**Criterios de aceptación**
- [ ] CA1: Agregar opciones (`ModifierOption`) dentro de un grupo.
- [ ] CA2: Cada opción requiere Nombre y Precio Extra (`ExtraPrice`).
- [ ] CA3: Listar opciones del grupo y permitir editarlas o eliminarlas.

**Reglas de negocio**
- [ ] RN5.1: `ExtraPrice` puede ser $0.00.
- [ ] RN5.2: `ExtraPrice` nunca negativo.
- [ ] RN5.3: No se puede eliminar una opción presente en pedidos en curso o históricos (soft delete).
- [ ] RN5.4 *(P-46)*: Nombre de opción de **máximo 24 caracteres**, porque se muestra como título de fila en las listas de WhatsApp (US-02b). El formulario muestra un contador de caracteres.

**Tareas técnicas**
- [ ] CRUD `/modifier-groups/{id}/options` + lista editable en línea.
- [ ] Soft delete obligatorio; pedidos guardan snapshot de nombre/precio.

**Pruebas clave:** precio negativo → 400; $0.00 → 201; opción eliminada no aparece en menú pero sí en pedidos históricos; nombre de 25 caracteres → 400.

---

### HU-6 · Marcar Platillo/Variante como Agotado por sucursal — 3 SP · Must · ⬜
*Historia agregada para cubrir la regla de "Agotado" de US-01. Ajustada por P-38: el menú es común, la disponibilidad es por sucursal (P-42 ✅ confirmada).*

**Como** administrador de sucursal o personal de cocina, **quiero** marcar un platillo o variante como agotado en mi sucursal, **para** que no se ofrezca a los clientes de esa sucursal en ningún canal, sin afectar a las demás.

- [ ] Tabla `BranchItemAvailability` (sin registro = disponible).
- *(P-06 ✅)* Solo se maneja el estado "Agotado"; no hay control de existencias (sería una épica futura).
- [ ] Toggle rápido de disponibilidad en el backoffice y en el tablero de cocina, limitado a la sucursal del usuario.
- [ ] Endpoints públicos (WhatsApp y Web) filtran por `IsActive` del menú **y** disponibilidad de la sucursal del pedido.

**Pruebas clave:** agotar un platillo en la sucursal A → no aparece en A, sí aparece en B; usuario de A no puede cambiar la disponibilidad de B.

---

## E2 · Pedidos por Chatbot de WhatsApp

### HT-WA · Integración base WhatsApp Business — 8 SP · Must · ⬜
*Habilitador técnico, compartido con E3.*

- [ ] **Una sola cuenta de WhatsApp Business (WABA)** en la Cloud API para todo el negocio (P-40 ✅), con **un número por sucursal** (registro, verificación y nombre visible aprobado por Meta para cada uno) y un número de pruebas.
- [ ] **Trámite de plantillas (P-19 ✅) — iniciar en el sprint 2**, porque la aprobación de Meta puede tardar:
  - Plantilla de **autenticación** para el código de 4 dígitos (WEB-03).
  - Plantilla de **utilidad** para "Tu orden #{{2}} está lista" (WEB-04).
  - Plantilla de **utilidad** para "Tu pedido #{{2}} fue cancelado" (WEB-06, P-32).
  - Al estar en una sola WABA, las plantillas aprobadas sirven para los números de todas las sucursales.
- [ ] Estimar el **costo mensual de mensajes** (plantillas de autenticación y utilidad se cobran por mensaje) e informarlo al Product Owner.
- [ ] Un solo webhook para todos los números; **enrutamiento a la sucursal** por `metadata.phone_number_id`. Mensajes a un número no registrado en SUC-01 → se ignoran y se registran en bitácora.
- [ ] El cliente de envío usa el `phone_number_id` de la sucursal del pedido.
- [ ] *(P-33)* **Comandos globales**, válidos en cualquier paso: **MENÚ** (volver a categorías), **CANCELAR** (descartar el carrito en curso), **BAJA** (ver US-01). Ante texto no reconocido, el bot responde con un mensaje de ayuda que lista estos comandos. Sin atención humana por ahora.
- [ ] *(P-30)* El carrito del chatbot expira tras **30 minutos sin actividad**; al siguiente mensaje del cliente se le avisa y se reinicia la conversación.
- [ ] *(P-36)* Normalización de teléfonos de México: el `wa_id` `521XXXXXXXXXX` se guarda como `+52XXXXXXXXXX`, igual que en WEB-02, para reconocer al mismo cliente en ambos canales.
- [ ] Webhook: verificación de *challenge*, validación de firma `X-Hub-Signature-256`, respuesta 200 inmediata y procesamiento asíncrono.
- [ ] Idempotencia por `message.id` (WhatsApp reintenta entregas).
- [ ] Máquina de estados de conversación (`Inicio → Navegando → Armando → Cerrando → EsperandoNombre → Confirmado`) con sesión por **(sucursal, teléfono del cliente)** y expiración.
- [ ] Cliente para enviar texto, listas interactivas y botones.
- [ ] Entorno QA simulado.

---

### US-01 · Consultar el Menú por WhatsApp — 8 SP · Must · ⬜

**Como** cliente, **quiero** consultar el menú del restaurante a través de WhatsApp **para** conocer los platillos disponibles y sus precios actualizados.

**Criterios de aceptación**
- [ ] AC1: Al enviar un saludo ("Hola", "Menú"), el chatbot responde con bienvenida y catálogo.
- [ ] AC2: Menú estructurado por categoría (Catálogo de WhatsApp Business o listas interactivas).
- [ ] AC3: Cada elemento muestra nombre, breve descripción y precio unitario.
- [ ] AC4 *(P-43)*: El mensaje de bienvenida incluye el enlace al **Aviso de Privacidad** (`/aviso-de-privacidad`, el mismo de WEB-02).
- [ ] AC5 *(P-43)*: Si el cliente escribe **"BAJA"**, el bot confirma: *"Listo, ya no te enviaremos avisos de esta sucursal. Si vuelves a escribirnos para hacer un pedido, los reactivaremos."*

**Reglas de privacidad (P-43)**
- [ ] RN-P1: "BAJA" (sin importar mayúsculas o acentos) registra la revocación en `ConsentRecord` para ese teléfono y sucursal, y cancela el carrito en curso.
- [ ] RN-P2: Con la baja activa, el sistema **no envía mensajes iniciados por el negocio** a ese teléfono (ej. "orden lista" de WEB-04).
- [ ] RN-P3: La baja se revierte solo si el cliente vuelve a iniciar un pedido por WhatsApp o acepta de nuevo el consentimiento en la web (WEB-02).

**Reglas de negocio**
- [ ] Precios sincronizados en tiempo real (o con desfase mínimo) desde la base de datos central.
- [ ] Productos con inventario cero o "Agotado" no se muestran como elegibles.

**Tareas técnicas**
- [ ] Detección de intención con palabras clave normalizadas (sin acentos ni mayúsculas).
- [ ] **Navegación en 2 pasos con listas interactivas (P-07 ✅):**
  - **Paso 1 — Categorías:** mensaje de bienvenida + lista interactiva con las categorías ordenadas por `DisplayOrder`.
  - **Paso 2 — Platillos:** al elegir una categoría, se envía una segunda lista con sus platillos (nombre en el título; precio y descripción en la descripción de la fila).
  - En el paso 2 se incluye la fila **"⬅ Volver a categorías"**.
- [ ] Respetar los límites de WhatsApp: máx. **10 filas** por lista, título de fila **24** caracteres, descripción **72**, texto del botón **20**.
- [ ] Título de cada fila = **nombre corto para WhatsApp** de la categoría o platillo; si no tiene, el nombre completo (recortado con "…" si pasa de 24). Ver RN-NC1 a RN-NC4 en HU-2.
- [ ] **Paginación:** si una categoría tiene más platillos de los que caben, se muestran 8 platillos + "Ver más ▸" + "⬅ Volver a categorías". Lo mismo si hay más de 10 categorías ("Ver más ▸").
- [ ] Descripción de fila: `"$85.00 · Pozole rojo con…"` (truncada a 72). Si el platillo tiene varias variantes: `"Desde $85.00 · …"`.
- [ ] Al elegir un platillo, se envía su descripción completa **y sus ingredientes incluidos** (P-34) antes de continuar con variante/cantidad (US-02a / US-02b).
- [ ] Ocultar categorías sin platillos disponibles en la sucursal.
- [ ] *(P-26)* Fuera de horario, en lugar del menú se responde "Estamos cerrados, abrimos {día} a las {hora}".
- [ ] Lectura directa de BD o caché corta (p. ej. 60 s) invalidada al editar en backoffice.
- [ ] Filtro por disponibilidad de la sucursal dueña del número que recibió el mensaje (HU-6).

**Pruebas clave:** variantes del saludo; categoría sin platillos disponibles se oculta; cambio de precio reflejado dentro del desfase; platillo agotado no se lista; categoría con 15 platillos se pagina correctamente; "Volver a categorías" regresa al paso 1; nombres de más de 24 caracteres se truncan sin romper el mensaje.

---

### US-02a · Agregar artículos y cantidades al carrito — 5 SP · Must · ⬜
*Parte 1 de la US-02 original.*

**Como** cliente, **quiero** agregar platillos a mi carrito o flujo de chat en WhatsApp **para** conformar mi pedido.

**Criterios de aceptación**
- [ ] AC1: Seleccionar artículos y especificar cantidad.
- [ ] AC2: Confirmación al agregar (ej. "1x Hamburguesa Clásica agregada. ¿Deseas agregar algo más o proceder al cierre?").
- [ ] AC3: Opción clara para finalizar ("Terminar pedido").

**Reglas de negocio**
- [ ] Pedido mínimo de 1 artículo.
- [ ] *(P-29)* Máximo **99 unidades por línea**, igual que en la web.

**Tareas técnicas**
- [ ] Selección desde lista → solicitud de cantidad (botones 1/2/3 o texto).
- [ ] Botones "Agregar más" / "Terminar pedido" + comando de texto.
- [ ] Bloquear "Terminar" con carrito vacío.

**Pruebas clave:** cantidad inválida (0, texto, negativa); terminar con carrito vacío; agregar el mismo artículo con la misma configuración suma la cantidad; con configuración distinta crea otra línea (P-28).

---

### US-02b · Variantes y modificadores en el chat — 8 SP · Must · ⬜
*Parte 2 de la US-02 original.*

**Reglas de negocio**
- [ ] Si un artículo tiene modificadores obligatorios, el bot los pregunta antes de confirmar el artículo.

**Tareas técnicas**
- [ ] Si hay más de una variante, preguntar cuál (lista con precio).
- [ ] Recorrer grupos (globales + de la variante elegida); no permitir omitir grupos con `MinSelections` > 0.
- [ ] Validar Min/Max y volver a preguntar si no cumple.
- [ ] Mostrar `ExtraPrice` en la descripción de cada opción.

**Pruebas clave:** selección por debajo del mínimo o arriba del máximo; caso "Combinado → Elige 2 guisos"; grupo opcional omitido.

---

### US-03 · Cierre de Pedido, Totalización e Identificación — 8 SP · Must · ⬜

**Como** cliente, **requiero** conocer el total exacto de mi cuenta, proporcionar mi nombre y recibir un número de orden **para** confirmar mi pedido.

**Criterios de aceptación**
- [ ] AC1: Desglose de cuenta (Subtotal, Impuestos si aplican, Total). *(P-08: los precios incluyen IVA, así que se muestra el Total y la leyenda informativa "IVA incluido: $X".)*
- [ ] AC2: Solicitar nombre: "Para identificar tu pedido al recogerlo, por favor escribe tu nombre."
- [ ] AC3: Ticket digital con nombre, resumen, total y Número de Orden visible. *(P-09: incluye "Pago al recoger en caja".)*
- [ ] AC4: Pedido registrado en el sistema interno (WEB-05) con nombre y número de orden. *(P-35 ✅: no hay POS externo; WEB-05 funciona como POS. El SKU de HU-3 queda opcional para una integración futura.)*

**Reglas de negocio**
- [ ] Cálculo del total estrictamente en backend.
- [ ] Número de orden secuencial o alfanumérico único en el día.
- [ ] La orden no pasa a "Recibida/En Cocina" (`Confirmada`) hasta capturar nombre y emitir número.

**Tareas técnicas**
- [ ] Servicio de cálculo: Σ (precio variante + extras) × cantidad. IVA incluido; el desglose informativo usa la tasa configurable (16% por defecto) (P-08).
- [ ] Estado `EsperandoNombre` con validación.
- [ ] Persistir `Order` + `OrderLines` con snapshot.
- [ ] Recalcular con precios actuales al cerrar. *(P-31)* Si cambió algún precio o se agotó un artículo, avisar al cliente con el detalle del cambio y pedirle confirmar de nuevo.
- [ ] Secuencia diaria por sucursal con el prefijo de SUC-01 (ej. `A-001`) y restricción única `(BranchId, Date, Number)` (P-21).
- [ ] Transición atómica a `Confirmada` en transacción.

**Pruebas clave:** cálculo con extras e impuestos; cambio de precio entre agregar y cerrar; dos cierres concurrentes sin número duplicado; orden no visible en cocina antes del nombre.

---

### US-04 · Ver y editar el carrito en el chatbot — 3 SP · Should · ⬜
*Historia agregada por P-10. En la web esto lo cubre WEB-01b (CA6).*

**Como** cliente en WhatsApp, **quiero** ver mi carrito y corregirlo antes de cerrar el pedido, **para** no tener que empezar de nuevo si me equivoqué.

**Criterios de aceptación**
- [ ] CA1: El comando **CARRITO** (válido en cualquier paso, junto a MENÚ, CANCELAR y BAJA) muestra las líneas del carrito con su configuración, cantidad y subtotal, más el total.
- [ ] CA2: El cliente puede elegir una línea (lista interactiva) y **quitarla** o **cambiar su cantidad**.
- [ ] CA3: Después de cada cambio, el bot muestra el carrito actualizado con los botones "Agregar más" y "Terminar pedido".

**Reglas de negocio**
- [ ] RN-C1: Si se quita la última línea, el carrito queda vacío y no se puede terminar el pedido (mínimo 1 artículo).
- [ ] RN-C2: La nueva cantidad respeta el máximo de 99 unidades por línea (P-29).

**Pruebas clave:** quitar una línea recalcula el total; cambiar cantidad a 0 → se pide confirmar quitar la línea; cantidad 100 → rechazada; "CARRITO" con carrito vacío → mensaje guía.

---

## E3 · Pedidos Web y Notificaciones por WhatsApp

### WEB-00 · Directorio de Sucursales Activas — 3 SP · Must · ⬜
*Historia agregada por P-25b.*

**Como** cliente, **quiero** ver en la web las sucursales activas con su número de WhatsApp, **para** elegir a cuál hacer mi pedido, ya sea en la web o por WhatsApp.

**Criterios de aceptación**
- [ ] CA1: La página inicial muestra todas las sucursales **activas** con Nombre, Dirección y Número de WhatsApp.
- [ ] CA2: El número de WhatsApp es un enlace que abre el chat con esa sucursal (`https://wa.me/52XXXXXXXXXX?text=Hola`).
- [ ] CA3: Cada sucursal tiene un botón "Pedir en línea" que lleva a su menú web (`/s/{sucursal}`, WEB-01).
- [ ] CA4: Las sucursales inactivas no se muestran.
- [ ] CA5 *(P-26)*: Las sucursales activas fuera de horario se muestran como **"Cerrado · abre {día} a las {hora}"** y su botón "Pedir en línea" queda deshabilitado.

**Reglas de negocio**
- [ ] RN-W0.1: Si se accede a la URL de una sucursal inactiva, se muestra un aviso y se redirige al directorio.

**Tareas técnicas**
- [ ] `GET /public/branches` (solo activas: nombre, dirección, WhatsApp, slug).
- [ ] Página responsiva con tarjetas por sucursal.
- [ ] Recordar la última sucursal elegida en el navegador (conveniencia, no obligatorio).

**Pruebas clave:** sucursal desactivada desaparece del directorio; enlace `wa.me` con formato correcto; URL de sucursal inactiva redirige.

---

### WEB-01 (HU-01) · Catálogo de Platillos y Cotizador Dinámico — 5 SP · Must · ⬜

**Como** cliente, **quiero** visualizar el menú con descripciones y precios, y seleccionar la cantidad de cada platillo, **para** armar mi pedido y conocer el total en tiempo real.

**Criterios de aceptación**
- [ ] Lista o cuadrícula de platillos disponibles con nombre, descripción y precio unitario.
- [ ] Control +/− por platillo (por defecto 0).
- [ ] Barra fija inferior con "Total a pagar".
- [ ] El total se actualiza dinámicamente al modificar cantidades.
- [ ] ~~"Continuar"/"Finalizar Pedido" habilitado solo si el total > $0.00.~~ → **Ajustado por P-29:** habilitado si hay **al menos 1 artículo** (se permite total $0.00, igual que en el chatbot).

**Reglas de negocio**
- [ ] Precios no negativos.
- [ ] Cantidad máxima: 99 unidades **por línea** (ajustado por P-28 y P-29).
- [ ] Total = Σ(`precio_unitario` × `cantidad`).

**Tareas técnicas**
- [ ] `GET /public/branches/{slug}/menu` (menú común, filtrado por la disponibilidad de la sucursal elegida en WEB-00) + tarjetas responsivas.
- [ ] Componente de cantidad; cálculo reactivo en cliente solo como **vista previa**.
- [ ] Límite de 99 en frontend **y** backend; usar `decimal`, nunca `float`.

**Pruebas clave:** + se detiene en 99; − no baja de 0; suma exacta con decimales (3 × $33.33); botón deshabilitado sin artículos y habilitado con un solo artículo de $0.00.

---

### WEB-01b · Variantes y modificadores en la web — 8 SP · Must · ⬜
*Historia agregada por P-15. Equivalente web de US-02b, para mantener la paridad entre canales (P-13) con carrito por líneas (P-28).*

**Como** cliente en la web, **quiero** elegir la variante y los modificadores de un platillo, **para** pedir exactamente lo que quiero, igual que por WhatsApp.

**Criterios de aceptación**
- [ ] CA1: Los platillos con más de una variante o con grupos de modificadores muestran un botón **"Elegir"** que abre un panel de configuración. Los platillos con una sola variante y sin modificadores conservan el control +/− de WEB-01.
- [ ] CA2: El panel muestra la descripción, los **ingredientes incluidos** (P-34), las variantes (selección única, con su precio) y los grupos aplicables: los globales del platillo y los de la variante elegida.
- [ ] CA3: Cada grupo indica si es **Obligatorio** u **Opcional** y cuántas opciones se pueden elegir (ej. "Elige 2"), y cada opción muestra su precio extra.
- [ ] CA4: El precio del panel se actualiza al momento: (precio de la variante + extras) × cantidad.
- [ ] CA5: "Agregar al pedido" solo se habilita si se cumplen todos los mínimos y máximos.
- [ ] CA6: El resumen del pedido muestra cada línea con su configuración y permite cambiar la cantidad o quitar la línea.

**Reglas de negocio**
- [ ] RN-W1b.1: Las validaciones son las mismas que en el chatbot (servicio `CartValidator` compartido).
- [ ] RN-W1b.2: Al cambiar de variante se limpian las selecciones de los grupos que solo aplicaban a la variante anterior.
- [ ] RN-W1b.3: Mismo platillo con la misma configuración → se suma a la línea existente; con otra configuración → nueva línea (P-28).
- [ ] RN-W1b.4: El backend vuelve a validar variantes, modificadores y precios al registrar el pedido (WEB-02); nunca confía en lo que envía el navegador.

**Tareas técnicas**
- [ ] Ampliar `GET /public/branches/{slug}/menu` con variantes, grupos, opciones, `IsRequired`, `MinSelections` y `MaxSelections`.
- [ ] Componente de panel/modal responsivo (en móvil, a pantalla completa).
- [ ] Reutilizar `CartValidator` y `PricingService` del dominio.

**Pruebas clave:** el grupo "Elige 2 guisos" aparece solo con la variante "Combinado"; botón deshabilitado si falta un grupo obligatorio; precio correcto con extras; cambiar de variante limpia selecciones; dos configuraciones distintas generan dos líneas; un modificador inválido enviado a mano → 400 en WEB-02.

---

### WEB-02 (HU-02) · Captura de Datos del Cliente, Consentimiento y Registro de Pedido — 8 SP · Must · ⬜
*Subió de 5 a 8 SP por el aviso de privacidad y el consentimiento (P-27).*

**Como** cliente con un pedido armado, **quiero** ingresar mi nombre y mi número de WhatsApp **para** que el restaurante identifique mi orden, la guarde y me contacte.

**Criterios de aceptación**
- [ ] Formulario con "Nombre completo" y "Número de WhatsApp".
- [ ] Ambos campos marcados como obligatorios.
- [ ] Guardar detalle del pedido, nombre, teléfono y estado `PendienteConfirmacion`.
- [ ] Mensaje: "Para procesar su orden, es necesario ingresar el número de confirmación que acabamos de enviar a su WhatsApp".
- [ ] *(P-27)* El formulario incluye un enlace visible al **Aviso de Privacidad**, que abre en una página o ventana sin perder el pedido armado.
- [ ] *(P-27)* El formulario incluye una **casilla de consentimiento**, desmarcada por defecto, con un texto como: "Acepto el Aviso de Privacidad y autorizo recibir por WhatsApp mensajes relacionados con mi pedido (código de confirmación y aviso de orden lista)."
- [ ] *(P-27)* El botón de envío solo se habilita con la casilla marcada; si se intenta enviar sin marcarla, se indica que es obligatoria.

**Reglas de negocio**
- [ ] Nombre sin caracteres especiales inválidos, mínimo 3 caracteres.
- [ ] Teléfono numérico de 10 dígitos (ajustable por país).
- [ ] No se registra si faltan campos obligatorios.
- [ ] RN-W2.1 *(P-27)*: Sin consentimiento no se registra el pedido ni se envía ningún mensaje por WhatsApp (el backend lo valida también → 400).
- [ ] RN-W2.2 *(P-27)*: Se guarda **evidencia del consentimiento**: teléfono, fecha y hora, versión del aviso aceptado, sucursal y canal.
- [ ] RN-W2.3 *(P-27)*: El consentimiento solo cubre mensajes del pedido (transaccionales). Cualquier mensaje promocional futuro requiere un consentimiento distinto.
- [ ] RN-W2.4 *(P-27)*: El texto del aviso es versionado (campo `version` de `aviso-privacidad.json`); si cambia, se registra la nueva versión y los consentimientos anteriores conservan la versión que aceptaron.

**Tareas técnicas**
- [ ] `POST /public/orders`; backend **recalcula el total** con precios de BD y guarda snapshot.
- [ ] Regex que acepte letras con acentos, ñ, espacios, apóstrofo y guion.
- [ ] Teléfono almacenado en E.164 (`+52XXXXXXXXXX`), lada configurable.
- [ ] Validación en servidor → 400.
- [ ] Página pública `/aviso-de-privacidad` que se genera a partir del archivo **`aviso-privacidad.json`** (P-44 ✅, ver formato abajo).
- [ ] Cargar el JSON con `IOptionsMonitor` para que un cambio en el archivo se aplique **sin redesplegar ni reiniciar**.
- [ ] Validar el JSON al iniciar y al recargar: si falta un campo obligatorio o la estructura es inválida, se conserva la última versión válida y se registra una alerta en el log.
- [ ] `GET /public/privacy-notice` devuelve el contenido para la web; el chatbot (US-01) usa `textoBienvenidaWhatsApp`.
- [ ] El formulario toma el texto de la casilla de `textoConsentimiento`, y `ConsentRecord.NoticeVersion` guarda el campo `version` del JSON (RN-W2.4).

**Formato de `aviso-privacidad.json` (P-44)**

```json
{
  "version": "1.0",
  "fechaActualizacion": "2026-11-10",
  "titulo": "Aviso de Privacidad",
  "responsable": {
    "razonSocial": "",
    "nombreComercial": "",
    "domicilio": "",
    "correoContacto": "",
    "telefonoContacto": ""
  },
  "secciones": [
    { "titulo": "Datos personales que recabamos", "parrafos": ["Nombre y número de WhatsApp."] },
    { "titulo": "Finalidades del tratamiento", "parrafos": ["Identificar y gestionar su pedido, enviarle el código de confirmación y avisarle cuando su orden esté lista."] },
    { "titulo": "Derechos ARCO", "parrafos": ["Para ejercer sus derechos de Acceso, Rectificación, Cancelación u Oposición, escriba a ..."] },
    { "titulo": "Revocación del consentimiento", "parrafos": ["Puede escribir BAJA en nuestro WhatsApp para dejar de recibir avisos."] },
    { "titulo": "Cambios al aviso", "parrafos": ["Cualquier cambio se publicará en esta página."] }
  ],
  "textoConsentimiento": "Acepto el Aviso de Privacidad y autorizo recibir por WhatsApp mensajes relacionados con mi pedido (código de confirmación y aviso de orden lista).",
  "textoBienvenidaWhatsApp": "Consulta nuestro Aviso de Privacidad en {url}"
}
```

| Campo | Obligatorio | Uso |
|---|---|---|
| `version` | Sí | Se guarda en cada consentimiento. **Debe cambiar cada vez que cambie el texto.** |
| `fechaActualizacion` | Sí | Se muestra en la página ("Última actualización"). |
| `titulo` | Sí | Encabezado de la página. |
| `responsable.*` | Sí (`razonSocial`, `domicilio`, `correoContacto`) | Identificación del responsable de los datos. |
| `secciones[]` | Sí, al menos una | Cada sección con su `titulo` y uno o más `parrafos`. |
| `textoConsentimiento` | Sí | Texto de la casilla en WEB-02. |
| `textoBienvenidaWhatsApp` | Sí | Línea del mensaje de bienvenida del bot (US-01); `{url}` se reemplaza por el enlace. |

- **QA/Staging:** se usa el ejemplo de arriba con datos ficticios.
- **Producción:** antes de salir, el negocio o su asesor legal entrega el texto definitivo y se coloca en el archivo.
- [ ] Entidad `ConsentRecord (Phone, AcceptedAt, NoticeVersion, BranchId, Channel)` guardada en la misma transacción que el pedido.
- [ ] Campo `consentAccepted` obligatorio en `POST /public/orders`.
- [ ] *(P-31)* Si al registrar hay precios distintos o artículos agotados respecto a lo que vio el cliente, responder **409** con el detalle; la pantalla muestra los cambios y pide confirmar de nuevo.
- [ ] *(P-26)* Si la sucursal cerró mientras el cliente armaba el pedido, se permite terminarlo (RN-S4); si ya estaba cerrada al entrar, no se muestra el menú.

**Pruebas clave:** "José Ñúñez" válido; "Al" inválido; teléfono de 9 dígitos o con letras inválido; total manipulado desde el cliente se ignora; envío sin consentimiento → 400 y no se manda código; el registro de consentimiento guarda la versión vigente del aviso; el enlace al aviso no borra el pedido armado.

---

### WEB-03 (HU-03) · Validación de Código por WhatsApp y Generación de Orden — 8 SP · Must · ⬜

**Como** sistema, **quiero** enviar un código único al WhatsApp del cliente y validar su captura, **para** confirmar la autenticidad del pedido y enviarlo a caja y cocina.

**Criterios de aceptación**
- [ ] Generar código de 4 dígitos y enviarlo por WhatsApp inmediatamente tras WEB-02.
- [ ] Campo numérico para capturar el código.
- [ ] Código incorrecto → mensaje de error y reintento.
- [ ] Código correcto → estado `Confirmada`.
- [ ] El pedido aparece automáticamente en Caja y Cocina.

**Reglas de negocio**
- [ ] Código de exactamente 4 dígitos, aleatorio (0000–9999).
- [ ] Expiración de 10 minutos; si expira, se solicita uno nuevo.
- [ ] Bloqueo temporal tras 3 intentos fallidos. *(P-20 ✅: se bloquea la **captura del código por 15 minutos**; además, máximo **3 reenvíos** del código por pedido.)*
- [ ] Caja y Cocina solo ven órdenes `Confirmada`.

**Tareas técnicas**
- [ ] Generador criptográfico (`RandomNumberGenerator`); envío con **plantilla de autenticación** de WhatsApp.
- [ ] Input con `inputmode="numeric"` y autocompletado OTP.
- [ ] `POST /public/orders/{id}/verify` → 400 con intentos restantes.
- [ ] Código almacenado como **hash**; `ExpiresAt`; endpoint de reenvío.
- [ ] Bloqueo por pedido y por teléfono + rate limit de reenvíos.
- [ ] *(P-30)* El pedido en `PendienteConfirmacion` expira a los **15 minutos** (proceso en segundo plano → `Expirada`). El código expira a los 10, lo que deja margen para un reenvío.
- [ ] Evento SignalR hacia WEB-05.

**Pruebas clave:** código correcto / incorrecto / expirado; 4.º intento bloqueado; reenvío invalida el código anterior; falla de la API al enviar.

---

### WEB-05 · Tablero de Caja y Cocina en tiempo real — 8 SP · Must · ⬜
*Historia agregada: WEB-03, WEB-04 y US-03 asumen que este módulo existe.*

**Como** personal del restaurante (cajero/cocinero), **quiero** ver los pedidos confirmados en tiempo real **para** cobrarlos y prepararlos.

- [ ] Vista **Cocina**: órdenes `Confirmada` y `EnPreparacion` con platillos, variantes y modificadores; botón "Iniciar preparación".
- [ ] Vista **Caja**: órdenes con total y estado; acciones **"Pagado"** (pago al recoger, P-09), **"Entregada"** y **"Cancelar"** (WEB-06).
- [ ] Actualización en tiempo real (SignalR) con alerta sonora para órdenes nuevas.
- [ ] Autenticación por roles (Cajero, Cocinero).
- [ ] Recibe pedidos de ambos canales (E2 y E3).

---

### WEB-04 (HU-04) · Notificación de Orden Lista para Entrega — 5 SP · Must · ⬜

**Como** personal del restaurante (cajero/cocinero), **quiero** marcar una orden como completada **para** que el sistema avise automáticamente al cliente por WhatsApp.

**Criterios de aceptación**
- [ ] Botón "Orden Lista" / "Finalizar" por pedido en curso.
- [ ] Estado actualizado a `ListaParaEntrega`.
- [ ] Petición a la API de mensajería con texto predefinido.
- [ ] Mensaje: "¡Hola [Nombre]! Tu orden #[ID] está lista. Ya puedes pasar a recogerla."

**Reglas de negocio**
- [ ] Solo desde `Confirmada` o `EnPreparacion`.
- [ ] Si falla el envío: registrar en bitácora y alertar al cajero para vocear manualmente.
- [ ] *(P-43)* Si el cliente tiene una **baja activa**, no se envía el mensaje; el tablero muestra "Cliente sin avisos por WhatsApp — vocear" igual que en una falla de envío.

**Tareas técnicas**
- [ ] Acción en WEB-05 + transición de dominio (inválida → 400).
- [ ] **Plantilla de utilidad** aprobada por Meta con variables `{{1}}` nombre, `{{2}}` orden.
- [ ] Log estructurado, marca `NotificationFailed`, alerta "Vocear cliente"; reintento opcional.

**Pruebas clave:** transición inválida desde `PendienteConfirmacion`; falla simulada de la API → alerta y bitácora.

---

### WEB-06 · Cancelación de Pedidos — 3 SP · Must · ⬜
*Historia agregada por P-32.*

**Como** cajero, **quiero** cancelar un pedido que aún no está listo, **para** resolver errores o pedidos que el cliente ya no quiere, avisándole por WhatsApp.

**Criterios de aceptación**
- [ ] CA1: En el tablero de Caja (WEB-05), los pedidos en `Confirmada` o `EnPreparacion` tienen la acción **"Cancelar"**, que pide un motivo y una confirmación.
- [ ] CA2: Al cancelar, el pedido pasa a `Cancelada`, desaparece de Cocina y se envía al cliente: *"Tu pedido #A-014 fue cancelado. Motivo: {motivo}."*
- [ ] CA3: El cliente puede cancelar **solo antes de confirmar**: con el comando CANCELAR en el chatbot o con un botón "Cancelar pedido" en la pantalla de verificación de la web.

**Reglas de negocio**
- [ ] RN-W6.1: No se puede cancelar un pedido en `ListaParaEntrega`, `Entregada`, `Cancelada` o `Expirada` → 400.
- [ ] RN-W6.2: Solo los roles Cajero, Administrador de sucursal y Administrador general pueden cancelar.
- [ ] RN-W6.3: Se registra quién canceló, cuándo y el motivo.
- [ ] RN-W6.4: Si el cliente tiene la baja activa (P-43) o falla el envío, se muestra la alerta para avisarle en persona.

**Tareas técnicas**
- [ ] `POST /orders/{id}/cancel` con motivo; transición en el dominio.
- [ ] Envío con la plantilla de utilidad "pedido cancelado" (HT-WA).
- [ ] Si el ticket ya se imprimió, marcarlo como cancelado en el tablero de Cocina.

**Pruebas clave:** cancelar en `EnPreparacion` → `Cancelada` + mensaje; cancelar en `ListaParaEntrega` → 400; cocinero intenta cancelar → 403; cliente cancela antes de verificar el código → el pedido no llega a Cocina.

---

## E4 · Impresión de Tickets

### HT-PRN · Cola de impresión y registro de dispositivos — 5 SP · Must · ⬜
*Habilitador técnico. Necesario para RN2.1 y RN3.2 y para evitar doble impresión.*

- [ ] Entidad `PrintJob` (`Pendiente → Enviado → Impreso` / `Fallido`) con `OrderId`, dispositivo destino, intentos y payload.
- [ ] Entidad `PrintDevice`: sucursal, teléfono Android con impresora Bluetooth (P-47), ancho (58 mm por defecto; 80 mm configurable), estaciones que atiende (Mostrador, Cocina o ambas).
- [ ] **Ruteo por estación (P-18 ✅):** cada sucursal tiene las estaciones **Mostrador** y **Cocina**. Al confirmarse un pedido se crea **un `PrintJob` por estación** (no por dispositivo). Si varios dispositivos atienden la misma estación, el primero que **reclama** el trabajo lo imprime y los demás lo ignoran (reclamo atómico en BD), así no hay tickets duplicados.
- [ ] **Un equipo por sucursal (P-48 ✅):** el teléfono de cada sucursal atiende **ambas estaciones**, por lo que imprime **2 tickets por pedido** (uno para Mostrador y uno para Cocina). El administrador de sucursal puede configurarlo para imprimir solo 1.
- [ ] **Patrón Outbox**: `PrintJob` guardado en la misma transacción que el pedido y publicado asíncronamente (RN1.2).
- [ ] `GET /print-jobs?status=Pendiente` y `POST /print-jobs/{id}/ack` con token por dispositivo.
- [ ] Hub SignalR que emite `NuevoPedidoGuardado` solo a dispositivos de la sucursal.

---

### PRN-01 (US-01) · Generación del Formato de Ticket (Layout) — 5 SP · Must · ⬜

**Como** sistema, **quiero** generar un ticket estructurado (58 mm y 80 mm) en cuanto un pedido se guarda, **para** que sea legible y útil en cocina y mostrador.

**Criterios de aceptación**
- [ ] AC1: Backend genera el layout en texto plano o comandos ESC/POS al guardar el pedido. *(P-14: "guardado" = el pedido pasa a `Confirmada`.)*
- [ ] AC2: Contiene Fecha/Hora, Número de Orden, Nombre del Cliente, artículos con modificadores y Total. *(Se agregan: leyenda "IVA incluido: $X" (P-08), "Pago al recoger" (P-09) y canal de origen WhatsApp/Web (P-13).)*
- [ ] AC3: Respeta el ancho máximo (32 caracteres en 58 mm / 48 en 80 mm), cantidades a la izquierda y precios a la derecha.

**Reglas de negocio**
- [ ] RN1.1: Textos largos se truncan o saltan de línea con indentación sin romper columnas.
- [ ] RN1.2: Generación síncrona al guardar; envío a cola de impresión asíncrono.

**Tareas técnicas**
- [ ] Servicio `TicketRenderer`: modelo de ticket → texto plano | bytes ESC/POS.
- [ ] Utilidad de columnas/padding según ancho de `PrintDevice`.
- [ ] Word-wrap con indentación de 2–3 espacios para modificadores.
- [ ] Configurar **code page ESC/POS** (CP850/CP858) para Ñ y acentos.
- [ ] *(P-23 ✅)* **Corte automático de papel** al final del ticket. Logotipo, código QR y apertura de cajón quedan fuera de este alcance.

**Pruebas clave:** snapshot tests 58/80 mm; nombre de 60 caracteres; modificador con extra; montos de $1,234.50 alineados; caracteres "Ñ" y acentos.

---

### PRN-02a (US-02) · Agente PC: conexión e impresión silenciosa — 8 SP · ~~Must~~ Won't · 🚫 FUERA DE ALCANCE
> *P-47: las sucursales imprimirán solo con Android + Bluetooth. Se conserva como referencia.*

**Como** operador en mostrador usando una PC, **quiero** que mi computadora reciba y mande a imprimir automáticamente los nuevos pedidos a mi impresora térmica (USB/Red), **para** agilizar la preparación sin intervención manual.

**Criterios de aceptación**
- [ ] AC1: Cliente/agente local con conexión en tiempo real (WebSockets/SignalR).
- [ ] AC2: Al recibir `NuevoPedidoGuardado`, enviarlo a la impresora predeterminada.
- [ ] AC3: Impresión silenciosa, sin diálogo del sistema operativo.

**Tareas técnicas**
- [ ] Agente local con cliente SignalR y reconexión automática.
- [ ] Impresión RAW: USB vía spooler del SO o red (TCP 9100).
- [ ] `ack` al servidor tras imprimir.

> **Decisión técnica (P-16 ✅):** se desarrolla un **agente propio en .NET** (Worker Service en la bandeja del sistema, Windows y Mac). Se descartan QZ Tray (licencia comercial) y Chrome `--kiosk-printing` (solo HTML, sin ESC/POS ni corte).

---

### PRN-02b (US-02) · Agente PC: recuperación y alertas — 5 SP · ~~Must~~ Won't · 🚫 FUERA DE ALCANCE
> *P-47: las sucursales imprimirán solo con Android + Bluetooth. Se conserva como referencia.*

**Reglas de negocio**
- [ ] RN2.1: Al reconectarse, consultar e imprimir los pedidos "No Impresos" recibidos durante la desconexión.
- [ ] RN2.2: Si la impresora marca error (sin papel, desconectada), mostrar alerta visual o sonora.

**Tareas técnicas**
- [ ] Al reconectar: `GET /print-jobs?status=Pendiente` y procesar en orden; `ack` evita duplicados.
- [ ] Lectura de estado (`DLE EOT` / spooler); notificación del SO + sonido; job a `Fallido` con reintento.
- [ ] Log local + envío al servidor ("Impresora no encontrada", "Desconexión WebSocket").

**Pruebas clave:** desconectar red → entran 3 pedidos → reconectar → 3 tickets impresos **una sola vez**; quitar papel → alerta.

---

### PRN-03a (US-03) · App Android: impresora Bluetooth e impresión — 8 SP · Must · ⬜
*Sube de Should a **Must**: es el único medio de impresión (P-47).*

**Como** operador móvil, **quiero** que la app en mi celular detecte los pedidos entrantes y los mande a imprimir por Bluetooth a mi impresora portátil, **para** operar sin depender de una computadora.

**Criterios de aceptación**
- [ ] AC1: App conectada al servidor escuchando nuevos pedidos.
- [ ] AC2: Transmitir comandos de impresión por Bluetooth a la impresora emparejada.
- [ ] AC3: Configurar la impresora Bluetooth "Activa".

**Tareas técnicas**
- [ ] App nativa o multiplataforma (MAUI / Flutter / React Native) con cliente SignalR.
- [ ] Bluetooth Classic (SPP).
- [ ] Pantalla de impresoras emparejadas, "Imprimir prueba" y preferencia persistida.

---

### PRN-03b (US-03) · App Android: segundo plano, retención y alertas — 8 SP · Must · ⬜
*Sube de Should a **Must** (P-47). Absorbe las reglas de recuperación que antes tenía el agente de PC (RN2.1).*

**Reglas de negocio**
- [ ] RN3.1: Ejecutar en segundo plano o, en su defecto, push crítica que active la impresión al abrirse.
- [ ] RN3.2: Si se pierde el emparejamiento, retener el ticket como "Pendiente de Impresión" y emitir alerta sonora.

**Tareas técnicas**
- [ ] **Foreground service** de Android con notificación persistente + exclusión de optimización de batería.
- [ ] Respaldo con FCM de alta prioridad.
- [ ] Cola local + `PrintJob` en `Pendiente`; alarma sonora y reintento al reconectar.
- [ ] *(Antes RN2.1 del agente PC)* Si el teléfono pierde internet y se reconecta, consulta `GET /print-jobs?status=Pendiente` e imprime en orden los pedidos que entraron durante la desconexión; el `ack` evita imprimirlos dos veces.
- [ ] *(Antes RN2.2 del agente PC)* Si la impresora reporta error (sin papel, apagada, fuera de alcance), alerta visual y sonora en el teléfono; el trabajo queda `Fallido` con opción de reintentar.
- [ ] Log local + envío al servidor de fallos de hardware ("Impresora no encontrada", "Bluetooth desconectado", "Desconexión WebSocket").

**Pruebas clave:** apagar el wifi, entrar 3 pedidos, reconectar → 3 pedidos impresos una sola vez (2 tickets cada uno); quitar el papel → alerta; apagar la impresora → ticket retenido y alarma; app en segundo plano 30 min → sigue imprimiendo.

> **Nota:** una PWA no sirve aquí (Web Bluetooth no funciona en segundo plano en Android y no existe en iOS Safari). **Decisión (P-17 ✅): solo Android en este alcance; iOS queda como historia futura.**

---

### PRN-04 · Reimpresión de tickets — 2 SP · Should · ⬜
*Historia agregada por P-22.*

**Como** cajero o cocinero, **quiero** reimprimir el ticket de un pedido, **para** reponerlo si se perdió, se manchó o falló la impresión.

**Criterios de aceptación**
- [ ] CA1: En el tablero de Caja y Cocina (WEB-05), cada pedido tiene un botón **"Reimprimir"**, que permite elegir la estación (Mostrador o Cocina).
- [ ] CA2: El ticket reimpreso lleva la leyenda **"REIMPRESIÓN"** con fecha y hora.

**Reglas de negocio**
- [ ] RN-R1: Se registra quién reimprimió, cuándo y para qué estación.
- [ ] RN-R2: Solo se reimprimen pedidos de la sucursal del usuario.

**Pruebas clave:** reimprimir crea un nuevo `PrintJob` marcado como reimpresión; la leyenda aparece en el ticket; usuario de otra sucursal → 404.

---

## Definición de Hecho (DoD)

> **P-12 ✅:** se unifica en una **DoD General** que aplica a todas las historias, más **anexos por épica** con los criterios particulares. Si un anexo contradice la DoD General, prevalece el criterio más estricto.

### DoD General (aplica a todas las historias)
1. **Criterios cumplidos:** todos los criterios de aceptación y reglas de negocio de la historia están implementados.
2. **Pruebas:** unitarias de reglas de negocio con **cobertura mínima del 80%**; pruebas de integración de endpoints con HTTP 200/201 en éxito y 400/403/404 con mensaje descriptivo en errores; prueba de aislamiento entre sucursales.
3. **Persistencia:** los datos se guardan correctamente en la base de datos (relaciones, montos, estados).
4. **UI/UX:** responsiva (escritorio y móvil), con indicadores de carga y alertas claras de éxito o error.
5. **Revisión de código:** Pull Request aprobado por al menos un desarrollador **Senior**.
6. **Despliegue:** desplegado y probado en Staging/QA sin regresiones.
7. **Requisitos no funcionales:** cumple los valores de la sección "Requisitos no funcionales" que le apliquen.
8. **Aceptación:** aprobada por el Product Owner y documentada técnicamente.

Los anexos siguientes conservan el texto original de cada épica; sus criterios **se suman** a la DoD General.

### DoD — E1 · Administración del Menú
1. **Código y Arquitectura:** cumple estándares del equipo; clases de Dominio C# mapeadas con Entity Framework Core.
2. **Pruebas:** unitarias de reglas de negocio con cobertura mínima del 80%; integración de endpoints con HTTP 200/201 en éxito y HTTP 400 con mensaje descriptivo en violaciones.
3. **Persistencia:** cambios desde la UI reflejados correctamente en la BD relacional.
4. **UI/UX:** responsiva (Desktop/Mobile), spinners durante peticiones, alertas claras (Toasts/Modales).
5. **Code Review:** PR aprobado por al menos un desarrollador Senior.
6. **Despliegue:** desplegado y probado en Staging/QA sin regresiones.

### DoD — E2 · Chatbot de WhatsApp
1. Código del webhook y lógica de negocio completado y en la rama principal.
2. Pruebas unitarias de cálculo de precios y de la máquina de estados del bot aprobadas.
3. Verificado en QA simulando el entorno real con la API de WhatsApp Business.
4. Sin errores de sintaxis o formato en los mensajes devueltos por WhatsApp.
5. Aprobado por el Product Owner y documentado técnicamente.

### DoD — E3 · Pedidos Web y Notificaciones
1. **Código Funcional:** cumple todos los criterios de aceptación.
2. **Pruebas Superadas:** unitarias e integración (cálculo de totales y llamadas a la API de mensajería).
3. **Persistencia Verificada:** relaciones, montos, estados, nombres y teléfonos guardados correctamente.
4. **Diseño UI/UX:** responsiva; el cotizador dinámico actualiza sin retrasos.
5. **Revisión de Código:** PR aprobado por al menos otro ingeniero.
6. **Despliegue:** desplegado en Staging (QA) sin conflictos.

### DoD — E4 · Impresión de Tickets
1. **Código Completo:** generación ESC/POS o texto y clientes de conexión (WebSocket/Push) desarrollados.
2. **Pruebas Unitarias/Integración:** se simuló el guardado de un pedido y se comprobó la emisión del evento de impresión en el bus de mensajes.
3. **Pruebas de Hardware:**
   - ~~Impresión silenciosa en PC Windows/Mac con impresora USB genérica.~~ *(Fuera de alcance por P-47.)*
   - Impresión Bluetooth desde Android hacia impresora térmica de 58 mm.
4. **Manejo de Errores:** logs registran fallos de hardware ("Impresora no encontrada", "Desconexión WebSocket").
5. **Aceptación de Producto:** aprobado por el PO tras demostración en vivo.

> **Hardware de pruebas y operación (P-47):** impresora térmica Bluetooth de 58 mm y teléfono Android (uno por sucursal; al menos uno para QA).

---

## Preguntas abiertas para el Product Owner

| ID | Tema | Pregunta | Afecta | Estado |
|---|---|---|---|---|
| P-01 | Borrado de categorías | RN1.2 dice "error **o bien** Desactivar". Recomendación: ambos (error + botón "Desactivar"). **Respuesta (2026-11-10): de acuerdo con la propuesta.** Mensaje + botón "Desactivar categoría" (HU-1). | HU-1 | ✅ Resuelta |
| P-02 | Sucursales | RN1.1 menciona sucursal pero no hay historia de gestión de sucursales. ¿Ya existe? **Respuesta (2026-11-10): todo debe diferenciarse por sucursal.** Se agrega SUC-01 y la sección "Aislamiento por sucursal"; HT-0 sube a 8 SP. | HT-0, SUC-01 | ✅ Resuelta |
| P-03 | Publicación | RN3.2 habla de "publicar" pero no hay historia de publicar/despublicar platillos. **Respuesta (2026-11-10): de acuerdo con la propuesta.** Sin paso manual de publicar; visibilidad automática (HU-3 RN3.5). | HU-3 | ✅ Resuelta |
| P-04 | Reutilizar grupos | ¿Los grupos de modificadores se comparten entre platillos o son propios de cada uno? Cambia el modelo de datos. **Respuesta (2026-11-10): sí, se reutilizan.** HU-4 agrega CA4–CA6. | HU-4 | ✅ Resuelta |
| P-05 | MaxSelections | ¿Se permite `MaxSelections` = 0 o debe ser ≥ 1? **Respuesta (2026-11-10): de acuerdo con la propuesta.** MaxSelections ≥ 1 (HU-4 RN4.4). | HU-4 | ✅ Resuelta |
| P-06 | Inventario | ¿Basta con el toggle "Agotado" (HU-6) o se requiere control de existencias real? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Solo "Agotado"; control de existencias queda como épica futura. | HU-6, US-01 | ✅ Resuelta |
| P-07 | Formato de menú en WhatsApp | Recomendación: listas interactivas (máx. 10 filas por mensaje → navegación en 2 pasos) o WhatsApp Flows; el Catálogo de Meta no maneja bien variantes/modificadores. **Respuesta (2026-11-10): listas interactivas; primero se elige la categoría y luego el platillo.** Ver tareas de US-01. | US-01 | ✅ Resuelta |
| P-45 | Nombres largos en WhatsApp | El título de cada fila de una lista de WhatsApp admite solo 24 caracteres. ¿Se limita el nombre de categorías y platillos a 24 caracteres en HU-1/HU-2, o se agrega un campo opcional "Nombre corto para WhatsApp"? Si no, los nombres largos se truncarán (ej. "Chilaquiles Verdes con…"). **Respuesta (2026-11-10): opción 2, campo opcional "Nombre corto para WhatsApp"** en categorías y platillos (HU-1 CA5, HU-2 CA4, RN-NC1 a RN-NC4). | HU-1, HU-2, US-01 | ✅ Resuelta |
| P-46 | Nombres largos en variantes y opciones | Las variantes (US-02b) y las opciones de modificadores también se muestran como filas de lista en WhatsApp (24 caracteres). **Propuesta:** limitar sus nombres a 24 caracteres en HU-3 y HU-5, ya que suelen ser cortos ("1/2 Litro", "Huevo"). **Respuesta (2026-11-10): sí, máximo 24 caracteres** (RN3.4 y RN5.4). | HU-3, HU-5 | ✅ Resuelta |
| P-08 | Impuestos | ¿Los precios incluyen IVA o se suma al final? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Los precios ya incluyen IVA; el ticket y el resumen muestran el IVA desglosado solo como información (US-03, PRN-01). | US-03, WEB-01 | ✅ Resuelta |
| P-09 | Pago | No hay historia de pago. ¿Se paga al recoger? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Se paga al recoger, en caja (WEB-05 marca "Pagado"). | E2, E3 | ✅ Resuelta |
| P-10 | Editar carrito | No hay criterios para quitar artículos o cambiar cantidades antes de cerrar (sugerido: Should, ~3 SP). *En la web ya lo cubre WEB-01b (CA6); queda abierta solo para el chatbot.* **Respuesta (2026-11-10): de acuerdo con la propuesta.** Comando CARRITO para ver, quitar o cambiar cantidades. Nueva US-04 (3 SP, Should). | US-02a | ✅ Resuelta |
| P-11 | Sesión abandonada | ¿Cuánto tiempo se conserva un carrito inactivo en el chatbot? **Resuelta por P-30: 30 minutos de inactividad.** | HT-WA | ✅ Resuelta |
| P-12 | DoD | Hay 4 DoD con criterios distintos (cobertura, revisor Senior vs cualquier ingeniero). ¿Se unifican? **Respuesta (2026-11-10): de acuerdo con la propuesta.** DoD General + anexos por épica. | Todas | ✅ Resuelta |
| P-13 | **Canales E2 vs E3** | ¿Conviven el chatbot y el pedido web, o E3 reemplaza a E2? **Respuesta (2026-11-10): conviven ambos canales.** Se mantienen E2 y E3 completas; ver "Reglas por convivencia de canales". | E2, E3 | ✅ Resuelta |
| P-14 | Estado "Guardado" | No existe en el modelo. Propuesta: imprimir al pasar a `Confirmada` (evita imprimir pedidos web no verificados). **Respuesta (2026-11-10): de acuerdo con la propuesta.** Se imprime al pasar a Confirmada (PRN-01). | PRN-01 | ✅ Resuelta |
| P-15 | Variantes/modificadores en web | WEB-01 solo maneja platillo × cantidad, pero E1 define variantes y modificadores y PRN-01 los imprime. ¿Falta historia equivalente a US-02b para web (~8 SP)? **Respuesta (2026-11-10): sí.** Se agrega WEB-01b (8 SP). | WEB-01, WEB-01b | ✅ Resuelta |
| P-16 | Agente de PC | ¿Agente nativo .NET (recomendado) o QZ Tray (licencia)? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Agente propio en .NET (PRN-02a). | PRN-02a | ✅ Resuelta · sin efecto por P-47 |
| P-17 | iOS | ¿iOS queda fuera de este alcance? La DoD de E4 solo exige Android. **Respuesta (2026-11-10): de acuerdo con la propuesta.** Solo Android; iOS como historia futura. | PRN-03a/b | ✅ Resuelta |
| P-18 | Ruteo de impresión | ¿Ticket para cocina y otro para mostrador? Si hay PC y celular conectados, ¿imprimen ambos? Propuesta: impresoras asignadas por estación. **Respuesta (2026-11-10): de acuerdo con la propuesta.** Un ticket por estación (Mostrador y Cocina); el primer dispositivo que reclama el trabajo lo imprime (HT-PRN). | HT-PRN | ✅ Resuelta |
| P-19 | Plantillas Meta | Código de verificación y "orden lista" requieren plantillas aprobadas por Meta (autenticación y utilidad) con costo por mensaje. Iniciar trámite en sprint 2. **Respuesta (2026-11-10): de acuerdo, se inicia el trámite en el sprint 2.** Ver HT-WA. | WEB-03, WEB-04, HT-WA | ✅ Resuelta |
| P-20 | Regla de bloqueo | "Bloquear el **envío** tras 3 intentos fallidos de **validación**": ¿se bloquea la captura o el reenvío? ¿Por cuánto tiempo? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Bloqueo de captura por 15 minutos tras 3 fallos; máximo 3 reenvíos por pedido (WEB-03). | WEB-03 | ✅ Resuelta |
| P-21 | Número de orden | WEB-04 usa `#[ID]`. Propuesta: usar el número diario legible (ej. `A-014`). **Respuesta (2026-11-10): de acuerdo con la propuesta.** Número diario legible con prefijo por sucursal, ej. A-014 (SUC-01, US-03, WEB-04). | WEB-04, US-03 | ✅ Resuelta |
| P-22 | Reimpresión | No hay historia para reimprimir un ticket desde WEB-05 (sugerido: Should, ~2 SP). **Respuesta (2026-11-10): de acuerdo con la propuesta.** Botón "Reimprimir" con leyenda REIMPRESIÓN. Nueva PRN-04 (2 SP, Should). | E4 | ✅ Resuelta |
| P-23 | Extras del ticket | ¿Logo, corte automático, apertura de cajón, código QR? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Solo corte automático; logo, QR y cajón fuera de alcance (PRN-01). | PRN-01 | ✅ Resuelta |
| P-24 | Acceso al backoffice | No hay historia de login, usuarios ni roles para el "administrador" (WEB-05 sí asume roles). ¿Se agrega historia (~5 SP)? **Respuesta (2026-11-10): sí, con los 4 roles propuestos.** Se agrega AUTH-01. | E1, AUTH-01 | ✅ Resuelta |
| P-25 | Sucursal por canal | ¿Cómo se sabe a qué sucursal va un pedido? **Respuesta (2026-11-10): por el número de WhatsApp; cada sucursal tiene un número diferente.** Ver "Aislamiento por sucursal" y HT-WA. Queda abierta solo la parte web (P-25b). | E2, SUC-01 | ✅ Resuelta |
| P-25b | Sucursal en el canal web | ¿Cómo elige el cliente la sucursal en la web? **Respuesta (2026-11-10): la web muestra las sucursales activas con sus números de WhatsApp.** Se agrega WEB-00 (3 SP). | E3, WEB-00 | ✅ Resuelta |
| P-40 | Cuenta de Meta | ¿Todos los números de las sucursales estarán en **una sola** cuenta de WhatsApp Business (WABA)? **Recomendado:** sí; así se comparten las plantillas aprobadas, un solo webhook y una sola facturación. **Respuesta (2026-11-10): de acuerdo, una sola WABA.** | HT-WA, P-19 | ✅ Resuelta |
| P-26 | Horario de atención | Ninguna historia impide pedir con el restaurante cerrado. ¿Se rechazan pedidos fuera de horario? ¿Dónde se configura? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Horario configurable por sucursal; fuera de horario no se aceptan pedidos (SUC-01 sube a 8 SP; US-01, WEB-00). | E2, E3 | ✅ Resuelta |
| P-27 | Privacidad y opt-in | Se guardan nombre y teléfono: se requiere aviso de privacidad (LFPDPPP) y, por política de Meta, consentimiento explícito para enviar mensajes iniciados por el negocio. ¿Se agrega checkbox de aceptación en WEB-02? **Respuesta (2026-11-10): sí, aviso de privacidad y casilla de consentimiento en el formulario web.** WEB-02 sube a 8 SP. | WEB-02, WEB-03, WEB-04 | ✅ Resuelta |
| P-43 | Privacidad en el chatbot | En WhatsApp el cliente inicia la conversación, así que puede recibir respuestas; pero también se guardan su nombre y teléfono. ¿Se envía el enlace al Aviso de Privacidad en el mensaje de bienvenida (US-01)? ¿Se acepta un comando "BAJA" para dejar de recibir mensajes? **Respuesta (2026-11-10): de acuerdo.** US-01 agrega AC4, AC5 y RN-P1 a RN-P3; WEB-04 respeta la baja. | US-01, WEB-04 | ✅ Resuelta |
| P-44 | Texto legal del aviso | ¿Quién redacta el Aviso de Privacidad (razón social del responsable, finalidades, derechos ARCO, contacto)? El equipo implementa la página, pero el texto debe venir del negocio o de su asesor legal **antes del sprint 4** (lo usa la bienvenida del chatbot, US-01; antes era sprint 7). **Respuesta (2026-11-10): el aviso será configurable mediante el archivo `aviso-privacidad.json`** (formato en WEB-02). El desarrollo ya no depende del texto legal: QA usa contenido de ejemplo. **Pendiente para producción:** el negocio entrega el texto definitivo para el JSON. | WEB-02, US-01 | ✅ Resuelta |
| P-28 | Líneas de pedido | WEB-01 maneja "cantidad por platillo", pero con variantes/modificadores el mismo platillo puede pedirse con configuraciones distintas (1 pozole grande de puerco + 1 chico de pollo). Propuesta: carrito por **líneas** (platillo + variante + modificadores + cantidad). **Respuesta (2026-11-10): sí, carrito por líneas.** Ver "Modelo de pedido unificado". | WEB-01, US-02a | ✅ Resuelta |
| P-29 | Reglas de carrito inconsistentes | E2 exige "≥ 1 artículo"; E3 exige "total > $0.00". Como se permiten precios de $0.00 (RN3.1, RN5.1), un pedido de solo artículos gratis pasa en E2 y no en E3. El límite de 99 unidades solo existe en E3. ¿Se unifican? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Mismas reglas en ambos canales: al menos 1 artículo, máximo 99 unidades por línea y se permite total $0.00 (US-02a, WEB-01). | US-02a, WEB-01 | ✅ Resuelta |
| P-30 | Expiración de pedidos | ¿Qué pasa con un pedido web en `PendienteConfirmacion` cuyo código nunca se valida, o un carrito de chat abandonado? ¿Después de cuánto tiempo pasa a `Expirada`? **Respuesta (2026-11-10): de acuerdo con la propuesta.** El pedido web sin verificar expira a los 15 minutos; el carrito del chatbot, a los 30 minutos de inactividad (HT-WA, WEB-03). | WEB-02, WEB-03, HT-WA | ✅ Resuelta |
| P-31 | Cambios durante el pedido | Si un platillo se agota o cambia de precio entre que el cliente lo agrega y confirma: ¿se quita, se respeta el precio anterior o se avisa y recalcula? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Al cerrar se avisa al cliente y se recalcula con los precios y la disponibilidad actuales (US-03, WEB-02). | US-03, WEB-02, WEB-03 | ✅ Resuelta |
| P-32 | Cancelaciones | Existe el estado `Cancelada` pero ninguna historia lo usa. ¿Quién puede cancelar (cliente, caja) y hasta qué estado? ¿Se avisa al cliente? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Caja puede cancelar hasta antes de ListaParaEntrega, con aviso al cliente; el cliente solo puede cancelar antes de confirmar. Nueva WEB-06 (3 SP). | E2, E3 | ✅ Resuelta |
| P-33 | Conversación fuera de flujo | ¿Qué responde el bot ante texto no reconocido? ¿Hay comandos "Cancelar", "Ayuda" o "Hablar con una persona"? **Respuesta (2026-11-10): de acuerdo con la propuesta.** El bot responde con ayuda y los comandos MENÚ, CANCELAR y BAJA; sin atención humana por ahora (HT-WA). | E2 | ✅ Resuelta |
| P-34 | Ingredientes incluidos | HU-2 registra ingredientes "para informar al cliente", pero US-01, WEB-01 y el ticket solo muestran descripción. ¿Dónde se muestran? ¿Se relacionan con opciones tipo "Sin cebolla"? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Ingredientes en el detalle del platillo (web y bot), no en el ticket ni vinculados a modificadores. | HU-2, US-01, WEB-01 | ✅ Resuelta |
| P-35 | POS externo | US-03 AC4 menciona "POS" y HU-3 tiene SKU. ¿Existe un POS externo con el que haya que integrarse, o WEB-05 es el POS? **Respuesta (2026-11-10): de acuerdo con la propuesta.** Sin POS externo; WEB-05 funciona como POS; SKU opcional. | US-03, HU-3 | ✅ Resuelta |
| P-36 | Teléfonos / países | ¿Solo México? WhatsApp entrega números mexicanos como `521…` y se envían como `52…`; hay que normalizar para que E2 y E3 identifiquen al mismo cliente. **Respuesta (2026-11-10): de acuerdo con la propuesta.** Solo México (+52); se normaliza el formato 521… de WhatsApp (HT-WA, WEB-02). | WEB-02, HT-WA | ✅ Resuelta |
| P-38 | Menú por sucursal | ¿Cada sucursal tiene un menú propio o es común? **Respuesta (2026-11-10): el menú es igual para todas las sucursales.** El menú es global; RN1.1 cambia a unicidad global; HU-6 pasa a disponibilidad por sucursal. | E1, HU-1, HU-6 | ✅ Resuelta |
| P-41 | Precios por sucursal | ¿Los precios son iguales en todas las sucursales? **Respuesta (2026-11-10): sí, los precios son los mismos para todas las sucursales.** Los precios viven en el menú global (HU-3, HU-5); no se requiere tabla de precios por sucursal. | HU-3, HU-5 | ✅ Resuelta |
| P-42 | Agotado por sucursal | ¿Un platillo agotado en una sucursal se oculta solo en esa sucursal? **Respuesta (2026-11-10): sí, funciona como se describe en HU-6.** | HU-6 | ✅ Resuelta |
| P-39 | Roles por sucursal | ¿Existen dos niveles de administrador (general y de sucursal)? ¿Un cajero o cocinero puede trabajar en varias sucursales? **Respuesta (2026-11-10): roles aprobados** (Administrador general, Administrador de sucursal, Cajero, Cocinero; cajero y cocinero pueden tener varias sucursales). | AUTH-01 | ✅ Resuelta |
| P-37 | Requisitos no funcionales | Faltan valores concretos: desfase máximo de precios ("tiempo real"), tiempo de respuesta del bot, pedidos simultáneos esperados, disponibilidad y monitoreo. **Respuesta (2026-11-10): de acuerdo con la propuesta.** Valores en la sección "Requisitos no funcionales". | Todas | ✅ Resuelta |
| P-47 | Medio de impresión | ¿Las sucursales imprimirán solo con teléfono Android e impresora Bluetooth? **Respuesta (2026-11-10): se usará impresora Bluetooth y teléfono Android; sin preferencia sobre PC → se adopta la recomendación: solo Android.** PRN-03a/b pasan a Must; PRN-02a/b fuera de alcance; P-16 queda sin efecto. | E4 | ✅ Resuelta |
| P-48 | Equipos por sucursal | ¿Cuántos teléfonos con impresora habrá por sucursal? **Respuesta (2026-11-10): sin preferencia → se adopta la recomendación: uno por sucursal**, que imprime 2 tickets por pedido (Mostrador y Cocina), configurable a 1. | HT-PRN | ✅ Resuelta |

### Prioridad de respuesta

| Urgencia | Preguntas | Motivo |
|---|---|---|
| 🔴 Antes del sprint 1 | ~~P-13~~, ~~P-02~~, ~~P-25~~, ~~P-38~~, ~~P-41~~, ~~P-42~~, ~~P-39~~, ~~P-24~~, ~~P-04~~, ~~P-28~~ — **todas resueltas** |
| 🟠 Antes del sprint 2 (adicional) | ~~P-40~~, ~~P-25b~~ | Resueltas. | Definen el alcance y el modelo de datos base (HT-0). |
| 🟠 Antes del sprint 2 | ~~P-19~~, ~~P-27~~, ~~P-07~~, ~~P-15~~, ~~P-43~~, ~~P-44~~ (JSON configurable), ~~P-45~~, ~~P-46~~ | El trámite con Meta tarda; el consentimiento afecta los formularios. |
| 🟡 Antes del sprint 4 | ~~P-08, P-09, P-26, P-29, P-30, P-31, P-32, P-33, P-14, P-21, P-36, P-37~~ — **todas resueltas** | Reglas del flujo de pedido. |
| 🟢 Antes del sprint 6 | ~~P-16, P-17, P-18, P-20, P-22, P-23, P-35, P-01, P-03, P-05, P-06, P-10, P-12, P-34~~ — **todas resueltas** | Impresión y detalles menores. |

---

## Historial de cambios

| Fecha | Cambio |
|---|---|
| 2026-11-10 | Creación del backlog con E1 (HU-1 a HU-5) + HT-0. |
| 2026-11-10 | Se agrega E2 (US-01 a US-03) + HT-WA + HU-6; US-02 dividida en US-02a/b. |
| 2026-11-10 | Se agrega E3 (WEB-01 a WEB-04) + WEB-05; modelo de pedido unificado. |
| 2026-11-10 | Se agrega E4 (PRN-01 a PRN-03) + HT-PRN; PRN-02 y PRN-03 divididas en a/b. |
| 2026-11-10 | Backlog consolidado en `BACKLOG.md`. |
| 2026-11-10 | Análisis transversal: se agregan preguntas P-24 a P-37 y prioridad de respuesta. |
| 2026-11-10 | P-13 resuelta: conviven ambos canales. Se agregan reglas por convivencia de canales. |
| 2026-11-10 | P-02 resuelta: sistema multi-sucursal. Se agrega SUC-01 (5 SP), HT-0 sube a 8 SP, total 154 SP. Se replanifica a 8 sprints (se elimina la sobrecarga del sprint 4). Nuevas preguntas P-38 y P-39. |
| 2026-11-10 | P-25 resuelta: cada sucursal tiene su propio número de WhatsApp. Se actualizan HT-WA (enrutamiento por número), SUC-01 y reglas de aislamiento. Nuevas preguntas P-25b y P-40. |
| 2026-11-10 | P-25b resuelta: la web muestra las sucursales activas con su WhatsApp → nueva WEB-00 (3 SP). P-38 resuelta: menú común para todas las sucursales → menú global, RN1.1 ajustada, HU-6 por sucursal. Total 157 SP. Nuevas preguntas P-41 y P-42. |
| 2026-11-10 | P-41 resuelta: precios iguales en todas las sucursales. P-42 resuelta: "Agotado" por sucursal confirmado. Sin cambios en SP. |
| 2026-11-10 | P-24 y P-39 resueltas: roles aprobados → nueva AUTH-01 (5 SP). P-04 resuelta: grupos de modificadores reutilizables (HU-4 CA4–CA6). P-28 resuelta: carrito por líneas. Total 162 SP; replanificación de sprints. P-15 marcada como prioritaria. |
| 2026-11-10 | P-15 resuelta: se agrega WEB-01b (8 SP). Total 170 SP. Replanificación a 9 sprints: todo lo Must termina en el sprint 8; el sprint 9 es impresión Android (Should). |
| 2026-11-10 | P-27 resuelta: aviso de privacidad y casilla de consentimiento en el formulario web. WEB-02 sube de 5 a 8 SP (total 173). WEB-02 pasa al sprint 7 y HT-PRN al 6. Nuevas preguntas P-43 y P-44. |
| 2026-11-10 | P-07 resuelta: menú de WhatsApp con listas interactivas en 2 pasos (categoría → platillo), con paginación y límites de WhatsApp. Sin cambio en SP. Nueva pregunta P-45. |
| 2026-11-10 | P-45 resuelta: campo opcional "Nombre corto para WhatsApp" en categorías y platillos (HU-1, HU-2, US-01). Se absorbe en la estimación actual. Nueva pregunta P-46. |
| 2026-11-10 | P-46 resuelta: nombres de variantes y opciones de modificadores limitados a 24 caracteres (RN3.4, RN5.4). Sin cambio en SP. |
| 2026-11-10 | P-40 resuelta (una sola WABA), P-19 resuelta (trámite de plantillas en sprint 2), P-43 resuelta (aviso de privacidad en bienvenida y comando "BAJA"). Cambios en HT-WA, US-01 y WEB-04. P-44 adelanta su fecha límite al sprint 4. Sin cambio en SP. |
| 2026-11-10 | Resueltas P-08, P-09, P-14, P-21, P-26, P-29, P-30, P-31, P-32, P-33, P-36 y P-37 según las propuestas. SUC-01 sube a 8 SP (horarios), nueva WEB-06 Cancelación (3 SP), nueva sección de Requisitos no funcionales. WEB-01 ajusta su criterio de habilitación (≥ 1 artículo). Total 179 SP. |
| 2026-11-10 | P-44 resuelta: el Aviso de Privacidad se configura con `aviso-privacidad.json` (formato y reglas en WEB-02). Sin cambio en SP. Pendiente para producción: texto legal definitivo. |
| 2026-11-10 | Resueltas las 14 preguntas restantes según las propuestas (P-01, P-03, P-05, P-06, P-10, P-12, P-16, P-17, P-18, P-20, P-22, P-23, P-34, P-35). Nuevas US-04 (3 SP) y PRN-04 (2 SP), ambas Should en el sprint 9. DoD General unificada. Total 184 SP. **No quedan preguntas abiertas.** |
| 2026-11-10 | P-47 y P-48 resueltas: impresión solo con Android + Bluetooth, un equipo por sucursal (2 tickets por pedido). PRN-03a/b pasan a Must y absorben la recuperación tras desconexión; PRN-02a/b fuera de alcance (−13 SP). PRN-04 depende de PRN-03a. Total 171 SP; MVP en el sprint 8. |
