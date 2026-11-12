#!/usr/bin/env python3
"""Genera harness.config.json desde el bloque harness-config de la constitución aprobada."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import harness_lib as h  # noqa: E402

if __name__ == "__main__":
    try:
        cfg = h.escribir_config(h.raiz())
        print(f"harness.config.json generado: rutas_codigo={cfg['rutas_codigo']}, "
              f"comando_test={cfg['comando_test']!r}")
    except h.HarnessError as e:
        print(str(e))
        sys.exit(1)
