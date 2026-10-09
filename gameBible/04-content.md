# 04 Content

**Status:** PLACEHOLDER. Names are working names; god names stay TBD canon.

## Counts (first milestone)

3 biomes, 3 biome bosses, The Last Test, 12 weapons, 12 passives, 10 enemy types, 3 minibosses, ~12 quests, 7 playable gods.

## Gods (working domains)

| # | Domain | Starting weapon | Passive |
|---|---|---|---|
| 1 | Storm | Chain Lightning | Swiftness |
| 2 | Forge | Hammer Orbit | Iron Skin |
| 3 | Tide | Tidal Wave | Regeneration |
| 4 | Hunt | Spear Volley | Keen Eye |
| 5 | Ember | Flame Aura | Fury |
| 6 | Earth | Quake | Bulwark |
| 7 | Elias (last unlock) | Crown Light | Resolve |

## Weapon shapes

`Projectile` (fires at the nearest enemy), `Aura` (damages around the player), `Area` (strikes a spot), `Orbit` (circles the player), `Pull` (drags enemies in). Each weapon is data: shape, cooldown, damage, count, size, pierce, and a per-level upgrade table.

## Biomes

1. **Grey Steppe:** rolling grassland, scattered standing stones. Boss: the Stone Warden.
2. **Ash Wood:** burnt forest, ridges and ravines. Boss: the Ash Stag.
3. **Drowned Coast:** cliffs over shallow water. Boss: the Tide Mother.

Final arena: **The Last Test**, a construct guarding the throne.

## Weapons (milestone 2)

| Weapon | Shape | Damage | Cooldown | Notes |
|---|---|---|---|---|
| Chain Lightning | Chain | 9 | 1.4 s | 3 jumps within 6 m |
| Hammer Orbit | Orbit | 12 | 0.5 s per foe | 2 hammers, heavy knockback |
| Tidal Wave | Projectile | 14 | 2.2 s | wide, pierces everything |
| Spear Volley | Projectile | 13 | 1.0 s | 2 spears, pierce 1 |
| Flame Aura | Aura | 4 | 0.45 s tick | 3 m ring |
| Quake | Area (on self) | 20 | 2.5 s | 5.5 m, big knockback |
| Crown Light | Area | 30 | 1.6 s | smites a nearby foe |
| Frost Shards | Projectile | 7 | 0.9 s | 3 shards, slow 1.5 s |
| Grave Pull | Pull | 5 per 0.5 s | 4 s | 2.5 s vortex |
| Bone Ring | Orbit | 6 | 0.4 s per foe | 4 bones, wide |
| Thunder Cloud | Area | 16 | 1.8 s | 2 strikes |
| Ember Rain | Area | 9 | 1.2 s | 4 small strikes |

## Passives (milestone 2)

Per Common level: Swiftness +8% move speed, Iron Skin +1 armour, Regeneration +0.3 health/s, Keen Eye +5% crit, Fury +8% damage, Bulwark +15 max health, Resolve +8% XP, Haste +8% attack speed, Reach +10% area, Multitude +1 projectile, Magnetism +1 m pickup range, Fortune +8% luck.

## Enemies

| Enemy | Health | Speed | Contact damage | XP |
|---|---|---|---|---|
| Husk | 12 | 3.3 | 4 | 1 |

Content assets live in `Assets/OldGods/Content/` and are the source of truth once created; `Old Gods > Build > Everything` only adds missing ones.
