"""La suite completa de Playwright bloquea el cierre de la feature (última /aceptar)."""
import json

import harness_lib as h
from conftest import correr, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK

OK = 'python3 -c "import sys; sys.exit(0)"'
KO = 'python3 -c "import sys; sys.exit(1)"'

REQ_2HU = REQ_OK.replace("## Preguntas abiertas", """### HU-02: Cobrar
- **RF-03**: Cuando se cobra, el sistema deberá cerrar la subcuenta.

## Preguntas abiertas""")
DIS_2HU = DIS_OK + "- Caja. Cubre: RF-03\n"
TAR_2HU = "### T-01 [RF-01]: a\n### T-02 [RF-02]: b\n### T-03 [RF-03]: c\n"
TAR_1HU = "### T-01 [RF-01]: a\n### T-02 [RF-02]: b\n"


def preparar(raiz, e2e=OK, dos_hu=True):
    constitucion_aprobada(raiz)
    cfg = json.loads((raiz / "harness.config.json").read_text())
    if e2e is None:
        cfg.pop("comando_e2e_stack")
    else:
        cfg["comando_e2e_stack"] = e2e
    (raiz / "harness.config.json").write_text(json.dumps(cfg))
    docs = ({h.REQ: REQ_2HU, h.DIS: DIS_2HU, h.TAR: TAR_2HU} if dos_hu
            else {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_1HU})
    f = crear_feature(raiz, "001-x", docs, aprobar=[h.REQ, h.DIS, h.TAR])
    tareas = ["T-01", "T-02", "T-03"] if dos_hu else ["T-01", "T-02"]
    h.guardar_progreso(f, {"tareas": {t: {"estado": "hecha"} for t in tareas}, "aceptadas": {}})
    return f


def aceptar(raiz, hu, **env):
    r = correr("aceptar.py", hu, raiz=raiz, env_extra=env)
    return r, r.stdout + r.stderr


def test_hu_no_final_no_ejecuta_suite(proyecto):
    f = preparar(proyecto, e2e=KO)
    r, out = aceptar(proyecto, "HU-01")
    assert r.returncode == 0, out
    assert "HU-01" in h.cargar_progreso(f)["aceptadas"]


def test_hu_final_verde_cierra(proyecto):
    f = preparar(proyecto, e2e=OK)
    assert aceptar(proyecto, "HU-01")[0].returncode == 0
    r, out = aceptar(proyecto, "HU-02")
    assert r.returncode == 0, out
    assert h.fase(f) == "cerrada"


def test_hu_final_rojo_no_cierra(proyecto):
    f = preparar(proyecto, e2e=KO)
    aceptar(proyecto, "HU-01")
    r, out = aceptar(proyecto, "HU-02")
    assert r.returncode == 1 and "docker compose up" in out
    assert "HU-02" not in h.cargar_progreso(f)["aceptadas"]


def test_hu_unica_ejecuta_suite(proyecto):
    f = preparar(proyecto, e2e=KO, dos_hu=False)
    r, out = aceptar(proyecto, "HU-01")
    assert r.returncode == 1
    assert "HU-01" not in h.cargar_progreso(f)["aceptadas"]


def test_config_sin_e2e_stack(proyecto):
    f = preparar(proyecto, e2e=None, dos_hu=False)
    r, out = aceptar(proyecto, "HU-01")
    assert r.returncode == 1 and "comando_e2e_stack" in out
    assert "HU-01" not in h.cargar_progreso(f)["aceptadas"]


def test_timeout_e2e(proyecto):
    f = preparar(proyecto, e2e="sleep 5", dos_hu=False)
    r, out = aceptar(proyecto, "HU-01", HARNESS_E2E_TIMEOUT="1")
    assert r.returncode == 1 and "tiempo" in out
    assert "HU-01" not in h.cargar_progreso(f)["aceptadas"]


def test_ejecutar_comando(proyecto):
    assert h.ejecutar_comando(OK, proyecto, 30) == (True, "")
    ok, motivo = h.ejecutar_comando("echo fallo; exit 3", proyecto, 30)
    assert ok is False and "3" in motivo and "fallo" in motivo
