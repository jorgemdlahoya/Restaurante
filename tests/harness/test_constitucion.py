"""constitucion.md: principios innegociables de la constitución."""
import harness_lib as h
from conftest import correr, crear_feature, REQ_OK, DIS_OK, CONFIG_BLOQUE, CONS_OK

DIS_000 = DIS_OK + CONFIG_BLOQUE


def test_constitucion_crea_tres_docs(proyecto):
    assert correr("nueva-feature.py", "x", raiz=proyecto).returncode == 0
    f = proyecto / "specs" / "000-constitucion"
    assert {h.REQ, h.DIS, h.CONS} <= {p.name for p in f.iterdir()}
    texto = (f / h.CONS).read_text()
    assert "### Artículo 1 — Seguridad" in texto
    assert "Regla rectora" in texto and "{{CORS_RULE}}" in texto


def test_aprobar_000_exige_constitucion_rellena(proyecto):
    crear_feature(proyecto, "000-constitucion",
                  {h.REQ: REQ_OK, h.DIS: DIS_000, h.CONS: CONS_OK + "{{CORS_RULE}}"})
    r = correr("aprobar.py", "000", raiz=proyecto)
    assert r.returncode == 1 and "constitucion.md" in r.stdout


def test_no_se_puede_quitar_el_nucleo(proyecto):
    sin_art3 = CONS_OK.replace("### Artículo 3", "### Otro")
    crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_000, h.CONS: sin_art3})
    r = correr("aprobar.py", "000", raiz=proyecto)
    assert r.returncode == 1 and "Artículo 3" in r.stdout


def test_aprobar_000_aprueba_los_tres(proyecto):
    f = crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_000, h.CONS: CONS_OK})
    r = correr("aprobar.py", "000", raiz=proyecto)
    assert r.returncode == 0, r.stdout
    assert all(h.doc_aprobado(f, d) for d in (h.REQ, h.DIS, h.CONS))
    (f / h.CONS).write_text(CONS_OK + "\nCambio.\n")
    assert h.constitucion_aprobada(proyecto) is False


def test_constitucion_antigua_sigue_aprobada(proyecto):
    crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_000},
                  aprobar=[h.REQ, h.DIS])
    assert h.constitucion_aprobada(proyecto) is True


def test_borrar_constitucion_aprobada_bloquea(proyecto):
    f = crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_000, h.CONS: CONS_OK},
                      aprobar=[h.REQ, h.DIS, "constitucion.md"])
    assert h.constitucion_aprobada(proyecto) is True
    (f / h.CONS).unlink()
    assert h.constitucion_aprobada(proyecto) is False
