#!/usr/bin/env python3
"""Registra la aceptación manual de una historia de usuario.

Uso: aceptar.py HU-NN [--feature NNN]
SÓLO lo ejecuta el usuario mediante /aceptar. El gate de Bash impide que lo ejecute el agente.
"""
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import harness_lib as h  # noqa: E402


def suite_completa(fdir: Path) -> str | None:
    """Ejecuta comando_e2e_stack. None si pasa; si no, el motivo."""
    raiz = h.raiz()
    comando = h.cargar_config(raiz).get("comando_e2e_stack")
    if not comando:
        return ("harness.config.json no tiene 'comando_e2e_stack'. Añádelo a mano (p. ej. "
                "\"npm --prefix apps/web exec playwright test --config playwright.stack.config.ts\") "
                "o re-aprueba la constitución con la plantilla actual.")
    print(f"Ejecutando la suite completa de Playwright: {comando}")
    timeout = int(os.environ.get("HARNESS_E2E_TIMEOUT", "1800"))
    ok, motivo = h.ejecutar_comando(comando, raiz, timeout)
    if ok:
        return None
    return ("la suite completa de Playwright no pasó. Levanta el sistema con "
            f"`docker compose up` y vuelve a intentarlo.\n{motivo}")


def main(argv: list[str]) -> int:
    if not argv:
        print("Uso: /aceptar HU-NN [--feature NNN]")
        return 1
    hu = argv[0].strip()
    nnn = argv[argv.index("--feature") + 1] if "--feature" in argv else None
    fdir = h.buscar_feature(h.raiz(), nnn)
    pendientes = h.motivo_docs(fdir)
    if pendientes:
        print(f"No se puede aceptar {hu}: la spec de {fdir.name} no está aprobada: {pendientes}.")
        return 1
    estados = h.estado_hus(fdir)
    if hu not in estados:
        print(f"{hu} no existe en {fdir.name}. Disponibles: {', '.join(estados) or 'ninguna'}.")
        return 1
    if estados[hu] == "aceptada":
        print(f"{hu} ya estaba aceptada.")
        return 0
    if estados[hu] != "implementada":
        print(f"No se puede aceptar {hu}: está '{estados[hu]}', debe estar implementada "
              "(todas sus tareas hechas con tests en verde).")
        return 1
    prog = h.cargar_progreso(fdir)
    restantes = [u for u, e in estados.items() if e != "aceptada" and u != hu]
    if not restantes:  # esta aceptación cerraría la feature: exige la suite completa
        error = suite_completa(fdir)
        if error:
            print(f"No se puede aceptar {hu}: es la última HU de {fdir.name} y {error}")
            return 1
    prog["aceptadas"][hu] = {"fecha": h.ahora()}
    h.guardar_progreso(fdir, prog)
    print(f"{hu} aceptada.")
    if h.fase(fdir) == "cerrada":
        print(f"Todas las HU de {fdir.name} están aceptadas: la feature está cerrada.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except h.HarnessError as e:
        print(str(e))
        sys.exit(1)
