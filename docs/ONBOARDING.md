# Onboarding

Cómo levantar el sistema (feature 001-plataforma-base: API de usuarios y autenticación) y
ejecutar las pruebas. Todos los comandos se ejecutan desde la **raíz del repositorio**.

## Requisitos previos

- **Docker** con Docker Compose v2 (para levantar el sistema y para las pruebas `Integracion`).
- **SDK de .NET 10** (para compilar y probar la API fuera de Docker).
- **Node 18+ con npm** (para `apps/web`).

## Levantar el sistema con Docker

1. Crea tu archivo de entorno a partir de la plantilla:

   ```bash
   cp .env.example .env
   ```

2. Edita `.env` y sustituye cada *placeholder* `<...>`:
   - `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` y la misma información dentro de
     `DB_CONNECTION_STRING` (el host es `db`, el servicio de compose).
   - `JWT_SIGNING_KEY`: clave aleatoria de **al menos 32 caracteres**.
   - `CORS_ALLOWED_ORIGINS`: orígenes separados por comas (por defecto
     `http://localhost:5173,http://localhost:8080`).
   - `ADMIN_USERNAME` y `ADMIN_PASSWORD`: administrador inicial, que se crea al primer arranque.

   `.env` está en `.gitignore`; nunca lo subas al repositorio.

3. Levanta la base de datos y la API:

   ```bash
   docker compose up
   ```

   La API aplica las migraciones al arrancar (`APPLY_MIGRATIONS=true`) y crea el administrador
   inicial. Escucha en `http://localhost:5000`.

4. Comprueba que está viva:

   ```bash
   curl -i localhost:5000/health
   ```

   Debe responder `200` (si la base de datos no es accesible responde `503`).

Para parar: `docker compose down` (añade `-v` para borrar también el volumen `datos-db`).

## Ejecutar las pruebas

- **Suite habitual** (`comando_test` de `harness.config.json`; no necesita Docker):

  ```bash
  (cd apps/web && npx vitest run --passWithNoTests && npx playwright test --pass-with-no-tests) \
    && dotnet test apps/api --filter "Category!=Integracion"
  ```

- **Suite de integración** (PostgreSQL real con Testcontainers; **requiere Docker en marcha**):

  ```bash
  dotnet test apps/api --filter "Category=Integracion"
  ```

## Documentación de apoyo

- Peticiones de ejemplo (`.http`): [`context/http/`](context/http/)
  (`health.http`, `auth.http`, `usuarios.http`).
- Mapa de arquitectura: [`context/architecture-map.md`](context/architecture-map.md).
- Glosario de dominio: [`context/glossary.md`](context/glossary.md) y de código
  [`GLOSSARY.md`](GLOSSARY.md).
- Reglas del proyecto: [`../specs/000-constitucion/constitucion.md`](../specs/000-constitucion/constitucion.md).
