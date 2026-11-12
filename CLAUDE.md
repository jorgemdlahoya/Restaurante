# Sistema de restaurante — reglas del harness SDD

Este proyecto se desarrolla con **Spec-Driven Development** y los skills de **Superpowers**.
Nunca se escribe código sin una spec aprobada por el usuario. Los hooks lo hacen cumplir.

## Flujo obligatorio

1. `/analizar "<requerimiento>"`: brainstorming → `requerimiento.md` + `diseno.md` (borrador).
2. `/aprobar NNN` (**sólo el usuario**): aprueba requerimiento y diseño (hash SHA-256).
3. `/planificar`: writing-plans → `tareas.md` (borrador).
4. `/aprobar NNN` (**sólo el usuario**): aprueba el plan.
5. `/implementar`: TDD por tarea; cada tarea se cierra con `completar-tarea.py`.
6. `/aceptar HU-NN` (**sólo el usuario**): acepta cada historia implementada.
7. `/estado`: avance y huecos en cualquier momento.

La primera feature siempre es `000-constitucion`: define stack, arquitectura y convenciones, y
al aprobarla se genera `harness.config.json` (rutas de código y comando de tests).

## Estructura

- `specs/NNN-slug/`: `requerimiento.md`, `diseno.md`, `tareas.md` (+ `.aprobaciones.json` y
  `progreso.json`, que sólo escriben los scripts).
- `specs/.activa`: feature en curso (una a la vez).
- `.claude/`: comandos, hooks y scripts del harness.

## IDs y trazabilidad

- `HU-NN` historia de usuario → `RF-NN` criterio EARS → `T-NN` tarea → test.
- Formatos: `### HU-01: ...`, `- **RF-01**: Cuando ..., el sistema deberá ...`,
  `### T-01 [RF-01, RF-02]: ...`.
- Los tests **siempre** llevan el RF calificado en su nombre: `NNN/RF-NN: ...`
  (p. ej. `it("003/RF-02: rechaza dividir con menos de 2 comensales")`).
- Una aprobación vale sólo mientras el documento no cambie. Editarlo lo devuelve a borrador.

## Prohibido para el agente

- Escribir en rutas de código sin los tres documentos de la feature activa aprobados.
- Editar `.claude/`, `CLAUDE.md`, `harness.config.json`, `.aprobaciones.json`,
  `progreso.json` o `specs/.activa`.
- Ejecutar `aprobar.py` o `aceptar.py`, o simular su efecto.
- Marcar una tarea como hecha sin `completar-tarea.py`, o dar algo por terminado sin evidencia.
- Intentar eludir los hooks (código inline que escribe archivos, rutas indirectas, etc.).
  Si un hook bloquea algo, explica el motivo al usuario y sigue el flujo.

## Idioma

Specs, mensajes y documentación en español.
