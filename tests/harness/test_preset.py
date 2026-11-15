"""Preset de frontend React fijo del harness."""
import json

import pytest

import harness_lib as h
from conftest import correr, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK, TAR_OK

BASE = {"stack": "React + TS + Vite + Vitest; backend: python",
        "frontend": "react-ts-vite-vitest",
        "rutas_codigo": ["apps/web/", "apps/api/"],
        "rutas_libres": ["README.md"],
        "comando_test": "npm --prefix apps/web exec vitest run"}


def bloque(**cambios):
    cfg = {**BASE, **cambios}
    cfg = {k: v for k, v in cfg.items() if v is not None}
    return "```harness-config\n" + json.dumps(cfg) + "\n```\n"


def test_bloque_valido():
    assert h.extraer_config(bloque())["frontend"] == "react-ts-vite-vitest"


@pytest.mark.parametrize("cambios,texto", [
    ({"frontend": None}, "frontend"),
    ({"frontend": "vue-vite"}, "frontend"),
    ({"rutas_codigo": ["apps/"]}, "apps/web/"),
    ({"comando_test": "npm test"}, "vitest"),
])
def test_preset_incumplido(cambios, texto):
    with pytest.raises(h.HarnessError) as e:
        h.extraer_config(bloque(**cambios))
    assert texto in str(e.value) and "fijo del harness" in str(e.value)


@pytest.mark.parametrize("rutas", [["apps/web"], ["./apps/web/"]])
def test_ruta_web_variantes(rutas):
    assert h.extraer_config(bloque(rutas_codigo=rutas))


def test_config_antigua_sigue_valiendo_en_gate(proyecto):
    constitucion_aprobada(proyecto)
    cfg = json.loads((proyecto / "harness.config.json").read_text())
    cfg.pop("frontend", None)
    (proyecto / "harness.config.json").write_text(json.dumps(cfg))
    crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                  aprobar=[h.REQ, h.DIS, h.TAR])
    assert h.evaluar_escritura(proyecto, "apps/a.ts") is None


def test_aprobar_000_rechaza_otro_frontend(proyecto):
    crear_feature(proyecto, "000-constitucion",
                  {h.REQ: REQ_OK, h.DIS: DIS_OK + bloque(frontend="angular")})
    r = correr("aprobar.py", "000", raiz=proyecto)
    assert r.returncode == 1 and "frontend" in r.stdout
