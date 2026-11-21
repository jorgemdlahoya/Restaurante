---
description: (Sólo usuario) Acepta manualmente una historia de usuario ya implementada.
argument-hint: "HU-NN [--feature NNN]"
disable-model-invocation: true
allowed-tools: Bash(python3 .claude/scripts/aceptar.py:*)
---

Resultado de la aceptación:

!`python3 .claude/scripts/aceptar.py $ARGUMENTS`

Informa al usuario el resultado anterior. Si era la última HU y la suite completa de Playwright
falló, indícale que levante el sistema con `docker compose up` y repita `/aceptar`. Si la feature quedó cerrada, invoca el skill
`superpowers:finishing-a-development-branch`. No modifiques ningún archivo de `specs/`.
