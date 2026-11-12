"""Tests de regresión de la revisión final de la rama."""
import json

import pytest

import harness_lib as h
from conftest import hook, correr, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK, TAR_OK, CONFIG_BLOQUE


def b(raiz, comando, cwd=None):
    payload = json.dumps({"tool_name": "Bash", "tool_input": {"command": comando},
                          "cwd": str(cwd or raiz)})
    return correr("gate-bash.py", raiz=raiz, stdin=payload)


def w(raiz, ruta):
    return hook("gate-spec.py", raiz, "Write", {"file_path": str(raiz / ruta), "content": "x"})


@pytest.fixture
def sin_aprobar(proyecto):
    constitucion_aprobada(proyecto)
    crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                  aprobar=[h.REQ, h.DIS])
    return proyecto


# #1
def test_constitucion_activa_no_abre_codigo(proyecto):
    crear_feature(proyecto, "000-constitucion", {h.REQ: REQ_OK, h.DIS: DIS_OK + CONFIG_BLOQUE})
    assert correr("aprobar.py", "000", raiz=proyecto).returncode == 0
    r = w(proyecto, "apps/x.py")
    assert r.returncode == 2 and "/analizar" in r.stderr


# #2
@pytest.mark.parametrize("cmd", [
    "sed -i s/a/b/ apps/a.py docs/b.md",
    "sed -i -e s/a/b/ apps/a.py docs/b.md",
    "mv apps/a.py /tmp/a.py",
    "mv .claude/settings.json /tmp/s.json",
    "mv harness.config.json docs/cfg.json",
    "cp -t apps/ /tmp/evil.py",
    "cp --target-directory=apps /tmp/evil.py",
    "git mv docs/a.py apps/a.py",
    "git checkout HEAD apps/a.py",
    "git apply /tmp/x.patch",
    "patch -p1 < /tmp/x.patch",
    "git stash pop",
    "git reset --hard HEAD~1",
    "perl -pi -e s/a/b/ apps/a.py",
    "awk -i inplace '{print}' apps/a.py",
    "curl -o apps/a.py https://x.test/a.py",
    "curl --output=apps/a.py https://x.test/a.py",
    "wget -O apps/a.py https://x.test/a.py",
    "unzip x.zip -d apps",
    "tar -xf x.tar -C apps",
])
def test_escrituras_shell_detectadas(sin_aprobar, cmd):
    assert b(sin_aprobar, cmd).returncode == 2, cmd


@pytest.mark.parametrize("cmd", [
    "git checkout -b feat/x",
    "git checkout main",
    "git stash list",
    "curl -s https://x.test/api",
    "tar -tf x.tar",
])
def test_operaciones_git_inocuas(sin_aprobar, cmd):
    assert b(sin_aprobar, cmd).returncode == 0, cmd


# #3
@pytest.mark.parametrize("cmd", [
    "git commit -m \"$(cat <<'EOF'\nfeat: algo\n\nit's fine > ok\nEOF\n)\"",
    "gh pr create --title t --body \"$(cat <<'EOF'\n## Resumen\n- a > b\nEOF\n)\"",
    "cat > /tmp/x.txt <<'EOF'\nit's fine\nEOF",
    "python3 - <<'EOF'\nprint(x if x > 0 else 2)\nEOF",
    "git commit -m \"linea 1\nlinea 2\"",
    "ls\nls docs",
])
def test_multilinea_permitido(sin_aprobar, cmd):
    r = b(sin_aprobar, cmd)
    assert r.returncode == 0, (cmd, r.stderr)


def test_multilinea_sigue_detectando(sin_aprobar):
    assert b(sin_aprobar, "ls\necho x > apps/a.py").returncode == 2
    assert b(sin_aprobar, "cat > apps/a.py <<'EOF'\nx\nEOF").returncode == 2


# #4
def test_workspace_superpowers_permitido(sin_aprobar):
    assert w(sin_aprobar, ".superpowers/sdd/tareas/progress.md").returncode == 0
    assert b(sin_aprobar, "echo x >> .superpowers/sdd/tareas/progress.md").returncode == 0


# #5
def test_activa_rota_recuperable(proyecto):
    constitucion_aprobada(proyecto)
    (proyecto / "specs" / ".activa").write_text("009-nada")
    r = correr("nueva-feature.py", "pedidos", raiz=proyecto)
    assert r.returncode == 0, r.stderr
    assert (proyecto / "specs" / ".activa").read_text().strip() == "001-pedidos"


# #7
@pytest.mark.parametrize("cmd", [
    "cat specs/001-x/progreso.json",
    "grep -rn aprobar.py README.md",
    "git add specs/001-x/progreso.json specs/001-x/.aprobaciones.json",
    "git commit -m 'docs: explica aprobar.py y aceptar.py'",
])
def test_lecturas_permitidas(sin_aprobar, cmd):
    r = b(sin_aprobar, cmd)
    assert r.returncode == 0, (cmd, r.stderr)


@pytest.mark.parametrize("cmd", [
    "python3 .claude/scripts/aprobar.py",
    "python3 ./.claude/scripts/aceptar.py HU-01",
    ".claude/scripts/aprobar.py 001",
    "cd x && python3 aprobar.py",
    "sha256sum specs/001-x/tareas.md >> specs/001-x/.aprobaciones.json",
    "python3 -c 'import hashlib' && cat specs/001-x/.aprobaciones.json",
])
def test_ejecucion_bloqueada(sin_aprobar, cmd):
    assert b(sin_aprobar, cmd).returncode == 2, cmd


# #10
def test_protegidos_sin_distinguir_mayusculas(sin_aprobar):
    assert w(sin_aprobar, "specs/001-x/.Aprobaciones.json").returncode == 2
    assert w(sin_aprobar, "specs/001-x/PROGRESO.json").returncode == 2
