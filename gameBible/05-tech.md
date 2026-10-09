# 05 Tech

**Status:** milestone 1 probe recorded 2026-10-09. Numbers are PLACEHOLDER budgets until milestone 11.

## Architecture

See [AGENTS.md](../AGENTS.md) for the four assemblies. Rules are pure C# and unit-tested; Runtime is thin MonoBehaviours; Editor generates scenes and content.

## World building

Scenes are thin: `Run.unity` holds one `RunController` that points at `Resources/GameAssets`. Everything else (ground, light, player, camera, horde, HUD) is built in code from the run seed when the run starts, so a fresh clone needs no hand-made scene content and the Editor builders (`Old Gods > Build > Everything`) regenerate assets and scenes. See [ADR 0002](../docs/adr/0002-world-is-built-in-code.md).

The ground is a height field in `OldGods.Rules` turned into flat-shaded mesh chunks with colliders, not a Unity `Terrain`: it gives the low-poly faceted look, and enemies read the exact rendered height from the field without raycasts.

## Shaders

Hand-written URP HLSL, not Shader Graph, so they are diffable and authored as text: `OldGods/LowPoly` (vertex colour times base colour, main light with shadows, ambient, fog) and `OldGods/HordeInstanced` (the same look, drawn from a structured buffer with `Graphics.RenderMeshPrimitives`, one call per enemy type). Enemy legs swing procedurally until baked vertex animation replaces it in milestone 9. Vertex colours are authored in sRGB and stored linear.

## Horde runtime

One `HordeManager` owns struct arrays (position, velocity, hp, type, animation phase). Each frame it steers enemies to the player, separates them through a spatial hash, snaps them to terrain height, resolves weapon hits and writes instance matrices. Rendering uses `Graphics.RenderMeshInstanced`. No Rigidbody, NavMesh agent or Animator per enemy. See [ADR 0001](../docs/adr/0001-horde-runtime-is-array-based.md).

## Performance budget

Target: 500 animated enemies at 60 fps on Nick's PC. The probe (`python Tools/smoke.py --probe`, or the PlayMode `HordeProbeTest`) spawns 250, 500 and 1000 enemies around a player running in a circle and records frame times and horde CPU time to `TestResults/horde-probe.json`.

Release player, 1600x900 windowed, Ryzen 7 9800X3D / Radeon RX 9070 XT, 2026-10-09:

| Count | Avg ms | 1% low ms | Avg fps | Horde CPU ms |
|---|---|---|---|---|
| 250 | 0.74 | 1.46 | 1354 | 0.11 |
| 500 | 0.82 | 1.25 | 1221 | 0.21 |
| 1000 | 1.07 | 1.41 | 933 | 0.44 |

**Decision (2026-10-09):** the single-threaded C# loop is far inside budget (0.44 ms for 1000 enemies), so the hot loop stays on the main thread; Jobs and Burst are not used. Re-run the probe after real enemy art and weapons land (milestones 2 and 9); revisit if horde CPU passes 4 ms at 1000.

## Seeds

One `ulong` run seed, split by name into independent streams (`map`, `spawns`, `draft`, `loot`, `shrines`) so changing one system does not shift another's rolls.

## Saves

JSON in `Application.persistentDataPath/oldgods/`, with a version field, a migration hook and corrupted-save recovery (keep the bad file as `.corrupt`, start fresh).

## Packages

Unity 6000.6.3f1 bundles Cinemachine 6.6.0, Burst 2.0.0, Collections 6.6.0 and Mathematics 1.4.0; those versions are used. `com.unity.pipeline` 0.8.0-exp.1 comes from the registry so the `unity` CLI can drive an open Editor.
