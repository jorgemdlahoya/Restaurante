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
   - `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`. La cadena de conexión de la API
     (`DB_CONNECTION_STRING`) la compone `docker-compose.yml` con estos tres valores (host `db`,
     puerto 5432), por lo que no se define en `.env`. La contraseña **no debe contener `;`**.
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
   inicial. Escucha en `http://localhost:5000`. El servicio `web` (nginx con la interfaz
   compilada, `apps/web/Dockerfile`) escucha en `http://localhost:8080` y reenvía `/api/` y
   `/health` a la API; úsalo con `docker compose up -d --build`.

4. Comprueba que está viva:

   ```bash
   curl -i localhost:5000/health
   ```

   Debe responder `200` (si la base de datos no es accesible responde `503`).

Para parar: `docker compose down` (añade `-v` para borrar también el volumen `datos-db`).

## Ejecutar la API sin compose

La API lee `DB_CONNECTION_STRING` del entorno del proceso; compose la construye por ti, pero si
arrancas la API a mano (`dotnet run --project apps/api/src/Restaurante.WebApi`) debes exportarla
junto con el resto de variables obligatorias:

```bash
export DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=<POSTGRES_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>"
export JWT_SIGNING_KEY="<clave-aleatoria-de-al-menos-32-caracteres>"
export CORS_ALLOWED_ORIGINS="http://localhost:5173"
export ADMIN_USERNAME="<usuario-administrador>"   # 3-50 caracteres: letras, números, . _ -
export ADMIN_PASSWORD="<contrasena-del-administrador>"   # mínimo 8 caracteres
```

Sin `DB_CONNECTION_STRING` (o con otra variable obligatoria ausente) la API se niega a arrancar y
lo indica en el log.

## Recuperar el acceso de administrador

El sistema impide dejar el último administrador activo (RF-17) y el administrador inicial
sólo se crea cuando no existe ningún administrador de plataforma (RF-09; el filtro global por
restaurante hace que sólo se cuenten los usuarios sin restaurante), así que **no se vuelve a sembrar**. Si aun así
se pierde el acceso (p. ej. dos administradores que se degradan o desactivan a la vez), la
**única vía de recuperación** es reactivar y promover un usuario existente directamente en la base
de datos, con el sistema levantado:

```bash
docker compose exec db psql -U <POSTGRES_USER> -d <POSTGRES_DB> -c \
  "UPDATE usuarios SET activo = true, rol = 'administrador_general' WHERE nombre_usuario_normalizado = '<usuario>';"
```

Sustituye `<POSTGRES_USER>` y `<POSTGRES_DB>` por los valores de tu `.env` y `<usuario>` por el
nombre del usuario **en minúsculas**. Debe responder `UPDATE 1`; después inicia sesión con él.

## Levantar el frontend

El frontend (React + TypeScript + Vite) vive en `apps/web`. Con la API en `http://localhost:5000`:

```bash
cd apps/web
npm install
npm run dev      # http://localhost:5173 (proxy de /api y /health hacia http://localhost:5000)
npm run build    # tsc --noEmit && vite build
```

`VITE_API_URL` (opcional) fija la URL base de la API; sin ella se usan rutas relativas con el proxy.
Las pruebas de Playwright (`npx playwright test`) arrancan solas el servidor de Vite en el puerto 5173.

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

- **Suite de punta a punta contra el sistema levantado** (`comando_e2e_stack`; requiere
  `docker compose up -d --build` en marcha). La prueba de alta de restaurante necesita
  `ADMIN_USERNAME` y `ADMIN_PASSWORD`, así que exporta `.env` antes:

  ```bash
  set -a; . ./.env; set +a
  dotnet test apps/api --filter "Category=Integracion" \
    && (cd apps/web && npx playwright test --config playwright.stack.config.ts)
  ```

  `E2E_BASE_URL` (por defecto `http://localhost:8080`) apunta a la interfaz y `E2E_API_URL`
  (por defecto `http://localhost:5000`) a la API.

## Documentación de apoyo

- Peticiones de ejemplo (`.http`): [`context/http/`](context/http/)
  (`health.http`, `auth.http`, `usuarios.http`, `restaurantes.http`).
- Mapa de arquitectura: [`context/architecture-map.md`](context/architecture-map.md).
- Glosario de dominio: [`context/glossary.md`](context/glossary.md) y de código
  [`GLOSSARY.md`](GLOSSARY.md).
- Reglas del proyecto: [`../specs/000-constitucion/constitucion.md`](../specs/000-constitucion/constitucion.md).
