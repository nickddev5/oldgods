# 05 Tech

**Status:** milestone 1 probe recorded 2026-10-09. Numbers are PLACEHOLDER budgets until milestone 11.

## Architecture

See [AGENTS.md](../AGENTS.md) for the four assemblies. Rules are pure C# and unit-tested; Runtime is thin MonoBehaviours; Editor generates scenes and content.

## World building

Scenes are thin: `Run.unity` holds one `RunController` that points at `Resources/GameAssets`. Everything else (ground, light, player, camera, horde, HUD) is built in code from the run seed when the run starts, so a fresh clone needs no hand-made scene content and the Editor builders (`Old Gods > Build > Everything`) regenerate assets and scenes. See [ADR 0002](../docs/adr/0002-world-is-built-in-code.md).

The ground is a height field in `OldGods.Rules` turned into flat-shaded mesh chunks with colliders, not a Unity `Terrain`: it gives the low-poly faceted look, and enemies read the exact rendered height from the field without raycasts.

Each stage map is then dressed by `LayoutGenerator` (Rules): the boss gate's site, the old road and wall, and the biome's landmarks, each stamping its shape into the height field and listing its ruin pieces, solid capsules, ground marks and detour spots. `Dressing` draws the pieces as one mesh and collider per landmark; grass tufts, flowers and pebbles are merged into one mesh per ground chunk. See [ADR 0004](../docs/adr/0004-levels-are-dressed-and-the-horde-uses-a-flow-field.md).

## Shaders

Hand-written URP HLSL, not Shader Graph, so they are diffable and authored as text: `OldGods/LowPoly` (vertex colour times base colour, main light with shadows, ambient, fog) and `OldGods/HordeInstanced` (the same look, drawn from a structured buffer with `Graphics.RenderMeshPrimitives`, one call per enemy type). Characters are animated procedurally in the vertex shader from tags baked into each mesh: the body part, the joint it swings around, and the knee or elbow height where it bends. Legs stride and bend at the knee, arms counter-swing with bent elbows, the body leans and bobs. The player also blends in a jump pose and a slide pose, and the model leans into turns and squashes on landing. Vertex colours are authored in sRGB and stored linear.

Both shaders have an inverted-hull outline pass (`_OutlineWidth` in pixels; 0 turns it off, so props pay only a culled vertex pass) and a pixel-art texture: `PixelTexture` generates a 128-pixel, point-filtered, four-tone detail map in code (grain, grass, streak channels), bound globally as `_OG_PixelTex`; the shaders project it in rest-pose object space by the face's dominant axis, at `_TexelsPerMeter` (10 on scenery, 14 on characters and the horde) and strength `_PixelAmount`. `OldGods/Sky` draws `SkyDome`, one camera-following mesh with a gradient, far hill rings and flat clouds, coloured from the biome's ambient and fog by `SkyColors`. `SceneLook` builds the post-processing volume in code (bloom, neutral tonemapping, colour adjustments, vignette). Cost at 1,000 enemies: about 0.3 ms per frame (565 to 487 fps on the probe, 2026-10-09).

## Horde runtime

One `HordeManager` owns struct arrays (position, velocity, hp, type, animation phase). Each frame it steers enemies to the player, separates them through a spatial hash, snaps them to terrain height, resolves weapon hits and writes instance matrices. Rendering uses `Graphics.RenderMeshInstanced`. No Rigidbody, NavMesh agent or Animator per enemy. See [ADR 0001](../docs/adr/0001-horde-runtime-is-array-based.md).

Around walls and cliffs the horde follows a flow field (shortest paths to the player over the height-field cells, re-solved when the player changes cell); in the open it still runs straight at the player. Enemies are pushed out of the dressing's capsules, cannot step onto cliffs and climb steep ground at half speed. See [ADR 0004](../docs/adr/0004-levels-are-dressed-and-the-horde-uses-a-flow-field.md).

## Performance budget

Target: 500 animated enemies at 60 fps on Nick's PC. The probe (`python Tools/smoke.py --probe`, or the PlayMode `HordeProbeTest`) spawns 250, 500 and 1000 enemies around a player running in a circle and records frame times and horde CPU time to `TestResults/horde-probe.json`.

Release player, 1600x900 windowed, Ryzen 7 9800X3D / Radeon RX 9070 XT, 2026-10-09:

| Count | Avg ms | 1% low ms | Avg fps | Horde CPU ms |
|---|---|---|---|---|
| 250 | 0.74 | 1.46 | 1354 | 0.11 |
| 500 | 0.82 | 1.25 | 1221 | 0.21 |
| 1000 | 1.07 | 1.41 | 933 | 0.44 |

After the milestone 9 art pass (13 enemy models, props, effects, sound): 1000 enemies at 801 fps, 1% low 1.53 ms, horde CPU 0.61 ms.

After the level pass (landmarks, flow field, ground clutter), seed 1111 stage 1: 1000 enemies at 548 fps, 1% low 3.25 ms, horde CPU 0.73 ms including path solves.

**Decision (2026-10-09):** the single-threaded C# loop is far inside budget (0.44 ms for 1000 enemies), so the hot loop stays on the main thread; Jobs and Burst are not used. Re-run the probe after real enemy art and weapons land (milestones 2 and 9); revisit if horde CPU passes 4 ms at 1000.

## Play bot (whole runs, report and suggestions)

`python Tools/playbot.py` plays whole runs in the Windows player and writes `TestResults/playbot/<time>/report.md` (and `report.json`, plus one `run-*.json` per run). Each run starts at the menu, picks the god at character select and goes through every biome, its boss and The Last Test. The bot explores, kites the horde, collects gems and gold, opens chests it can afford, uses Charge, Gifts and Drawing shrines, buys from the merchant, reads lore stones, wakes each boss at 70% of the stage clock (later if below half health) (`-botBossAt`), circles bosses at its weapons' reach, steps out of telegraphed circles and charge lines, jumps shockwaves and steps, slides out of crowds and down long slopes, and walks round walls. It knows where the gate is; everything else it must find.

Its choices come from two decision trees in `Rules/BotTree.cs` (`BotTrees`, tested in `BotTreeTests`), asked every 0.4 s. Thresholds are PLACEHOLDER.

- **Goal tree (where to go):** in The Last Test, fight the last boss. If a boss is awake, fight it. If the boss is down, open its free chest, then take the portal. Below 30% health, recover (safe gems, else open ground in the middle). At 70% of the stage clock, with the gate usable and health at least half (or the final swarm close), wake the boss. Otherwise use a wanted feature (charge shrines by standing in the ring), collect gems when no enemy is on top, or explore.
- **Stance tree (how to treat the horde):** holding a close-range weapon (base reach under 5 m: Rending Claws, Flame Aura, Hammer Orbit, Bone Ring), the bot stays close to the nearest enemy at about two thirds of that weapon's reach, with extra room for champions, and fights bosses from just outside their body. It backs off to kiting below half health or with four enemies touching it. With only ranged weapons it kites.

The report's "How the bot spent its time" tables give the share of play time on each branch, so a change to the trees shows up in the numbers.

- `python Tools/playbot.py`: every god in the game data (asked from the player, so new gods are picked up), seeds 1111 and 2222, all content unlocked.
- `--gods god.storm,god.forge --seeds 1111,2222,3333` or `--runs 3`; `--jobs 2` runs two players at once. Every run opens its own game window, so a sweep is capped at 24 runs and 3 windows at once; pass `--max-runs N` for a bigger sweep on purpose.
- `--campaign 8`: eight runs on one fresh save; between runs the bot spends Embers (next god first, then the cheapest unlock or powerup with what is left above the next god's price) and plays its newest god.
- `--picks smart|first|random`: smart weighs card kind and rarity with a seeded tie-break, so seeds try different builds.
- `--summarise DIR` rebuilds the report from saved runs.

Bot windows are always silent: the bot sets `Audio.Muted`, so no music or effects play and the volume stays at 0 (the Windows mixer shows the window at zero). Runs go faster than real time: the player steps the game a fixed 1/30 s per frame (`--step`) without waiting for the clock, so the speed-up is the frame rate over 30. Reports carry time per stage, deaths and what killed the bot, damage taken by source (each boss attack by name), level-up offers and picks, win rate per god and weapon, weapon damage shares, chests, shrines, gold, Embers, stuck spots, falls out of the world, jumps, slides, dodges and frame rate. The suggestions are plain rules over those numbers (a boss that kills most runs, a stage that never hurts, a weapon never offered or doing little damage, a god far behind the others, repeated stuck spots, holes in the ground, 1% lows under 30 fps, unspent gold, runs needed to unlock every god); they point at things to look at, not decisions.

The bot is a steady player, not a skilled one: read its numbers as a floor. `python Tools/smoke.py --autoplay TestResults/autoplay.json --seed 1111` still runs the old stage-1 check (the same bot, first card, stopping after stage 1). The table below is from the earlier, simpler stage-1 bot.

| Date | Change | Seed | Result |
|---|---|---|---|
| 2026-10-09 | before tuning | 1111 | level 6 at 8:00 with 387 kills; most gems left on the ground; died at the gate |
| 2026-10-09 | XP curve 5 + 4(n-1)^1.25, pickup range 5 m, bot collects gems | 1111 | level 16 at 8:00, 957 kills, full health until the gate; died to the Stone Warden plus the horde |
| 2026-10-09 | Stone Warden contact 14 to 10, bot keeps 14 m from bosses | 2222 | short-range draft (Quake, Flame Aura): level 11 at 8:00, worn down to 20% on the way to the gate, died there |

Reading: the first eight minutes are survivable and the draft now flows; the gate trek through the late-stage horde and the boss are the hard part. Nick's play-test decides whether that is right for a human.

## Seeds

One `ulong` run seed, split by name into independent streams (`map`, `spawns`, `draft`, `loot`, `shrines`) so changing one system does not shift another's rolls.

## Saves

JSON in `Application.persistentDataPath/oldgods/`, with a version field, a migration hook and corrupted-save recovery (keep the bad file as `.corrupt`, start fresh).

## Packages

Unity 6000.6.3f1 bundles Cinemachine 6.6.0, Burst 2.0.0, Collections 6.6.0 and Mathematics 1.4.0; those versions are used. `com.unity.pipeline` 0.8.0-exp.1 comes from the registry so the `unity` CLI can drive an open Editor.
