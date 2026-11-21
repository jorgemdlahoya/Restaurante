# Playwright + Ralph Loop en el preset — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir Playwright (suite simulada + suite completa) al preset de frontend, bloquear el cierre de la feature con la suite completa y usar Ralph Loop en la fase verde de las tareas con flujo visible.

**Architecture:** `PRESET_FRONTEND` y `_validar_preset` en `harness_lib.py` amplían las exigencias del bloque `harness-config`; un helper común ejecuta comandos con timeout para `completar-tarea.py` y `aceptar.py`; el gate permite sólo el archivo de estado de Ralph Loop dentro de `.claude/`; los comandos `.md` describen el flujo.

**Tech Stack:** Python 3 stdlib, pytest, Markdown.

**Spec:** `docs/superpowers/specs/2026-11-21-preset-playwright-design.md` (y `2026-11-14-preset-react-design.md`).

## Global Constraints

- Se trabaja en una copia aparte del repo (autorizada); verificación final en el proyecto con los hooks activos; entrega por Pull Request.
- Preset: `{"id": "react-ts-vite-vitest-playwright", "ruta": "apps/web/", "test": "vitest", "e2e": "playwright"}`.
- `comando_test` debe contener `vitest` y `playwright`; `comando_e2e_stack` es obligatorio y debe contener `playwright`.
- Plantilla: `comando_test` = `(cd apps/web && npx vitest run --passWithNoTests && npx playwright test --pass-with-no-tests)`; `comando_e2e_stack` = `cd apps/web && npx playwright test --config playwright.stack.config.ts`.
- Suites: `apps/web/e2e/` (API simulada con `page.route`) y `apps/web/e2e-stack/` (contra `E2E_BASE_URL`, por defecto `http://localhost:8080`).
- Ralph Loop: `--completion-promise "T-NN VERDE"`, `--max-iterations 8`; estado en `.claude/ralph-loop.local.md` (único archivo escribible de `.claude/`, en `.gitignore`).
- `HARNESS_E2E_TIMEOUT` por defecto 1800 s. Textos en español; scripts sólo stdlib.

## Review Focus

1. Docker apagado al aceptar la última HU → `comando_e2e_stack` falla; no se registra la aceptación y el mensaje sugiere `docker compose up`. (Task 3)
2. Feature con una sola HU → su aceptación es la final y ejecuta la suite completa. (Task 3)
3. Config generada antes de este cambio (sin `comando_e2e_stack`) → la última aceptación se rechaza con un mensaje que explica cómo añadirla. (Task 3)
4. `rm -f ./.claude/ralph-loop.local.md` (con flag y `./`) → permitido. (Task 4)
5. Variantes que no son el archivo de Ralph (`.claude/ralph-loop.local.md`, `.claude/ralph-loop.local.md.bak`) → bloqueadas. (Task 4)

---

### Task 1: Validación del preset con Playwright

**Files:**
- Modify: `.claude/scripts/harness_lib.py` (`PRESET_FRONTEND`, `_validar_preset`)
- Modify: `tests/harness/conftest.py` (`CONFIG_BLOQUE`, `constitucion_aprobada`), `tests/harness/test_preset.py` (`BASE`), `tests/harness/test_e2e.py`
- Test: `tests/harness/test_playwright.py`

**Interfaces:**
- Produces: `PRESET_FRONTEND` con la clave `"e2e"`; `extraer_config(texto)` rechaza (con `"fijo del harness"` en el mensaje): `frontend` distinto de `react-ts-vite-vitest-playwright`; `comando_test` sin `playwright` (mensaje contiene `"playwright"`); `comando_e2e_stack` ausente, no string o sin `playwright` (mensaje contiene `"comando_e2e_stack"`).

- [ ] **Step 1: Tests que fallan** en `test_playwright.py` (helper `bloque(**cambios)` como en `test_preset.py`, con la base de la plantilla):
  - `test_bloque_playwright_valido` → `extraer_config(bloque())["comando_e2e_stack"]` contiene `"playwright"`.
  - `test_frontend_antiguo_rechazado` (`frontend="react-ts-vite-vitest"`) → mensaje contiene `"frontend"`.
  - `test_comando_test_sin_playwright` (`comando_test="npm --prefix apps/web exec vitest run"`) → `"playwright"`.
  - `test_falta_comando_e2e_stack` (`comando_e2e_stack=None`) y `test_e2e_stack_sin_playwright` (`"npm run e2e"`) → `"comando_e2e_stack"`.
- [ ] **Step 2:** `python3 -m pytest tests/harness/test_playwright.py -q` → FAIL.
- [ ] **Step 3:** Implementar. Actualizar `CONFIG_BLOQUE`, `BASE` de `test_preset.py`, `constitucion_aprobada` (añade `comando_e2e_stack`) y `test_e2e.py` (`comando_test` = `"vitest run && playwright test"` con dos ejecutables falsos `vitest` y `playwright` en el `PATH`, el segundo `exit 0`).
- [ ] **Step 4:** `python3 -m pytest tests/harness -q` → PASS.
- [ ] **Step 5:** `git commit -am "feat(harness): preset con Playwright"`

### Task 2: Plantilla de constitución con Playwright

**Files:**
- Modify: `specs/_plantillas/constitucion/diseno.md`
- Test: `tests/harness/test_playwright.py`

- [ ] **Step 1: Test que falla** `test_plantilla_constitucion_playwright`: tras `nueva-feature.py` en proyecto vacío, `diseno.md` contiene `"Playwright"`, `"apps/web/e2e/"`, `"apps/web/e2e-stack/"` y `extraer_config(texto)` devuelve los dos comandos de Global Constraints.
- [ ] **Step 2:** FAIL. **Step 3:** Actualizar la sección "Frontend (fijo del harness)" (Vitest para componentes/lógica; Playwright con suite simulada en `e2e/` en cada tarea y suite completa en `e2e-stack/` contra `E2E_BASE_URL` al cerrar la feature; `test("NNN/RF-NN: …")`), la estrategia de pruebas y el bloque `harness-config`. **Step 4:** PASS.
- [ ] **Step 5:** `git commit -am "feat(harness): plantilla de constitución con Playwright"`

### Task 3: Suite completa al cerrar la feature (`aceptar.py`)

**Files:**
- Modify: `.claude/scripts/harness_lib.py` (helper), `.claude/scripts/completar-tarea.py` (usarlo), `.claude/scripts/aceptar.py`
- Test: `tests/harness/test_cierre_e2e.py`

**Interfaces:**
- Produces: `ejecutar_comando(comando: str, raiz: Path, timeout: int) -> tuple[bool, str]` — `(True, "")` si exit 0; `(False, motivo + cola de 40 líneas)` si exit≠0 o timeout (motivo contiene `"tiempo"` en timeout). `completar-tarea.py` lo usa con `HARNESS_TEST_TIMEOUT`; `aceptar.py` con `HARNESS_E2E_TIMEOUT` (1800).
- `aceptar.py`: si tras registrar la HU todas quedarían aceptadas, ejecuta `comando_e2e_stack` **antes** de guardar; si falta la clave o falla → exit 1, no guarda, mensaje con `"docker compose up"` (fallo) o `"comando_e2e_stack"` (falta).

- [ ] **Step 1: Tests que fallan** (feature con 2 HU salvo indicación; ejecutables vía `comando_e2e_stack` = `python3 -c "import sys; sys.exit(N)"` o `"sleep 5"`):
  - `test_hu_no_final_no_ejecuta_suite`: e2e = exit 1; aceptar HU-01 → exit 0, HU-01 aceptada.
  - `test_hu_final_verde_cierra`: e2e = exit 0; aceptar HU-01 y HU-02 → `fase == "cerrada"`.
  - `test_hu_final_rojo_no_cierra` → exit 1, `"docker compose up"` en la salida, HU-02 no aceptada. *(Review Focus 1)*
  - `test_hu_unica_ejecuta_suite`: feature de 1 HU con e2e = exit 1 → rechazada. *(Review Focus 2)*
  - `test_config_sin_e2e_stack` (borrar la clave del config) → rechazada, `"comando_e2e_stack"` en la salida. *(Review Focus 3)*
  - `test_timeout_e2e` (`HARNESS_E2E_TIMEOUT=1`, `sleep 5`) → rechazada, `"tiempo"` en la salida.
- [ ] **Step 2:** FAIL. **Step 3:** Implementar helper, refactorizar `completar-tarea.py` (sus tests existentes siguen verdes) y `aceptar.py`. **Step 4:** `python3 -m pytest tests/harness -q` → PASS.
- [ ] **Step 5:** `git commit -am "feat(harness): la suite completa de Playwright bloquea el cierre de la feature"`

### Task 4: Excepción del gate para Ralph Loop

**Files:**
- Modify: `.claude/scripts/harness_lib.py` (`_es_protegido`, rutas libres de `evaluar_escritura`), `.gitignore`
- Test: `tests/harness/test_ralph.py`

**Interfaces:**
- Produces: constante `ESTADO_RALPH = ".claude/ralph-loop.local.md"`; `evaluar_escritura` devuelve `None` para esa ruta exacta (comparación `casefold`) sin depender de la constitución.

- [ ] **Step 1: Tests que fallan:**
  - `test_write_estado_ralph` (gate-spec, constitución pendiente) → exit 0.
  - `test_bash_rm_estado_ralph`: `rm .claude/ralph-loop.local.md` y `rm -f ./.claude/ralph-loop.local.md` → exit 0. *(Review Focus 4)*
  - `test_resto_de_claude_sigue_protegido`: `.claude/otro.local.md`, `.claude/ralph-loop.local.md`, `.claude/ralph-loop.local.md.bak`, `.claude/settings.json` → exit 2. *(Review Focus 5)*
  - `test_gitignore_estado_ralph`: `.gitignore` del repo contiene `.claude/ralph-loop.local.md`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementar. **Step 4:** PASS.
- [ ] **Step 5:** `git commit -am "feat(harness): el gate permite el estado de Ralph Loop"`

### Task 5: Comandos y documentación

**Files:**
- Modify: `.claude/commands/planificar.md`, `.claude/commands/implementar.md`, `.claude/commands/aceptar.md`, `CLAUDE.md`, `README.md`, `docs/superpowers/specs/2026-11-14-preset-react-design.md` (nota de actualización)
- Test: `tests/harness/test_comandos.py`

- [ ] **Step 1: Tests que fallan** en `test_comandos.py`:
  - `test_planificar_marca_ui`: `planificar.md` contiene `"UI: sí"` y `"Playwright"`.
  - `test_implementar_ralph`: `implementar.md` contiene `"ralph-loop:ralph-loop"`, `"--completion-promise"`, `"--max-iterations 8"`, `"T-NN VERDE"`, `"No modifiques las pruebas Playwright"` y `"apps/web/e2e/"`.
  - `test_aceptar_menciona_suite_completa`: `aceptar.md` contiene `"docker compose up"`.
  - `test_claude_md_y_readme_playwright`: `CLAUDE.md` (≤ 80 líneas) contiene `"Playwright"` y `"Ralph Loop"`; `README.md` contiene `"comando_e2e_stack"` y `"Ralph Loop"`; `analizar.md` contiene `"react-ts-vite-vitest-playwright"`.
- [ ] **Step 2:** FAIL. **Step 3:** Editar los textos según la spec (flujo Ralph de 4 pasos en `/implementar`; `/aceptar` avisa que la última HU necesita el sistema levantado). **Step 4:** PASS.
- [ ] **Step 5:** `git commit -am "docs(harness): Playwright y Ralph Loop en comandos y documentación"`

### Task 6: Verificación con hooks y entrega

- [ ] **Step 1:** Subir la rama, traerla al proyecto y ejecutar `python3 -m pytest tests/harness -q` con los hooks activos → todo PASS.
- [ ] **Step 2:** Comprobar el gate en vivo: `Write` de `apps/web/x.ts` → bloqueado.
- [ ] **Step 3:** Revisión final de la rama y Pull Request contra `main`.
