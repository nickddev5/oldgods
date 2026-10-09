"""Checks the Python tooling: every file compiles, and mypy runs if it is installed.

Usage: python Tools/typecheck.py [files...]
"""
from __future__ import annotations

import importlib.util
import py_compile
import subprocess
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent


def targets(argv: list[str]) -> list[Path]:
    if argv:
        return [Path(a) for a in argv if a.endswith(".py")]
    return sorted(TOOLS.glob("*.py"))


def main(argv: list[str] | None = None) -> int:
    files = targets(sys.argv[1:] if argv is None else argv)
    failed = 0
    for f in files:
        try:
            py_compile.compile(str(f), doraise=True)
        except py_compile.PyCompileError as e:
            print(e.msg)
            failed += 1
    if importlib.util.find_spec("mypy") is not None and files:
        code = subprocess.call([sys.executable, "-m", "mypy", "--ignore-missing-imports", *map(str, files)])
        failed += 1 if code else 0
    else:
        print("mypy not installed; compiled only")
    print(f"{len(files)} files checked, {failed} problems")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
