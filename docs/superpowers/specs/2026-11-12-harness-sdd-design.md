---
titulo: Harness SDD + Superpowers para Claude Code
fecha: 2026-11-12
estado: borrador
---

# Harness SDD + Superpowers para Claude Code

## 1. Propósito

Configurar un proyecto de Claude Code que obligue a desarrollar el sistema del
restaurante con **Spec-Driven Development (SDD)**: ningún código se escribe hasta
que el requerimiento, su diseño y su plan de tareas hayan sido analizados con
`superpowers:brainstorming` / `superpowers:writing-plans` y **aprobados por el
usuario**.

### Criterios de éxito

- CE-1: `/analizar` es el comando base; siempre ejecuta brainstorming y produce
  `requerimiento.md` + `diseno.md` antes de cualquier código.
- CE-2: El agente no puede escribir en directorios de código si la feature activa
  no tiene los tres documentos aprobados (bloqueo duro por hooks).
- CE-3: El agente no puede auto-aprobarse, marcar tareas sin tests en verde ni
  aceptar historias de usuario.
- CE-4: En todo momento `/estado` muestra qué HU, RF y tareas están pendientes,
  en curso, implementadas o aceptadas.
- CE-5: El harness es agnóstico al stack; el stack se decide en la feature
  `000-constitucion`.

### Decisiones tomadas (con el usuario)

| Tema | Decisión |
|---|---|
| Forma | Configuración de proyecto para Claude Code (sin programa propio) |
| Artefactos SDD | 3 documentos por feature: requerimiento, diseño, tareas |
| Rigidez | Bloqueo duro con hooks |
| Stack | Agnóstico; se define en `000-constitucion` → `harness.config.json` |
| Dominio | Sin dominio precargado; el contexto viene de cada requerimiento |
| Integración | Comandos que orquestan los skills de Superpowers (no se copian) |
| Aprobación | Por hash SHA-256, sólo vía comandos que ejecuta el usuario |
| Avance | `progreso.json` fuera del hash, escrito sólo por scripts |
| Aceptación | Manual por HU (`/aceptar`) |

### Fuera de alcance

- El sistema del restaurante en sí (lo producirá el harness).
- Orquestación por CI / Agent SDK.
- Seguridad frente a un agente adversario: el gate de Bash es heurístico (ver §6.3).

## 2. Estructura de archivos

```
restaurante/
├── CLAUDE.md
├── harness.config.json
├── .claude/
│   ├── settings.json
│   ├── commands/
│   │   ├── analizar.md
│   │   ├── aprobar.md
│   │   ├── planificar.md
│   │   ├── implementar.md
│   │   ├── aceptar.md
│   │   └── estado.md
│   ├── hooks/
│   │   ├── verificar-superpowers.sh
│   │   ├── gate-spec.py
│   │   └── gate-bash.py
│   └── scripts/
│       ├── harness_lib.py        # lógica compartida (config, hashes, estado)
│       ├── nueva-feature.py
│       ├── aprobar.py
│       ├── completar-tarea.py
│       ├── aceptar.py
│       ├── estado.py
│       └── generar-config.py
├── specs/
│   ├── .activa                   # p.ej. "003-dividir-cuenta"
│   ├── _plantillas/
│   │   ├── requerimiento.md
│   │   ├── diseno.md
│   │   └── tareas.md
│   └── NNN-<slug>/
│       ├── requerimiento.md
│       ├── diseno.md
│       ├── tareas.md
│       ├── .aprobaciones.json
│       └── progreso.json
├── tests/harness/                # pytest de hooks y scripts
└── docs/
```

Todos los scripts en Python 3 sin dependencias externas (stdlib), para que el
harness funcione con cualquier stack del producto.

## 3. Artefactos SDD y trazabilidad

### 3.1 IDs

| Prefijo | Significado | Dónde se define |
|---|---|---|
| `HU-NN` | Historia de usuario | `requerimiento.md` |
| `RF-NN` | Requisito funcional / criterio de aceptación (formato EARS) | `requerimiento.md`, dentro de una HU |
| `RNF-NN` | Requisito no funcional | `requerimiento.md` |
| `T-NN` | Tarea de implementación | `tareas.md` |

Cadena: `HU → RF → T → test`. Los IDs son únicos dentro de la feature; fuera de
ella se referencian como `003/RF-02`.

### 3.2 `requerimiento.md`

Front-matter `feature`, `estado` (informativo). Secciones: Contexto y propósito,
Actores, Historias de usuario (cada una con sus `RF` en EARS:
"Cuando <evento>, el sistema deberá <respuesta>"), Requisitos no funcionales,
Fuera de alcance, Preguntas abiertas (debe estar vacía para aprobar).

Formato parseable de una HU:

```markdown
### HU-01: Dividir cuenta
Como mesero quiero dividir la cuenta de una mesa para cobrar a cada comensal.

- **RF-01**: Cuando el mesero elige "dividir en N", el sistema deberá generar N subcuentas de igual importe.
- **RF-02**: Si N es menor que 2, el sistema deberá rechazar la división.
```

### 3.3 `diseno.md`

Secciones: Enfoque elegido y alternativas descartadas, Arquitectura,
Componentes (cada uno con `Cubre: RF-01, RF-02`), Datos, Manejo de errores,
Estrategia de pruebas. Para la constitución, además, el bloque de configuración
de §8.

### 3.4 `tareas.md`

Generado por `writing-plans`. Formato parseable por tarea:

```markdown
### T-03 [RF-02]: Validar número de comensales
Archivos: ...
Pasos TDD: test rojo → implementación → refactor
```

### 3.5 `.aprobaciones.json` (sólo scripts)

```json
{ "requerimiento.md": {"sha256": "...", "fecha": "..."},
  "diseno.md":        {"sha256": "...", "fecha": "..."},
  "tareas.md":        {"sha256": "...", "fecha": "..."} }
```

Un documento está **aprobado** sii existe su entrada y el SHA-256 actual del
archivo coincide. Cualquier edición posterior lo devuelve a *borrador*
automáticamente.

### 3.6 `progreso.json` (sólo scripts)

```json
{ "tareas": { "T-03": {"estado": "hecha", "fecha": "...", "commit": "abc123"} },
  "aceptadas": { "HU-01": {"fecha": "..."} } }
```

### 3.7 Estados calculados

- **Fase de la feature**: `analisis` → `diseno-aprobado` → `plan-aprobado` →
  `implementando` → `cerrada` (todas las HU aceptadas).
- **HU**: `pendiente` (0 tareas hechas) · `en-curso` · `implementada` (todas las
  tareas de todos sus RF hechas) · `aceptada`.

Nada de esto se escribe a mano; `estado.py` lo deriva de los archivos.

## 4. Comandos

Todos en `.claude/commands/*.md`, en español.

### 4.1 `/analizar <requerimiento>` — comando base

1. Ejecuta `nueva-feature.py "<requerimiento>"`: asigna `NNN`, crea
   `specs/NNN-<slug>/` desde plantillas, escribe `specs/.activa`. Si la
   constitución no está aprobada y no es la propia constitución (`NNN=000`), se
   detiene con mensaje. Si ya existe una feature activa no cerrada, pregunta
   antes de cambiarla.
2. Lee `CLAUDE.md`, `specs/000-constitucion/` y los requerimientos aprobados para
   detectar solapes y dependencias.
3. Invoca `superpowers:brainstorming` con estas sobrecargas:
   - Clasificación siempre **arquitectónica** (camino completo).
   - Preguntas de una en una sobre propósito, actores, reglas de negocio, casos
     borde, criterios de éxito.
   - Si el requerimiento abarca varios subsistemas independientes, propone
     dividirlo en varias features antes de seguir.
   - La spec se escribe en **dos** archivos (`requerimiento.md` y `diseno.md`)
     de la feature activa, no en `docs/superpowers/specs/`.
4. Autorrevisión: sin placeholders, sin contradicciones, preguntas abiertas
   vacías, cada `RF` cubierto por al menos un componente del diseño.
5. **Termina** pidiendo al usuario revisar y ejecutar `/aprobar NNN`. No invoca
   `writing-plans`.

### 4.2 `/aprobar [NNN]` — sólo usuario

- Front-matter `disable-model-invocation: true`.
- Ejecuta `aprobar.py` mediante `!` (antes de que intervenga el modelo).
- Aprueba los documentos de la fase actual que estén en borrador: primero
  `requerimiento.md` + `diseno.md`; si ya lo están, `tareas.md`.
- Valida antes de aprobar: IDs bien formados, "Preguntas abiertas" vacía, en
  `tareas.md` todo `RF` tiene tarea y toda tarea referencia un `RF` existente.
  Si falla, no aprueba y muestra el motivo.
- Si la feature es `000` y se aprueba su diseño, ejecuta `generar-config.py`.

### 4.3 `/planificar`

- Requiere requerimiento y diseño aprobados (si no, se detiene).
- Invoca `superpowers:writing-plans` con salida en `tareas.md`, tareas
  pequeñas en TDD, cada una con sus `RF` y archivos.
- Autorrevisión de cobertura RF↔T. Termina pidiendo `/aprobar NNN`.

### 4.4 `/implementar [T-NN]`

- Requiere los tres documentos aprobados.
- Invoca `superpowers:subagent-driven-development` (un subagente por tarea,
  `test-driven-development`, `requesting-code-review` entre tareas).
- Los tests llevan el ID del RF en el nombre (`"RF-02: ..."`).
- Cada tarea termina con `completar-tarea.py T-NN`; si falla, aplica
  `superpowers:systematic-debugging`, nunca la marca.
- Al terminar, pide `/aceptar HU-NN` por cada HU implementada. Con todas
  aceptadas, invoca `superpowers:finishing-a-development-branch`.

### 4.5 `/aceptar HU-NN` — sólo usuario

`disable-model-invocation: true`; ejecuta `aceptar.py` vía `!`. Rechaza si la HU
no está `implementada` o si algún documento perdió su aprobación.

### 4.6 `/estado [NNN|todas]`

Ejecuta `estado.py`: matriz HU → RF → T → tests de la feature activa, o resumen
de todas. Señala huecos: RF sin tarea, tarea sin test, documento con hash
alterado.

## 5. `completar-tarea.py T-NN`

1. Verifica que los tres documentos sigan aprobados.
2. Lee los `RF` de la tarea en `tareas.md`.
3. Busca en `rutas_codigo` tests cuyo texto contenga cada `RF-NN`; si falta
   alguno → falla.
4. Ejecuta `comando_test` de la config; exit ≠ 0 → falla.
5. Registra la tarea como `hecha` con fecha y `git rev-parse HEAD` (si hay git).

## 6. Hooks

Registrados en `.claude/settings.json`. Entrada: JSON de Claude Code por stdin.
Bloqueo: exit 2 con mensaje en stderr (Claude lo ve). Ante cualquier error de
lectura o parseo, **falla cerrado** (bloquea).

### 6.1 `gate-spec.py` — PreToolUse `Write|Edit|MultiEdit|NotebookEdit`

Ruta normalizada relativa a la raíz del proyecto; rutas fuera del proyecto se
bloquean.

| Ruta destino | Regla |
|---|---|
| `specs/**/.aprobaciones.json`, `specs/**/progreso.json`, `specs/.activa` | Bloqueado |
| `.claude/**`, `harness.config.json`, `CLAUDE.md` | Bloqueado (los edita el usuario a mano) |
| `specs/**`, `docs/**` | Permitido |
| `rutas_codigo` (config) | Permitido sólo si la feature activa tiene los 3 documentos aprobados |
| `rutas_libres` (config) | Permitido sólo con constitución aprobada |
| Cualquier otra | Bloqueado |

Mensaje ejemplo: *"Bloqueado: `diseno.md` de 003-dividir-cuenta no está aprobado
(cambió desde la aprobación). Pide al usuario ejecutar /aprobar 003."*

### 6.2 `gate-bash.py` — PreToolUse `Bash`

- Permite ejecutar los scripts de `.claude/scripts/` salvo `aprobar.py` y
  `aceptar.py` (bloqueados para el agente).
- Detecta escrituras por shell (`>`, `>>`, `tee`, `sed -i`, `cp`, `mv`, `rm`,
  `touch`, `git checkout --`/`restore`) y aplica a sus destinos la tabla de §6.1.
- Bloquea `sha256sum`/`hashlib` combinados con `.aprobaciones`.

### 6.3 Limitación declarada

El análisis de Bash es heurístico: un comando ofuscado (p.ej. código Python
inline que escribe archivos) puede eludirlo. Es una barrera de buena fe; las
reglas en `CLAUDE.md` refuerzan que el agente no debe intentarlo.

### 6.4 `verificar-superpowers.sh` — SessionStart

- Comprueba que el plugin `superpowers` está instalado; si no, imprime cómo
  instalarlo.
- Inyecta en el contexto la feature activa y su fase (salida de
  `estado.py --breve`).

## 7. `settings.json` y `CLAUDE.md`

- `settings.json`: declara el marketplace y habilita `superpowers`
  (`enabledPlugins`), registra los hooks y permite (`permissions.allow`) los
  scripts del harness. La sintaxis exacta del marketplace se verifica durante la
  implementación.
- `CLAUDE.md`: flujo SDD (`/analizar` → `/aprobar` → `/planificar` → `/aprobar`
  → `/implementar` → `/aceptar`), formato de IDs, convención de nombres de
  tests, prohibición de eludir los gates, idioma español.

## 8. Constitución (`000-constitucion`)

- Se crea con `/analizar "constitución del proyecto"`; mismo brainstorming,
  centrado en stack, arquitectura general, principios y convenciones.
- Mientras no esté aprobada, `harness.config.json` contiene sólo
  `{"constitucion": "pendiente"}` y no hay rutas de código.
- Su `diseno.md` debe incluir este bloque, que `generar-config.py` extrae:

````markdown
```harness-config
{ "stack": "…",
  "rutas_codigo": ["apps/", "packages/"],
  "rutas_libres": ["package.json", "README.md"],
  "comando_test": "npm test" }
```
````

- Las features de constitución no tienen tareas de código obligatorias: su fase
  termina al aprobar el diseño (no requiere `tareas.md` ni `/aceptar`).

## 9. Pruebas del harness

- **Unitarias (pytest, `tests/harness/`)** con JSON simulado de hooks:
  escritura en código sin aprobación; con hash alterado; con los tres aprobados
  (permitido); edición de `.aprobaciones.json`, `progreso.json`, `.claude/`,
  `harness.config.json`; ruta fuera del proyecto; config corrupta (falla
  cerrado); Bash con `>`, `sed -i`, `tee`, `cp`; Bash invocando `aprobar.py`;
  `completar-tarea` con tests en rojo, sin test del RF y en verde; `aprobar`
  con preguntas abiertas o RF sin tarea; `aceptar` sobre HU no implementada.
- **Extremo a extremo**: repo temporal, constitución con stack ficticio
  (`comando_test` = script de shell), una feature con 1 HU / 2 RF / 2 tareas,
  recorrida con los scripts en el orden del flujo.

## 10. Riesgos

- Cambios en el formato de entrada de hooks o de `settings.json` de Claude Code
  → se verifica contra la documentación actual al implementar.
- Que `superpowers:brainstorming` ignore la ruta de salida sobrecargada → el
  comando lo indica explícitamente y el skill permite la sobrecarga ("User
  preferences for spec location override this default").
- Parseo frágil de Markdown → formatos de §3 estrictos y validados por
  `aprobar.py`.
