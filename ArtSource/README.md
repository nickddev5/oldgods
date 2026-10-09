# ArtSource: how art gets into The Old Gods

Every model, texture and sound in the game must be **CC0 or made for this project**. Record each imported file in [SOURCES.md](SOURCES.md) with where it came from and its licence before it is committed.

## What exists now (milestone 9)

All shipped art is built in code, so it is ours and licence-safe:

| What | Where |
|---|---|
| Gods (7 head pieces, robe and mark colours) | `Runtime/MeshKit.cs` `PlaceholderMeshes.God(look)` |
| Enemies (10 types, 3 champions) | `Runtime/Art/EnemyModels.cs` |
| Bosses and The Last Test | `PlaceholderMeshes.Boss(model)` |
| Biome props (standing stones, boulders, sea stacks, pines, dead trees, driftwood) | `Runtime/Art/PropModels.cs` |
| Ground | `Runtime/Ground.cs` (height field, flat-shaded, coloured by height and slope) |
| Effects | `Runtime/Combat/Effects.cs`, `Runtime/Fx.cs` |
| Sound and music | `Runtime/Art/Audio.cs` (synthesized at startup; see [Tools/audio](../Tools/audio/README.md)) |

These are deliberately simple. A hand-made art pass should replace them one asset at a time without code changes.

## Replacing a model with a Blender model

1. Model low-poly and flat-shaded. Keep silhouettes readable from a 9 m chase camera: a husk is about 1.6 m, a god about 1.8 m, a boss 5 to 6 m.
2. Colour with **vertex colours** in sRGB (the shaders multiply vertex colour by the material colour; the import converts to linear). No textures are needed.
3. Orientation: face +Z, feet at Y = 0, metres. Apply all transforms before export.
4. Export FBX to `ArtSource/<kind>/<name>.fbx` and the .blend beside it. Import into `Assets/OldGods/Art/<kind>/`.
5. Enemies: set the **Mesh** field on the enemy's asset in `Assets/OldGods/Content/Enemies/`. The horde shader swings anything below 0.7 m and off the centre line as legs, so keep legs there and keep cloaks or bodies above 0.7 m (or set **Walk Swing** near 0 for drifting enemies).
6. Bosses, gods and props: add a mesh field the same way when the first one lands (one small code change per kind), keeping the built-in model as the fallback.
7. Check with `python Tools/smoke.py --shots TestResults/shots -- -shotTimes "6,20" -autopick` and the horde probe (`python Tools/smoke.py --probe`): 1000 enemies must stay above 60 fps.

## Vertex animation textures

The plan called for baking rigged enemy animations into vertex animation textures. Until enemies have rigs and clips, the horde shader animates them procedurally (legs swing with distance travelled, bodies bob). See [ADR 0003](../docs/adr/0003-procedural-art-and-sound.md). When rigged enemies arrive, add the baker then, with the probe as the gate.
