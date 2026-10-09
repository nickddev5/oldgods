# Milestone 11: Finish

Branch: `milestone-11-finish`

## Scope

Balance and performance passes; Windows build; smoke test; fresh-save play-through.

## Done when

Nick plays from a fresh save to the ending in the Windows build.

## Progress

- [x] Autoplay balance bot (`Tools/smoke.py --autoplay`); XP curve flattened, pickup range widened, Stone Warden contact damage lowered (gameBible/05-tech.md)
- [x] Performance: 1000 enemies at 782 fps in the release build (0.61 ms horde CPU)
- [x] Release Windows build via Tools/build_windows.py; smoke test passes through menu, run, death, restart and menu
- [x] EditMode 86 and PlayMode 14 tests pass; verifiers pass
- [ ] Nick plays from a fresh save to the ending in the Windows build
