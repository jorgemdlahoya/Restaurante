import os

import harness_lib as h
from conftest import hook, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK, TAR_OK


def w(raiz, ruta, tool="Write"):
    clave = "notebook_path" if tool == "NotebookEdit" else "file_path"
    return hook("gate-spec.py", raiz, tool, {clave: str(ruta), "content": "x"})


def feature_lista(raiz, aprobar=(h.REQ, h.DIS, h.TAR)):
    constitucion_aprobada(raiz)
    return crear_feature(raiz, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                         aprobar=list(aprobar))


def test_specs_permitido(proyecto):
    assert w(proyecto, proyecto / "specs" / "001-x" / "requerimiento.md").returncode == 0


def test_docs_permitido(proyecto):
    assert w(proyecto, "docs/notas.md").returncode == 0


def test_protegidos(proyecto):
    for ruta in ["specs/001-x/.aprobaciones.json", "specs/001-x/progreso.json", "specs/.activa",
                 ".claude/settings.json", ".claude/hooks/gate-spec.py", "harness.config.json",
                 "CLAUDE.md"]:
        r = w(proyecto, proyecto / ruta)
        assert r.returncode == 2, ruta
        assert r.stderr.strip(), ruta


def test_codigo_sin_constitucion(proyecto):
    r = w(proyecto, proyecto / "apps" / "a.ts")
    assert r.returncode == 2
    assert "constitución" in r.stderr


def test_codigo_sin_aprobar(proyecto):
    feature_lista(proyecto, aprobar=(h.REQ, h.DIS))
    r = w(proyecto, proyecto / "apps" / "a.ts")
    assert r.returncode == 2
    assert "/aprobar 001" in r.stderr


def test_codigo_sin_feature_activa(proyecto):
    constitucion_aprobada(proyecto)
    r = w(proyecto, proyecto / "apps" / "a.ts")
    assert r.returncode == 2 and "/analizar" in r.stderr


def test_codigo_aprobado(proyecto):
    f = feature_lista(proyecto)
    assert w(proyecto, proyecto / "apps" / "web" / "a.ts", "Edit").returncode == 0
    (f / h.DIS).write_text(DIS_OK + "cambio")
    r = w(proyecto, proyecto / "apps" / "a.ts", "MultiEdit")
    assert r.returncode == 2
    assert "cambió" in r.stderr


def test_rutas_libres(proyecto):
    assert w(proyecto, proyecto / "README.md").returncode == 2
    constitucion_aprobada(proyecto)
    assert w(proyecto, proyecto / "README.md").returncode == 0
    assert w(proyecto, proyecto / "otra" / "cosa.txt").returncode == 2


def test_escape_raiz(proyecto, tmp_path):
    feature_lista(proyecto)
    assert w(proyecto, "../fuera.txt").returncode == 2
    assert w(proyecto, "/etc/passwd").returncode == 2
    (proyecto / "apps").mkdir()
    os.symlink(proyecto / "apps", proyecto / "specs" / "link")
    crear_feature(proyecto, "001-x", {}, aprobar=[])  # sigue aprobada
    (proyecto / "specs" / "001-x" / h.DIS).write_text(DIS_OK + "cambio")
    r = w(proyecto, proyecto / "specs" / "link" / "a.ts")
    assert r.returncode == 2


def test_activa_rota(proyecto):
    constitucion_aprobada(proyecto)
    (proyecto / "specs" / ".activa").write_text("009-nada")
    r = w(proyecto, proyecto / "apps" / "a.ts")
    assert r.returncode == 2
    assert "009-nada" in r.stderr and "Traceback" not in r.stderr


def test_config_corrupta(proyecto):
    feature_lista(proyecto)
    (proyecto / "harness.config.json").write_text("{")
    r = w(proyecto, proyecto / "apps" / "a.ts")
    assert r.returncode == 2


def test_notebook_path(proyecto):
    feature_lista(proyecto, aprobar=(h.REQ,))
    assert w(proyecto, proyecto / "apps" / "n.ipynb", "NotebookEdit").returncode == 2


def test_json_invalido(proyecto):
    from conftest import correr
    r = correr("gate-spec.py", raiz=proyecto, stdin="no json")
    assert r.returncode == 2 and "bloqueado" in r.stderr
