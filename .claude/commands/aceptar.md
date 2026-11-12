---
description: (Sólo usuario) Acepta manualmente una historia de usuario ya implementada.
argument-hint: "HU-NN [--feature NNN]"
disable-model-invocation: true
allowed-tools: Bash(python3 .claude/scripts/aceptar.py:*)
---

Resultado de la aceptación:

!`python3 .claude/scripts/aceptar.py $ARGUMENTS`

Informa al usuario el resultado anterior. Si la feature quedó cerrada, invoca el skill
`superpowers:finishing-a-development-branch`. No modifiques ningún archivo de `specs/`.
