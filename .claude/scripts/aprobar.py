#!/usr/bin/env python3
"""Aprueba (por hash) los documentos de la fase actual de una feature.

Uso: aprobar.py [NNN]   (sin NNN: la feature activa)
SÓLO lo ejecuta el usuario mediante /aprobar. El gate de Bash impide que lo ejecute el agente.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import harness_lib as h  # noqa: E402


def main(argv: list[str]) -> int:
    raiz = h.raiz()
    fdir = h.buscar_feature(raiz, argv[0] if argv else None)
    if not (h.doc_aprobado(fdir, h.REQ) and h.doc_aprobado(fdir, h.DIS)):
        errores = h.validar_req_diseno(fdir)
        if errores:
            print(f"No se aprobó {fdir.name}. Corrige:")
            print("\n".join(f"  - {e}" for e in errores))
            return 1
        h.aprobar_docs(fdir, [h.REQ, h.DIS])
        print(f"Aprobados requerimiento.md y diseno.md de {fdir.name}.")
        if h.revocar_doc(fdir, h.TAR):
            print("Aviso: tareas.md perdió su aprobación; revisa el plan contra el nuevo diseño "
                  "(/planificar) y vuelve a /aprobar.")
        if h.es_constitucion(fdir):
            cfg = h.escribir_config(raiz)
            print(f"Constitución aprobada. harness.config.json generado "
                  f"(rutas_codigo={cfg['rutas_codigo']}).")
        else:
            print("Siguiente paso: /planificar")
        return 0
    if h.es_constitucion(fdir) or h.doc_aprobado(fdir, h.TAR):
        print(f"Nada que aprobar en {fdir.name}: todos sus documentos ya están aprobados.")
        return 0
    errores = h.validar_tareas(fdir)
    if errores:
        print(f"No se aprobó tareas.md de {fdir.name}. Corrige:")
        print("\n".join(f"  - {e}" for e in errores))
        return 1
    h.aprobar_docs(fdir, [h.TAR])
    print(f"Aprobado tareas.md de {fdir.name}. Siguiente paso: /implementar")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except h.HarnessError as e:
        print(str(e))
        sys.exit(1)
