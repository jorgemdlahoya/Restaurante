#!/usr/bin/env python3
"""Hook PreToolUse (Bash): aplica las reglas del gate SDD a las escrituras hechas por shell.

Heurístico: detecta redirecciones y comandos de escritura comunes. No es una garantía de
seguridad frente a comandos ofuscados (ver spec §6.3).
"""
import json
import os
import re
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))

PROHIBIDOS = [
    (re.compile(r"aprobar\.py|aceptar\.py"),
     "Sólo el usuario puede aprobar o aceptar (comandos /aprobar y /aceptar)."),
    (re.compile(r"\.aprobaciones|progreso\.json"),
     "Los registros de aprobación y avance sólo los tocan los scripts del harness; usa /estado."),
]


def temporal(destino: str) -> bool:
    tmp = [Path(tempfile.gettempdir()).resolve(), Path("/tmp").resolve()]
    p = Path(destino)
    return p.is_absolute() and any(p.resolve().is_relative_to(t) for t in tmp)


def main() -> int:
    import harness_lib as h
    datos = json.loads(sys.stdin.read())
    comando = (datos.get("tool_input") or {}).get("command", "")
    for patron, motivo in PROHIBIDOS:
        if patron.search(comando):
            print(f"Bloqueado por el gate SDD: {motivo}", file=sys.stderr)
            return 2
    destinos = [d for d in h.destinos_escritura(comando) if not temporal(d)]
    if not destinos:
        return 0
    if h.NO_PARSEABLE in destinos:
        print("Bloqueado por el gate SDD: no se pudo analizar el comando (comillas sin cerrar).",
              file=sys.stderr)
        return 2
    if h.cambia_directorio(comando):
        print("Bloqueado por el gate SDD: no combines 'cd' con escrituras; usa rutas desde la "
              "raíz del proyecto.", file=sys.stderr)
        return 2
    raiz = h.raiz(datos.get("cwd"))
    cwd = Path(datos.get("cwd") or raiz)
    for d in destinos:
        motivo = h.evaluar_escritura(raiz, str(d if Path(d).is_absolute() else cwd / d))
        if motivo:
            print(motivo, file=sys.stderr)
            return 2
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:  # noqa: BLE001 — falla cerrado
        print(f"Gate SDD: error interno ({e}) — bloqueado por seguridad.", file=sys.stderr)
        sys.exit(2)
