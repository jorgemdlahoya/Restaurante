---
feature: 006-gestion-de-platillos-del-menu-hu-2
estado: borrador
---
# Requerimiento: Gestión de platillos del menú (HU-2)

## Contexto y propósito

Cada categoría del menú (feature 005) agrupa sus platillos base: por ejemplo "Pozole rojo" en
Pozoles o "Agua de horchata" en Bebidas. El administrador general los registra con su descripción
y los ingredientes que incluyen por defecto, para que el cliente sepa qué contiene su comida antes
de cualquier cambio (HU-2 del backlog, sprint 2). El menú es común a todas las sucursales
(Art. 12.4): un platillo no lleva sucursal. Los ingredientes se mostrarán en el detalle del
platillo en la web (WEB-01b) y en el bot (US-01); no se imprimen en el ticket ni se vinculan con
modificadores (P-34). Esta feature es la base de las variantes y precios (HU-3), los
modificadores (HU-4) y los agotados por sucursal (HU-6). También completa la regla de la HU-1 que
quedó aplazada: no eliminar una categoría que tiene platillos (RN1.2).

## Actores

- **Administrador general**: único rol que consulta y modifica los platillos en esta feature.
- **Resto del personal** (administrador de sucursal, cajero, cocinero): no accede a la gestión de
  platillos.

## Historias de usuario

### HU-01: Alta y consulta de platillos
Como administrador general quiero agregar platillos a una categoría y verlos en orden para armar el menú.

- **RF-01**: Cuando el administrador general envía nombre, orden y, opcionalmente, nombre corto, descripción e ingredientes a `POST /api/r/{slug}/categorias/{categoriaId}/platillos`, el sistema deberá crear el platillo activo en esa categoría y responder 201 con su identificador, categoría, nombre, nombre corto, descripción, orden, ingredientes y estado.
- **RF-02**: Si el nombre está vacío o supera 60 caracteres tras recortar espacios, si el nombre corto supera 24, si la descripción supera 300, si el orden falta o no es un entero mayor o igual que 1, o si los ingredientes son más de 30, alguno queda vacío o supera 40 caracteres tras recortar espacios, o se repiten sin distinguir mayúsculas ni espacios exteriores, el sistema deberá responder 400 con el error en su campo; un nombre corto o una descripción vacíos o sólo con espacios se guardan como ausentes.
- **RF-03**: Si la categoría de la ruta no existe, está eliminada o es de otro restaurante, el sistema deberá responder 404 "Categoría no encontrada"; y si está inactiva, deberá responder 400 en `categoriaId` con "La categoría está inactiva".
- **RF-04**: Si el nombre o el nombre corto ya existen en otro platillo no eliminado del mismo restaurante, sin distinguir mayúsculas ni espacios exteriores, el sistema deberá responder 409 indicando en su campo cuál se repite (los dos si se repiten ambos).
- **RF-05**: Cuando el administrador general consulta `GET /api/r/{slug}/categorias/{categoriaId}/platillos?pagina=N&tamano=M`, el sistema deberá devolver los platillos no eliminados de esa categoría (activos e inactivos) ordenados por orden y después por nombre, con el total, la página y el tamaño; por defecto `pagina=1` y `tamano=20`, 400 si `pagina` es menor que 1 o `tamano` no está entre 1 y 100, y 404 si la categoría no existe, está eliminada o es de otro restaurante (una categoría inactiva sí lista sus platillos); y `GET /api/r/{slug}/categorias/{categoriaId}` deberá devolver la categoría con su estado, con el mismo 404.
- **RF-06**: Cuando el administrador general consulta `GET /api/r/{slug}/platillos/{id}`, el sistema deberá devolver el platillo con su categoría y sus ingredientes en el orden en que se guardaron.

### HU-02: Edición, cambio de categoría y desactivación
Como administrador general quiero corregir un platillo, cambiarlo de categoría y desactivarlo para mantener el menú al día.

- **RF-07**: Cuando el administrador general envía categoría, nombre, orden y los datos opcionales a `PUT /api/r/{slug}/platillos/{id}`, el sistema deberá guardarlos con las reglas de RF-02 y RF-04 (el propio platillo no cuenta como repetido) y responder 200; si la categoría indicada no existe en el restaurante deberá responder 400 en `categoriaId` con "La categoría no existe", y si está inactiva, 400 en `categoriaId` con "La categoría está inactiva" (salvo que sea la categoría actual del platillo).
- **RF-08**: Cuando el administrador general envía `activo` a `PUT /api/r/{slug}/platillos/{id}/estado`, el sistema deberá activar o desactivar el platillo y responder 200 (400 en `activo` si falta o no es booleano); un platillo inactivo sigue en la lista de RF-05 y conserva su nombre y su nombre corto reservados.

### HU-03: Eliminación de platillos y de categorías con platillos
Como administrador general quiero eliminar los platillos que ya no vendo, sin dejar platillos sueltos al eliminar una categoría.

- **RF-09**: Cuando el administrador general llama a `DELETE /api/r/{slug}/platillos/{id}`, el sistema deberá marcarlo como eliminado (sin borrarlo físicamente, Art. 13), responder 204, dejar de devolverlo en RF-05 y RF-06 y liberar su nombre y su nombre corto.
- **RF-10**: Si el identificador no corresponde a un platillo no eliminado del restaurante del contexto, el sistema deberá responder 404 "Platillo no encontrado" en consulta, edición, cambio de estado y eliminación.
- **RF-11**: Si un usuario que no es administrador general llama a cualquier endpoint de platillos, el sistema deberá responder 403.
- **RF-12**: Si el administrador general intenta eliminar una categoría que tiene platillos no eliminados (activos o inactivos), el sistema deberá responder 409 con el detalle "Esta categoría tiene N platillos" ("1 platillo" en singular) y la cantidad en `platillos`, sin eliminarla.

### HU-04: Pantallas de platillos
Como administrador general quiero gestionar los platillos desde cada categoría y ver cómo se verán en WhatsApp.

- **RF-13**: Cuando el administrador general pulsa "Platillos" en una fila de `/r/{slug}/menu/categorias`, el sistema deberá abrir `/r/{slug}/menu/categorias/{categoriaId}/platillos` con el título "Platillos de {categoría}", el enlace "Categorías" para volver, una tabla con orden, nombre, título en WhatsApp, ingredientes y estado paginada de 20 en 20 con "Anterior" y "Siguiente", y el botón "Nuevo platillo"; si la categoría está inactiva deberá mostrar el aviso "Esta categoría está inactiva. Sus platillos no se muestran a los clientes." y deshabilitar "Nuevo platillo".
- **RF-14**: Cuando el administrador general abre el formulario de alta o edición, el sistema deberá mostrar los campos nombre, nombre corto, descripción, orden e ingredientes incluidos (en edición, también la categoría, entre las activas más la actual, marcada "(inactiva)" si lo está); los ingredientes se agregan con "Agregar ingrediente" o con Enter y se quitan con "Quitar" en cada uno, y uno vacío o repetido no se agrega y muestra el motivo; la vista previa de WhatsApp sigue la regla de la feature 005.
- **RF-15**: Si el orden escrito no es un entero mayor o igual que 1, el sistema deberá mostrar el error junto al campo sin enviar la petición; los errores 400 y 409 del servidor deberán mostrarse junto a su campo.
- **RF-16**: Cuando el administrador general pulsa "Eliminar" en un platillo, el sistema deberá pedir confirmación con "¿Eliminar {nombre}?" y "No se podrá recuperar. Si sólo quieres ocultarlo, desactívalo." antes de llamar a la API; "Desactivar" y "Activar" no piden confirmación.
- **RF-17**: Si al eliminar una categoría la API responde que tiene platillos, el sistema deberá mostrar "Esta categoría tiene N platillos" con los botones "Desactivar categoría" (que la desactiva) y "Cancelar"; si la categoría ya está inactiva, sólo "Cancelar".
- **RF-18**: Si otro rol abre la pantalla de platillos, el sistema deberá llevarlo a `/r/{slug}/inicio`; y si la categoría de la ruta no existe, deberá mostrar "Categoría no encontrada" con el enlace "Categorías".

## Requisitos no funcionales

- **RNF-01**: Pruebas de aislamiento del Art. 12.5: el administrador del restaurante A no ve, crea, edita, desactiva ni elimina platillos del B ni los crea en categorías del B (404), y el mismo nombre de platillo puede existir en restaurantes distintos.
- **RNF-02**: El título en WhatsApp del platillo usa la misma función del dominio que las categorías (`TituloWhatsApp`) y la misma del frontend (`tituloWhatsApp`), sin copias nuevas.
- **RNF-03**: Las pantallas funcionan en escritorio (1440 px) y móvil (390 px) y sólo usan colores de las variables de tema (Art. 15.3).
- **RNF-04**: La migración es aditiva (tabla nueva); no cambia datos existentes. Las pruebas de la feature 005 siguen pasando, salvo la eliminación de una categoría con platillos, que ahora responde 409.

## Fuera de alcance

- Variantes, precios y la variante "Regular" automática (HU-3); modificadores (HU-4); agotados por
  sucursal (HU-6).
- RN2.3 (al eliminar o desactivar un platillo, sus variantes y modificadores quedan fuera de la
  venta): se implementa con la HU-3 y la HU-4, cuando existan.
- Mostrar los platillos y sus ingredientes en la web pública o en WhatsApp (WEB-01, US-01) y la regla
  de visibilidad automática (RN3.5).
- Recuperar un platillo eliminado; reordenar arrastrando; buscar platillos entre categorías; mover
  varios platillos a la vez.
- Que otros roles consulten los platillos.

## Preguntas abiertas

- Ninguna
