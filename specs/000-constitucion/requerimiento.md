---
feature: 000-constitucion
estado: borrador
---
# Requerimiento: Constitución del proyecto

## Contexto y propósito

Plataforma **multi-restaurante** de menú y pedidos: el operador de la plataforma da de alta
restaurantes independientes, cada uno **multi-sucursal**, con backoffice de menú,
sucursales y usuarios; pedidos por chatbot de WhatsApp y por web; tablero de caja y cocina; e
impresión de tickets. El backlog definitivo está en `docs/backlog/BACKLOG.md`. Se compone de un
frontend web (React) y un microservicio backend con base de datos propia que el frontend consume
por HTTP.

Esta constitución fija las decisiones técnicas y los requisitos **transversales** que toda
feature debe cumplir: acceso por roles, validación y errores uniformes, trazabilidad,
configuración por entorno, despliegue y documentación del API. No incluye funcionalidades de
negocio ni código: la plataforma base se construye en la feature `001-plataforma-base`.

## Actores

- **Administrador de la plataforma**: da de alta, edita y desactiva restaurantes y crea su primer administrador general.
- **Administrador general**: gestiona menú, precios, sucursales y usuarios de todas las sucursales de su restaurante.
- **Administrador de sucursal**: gestiona agotados, personal e impresoras de su sucursal.
- **Cajero**: ve los pedidos de su(s) sucursal(es), cobra y marca "Entregada".
- **Cocinero**: ve los pedidos de su(s) sucursal(es) y marca "En preparación" y "Lista".
- **Cliente**: consulta el menú y pide por WhatsApp o por la web.
- **Equipo de desarrollo**: construye y mantiene el sistema siguiendo esta constitución.

## Historias de usuario

### HU-01: Acceso por roles
Como administrador general quiero que sólo los usuarios autenticados, y según su rol, puedan usar cada
función del sistema, para proteger la operación del restaurante.

- **RF-01**: Cuando un usuario envía credenciales válidas al endpoint de login, el sistema deberá devolver un JWT firmado que incluya su identificador, su rol y su fecha de expiración.
- **RF-02**: Si un usuario envía credenciales inválidas al endpoint de login, el sistema deberá responder 401 sin indicar si falló el usuario o la contraseña.
- **RF-03**: Si una petición llega a un endpoint protegido sin un JWT válido o con un JWT expirado, el sistema deberá responder 401.
- **RF-04**: Si una petición llega con un JWT válido cuyo rol no está autorizado para esa acción, el sistema deberá responder 403.

### HU-02: Validación y errores uniformes
Como desarrollador del frontend quiero que todos los errores de la API tengan el mismo formato,
para mostrarlos de forma consistente al usuario.

- **RF-05**: Si los datos de un comando o consulta no superan su validación, el sistema deberá responder 400 con un `ProblemDetails` que liste los errores por campo.
- **RF-06**: Si ocurre una excepción no controlada al procesar una petición, el sistema deberá responder 500 con un `ProblemDetails` que incluya un identificador de traza y ningún detalle interno (trazas, mensajes de excepción, SQL).

### HU-03: Trazabilidad operativa
Como equipo de desarrollo quiero que cada operación quede registrada, para diagnosticar
problemas en producción.

- **RF-07**: Cuando el sistema procesa un comando o una consulta, el sistema deberá registrar en el log su nombre, su duración y si terminó bien o con error, sin incluir datos sensibles (contraseñas, tokens, cadenas de conexión).

### HU-04: Configuración y despliegue
Como equipo de desarrollo quiero levantar el sistema completo de forma reproducible y
configurarlo sólo por entorno, para no versionar secretos.

- **RF-08**: Si al arrancar falta una variable de entorno obligatoria (cadena de conexión, clave de firma JWT u orígenes CORS), el sistema deberá negarse a arrancar e indicar en el log el nombre de la variable que falta, sin mostrar valores.
- **RF-09**: Cuando se consulta el endpoint de salud, el sistema deberá responder 200 si la base de datos es accesible y 503 si no lo es.
- **RF-10**: Si una petición proviene de un origen que no figura en `CORS_ALLOWED_ORIGINS`, el sistema no deberá devolver cabeceras CORS que la autoricen.

### HU-05: Documentación del API
Como desarrollador del frontend quiero ver y probar los endpoints disponibles, para integrarme
sin leer el código del backend.

- **RF-11**: Cuando el sistema se ejecuta en el entorno `Development`, el sistema deberá exponer Swagger UI con todos los endpoints, sus DTOs y el esquema de autenticación JWT.

## Requisitos no funcionales

- **RNF-01**: El backend usa .NET 10 / ASP.NET Core y PostgreSQL 16 o superior; el frontend, React + TypeScript + Vite con Vitest y Playwright (preset del harness). MediatR se fija en la versión 12.x (licencia Apache 2.0).
- **RNF-02**: Un único comando (`comando_test` de `harness.config.json`) ejecuta las pruebas del frontend (Vitest y la suite simulada de Playwright) y del backend que no necesitan Docker, y debe terminar en verde antes de cerrar cualquier tarea; las pruebas de integración del backend (Testcontainers) y la suite completa de Playwright (`comando_e2e_stack`) deben pasar antes de cerrar cada feature.
- **RNF-03**: Un test de arquitectura falla si alguna capa del backend viola la regla de dependencias de Clean Architecture.
- **RNF-04**: Las contraseñas se almacenan sólo como hash; el JWT expira a las 8 horas (un turno).
- **RNF-05**: `docker compose up` con un `.env` completo levanta `web`, `api` y `db` sin pasos manuales adicionales.
- **RNF-06** (backlog P-37): un cambio de precio o disponibilidad se refleja en los canales en ≤ 60 segundos.
- **RNF-07** (backlog P-37): el chatbot responde en < 3 segundos por mensaje.
- **RNF-08** (backlog P-37): 50 pedidos simultáneos por sucursal sin errores ni números de orden duplicados.
- **RNF-09** (backlog P-37): disponibilidad del 99.5 % mensual; logs estructurados y alertas de fallas en webhook, envío de WhatsApp e impresión.
- **RNF-10** (backlog P-36): sólo teléfonos de México (+52), almacenados en formato E.164.
- **RNF-11** (backlog P-08): precios con IVA incluido; tasa configurable (16 % por defecto) sólo para el desglose informativo.

## Fuera de alcance

- Código de cualquier tipo: la plataforma base (solución .NET, `docker-compose`, pipeline,
  filtros, login, esqueleto React) es la feature `001-plataforma-base`.
- Funcionalidades de negocio: se construyen feature a feature según `docs/backlog/BACKLOG.md`.
- Lo que el backlog marca como fuera de alcance (p. ej. agente de impresión para PC, iOS) y lo que
  no contempla: pagos en línea, facturación electrónica, *refresh tokens*, recuperación de
  contraseña.
- La app Android de impresión (E4) se incorporará a esta constitución (tecnología, ruta de código
  y pruebas) cuando se aborde su sprint.

## Preguntas abiertas

- Ninguna
