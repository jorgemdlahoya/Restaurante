"""Preset con Playwright: dos suites de punta a punta."""
import json

import pytest

import harness_lib as h
from conftest import correr

BASE = {"stack": "React + TS + Vite + Vitest + Playwright; backend: x",
        "frontend": "react-ts-vite-vitest-playwright",
        "rutas_codigo": ["apps/web/", "apps/api/"],
        "rutas_libres": ["README.md"],
        "comando_test": "(cd apps/web && npx vitest run --passWithNoTests && "
                        "npx playwright test --pass-with-no-tests)",
        "comando_e2e_stack": "cd apps/web && npx playwright test --config playwright.stack.config.ts"}


def bloque(**cambios):
    cfg = {k: v for k, v in {**BASE, **cambios}.items() if v is not None}
    return "```harness-config\n" + json.dumps(cfg) + "\n```\n"


def _error(**cambios):
    with pytest.raises(h.HarnessError) as e:
        h.extraer_config(bloque(**cambios))
    assert "fijo del harness" in str(e.value)
    return str(e.value)


def test_bloque_playwright_valido():
    assert "playwright" in h.extraer_config(bloque())["comando_e2e_stack"]


def test_frontend_antiguo_rechazado():
    assert "frontend" in _error(frontend="react-ts-vite-vitest")


def test_comando_test_sin_playwright():
    assert "playwright" in _error(comando_test="npm --prefix apps/web exec vitest run")


def test_falta_comando_e2e_stack():
    assert "comando_e2e_stack" in _error(comando_e2e_stack=None)


def test_e2e_stack_sin_playwright():
    assert "comando_e2e_stack" in _error(comando_e2e_stack="npm run e2e")


def test_plantilla_constitucion_playwright(proyecto):
    assert correr("nueva-feature.py", "x", raiz=proyecto).returncode == 0
    texto = (proyecto / "specs" / "000-constitucion" / h.DIS).read_text()
    for frag in ("Playwright", "apps/web/e2e/", "apps/web/e2e-stack/", "E2E_BASE_URL"):
        assert frag in texto, frag
    cfg = h.extraer_config(texto)
    assert cfg["comando_test"] == BASE["comando_test"]
    assert cfg["comando_e2e_stack"] == BASE["comando_e2e_stack"]


def test_plantilla_vitest_excluye_suites_playwright(proyecto):
    correr("nueva-feature.py", "x", raiz=proyecto)
    texto = (proyecto / "specs" / "000-constitucion" / h.DIS).read_text()
    assert "excluye `e2e/` y `e2e-stack/`" in texto


def test_ejecutar_comando_no_se_cuelga_con_nietos(proyecto):
    import time
    t0 = time.monotonic()
    ok, motivo = h.ejecutar_comando("setsid sleep 20 & sleep 30", proyecto, 1)
    assert ok is False and "tiempo" in motivo
    assert time.monotonic() - t0 < 10
