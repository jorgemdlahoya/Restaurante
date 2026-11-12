---
description: Genera tareas.md (plan TDD) a partir de requerimiento y diseño aprobados.
argument-hint: "[NNN]"
---

# /planificar — plan de tareas (SDD)

1. Ejecuta `python3 .claude/scripts/estado.py $ARGUMENTS`. Si `requerimiento.md` o
   `diseno.md` aparecen sin aprobar o con cambios, **detente** y pide al usuario `/aprobar`.
2. Lee `requerimiento.md` y `diseno.md` de la feature, y `specs/000-constitucion/diseno.md`.
3. Invoca `superpowers:writing-plans` con estas preferencias del usuario:
   - **Ubicación:** escribe el plan en `tareas.md` de la feature (no en `docs/superpowers/plans/`),
     reemplazando los `{{...}}` de la plantilla.
   - Cada tarea con el formato `### T-NN [RF-NN, ...]: título`, la lista de archivos y los
     pasos TDD (test rojo → implementación mínima → test verde → commit).
   - Los tests se nombran con el RF calificado: `"NNN/RF-NN: ..."` (p. ej. `"003/RF-02: ..."`).
   - Tareas pequeñas, cada una con un entregable verificable.
   - **Omite el paso de elegir método de ejecución**: eso lo hace `/implementar`.
4. Autorrevisión: todo `RF` tiene al menos una tarea y toda tarea referencia RF existentes.
5. Termina con: "Plan listo en `tareas.md`. Revísalo y ejecuta `/aprobar NNN`; luego `/implementar`."
