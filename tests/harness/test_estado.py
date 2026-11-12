import harness_lib as h
from conftest import correr, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK, TAR_OK


def test_breve_sin_activa(proyecto):
    r = correr("estado.py", "--breve", raiz=proyecto)
    assert r.returncode == 0
    assert r.stdout.strip() == "Sin feature activa — usa /analizar"


def test_breve_con_activa(proyecto):
    constitucion_aprobada(proyecto)
    crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK})
    r = correr("estado.py", "--breve", raiz=proyecto)
    assert r.stdout.strip() == "Feature activa: 001-x (fase: analisis)"


def test_matriz_y_huecos(proyecto):
    constitucion_aprobada(proyecto)
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK,
                                          h.TAR: "### T-01 [RF-01]: a\n"})
    h.guardar_progreso(f, {"tareas": {"T-01": {"estado": "hecha"}}, "aceptadas": {}})
    out = correr("estado.py", raiz=proyecto).stdout
    assert "001-x — fase: analisis" in out
    assert "HU-01 [en-curso]" in out
    assert "RF-01 → T-01 ✔" in out
    huecos = out.split("Huecos:")[1]
    assert "RF-02" in huecos and "sin tarea" in huecos
    assert "001/RF-01" in huecos  # sin test


def test_hash_alterado_se_reporta(proyecto):
    constitucion_aprobada(proyecto)
    f = crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                      aprobar=[h.REQ, h.DIS, h.TAR])
    (f / h.DIS).write_text(DIS_OK + "x")
    out = correr("estado.py", "001", raiz=proyecto).stdout
    assert "diseno.md: cambió desde la aprobación" in out


def test_todas(proyecto):
    constitucion_aprobada(proyecto)
    crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK})
    out = correr("estado.py", "todas", raiz=proyecto).stdout
    lineas = out.strip().splitlines()
    assert any(l.split()[:2] == ["000-constitucion", "cerrada"] for l in lineas)
    assert any(l.split()[:3] == ["001-x", "analisis", "0/1"] for l in lineas)


def test_error_sin_traceback(proyecto):
    (proyecto / "specs" / ".activa").write_text("009-nada")
    r = correr("estado.py", raiz=proyecto)
    assert r.returncode == 1 and "Traceback" not in r.stderr + r.stdout
