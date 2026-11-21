"""El gate permite sólo el archivo de estado de Ralph Loop dentro de .claude/."""
import json

import pytest

from conftest import REPO, correr, hook


def w(raiz, ruta):
    return hook("gate-spec.py", raiz, "Write", {"file_path": str(raiz / ruta), "content": "x"})


def b(raiz, comando):
    payload = json.dumps({"tool_name": "Bash", "tool_input": {"command": comando}, "cwd": str(raiz)})
    return correr("gate-bash.py", raiz=raiz, stdin=payload)


def test_write_estado_ralph(proyecto):
    assert w(proyecto, ".claude/ralph-loop.local.md").returncode == 0


@pytest.mark.parametrize("cmd", ["rm .claude/ralph-loop.local.md",
                                 "rm -f ./.claude/ralph-loop.local.md"])
def test_bash_rm_estado_ralph(proyecto, cmd):
    r = b(proyecto, cmd)
    assert r.returncode == 0, r.stderr


@pytest.mark.parametrize("ruta", [".claude/otro.local.md", ".claude/.ralph-loop.local.md",
                                  ".claude/ralph-loop.local.md.bak", ".claude/RALPH-LOOP.local.md",
                                  ".claude/settings.json"])
def test_resto_de_claude_sigue_protegido(proyecto, ruta):
    assert w(proyecto, ruta).returncode == 2
    assert b(proyecto, f"rm {ruta}").returncode == 2


def test_gitignore_estado_ralph():
    assert ".claude/ralph-loop.local.md" in (REPO / ".gitignore").read_text().splitlines()
