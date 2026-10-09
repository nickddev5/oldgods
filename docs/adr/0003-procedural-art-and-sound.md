# 3. Art and sound are generated in code until a hand-made pass

Date: 2026-10-09. Status: accepted.

## Context

The first milestone needs every placeholder replaced with licence-safe art and audio. All art must be CC0 or made for this project. The agents building the game cannot model in Blender or audition sound interactively, and enemies have no rigs to bake into vertex animation textures.

## Decision

- Models (gods, 13 enemies, bosses, props) are built in code from flat-shaded boxes, prisms and gems with vertex colours (`MeshKit`, `EnemyModels`, `PropModels`). They are made for this project, so they are licence-safe.
- Enemy animation is procedural in the horde shader (legs swing with distance travelled, bodies bob), tuned per enemy type with Walk Swing. No vertex animation texture baker is built until rigged enemies exist.
- Sound effects and music are synthesized at startup (`Audio.cs`).
- Every asset can be replaced by a hand-made one without code changes for enemies (the Mesh field) and with one small change per kind otherwise. The process is in `ArtSource/README.md`.

## Consequences

- The game ships with a consistent, simple low-poly look and functional audio, but not art an artist would sign off.
- The horde probe measured after the change: 1000 enemies at 801 fps, 0.61 ms horde CPU (release player, RX 9070 XT).
- A later art pass should start with enemies and gods, the most-seen silhouettes.
