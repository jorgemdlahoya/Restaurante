---
feature: 004-usuarios-roles-y-asignacion-a-sucursales
estado: borrador
---
# Requerimiento: Usuarios, roles y asignación a sucursales (AUTH-01)

## Contexto y propósito

Cada persona del restaurante debe acceder sólo a las funciones y sucursales que le corresponden
(AUTH-01 del backlog, cierre del sprint 1). La feature 002 dejó usuarios por restaurante con rol,
y la 003 creó las sucursales, pero hoy un usuario no tiene sucursales asignadas: cualquier usuario
del restaurante puede pedir datos de cualquier sucursal, no hay pantallas de usuarios y desactivar
a alguien no corta su sesión (el token sigue valiendo hasta 8 h).

Esta feature asigna sucursales a los usuarios, da al administrador de sucursal la gestión de su
personal, corta las sesiones al momento cuando cambia el acceso de un usuario y añade las
pantallas de usuarios y de elección de sucursal. Es la base de autorización por sucursal sobre la
que se apoyarán los tableros de caja y cocina.

## Actores

- **Administrador general**: gestiona a todos los usuarios de su restaurante; trabaja en todas
  sus sucursales.
- **Administrador de sucursal**: gestiona a los cajeros y cocineros que trabajan sólo en su
  sucursal.
- **Cajero y cocinero**: trabajan en una o varias sucursales y eligen con cuál trabajar.
- **Administrador de la plataforma**: no cambia en esta feature (crea el primer administrador
  general de cada restaurante, sin sucursales).

## Historias de usuario

### HU-01: Rol y sucursales de cada usuario
Como administrador general quiero dar de alta y editar usuarios con un rol y sus sucursales para
que cada persona trabaje sólo donde le corresponde.

- **RF-01**: Cuando el administrador general envía nombre de usuario, contraseña, rol y sucursales a `POST /api/r/{slug}/usuarios`, el sistema deberá crear el usuario activo con esas sucursales y responder 201 con su identificador, nombre, rol, estado y sucursales (`slug` y nombre).
- **RF-02**: Si el rol es `administrador_sucursal` y no se indica exactamente una sucursal, si el rol es `cajero` o `cocinero` y no se indica al menos una, si el rol es `administrador_general` y se indica alguna, o si alguna sucursal no existe en el restaurante, el sistema deberá responder 400 con el error en el campo `sucursales`.
- **RF-03**: Cuando el administrador general consulta `GET /api/r/{slug}/usuarios`, el sistema deberá devolver todos los usuarios del restaurante con su rol, estado y sucursales asignadas (activas o no), ordenados por nombre de usuario.
- **RF-04**: Cuando el administrador general envía rol y sucursales a `PUT /api/r/{slug}/usuarios/{id}`, el sistema deberá guardarlos con las mismas reglas de RF-02; el nombre de usuario no se puede modificar.
- **RF-05**: Si el cambio de RF-04 o la desactivación dejaría al restaurante sin ningún administrador general activo, o si el usuario intenta desactivarse a sí mismo o quitarse el rol de administrador general, el sistema deberá responder 409 sin cambiar nada.
- **RF-06**: Cuando quien gestiona a un usuario envía una contraseña nueva de al menos 8 caracteres a `PUT /api/r/{slug}/usuarios/{id}/contrasena`, el sistema deberá guardarla como hash y responder 204; si tiene menos de 8 caracteres, deberá responder 400. El administrador general puede asignársela a sí mismo; su sesión se corta como la de cualquier otro (RF-13).

### HU-02: El administrador de sucursal gestiona su personal
Como administrador de sucursal quiero dar de alta y administrar a los cajeros y cocineros de mi
sucursal para no depender del administrador general en el día a día.

- **RF-07**: Cuando el administrador de sucursal consulta `GET /api/r/{slug}/usuarios`, el sistema deberá devolver sólo los usuarios asignados a su sucursal (sin los administradores generales), indicando en cada uno si puede gestionarlo.
- **RF-08**: Cuando el administrador de sucursal crea un usuario, el sistema deberá aceptar sólo los roles `cajero` y `cocinero` con su propia sucursal como única sucursal; con otro rol o con otra sucursal deberá responder 403.
- **RF-09**: Cuando el administrador de sucursal edita, cambia la contraseña o activa/desactiva a un cajero o cocinero asignado únicamente a su sucursal, el sistema deberá aplicarlo igual que al administrador general, sin permitirle asignar otro rol ni otra sucursal (403).
- **RF-10**: Si el administrador de sucursal intenta modificar a un usuario que no es cajero o cocinero (incluido él mismo), o que tiene asignada alguna sucursal distinta de la suya (personal compartido), el sistema deberá responder 403 sin cambiar nada. Una petición con errores de formato o de cardinalidad responde primero 400 (RF-02); bien formada, cualquier sucursal que no sea la suya, exista o no, responde 403.
- **RF-11**: Si un cajero o cocinero llama a cualquier endpoint de `/api/r/{slug}/usuarios`, el sistema deberá responder 403.

### HU-03: Cortar sesiones al cambiar el acceso
Como administrador quiero que, al desactivar a alguien o cambiar su acceso, su sesión abierta deje
de funcionar al momento para que nadie trabaje con permisos que ya no tiene.

- **RF-12**: Si un usuario desactivado intenta iniciar sesión, el sistema deberá responder 401 con el mismo mensaje que para credenciales incorrectas.
- **RF-13**: Cuando se desactiva a un usuario o se cambian su contraseña, su rol o sus sucursales (una edición que no cambia ni el rol ni las sucursales no cuenta como cambio), el sistema deberá rechazar con 401 "Sesión no válida" toda petición posterior hecha con un token emitido antes del cambio; un token nuevo obtenido tras el cambio deberá funcionar.
- **RF-14**: Si un token no trae el sello de sesión, pertenece a un usuario que ya no existe o su sello no coincide con el actual del usuario, el sistema deberá responder 401 "Sesión no válida" en cualquier endpoint autenticado, incluidos los de la plataforma.

### HU-04: Acceso sólo a mis sucursales
Como dueño del restaurante quiero que cada persona vea y modifique sólo los datos de las
sucursales donde trabaja.

- **RF-15**: Si un usuario que no es administrador general hace una petición a `/api/r/{slug}/s/{sucursal}/…` de una sucursal que no tiene asignada, el sistema deberá responder 404 "Sucursal no disponible", igual que para una sucursal inexistente; el endpoint público de estado de la sucursal no cambia.
- **RF-16**: Cuando un usuario autenticado consulta `GET /api/r/{slug}/mi-sesion`, el sistema deberá devolver su nombre, su rol y las sucursales activas con las que puede trabajar (`slug` y nombre, ordenadas por nombre): todas las activas del restaurante para el administrador general y sus sucursales asignadas activas para el resto.
- **RF-17**: Si un usuario que no es administrador general inicia sesión con credenciales válidas pero ninguna de sus sucursales está activa, el sistema deberá responder 403 con el título "Sin sucursales activas" y no emitir token.
- **RF-25**: Si un usuario que no es administrador general hace una petición a `/api/r/{slug}/s/{sucursal}/…` de una sucursal asignada pero desactivada, el sistema deberá responder 404 "Sucursal no disponible"; desactivar una sucursal no corta las sesiones abiertas, pero deja de ofrecerse en RF-16.
- **RF-26**: Si la sucursal del administrador de sucursal está desactivada, el sistema deberá responder 403 a sus operaciones sobre usuarios (consultar la lista incluida).

### HU-05: Pantallas de usuarios y elección de sucursal
Como personal del restaurante quiero pantallas para gestionar usuarios y elegir mi sucursal.

- **RF-18**: Cuando un administrador general o de sucursal abre `/r/{slug}/usuarios`, el sistema deberá mostrar la lista de RF-03 o RF-07 con nombre, rol, sucursales y estado, y permitir crear, editar rol y sucursales, asignar contraseña nueva y activar/desactivar (pidiendo confirmación para desactivar) los usuarios que puede gestionar; los que no puede gestionar se muestran sin acciones.
- **RF-19**: Cuando el administrador de sucursal abre el formulario de alta o edición, el sistema deberá ofrecer sólo los roles cajero y cocinero y fijar su sucursal sin permitir cambiarla.
- **RF-20**: Cuando un usuario con varias sucursales activas (no administrador general) inicia sesión, el sistema deberá llevarlo a `/r/{slug}/elegir-sucursal` para escoger una antes de ir al inicio; con una sola, deberá fijarla y llevarlo directo al inicio.
- **RF-21**: Cuando un usuario con varias sucursales tiene una sesión abierta, el sistema deberá mostrar en el encabezado un selector con sus sucursales de RF-16 para cambiar la activa; con una sola, deberá mostrar su nombre sin selector.
- **RF-22**: Cuando un administrador general o de sucursal está en `/r/{slug}/inicio`, el sistema deberá mostrar el enlace "Usuarios"; si otro rol abre `/r/{slug}/usuarios`, deberá llevarlo a `/r/{slug}/inicio`.
- **RF-23**: Si una petición del frontend recibe 401 "Sesión no válida", el sistema deberá cerrar la sesión local y llevar al usuario a `/r/{slug}/login` con el aviso "Tu sesión terminó. Vuelve a iniciar sesión.".
- **RF-24**: Si el inicio de sesión responde 403 "Sin sucursales activas", el sistema deberá mostrar en el login "No tienes sucursales activas. Habla con tu administrador.".
- **RF-27**: Si un usuario con sesión abierta (no administrador general) se queda sin sucursales activas en RF-16, el sistema deberá mostrar en el inicio "No tienes sucursales activas. Habla con tu administrador." sin selector de sucursal, y mantener "Cerrar sesión".

## Requisitos no funcionales

- **RNF-01**: La comprobación de sesión por petición se resuelve con una única consulta por clave primaria del usuario (con sus sucursales) y no añade más de 10 ms de mediana en las pruebas de integración.
- **RNF-02**: Toda historia lleva las pruebas de aislamiento del Art. 12.5: un usuario del restaurante A no ve ni modifica usuarios del B (404), y un usuario de la sucursal A no accede a datos de la B (404).
- **RNF-03**: Las pantallas funcionan en escritorio (1440 px) y móvil (390 px) y sólo usan colores de las variables de tema (Art. 15.3).
- **RNF-04**: La migración es aditiva: los usuarios existentes conservan su rol, quedan sin sucursales asignadas y con un sello de sesión nuevo (los tokens previos dejan de valer y hay que volver a iniciar sesión).

## Fuera de alcance

- Que cada usuario cambie su propia contraseña o la recupere por correo.
- Cambiar el nombre de usuario.
- Ver o cerrar sesiones una por una, y bitácora de cambios de usuarios.
- Las funciones propias de cada rol en otras pantallas (agotados, impresoras, tableros de caja y
  cocina): llegan con sus features; aquí sólo se fija quién entra a qué sucursal.
- Asignar sucursales a los usuarios existentes de forma automática (RNF-04): lo hace el
  administrador general desde la pantalla de usuarios.

## Preguntas abiertas

- Ninguna
