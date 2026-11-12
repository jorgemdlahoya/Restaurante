import json

import harness_lib as h
from conftest import correr, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK, TAR_OK, CONFIG_BLOQUE


def test_aprueba_req_diseno_y_luego_tareas(proyecto):
    constitucion_aprobada(proyecto)
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK})
    r = correr("aprobar.py", raiz=proyecto)
    assert r.returncode == 0, r.stderr
    assert h.doc_aprobado(f, h.REQ) and h.doc_aprobado(f, h.DIS) and not h.doc_aprobado(f, h.TAR)
    r = correr("aprobar.py", "001", raiz=proyecto)
    assert r.returncode == 0, r.stderr
    assert h.doc_aprobado(f, h.TAR)


def test_rechaza_preguntas_abiertas(proyecto):
    constitucion_aprobada(proyecto)
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK.replace("- Ninguna", "- ¿Propina?"),
                                          h.DIS: DIS_OK})
    r = correr("aprobar.py", raiz=proyecto)
    assert r.returncode == 1
    assert "Preguntas abiertas" in r.stdout + r.stderr
    assert h.cargar_aprobaciones(f) == {}


def test_rechaza_tareas_invalidas(proyecto):
    constitucion_aprobada(proyecto)
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK,
                                          h.TAR: "### T-01 [RF-01]: a\n"}, aprobar=[h.REQ, h.DIS])
    r = correr("aprobar.py", raiz=proyecto)
    assert r.returncode == 1 and "RF-02" in r.stdout + r.stderr
    assert not h.doc_aprobado(f, h.TAR)


def test_reaprobar_invalida_tareas(proyecto):
    constitucion_aprobada(proyecto)
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                      aprobar=[h.REQ, h.DIS, h.TAR])
    (f / h.REQ).write_text(REQ_OK.replace("dividir la cuenta.", "dividir la cuenta rápido."))
    r = correr("aprobar.py", raiz=proyecto)
    assert r.returncode == 0, r.stderr
    assert h.doc_aprobado(f, h.REQ)
    assert not h.doc_aprobado(f, h.TAR)
    assert "tareas.md" in r.stdout


def test_constitucion_genera_config(proyecto):
    crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_OK + CONFIG_BLOQUE})
    r = correr("aprobar.py", "000", raiz=proyecto)
    assert r.returncode == 0, r.stderr
    cfg = json.loads((proyecto / "harness.config.json").read_text())
    assert cfg["constitucion"] == "aprobada" and cfg["rutas_codigo"] == ["apps/"]


def test_generar_config_sin_aprobar_falla(proyecto):
    crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_OK + CONFIG_BLOQUE})
    r = correr("generar-config.py", raiz=proyecto)
    assert r.returncode == 1


def test_nada_que_aprobar(proyecto):
    constitucion_aprobada(proyecto)
    crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                  aprobar=[h.REQ, h.DIS, h.TAR])
    r = correr("aprobar.py", raiz=proyecto)
    assert r.returncode == 0 and "Nada que aprobar" in r.stdout


def test_sin_feature(proyecto):
    r = correr("aprobar.py", raiz=proyecto)
    assert r.returncode == 1 and "Traceback" not in r.stderr
