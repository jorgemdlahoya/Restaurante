#!/usr/bin/env python3
"""Crea una feature SDD: specs/NNN-<slug>/ desde plantillas y la marca como activa.

Uso: nueva-feature.py "<texto del requerimiento>" [--forzar]
Exit: 0 ok · 1 error · 3 hay una feature activa sin cerrar (re-ejecutar con --forzar)
"""
import sys
import unicodedata
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import harness_lib as h  # noqa: E402


def slug(texto: str) -> str:
    t = unicodedata.normalize("NFKD", texto).encode("ascii", "ignore").decode().lower()
    t = "".join(c if c.isalnum() else "-" for c in t)
    while "--" in t:
        t = t.replace("--", "-")
    return t.strip("-")[:40].rstrip("-")


def crear(raiz: Path, nombre: str, titulo: str) -> Path:
    fdir = h.specs_dir(raiz) / nombre
    fdir.mkdir(parents=True)
    plantillas = h.specs_dir(raiz) / "_plantillas"
    for doc in (h.REQ, h.DIS, h.TAR):
        texto = h.leer(plantillas / doc).replace("{{feature}}", nombre).replace("{{titulo}}", titulo)
        (fdir / doc).write_text(texto, encoding="utf-8")
    return fdir


def activar(raiz: Path, nombre: str) -> None:
    (h.specs_dir(raiz) / ".activa").write_text(nombre + "\n", encoding="utf-8")


def main(argv: list[str]) -> int:
    forzar = "--forzar" in argv
    args = [a for a in argv if a != "--forzar"]
    if not args:
        print("Uso: nueva-feature.py \"<requerimiento>\" [--forzar]", file=sys.stderr)
        return 1
    texto = " ".join(args)
    raiz = h.raiz()
    activa = h.feature_activa(raiz)

    constit = sorted(h.specs_dir(raiz).glob("000-*"))
    if not constit:
        crear(raiz, "000-constitucion", "Constitución del proyecto")
        activar(raiz, "000-constitucion")
        print("specs/000-constitucion")
        print("Aviso: el proyecto aún no tiene constitución; se creó 000-constitucion. "
              "Analiza primero stack, arquitectura y principios.")
        return 0
    if not h.constitucion_aprobada(raiz):
        if activa is not None and activa == constit[0]:
            print(f"specs/{activa.name}")
            print("Aviso: la constitución sigue pendiente; continúa con ella.")
            return 0
        print(f"La constitución ({constit[0].name}) no está aprobada. "
              "Termínala con /analizar y /aprobar 000 antes de otra feature.", file=sys.stderr)
        return 1

    if activa is not None and h.fase(activa) != "cerrada" and not forzar:
        print(f"La feature activa {activa.name} no está cerrada (fase: {h.fase(activa)}). "
              "Pregunta al usuario y re-ejecuta con --forzar para cambiarla.", file=sys.stderr)
        return 3

    s = slug(texto)
    if not s:
        print("El requerimiento no produce un nombre válido; usa palabras.", file=sys.stderr)
        return 1
    numeros = [int(p.name[:3]) for p in h.listar_features(raiz)]
    nombre = f"{max(numeros, default=0) + 1:03d}-{s}"
    crear(raiz, nombre, texto.strip())
    activar(raiz, nombre)
    print(f"specs/{nombre}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except h.HarnessError as e:
        print(str(e), file=sys.stderr)
        sys.exit(1)
