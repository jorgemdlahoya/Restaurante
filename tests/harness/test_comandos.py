import pytest

from conftest import REPO

CMD = REPO / ".claude" / "commands"


def _texto(nombre):
    return (CMD / f"{nombre}.md").read_text(encoding="utf-8")


def _frontmatter(texto):
    assert texto.startswith("---\n")
    return texto.split("---\n")[1]


@pytest.mark.parametrize("nombre", ["analizar", "aprobar", "planificar", "implementar", "aceptar", "estado"])
def test_existen_con_descripcion(nombre):
    assert "description:" in _frontmatter(_texto(nombre))


@pytest.mark.parametrize("nombre,script", [("aprobar", "aprobar.py"), ("aceptar", "aceptar.py")])
def test_solo_usuario(nombre, script):
    t = _texto(nombre)
    fm = _frontmatter(t)
    assert "disable-model-invocation: true" in fm
    assert f"Bash(python3 .claude/scripts/{script}:*)" in fm
    assert f"!`python3 .claude/scripts/{script} $ARGUMENTS`" in t


def test_analizar():
    t = _texto("analizar")
    for clave in ["superpowers:brainstorming", "requerimiento.md", "diseno.md",
                  "nueva-feature.py", "--forzar", "/aprobar"]:
        assert clave in t, clave
    assert "NO invoques" in t and "writing-plans" in t


def test_planificar():
    t = _texto("planificar")
    assert "superpowers:writing-plans" in t and "tareas.md" in t


def test_implementar():
    t = _texto("implementar")
    for clave in ["subagent-driven-development", "completar-tarea.py", "NNN/RF-NN",
                  "systematic-debugging", "/aceptar", "finishing-a-development-branch"]:
        assert clave in t, clave


def test_claude_md_y_readme():
    c = (REPO / "CLAUDE.md").read_text(encoding="utf-8")
    assert len(c.splitlines()) <= 80
    assert "/analizar" in c and "NNN/RF-NN" in c
    assert "pytest tests/harness" in (REPO / "README.md").read_text(encoding="utf-8")
