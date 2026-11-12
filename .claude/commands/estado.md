---
description: Muestra el avance SDD (HU → RF → tareas → tests) y los huecos.
argument-hint: "[NNN|todas]"
allowed-tools: Bash(python3 .claude/scripts/estado.py:*)
---

!`python3 .claude/scripts/estado.py $ARGUMENTS`

Resume el estado anterior para el usuario: fase, HU por estado y huecos con la acción que
corresponde a cada uno (`/aprobar`, `/planificar`, `/implementar` o `/aceptar`).
