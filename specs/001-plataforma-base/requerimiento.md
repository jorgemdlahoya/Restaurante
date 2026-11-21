---
feature: 001-plataforma-base
estado: borrador
---
# Requerimiento: Plataforma base — backend

## Contexto y propósito

Primera feature tras la constitución. Construye el **backend** sobre el que se apoyarán todas
las funcionalidades del restaurante: la solución .NET 10 en cuatro capas, el acceso con JWT y
roles, la gestión mínima de usuarios, el manejo uniforme de errores, el logging, la
configuración por entorno, la salud del servicio y el despliegue con `docker-compose`.

Implementa los requisitos transversales de la constitución (sus RF-01 a RF-11) para el backend.
El frontend (esqueleto React, pantalla de login) es la feature `002-plataforma-frontend`.

## Actores

- **Administrador**: inicia sesión y gestiona los usuarios y sus roles.
- **Mesero, Cocina, Caja**: inician sesión; en esta feature sólo pueden autenticarse (las
  funciones de su área llegan en features posteriores).
- **Operador del despliegue**: levanta el sistema y lo configura por variables de entorno.
- **Desarrollador del frontend**: consume la API y su documentación.

## Historias de usuario

### HU-01: Inicio de sesión
Como usuario del restaurante quiero iniciar sesión con mi nombre de usuario y contraseña para
usar las funciones de mi rol.

- **RF-01**: Cuando un usuario activo envía credenciales válidas a `POST /api/auth/login`, el sistema deberá responder 200 con un JWT firmado que incluya su identificador, su nombre de usuario y su rol, y que expire a las 8 horas, junto con la fecha de expiración.
- **RF-02**: Si el nombre de usuario no existe o la contraseña es incorrecta, el sistema deberá responder 401 con el mismo mensaje en ambos casos.
- **RF-03**: Si el usuario está desactivado, el sistema deberá responder 401 con el mismo mensaje que para credenciales incorrectas.
- **RF-04**: Si falta el nombre de usuario o la contraseña en la petición de login, el sistema deberá responder 400 con un `ValidationProblemDetails` que indique el campo.

### HU-02: Acceso por rol
Como administrador quiero que cada endpoint sólo lo puedan usar los roles autorizados, para
proteger la operación.

- **RF-05**: Si una petición llega a un endpoint protegido sin JWT, con un JWT de firma inválida o con un JWT expirado, el sistema deberá responder 401 con un `ProblemDetails`.
- **RF-06**: Si una petición llega con un JWT válido cuyo rol no está autorizado para ese endpoint, el sistema deberá responder 403 con un `ProblemDetails`.
- **RF-07**: Cuando una petición llega a `POST /api/auth/login` o a `GET /health`, el sistema deberá atenderla sin exigir JWT.

### HU-03: Administrador inicial
Como operador del despliegue quiero que el sistema cree el primer administrador desde la
configuración, para poder entrar sin versionar credenciales.

- **RF-08**: Cuando la API arranca y no existe ningún usuario, el sistema deberá crear un usuario activo con rol `administrador` usando `ADMIN_USERNAME` y `ADMIN_PASSWORD`, guardando la contraseña sólo como hash.
- **RF-09**: Cuando la API arranca y ya existe al menos un usuario, el sistema no deberá crear ni modificar usuarios, aunque `ADMIN_USERNAME` y `ADMIN_PASSWORD` estén definidas.
- **RF-10**: Si al arrancar no existe ningún usuario y falta `ADMIN_USERNAME` o `ADMIN_PASSWORD`, el sistema deberá negarse a arrancar e indicar en el log el nombre de la variable que falta, sin mostrar valores.

### HU-04: Gestión de usuarios
Como administrador quiero crear usuarios, asignarles un rol y desactivarlos, para que el
personal acceda sólo a lo que le corresponde.

- **RF-11**: Cuando un administrador envía a `POST /api/usuarios` un nombre de usuario, una contraseña y un rol válidos, el sistema deberá crear el usuario activo y responder 201 con sus datos (identificador, nombre de usuario, rol, activo) sin la contraseña ni su hash.
- **RF-12**: Si el nombre de usuario ya existe, sin distinguir mayúsculas de minúsculas, el sistema deberá responder 409 con un `ProblemDetails`.
- **RF-13**: Si el nombre de usuario no tiene entre 3 y 50 caracteres de letras, números, `.`, `_` o `-`, si la contraseña tiene menos de 8 caracteres o si el rol no es `mesero`, `cocina`, `caja` o `administrador`, el sistema deberá responder 400 con un `ValidationProblemDetails` que liste los errores por campo.
- **RF-14**: Cuando un administrador consulta `GET /api/usuarios`, el sistema deberá responder 200 con la lista de usuarios (identificador, nombre de usuario, rol, activo), sin contraseñas ni hashes.
- **RF-15**: Cuando un administrador envía a `PUT /api/usuarios/{id}/estado` el valor activo o inactivo, el sistema deberá actualizar el estado del usuario y responder 200 con sus datos.
- **RF-16**: Cuando un administrador envía a `PUT /api/usuarios/{id}/rol` un rol válido, el sistema deberá cambiar el rol del usuario y responder 200 con sus datos.
- **RF-17**: Si una desactivación o un cambio de rol dejaría el sistema sin ningún administrador activo, el sistema deberá rechazarlo con 409 y un `ProblemDetails`.
- **RF-18**: Si un administrador intenta desactivarse a sí mismo, el sistema deberá rechazarlo con 409 y un `ProblemDetails`.
- **RF-19**: Si el `{id}` de una operación sobre un usuario no existe, el sistema deberá responder 404 con un `ProblemDetails`.

### HU-05: Errores y trazabilidad
Como desarrollador quiero errores uniformes y un registro de cada operación, para integrar y
diagnosticar el sistema.

- **RF-20**: Si ocurre una excepción no controlada, el sistema deberá responder 500 con un `ProblemDetails` que incluya un `traceId` y ningún detalle interno, y registrar la excepción en el log con ese `traceId`.
- **RF-21**: Cuando el sistema procesa un comando o una consulta, el sistema deberá registrar en el log su nombre, su duración en milisegundos y si terminó bien o con error, sin incluir contraseñas, hashes, tokens ni cadenas de conexión.

### HU-06: Configuración, salud y despliegue
Como operador del despliegue quiero configurar el sistema sólo por entorno y saber si está
sano, para operarlo sin tocar el código.

- **RF-22**: Si al arrancar falta `DB_CONNECTION_STRING`, `JWT_SIGNING_KEY` o `CORS_ALLOWED_ORIGINS`, o si `JWT_SIGNING_KEY` tiene menos de 32 caracteres, el sistema deberá negarse a arrancar e indicar en el log el nombre de la variable y el motivo, sin mostrar valores.
- **RF-23**: Cuando se consulta `GET /health`, el sistema deberá responder 200 si la base de datos es accesible y 503 si no lo es.
- **RF-24**: Si una petición proviene de un origen que no figura en `CORS_ALLOWED_ORIGINS`, el sistema no deberá devolver la cabecera `Access-Control-Allow-Origin`; si figura, deberá devolverla con ese origen.
- **RF-25**: Cuando la API se ejecuta en el entorno `Development`, el sistema deberá exponer Swagger UI con todos los endpoints y el esquema de autenticación Bearer; en otros entornos no deberá exponerlo.
- **RF-26**: Cuando la API arranca con `APPLY_MIGRATIONS=true`, el sistema deberá aplicar las migraciones pendientes antes de atender peticiones; con otro valor o sin la variable, no deberá aplicarlas.

## Requisitos no funcionales

- **RNF-01**: .NET 10 / ASP.NET Core, MediatR 12.x, FluentValidation, EF Core con Npgsql y PostgreSQL 16+, según la constitución.
- **RNF-02**: Un test de arquitectura (NetArchTest) falla si `Domain` referencia otra capa o paquete externo, o si `Application` referencia `Infrastructure` o `WebApi`.
- **RNF-03**: `docker compose up` con un `.env` completo levanta `api` y `db`, y `GET /health` responde 200 sin pasos manuales.
- **RNF-04**: Las pruebas que necesitan Docker (Testcontainers) llevan `[Trait("Category", "Integracion")]`; el resto se ejecuta sin Docker.
- **RNF-05**: Cada endpoint tiene su archivo `.http` en `docs/context/http/` con *placeholders* para credenciales (constitución, Art. 5).

## Fuera de alcance

- Frontend (feature `002-plataforma-frontend`) y el servicio `web` de `docker-compose`.
- Funcionalidades de negocio: menú, mesas, pedidos, cocina, caja, reportes.
- Borrado de usuarios, edición del nombre de usuario, cambio o recuperación de contraseña.
- Revocación inmediata de tokens: un token emitido a un usuario luego desactivado sigue siendo
  válido hasta su expiración (máximo 8 horas).
- *Refresh tokens*, bloqueo por intentos fallidos, auditoría de cambios.
- Clientes HTTP hacia servicios externos (llegarán con la feature que los necesite).

## Preguntas abiertas

- Ninguna
