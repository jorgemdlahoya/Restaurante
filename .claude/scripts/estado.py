#!/usr/bin/env python3
"""Muestra el avance SDD: matriz HU → RF → T → tests y huecos.

Uso: estado.py [NNN|todas] [--breve]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import harness_lib as h  # noqa: E402


def breve(raiz: Path) -> str:
    fdir = h.feature_activa(raiz)
    if fdir is None:
        return "Sin feature activa — usa /analizar"
    return f"Feature activa: {fdir.name} (fase: {h.fase(fdir)})"


def resumen_todas(raiz: Path) -> str:
    lineas = []
    for fdir in h.listar_features(raiz):
        est = h.estado_hus(fdir)
        acept = sum(1 for e in est.values() if e == "aceptada")
        lineas.append(f"{fdir.name}  {h.fase(fdir)}  {acept}/{len(est)} HU aceptadas")
    return "\n".join(lineas) or "No hay features. Usa /analizar."


def detalle(raiz: Path, fdir: Path) -> str:
    out = [f"{fdir.name} — fase: {h.fase(fdir)}", ""]
    req = h.parsear_requerimiento(h.leer(fdir / h.REQ)) if (fdir / h.REQ).exists() else {}
    tareas = h.parsear_tareas(h.leer(fdir / h.TAR)) if (fdir / h.TAR).exists() else {}
    hechas = h.tareas_hechas(fdir)
    estados = h.estado_hus(fdir)
    huecos = []
    for d in h.docs_requeridos(fdir):
        if h.doc_alterado(fdir, d):
            huecos.append(f"{d}: cambió desde la aprobación (pide /aprobar)")
        elif not h.doc_aprobado(fdir, d):
            out.append(f"{d}: sin aprobar")
    try:
        rutas = h.cargar_config(raiz).get("rutas_codigo", [])
    except h.HarnessError:
        rutas = []
    for hu, rfs in req.items():
        out.append(f"{hu} [{estados.get(hu, 'pendiente')}]")
        for rf in rfs:
            ts = [t for t, trfs in tareas.items() if rf in trfs]
            marcas = ", ".join(f"{t} {'✔' if t in hechas else '·'}" for t in ts) or "—"
            out.append(f"  {rf} → {marcas}")
            if not ts and not h.es_constitucion(fdir):
                huecos.append(f"{rf}: sin tarea")
    if not h.es_constitucion(fdir) and tareas:
        ids = sorted({f"{fdir.name[:3]}/{rf}" for rfs in req.values() for rf in rfs})
        for i in h.rf_sin_test(raiz, rutas, ids):
            huecos.append(f"{i}: sin test")
    out += ["", "Huecos:"] + ([f"  - {x}" for x in huecos] or ["  (ninguno)"])
    return "\n".join(out)


def main(argv: list[str]) -> int:
    raiz = h.raiz()
    if "--breve" in argv:
        print(breve(raiz))
        return 0
    args = [a for a in argv if not a.startswith("--")]
    if args and args[0] == "todas":
        print(resumen_todas(raiz))
        return 0
    print(detalle(raiz, h.buscar_feature(raiz, args[0] if args else None)))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except h.HarnessError as e:
        print(str(e))
        sys.exit(1)
