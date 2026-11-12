"""Lógica compartida del harness SDD: config, features, aprobaciones, parseo y gates.

Sólo biblioteca estándar. Todos los mensajes de error están en español.
"""
from __future__ import annotations

import datetime as _dt
import hashlib
import json
import os
import re as _re
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


# ---------------------------------------------------------------- parseo de specs


_RE_HU = _re.compile(r"^###\s+(HU-\d{2,})\s*:", _re.M)
_RE_RF = _re.compile(r"\*\*(RF-\d{2,})\*\*")
_RE_T = _re.compile(r"^###\s+(T-\d{2,})\s*\[([^\]]*)\]", _re.M)
_RE_CFG = _re.compile(r"```harness-config\s*\n(.*?)\n```", _re.S)
_RE_COMENT = _re.compile(r"<!--.*?-->", _re.S)
_CLAVES_CFG = {"stack": str, "rutas_codigo": list, "rutas_libres": list, "comando_test": str}


def leer(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")


def _rf_por_hu(texto: str) -> list[tuple[str, list[str]]]:
    """Lista ordenada (HU, [RF...]) conservando duplicados para validar."""
    texto = texto.lstrip("﻿").replace("\r\n", "\n")
    res: list[tuple[str, list[str]]] = []
    actual = None
    for linea in texto.split("\n"):
        m = _RE_HU.match(linea)
        if m:
            actual = (m.group(1), [])
            res.append(actual)
            continue
        if linea.startswith("## ") or linea.startswith("# "):
            actual = None
            continue
        if actual is not None:
            actual[1].extend(_RE_RF.findall(linea))
    return res


def parsear_requerimiento(texto: str) -> dict[str, list[str]]:
    return {hu: rfs for hu, rfs in _rf_por_hu(texto)}


def preguntas_abiertas(texto: str) -> str:
    texto = texto.replace("\r\n", "\n")
    m = _re.search(r"^## Preguntas abiertas\s*$(.*?)(?=^## |\Z)", texto, _re.M | _re.S)
    if not m:
        return ""
    cuerpo = _RE_COMENT.sub("", m.group(1))
    lineas = [l.strip() for l in cuerpo.split("\n")]
    return "\n".join(l for l in lineas if l and l.lower().lstrip("-* ").rstrip(".") != "ninguna")


def parsear_tareas(texto: str) -> dict[str, list[str]]:
    texto = texto.replace("\r\n", "\n")
    return {t: [r.strip() for r in rfs.split(",") if r.strip()] for t, rfs in _RE_T.findall(texto)}


def extraer_config(texto: str) -> dict:
    m = _RE_CFG.search(texto.replace("\r\n", "\n"))
    if not m:
        raise HarnessError("Falta el bloque ```harness-config``` en diseno.md de la constitución.")
    try:
        cfg = json.loads(m.group(1))
    except json.JSONDecodeError as e:
        raise HarnessError(f"El bloque harness-config no es JSON válido: {e}") from e
    for clave, tipo in _CLAVES_CFG.items():
        if not isinstance(cfg.get(clave), tipo):
            raise HarnessError(f"harness-config: falta '{clave}' o no es {tipo.__name__}.")
    if not cfg["rutas_codigo"]:
        raise HarnessError("harness-config: 'rutas_codigo' no puede estar vacía.")
    return cfg


def _placeholders(fdir: Path, nombres: list[str]) -> list[str]:
    return [f"{n}: quedan placeholders {{{{...}}}} de la plantilla"
            for n in nombres if (fdir / n).exists() and "{{" in leer(fdir / n)]


def validar_req_diseno(fdir: Path) -> list[str]:
    errores: list[str] = []
    for n in (REQ, DIS):
        if not (fdir / n).exists():
            errores.append(f"Falta {n}.")
    if errores:
        return errores
    req, dis = leer(fdir / REQ), leer(fdir / DIS)
    hus = _rf_por_hu(req)
    if not hus:
        errores.append("requerimiento.md: no hay historias de usuario (### HU-NN: ...).")
    vistos: set[str] = set()
    for hu, rfs in hus:
        if not rfs:
            errores.append(f"requerimiento.md: {hu} no tiene RF (- **RF-NN**: ...).")
        for rf in rfs:
            if rf in vistos:
                errores.append(f"requerimiento.md: {rf} está duplicado.")
            vistos.add(rf)
    if preguntas_abiertas(req):
        errores.append("requerimiento.md: la sección 'Preguntas abiertas' no está vacía.")
    errores += _placeholders(fdir, [REQ, DIS])
    for rf in sorted(vistos):
        if not _re.search(rf"\b{rf}\b", dis):
            errores.append(f"diseno.md: {rf} no está cubierto por ningún componente.")
    if es_constitucion(fdir):
        try:
            extraer_config(dis)
        except HarnessError as e:
            errores.append(str(e))
    return errores


def validar_tareas(fdir: Path) -> list[str]:
    if not (fdir / TAR).exists():
        return ["Falta tareas.md."]
    errores: list[str] = []
    rfs_req = {rf for rfs in parsear_requerimiento(leer(fdir / REQ)).values() for rf in rfs}
    tareas = parsear_tareas(leer(fdir / TAR))
    if not tareas:
        errores.append("tareas.md: no hay tareas (### T-NN [RF-NN]: ...).")
    cubiertos: set[str] = set()
    for t, rfs in tareas.items():
        if not rfs:
            errores.append(f"tareas.md: {t} no referencia ningún RF.")
        for rf in rfs:
            if rf not in rfs_req:
                errores.append(f"tareas.md: {t} referencia {rf}, que no existe en requerimiento.md.")
            cubiertos.add(rf)
    for rf in sorted(rfs_req - cubiertos):
        errores.append(f"tareas.md: {rf} no tiene ninguna tarea.")
    errores += _placeholders(fdir, [TAR])
    return errores


# ---------------------------------------------------------------- avance y estados

def cargar_progreso(fdir: Path) -> dict:
    data = _leer_json(fdir / PROGRESO, defecto={})
    data.setdefault("tareas", {})
    data.setdefault("aceptadas", {})
    return data


def guardar_progreso(fdir: Path, data: dict) -> None:
    _escribir_json(fdir / PROGRESO, data)


def tareas_hechas(fdir: Path) -> set[str]:
    return {t for t, v in cargar_progreso(fdir)["tareas"].items() if v.get("estado") == "hecha"}


def estado_hus(fdir: Path) -> dict[str, str]:
    hus = parsear_requerimiento(leer(fdir / REQ)) if (fdir / REQ).exists() else {}
    tareas = parsear_tareas(leer(fdir / TAR)) if (fdir / TAR).exists() else {}
    hechas = tareas_hechas(fdir)
    aceptadas = cargar_progreso(fdir)["aceptadas"]
    res = {}
    for hu, rfs in hus.items():
        propias = {t for t, trfs in tareas.items() if set(trfs) & set(rfs)}
        if hu in aceptadas:
            res[hu] = "aceptada"
        elif propias and propias <= hechas:
            res[hu] = "implementada"
        elif propias & hechas:
            res[hu] = "en-curso"
        else:
            res[hu] = "pendiente"
    return res


def fase(fdir: Path) -> str:
    if not (doc_aprobado(fdir, REQ) and doc_aprobado(fdir, DIS)):
        return "analisis"
    if es_constitucion(fdir):
        return "cerrada"
    if not doc_aprobado(fdir, TAR):
        return "diseno-aprobado"
    estados = estado_hus(fdir)
    if estados and all(e == "aceptada" for e in estados.values()):
        return "cerrada"
    return "implementando" if tareas_hechas(fdir) else "plan-aprobado"
