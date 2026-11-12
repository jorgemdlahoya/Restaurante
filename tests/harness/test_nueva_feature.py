import harness_lib as h
from conftest import correr, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK


def test_primera_es_constitucion(proyecto):
    r = correr("nueva-feature.py", "lo que sea", raiz=proyecto)
    assert r.returncode == 0, r.stderr
    f = proyecto / "specs" / "000-constitucion"
    assert sorted(p.name for p in f.iterdir()) == [h.DIS, h.REQ, h.TAR]
    assert (proyecto / "specs" / ".activa").read_text().strip() == "000-constitucion"
    assert "feature: 000-constitucion" in (f / h.REQ).read_text()
    assert "constitución" in r.stdout


def test_reutiliza_constitucion_activa(proyecto):
    correr("nueva-feature.py", "x", raiz=proyecto)
    r = correr("nueva-feature.py", "otra cosa", raiz=proyecto)
    assert r.returncode == 0
    assert "000-constitucion" in r.stdout


def test_bloquea_sin_constitucion(proyecto):
    correr("nueva-feature.py", "x", raiz=proyecto)
    (proyecto / "specs" / ".activa").unlink()
    r = correr("nueva-feature.py", "pedidos", raiz=proyecto)
    assert r.returncode == 1
    assert "constitución" in r.stderr


def test_slug_y_numero(proyecto):
    constitucion_aprobada(proyecto)
    crear_feature(proyecto, "001-x", {}, activa=False)
    r = correr("nueva-feature.py", "Dividir la cuenta en partes ¡iguales!", raiz=proyecto)
    assert r.returncode == 0, r.stderr
    assert (proyecto / "specs" / "002-dividir-la-cuenta-en-partes-iguales").is_dir()
    assert "002-dividir-la-cuenta-en-partes-iguales" in r.stdout


def test_slug_largo_recortado(proyecto):
    constitucion_aprobada(proyecto)
    r = correr("nueva-feature.py", "a" * 30 + " " + "b" * 30, raiz=proyecto)
    nombre = (proyecto / "specs" / ".activa").read_text().strip()
    assert len(nombre) <= 44 and not nombre.endswith("-")


def test_activa_no_cerrada(proyecto):
    constitucion_aprobada(proyecto)
    crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK})
    r = correr("nueva-feature.py", "pedidos", raiz=proyecto)
    assert r.returncode == 3
    assert "001-x" in r.stderr
    r = correr("nueva-feature.py", "pedidos", "--forzar", raiz=proyecto)
    assert r.returncode == 0
    assert (proyecto / "specs" / ".activa").read_text().strip() == "002-pedidos"


def test_texto_vacio(proyecto):
    constitucion_aprobada(proyecto)
    r = correr("nueva-feature.py", "¡¡!!", raiz=proyecto)
    assert r.returncode == 1
