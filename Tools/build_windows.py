"""Builds the Windows player in batch mode.

Usage: python Tools/build_windows.py [--out PATH] [--development]

Runs Unity.exe -batchmode -executeMethod OldGods.Editor.Build.Windows, which rebuilds
generated assets, checks the settings guard before and after, and exits non-zero on
any failure. Refuses to run while an Editor has the project open.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from unity_paths import DEFAULT_BUILD, PROJECT, project_is_open, unity_exe  # noqa: E402


def build_command(exe: Path, out: Path, development: bool, log: Path) -> list[str]:
    cmd = [
        str(exe), "-batchmode", "-quit", "-projectPath", str(PROJECT),
        "-executeMethod", "OldGods.Editor.Build.Windows",
        "-buildOut", str(out), "-logFile", str(log),
    ]
    if development:
        cmd.append("-development")
    return cmd


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Build the Old Gods Windows player")
    parser.add_argument("--out", type=Path, default=DEFAULT_BUILD)
    parser.add_argument("--development", action="store_true")
    parser.add_argument("--log", type=Path, default=PROJECT / "Logs" / "build_windows.log")
    args = parser.parse_args(argv)

    if project_is_open():
        print("The project is open in an Editor. Close it, or use Old Gods > Build > Windows Player.")
        return 2
    exe = unity_exe()
    if not exe.exists():
        print(f"Unity editor not found at {exe}; set UNITY_EXE.")
        return 2
    args.log.parent.mkdir(parents=True, exist_ok=True)
    print("Building:", args.out)
    code = subprocess.call(build_command(exe, args.out, args.development, args.log))
    print(f"Unity exited {code}; log: {args.log}")
    if code == 0 and not args.out.exists():
        print("Build reported success but the player is missing.")
        return 1
    return code


if __name__ == "__main__":
    sys.exit(main())
