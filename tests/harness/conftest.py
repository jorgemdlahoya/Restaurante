import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

REPO = Path(__file__).resolve().parents[2]
SCRIPTS = REPO / ".claude" / "scripts"
HOOKS = REPO / ".claude" / "hooks"
sys.path.insert(0, str(SCRIPTS))


@pytest.fixture
def proyecto(tmp_path, monkeypatch):
    raiz = tmp_path / "proj"
    (raiz / "specs").mkdir(parents=True)
    shutil.copytree(REPO / "specs" / "_plantillas", raiz / "specs" / "_plantillas",
                    dirs_exist_ok=True) if (REPO / "specs" / "_plantillas").exists() else None
    (raiz / "harness.config.json").write_text('{"constitucion": "pendiente"}')
    monkeypatch.setenv("CLAUDE_PROJECT_DIR", str(raiz))
    return raiz


def crear_feature(raiz, nombre, docs, aprobar=(), activa=True):
    import harness_lib as h
    fdir = raiz / "specs" / nombre
    fdir.mkdir(parents=True, exist_ok=True)
    for doc, texto in docs.items():
        (fdir / doc).write_text(texto, encoding="utf-8")
    if aprobar:
        data = h.cargar_aprobaciones(fdir)
        for doc in aprobar:
            data[doc] = {"sha256": h.sha256(fdir / doc), "fecha": "2026-01-01T00:00:00"}
        h.guardar_aprobaciones(fdir, data)
    if activa:
        (raiz / "specs" / ".activa").write_text(nombre)
    return fdir


def correr(script, *args, raiz, stdin=None, env_extra=None):
    """Ejecuta un script del harness (en .claude/scripts o .claude/hooks)."""
    ruta = SCRIPTS / script if (SCRIPTS / script).exists() else HOOKS / script
    env = dict(os.environ, CLAUDE_PROJECT_DIR=str(raiz), **(env_extra or {}))
    cmd = (["bash", str(ruta)] if ruta.suffix == ".sh" else [sys.executable, str(ruta)]) + list(args)
    return subprocess.run(cmd, input=stdin, capture_output=True, text=True, cwd=raiz, env=env)


def hook(script, raiz, tool_name, tool_input):
    payload = json.dumps({"tool_name": tool_name, "tool_input": tool_input,
                          "cwd": str(raiz), "hook_event_name": "PreToolUse"})
    return correr(script, raiz=raiz, stdin=payload)


REQ_OK = """---
feature: 001-x
estado: borrador
---
# Requerimiento

## Historias de usuario

### HU-01: Dividir cuenta
Como mesero quiero dividir la cuenta.

- **RF-01**: Cuando el mesero elige dividir en N, el sistema deberá generar N subcuentas.
- **RF-02**: Si N es menor que 2, el sistema deberá rechazar la división.

## Preguntas abiertas

- Ninguna
"""

DIS_OK = """# Diseño

## Componentes

- Divisor de cuentas. Cubre: RF-01, RF-02
"""

TAR_OK = """# Tareas

### T-01 [RF-01]: Generar subcuentas
### T-02 [RF-02]: Validar N
"""

CONFIG_BLOQUE = """
```harness-config
{"stack": "python", "rutas_codigo": ["apps/"], "rutas_libres": ["README.md"],
 "comando_test": "python3 -c \\"import sys; sys.exit(0)\\""}
```
"""


def constitucion_aprobada(raiz, rutas_codigo=("apps/",), comando_test=None):
    """Crea 000-constitucion aprobada y la config correspondiente."""
    crear_feature(raiz, "000-constitucion",
                  {"requerimiento.md": REQ_OK, "diseno.md": DIS_OK + CONFIG_BLOQUE},
                  aprobar=["requerimiento.md", "diseno.md"], activa=False)
    cfg = {"constitucion": "aprobada", "stack": "python", "rutas_codigo": list(rutas_codigo),
           "rutas_libres": ["README.md"],
           "comando_test": comando_test or 'python3 -c "import sys; sys.exit(0)"'}
    (raiz / "harness.config.json").write_text(json.dumps(cfg))
