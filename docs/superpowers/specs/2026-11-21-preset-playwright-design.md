---
titulo: Playwright en el preset de frontend del harness
fecha: 2026-11-21
estado: borrador
---

# Playwright en el preset de frontend del harness

## Intención

El usuario quiere que las pruebas del frontend se hagan con **Playwright** (`@playwright/test`),
además de Vitest. Vitest sigue para componentes y lógica (TDD rápido, Art. 2); Playwright
cubre los flujos de punta a punta en navegador real (evidencia de "comportamiento observado",
Art. 4). Como el harness se reutiliza en varios proyectos React, va en el preset.

- **Objetivo:** todo proyecto del harness tiene dos suites de Playwright: una con la API
  simulada que bloquea cada tarea y otra contra el sistema completo que bloquea el cierre de la
  feature.
- **No objetivo:** reemplazar Vitest; probar el backend con Playwright; usar el plugin de
  navegador de la sesión como sustituto de las pruebas versionadas.
- **Anti-objetivo:** que el ciclo de cada tarea dependa de Docker, .NET o PostgreSQL.

## Enfoques

**Herramienta** (elegido por el usuario: A)
- A) Vitest + Playwright — elegido.
- B) Sólo Playwright (con *component testing*): descartado, suite lenta y component testing
  experimental.
- C) Sólo Playwright de punta a punta: descartado, fallos de lógica detectados tarde.

**Contra qué corre Playwright** (elegido por el usuario: C)
- A) Sólo frontend con API simulada.
- B) Sólo sistema completo con `docker compose`.
- C) Ambas: la simulada en `comando_test` (bloquea cada tarea) y la completa como suite aparte
  que bloquea el cierre de la feature — elegido.

## Diseño

- **Preset renombrado** a `react-ts-vite-vitest-playwright` (llamada del agente: el nombre
  dice qué exige). `PRESET_FRONTEND` gana `"e2e": "playwright"`; `extraer_config` exige además
  que `comando_test` contenga `playwright`. Las configuraciones ya generadas no se re-validan
  (siguen funcionando); re-aprobar una constitución antigua exige el preset nuevo.
- **Clave nueva obligatoria en `harness-config`: `comando_e2e_stack`** (string que contiene
  `playwright`), p. ej.
  `cd apps/web && npx playwright test --config playwright.stack.config.ts`.
- **Suite simulada** (`apps/web/e2e/`): Playwright levanta la app con Vite (`webServer`) y
  responde a la API con `page.route`. Entra en `comando_test` con `--pass-with-no-tests`.
  Cada test lleva el RF calificado: `test("NNN/RF-NN: …")`, que `completar-tarea` ya detecta
  porque `apps/web/` es ruta de código.
- **Suite completa** (`apps/web/e2e-stack/`): mismo Playwright contra `E2E_BASE_URL` (por
  defecto `http://localhost:8080`, el `web` de `docker-compose`), sin simulaciones. Se ejecuta con
  `comando_e2e_stack` tras `docker compose up`.
- **Bloqueo del cierre** (llamada del agente): `aceptar.py`, cuando la aceptación cerraría la
  feature (última HU), ejecuta `comando_e2e_stack` y, si falla o no está configurado, **no**
  registra la aceptación y muestra la cola de la salida. Las HU anteriores se aceptan sin
  ejecutarla. Timeout configurable con `HARNESS_E2E_TIMEOUT` (por defecto 1800 s).
- **Plantilla de constitución:** sección de frontend con las dos suites y bloque
  `harness-config` con `frontend` nuevo,
  `comando_test` = `(cd apps/web && npx vitest run --passWithNoTests && npx playwright test --pass-with-no-tests)`
  y `comando_e2e_stack` = `cd apps/web && npx playwright test --config playwright.stack.config.ts`.
- **Comandos:** `/planificar` incluye una prueba Playwright (suite simulada) en cada tarea que
  toca un flujo visible de la UI; `/implementar` lo exige y, antes de pedir la última
  aceptación, recuerda levantar el sistema y ejecutar la suite completa.
- **`CLAUDE.md`, `README.md`** y spec del preset React actualizados.
- **Constitución en curso** (`specs/000-constitucion`, rama `spec/000-constitucion`): se
  actualiza su `diseno.md` (frontend, estrategia de pruebas, bloque de config) una vez
  integrado este cambio, antes de `/aprobar 000`.

## Ralph Loop en las tareas con flujo visible

El usuario pide complementar las pruebas de Playwright con el plugin **Ralph Loop** (bucle que
repite la misma instrucción hasta que el agente emite `<promise>…</promise>` o se agota
`--max-iterations`).

- **Enfoques:** (A) en cada tarea con flujo visible, para la fase verde — **elegido por el
  usuario**; (B) sólo como rescate cuando la prueba sigue fallando — descartado: se usaría
  poco; (C) también para la suite completa antes del cierre — descartado por ahora: exige
  Docker en la máquina donde corre el bucle.
- **Flujo en `/implementar`**, para cada tarea que toca un flujo visible de la UI:
  1. Escribir la prueba Playwright (suite simulada, `test("NNN/RF-NN: …")`) y verla fallar (rojo).
  2. Invocar el skill `ralph-loop:ralph-loop` con
     `--completion-promise "T-NN VERDE"` y `--max-iterations 8`, y esta instrucción:
     "Implementa T-NN de `specs/<feature>/tareas.md` hasta que
     `python3 .claude/scripts/completar-tarea.py T-NN` termine con código 0. No modifiques las
     pruebas Playwright de los RF de T-NN. Sólo cuando `completar-tarea` termine en verde,
     escribe `<promise>T-NN VERDE</promise>`."
  3. Si se agotan las 8 iteraciones: el bucle se detiene, el agente aplica
     `superpowers:systematic-debugging` e informa al usuario; la tarea no se da por hecha.
  4. La revisión de código de la tarea comprueba que el diff no altera las pruebas Playwright
     escritas en el paso 1.
  Las tareas sin flujo visible siguen el TDD normal, sin Ralph Loop.
- **Compatibilidad con el gate:** Ralph Loop guarda su estado en
  `.claude/ralph-loop.local.md`. El gate (Write/Edit y Bash) permite escribir y borrar
  **sólo ese archivo** dentro de `.claude/`; el resto de `.claude/` sigue protegido. El archivo
  se añade a `.gitignore`.
- `/planificar` marca en `tareas.md` las tareas con flujo visible (línea `UI: sí`), para que
  `/implementar` sepa cuáles usan Ralph Loop.

> Corrección tras la revisión final: el estado de Ralph Loop es `.claude/ralph-loop.local.md`
> (sin punto); los comandos usan `cd apps/web && npx …` porque `npm --prefix … exec` ejecuta
> desde la raíz y descarta las opciones; las tareas con `UI: sí` se ejecutan en línea por el
> agente principal.

## Pruebas

- `extraer_config` rechaza: `frontend` antiguo, `comando_test` sin `playwright`, falta de
  `comando_e2e_stack` o sin `playwright`; acepta el bloque de la plantilla.
- `aceptar.py`: HU no final se acepta sin ejecutar la suite completa; HU final con
  `comando_e2e_stack` en verde cierra la feature; en rojo, ausente o con timeout no registra la
  aceptación.
- La plantilla de constitución contiene la sección de Playwright y su bloque pasa la validación.
- Se actualizan fixtures y la prueba extremo a extremo (comandos `vitest`/`playwright`
  simulados en el `PATH`).
- Gate: permite Write y `rm` de `.claude/ralph-loop.local.md`; sigue bloqueando cualquier
  otro archivo de `.claude/` (p. ej. `.claude/otro.local.md`, `.claude/settings.json`).
- `implementar.md` menciona `ralph-loop:ralph-loop`, `--completion-promise`, `--max-iterations 8`
  y la prohibición de modificar las pruebas; `planificar.md` menciona `UI: sí`; `.gitignore`
  incluye `.claude/ralph-loop.local.md`.

## Forma de trabajo

Como en el cambio anterior: los hooks protegen `.claude/` y `CLAUDE.md`, así que la
implementación se hace en una copia aparte del repo (requiere autorización del usuario), con
verificación final en el proyecto con los hooks activos, y se entrega como Pull Request.

## Decisiones del agente (revisables)

- Nombre del preset `react-ts-vite-vitest-playwright`.
- `comando_e2e_stack` obligatorio y ejecutado por `aceptar.py` al cerrar la feature.
- Carpetas `apps/web/e2e/` (simulada) y `apps/web/e2e-stack/` (completa); `E2E_BASE_URL` por
  defecto `http://localhost:8080`.
- Ralph Loop: tope de 8 iteraciones, promesa `T-NN VERDE` ligada a `completar-tarea`, marca
  `UI: sí` en `tareas.md`.
