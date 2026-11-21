---
feature: {{feature}}
estado: borrador
---
# Diseño: {{titulo}}

<!-- Constitución del proyecto. El frontend está fijado por el harness: no se discute.
El brainstorming decide backend, arquitectura, datos, despliegue y convenciones. -->

## Enfoque elegido y alternativas descartadas

{{enfoque}}

## Frontend (fijo del harness)

- **React 18+ con TypeScript**, empaquetado con **Vite**. Código en `apps/web/`.
- **Vitest + Testing Library + jsdom** para componentes y lógica (TDD rápido).
- **Playwright** (`@playwright/test`) para los flujos de punta a punta, en dos suites:
  - `apps/web/e2e/`: la app servida por Vite con la API simulada (`page.route`). Forma parte
    de `comando_test` y bloquea cada tarea; toda tarea con flujo visible tiene aquí su prueba.
  - `apps/web/e2e-stack/`: contra el sistema completo levantado con `docker compose`
    (`E2E_BASE_URL`, por defecto `http://localhost:8080`), sin simulaciones. Se ejecuta con
    `comando_e2e_stack` y bloquea el cierre de la feature (última `/aceptar`).
- Cada test lleva el RF calificado en su nombre: `it("NNN/RF-NN: …")` en Vitest y
  `test("NNN/RF-NN: …")` en Playwright.
- En las tareas con flujo visible, la fase verde se itera con **Ralph Loop** (ver `/implementar`).

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

<!-- Frontend: Vitest (componentes y lógica) + Playwright (suite simulada en apps/web/e2e/ y
suite completa en apps/web/e2e-stack/). Indica aquí las pruebas del backend y cómo se ejecutan. -->

{{pruebas}}

## Configuración del harness

<!-- /aprobar 000 genera harness.config.json a partir de este bloque.
- "frontend" y "apps/web/" son fijos del harness: no los quites.
- Añade las rutas del backend a "rutas_codigo" (p. ej. "apps/api/").
- "comando_test" debe ejecutar vitest y playwright; "comando_e2e_stack" ejecuta la suite completa.
- Si el backend tiene tests, añádelos a "comando_test" con &&,
  p. ej. "npm --prefix apps/web exec vitest run --passWithNoTests && pytest apps/api".
- --passWithNoTests permite cerrar tareas de backend mientras apps/web aún no tiene tests. -->

```harness-config
{ "stack": "React + TypeScript + Vite + Vitest + Playwright; backend: {{backend_corto}}",
  "frontend": "react-ts-vite-vitest-playwright",
  "rutas_codigo": ["apps/web/"],
  "rutas_libres": ["package.json", "README.md"],
  "comando_test": "npm --prefix apps/web exec vitest run --passWithNoTests && npm --prefix apps/web exec playwright test --pass-with-no-tests",
  "comando_e2e_stack": "npm --prefix apps/web exec playwright test --config playwright.stack.config.ts" }
```
