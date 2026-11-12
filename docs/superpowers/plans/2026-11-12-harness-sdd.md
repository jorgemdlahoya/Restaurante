# Harness SDD + Superpowers — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir la configuración de proyecto de Claude Code que fuerza el flujo SDD (`/analizar` → `/aprobar` → `/planificar` → `/aprobar` → `/implementar` → `/aceptar`) con gates por hooks.

**Architecture:** Una librería Python (`.claude/scripts/harness_lib.py`) concentra toda la lógica (config, hashes, parseo de specs, estados, reglas de escritura). Scripts CLI delgados y hooks la importan. Los comandos `.md` orquestan los skills de Superpowers y llaman a los scripts.

**Tech Stack:** Python 3 stdlib (scripts y hooks), bash (hook SessionStart), pytest (sólo para probar el harness), Markdown (comandos, plantillas, CLAUDE.md).

**Spec:** `docs/superpowers/specs/2026-11-12-harness-sdd-design.md`

## Global Constraints

- Scripts y hooks: Python 3 **sólo stdlib**; pytest es dependencia de desarrollo, no de runtime.
- Todo texto visible (mensajes, comandos, plantillas, CLAUDE.md) en **español**.
- IDs: `HU-\d{2,}`, `RF-\d{2,}`, `RNF-\d{2,}`, `T-\d{2,}`; features `\d{3}-<slug>`.
- Tests del producto referencian RF calificado: `NNN/RF-NN` (p.ej. `003/RF-02`).
- Hooks bloquean con **exit 2** + mensaje en stderr; cualquier error interno → bloquear (falla cerrado).
- Raíz del proyecto: `$CLAUDE_PROJECT_DIR`; si no existe, el `cwd` del JSON del hook; si no, `os.getcwd()`.
- Feature `000-constitucion`: requiere sólo `requerimiento.md` + `diseno.md`; el resto, además `tareas.md`.
- Re-aprobar requerimiento + diseño borra la aprobación de `tareas.md`.
- `aprobar.py` y `aceptar.py` nunca ejecutables por el agente (gate-bash los bloquea; comandos con `disable-model-invocation: true`).

## Review Focus

1. Rutas con `..`, absolutas o con symlinks que escapan de la raíz o disfrazan un dir de código → se resuelven antes de evaluar; fuera de raíz = bloqueado. (Task 4)
2. `specs/.activa` apunta a un directorio inexistente o vacío → bloqueo con mensaje claro, no excepción. (Task 4)
3. Markdown con `\r\n` o BOM → el parseo encuentra los mismos IDs. (Task 2)
4. `comando_test` inexistente o que no termina → la tarea no se marca; timeout de 1800 s. (Task 6)
5. Editar el requerimiento con el plan ya aprobado y re-aprobar → `tareas.md` deja de estar aprobado. (Task 5)

---

### Task 1: Núcleo de la librería — raíz, config, feature activa, aprobaciones

**Files:**
- Create: `.claude/scripts/harness_lib.py`
- Create: `tests/harness/conftest.py`, `tests/harness/test_lib_core.py`
- Create: `requirements-dev.txt` (`pytest>=8`)

**Interfaces:**
- Produces:
  - `class HarnessError(Exception)` — error con mensaje en español para el usuario.
  - `raiz(cwd: str | None = None) -> Path`
  - `cargar_config(raiz: Path) -> dict` — `HarnessError` si falta o JSON inválido.
  - `feature_activa(raiz: Path) -> Path | None` — `None` si no hay `.activa`; `HarnessError` si apunta a dir inexistente.
  - `buscar_feature(raiz: Path, nnn: str | None) -> Path` — `None` → activa; `HarnessError` si no existe.
  - `sha256(path: Path) -> str`
  - `cargar_aprobaciones(fdir: Path) -> dict`, `guardar_aprobaciones(fdir: Path, data: dict) -> None`
  - `doc_aprobado(fdir: Path, nombre: str) -> bool` — entrada existe y hash coincide.
  - `es_constitucion(fdir: Path) -> bool` — nombre empieza por `000-`.
  - `docs_requeridos(fdir: Path) -> list[str]`
  - `docs_aprobados(fdir: Path) -> bool` — todos los requeridos aprobados.
  - `constitucion_aprobada(raiz: Path) -> bool`
  - Constantes `REQ="requerimiento.md"`, `DIS="diseno.md"`, `TAR="tareas.md"`.
- `conftest.py` fixture `proyecto(tmp_path) -> Path`: crea raíz con `harness.config.json` = `{"constitucion":"pendiente"}`, `specs/`, `monkeypatch.setenv("CLAUDE_PROJECT_DIR", ...)`, y añade `.claude/scripts` del repo a `sys.path`. Helper `crear_feature(raiz, nombre, docs: dict[str,str], aprobar: list[str]=[]) -> Path`.

- [ ] **Step 1:** `pip install -r requirements-dev.txt`; esperado: `pytest --version` imprime 8.x.
- [ ] **Step 2: Tests que fallan** en `test_lib_core.py`:
  - `test_doc_aprobado_por_hash`: aprobar `diseno.md` → `True`; reescribir con un carácter distinto → `False`.
  - `test_docs_requeridos_constitucion`: `000-constitucion` → `[REQ, DIS]`; `001-x` → `[REQ, DIS, TAR]`.
  - `test_constitucion_aprobada`: sin `000-*` → `False`; con REQ+DIS aprobados → `True`.
  - `test_activa_inexistente`: `.activa` = `"009-nada"` → `pytest.raises(HarnessError)`.
  - `test_config_corrupta`: config `"{"` → `HarnessError`.
- [ ] **Step 3:** `pytest tests/harness/test_lib_core.py -v` → FAIL (módulo inexistente).
- [ ] **Step 4:** Implementar en `harness_lib.py`. `.aprobaciones.json` con forma `{nombre: {"sha256": str, "fecha": iso8601}}`.
- [ ] **Step 5:** `pytest tests/harness/test_lib_core.py -v` → PASS.
- [ ] **Step 6:** `git add -A && git commit -m "feat(harness): núcleo de harness_lib"`

### Task 2: Parseo de specs, validaciones y estados

**Files:**
- Modify: `.claude/scripts/harness_lib.py`
- Test: `tests/harness/test_lib_spec.py`

**Interfaces:**
- Consumes: Task 1.
- Produces:
  - `leer(path: Path) -> str` — UTF-8 con `utf-8-sig`, normaliza `\r\n` → `\n`.
  - `parsear_requerimiento(texto: str) -> dict[str, list[str]]` — `{"HU-01": ["RF-01","RF-02"]}`; HU por `^### (HU-\d{2,}):`, RF por `\*\*(RF-\d{2,})\*\*` bajo la HU vigente.
  - `preguntas_abiertas(texto: str) -> str` — contenido de `## Preguntas abiertas` hasta el siguiente `## `, sin comentarios `<!-- -->`, sin líneas `- Ninguna`, strip.
  - `parsear_tareas(texto: str) -> dict[str, list[str]]` — `^### (T-\d{2,}) \[([^\]]+)\]` → RFs separados por coma.
  - `extraer_config(texto: str) -> dict` — bloque ```` ```harness-config ````; claves obligatorias `stack:str`, `rutas_codigo:list[str]` (no vacía), `rutas_libres:list[str]`, `comando_test:str`; `HarnessError` si falta/ inválido.
  - `validar_req_diseno(fdir: Path) -> list[str]` — errores: sin HU; HU sin RF; RF duplicado; preguntas abiertas no vacías; placeholder `{{` en cualquiera de los dos; RF no mencionado en `diseno.md`; en constitución, `extraer_config` falla.
  - `validar_tareas(fdir: Path) -> list[str]` — errores: tarea sin RF; RF inexistente; RF sin tarea; `{{` presente.
  - `cargar_progreso(fdir) -> dict` (`{"tareas":{}, "aceptadas":{}}` por defecto), `guardar_progreso(fdir, data) -> None`
  - `estado_hus(fdir: Path) -> dict[str, str]` — valores `pendiente|en-curso|implementada|aceptada` (spec §3.7).
  - `fase(fdir: Path) -> str` — `analisis|diseno-aprobado|plan-aprobado|implementando|cerrada`; constitución con REQ+DIS aprobados → `cerrada`.

- [ ] **Step 1: Tests que fallan:**
  - `test_parsear_requerimiento` con 2 HU / 3 RF → `{"HU-01":["RF-01","RF-02"],"HU-02":["RF-03"]}`.
  - `test_parseo_crlf_bom`: mismo texto con `﻿` y `\r\n` vía `leer` → mismo resultado. *(Review Focus 3)*
  - `test_preguntas_abiertas_ninguna`: sección con `- Ninguna` → `""`.
  - `test_validar_rf_sin_diseno`: RF-02 ausente de diseño → error contiene `"RF-02"`.
  - `test_validar_tareas_rf_sin_tarea` → error contiene `"RF-03"`.
  - `test_extraer_config_falta_clave` → `HarnessError`.
  - `test_estado_hus`: tareas T-01[RF-01] hecha, T-02[RF-02] pendiente → HU-01 `en-curso`; ambas hechas → `implementada`; con aceptación → `aceptada`.
  - `test_fase`: sin aprobar → `analisis`; REQ+DIS → `diseno-aprobado`; +TAR → `plan-aprobado`; 1 tarea hecha → `implementando`; todas las HU aceptadas → `cerrada`.
- [ ] **Step 2:** `pytest tests/harness/test_lib_spec.py -v` → FAIL.
- [ ] **Step 3:** Implementar las funciones.
- [ ] **Step 4:** `pytest tests/harness -v` → PASS.
- [ ] **Step 5:** `git commit -am "feat(harness): parseo y estados de specs"`

### Task 3: Plantillas y `nueva-feature.py`

**Files:**
- Create: `specs/_plantillas/requerimiento.md`, `diseno.md`, `tareas.md`
- Create: `.claude/scripts/nueva-feature.py`
- Test: `tests/harness/test_nueva_feature.py`

**Interfaces:**
- Consumes: Tasks 1–2.
- Produces: CLI `python3 .claude/scripts/nueva-feature.py "<texto>" [--forzar]`; imprime la ruta relativa creada. Exit 0 ok, 1 error, 3 = hay feature activa no cerrada (requiere `--forzar`).
- Plantillas: secciones exactas de spec §3.2–3.4, con placeholders `{{...}}` (p.ej. `{{proposito}}`) y front-matter `feature: {{feature}}`, `estado: borrador` (reemplaza `{{feature}}` al crear). `diseno.md` incluye, comentada, la guía del bloque `harness-config` (sólo se exige en constitución).

Reglas:
- No existe `specs/000-*` → crea `000-constitucion` (ignora el texto como slug) e imprime aviso.
- Existe 000 no aprobada → si ya es la activa, la reutiliza (exit 0, imprime ruta); si no, error.
- Si no, `NNN` = máximo existente + 1, `slug` = minúsculas, sin acentos (`unicodedata` NFKD), no alfanumérico → `-`, recortado a 40 y sin `-` finales.
- Escribe `specs/.activa` con el nombre del dir.

- [ ] **Step 1: Tests que fallan** (subprocess):
  - `test_primera_es_constitucion`: proyecto vacío → crea `specs/000-constitucion/` con 3 plantillas y `.activa`.
  - `test_bloquea_sin_constitucion`: 000 existe no aprobada y `.activa` borrada → exit 1, stderr contiene `"constitución"`.
  - `test_slug_y_numero`: con 000 aprobada y `001-x`, texto `"Dividir la cuenta en partes ¡iguales!"` → `002-dividir-la-cuenta-en-partes-iguales`.
  - `test_activa_no_cerrada`: activa en fase `analisis` → exit 3; con `--forzar` → exit 0.
- [ ] **Step 2:** Ejecutar → FAIL. **Step 3:** Implementar. **Step 4:** `pytest tests/harness -v` → PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): plantillas y nueva-feature"`

### Task 4: Reglas de escritura y `gate-spec.py`

**Files:**
- Modify: `.claude/scripts/harness_lib.py`
- Create: `.claude/hooks/gate-spec.py`
- Test: `tests/harness/test_gate_spec.py`

**Interfaces:**
- Consumes: Tasks 1–2.
- Produces: `evaluar_escritura(raiz: Path, ruta: str) -> str | None` — `None` = permitido; `str` = motivo en español que incluye la acción sugerida (`/aprobar NNN`, `/analizar`). Tabla exacta de spec §6.1, evaluada en ese orden. `ruta` se resuelve con `Path(raiz, ruta).resolve()` (absolutas y symlinks incluidos); fuera de `raiz.resolve()` → motivo. `rutas_codigo`/`rutas_libres` se comparan como prefijos de la ruta relativa (`"apps/"` cubre `apps/web/x.ts`; `"README.md"` exacto).
- Hook: lee JSON stdin; ruta = `tool_input.file_path` o `tool_input.notebook_path`; motivo → stderr + exit 2; permitido → exit 0; cualquier excepción → stderr `"Gate SDD: error interno (...) — bloqueado por seguridad"` + exit 2. Importa la lib vía `Path(__file__).resolve().parents[1] / "scripts"`.

- [ ] **Step 1: Tests que fallan** (subprocess con JSON `{"tool_name":"Write","tool_input":{"file_path":...},"cwd":...}`):
  - `test_specs_permitido`, `test_docs_permitido` → exit 0.
  - `test_protegidos`: `specs/001-x/.aprobaciones.json`, `specs/001-x/progreso.json`, `specs/.activa`, `.claude/settings.json`, `harness.config.json`, `CLAUDE.md` → exit 2.
  - `test_codigo_sin_constitucion`: `apps/a.ts` con config pendiente → exit 2.
  - `test_codigo_sin_aprobar`: constitución con `rutas_codigo:["apps/"]`, activa `001` sin TAR aprobado → exit 2, stderr contiene `"/aprobar 001"`.
  - `test_codigo_aprobado` → exit 0; luego modificar `diseno.md` → exit 2 y stderr contiene `"cambió"`.
  - `test_escape_raiz`: `../fuera.txt`, `/etc/passwd`, symlink `specs/link -> ../apps` + `specs/link/a.ts` → exit 2. *(Review Focus 1)*
  - `test_activa_rota`: `.activa`=`"009-nada"` + `apps/a.ts` → exit 2 con mensaje, sin traceback. *(Review Focus 2)*
  - `test_config_corrupta` → exit 2 (falla cerrado).
  - `test_notebook_path`: `NotebookEdit` con `notebook_path` en código sin aprobar → exit 2.
- [ ] **Step 2:** FAIL. **Step 3:** Implementar. **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): gate de escritura"`

### Task 5: `aprobar.py` y `generar-config.py`

**Files:**
- Create: `.claude/scripts/aprobar.py`, `.claude/scripts/generar-config.py`
- Test: `tests/harness/test_aprobar.py`

**Interfaces:**
- Consumes: `buscar_feature`, `validar_req_diseno`, `validar_tareas`, `extraer_config`, aprobaciones.
- Produces: `aprobar.py [NNN]` — lógica spec §4.2; imprime qué aprobó o los errores (exit 1). Al aprobar REQ+DIS elimina la entrada `tareas.md`. Si es constitución, llama a `generar-config.py`. `generar-config.py` escribe `harness.config.json` = `{"constitucion":"aprobada", **extraer_config(diseno)}` con indent 2.

- [ ] **Step 1: Tests que fallan:**
  - `test_aprueba_req_diseno` → ambos con hash; segunda ejecución aprueba `tareas.md` válido.
  - `test_rechaza_preguntas_abiertas` → exit 1, nada aprobado.
  - `test_reaprobar_invalida_tareas`: plan aprobado, editar REQ, aprobar → `doc_aprobado(TAR)` es `False`. *(Review Focus 5)*
  - `test_constitucion_genera_config` → config con `rutas_codigo` del bloque.
  - `test_nada_que_aprobar` → exit 0, mensaje `"Nada que aprobar"`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementar. **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): aprobación por hash"`

### Task 6: `completar-tarea.py` y `aceptar.py`

**Files:**
- Create: `.claude/scripts/completar-tarea.py`, `.claude/scripts/aceptar.py`
- Test: `tests/harness/test_avance.py`

**Interfaces:**
- Consumes: `docs_aprobados`, `parsear_tareas`, `cargar_config`, progreso, `estado_hus`.
- Produces:
  - `completar-tarea.py T-NN [--feature NNN]` — pasos spec §5. Búsqueda de `NNN/RF-NN` en archivos bajo `rutas_codigo` (ignora `node_modules`, `.git`, binarios no decodificables). `subprocess.run(comando_test, shell=True, cwd=raiz, timeout=1800)`; timeout o exit≠0 → exit 1 con la cola de la salida (últimas 40 líneas). Commit = `git rev-parse HEAD` o `null`.
  - `aceptar.py HU-NN [--feature NNN]` — spec §4.5.

- [ ] **Step 1: Tests que fallan** (config con `comando_test` = `python3 -c "import sys; sys.exit(0)"` o `exit 1`):
  - `test_completa_con_tests_verdes`: archivo `apps/t.test.js` contiene `"001/RF-01"` → `progreso.json` T-01 `hecha`.
  - `test_falta_test_del_rf` → exit 1, stderr contiene `"001/RF-01"`.
  - `test_tests_rojos` → exit 1, no marcada.
  - `test_comando_inexistente_o_timeout`: `comando_test` = `"comando-que-no-existe"` → exit 1; con timeout parcheable por env `HARNESS_TEST_TIMEOUT=1` y `sleep 5` → exit 1. *(Review Focus 4)*
  - `test_docs_no_aprobados` → exit 1.
  - `test_aceptar_requiere_implementada` → exit 1; tras completar todas → exit 0 y `aceptadas["HU-01"]`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementar (timeout por defecto 1800, sobrescribible con `HARNESS_TEST_TIMEOUT`). **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): completar tareas y aceptar HU"`

### Task 7: `estado.py`

**Files:**
- Create: `.claude/scripts/estado.py`
- Test: `tests/harness/test_estado.py`

**Interfaces:**
- Produces: `estado.py [NNN|todas] [--breve]`.
  - Por defecto: feature activa → cabecera `NNN-slug — fase: X`, luego por HU: `HU-01 [en-curso]`, por RF: `RF-01 → T-01 ✔, T-02 ·`; sección `Huecos:` con RF sin tarea, tarea sin test (`NNN/RF` no encontrado), documentos con hash alterado (`diseno.md: cambió desde la aprobación`).
  - `todas`: una línea por feature `NNN-slug  fase  HU aceptadas/total`.
  - `--breve`: una línea `Feature activa: NNN-slug (fase: X)` o `Sin feature activa — usa /analizar`.
  - Nunca falla con traceback: `HarnessError` → mensaje + exit 1.
- [ ] **Step 1: Tests que fallan:** `test_breve_sin_activa`, `test_matriz_y_huecos` (salida contiene `"RF-02"` bajo `Huecos:` si no tiene tarea), `test_todas`, `test_hash_alterado_se_reporta`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementar. **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): comando de estado"`

### Task 8: `gate-bash.py`

**Files:**
- Modify: `.claude/scripts/harness_lib.py`
- Create: `.claude/hooks/gate-bash.py`
- Test: `tests/harness/test_gate_bash.py`

**Interfaces:**
- Consumes: `evaluar_escritura`.
- Produces: `destinos_escritura(comando: str) -> list[str]` — divide por `;`, `&&`, `||`, `|`, salto de línea; `shlex.split` por segmento (si falla → devuelve `["<no-parseable>"]`, que bloquea); destinos: token tras `>`/`>>` (incluido `2>` salvo `/dev/null`), args no-flag de `tee`, último arg de `sed -i…`, último arg de `cp`/`mv`/`install`, todos los args no-flag de `rm`/`touch`/`mkdir`/`truncate`, paths tras `--` en `git checkout`, args de `git restore`.
- Hook: bloquea (exit 2) si el comando contiene `aprobar.py`, `aceptar.py`, `.aprobaciones` o `progreso.json`; si no, aplica `evaluar_escritura` a cada destino. Excepción → exit 2.

- [ ] **Step 1: Tests que fallan:**
  - permitidos: `pytest -q`, `ls apps`, `echo hola > specs/001-x/notas.md`, `python3 .claude/scripts/completar-tarea.py T-01`, `cmd 2>/dev/null`.
  - bloqueados (sin aprobación): `echo x > apps/a.ts`, `sed -i s/a/b/ apps/a.ts`, `cat a | tee apps/b.ts`, `cp /tmp/a apps/a.ts`, `rm apps/a.ts`, `python3 .claude/scripts/aprobar.py`, `cat specs/001-x/.aprobaciones.json`, `echo '{' > harness.config.json`.
- [ ] **Step 2:** FAIL. **Step 3:** Implementar. **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): gate de bash"`

### Task 9: `verificar-superpowers.sh` y `.claude/settings.json`

**Files:**
- Create: `.claude/hooks/verificar-superpowers.sh` (ejecutable), `.claude/settings.json`
- Test: `tests/harness/test_settings.py`

**Interfaces:**
- `verificar-superpowers.sh`: si no existe ningún `skills/brainstorming/SKILL.md` bajo `${CLAUDE_CONFIG_DIR:-$HOME/.claude}/plugins` → imprime aviso con `/plugin marketplace add obra/superpowers-marketplace` y `/plugin install superpowers@superpowers-marketplace`. Siempre imprime `python3 .claude/scripts/estado.py --breve`. Exit 0 siempre.
- `settings.json`:
  - `extraKnownMarketplaces.superpowers-marketplace.source = {"source":"github","repo":"obra/superpowers-marketplace"}`; `enabledPlugins["superpowers@superpowers-marketplace"] = true`.
  - `hooks.SessionStart` → `bash "$CLAUDE_PROJECT_DIR/.claude/hooks/verificar-superpowers.sh"`; `hooks.PreToolUse`: matcher `Write|Edit|MultiEdit|NotebookEdit` → `python3 "$CLAUDE_PROJECT_DIR/.claude/hooks/gate-spec.py"`; matcher `Bash` → `gate-bash.py`.
  - `permissions.allow`: `Bash(python3 .claude/scripts/nueva-feature.py:*)`, `…/completar-tarea.py:*`, `…/estado.py:*`.

- [ ] **Step 1: Test que falla** `test_settings_hooks`: carga el JSON y comprueba los dos matchers, que cada `command` apunta a un archivo existente, y que el plugin está habilitado. `test_sessionstart_sin_plugin`: `CLAUDE_CONFIG_DIR` vacío → stdout contiene `"superpowers"` y exit 0.
- [ ] **Step 2:** FAIL. **Step 3:** Crear archivos; verificar el formato de `settings.json` contra la documentación actual de Claude Code (hooks y `extraKnownMarketplaces`) y ajustar si difiere. **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): settings y hook de inicio"`

### Task 10: Comandos, `CLAUDE.md` y `README.md`

**Files:**
- Create: `.claude/commands/analizar.md`, `aprobar.md`, `planificar.md`, `implementar.md`, `aceptar.md`, `estado.md`
- Create: `CLAUDE.md`, `README.md`
- Test: `tests/harness/test_comandos.py`

**Interfaces:**
- Frontmatter de cada comando: `description`, `argument-hint`. `aprobar.md` y `aceptar.md` además `disable-model-invocation: true` y `allowed-tools: Bash(python3 .claude/scripts/aprobar.py:*)` (resp. `aceptar.py`), con cuerpo `` !`python3 .claude/scripts/aprobar.py $ARGUMENTS` `` seguido de la instrucción de sólo informar el resultado.
- `analizar.md`: cuerpo con los pasos 1–5 de spec §4.1 literalmente, incluida la sobrecarga de rutas de salida y "NO invoques writing-plans". Paso 1 indica: si exit 3, pregunta al usuario y re-ejecuta con `--forzar`.
- `planificar.md`, `implementar.md`: spec §4.3 y §4.4 (incluye convención `NNN/RF-NN` en nombres de tests y `completar-tarea.py` por tarea).
- `estado.md`: `` !`python3 .claude/scripts/estado.py $ARGUMENTS` `` y resumir.
- `CLAUDE.md`: contenido de spec §7 en ≤ 80 líneas.
- `README.md`: instalación (abrir el proyecto en Claude Code, aceptar el marketplace), flujo con ejemplo, cómo ejecutar `pytest tests/harness`.

- [ ] **Step 1: Test que falla** `test_comandos`: existen los 6; `aprobar.md`/`aceptar.md` tienen `disable-model-invocation: true`; `analizar.md` menciona `superpowers:brainstorming`, `requerimiento.md`, `diseno.md`; `planificar.md` menciona `superpowers:writing-plans`; `implementar.md` menciona `subagent-driven-development` y `completar-tarea.py`.
- [ ] **Step 2:** FAIL. **Step 3:** Escribir los archivos. **Step 4:** PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat(harness): comandos y documentación"`

### Task 11: Prueba extremo a extremo y verificación en Claude Code real

**Files:**
- Test: `tests/harness/test_e2e.py`

- [ ] **Step 1: Test** `test_flujo_completo` en repo temporal (copia de `.claude/` + `specs/_plantillas/`, `git init`): nueva-feature (000) → rellenar REQ/DIS con bloque `harness-config` (`rutas_codigo:["app/"]`, `comando_test:"python3 -m unittest discover -s app"`) → aprobar → gate permite `app/x.py` sólo tras aprobar 001 completo → nueva-feature 001 con 1 HU / 2 RF / 2 T → aprobar ×2 → escribir `app/test_x.py` con `001/RF-01` y `001/RF-02` → completar T-01, T-02 → aceptar HU-01 → `estado.py` muestra `fase: cerrada`.
- [ ] **Step 2:** `pytest tests/harness -v` → todo PASS.
- [ ] **Step 3: Verificación manual con la CLI** (`claude -p` en una copia temporal): (a) pedir escribir `app/x.py` sin aprobación → la respuesta refleja el bloqueo; (b) comprobar si el `!` de `/aprobar` pasa por `gate-bash`. Si lo bloquea, cambiar la detección de `aprobar.py`/`aceptar.py` para permitir la invocación exacta usada por el comando y documentar el resultado en `README.md` (sección "Limitaciones").
- [ ] **Step 4:** `git add -A && git commit -m "test(harness): prueba extremo a extremo"`
