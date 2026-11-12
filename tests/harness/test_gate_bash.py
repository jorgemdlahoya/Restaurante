import pytest

import harness_lib as h
from conftest import hook, crear_feature, constitucion_aprobada, REQ_OK, DIS_OK, TAR_OK


def b(raiz, comando):
    return hook("gate-bash.py", raiz, "Bash", {"command": comando})


@pytest.fixture
def sin_aprobar(proyecto):
    constitucion_aprobada(proyecto)
    crear_feature(proyecto, "001-x", {h.REQ: REQ_OK, h.DIS: DIS_OK, h.TAR: TAR_OK},
                  aprobar=[h.REQ, h.DIS])
    return proyecto


@pytest.mark.parametrize("cmd", [
    "pytest -q",
    "ls apps",
    "echo hola > specs/001-x/notas.md",
    "python3 .claude/scripts/completar-tarea.py T-01",
    "python3 .claude/scripts/estado.py --breve",
    "npm test 2>/dev/null",
    "git status && git log --oneline | head -3",
    "grep -rn 'RF-01' apps",
    "echo x > /tmp/salida.txt",
])
def test_permitidos(sin_aprobar, cmd):
    r = b(sin_aprobar, cmd)
    assert r.returncode == 0, (cmd, r.stderr)


@pytest.mark.parametrize("cmd", [
    "echo x > apps/a.ts",
    "echo x >> apps/a.ts",
    "echo x>apps/a.ts",
    "sed -i s/a/b/ apps/a.ts",
    "cat a | tee apps/b.ts",
    "cat a | tee -a apps/b.ts",
    "cp /tmp/a apps/a.ts",
    "mv specs/x.md apps/a.ts",
    "rm -f apps/a.ts",
    "touch apps/nuevo.ts",
    "mkdir -p apps/mod",
    "git checkout -- apps/a.ts",
    "git restore apps/a.ts",
    "python3 .claude/scripts/aprobar.py",
    "python3 .claude/scripts/aceptar.py HU-01",
    "echo '{' > harness.config.json",
    "echo x > .claude/settings.json",
    "cd apps; echo x > a.ts",
    "echo 'sin cerrar",
])
def test_bloqueados(sin_aprobar, cmd):
    r = b(sin_aprobar, cmd)
    assert r.returncode == 2, cmd
    assert r.stderr.strip()


def test_codigo_aprobado_permite_escritura(sin_aprobar):
    crear_feature(sin_aprobar, "001-x", {}, aprobar=[h.TAR])
    assert b(sin_aprobar, "echo x > apps/a.ts").returncode == 0


def test_destinos_escritura():
    assert h.destinos_escritura("cat a > b.txt && sed -i 's/x/y/' c.txt | tee d e") == [
        "b.txt", "c.txt", "d", "e"]
    assert h.destinos_escritura("ls 2>/dev/null") == []


def test_rutas_relativas_al_cwd(sin_aprobar):
    import json
    from conftest import correr
    sub = sin_aprobar / "specs" / "001-x"
    payload = json.dumps({"tool_name": "Bash", "tool_input": {"command": "echo x > notas.md"},
                          "cwd": str(sub)})
    assert correr("gate-bash.py", raiz=sin_aprobar, stdin=payload).returncode == 0
    payload = json.dumps({"tool_name": "Bash", "tool_input": {"command": "echo x > ../../apps/a.ts"},
                          "cwd": str(sub)})
    assert correr("gate-bash.py", raiz=sin_aprobar, stdin=payload).returncode == 2
