# 4. Levels are dressed from a layout, and the horde paths with a flow field

Date: 2026-10-09. Status: accepted.

## Context

Nick's play-test: a stage was "a flat green plane". Megabonk-style stages need height worth climbing, ruins to fight around and places worth a detour. Solid ruins and cliffs break ADR 0001's direct steering: enemies would walk through walls or pile up against a cliff under a player standing on a plateau.

## Decision

- `LayoutGenerator` (Rules) dresses each generated height field: it picks the boss gate's site, lays the old road (and the steppe's wall), then places each biome's landmarks on clear ground. Each landmark stamps its shape into the terrain (plateaus with cliff sides and ramps, mounds, ravines, causeways), lists its pieces (columns, walls, the fallen colossus, the wreck), its solid shapes as capsules, its ground marks and its detour spots. Features take those spots first, so chests sit on hill forts and shrines in stone circles.
- The runtime only draws: `Dressing` builds one mesh and collider per landmark from the pieces (`LandmarkModels`), plus one glowing mesh for embers and fire.
- The horde still steers in one array loop with no NavMesh. A `FlowField` over the height-field cells classes each cell as open, steep (passable at a cost and half speed), cliff (blocked) or under dressing (paths avoid it). It is re-solved only when the player moves to another cell. An enemy follows it only when its path is more than 1.25 times the straight line, so in the open the horde still runs straight at the player. Enemies are pushed out of the dressing's capsules and cannot step onto a cliff cell.
- Built-in rocks and trees are capsules too, so the Ash Wood's trees form a thicket the horde must thread.

## Consequences

- High ground is an advantage, not a refuge: plateaus are reached by ramps, and rock shelves low enough to jump onto are scrambled up slowly by the horde.
- The probe now includes the path solve: 1000 enemies at 548 fps, 0.73 ms horde CPU on Nick's PC (was 0.63 ms).
- Landmark kinds, counts and ground colours are per-biome data (`LayoutPresets`, `BiomeDefinition`), PLACEHOLDER like every other number.
- Ground marks are exact shapes, evaluated per terrain triangle, so their edges follow the mesh rather than the cell grid.
