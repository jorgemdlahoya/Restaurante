"""Lógica compartida del harness SDD: config, features, aprobaciones, parseo y gates.

Sólo biblioteca estándar. Todos los mensajes de error están en español.
"""
from __future__ import annotations

import datetime as _dt
import hashlib
import json
import os
from pathlib import Path

REQ = "requerimiento.md"
DIS = "diseno.md"
TAR = "tareas.md"
APROBACIONES = ".aprobaciones.json"
PROGRESO = "progreso.json"
CONFIG = "harness.config.json"


class HarnessError(Exception):
    """Error con mensaje en español pensado para el usuario."""


# ---------------------------------------------------------------- raíz y config

def raiz(cwd: str | None = None) -> Path:
    env = os.environ.get("CLAUDE_PROJECT_DIR")
    if env:
        return Path(env).resolve()
    return Path(cwd or os.getcwd()).resolve()


def _leer_json(path: Path, defecto=None):
    if not path.exists():
        if defecto is not None:
            return defecto
        raise HarnessError(f"No existe {path.name}.")
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, UnicodeDecodeError) as e:
        raise HarnessError(f"{path.name} no es JSON válido: {e}") from e


def _escribir_json(path: Path, data) -> None:
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def cargar_config(raiz: Path) -> dict:
    data = _leer_json(raiz / CONFIG)
    if not isinstance(data, dict):
        raise HarnessError(f"{CONFIG} debe ser un objeto JSON.")
    return data


def ahora() -> str:
    return _dt.datetime.now().isoformat(timespec="seconds")


# ---------------------------------------------------------------- features

def specs_dir(raiz: Path) -> Path:
    return raiz / "specs"


def feature_activa(raiz: Path) -> Path | None:
    marca = specs_dir(raiz) / ".activa"
    if not marca.exists():
        return None
    nombre = marca.read_text(encoding="utf-8").strip()
    if not nombre:
        return None
    fdir = specs_dir(raiz) / nombre
    if not fdir.is_dir():
        raise HarnessError(f"specs/.activa apunta a '{nombre}', que no existe. Usa /analizar.")
    return fdir


def buscar_feature(raiz: Path, nnn: str | None) -> Path:
    if not nnn:
        fdir = feature_activa(raiz)
        if fdir is None:
            raise HarnessError("No hay feature activa. Usa /analizar.")
        return fdir
    nnn = nnn.strip()
    candidatos = sorted(p for p in specs_dir(raiz).glob(f"{nnn}*")
                        if p.is_dir() and (p.name == nnn or p.name.startswith(nnn + "-")))
    if not candidatos:
        raise HarnessError(f"No existe la feature '{nnn}'.")
    return candidatos[0]


def listar_features(raiz: Path) -> list[Path]:
    return sorted(p for p in specs_dir(raiz).iterdir()
                  if p.is_dir() and p.name[:3].isdigit()) if specs_dir(raiz).is_dir() else []


def es_constitucion(fdir: Path) -> bool:
    return fdir.name.startswith("000-")


def docs_requeridos(fdir: Path) -> list[str]:
    return [REQ, DIS] if es_constitucion(fdir) else [REQ, DIS, TAR]


# ---------------------------------------------------------------- aprobaciones

def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def cargar_aprobaciones(fdir: Path) -> dict:
    return _leer_json(fdir / APROBACIONES, defecto={})


def guardar_aprobaciones(fdir: Path, data: dict) -> None:
    _escribir_json(fdir / APROBACIONES, data)


def doc_aprobado(fdir: Path, nombre: str) -> bool:
    entrada = cargar_aprobaciones(fdir).get(nombre)
    doc = fdir / nombre
    return bool(entrada) and doc.exists() and entrada.get("sha256") == sha256(doc)


def doc_alterado(fdir: Path, nombre: str) -> bool:
    """Tenía aprobación pero el contenido cambió después."""
    return bool(cargar_aprobaciones(fdir).get(nombre)) and not doc_aprobado(fdir, nombre)


def docs_aprobados(fdir: Path) -> bool:
    return all(doc_aprobado(fdir, d) for d in docs_requeridos(fdir))


def constitucion_aprobada(raiz: Path) -> bool:
    cands = sorted(specs_dir(raiz).glob("000-*")) if specs_dir(raiz).is_dir() else []
    return bool(cands) and docs_aprobados(cands[0])
