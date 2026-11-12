#!/usr/bin/env python3
"""Marca una tarea como hecha SÓLO si sus RF tienen tests y la suite pasa.

Uso: completar-tarea.py T-NN [--feature NNN]
"""
import os
import re
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import harness_lib as h  # noqa: E402

IGNORAR = {"node_modules", ".git", "dist", "build", ".venv", "venv", "__pycache__", "target"}
MAX_BYTES = 2_000_000


def textos_de_codigo(raiz: Path, rutas: list[str]):
    for r in rutas:
        base = raiz / r.rstrip("/")
        archivos = [base] if base.is_file() else (base.rglob("*") if base.is_dir() else [])
        for p in archivos:
            if not p.is_file() or IGNORAR & set(p.relative_to(raiz).parts):
                continue
            try:
                if p.stat().st_size <= MAX_BYTES:
                    yield p.read_text(encoding="utf-8")
            except (UnicodeDecodeError, OSError):
                continue


def rf_sin_test(raiz: Path, rutas: list[str], ids: list[str]) -> list[str]:
    pendientes = set(ids)
    for texto in textos_de_codigo(raiz, rutas):
        for i in list(pendientes):
            if re.search(re.escape(i) + r"(?!\d)", texto):
                pendientes.discard(i)
        if not pendientes:
            break
    return sorted(pendientes)


def commit_actual(raiz: Path):
    try:
        r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=raiz, capture_output=True, text=True)
        return r.stdout.strip() or None if r.returncode == 0 else None
    except OSError:
        return None


def main(argv: list[str]) -> int:
    if not argv or not re.fullmatch(r"T-\d{2,}", argv[0]):
        print("Uso: completar-tarea.py T-NN [--feature NNN]", file=sys.stderr)
        return 1
    tarea = argv[0]
    nnn = argv[argv.index("--feature") + 1] if "--feature" in argv else None
    raiz = h.raiz()
    fdir = h.buscar_feature(raiz, nnn)
    pendientes = h.motivo_docs(fdir)
    if pendientes:
        print(f"No se puede completar {tarea}: la spec de {fdir.name} no está aprobada: {pendientes}.",
              file=sys.stderr)
        return 1
    tareas = h.parsear_tareas(h.leer(fdir / h.TAR))
    if tarea not in tareas:
        print(f"{tarea} no existe en {fdir.name}/tareas.md.", file=sys.stderr)
        return 1
    cfg = h.cargar_config(raiz)
    ids = [f"{fdir.name[:3]}/{rf}" for rf in tareas[tarea]]
    faltan = rf_sin_test(raiz, cfg.get("rutas_codigo", []), ids)
    if faltan:
        print(f"No se puede completar {tarea}: no hay tests que referencien {', '.join(faltan)}. "
              "Nombra los tests como \"NNN/RF-NN: ...\".", file=sys.stderr)
        return 1
    timeout = int(os.environ.get("HARNESS_TEST_TIMEOUT", "1800"))
    try:
        r = subprocess.run(cfg["comando_test"], shell=True, cwd=raiz, capture_output=True,
                           text=True, timeout=timeout)
    except subprocess.TimeoutExpired:
        print(f"No se puede completar {tarea}: los tests superaron el tiempo límite ({timeout} s).",
              file=sys.stderr)
        return 1
    if r.returncode != 0:
        cola = "\n".join((r.stdout + r.stderr).splitlines()[-40:])
        print(f"No se puede completar {tarea}: '{cfg['comando_test']}' terminó con código "
              f"{r.returncode}.\n{cola}", file=sys.stderr)
        return 1
    prog = h.cargar_progreso(fdir)
    prog["tareas"][tarea] = {"estado": "hecha", "fecha": h.ahora(), "commit": commit_actual(raiz)}
    h.guardar_progreso(fdir, prog)
    print(f"{tarea} completada en {fdir.name} (tests en verde).")
    implementadas = [hu for hu, e in h.estado_hus(fdir).items() if e == "implementada"]
    if implementadas:
        print("HU listas para que el usuario las acepte con /aceptar: " + ", ".join(implementadas))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except (h.HarnessError, KeyError) as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)
