import pytest

import harness_lib as h
from conftest import crear_feature, REQ_OK, DIS_OK, TAR_OK, CONFIG_BLOQUE

REQ_2HU = REQ_OK.replace("## Preguntas abiertas", """### HU-02: Cobrar
- **RF-03**: Cuando se cobra, el sistema deberá cerrar la subcuenta.

## Preguntas abiertas""")


def test_parsear_requerimiento():
    assert h.parsear_requerimiento(REQ_2HU) == {"HU-01": ["RF-01", "RF-02"], "HU-02": ["RF-03"]}


def test_parseo_crlf_bom(tmp_path):
    p = tmp_path / "r.md"
    p.write_bytes(("﻿" + REQ_2HU).replace("\n", "\r\n").encode("utf-8"))
    assert h.parsear_requerimiento(h.leer(p)) == h.parsear_requerimiento(REQ_2HU)


def test_preguntas_abiertas_ninguna():
    assert h.preguntas_abiertas(REQ_OK) == ""


def test_preguntas_abiertas_con_contenido():
    t = REQ_OK.replace("- Ninguna", "<!-- guía -->\n- ¿Se permite propina?")
    assert "propina" in h.preguntas_abiertas(t)


def test_parsear_tareas():
    assert h.parsear_tareas("### T-01 [RF-01, RF-02]: a\n### T-02 [RF-03]: b\n") == {
        "T-01": ["RF-01", "RF-02"], "T-02": ["RF-03"]}


def test_validar_ok(proyecto):
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK})
    assert h.validar_req_diseno(f) == []
    assert h.validar_tareas(f) == []


def test_validar_rf_sin_diseno(proyecto):
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK.replace(", RF-02", "")})
    errores = h.validar_req_diseno(f)
    assert any("RF-02" in e for e in errores)


def test_validar_preguntas_y_placeholder(proyecto):
    req = REQ_OK.replace("- Ninguna", "- ¿Propina?") + "{{proposito}}"
    f = crear_feature(proyecto, "001-x", {h.REQ: req, h.DIS: DIS_OK})
    errores = " ".join(h.validar_req_diseno(f))
    assert "Preguntas abiertas" in errores and "{{" in errores


def test_validar_rf_duplicado_y_hu_sin_rf(proyecto):
    req = REQ_OK.replace("**RF-02**", "**RF-01**").replace("## Preguntas abiertas",
                                                           "### HU-02: Vacía\n\n## Preguntas abiertas")
    f = crear_feature(proyecto, "001-x", {h.REQ: req, h.DIS: DIS_OK})
    errores = " ".join(h.validar_req_diseno(f))
    assert "RF-01" in errores and "HU-02" in errores


def test_validar_constitucion_exige_config(proyecto):
    f = crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_OK})
    assert any("harness-config" in e for e in h.validar_req_diseno(f))
    (f / h.DIS).write_text(DIS_OK + CONFIG_BLOQUE)
    assert h.validar_req_diseno(f) == []


def test_validar_tareas_rf_sin_tarea(proyecto):
    req = REQ_2HU
    f = crear_feature(proyecto, "001-x", {h.REQ: req, h.DIS: DIS_OK, h.TAR: TAR_OK + "### T-03 [RF-09]: x\n"})
    errores = " ".join(h.validar_tareas(f))
    assert "RF-03" in errores and "RF-09" in errores


def test_extraer_config_falta_clave():
    with pytest.raises(h.HarnessError):
        h.extraer_config('```harness-config\n{"stack": "x"}\n```')
    with pytest.raises(h.HarnessError):
        h.extraer_config("sin bloque")
    cfg = h.extraer_config(CONFIG_BLOQUE)
    assert cfg["rutas_codigo"] == ["apps/"]


def _avance(f, tareas=(), aceptadas=()):
    h.guardar_progreso(f, {"tareas": {t: {"estado": "hecha"} for t in tareas},
                           "aceptadas": {u: {} for u in aceptadas}})


def test_estado_hus(proyecto):
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK})
    assert h.estado_hus(f) == {"HU-01": "pendiente"}
    _avance(f, ["T-01"])
    assert h.estado_hus(f) == {"HU-01": "en-curso"}
    _avance(f, ["T-01", "T-02"])
    assert h.estado_hus(f) == {"HU-01": "implementada"}
    _avance(f, ["T-01", "T-02"], ["HU-01"])
    assert h.estado_hus(f) == {"HU-01": "aceptada"}


def test_fase(proyecto):
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK})
    assert h.fase(f) == "analisis"
    crear_feature(proyecto, "001-x", {}, aprobar=[h.REQ, h.DIS])
    assert h.fase(f) == "diseno-aprobado"
    crear_feature(proyecto, "001-x", {}, aprobar=[h.TAR])
    assert h.fase(f) == "plan-aprobado"
    _avance(f, ["T-01"])
    assert h.fase(f) == "implementando"
    _avance(f, ["T-01", "T-02"], ["HU-01"])
    assert h.fase(f) == "cerrada"


def test_fase_constitucion(proyecto):
    f = crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_OK},
                      aprobar=[h.REQ, h.DIS])
    assert h.fase(f) == "cerrada"
