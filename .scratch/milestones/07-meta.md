# Milestone 7: Meta-progression and saving

Branch: `milestone-7-meta`

## Scope

Currency, unlock tree, capped powerups, quests, difficulty modifiers; JSON save with version, migration and corruption recovery.

## Done when

Save/load/new-game tested; a scripted PlayMode run shows a fresh save can reach Elias.

## Progress

- [x] Rules: powerups, unlock tree, quests (cumulative and best-run), modifiers, run payout into the save; EditMode tests including a fresh-save progression check
- [x] Runs pay Embers into the save, record the last run and complete quests; results show quest completions and the balance
- [x] Shrine of Embers and Quests pages in the menu; modifier toggles on character select
- [x] Locked weapons, passives and items stay out of drafts, chests and the merchant until bought
- [x] PlayMode tests: a real winning run saves and reloads, repeated payouts reach Elias, a run as Elias starts; corrupt save recovery
- [ ] Nick's own fresh-save progression
