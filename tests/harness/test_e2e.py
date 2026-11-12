"""Recorre el flujo SDD completo con los scripts y hooks reales sobre un repo temporal."""
import json
import shutil
import subprocess

import harness_lib as h
from conftest import REPO, correr, hook, REQ_OK, DIS_OK, TAR_OK

CONFIG = """
```harness-config
{"stack": "python", "rutas_codigo": ["app/"], "rutas_libres": ["README.md"],
 "comando_test": "python3 -m unittest discover -s app -q"}
```
"""

TEST_APP = '''import unittest


class T(unittest.TestCase):
    def test_rf01(self):
        """001/RF-01: genera N subcuentas"""
        self.assertTrue(True)

    def test_rf02(self):
        """001/RF-02: rechaza N < 2"""
        self.assertTrue(True)
'''


def escribir(raiz, ruta):
    return hook("gate-spec.py", raiz, "Write", {"file_path": str(raiz / ruta), "content": "x"})


def test_flujo_completo(tmp_path, monkeypatch):
    raiz = tmp_path / "rest"
    raiz.mkdir()
    shutil.copytree(REPO / ".claude", raiz / ".claude")
    shutil.copytree(REPO / "specs" / "_plantillas", raiz / "specs" / "_plantillas")
    (raiz / "harness.config.json").write_text('{"constitucion": "pendiente"}')
    monkeypatch.setenv("CLAUDE_PROJECT_DIR", str(raiz))
    subprocess.run(["git", "init", "-q"], cwd=raiz, check=True)

    # Constitución
    assert correr("nueva-feature.py", "constitución del proyecto", raiz=raiz).returncode == 0
    c = raiz / "specs" / "000-constitucion"
    (c / h.REQ).write_text(REQ_OK)
    (c / h.DIS).write_text(DIS_OK + CONFIG)
    assert escribir(raiz, "app/x.py").returncode == 2
    assert hook("gate-bash.py", raiz, "Bash",
                {"command": "python3 .claude/scripts/aprobar.py 000"}).returncode == 2
    r = correr("aprobar.py", "000", raiz=raiz)
    assert r.returncode == 0, r.stdout
    assert json.loads((raiz / "harness.config.json").read_text())["rutas_codigo"] == ["app/"]

    # Feature 001
    r = correr("nueva-feature.py", "Dividir la cuenta", raiz=raiz)
    assert r.returncode == 0, r.stderr
    f = raiz / "specs" / "001-dividir-la-cuenta"
    (f / h.REQ).write_text(REQ_OK)
    (f / h.DIS).write_text(DIS_OK)
    (f / h.TAR).write_text(TAR_OK)
    assert escribir(raiz, "app/x.py").returncode == 2
    assert correr("aprobar.py", raiz=raiz).returncode == 0
    assert escribir(raiz, "app/x.py").returncode == 2          # falta tareas.md
    assert correr("aprobar.py", raiz=raiz).returncode == 0
    assert escribir(raiz, "app/x.py").returncode == 0          # código desbloqueado

    (raiz / "app").mkdir()
    (raiz / "app" / "test_x.py").write_text(TEST_APP)
    for t in ("T-01", "T-02"):
        r = correr("completar-tarea.py", t, raiz=raiz)
        assert r.returncode == 0, r.stdout + r.stderr
    assert "HU-01" in r.stdout
    assert correr("aceptar.py", "HU-01", raiz=raiz).returncode == 0
    out = correr("estado.py", raiz=raiz).stdout
    assert "fase: cerrada" in out and "(ninguno)" in out
