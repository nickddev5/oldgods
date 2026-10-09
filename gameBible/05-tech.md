# 05 Tech

**Status:** PLACEHOLDER until the milestone 1 probe is recorded.

## Architecture

See [AGENTS.md](../AGENTS.md) for the four assemblies. Rules are pure C# and unit-tested; Runtime is thin MonoBehaviours; Editor generates scenes and content.

## Horde runtime

One `HordeManager` owns struct arrays (position, velocity, hp, type, animation phase). Each frame it steers enemies to the player, separates them through a spatial hash, snaps them to terrain height, resolves weapon hits and writes instance matrices. Rendering uses `Graphics.RenderMeshInstanced`. No Rigidbody, NavMesh agent or Animator per enemy. See [ADR 0001](../docs/adr/0001-horde-runtime-is-array-based.md).

## Performance budget

Target to measure: 500 animated enemies at 60 fps on Nick's PC. The milestone 1 probe spawns 250, 500 and 1000 and records average and 1% frame times in `TestResults/horde-probe.json`. Results go here.

| Count | Avg ms | 1% low ms | Notes |
|---|---|---|---|
| 250 | TBD | TBD | |
| 500 | TBD | TBD | |
| 1000 | TBD | TBD | |

## Seeds

One `ulong` run seed, split by name into independent streams (`map`, `spawns`, `draft`, `loot`, `shrines`) so changing one system does not shift another's rolls.

## Saves

JSON in `Application.persistentDataPath/oldgods/`, with a version field, a migration hook and corrupted-save recovery (keep the bad file as `.corrupt`, start fresh).

## Packages

Unity 6000.6.3f1 bundles Cinemachine 6.6.0, Burst 2.0.0, Collections 6.6.0 and Mathematics 1.4.0; those versions are used. `com.unity.pipeline` 0.8.0-exp.1 comes from the registry so the `unity` CLI can drive an open Editor.
