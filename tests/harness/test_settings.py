import json
import os
import re

from conftest import REPO, correr

SETTINGS = REPO / ".claude" / "settings.json"


def _comandos(cfg, evento):
    return {m.get("matcher", ""): [h["command"] for h in m["hooks"]] for m in cfg["hooks"][evento]}


def test_settings_hooks():
    cfg = json.loads(SETTINGS.read_text())
    pre = _comandos(cfg, "PreToolUse")
    assert set(pre) == {"Write|Edit|MultiEdit|NotebookEdit", "Bash"}
    todos = [c for cs in pre.values() for c in cs] + [c for cs in _comandos(cfg, "SessionStart").values() for c in cs]
    for c in todos:
        rel = re.search(r'\$CLAUDE_PROJECT_DIR/([^"]+)"', c).group(1)
        assert (REPO / rel).exists(), c
    assert cfg["enabledPlugins"]["superpowers@superpowers-marketplace"] is True
    src = cfg["extraKnownMarketplaces"]["superpowers-marketplace"]["source"]
    assert src == {"source": "github", "repo": "obra/superpowers-marketplace"}


def test_sessionstart_sin_plugin(proyecto, tmp_path):
    r = correr("verificar-superpowers.sh", raiz=proyecto,
               env_extra={"CLAUDE_CONFIG_DIR": str(tmp_path / "vacio")})
    assert r.returncode == 0
    assert "superpowers" in r.stdout and "/plugin install" in r.stdout
    assert "Sin feature activa" in r.stdout


def test_sessionstart_con_plugin(proyecto, tmp_path):
    skill = tmp_path / "cfg" / "plugins" / "x" / "skills" / "brainstorming"
    skill.mkdir(parents=True)
    (skill / "SKILL.md").write_text("x")
    r = correr("verificar-superpowers.sh", raiz=proyecto,
               env_extra={"CLAUDE_CONFIG_DIR": str(tmp_path / "cfg")})
    assert r.returncode == 0 and "/plugin install" not in r.stdout
