---
feature: 002-base-multisucursal-y-sucursales
estado: borrador
---
# Requerimiento: Base multi-restaurante

> El nombre de la carpeta es histórico; esta feature es la **base multi-restaurante**. Las historias
> HT-0 y SUC-01 del backlog pasan a la feature 003.

## Contexto y propósito

La plataforma pasa a ser **multi-restaurante** (constitución Art. 12): el operador de la plataforma
da de alta restaurantes independientes y completamente aislados, cada uno con su identidad visual
(Art. 15). Esta feature construye esa base: la entidad restaurante, el rol
`administrador_plataforma`, el login por restaurante, el aislamiento por restaurante, la identidad
configurable y el esqueleto del backoffice en React. Reutiliza el backend de
`001-plataforma-base`.

## Actores

- **Administrador de la plataforma**: da de alta, edita, activa y desactiva restaurantes.
- **Administrador general**: administra su restaurante (en esta feature: sus usuarios).
- **Personal del restaurante** (administrador de sucursal, cajero, cocinero): inicia sesión en su
  restaurante.

## Historias de usuario

### HU-01: Gestión de restaurantes
Como administrador de la plataforma quiero dar de alta y administrar restaurantes para que cada
negocio opere de forma independiente.

- **RF-01**: Cuando el administrador de la plataforma envía nombre, `slug`, paleta (opcional) y usuario y contraseña del primer administrador general, el sistema deberá crear el restaurante activo con ese administrador y responder 201.
- **RF-02**: Si el `slug` no cumple `^[a-z0-9-]{3,40}$`, o el nombre está vacío o supera 80 caracteres, o algún color de la paleta no es hexadecimal `#RRGGBB`, el sistema deberá responder 400 por campo; si el nombre o el `slug` ya existen, deberá responder 409.
- **RF-03**: Cuando el administrador de la plataforma consulta los restaurantes, el sistema deberá devolver nombre, `slug`, estado y paleta de cada uno.
- **RF-04**: Cuando el administrador de la plataforma edita el nombre o la paleta de un restaurante, el sistema deberá guardar los cambios; el `slug` no se puede modificar.
- **RF-05**: Cuando el administrador de la plataforma sube un logo PNG, JPG o WebP de hasta 1 MB, el sistema deberá guardarlo en la base de datos; si el archivo es de otro tipo (incluido SVG) o pesa más, deberá responder 400.
- **RF-06**: Cuando el administrador de la plataforma desactiva un restaurante, el sistema deberá impedir que su personal inicie sesión y rechazar con 401 las peticiones de tokens ya emitidos para ese restaurante; al reactivarlo, todo deberá volver a funcionar sin pérdida de datos.
- **RF-07**: Si un usuario sin rol `administrador_plataforma` intenta gestionar restaurantes, el sistema deberá responder 403.

### HU-02: Inicio de sesión por restaurante
Como personal de un restaurante quiero iniciar sesión en la dirección de mi restaurante para
trabajar con su marca y sus datos.

- **RF-08**: Cuando un usuario activo envía credenciales válidas a `POST /api/r/{slug}/auth/login` de su restaurante activo, el sistema deberá devolver un JWT con su identificador, su rol y el identificador del restaurante.
- **RF-09**: Si el usuario no pertenece a ese restaurante, el sistema deberá responder 401 con el mismo mensaje que para credenciales incorrectas.
- **RF-10**: Si el `slug` no existe o el restaurante está inactivo, el sistema deberá responder 404 con el título "Restaurante no disponible".
- **RF-11**: Cuando el administrador de la plataforma envía credenciales válidas a `POST /api/plataforma/auth/login`, el sistema deberá devolver un JWT con el rol `administrador_plataforma` y sin restaurante.

### HU-03: Aislamiento por restaurante
Como dueño de un restaurante quiero que ningún otro restaurante vea ni modifique mis datos.

- **RF-12**: Si un token de un restaurante A se usa para consultar o modificar datos del restaurante B, el sistema deberá responder 404.
- **RF-13**: Cuando un administrador general crea un usuario, el sistema deberá exigir que el nombre sea único sólo dentro de su restaurante (el mismo nombre puede existir en otro restaurante) y deberá aplicar la regla del último administrador por restaurante.

### HU-04: Identidad visual configurable
Como restaurante quiero que mis pantallas muestren mi nombre, logo y colores.

- **RF-14**: Cuando se consulta `GET /api/r/{slug}/identidad` de un restaurante activo, el sistema deberá devolver sin autenticación su nombre, su paleta y la dirección de su logo (o ninguna si no tiene).
- **RF-15**: Cuando se consulta `GET /api/r/{slug}/logo`, el sistema deberá devolver la imagen con su tipo de contenido y cabeceras de caché con `ETag`, y 304 si no cambió.
- **RF-16**: Cuando el frontend carga una pantalla de un restaurante, el sistema deberá aplicar su paleta como variables de tema y, si falta algún color, el valor por defecto de la constitución (Art. 15).

### HU-05: Roles de la plataforma
Como operador quiero que los roles coincidan con la constitución.

- **RF-17**: Cuando se aplican las migraciones, el sistema deberá usar los roles `administrador_plataforma`, `administrador_general`, `administrador_sucursal`, `cajero` y `cocinero`, y el administrador inicial creado desde `ADMIN_USERNAME`/`ADMIN_PASSWORD` deberá tener el rol `administrador_plataforma`.

### HU-06: Backoffice web
Como usuario quiero pantallas para entrar y administrar.

- **RF-18**: Cuando el administrador de la plataforma entra en `/plataforma/login` y luego en `/plataforma/restaurantes`, el sistema deberá mostrar la lista con estado y permitir crear, editar (nombre, paleta con vista previa, logo), activar y desactivar restaurantes, pidiendo confirmación para desactivar.
- **RF-19**: Cuando un usuario entra en `/r/{slug}/login`, el sistema deberá mostrar el nombre, el logo y los colores del restaurante, y tras iniciar sesión llevarlo a `/r/{slug}/inicio`, que muestra el encabezado con la marca, "Hola, {usuario}" y "Cerrar sesión".
- **RF-20**: Si `/r/{slug}/…` corresponde a un restaurante inexistente o inactivo, el sistema deberá mostrar la pantalla "Restaurante no disponible".
- **RF-21**: Si un usuario sin sesión o sin el rol requerido abre una pantalla protegida, el sistema deberá llevarlo al login correspondiente (`/plataforma/login` o `/r/{slug}/login`).

## Requisitos no funcionales

- **RNF-01**: El token se guarda en memoria y en `sessionStorage` (nunca en `localStorage`) y la sesión termina al expirar (8 h).
- **RNF-02**: Los colores sólo se aplican mediante variables CSS de tema; ningún componente usa colores literales (Art. 15.3).
- **RNF-03**: Las pantallas funcionan en escritorio (1440 px) y móvil (390 px).
- **RNF-04**: La migración parte de una base limpia: no se convierten los usuarios de prueba de 001.

## Fuera de alcance

- Sucursales, horarios y aislamiento por sucursal (HT-0 y SUC-01, feature 003).
- Asignación de usuarios a sucursales, alcance del administrador de sucursal, cierre de sesiones al desactivar un usuario y pantallas de usuarios (AUTH-01, feature 004).
- Elegir la tipografía por restaurante (queda fija en Nunito).
- Cambio y recuperación de contraseña; subdominios por restaurante.

## Preguntas abiertas

- Ninguna
