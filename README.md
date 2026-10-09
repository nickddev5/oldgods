# The Old Gods

A 3D low-poly horde-survival roguelike and the prequel to *The Empty Throne*. You play one of the old gods: survive ~10-minute stages on a random map with automatic weapons, find the hidden boss portal, outlast the final swarm, and unlock the rest of the pantheon. Elias is the last unlock, and the only one who can beat The Last Test and take the throne.

- Engine: Unity 6000.6.3f1, URP 17.6, Input System, Cinemachine.
- Code: `Assets/OldGods/` (see [AGENTS.md](AGENTS.md) for the assembly layout and rules).
- Design: [gameBible/](gameBible/README.md). Architecture decisions: [docs/adr/](docs/adr/).
- Milestone tickets: [.scratch/milestones/](.scratch/milestones/).

## Running

Open the repo root in Unity 6000.6.3f1, run **Old Gods > Build > Everything** once, then open `Assets/OldGods/Scenes/Run.unity` and press Play.

Controls: WASD or left stick to move, mouse or right stick to look, Space or A to jump, Shift/Ctrl or B to slide (hold).

Tests:

```
unity test "C:\The Old Gods" --mode EditMode --output TestResults/editmode.xml
```

Windows build, smoke test, horde probe and screenshots:

```
python Tools/build_windows.py
python Tools/smoke.py
python Tools/smoke.py --probe
python Tools/smoke.py --shots TestResults/shots
```
