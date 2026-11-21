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

## Stack fijo del frontend

Todo proyecto con este harness usa **React + TypeScript + Vite**, con **Vitest** (+ Testing
Library) y **Playwright** en `apps/web/` (preset `react-ts-vite-vitest-playwright`). No se
discute en los análisis; `/aprobar 000` rechaza una constitución que no lo declare.
- Playwright: suite simulada en `apps/web/e2e/` (bloquea cada tarea) y suite completa en
  `apps/web/e2e-stack/` (bloquea la última `/aceptar`).
- Las tareas con `UI: sí` resuelven su fase verde con **Ralph Loop** (ver `/implementar`).
El backend lo decide cada constitución.

## Principios innegociables

`specs/000-constitucion/constitucion.md` (Art. 1–4 núcleo universal + artículos del proyecto)
rige todo diseño, plan, código y revisión. Se aprueba con la constitución y no se modifica sin
volver a `/aprobar 000`.

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
