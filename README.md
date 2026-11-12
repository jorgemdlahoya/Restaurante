# Harness SDD + Superpowers — Sistema de restaurante

Configuración de proyecto para **Claude Code** que obliga a desarrollar con
*Spec-Driven Development*: cada requerimiento se analiza con
`superpowers:brainstorming`, se especifica, se planifica y se aprueba **antes** de
escribir código.

## Requisitos

- Claude Code
- Python 3.10+ (los scripts sólo usan la biblioteca estándar)
- Plugin **superpowers**. Al abrir el proyecto, Claude Code ofrece instalarlo desde
  `.claude/settings.json`. Si no, instálalo a mano:
  ```
  /plugin marketplace add obra/superpowers-marketplace
  /plugin install superpowers@superpowers-marketplace
  ```

## Flujo

| Paso | Comando | Quién | Resultado |
|---|---|---|---|
| 1 | `/analizar "constitución del proyecto"` | Claude + tú | `specs/000-constitucion/` (stack, arquitectura) |
| 2 | `/aprobar 000` | tú | genera `harness.config.json` |
| 3 | `/analizar "Los meseros deben poder dividir la cuenta"` | Claude + tú | `requerimiento.md` + `diseno.md` |
| 4 | `/aprobar 001` | tú | requerimiento y diseño aprobados |
| 5 | `/planificar` | Claude | `tareas.md` |
| 6 | `/aprobar 001` | tú | plan aprobado: el código se desbloquea |
| 7 | `/implementar` | Claude | TDD por tarea, `completar-tarea.py` con tests en verde |
| 8 | `/aceptar HU-01` | tú | historia aceptada; con todas, la feature se cierra |
| — | `/estado [NNN\|todas]` | cualquiera | matriz HU → RF → T → tests y huecos |

## Cómo se hace cumplir

- **`gate-spec.py`** (Write/Edit): el código sólo se escribe si la feature activa tiene los
  tres documentos aprobados. `.claude/`, `CLAUDE.md`, la config y los registros quedan
  protegidos.
- **`gate-bash.py`** (Bash): aplica las mismas reglas a redirecciones y comandos de escritura,
  e impide que el agente ejecute `aprobar.py` o `aceptar.py`.
- **Aprobación por hash**: si un documento cambia después de aprobarse, vuelve a borrador.
- **`completar-tarea.py`**: sólo marca una tarea si existen tests `NNN/RF-NN` para sus RF y el
  `comando_test` pasa.

## Limitaciones

- El análisis de Bash es heurístico: un comando ofuscado (p. ej. Python inline que escribe
  archivos) puede eludirlo. `CLAUDE.md` lo prohíbe explícitamente.
- **Verificación pendiente:** la documentación de Claude Code no aclara si los comandos `!`
  de `/aprobar` y `/aceptar` pasan por los hooks PreToolUse. Pruébalo una vez: crea la
  constitución con `/analizar` y ejecuta `/aprobar 000`. Si ves *"Sólo el usuario puede
  aprobar o aceptar"*, el gate de Bash bloqueó el comando: avísame para cambiar el
  mecanismo de aprobación a un hook `UserPromptSubmit`.
- `MultiEdit` figura en el matcher por compatibilidad con versiones antiguas; si la
  herramienta ya no existe, la entrada simplemente no se activa.
- Para cambiar `.claude/`, `CLAUDE.md` o `harness.config.json`, edítalos tú a mano.

## Pruebas del harness

```bash
pip install -r requirements-dev.txt
pytest tests/harness
```
