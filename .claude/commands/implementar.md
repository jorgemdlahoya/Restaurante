---
description: Implementa las tareas aprobadas con TDD, marcando cada una sólo con tests en verde.
argument-hint: "[T-NN]"
---

# /implementar — implementación (SDD)

1. Ejecuta `python3 .claude/scripts/estado.py`. Si algún documento (`requerimiento.md`,
   `diseno.md`, `tareas.md`) no está aprobado, **detente** y pide `/aprobar`. El gate
   bloqueará cualquier escritura de código mientras tanto.
2. Tareas a ejecutar: `$ARGUMENTS` si se indica una; si no, todas las pendientes de
   `tareas.md`, en orden.
3. Invoca `superpowers:subagent-driven-development` sobre `tareas.md` (un subagente por tarea,
   con `superpowers:test-driven-development` y revisión con `superpowers:requesting-code-review`
   entre tareas), con estas preferencias del usuario:
   - **No crees un git worktree**: trabaja en una rama del propio proyecto
     (`git checkout -b feat/NNN-slug`). El gate SDD sólo reconoce las rutas del proyecto.
   - Todos los planes se llaman `tareas.md`: si `.superpowers/sdd/tareas/progress.md` existe y
     su primera línea no nombra la ruta de **esta** feature, borra `.superpowers/sdd/tareas/`
     antes de empezar (es un espacio de trabajo temporal, ignorado por git).

   Indica a cada subagente:
   - los tests llevan el RF calificado en el nombre: `NNN/RF-NN: ...`;
   - no puede editar `specs/` ni `.claude/`.
4. Al terminar cada tarea, ejecuta:
   ```bash
   python3 .claude/scripts/completar-tarea.py T-NN
   ```
   Si falla, **no** des la tarea por hecha: usa `superpowers:systematic-debugging`, corrige y
   vuelve a ejecutarlo. Nunca edites `progreso.json`.
5. Cuando `completar-tarea.py` anuncie HU implementadas, pide al usuario que las pruebe y
   ejecute `/aceptar HU-NN`.
6. Cuando todas las HU estén aceptadas, invoca `superpowers:finishing-a-development-branch`.
