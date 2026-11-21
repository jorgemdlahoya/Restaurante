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
   - el código cumple `specs/000-constitucion/constitucion.md`, y la revisión de código se
     hace contra los requisitos **y** contra esa constitución (Art. 4.3);
   - no puede editar `specs/` ni `.claude/`.
4. **Tareas con `UI: sí` (flujo visible): fase verde con Ralph Loop.** Estas tareas las ejecuta
   el agente principal **en línea, sin subagente** (el bucle de Ralph Loop sólo funciona en la
   sesión principal).
   1. Escribe la prueba Playwright de la suite simulada en `apps/web/e2e/`
      (`test("NNN/RF-NN: …")`, API simulada con `page.route`), comprueba que falla y haz commit
      de esa prueba sola (será la referencia del paso 2).
   2. Invoca el skill `ralph-loop:ralph-loop` con `--completion-promise "T-NN VERDE"` y
      `--max-iterations 8`. Pásale la instrucción siguiente **entre comillas simples**,
      sustituyendo `NNN-slug`, `T-NN` y `COMMIT` (el commit del paso 1):

      <!-- INICIO-PROMPT-RALPH -->
      'Implementa T-NN de specs/NNN-slug/tareas.md. No modifiques las pruebas Playwright de
      apps/web/e2e/ escritas para T-NN. Cuando las pruebas pasen, comprueba que git diff COMMIT
      -- apps/web/e2e/ no muestra cambios y ejecuta python3 .claude/scripts/completar-tarea.py
      T-NN. Sólo si el diff está vacío y completar-tarea termina con código 0, escribe la etiqueta
      promise con el texto T-NN VERDE.'
      <!-- FIN-PROMPT-RALPH -->
   3. Si se agotan las 8 iteraciones, la tarea **no** está hecha: aplica
      `superpowers:systematic-debugging` e informa al usuario.
   4. Al emitir la promesa, el bucle deja terminar el turno: dile al usuario que T-NN quedó
      hecha y que vuelva a ejecutar `/implementar` para seguir con las tareas pendientes.

   Las tareas sin `UI: sí` siguen el TDD normal con subagentes (paso 3), sin Ralph Loop.
5. Al terminar cada tarea, ejecuta:
   ```bash
   python3 .claude/scripts/completar-tarea.py T-NN
   ```
   Si falla, **no** des la tarea por hecha: usa `superpowers:systematic-debugging`, corrige y
   vuelve a ejecutarlo. Nunca edites `progreso.json`.
6. Cuando `completar-tarea.py` anuncie HU implementadas, pide al usuario que las pruebe y
   ejecute `/aceptar HU-NN`. Avísale de que la **última** aceptación ejecuta la suite completa
   de Playwright (`apps/web/e2e-stack/`), así que antes debe levantar el sistema con
   `docker compose up`.
7. Cuando todas las HU estén aceptadas, invoca `superpowers:finishing-a-development-branch`.
