# 03 Progression

**Status:** PLACEHOLDER numbers throughout.

## In a run

- **XP curve:** level n needs `5 + 4 * (n - 1) ^ 1.25` XP, rounded (PLACEHOLDER; flattened 2026-10-09 after the autoplay balance check showed level 6 at eight minutes).
- **Level-up draft:** the game pauses and offers 3 choices: a new weapon, a new passive, or a level for one already owned.
  - Slots: 4 weapons, 4 passives (PLACEHOLDER).
  - Weapons and passives max at level 8.
  - Rarity: Common, Uncommon, Rare, Epic, Legendary, with base weights 60/25/10/4/1. An upgrade's step is multiplied by 1, 1.25, 1.6, 2 or 2.6; Epic and Legendary weapon upgrades roll two stats. Whole-number stats (count, pierce) give +1, or +2 at Epic and above. Luck multiplies each tier's weight by (1 + luck)^(1.5 x tier).
  - New weapons and passives are always Common; owned items that can still level are three times as likely to be dealt as new ones.
  - When nothing can be dealt, a single Restore card heals 30% of max health.
  - Charges per run: 2 Refresh (reroll all three, never dealing the same three again), 2 Skip (take nothing, gain 20% of the level's XP), 1 Banish (remove one card's item from the rest of the run and deal a replacement).
  - Controls: click, 1-3, or gamepad to pick; R / Y to refresh, Q / LB to skip, X / RB then a card to banish.
- **Gold:** dropped by most enemies (each type has a gold chance: common foes 25%, brutes 50%, hulks and shell brutes 60%, champions always, ghosts never), 1 gold per drop times (stage + 1), 12 from elites, times Gold Gain. Spent on chests, the merchant and the duplicator. Their prompts show the price even when you cannot afford it yet. Gold is never kept between runs.
- **Chests:** about 10 per map. The nth paid chest costs round((12 + 12 x n^1.35) x (1 + 0.25 x stage)). Bosses, cursed bosses and champions drop free chests. A chest gives one item.
- **Items:** stack without slots; rarity weights 55/28/12/4/1 shifted by Luck. Twelve PLACEHOLDER items, all stat changes except Bloodstone (8% chance per kill to heal 2).
- **Merchant:** one per map, sells one item (Uncommon or better) for 1.2 + 0.45 x rarity tier times the chest price.
- **Duplicator:** one per map, copies the last item found for the chest price, once.
- **Drops:** common enemies drop a healing morsel (20 health) 0.6% of the time and a magnet that pulls every gem 0.15% of the time.
- **Shrines (8 per map, each kind at least once):**
  - Charge: stand in its 5 m ring for 5 seconds (it drains slowly when you leave), then pick one of three stat boosts with rolled rarity.
  - Gifts (Item): costs 20% of max health, gives an item of Uncommon or better.
  - Greed: this stage's foes +15% density and health, gold +25%. Stacks.
  - Curse (Boss Curse): one more boss wakes at the gate, and each boss drops a chest.
  - Challenge: calls a champion now; champions drop a chest.
  - Drawing (Magnet): pulls every gem and coin on the map to you.

## Between runs

- **Currency:** "Embers" (working name), paid at the end of a run: 20 per stage cleared, 30 per boss, 1 per 30 kills, 40 for reaching The Last Test, 100 for winning; times the best final-swarm survival multiplier; times (1 + difficulty modifier bonus). Shown on the results screen with the seed.
- **Shrine of Embers (menu):** spend Embers on the unlock tree and powerups.
- **Unlock tree:** gods in order (Forge 150, Tide 300, Hunt 500, Ember 750, Earth 1000; Elias 1500 once all six are owned), five weapons (Frost Shards 100, Bone Ring 100, Grave Pull 150, Ember Rain 150, Thunder Cloud 200), three passives (Magnetism 80, Fortune 100, Multitude 200) and three items (Bloodstone 150, Quiver of Dawn 200, Winged Crown 250). Locked content never appears in drafts, chests or the merchant. Most of the tree is content, not power.
- **Powerups:** six capped stat buys, five levels each, costing base x next level: Vitality +5 health (40), Might +3% damage (60), Stride +2% speed (50), Reach of Hand +0.3 m pickup (30), Favour +2% luck (60), Insight +3% XP (50).
- **Quests (12):** First Steps (clear a stage, 30), Slayer (1000 kills, 50), Opener (25 chests, 60), Devout (20 shrines, 60), Guardians' Bane (5 bosses, 80), Swarm Walker (60 s of a final swarm in one run, 80), The Deep Road (reach The Last Test, 100), Hoarder (2000 gold, 60), Ascendant (level 30 in one run, 80), Collector (15 items in one run, 80), Slaughter (1500 kills in one run, 100), The Throne (win as Elias, 200). Totals add up across runs unless marked "in one run". Rewards are Embers.
- **Difficulty modifiers (character select):** Hardened (foes +30% health and numbers, +25% Embers), Swift Horde (foes +15% speed, +20%), Frail (-30% max health, +25%), No Second Thoughts (no refresh, skip or banish, +15%). Bonuses add.
- **Elias:** unlocks only when every other god is unlocked. A typical winning run pays about 400 Embers, so a player who wins reaches Elias in roughly ten wins (checked by the FreshSaveReachesEliasByPlaying test).
- **Save:** JSON at persistentDataPath/oldgods/save.json with a version number and migration step; written through a temp file; an unreadable save is kept as .corrupt and a fresh one starts.

No banking: currency is awarded at the end of a run, never stored mid-run.
