# 1. The horde runtime is array-based

Date: 2026-10-09. Status: accepted.

## Context

The game needs hundreds of enemies on screen at 60 fps on 3D terrain. A GameObject per enemy with a Rigidbody, NavMesh agent and Animator costs too much CPU per enemy and too many draw calls.

## Decision

One `HordeManager` owns all common enemies as struct arrays. It steers, separates through a spatial hash, snaps to terrain height and resolves hits in one loop, and renders with `Graphics.RenderMeshInstanced` and a vertex-animation-texture shader. Steering and separation math lives in `OldGods.Rules` so it is unit-tested. Only the player, minibosses and bosses are full GameObjects with rigs.

If the milestone 1 probe shows the loop over budget, the hot loop moves to `IJobParallelFor` with Burst behind the same manager API.

## Consequences

- Enemies have no physics colliders; weapon hits query the spatial hash instead.
- Enemy animation is baked to textures by an Editor tool, so enemy rigs need a bake step.
- Pathing is direct steering with separation, not NavMesh. Maps are generated without traps that would strand enemies. (Amended by [ADR 0004](0004-levels-are-dressed-and-the-horde-uses-a-flow-field.md): around walls and cliffs the horde follows a flow field.)
