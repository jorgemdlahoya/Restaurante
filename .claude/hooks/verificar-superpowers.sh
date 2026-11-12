#!/usr/bin/env bash
# Hook SessionStart: comprueba que Superpowers está instalado e informa la feature activa.
# Su salida estándar se añade al contexto de la sesión. Nunca falla.
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGINS="${CLAUDE_CONFIG_DIR:-$HOME/.claude}/plugins"

echo "Harness SDD activo. Flujo: /analizar → /aprobar → /planificar → /aprobar → /implementar → /aceptar"
if ! find "$PLUGINS" -path '*skills/brainstorming/SKILL.md' -print -quit 2>/dev/null | grep -q .; then
  echo "AVISO: no se encontró el plugin superpowers (requerido por /analizar, /planificar e /implementar)."
  echo "Pide al usuario que lo instale:"
  echo "  /plugin marketplace add obra/superpowers-marketplace"
  echo "  /plugin install superpowers@superpowers-marketplace"
fi
python3 "$DIR/../scripts/estado.py" --breve 2>&1 || true
exit 0
