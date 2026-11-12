import pytest

import harness_lib as h
from conftest import crear_feature, REQ_OK, DIS_OK


def test_doc_aprobado_por_hash(proyecto):
    f = crear_feature(proyecto, "001-x", {h.DIS: DIS_OK}, aprobar=[h.DIS])
    assert h.doc_aprobado(f, h.DIS) is True
    (f / h.DIS).write_text(DIS_OK + "x")
    assert h.doc_aprobado(f, h.DIS) is False


def test_doc_no_registrado_no_aprobado(proyecto):
    f = crear_feature(proyecto, "001-x", {h.DIS: DIS_OK})
    assert h.doc_aprobado(f, h.DIS) is False


def test_docs_requeridos_constitucion(proyecto):
    c = crear_feature(proyecto, "000-constitucion", {})
    f = crear_feature(proyecto, "001-x", {})
    assert h.docs_requeridos(c) == [h.REQ, h.DIS]
    assert h.docs_requeridos(f) == [h.REQ, h.DIS, h.TAR]


def test_constitucion_aprobada(proyecto):
    assert h.constitucion_aprobada(proyecto) is False
    crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_OK},
                  aprobar=[h.REQ, h.DIS])
    assert h.constitucion_aprobada(proyecto) is True


def test_activa_inexistente(proyecto):
    (proyecto / "specs" / ".activa").write_text("009-nada")
    with pytest.raises(h.HarnessError):
        h.feature_activa(proyecto)


def test_sin_activa(proyecto):
    assert h.feature_activa(proyecto) is None


def test_config_corrupta(proyecto):
    (proyecto / "harness.config.json").write_text("{")
    with pytest.raises(h.HarnessError):
        h.cargar_config(proyecto)


def test_buscar_feature_por_numero(proyecto):
    f = crear_feature(proyecto, "003-algo", {}, activa=False)
    assert h.buscar_feature(proyecto, "003") == f
    with pytest.raises(h.HarnessError):
        h.buscar_feature(proyecto, "004")


def test_raiz_desde_env(proyecto):
    assert h.raiz() == proyecto
