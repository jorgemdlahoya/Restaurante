---
feature: 000-constitucion
estado: borrador
---
# Diseño: Constitución del proyecto

<!-- Constitución del proyecto. El frontend está fijado por el harness: no se discute.
El brainstorming decide backend, arquitectura, datos, despliegue y convenciones. -->

## Enfoque elegido y alternativas descartadas

{{enfoque}}

## Frontend (fijo del harness)

- **React 18+ con TypeScript**, empaquetado con **Vite**.
- Pruebas con **Vitest + Testing Library + jsdom**.
- Código en `apps/web/`.
- Cada test lleva el RF calificado en su nombre: `it("NNN/RF-NN: …")`.

## Backend

{{backend}}

## Arquitectura

<!-- Cómo se comunican frontend y backend, módulos principales, límites. -->

{{arquitectura}}

## Componentes

<!-- Cada componente indica qué RF de requerimiento.md cubre: "Cubre: RF-01, RF-02". -->

{{componentes}}

## Datos

{{datos}}

## Manejo de errores

{{errores}}

## Despliegue

{{despliegue}}

## Convenciones del proyecto

{{convenciones}}

## Estrategia de pruebas

<!-- Frontend: Vitest. Indica aquí las pruebas del backend y cómo se ejecutan. -->

{{pruebas}}

## Configuración del harness

<!-- /aprobar 000 genera harness.config.json a partir de este bloque.
- "frontend" y "apps/web/" son fijos del harness: no los quites.
- Añade las rutas del backend a "rutas_codigo" (p. ej. "apps/api/").
- Si el backend tiene tests, añádelos a "comando_test" con &&,
  p. ej. "npm --prefix apps/web exec vitest run --passWithNoTests && pytest apps/api".
- --passWithNoTests permite cerrar tareas de backend mientras apps/web aún no tiene tests. -->

```harness-config
{ "stack": "React + TypeScript + Vite + Vitest; backend: {{backend_corto}}",
  "frontend": "react-ts-vite-vitest",
  "rutas_codigo": ["apps/web/"],
  "rutas_libres": ["package.json", "README.md"],
  "comando_test": "npm --prefix apps/web exec vitest run --passWithNoTests" }
```
