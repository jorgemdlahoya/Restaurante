---
titulo: Preset React para el harness SDD
fecha: 2026-11-14
estado: borrador
---

# Preset React para el harness SDD

> Actualizado el 2026-11-21: el preset pasó a `react-ts-vite-vitest-playwright` (añade
> Playwright y Ralph Loop). Ver `2026-11-21-preset-playwright-design.md`.

## Intención

El harness se reutilizará en varios proyectos, todos con frontend React. El stack de
frontend deja de decidirse en cada constitución: viene fijo en el harness, y `/aprobar 000`
rechaza una constitución que no lo respete. La constitución sólo decide lo que varía entre
proyectos (backend, dominio, despliegue, convenciones propias).

- **Objetivo:** frontend siempre React + TypeScript + Vite + Vitest (+ Testing Library) en
  `apps/web/`, sin volver a preguntarlo.
- **No objetivo:** fijar backend, estado global, estilos o enrutado (los decide cada constitución).
- **Anti-objetivo:** generar código del frontend al aprobar la constitución (sería código sin
  feature ni tests).

## Enfoques

1. **Preset + validación** — elegido por el usuario. Plantilla de constitución prerrellenada,
   `/analizar` no pregunta el frontend y `/aprobar 000` valida el bloque `harness-config`.
2. Sólo preset — descartado: nada impide que un análisis cambie el stack.
3. Preset + esqueleto generado — descartado: código fuera del flujo SDD y dependencia de red.

## Diseño

- **Un único lugar para el preset:** constante `PRESET_FRONTEND` en `harness_lib.py`:
  `{"id": "react-ts-vite-vitest", "ruta": "apps/web/", "test": "vitest"}`. Cambiar de preset
  en el futuro = cambiar esa constante y la plantilla.
- **`harness-config` gana la clave obligatoria `frontend`.** `extraer_config` exige, además de
  lo actual:
  - `frontend == "react-ts-vite-vitest"`;
  - `"apps/web/"` dentro de `rutas_codigo`;
  - `comando_test` contiene `vitest`.

  Si falla, el error dice qué falta y que el frontend es fijo del harness.
- **Plantilla propia de la constitución:** `specs/_plantillas/constitucion/diseno.md`, con:
  - la sección "Frontend (fijo del harness)" ya escrita: React 18+ con TypeScript, Vite,
    Vitest + Testing Library + jsdom, código en `apps/web/`, tests como
    `it("NNN/RF-NN: …")`;
  - placeholders `{{...}}` para backend, arquitectura, datos, despliegue y convenciones;
  - el bloque `harness-config` sin comentar, con `frontend` y la ruta `apps/web/` ya puestos, y
    `comando_test` partiendo de `npm --prefix apps/web exec vitest run` (la constitución añade
    el del backend con `&&`).

  `nueva-feature.py` usa esta plantilla para `diseno.md` al crear `000-constitucion`. Las demás
  features siguen con las plantillas actuales.
- **`/analizar`:** en el caso de la constitución, indica que el frontend está fijado y no se
  pregunta; el brainstorming cubre backend, dominio, despliegue y convenciones.
- **`CLAUDE.md`:** nueva sección "Stack fijo del frontend" (≤ 8 líneas).
- **`README.md`:** el harness se presenta como harness para proyectos React y explica cómo
  cambiar el preset.
- **Spec original** (`2026-11-12-harness-sdd-design.md`): CE-5 y §8 actualizados (el
  frontend deja de ser agnóstico).

## Principios innegociables (`constitucion.md`)

El usuario aporta un texto de principios (Art. 1–4 núcleo universal: seguridad sin secretos,
TDD, diseño acotado con inyección de dependencias, verificar antes de declarar terminado; Art. 5
documentación ejecutable del API, condicional). Deben venir de serie en toda constitución.

- **Enfoques:** (A) tercer documento `constitucion.md` aprobado por hash — **elegido por el
  usuario**; (B) sección dentro de `diseno.md` — descartado: mezcla principios permanentes con
  decisiones técnicas; (C) archivo en `docs/` — descartado: el agente podría editarlo sin aprobación.
- **Plantilla** `specs/_plantillas/constitucion/constitucion.md` con el texto del usuario,
  incluidos `{{CORS_RULE}}` y las referencias del Art. 5 tal cual, más:
  - **Regla rectora** definida al inicio (llamada del agente, a partir de la frase del usuario):
    "Estos principios aplican íntegramente al código nuevo y, en cualquier flujo que se toque,
    a todo su alcance".
  - Art. 5 marcado como condicional: se conserva y adapta si el proyecto expone una API;
    si no, se elimina en el análisis.
  - Sección final `## Artículos del proyecto` con `{{articulos_proyecto}}`.
- **`000-constitucion` requiere tres documentos:** `requerimiento.md`, `diseno.md` y
  `constitucion.md`. `/aprobar 000` los valida y aprueba juntos; editar cualquiera lo devuelve
  a borrador. Validación extra: `constitucion.md` existe, no tiene `{{`, y conserva los
  encabezados `### Artículo 1` a `### Artículo 4` (el núcleo no se puede quitar).
- **Compatibilidad:** `constitucion.md` es obligatorio en toda constitución, salvo en las
  aprobadas antes de este cambio (con aprobación de `requerimiento.md` y sin haber tenido nunca
  `constitucion.md`), que siguen aprobadas. Borrarlo, antes o después de aprobar, impide aprobar.
  Los comentarios HTML no cuentan para placeholders ni para los encabezados del núcleo.
- **Uso en el flujo:** `/analizar` (constitución) rellena `constitucion.md` en el brainstorming;
  `/analizar` de otras features, `/planificar` e `/implementar` leen
  `specs/000-constitucion/constitucion.md` y comprueban que el diseño, el plan y el código lo
  cumplen; la revisión de código se hace contra requisitos y contra la constitución (Art. 4.3).
  `CLAUDE.md` lo menciona como fuente de principios.

## Pruebas

- `extraer_config` rechaza cada uno de estos casos: falta `frontend`, otro valor de
  `frontend`, falta `apps/web/` en las rutas, `comando_test` sin `vitest`. Acepta el bloque de
  la plantilla una vez rellenado.
- `nueva-feature.py` crea la constitución con la sección de frontend y el bloque
  `harness-config` prerrellenados; una feature normal sigue usando la plantilla genérica.
- `aprobar.py 000` rechaza una constitución con otro frontend.
- Se actualizan los tests y fixtures existentes (`CONFIG_BLOQUE`, prueba extremo a extremo)
  al nuevo formato.

## Forma de trabajo

Los hooks protegen `.claude/` y `CLAUDE.md`. El usuario añade `"disableAllHooks": true` en
`.claude/settings.local.json` antes de implementar y lo quita al terminar; la verificación
final se hace con los hooks reactivados (suite completa en verde).

## Decisiones del agente (revisables)

- `react-ts-vite-vitest` como identificador del preset.
- `npm --prefix apps/web exec vitest run` como comando de tests base.
- Una plantilla de `diseno.md` separada para la constitución, en lugar de condicionales dentro
  de la plantilla genérica.
