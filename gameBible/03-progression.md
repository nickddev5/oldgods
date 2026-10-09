# 03 Progression

**Status:** PLACEHOLDER numbers throughout.

## In a run

- **XP curve:** level n needs `5 + 10 * (n - 1) ^ 1.3` XP, rounded (PLACEHOLDER).
- **Level-up draft:** the game pauses and offers 3 choices: a new weapon, a new passive, or a level for one already owned.
  - Slots: 4 weapons, 4 passives (PLACEHOLDER).
  - Weapons and passives max at level 8.
  - Rarity: Common, Uncommon, Rare, Epic, Legendary, with base weights 60/25/10/4/1. An upgrade's step is multiplied by 1, 1.25, 1.6, 2 or 2.6; Epic and Legendary weapon upgrades roll two stats. Whole-number stats (count, pierce) give +1, or +2 at Epic and above. Luck multiplies each tier's weight by (1 + luck)^(1.5 x tier).
  - New weapons and passives are always Common; owned items that can still level are three times as likely to be dealt as new ones.
  - When nothing can be dealt, a single Restore card heals 30% of max health.
  - Charges per run: 2 Refresh (reroll all three, never dealing the same three again), 2 Skip (take nothing, gain 20% of the level's XP), 1 Banish (remove one card's item from the rest of the run and deal a replacement).
  - Controls: click, 1-3, or gamepad to pick; R / Y to refresh, Q / LB to skip, X / RB then a card to banish.
- **Gold:** dropped by some enemies (each type has a gold chance; brutes 10%, champions always), 1 gold per drop times (stage + 1), 12 from elites, times Gold Gain. Spent on chests, the merchant and the duplicator. Gold is never kept between runs.
- **Chests:** about 10 per map. The nth paid chest costs round((15 + 12 x n^1.35) x (1 + 0.25 x stage)). Bosses, cursed bosses and champions drop free chests. A chest gives one item.
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
- **Unlock tree:** gods, weapons, passives and items enter the pool when bought. Most of the tree is content, not power.
- **Powerups:** a few small, capped stat buys (max health, damage, pickup range, move speed, luck, XP gain).
- **Quests:** about 12 goals ("open 25 chests", "beat a boss without being hit") that grant currency or unlock items.
- **Difficulty modifiers:** optional; raise enemy health or density for a higher currency payout.
- **Elias:** unlocks only when every other god is unlocked.

No banking: currency is awarded at the end of a run, never stored mid-run.
