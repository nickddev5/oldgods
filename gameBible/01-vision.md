# 01 Vision

**Status:** PLACEHOLDER. Direction confirmed by Nick on 2026-10-09.

## Pitch

You are one of the old gods in the last days before the throne. Survive ~10-minute stages on a random 3D map against an endless horde, with weapons that fire on their own. Find the hidden portal, kill the stage boss, outlast the final swarm. Every run earns power that unlocks the rest of the pantheon. The last god to unlock is Elias, and only Elias can beat The Last Test, take the throne and end the age of the old gods.

## Pillars

1. **Movement is the skill.** Weapons aim themselves; the player wins by where they run, jump and slide. A slide builds speed downhill, and jumping out of it keeps that speed in the air. Releasing jump early gives a short hop, and falls are faster than rises. A long drop hurts but never kills: it leaves at least 1 health.
2. **Every level-up is a choice.** Three options, real trade-offs, a few rerolls.
3. **Short runs, long arc.** A run is ~30 minutes; the pantheon takes many runs to unlock.
4. **A straight myth.** The tone is serious and spare. No meme humour.

## Reference

Megabonk for structure and feel (3D horde survival, random maps, auto weapons, boss portal, final swarm). Differences: a straighter tone, a story arc that ends, and meta-progression that mostly unlocks content rather than raw power.

## Camera

Third-person chase camera behind and slightly above the player, shallow downward tilt (15 degrees by default, so the horizon and sky stay in view; Nick, 2026-10-09), short lag when turning, pulls in against walls. The player sits low and centred in frame. Pitch stays where the player puts it (6 to 60 degrees); only yaw swings back behind the direction of travel after a second without looking. Height changes are smoothed so jumps and slopes do not bounce the view. The player cannot leave the map: a wall partway up the rim stops them. The horde and bosses can climb the rim to the same wall, so standing on the slope is no refuge.

## Art direction

Low-poly 3D, flat shaded, a strong palette per biome. Enemy silhouettes read at distance. Effects are bright and short.

Each biome has one dominant mood colour that the fog, the sky horizon and the far hills share, so distance melts into it (PLACEHOLDER values in the biome assets). Characters, enemies and bosses carry a thin dark outline so a crowd reads against any ground; every surface carries a coarse pixel-art texture over its colour blocks (Nick, 2026-10-09), with bigger texels on the ground than on characters. A light bloom, colour grade and vignette sit over everything. Notes on what this borrows from Megabonk, and what it does not: [docs/art/megabonk-look.md](../docs/art/megabonk-look.md).
