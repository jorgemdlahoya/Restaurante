---
feature: {{feature}}
estado: borrador
---
# Diseño: {{titulo}}

## Enfoque elegido y alternativas descartadas

{{enfoque}}

## Arquitectura

{{arquitectura}}

## Componentes

<!-- Cada componente indica qué RF cubre, p. ej.:
- **DivisorDeCuenta**: ... Cubre: RF-01, RF-02
Todo RF de requerimiento.md debe aparecer aquí. -->

{{componentes}}

## Datos

{{datos}}

## Manejo de errores

{{errores}}

## Estrategia de pruebas

<!-- Los tests deben nombrarse con el RF calificado: "NNN/RF-NN: ..." -->

{{pruebas}}

<!--
SÓLO EN 000-constitucion: incluye este bloque (sin comentario). /aprobar genera
harness.config.json a partir de él.

```harness-config
{ "stack": "...",
  "rutas_codigo": ["apps/"],
  "rutas_libres": ["package.json", "README.md"],
  "comando_test": "npm test" }
```
-->
