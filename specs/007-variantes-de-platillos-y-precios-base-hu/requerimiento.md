---
feature: 007-variantes-de-platillos-y-precios-base-hu
estado: borrador
---
# Requerimiento: Variantes de platillos y precios base (HU-3)

## Contexto y propósito

Un platillo (feature 006) se vende en una o varias versiones con precio propio: "1/2 litro" y
"1 litro" de un agua, o "Sencillo", "Suadero" y "Combinado" de unos tacos (HU-3 del backlog,
sprint 2). Cada versión es una **variante** con nombre, precio base y SKU opcional, para cobrar
correctamente lo que elige el cliente. El menú y sus precios son comunes a todas las sucursales
(P-38, P-41) y los precios incluyen IVA (P-08). Las variantes son la base de los modificadores
(HU-4), los agotados por sucursal (HU-6), el menú por WhatsApp (US-01) y la web (WEB-01). Esta
feature también deja en el dominio la regla aplazada de la HU-2 (RN2.3): un platillo eliminado o
desactivado no vende ninguna de sus variantes.

## Actores

- **Administrador general**: único rol que consulta y modifica las variantes en esta feature.
- **Resto del personal** (administrador de sucursal, cajero, cocinero): no accede a la gestión de
  variantes.

## Historias de usuario

### HU-01: Alta y consulta de variantes
Como administrador general quiero definir las variantes de cada platillo con su precio para cobrar según la versión que elija el cliente.

- **RF-01**: Cuando el administrador general crea un platillo con `POST /api/r/{slug}/categorias/{categoriaId}/platillos`, el sistema deberá exigir además `precio` (con las reglas de RF-03) y crear en el mismo guardado una variante activa llamada "Regular" con ese precio y sin SKU.
- **RF-02**: Cuando el administrador general envía nombre, precio y, opcionalmente, SKU a `POST /api/r/{slug}/platillos/{platilloId}/variantes`, el sistema deberá crear la variante activa en ese platillo y responder 201 con su identificador, platillo, nombre, precio, SKU y estado.
- **RF-03**: Si el nombre está vacío o supera 24 caracteres tras recortar espacios, si el precio falta, es negativo, supera 99,999.99, tiene más de 2 decimales o no es un número, o si el SKU no cumple `^[A-Za-z0-9_-]{1,40}$` tras recortar espacios, el sistema deberá responder 400 con el error en su campo; un SKU vacío o sólo con espacios se guarda como ausente.
- **RF-04**: Si el nombre ya existe en otra variante no eliminada del mismo platillo, sin distinguir mayúsculas ni espacios exteriores, o si el SKU ya existe en otra variante no eliminada del restaurante, sin distinguir mayúsculas, el sistema deberá responder 409 indicando en su campo cuál se repite (los dos si se repiten ambos).
- **RF-05**: Cuando el administrador general consulta `GET /api/r/{slug}/platillos/{platilloId}/variantes`, el sistema deberá devolver todas las variantes no eliminadas del platillo (activas e inactivas), ordenadas por precio de menor a mayor y después por nombre.
- **RF-06**: Si el platillo de la ruta no existe, está eliminado o es de otro restaurante, el sistema deberá responder 404 "Platillo no encontrado" en el alta y en la lista de variantes.

### HU-02: Edición y desactivación de variantes
Como administrador general quiero corregir el nombre, el precio y el SKU de una variante y desactivarla para mantener los precios al día.

- **RF-07**: Cuando el administrador general envía nombre, precio y SKU opcional a `PUT /api/r/{slug}/variantes/{id}`, el sistema deberá guardarlos con las reglas de RF-03 y RF-04 (la propia variante no cuenta como repetida) y responder 200 con la variante.
- **RF-08**: Cuando el administrador general envía `activa` a `PUT /api/r/{slug}/variantes/{id}/estado`, el sistema deberá activarla o desactivarla y responder 200 (400 en `activa` si falta o no es booleano), también si es la última variante activa del platillo; una variante inactiva sigue en la lista de RF-05 y conserva su nombre y su SKU reservados.

### HU-03: Eliminación y venta
Como administrador general quiero eliminar las variantes que ya no vendo sin dejar platillos sin variantes, y que lo que no está a la venta no se venda.

- **RF-09**: Cuando el administrador general llama a `DELETE /api/r/{slug}/variantes/{id}`, el sistema deberá marcarla como eliminada (sin borrarla físicamente, Art. 13), responder 204, dejar de devolverla en RF-05 y liberar su nombre y su SKU.
- **RF-10**: Si la variante es la única no eliminada de su platillo, el sistema deberá responder 409 con el detalle "No se puede eliminar la única variante del platillo" sin eliminarla.
- **RF-11**: Si el identificador no corresponde a una variante no eliminada, de un platillo no eliminado del restaurante del contexto, el sistema deberá responder 404 "Variante no encontrada" en edición, cambio de estado y eliminación.
- **RF-12**: Si un usuario que no es administrador general llama a cualquier endpoint de variantes, el sistema deberá responder 403.
- **RF-13**: Cuando se elimina un platillo (`DELETE /api/r/{slug}/platillos/{id}`, feature 006), el sistema deberá marcar como eliminadas también sus variantes en el mismo guardado, liberando sus SKU.
- **RF-14**: Cuando el dominio calcule las variantes a la venta de un platillo, el sistema deberá devolver sólo sus variantes activas y no eliminadas, y ninguna si el platillo está inactivo o eliminado o si su categoría está inactiva.

### HU-04: Pantallas de variantes y precio del platillo
Como administrador general quiero gestionar las variantes desde cada platillo y fijar su precio al crearlo.

- **RF-15**: Cuando el administrador general abre el formulario de alta de platillo, el sistema deberá mostrar el campo "Precio" obligatorio; en edición no se muestra (el precio vive en las variantes).
- **RF-16**: Cuando el administrador general pulsa "Variantes" en una fila de platillos, el sistema deberá abrir `/r/{slug}/menu/platillos/{platilloId}/variantes` con el título "Variantes de {platillo}", el enlace "Platillos de {categoría}" para volver, una tabla con nombre, precio (en formato `$1,234.50`), SKU y estado, y el botón "Nueva variante"; si el platillo no tiene variantes activas deberá mostrar "Este platillo no tiene variantes activas: no se muestra a los clientes."
- **RF-17**: Cuando el administrador general escribe el nombre de una variante, el sistema deberá mostrar un contador "N/24"; si el precio escrito no es un número de 0 a 99,999.99 con hasta 2 decimales, deberá mostrar el error junto al campo sin enviar la petición; los errores 400 y 409 del servidor deberán mostrarse junto a su campo, y un 409 sin campo (la única variante) como aviso general.
- **RF-18**: Cuando el administrador general pulsa "Eliminar" en una variante, el sistema deberá pedir confirmación con "¿Eliminar {nombre}?" y "No se podrá recuperar. Si sólo quieres ocultarla, desactívala." antes de llamar a la API; "Desactivar" y "Activar" no piden confirmación.
- **RF-19**: Si otro rol abre la pantalla de variantes, el sistema deberá llevarlo a `/r/{slug}/inicio`; y si el platillo de la ruta no existe, deberá mostrar "Platillo no encontrado" con el enlace "Categorías".

## Requisitos no funcionales

- **RNF-01**: Pruebas de aislamiento del Art. 12.5: el administrador del restaurante A no ve, crea, edita, desactiva ni elimina variantes del B ni las crea en platillos del B (404), y el mismo SKU puede existir en restaurantes distintos.
- **RNF-02**: Los precios se guardan como `numeric(10,2)` y viajan como número JSON (no como texto); nunca se redondean en silencio.
- **RNF-03**: Las pantallas funcionan en escritorio (1440 px) y móvil (390 px) y sólo usan colores de las variables de tema (Art. 15.3).
- **RNF-04**: La migración es aditiva: crea la tabla de variantes y, a cada platillo no eliminado que ya exista, le añade una variante "Regular" **inactiva** con precio 0.00; no cambia otros datos. Las pruebas de 005 y 006 siguen pasando, salvo el alta de platillo, que ahora exige `precio`.

## Fuera de alcance

- Modificadores (HU-4) y agotados por sucursal (HU-6).
- Mostrar variantes y precios en la web pública o en WhatsApp (WEB-01, US-01) y aplicar la regla
  de visibilidad completa de RN3.5 (que incluye los agotados); aquí sólo queda en el dominio la
  parte de RF-14.
- Precios distintos por sucursal, descuentos, impuestos separados y moneda distinta del peso.
- Recuperar una variante eliminada; mover una variante a otro platillo; reordenarlas a mano.
- Que otros roles consulten las variantes.

## Preguntas abiertas

- Ninguna
