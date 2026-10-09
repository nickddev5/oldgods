using System;
using System.Collections.Generic;
using System.Linq;

namespace OldGods.Rules
{
    /// <summary>A small, capped permanent stat buy. PLACEHOLDER numbers.</summary>
    public sealed class PowerupDef
    {
        public string Id;
        public string Name;
        public StatMod PerLevel;
        public int MaxLevel;
        public int BaseCost;

        public PowerupDef(string id, string name, StatMod perLevel, int maxLevel, int baseCost)
        {
            Id = id; Name = name; PerLevel = perLevel; MaxLevel = maxLevel; BaseCost = baseCost;
        }
    }

    public enum UnlockKind { God, Weapon, Passive, Item }

    /// <summary>Something in the unlock tree: a god, or content that enters the draft and chests.</summary>
    public sealed class UnlockEntry
    {
        public string Id;
        public string Name;
        public UnlockKind Kind;
        public int Cost;
        public GodDef God; // set for gods, which have ordering rules
    }

    public enum QuestStat { StagesCleared, Kills, ChestsOpened, ShrinesUsed, BossesKilled, SwarmSeconds, ReachedThrone, GoldEarned, Level, ItemsFound, RunKills, WonAsElias }

    public sealed class QuestDef
    {
        public string Id;
        public string Name;
        public string Description;
        public QuestStat Stat;
        /// <summary>True: best single run counts. False: totals add up across runs.</summary>
        public bool BestRun;
        public int Target;
        public int Reward;
    }

    /// <summary>Opt-in difficulty for a higher Embers payout.</summary>
    public sealed class ModifierDef
    {
        public string Id;
        public string Name;
        public string Description;
        public float EnemyHealthAndDensity;
        public float EnemySpeed;
        public float PlayerMaxHealthPercent;
        public bool NoDraftCharges;
        public float Payout;
    }

    public static class MetaCatalog
    {
        public static readonly PowerupDef[] Powerups =
        {
            new PowerupDef("power.health", "Vitality", new StatMod(StatId.MaxHealth, 5f), 5, 40),
            new PowerupDef("power.damage", "Might", new StatMod(StatId.Damage, 0.03f), 5, 60),
            new PowerupDef("power.speed", "Stride", new StatMod(StatId.MoveSpeed, 0.02f), 5, 50),
            new PowerupDef("power.pickup", "Reach of Hand", new StatMod(StatId.PickupRange, 0.3f), 5, 30),
            new PowerupDef("power.luck", "Favour", new StatMod(StatId.Luck, 0.02f), 5, 60),
            new PowerupDef("power.xp", "Insight", new StatMod(StatId.XpGain, 0.03f), 5, 50),
        };

        public static readonly QuestDef[] Quests =
        {
            new QuestDef { Id = "quest.first_steps", Name = "First Steps", Description = "Clear a stage", Stat = QuestStat.StagesCleared, Target = 1, Reward = 30 },
            new QuestDef { Id = "quest.slayer", Name = "Slayer", Description = "Defeat 1000 foes", Stat = QuestStat.Kills, Target = 1000, Reward = 50 },
            new QuestDef { Id = "quest.opener", Name = "Opener", Description = "Open 25 chests", Stat = QuestStat.ChestsOpened, Target = 25, Reward = 60 },
            new QuestDef { Id = "quest.devout", Name = "Devout", Description = "Use 20 shrines", Stat = QuestStat.ShrinesUsed, Target = 20, Reward = 60 },
            new QuestDef { Id = "quest.guardians_bane", Name = "Guardians' Bane", Description = "Defeat 5 bosses", Stat = QuestStat.BossesKilled, Target = 5, Reward = 80 },
            new QuestDef { Id = "quest.swarm_walker", Name = "Swarm Walker", Description = "Survive 60 seconds of a final swarm", Stat = QuestStat.SwarmSeconds, BestRun = true, Target = 60, Reward = 80 },
            new QuestDef { Id = "quest.deep_road", Name = "The Deep Road", Description = "Reach The Last Test", Stat = QuestStat.ReachedThrone, BestRun = true, Target = 1, Reward = 100 },
            new QuestDef { Id = "quest.hoarder", Name = "Hoarder", Description = "Gather 2000 gold", Stat = QuestStat.GoldEarned, Target = 2000, Reward = 60 },
            new QuestDef { Id = "quest.ascendant", Name = "Ascendant", Description = "Reach level 30 in one run", Stat = QuestStat.Level, BestRun = true, Target = 30, Reward = 80 },
            new QuestDef { Id = "quest.collector", Name = "Collector", Description = "Find 15 items in one run", Stat = QuestStat.ItemsFound, BestRun = true, Target = 15, Reward = 80 },
            new QuestDef { Id = "quest.slaughter", Name = "Slaughter", Description = "Defeat 1500 foes in one run", Stat = QuestStat.RunKills, BestRun = true, Target = 1500, Reward = 100 },
            new QuestDef { Id = "quest.the_throne", Name = "The Throne", Description = "Win as Elias", Stat = QuestStat.WonAsElias, BestRun = true, Target = 1, Reward = 200 },
        };

        public static readonly ModifierDef[] Modifiers =
        {
            new ModifierDef { Id = "mod.hardened", Name = "Hardened", Description = "Foes are 30% tougher and 30% more numerous", EnemyHealthAndDensity = 0.3f, Payout = 0.25f },
            new ModifierDef { Id = "mod.swift", Name = "Swift Horde", Description = "Foes move 15% faster", EnemySpeed = 0.15f, Payout = 0.2f },
            new ModifierDef { Id = "mod.frail", Name = "Frail", Description = "30% less max health", PlayerMaxHealthPercent = -0.3f, Payout = 0.25f },
            new ModifierDef { Id = "mod.no_choices", Name = "No Second Thoughts", Description = "No refresh, skip or banish", NoDraftCharges = true, Payout = 0.15f },
        };

        public static ModifierDef Modifier(string id) => Modifiers.FirstOrDefault(m => m.Id == id);
    }

    public static class MetaRules
    {
        public static int PowerupLevel(SaveData save, string id) => save.powerups.FirstOrDefault(p => p.id == id)?.level ?? 0;

        /// <summary>Cost of the next level: base x (level + 1). Int.MaxValue when maxed.</summary>
        public static int PowerupCost(PowerupDef def, int level) => level >= def.MaxLevel ? int.MaxValue : def.BaseCost * (level + 1);

        public static bool TryBuyPowerup(SaveData save, PowerupDef def)
        {
            int level = PowerupLevel(save, def.Id);
            int cost = PowerupCost(def, level);
            if (level >= def.MaxLevel || save.currency < cost) return false;
            save.currency -= cost;
            var entry = save.powerups.FirstOrDefault(p => p.id == def.Id);
            if (entry == null) save.powerups.Add(new PowerupLevel { id = def.Id, level = 1 });
            else entry.level++;
            return true;
        }

        public static IEnumerable<StatMod> PowerupMods(SaveData save)
        {
            foreach (var def in MetaCatalog.Powerups)
            {
                int level = PowerupLevel(save, def.Id);
                if (level > 0) yield return def.PerLevel.Scaled(level);
            }
        }

        /// <summary>The unlock tree for a content set: gods by their rules, other content by cost.</summary>
        public static List<UnlockEntry> Tree(ContentSet content, Func<string, int> costOf)
        {
            var list = new List<UnlockEntry>();
            foreach (var g in GodRules.Ordered(content.Gods))
                if (g.Cost > 0) list.Add(new UnlockEntry { Id = GodRules.UnlockId(g), Name = g.Name, Kind = UnlockKind.God, Cost = g.Cost, God = g });
            foreach (var w in content.Weapons)
                if (!string.IsNullOrEmpty(w.UnlockId)) list.Add(new UnlockEntry { Id = w.UnlockId, Name = w.Name, Kind = UnlockKind.Weapon, Cost = costOf(w.UnlockId) });
            foreach (var p in content.Passives)
                if (!string.IsNullOrEmpty(p.UnlockId)) list.Add(new UnlockEntry { Id = p.UnlockId, Name = p.Name, Kind = UnlockKind.Passive, Cost = costOf(p.UnlockId) });
            foreach (var i in content.Items)
                if (!string.IsNullOrEmpty(i.UnlockId)) list.Add(new UnlockEntry { Id = i.UnlockId, Name = i.Name, Kind = UnlockKind.Item, Cost = costOf(i.UnlockId) });
            return list;
        }

        public static bool CanUnlock(SaveData save, UnlockEntry e, IReadOnlyList<GodDef> gods)
        {
            if (save.IsUnlocked(e.Id) || save.currency < e.Cost) return false;
            return e.Kind != UnlockKind.God || GodRules.CanUnlock(e.God, gods, save.IsUnlocked);
        }

        public static bool TryUnlock(SaveData save, UnlockEntry e, IReadOnlyList<GodDef> gods)
        {
            if (!CanUnlock(save, e, gods)) return false;
            save.currency -= e.Cost;
            save.Unlock(e.Id);
            return true;
        }

        public static float PayoutBonus(IEnumerable<string> modifierIds) =>
            modifierIds.Select(MetaCatalog.Modifier).Where(m => m != null).Sum(m => m.Payout);

        static int QuestValue(QuestStat stat, RunSummary s)
        {
            switch (stat)
            {
                case QuestStat.StagesCleared: return s.StagesCleared;
                case QuestStat.Kills: return s.Kills;
                case QuestStat.ChestsOpened: return s.ChestsOpened;
                case QuestStat.ShrinesUsed: return s.ShrinesUsed;
                case QuestStat.BossesKilled: return s.BossesKilled;
                case QuestStat.SwarmSeconds: return (int)s.BestSwarmSeconds;
                case QuestStat.ReachedThrone: return s.ReachedThrone ? 1 : 0;
                case QuestStat.GoldEarned: return s.GoldEarned;
                case QuestStat.Level: return s.Level;
                case QuestStat.ItemsFound: return s.ItemsFound;
                case QuestStat.RunKills: return s.Kills;
                case QuestStat.WonAsElias: return s.Won && LastTest.TakesTheThrone(s.GodId) ? 1 : 0;
                default: return 0;
            }
        }

        public static int QuestProgressValue(SaveData save, QuestDef q) => save.quests.FirstOrDefault(p => p.id == q.Id)?.value ?? 0;

        /// <summary>
        /// Pays a finished run into the save: Embers, records, and quest progress. Quests that
        /// complete pay their reward too. Returns the quests completed by this run.
        /// </summary>
        public static List<QuestDef> PayRun(SaveData save, RunSummary s, int embers)
        {
            save.currency += embers;
            save.lifetimeCurrency += embers;
            if (s.Won) save.runsWon++;
            save.lastRun = new RunRecord
            {
                seed = s.Seed, god = s.GodId, stageReached = s.ReachedThrone ? 4 : s.StagesCleared + 1, timeSeconds = s.Seconds,
                kills = s.Kills, level = s.Level, currencyEarned = embers, won = s.Won,
            };

            var done = new List<QuestDef>();
            foreach (var q in MetaCatalog.Quests)
            {
                if (save.completedQuests.Contains(q.Id)) continue;
                int add = QuestValue(q.Stat, s);
                var entry = save.quests.FirstOrDefault(p => p.id == q.Id);
                if (entry == null) save.quests.Add(entry = new QuestProgress { id = q.Id, value = 0 });
                entry.value = q.BestRun ? Math.Max(entry.value, add) : entry.value + add;
                if (entry.value >= q.Target)
                {
                    save.completedQuests.Add(q.Id);
                    save.currency += q.Reward;
                    save.lifetimeCurrency += q.Reward;
                    done.Add(q);
                }
            }
            return done;
        }
    }
}
