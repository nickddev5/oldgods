# 02 Run loop

**Status:** PLACEHOLDER.

1. **Character select.** Pick an unlocked god. Each god starts with one weapon and one passive.
2. **Stage (x3).** A random map in the stage's biome.
   - A stage is a 10:00 countdown (PLACEHOLDER).
   - Enemies spawn on a timeline: waves, minibosses at set times, swarms. Density grows with time and stage number.
   - Kill enemies, collect XP gems, level up, draft.
   - The map hides a **boss portal**. Entering it starts the stage boss early. Killing the boss opens the **next-stage portal**.
   - At 0:00 the **final swarm** begins: endless ghost waves. Surviving longer raises a survival multiplier on meta currency. Dying ends the run.
   - Map features: chests (rising price, free from bosses and elites), a merchant, a duplicator, shrines (Charge, Item, Greed, Boss Curse, Challenge, Magnet).
3. **The Last Test.** After the third stage, a final arena with a construct guarding the throne. Only Elias can win it; other gods can reach it and earn currency but cannot break it (PLACEHOLDER rule, see open questions).
4. **Ending.** Elias sits the throne; the old gods' power merges into him and they vanish.
5. **Results.** Seed, time, kills, level, currency earned, quests progressed.

Every run has one seed (`ulong`) split into streams: `map`, `spawns`, `draft`, `loot`, `shrines`. The seed is on the results screen and in the save for bug reports.
