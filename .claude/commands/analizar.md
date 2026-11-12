---
description: Comando base SDD. Analiza un requerimiento con brainstorming y produce requerimiento.md + diseno.md (sin código).
argument-hint: "<descripción del requerimiento>"
---

# /analizar — análisis de requerimiento (SDD)

Requerimiento del usuario: **$ARGUMENTS**

Esta fase **no produce código**. Su única salida son `requerimiento.md` y `diseno.md` en
borrador dentro de la feature activa. Sigue los pasos en orden.

## 1. Preparar la feature

Ejecuta:

```bash
python3 .claude/scripts/nueva-feature.py "$ARGUMENTS"
```

- La primera línea de la salida es la carpeta de la feature (`specs/NNN-slug`). Trabaja ahí.
- Si crea o reutiliza `000-constitucion`, el análisis es la **constitución del proyecto**:
  stack, arquitectura general, convenciones, estrategia de pruebas y principios. Su
  `diseno.md` debe incluir el bloque ```` ```harness-config ```` (ver la plantilla).
- Exit 3: hay otra feature activa sin cerrar. **Pregunta al usuario** si quiere cambiar; sólo
  si acepta, re-ejecuta con `--forzar`.
- Exit 1: informa el mensaje al usuario y detente.

## 2. Reunir contexto

Lee `CLAUDE.md`, `specs/000-constitucion/` (si está aprobada) y los `requerimiento.md` de las
features existentes, para detectar solapamientos o dependencias con este requerimiento.

## 3. Brainstorming

Invoca el skill `superpowers:brainstorming` y síguelo, con estas preferencias del usuario,
que tienen prioridad sobre los valores por defecto del skill:

- Clasifica siempre el trabajo como **arquitectónico** (camino completo).
- Haz las preguntas **de una en una**, sobre: propósito, actores, reglas de negocio,
  casos borde y criterios de éxito.
- Si el requerimiento abarca varios subsistemas independientes, propón dividirlo en varias
  features antes de seguir; analiza aquí sólo la primera.
- Propón 2–3 enfoques con tu recomendación y presenta el diseño por secciones, pidiendo
  aprobación de cada una.
- **Ubicación de la spec:** no uses `docs/superpowers/specs/`. Escribe la spec en **dos**
  archivos de la feature activa, reemplazando todos los `{{...}}` de las plantillas:
  - `requerimiento.md`: historias `### HU-NN:` con sus criterios `- **RF-NN**:` en formato
    EARS, RNF, fuera de alcance y preguntas abiertas.
  - `diseno.md`: enfoque, arquitectura, componentes (cada uno con `Cubre: RF-..`), datos,
    errores y estrategia de pruebas.
- No hagas commit de la spec en este paso.

## 4. Autorrevisión

Antes de entregar, revisa y corrige:
- no quedan `{{...}}`, "TBD" ni secciones vacías;
- no hay contradicciones entre requerimiento y diseño;
- cada `RF` aparece en al menos un componente de `diseno.md`;
- "Preguntas abiertas" queda vacía (o `- Ninguna`); si queda alguna, pregúntala al usuario.

## 5. Cierre

**NO invoques `superpowers:writing-plans`** ni escribas código. Termina con:

> Spec de `NNN-slug` lista en `requerimiento.md` y `diseno.md`. Revísala y, si está bien,
> ejecuta `/aprobar NNN`. Después seguiremos con `/planificar`.
