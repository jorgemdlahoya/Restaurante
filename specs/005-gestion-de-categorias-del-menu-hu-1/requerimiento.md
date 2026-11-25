---
feature: 005-gestion-de-categorias-del-menu-hu-1
estado: borrador
---
# Requerimiento: Gestión de categorías del menú (HU-1)

## Contexto y propósito

El menú de cada restaurante se organiza en categorías (Pozoles, Chilaquiles, Bebidas…) que ordenan
la oferta y la navegación del cliente en la web y en WhatsApp (HU-1 del backlog, primer elemento del
sprint 2). El menú es común a todas las sucursales del restaurante (Art. 12.4): una categoría se
administra una sola vez y no lleva sucursal. Esta feature es la base de los platillos (HU-2), las
variantes (HU-3) y los agotados por sucursal (HU-6).

## Actores

- **Administrador general**: único rol que consulta y modifica las categorías en esta feature.
- **Resto del personal** (administrador de sucursal, cajero, cocinero): no accede a la gestión de
  categorías.

## Historias de usuario

### HU-01: Alta y consulta de categorías
Como administrador general quiero crear categorías y verlas en orden para organizar el menú.

- **RF-01**: Cuando el administrador general envía nombre, orden y, opcionalmente, nombre corto a `POST /api/r/{slug}/categorias`, el sistema deberá crear la categoría activa y responder 201 con su identificador, nombre, nombre corto, orden y estado.
- **RF-02**: Si el nombre está vacío o supera 60 caracteres tras recortar espacios, si el nombre corto supera 24 caracteres, o si el orden falta o no es un entero mayor o igual que 1 (incluidos 0, negativos, decimales y texto), el sistema deberá responder 400 con el error en su campo; un nombre corto vacío o sólo con espacios se guarda como ausente.
- **RF-03**: Si el nombre o el nombre corto ya existen en otra categoría no eliminada del mismo restaurante, sin distinguir mayúsculas ni espacios exteriores, el sistema deberá responder 409 indicando en su campo cuál se repite (los dos si se repiten ambos).
- **RF-04**: Cuando el administrador general consulta `GET /api/r/{slug}/categorias?pagina=N&tamano=M`, el sistema deberá devolver las categorías no eliminadas del restaurante (activas e inactivas) ordenadas por orden y después por nombre, junto con el total, la página y el tamaño; por defecto `pagina=1` y `tamano=20`, y si `pagina` es menor que 1 o `tamano` no está entre 1 y 100 deberá responder 400.

### HU-02: Edición, orden y desactivación
Como administrador general quiero cambiar nombre, nombre corto y orden de una categoría, y
desactivarla, para mantener el menú al día.

- **RF-05**: Cuando el administrador general envía nombre, orden y nombre corto opcional a `PUT /api/r/{slug}/categorias/{id}`, el sistema deberá guardarlos con las mismas reglas de RF-02 y RF-03 (la propia categoría no cuenta como repetida) y responder 200 con la categoría.
- **RF-06**: Cuando el administrador general envía `activa` a `PUT /api/r/{slug}/categorias/{id}/estado`, el sistema deberá activar o desactivar la categoría y responder 200 (400 en `activa` si falta o no es booleano); una categoría inactiva sigue en la lista de RF-04 y conserva su nombre y su nombre corto reservados.

### HU-03: Eliminación
Como administrador general quiero eliminar una categoría que ya no uso para limpiar el menú.

- **RF-07**: Cuando el administrador general llama a `DELETE /api/r/{slug}/categorias/{id}`, el sistema deberá marcarla como eliminada (sin borrarla físicamente, Art. 13), responder 204, dejar de devolverla en RF-04 y liberar su nombre y su nombre corto para nuevas categorías.
- **RF-08**: Si el identificador no corresponde a una categoría no eliminada del restaurante del contexto (incluida una de otro restaurante o una ya eliminada), el sistema deberá responder 404 en edición, cambio de estado y eliminación.
- **RF-09**: Si un usuario que no es administrador general llama a cualquier endpoint de `/api/r/{slug}/categorias`, el sistema deberá responder 403.

### HU-04: Pantalla de categorías
Como administrador general quiero una pantalla para gestionar las categorías y ver cómo se verán en
WhatsApp.

- **RF-10**: Cuando el administrador general abre `/r/{slug}/menu/categorias`, el sistema deberá mostrar una tabla con orden, nombre, título en WhatsApp y estado, paginada de 20 en 20 con "Anterior" y "Siguiente", y el botón "Nueva categoría".
- **RF-11**: Cuando el administrador general escribe en el formulario de alta o edición, el sistema deberá mostrar la vista previa del título en WhatsApp: el nombre corto si lo hay; si no, el nombre completo si tiene 24 caracteres o menos; y si pasa de 24, los primeros 23 caracteres seguidos de "…" junto con el aviso "Este nombre se verá recortado en WhatsApp".
- **RF-12**: Si el orden escrito en el formulario no es un entero mayor o igual que 1, el sistema deberá mostrar el error junto al campo sin enviar la petición; los errores 400 y 409 del servidor deberán mostrarse junto a su campo.
- **RF-13**: Cuando el administrador general pulsa "Eliminar", el sistema deberá pedir confirmación con "¿Eliminar {nombre}?" y "No se podrá recuperar. Si sólo quieres ocultarla, desactívala." antes de llamar a la API; "Desactivar" y "Activar" no piden confirmación.
- **RF-14**: Cuando el administrador general está en `/r/{slug}/inicio`, el sistema deberá mostrar el enlace "Menú" que lleva a las categorías; si otro rol abre `/r/{slug}/menu/categorias`, deberá llevarlo a `/r/{slug}/inicio`.

## Requisitos no funcionales

- **RNF-01**: Pruebas de aislamiento del Art. 12.5: el administrador del restaurante A no ve, edita, desactiva ni elimina categorías del B (404), y el mismo nombre puede existir en restaurantes distintos.
- **RNF-02**: La regla del título en WhatsApp (RF-11) vive en una única función del dominio, reutilizable por el chatbot (US-01), y el frontend la replica con las mismas pruebas de casos.
- **RNF-03**: Las pantallas funcionan en escritorio (1440 px) y móvil (390 px) y sólo usan colores de las variables de tema (Art. 15.3).
- **RNF-04**: La migración es aditiva (tabla nueva); no cambia datos existentes.

## Fuera de alcance

- La regla "no eliminar una categoría con platillos activos" y el botón "Desactivar categoría" en
  ese mensaje (RN1.2 del backlog): se implementan con la HU-2, cuando existan platillos.
- Platillos, variantes, modificadores y agotados (HU-2 a HU-6).
- Mostrar las categorías en la web pública o en WhatsApp (WEB-01, US-01).
- Recuperar una categoría eliminada; reordenar arrastrando.
- Que otros roles consulten las categorías.

## Preguntas abiertas

- Ninguna
