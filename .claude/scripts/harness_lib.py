"""Lógica compartida del harness SDD: config, features, aprobaciones, parseo y gates.

Sólo biblioteca estándar. Todos los mensajes de error están en español.
"""
from __future__ import annotations

import datetime as _dt
import hashlib
import json
import os
import re as _re
import shlex as _shlex
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
        elif propias and propias <= hechas and all(
                any(rf in trfs for trfs in tareas.values()) for rf in rfs):
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


# ---------------------------------------------------------------- reglas de escritura

def _coincide(rel: str, patrones: list[str]) -> bool:
    for p in patrones:
        p = p.strip()
        if p.startswith("./"):
            p = p[2:]
        if not p:
            continue
        if p.endswith("/"):
            if rel.startswith(p):
                return True
        elif rel == p or rel.startswith(p + "/"):
            return True
    return False


def _relativa(raiz: Path, ruta: str) -> str | None:
    base = raiz.resolve()
    p = Path(ruta)
    destino = (p if p.is_absolute() else base / p).resolve()
    try:
        return destino.relative_to(base).as_posix()
    except ValueError:
        return None


def _es_protegido(rel: str) -> bool:
    nombre = rel.rsplit("/", 1)[-1]
    return (rel.startswith(".claude/") or rel == ".claude"
            or rel in (CONFIG, "CLAUDE.md", "specs/.activa")
            or (rel.startswith("specs/") and nombre in (APROBACIONES, PROGRESO)))


def motivo_docs(fdir: Path) -> str:
    faltan = []
    for d in docs_requeridos(fdir):
        if doc_alterado(fdir, d):
            faltan.append(f"{d} (cambió desde la aprobación)")
        elif not doc_aprobado(fdir, d):
            faltan.append(f"{d} (sin aprobar)")
    return ", ".join(faltan)


def evaluar_escritura(raiz: Path, ruta: str) -> str | None:
    """None = permitido; str = motivo del bloqueo (español, con la acción sugerida)."""
    rel = _relativa(raiz, ruta)
    if rel is None:
        return f"Bloqueado: '{ruta}' está fuera del proyecto."
    if _es_protegido(rel):
        return (f"Bloqueado: '{rel}' está protegido por el harness SDD. Sólo lo modifica el usuario "
                "(o los comandos /aprobar, /aceptar y los scripts del harness).")
    if _coincide(rel, ["specs/", "docs/"]):
        return None
    if not constitucion_aprobada(raiz):
        return (f"Bloqueado: '{rel}' — la constitución (000) no está aprobada, así que aún no hay "
                "rutas de código. Completa /analizar y pide al usuario /aprobar 000.")
    cfg = cargar_config(raiz)
    if _coincide(rel, list(cfg.get("rutas_codigo", []))):
        fdir = feature_activa(raiz)
        if fdir is None:
            return f"Bloqueado: '{rel}' es código y no hay feature activa. Usa /analizar."
        pendientes = motivo_docs(fdir)
        if pendientes:
            return (f"Bloqueado: '{rel}' es código y {fdir.name} no tiene la spec aprobada: "
                    f"{pendientes}. Pide al usuario ejecutar /aprobar {fdir.name[:3]}.")
        return None
    if _coincide(rel, list(cfg.get("rutas_libres", []))):
        return None
    return (f"Bloqueado: '{rel}' no está en rutas_codigo ni rutas_libres de {CONFIG}. "
            "Si debe permitirse, el usuario debe añadirla a mano.")


# ---------------------------------------------------------------- aprobación y config

def aprobar_docs(fdir: Path, nombres: list[str]) -> None:
    data = cargar_aprobaciones(fdir)
    for n in nombres:
        data[n] = {"sha256": sha256(fdir / n), "fecha": ahora()}
    guardar_aprobaciones(fdir, data)


def revocar_doc(fdir: Path, nombre: str) -> bool:
    data = cargar_aprobaciones(fdir)
    if nombre in data:
        del data[nombre]
        guardar_aprobaciones(fdir, data)
        return True
    return False


def escribir_config(raiz: Path) -> dict:
    cands = sorted(specs_dir(raiz).glob("000-*"))
    if not cands or not (doc_aprobado(cands[0], REQ) and doc_aprobado(cands[0], DIS)):
        raise HarnessError("La constitución no está aprobada; no se puede generar la config.")
    cfg = {"constitucion": "aprobada", **extraer_config(leer(cands[0] / DIS))}
    _escribir_json(raiz / CONFIG, cfg)
    return cfg


# ---------------------------------------------------------------- trazabilidad de tests

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
            if _re.search(_re.escape(i) + r"(?!\d)", texto):
                pendientes.discard(i)
        if not pendientes:
            break
    return sorted(pendientes)


# ---------------------------------------------------------------- análisis de Bash (heurístico)


_SEPARADORES = {";", "&&", "||", "|", "&", "|&"}
_REDIRECCIONES = {">", ">>", ">|", "&>", "&>>"}
NO_PARSEABLE = "<no-parseable>"


def _segmentos(comando: str) -> list[list[str]]:
    segs: list[list[str]] = []
    for linea in comando.replace("\\\n", " ").split("\n"):
        lex = _shlex.shlex(linea, posix=True, punctuation_chars=";&|<>")
        lex.whitespace_split = True
        actual: list[str] = []
        for tok in lex:
            if tok in _SEPARADORES:
                segs.append(actual)
                actual = []
            else:
                actual.append(tok)
        segs.append(actual)
    return [s for s in segs if s]


def _sin_flags(args: list[str]) -> list[str]:
    return [a for a in args if not a.startswith("-")]


def _destinos_segmento(seg: list[str]) -> tuple[list[str], list[str]]:
    """Devuelve (destinos, argv sin redirecciones)."""
    destinos, argv, i = [], [], 0
    while i < len(seg):
        tok = seg[i]
        if tok in _REDIRECCIONES and i + 1 < len(seg):
            destinos.append(seg[i + 1])
            i += 2
            continue
        if tok in ("<", "<<", "<<<") and i + 1 < len(seg):
            i += 2
            continue
        argv.append(tok)
        i += 1
    while argv and ("=" in argv[0] and not argv[0].startswith("-") or argv[0] in ("sudo", "env", "command")):
        argv.pop(0)
    # descriptor numérico pegado a la redirección (2>, 1>>): queda como último arg suelto
    if destinos and argv and argv[-1].isdigit():
        argv.pop()
    if not argv:
        return destinos, argv
    cmd, args = Path(argv[0]).name, argv[1:]
    if cmd == "tee":
        destinos += _sin_flags(args)
    elif cmd == "sed" and any(a.startswith("-i") or a == "--in-place" for a in args):
        destinos += _sin_flags(args)[-1:]
    elif cmd in ("cp", "mv", "install", "ln", "rsync", "truncate"):
        destinos += _sin_flags(args)[-1:]
    elif cmd in ("rm", "rmdir", "touch", "mkdir", "shred", "unlink"):
        destinos += _sin_flags(args)
    elif cmd == "dd":
        destinos += [a[3:] for a in args if a.startswith("of=")]
    elif cmd == "git" and args:
        if args[0] == "checkout" and "--" in args:
            destinos += args[args.index("--") + 1:]
        elif args[0] == "restore":
            destinos += _sin_flags(args[1:])
    return destinos, argv


def destinos_escritura(comando: str) -> list[str]:
    try:
        segs = _segmentos(comando)
    except ValueError:
        return [NO_PARSEABLE]
    destinos: list[str] = []
    for seg in segs:
        destinos += _destinos_segmento(seg)[0]
    return [d for d in destinos if d not in ("/dev/null", "/dev/stdout", "/dev/stderr")]


def cambia_directorio(comando: str) -> bool:
    try:
        return any(seg and seg[0] in ("cd", "pushd") for seg in _segmentos(comando))
    except ValueError:
        return True
