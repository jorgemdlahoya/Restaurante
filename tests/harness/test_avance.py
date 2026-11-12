import harness_lib as h
from conftest import correr, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK, TAR_OK

OK = 'python3 -c "import sys; sys.exit(0)"'
KO = 'python3 -c "import sys; sys.exit(1)"'


def preparar(raiz, comando=OK, tests_rf=("001/RF-01", "001/RF-02"), aprobar=(h.REQ, h.DIS, h.TAR)):
    constitucion_aprobada(raiz, comando_test=comando)
    f = crear_feature(raiz, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                      aprobar=list(aprobar))
    (raiz / "apps").mkdir(exist_ok=True)
    (raiz / "apps" / "t.test.js").write_text(
        "\n".join(f'it("{rf}: algo", () => {{}})' for rf in tests_rf))
    return f


def test_completa_con_tests_verdes(proyecto):
    f = preparar(proyecto)
    r = correr("completar-tarea.py", "T-01", raiz=proyecto)
    assert r.returncode == 0, r.stdout + r.stderr
    assert h.cargar_progreso(f)["tareas"]["T-01"]["estado"] == "hecha"


def test_falta_test_del_rf(proyecto):
    f = preparar(proyecto, tests_rf=("001/RF-02", "002/RF-01"))
    r = correr("completar-tarea.py", "T-01", raiz=proyecto)
    assert r.returncode == 1
    assert "001/RF-01" in r.stderr
    assert "T-01" not in h.cargar_progreso(f)["tareas"]


def test_ignora_node_modules(proyecto):
    preparar(proyecto, tests_rf=("001/RF-02",))
    (proyecto / "apps" / "node_modules").mkdir()
    (proyecto / "apps" / "node_modules" / "x.js").write_text("001/RF-01")
    assert correr("completar-tarea.py", "T-01", raiz=proyecto).returncode == 1


def test_tests_rojos(proyecto):
    f = preparar(proyecto, comando=KO)
    r = correr("completar-tarea.py", "T-01", raiz=proyecto)
    assert r.returncode == 1
    assert "T-01" not in h.cargar_progreso(f)["tareas"]


def test_comando_inexistente(proyecto):
    preparar(proyecto, comando="comando-que-no-existe-xyz")
    assert correr("completar-tarea.py", "T-01", raiz=proyecto).returncode == 1


def test_timeout(proyecto):
    preparar(proyecto, comando="sleep 5")
    r = correr("completar-tarea.py", "T-01", raiz=proyecto, env_extra={"HARNESS_TEST_TIMEOUT": "1"})
    assert r.returncode == 1 and "tiempo" in r.stderr


def test_tarea_inexistente(proyecto):
    preparar(proyecto)
    assert correr("completar-tarea.py", "T-09", raiz=proyecto).returncode == 1


def test_docs_no_aprobados(proyecto):
    preparar(proyecto, aprobar=(h.REQ, h.DIS))
    r = correr("completar-tarea.py", "T-01", raiz=proyecto)
    assert r.returncode == 1 and "tareas.md" in r.stderr


def test_aceptar_requiere_implementada(proyecto):
    f = preparar(proyecto)
    r = correr("aceptar.py", "HU-01", raiz=proyecto)
    assert r.returncode == 1 and "implementada" in r.stdout
    for t in ("T-01", "T-02"):
        assert correr("completar-tarea.py", t, raiz=proyecto).returncode == 0
    r = correr("aceptar.py", "HU-01", raiz=proyecto)
    assert r.returncode == 0, r.stdout
    assert "HU-01" in h.cargar_progreso(f)["aceptadas"]
    assert h.fase(f) == "cerrada"


def test_aceptar_hu_inexistente(proyecto):
    preparar(proyecto)
    assert correr("aceptar.py", "HU-07", raiz=proyecto).returncode == 1
