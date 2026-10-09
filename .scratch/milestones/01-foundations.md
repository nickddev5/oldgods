# Milestone 1: Foundations

Branch: `milestone-1-foundations`

## Scope

Horde performance probe (250/500/1000 to `TestResults/horde-probe.json`); player motor (run, jump, slide, air control, fall damage); chase camera; run seed and streams; content loader; save folder; Windows build, smoke test and settings guard.

## Done when

Probe recorded in gameBible/05-tech.md; movement and camera play-tested by Nick on a greybox hill map; smoke build passes.

## Progress

- [x] Rules: run seed and streams, horde steering and spatial hash, motor, health, height field, terrain generator, save model (EditMode tests)
- [x] Horde manager and instanced renderer (one draw per enemy type), procedural walk
- [x] Player motor: run, jump (coyote, buffer), slide (slope gain), air control, fall damage
- [x] Chase camera: Cinemachine orbital follow, recentres behind travel, pulls in against ground
- [x] Content loader (ScriptableObjects to rules records), save folder with corruption recovery
- [x] Greybox hill map with cliffs, test spawner, HUD, death and restart
- [x] Windows build (`Tools/build_windows.py`), smoke test, settings guard, screenshots, horde probe
- [x] Probe recorded in gameBible/05-tech.md (1000 enemies at 933 fps; no Jobs/Burst needed)
- [ ] Nick play-tests movement and camera on the greybox map
