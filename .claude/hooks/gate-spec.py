#!/usr/bin/env python3
"""Hook PreToolUse (Write|Edit|MultiEdit|NotebookEdit): bloquea escrituras fuera del flujo SDD.

Exit 0 = permitido · exit 2 = bloqueado (el motivo va a stderr y Claude lo ve).
Ante cualquier error interno, bloquea (falla cerrado).
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))


def main() -> int:
    import harness_lib as h
    datos = json.loads(sys.stdin.read())
    entrada = datos.get("tool_input") or {}
    ruta = entrada.get("file_path") or entrada.get("notebook_path")
    if not ruta:
        print("Gate SDD: la herramienta no indica la ruta destino — bloqueado por seguridad.",
              file=sys.stderr)
        return 2
    motivo = h.evaluar_escritura(h.raiz(datos.get("cwd")), ruta)
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
