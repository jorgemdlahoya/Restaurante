# Preset React — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fijar en el harness el frontend React + TypeScript + Vite + Vitest y hacer que `/aprobar 000` rechace constituciones que no lo declaren.

**Architecture:** Una constante `PRESET_FRONTEND` en `harness_lib.py` y una validación extra en `extraer_config`. Plantilla de `diseno.md` propia para la constitución, que `nueva-feature.py` usa al crear `000`. Ajustes de texto en `/analizar`, `CLAUDE.md`, `README.md` y la spec original.

**Tech Stack:** Python 3 stdlib, pytest, Markdown.

**Spec:** `docs/superpowers/specs/2026-11-14-preset-react-design.md` (y la spec base `docs/superpowers/specs/2026-11-12-harness-sdd-design.md`).

## Global Constraints

- Antes de la Task 1 el usuario añade `"disableAllHooks": true` en `.claude/settings.local.json`; la verificación final (Task 4) se hace con ese archivo eliminado y los hooks activos.
- Preset: `{"id": "react-ts-vite-vitest", "ruta": "apps/web/", "test": "vitest"}`.
- `harness-config` exige además: `frontend == "react-ts-vite-vitest"`, `apps/web/` en `rutas_codigo`, `comando_test` contiene `vitest`.
- Comando de tests base en la plantilla: `npm --prefix apps/web exec vitest run`.
- Textos en español; scripts sólo stdlib.

## Review Focus

1. `rutas_codigo` con `"apps/web"` o `"./apps/web/"` (sin barra final o con `./`) → se acepta igual que `"apps/web/"`. (Task 1)
2. `comando_test` = `"npm test"` (vitest oculto en package.json) → se rechaza con un mensaje que pide nombrar `vitest` explícitamente. (Task 1)
3. Un proyecto con constitución aprobada antes del preset (config sin `frontend`) → el gate sigue funcionando; sólo una re-aprobación de 000 exige el preset. (Task 1)
4. Copia del harness sin `specs/_plantillas/constitucion/` → `nueva-feature.py` usa la plantilla genérica en vez de fallar. (Task 2)
5. Plantilla de constitución recién creada → `/aprobar 000` la rechaza por placeholders, no la aprueba vacía. (Task 2)

---

### Task 1: Validación del preset en `extraer_config`

**Files:**
- Modify: `.claude/scripts/harness_lib.py` (constantes y `extraer_config`)
- Modify: `tests/harness/conftest.py` (`CONFIG_BLOQUE`, `constitucion_aprobada`)
- Modify: `tests/harness/test_e2e.py` (`CONFIG` y ejecución)
- Test: `tests/harness/test_preset.py`

**Interfaces:**
- Produces: `PRESET_FRONTEND: dict` = `{"id": "react-ts-vite-vitest", "ruta": "apps/web/", "test": "vitest"}`; `extraer_config(texto) -> dict` añade las tres comprobaciones; mensajes de error contienen `"frontend"`, `"apps/web/"` o `"vitest"` respectivamente y la frase `"fijo del harness"`.
- `CONFIG_BLOQUE` (conftest) pasa a: `{"stack": "react + python", "frontend": "react-ts-vite-vitest", "rutas_codigo": ["apps/web/", "apps/api/"], "rutas_libres": ["README.md"], "comando_test": "npm --prefix apps/web exec vitest run"}`. Las aserciones que esperan `["apps/"]` pasan a `["apps/web/", "apps/api/"]`.

- [ ] **Step 1: Tests que fallan** en `test_preset.py` (helper `bloque(**cambios)` que serializa un dict válido base a un bloque ```` ```harness-config ````):
  - `test_bloque_valido` → `extraer_config(bloque())["frontend"] == "react-ts-vite-vitest"`.
  - `test_falta_frontend`, `test_otro_frontend` (`"vue-vite"`), `test_sin_ruta_web` (`rutas_codigo=["apps/"]`), `test_sin_vitest` (`comando_test="npm test"`) → `HarnessError`, y el mensaje contiene respectivamente `"frontend"`, `"frontend"`, `"apps/web/"`, `"vitest"` y siempre `"fijo del harness"`. *(Review Focus 2)*
  - `test_ruta_web_variantes`: `["apps/web"]` y `["./apps/web/"]` → válido. *(Review Focus 1)*
  - `test_config_antigua_sigue_valiendo_en_gate`: config escrita sin `frontend` + feature aprobada → `evaluar_escritura(raiz, "apps/a.ts") is None`. *(Review Focus 3)*
  - `test_aprobar_000_rechaza_otro_frontend` (subprocess `aprobar.py 000`) → exit 1, stdout contiene `"frontend"`.
- [ ] **Step 2:** `python3 -m pytest tests/harness/test_preset.py -q` → FAIL.
- [ ] **Step 3:** Implementar. Normalizar rutas para la comprobación con `r.strip().removeprefix("./").rstrip("/") + "/"`.
- [ ] **Step 4:** Actualizar `CONFIG_BLOQUE`, las aserciones de `rutas_codigo` en `test_lib_spec.py`/`test_aprobar.py`, y `test_e2e.py`: `rutas_codigo: ["apps/web/"]`, `comando_test: "vitest run"`, tests en `apps/web/test_x.py`, y un ejecutable falso `vitest` (script sh en un dir temporal añadido al `PATH` vía `env_extra`) que ejecuta `python3 -m unittest discover -s apps/web -q`.
- [ ] **Step 5:** `python3 -m pytest tests/harness -q` → todo PASS.
- [ ] **Step 6:** `git add -A && git commit -m "feat(harness): validación del preset React"`

### Task 2: Plantilla de la constitución

**Files:**
- Create: `specs/_plantillas/constitucion/diseno.md`
- Modify: `.claude/scripts/nueva-feature.py` (`crear`)
- Test: `tests/harness/test_preset.py`

**Interfaces:**
- Consumes: `PRESET_FRONTEND`, `validar_req_diseno`.
- Produces: `crear(raiz, nombre, titulo)` usa `_plantillas/constitucion/<doc>` si existe y `nombre` empieza por `000-`; si no, la genérica.
- Plantilla: front-matter igual que la genérica; secciones de la genérica más `## Frontend (fijo del harness)` con React 18+ y TypeScript, Vite, Vitest + Testing Library + jsdom, `apps/web/`, tests `it("NNN/RF-NN: …")`; placeholders `{{backend}}`, `{{arquitectura}}`, `{{datos}}`, `{{despliegue}}`, `{{convenciones}}`; bloque `harness-config` sin comentar con `"frontend": "react-ts-vite-vitest"`, `"rutas_codigo": ["apps/web/"]`, `"rutas_libres": ["package.json", "README.md"]`, `"comando_test": "npm --prefix apps/web exec vitest run"`, `"stack": "React + TypeScript + Vite + Vitest; backend: {{backend_corto}}"`.

- [ ] **Step 1: Tests que fallan:**
  - `test_constitucion_usa_plantilla_react`: `nueva-feature.py` en proyecto vacío → `diseno.md` contiene `"## Frontend (fijo del harness)"` y `extraer_config(texto)["frontend"] == "react-ts-vite-vitest"`.
  - `test_feature_normal_plantilla_generica`: con constitución aprobada, `nueva-feature.py "pedidos"` → `diseno.md` **no** contiene `"Frontend (fijo del harness)"`.
  - `test_sin_plantilla_constitucion_usa_generica`: borrar `specs/_plantillas/constitucion/` del proyecto → exit 0 y `diseno.md` creado. *(Review Focus 4)*
  - `test_plantilla_constitucion_no_aprobable_vacia`: tras crearla, `aprobar.py 000` → exit 1 con `"{{"` en la salida. *(Review Focus 5)*
- [ ] **Step 2:** FAIL. **Step 3:** Crear plantilla y ajustar `crear`. **Step 4:** `python3 -m pytest tests/harness -q` → PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): plantilla de constitución con preset React"`

### Task 3: `/analizar`, `CLAUDE.md`, `README.md` y spec base

**Files:**
- Modify: `.claude/commands/analizar.md` (paso 1, viñeta de la constitución)
- Modify: `CLAUDE.md`, `README.md`, `docs/superpowers/specs/2026-11-12-harness-sdd-design.md` (CE-5, tabla de decisiones, §8)
- Test: `tests/harness/test_comandos.py`

- [ ] **Step 1: Tests que fallan** en `test_comandos.py`:
  - `test_analizar_preset`: `analizar.md` contiene `"react-ts-vite-vitest"` y `"no lo preguntes"`.
  - `test_claude_md_preset`: `CLAUDE.md` contiene `"## Stack fijo del frontend"` y `"apps/web/"`, y sigue con ≤ 80 líneas.
  - `test_readme_preset`: `README.md` contiene `"PRESET_FRONTEND"` (sección "Cambiar el preset").
- [ ] **Step 2:** FAIL. **Step 3:** Editar los textos (viñeta de `/analizar`: el frontend es React + TS + Vite + Vitest en `apps/web/`, fijo del harness — no lo preguntes; el brainstorming de la constitución cubre backend, arquitectura, datos, despliegue y convenciones). **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "docs(harness): preset React en comandos y documentación"`

### Task 4: Verificación con hooks activos

- [ ] **Step 1:** Pedir al usuario que borre `.claude/settings.local.json` (o quite `disableAllHooks`).
- [ ] **Step 2:** `python3 -m pytest tests/harness -q` → todo PASS.
- [ ] **Step 3:** Comprobar el gate en vivo: intentar `Write` de `apps/web/x.ts` → bloqueado ("constitución (000) no está aprobada").
