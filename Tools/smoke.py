"""Runs the Windows player's built-in smoke test, horde probe or screenshot pass.

Usage:
  python Tools/smoke.py                 # start run, die, restart, quit; exit code 0 = pass
  python Tools/smoke.py --probe         # horde probe; writes TestResults/horde-probe.json
  python Tools/smoke.py --shots DIR     # screenshots of a run into DIR
"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from unity_paths import DEFAULT_BUILD, PROJECT  # noqa: E402

RESULTS = PROJECT / "TestResults"


def player_command(exe: Path, mode: str, out: Path, log: Path, seed: str | None,
                   width: int, height: int, extra: list[str] | None = None) -> list[str]:
    cmd = [str(exe), "-screen-fullscreen", "0", "-screen-width", str(width),
           "-screen-height", str(height), "-logFile", str(log)]
    if mode == "smoke":
        cmd += ["-smoke", "-smokeOut", str(out)]
    elif mode == "probe":
        cmd += ["-probe", "-probeOut", str(out)]
    elif mode == "shots":
        cmd += ["-shots", str(out)]
    else:
        raise ValueError(f"unknown mode {mode}")
    if seed:
        cmd += ["-seed", seed]
    return cmd + (extra or [])


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Old Gods player checks")
    group = parser.add_mutually_exclusive_group()
    group.add_argument("--probe", action="store_true")
    group.add_argument("--shots", type=Path)
    parser.add_argument("--exe", type=Path, default=DEFAULT_BUILD)
    parser.add_argument("--seed")
    parser.add_argument("--timeout", type=float, default=300.0)
    parser.add_argument("--width", type=int, default=1600)
    parser.add_argument("--height", type=int, default=900)
    parser.add_argument("extra", nargs="*", help="extra player arguments, after --")
    args = parser.parse_args(argv)

    if not args.exe.exists():
        print(f"Player not found at {args.exe}; run Tools/build_windows.py first.")
        return 2
    RESULTS.mkdir(exist_ok=True)
    if args.probe:
        mode, out = "probe", RESULTS / "horde-probe.json"
    elif args.shots:
        mode, out = "shots", args.shots
    else:
        mode, out = "smoke", RESULTS / "smoke.log"
    log = RESULTS / f"player-{mode}.log"
    cmd = player_command(args.exe, mode, out, log, args.seed, args.width, args.height, args.extra)
    print("Running:", " ".join(cmd))
    try:
        code = subprocess.call(cmd, timeout=args.timeout)
    except subprocess.TimeoutExpired:
        print(f"Timed out after {args.timeout:.0f}s; see {log}")
        return 1
    if mode in ("smoke", "probe") and out.exists():
        print(out.read_text(encoding="utf-8"))
    print(f"Player exited {code}; log: {log}")
    return code


if __name__ == "__main__":
    sys.exit(main())
