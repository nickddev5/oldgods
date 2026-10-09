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

1. **The Grey Steppe:** rolling grassland (hills 14 m, few cliffs), 110 standing stones, cool light. Enemies: husk, runner, brute; champion: Husk Champion. Boss: the Stone Warden.
2. **The Ash Wood:** burnt forest on ridges and ravines (hills 22 m, many cliffs), 140 dead trees, low red sun, warm haze. Enemies: cinder husk, ashling, charred hulk; champion: Ash Champion. Boss: the Ash Stag.
3. **The Drowned Coast:** sand and wet rock with cliffs over shallow water (water at 6.5 m). Enemies: drowned, brine runner, shell brute; champion: Tide Champion. Boss: the Tide Mother.

Final arena: **The Last Test**, a flat marble arena (128 m) with the throne and six stone gods; the construct guards it.

All three stages use the standard ten-minute timeline with the biome's own basic, fast, tank and champion enemies.

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

## Items (milestone 4)

| Item | Rarity | Effect per copy |
|---|---|---|
| Whetstone | Common | +6% damage |
| Runner's Sandals | Common | +6% move speed |
| Heart of Oak | Common | +15 max health |
| Lodestone | Common | +1.2 m pickup range |
| War Drum | Uncommon | +8% attack speed |
| Golden Thread | Uncommon | +8% luck, +10% gold |
| Wide Horn | Uncommon | +10% area |
| Iron Collar | Rare | +2 armour, +10 max health |
| Hourglass | Rare | +15% duration, +6% XP |
| Bloodstone | Epic | 8% chance per kill to heal 2, +0.2 regeneration |
| Quiver of Dawn | Epic | +1 projectile |
| Winged Crown | Legendary | +1 jump, +8% move speed, +6% crit |

## Enemies

| Enemy | Health | Speed | Contact damage | XP |
|---|---|---|---|---|
| Husk | 12 | 3.3 | 4 | 1 |
| Runner | 7 | 5.6 | 3 | 1 |
| Brute | 55 | 2.6 | 9 | 4 |
| Husk Champion (miniboss) | 600 | 3.2 | 14 | 30 |
| Ghost (final swarm) | 20 | 5.0 | 8 | 2 |
| Ashling | 6 | 6.2 | 3 | 1 |
| Cinder Husk | 14 | 3.6 | 6 | 2 |
| Charred Hulk | 60 | 2.4 | 12 | 6 |
| Drowned | 16 | 3.0 | 7 | 2 |
| Brine Runner | 9 | 6.0 | 5 | 2 |
| Shell Brute | 75 | 2.2 | 15 | 8 |
| Ash Champion (miniboss) | 700 | 3.6 | 16 | 40 |
| Tide Champion (miniboss) | 800 | 3.0 | 18 | 50 |

Health is multiplied by 1 + 1.1 x stage index, so the same numbers mean more in later biomes.

## Bosses

**The Stone Warden** (greybox stage): 3000 health (x 1 + 1.4 x stage), slam (24, 4.5 m circle, 1.1 s warning), charge (20, 16 m line, 0.9 s warning), shockwave (18, ring to 22 m, jump it), summon (8 husks). Enrages under 35% health and rests half as long. The boss gate is a stone arch; the boss rises beside it.

**The Ash Stag** (Ash Wood): 3200 base health, fast; charge (24, 22 m), volley (20, five 3.5 m circles), shockwave (20), summons ashlings.

**The Tide Mother** (Drowned Coast): 3400 base health, slow; volley (26), summons drowned, slam (30, 5.5 m), shockwave (24).

**The Last Test** (final arena): 4000 base health (x 5.2 for its place after stage 3), every attack, rests 0.9 s, enrages under 50%. Boss health is multiplied by 1 + 1.4 x stage index.

Content assets live in `Assets/OldGods/Content/` and are the source of truth once created; `Old Gods > Build > Everything` only adds missing ones.
