# 02 Run loop

**Status:** PLACEHOLDER.

1. **Character select.** Pick an unlocked god. Each god starts with one weapon and one passive.
2. **Stage (x3).** A random map in the stage's biome.
   - A stage is a 10:00 countdown (PLACEHOLDER).
   - Enemies spawn on a timeline: waves, minibosses at set times, swarms. Density grows with time and stage number.
   - Kill enemies, collect XP gems, level up, draft.
   - The map hides a **boss portal**. Entering it starts the stage boss early. Killing the boss opens the **next-stage portal**.
   - At 0:00 the **final swarm** begins: endless ghost waves that grow in number and health. Every full 30 seconds survived adds +25% to the stage's currency (survival multiplier). The boss gate and the next-stage portal still work, so the player chooses when to leave. Dying ends the run.
   - The boss gate is a stone arch at least 70 m from the start. Holding Interact at it for 1 second wakes the boss beside it. The minimap shows the gate once the player has been within 30 m of it.
   - Stage clock (greybox timeline, PLACEHOLDER): enemies kept alive climb from 15 to 220 over ten minutes across four phases; a runner swarm at 1:30, champions at 2:30, 5:30 and 8:30, a brute elite wave at 4:30, a husk swarm at 7:00. Enemy health grows 18% per minute; later stages multiply health by 1 + 1.1 x stage, density by 1 + 0.3 x stage and damage by 1 + 0.5 x stage.
   - Map features: chests (rising price, free from bosses and elites), a merchant, a duplicator, shrines (Charge, Item, Greed, Boss Curse, Challenge, Magnet).
3. **The Last Test.** The third stage's portal leads to a marble arena: the empty throne on a stepped dais, six old gods in stone behind it. The construct wakes four seconds after arrival. There is no clock and no final swarm here, only a thin stream of ghosts and husks.
4. **Ending.** Any god can break The Last Test, and that wins the run. If the god is Elias, he climbs the dais and sits; light leaves each statue for him and the statues vanish. Any other god walks to the foot of the throne and it does not answer them; the run still counts as won (decision 2026-10-09, see open questions).
5. **Results.** Seed, time, kills, level, currency earned, quests progressed.

Every run has one seed (`ulong`) split into streams: `map`, `spawns`, `draft`, `loot`, `shrines`. The seed is on the results screen and in the save for bug reports.
