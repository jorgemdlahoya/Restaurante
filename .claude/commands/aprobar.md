---
description: (Sólo usuario) Aprueba por hash los documentos de la fase actual de una feature.
argument-hint: "[NNN]"
disable-model-invocation: true
allowed-tools: Bash(python3 .claude/scripts/aprobar.py:*)
---

Resultado de la aprobación:

!`python3 .claude/scripts/aprobar.py $ARGUMENTS`

Informa al usuario el resultado anterior tal cual, en una o dos frases, e indica el siguiente
paso que muestra. No modifiques ningún archivo.
