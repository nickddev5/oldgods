using System;
using System.Collections.Generic;
using System.Linq;

namespace OldGods.Rules
{
    /// <summary>A playable god. Names are working domains until the canon names are decided.</summary>
    public sealed class GodDef
    {
        public string Id;
        public string Name;
        public string Lore;
        public string StartingWeapon;
        public string StartingPassive;
        public List<StatMod> Kit = new List<StatMod>();
        /// <summary>Position in the unlock order; 0 is unlocked from the start.</summary>
        public int Order;
        /// <summary>Embers to unlock; 0 for the first god.</summary>
        public int Cost;
        /// <summary>Elias: unlocks only when every other god is unlocked, and takes the throne.</summary>
        public bool IsLast;
    }

    public static class GodRules
    {
        public static string UnlockId(GodDef god) => god.Id;

        /// <summary>The god is unlocked (playable) for this save.</summary>
        public static bool IsUnlocked(GodDef god, Func<string, bool> owned) => god.Cost <= 0 || owned(UnlockId(god));

        /// <summary>
        /// Whether the god can be bought now: not owned, every earlier god in the order owned,
        /// and for the last god, every other god owned.
        /// </summary>
        public static bool CanUnlock(GodDef god, IReadOnlyList<GodDef> all, Func<string, bool> owned)
        {
            if (IsUnlocked(god, owned)) return false;
            if (god.IsLast) return all.Where(g => !g.IsLast).All(g => IsUnlocked(g, owned));
            return all.Where(g => g.Order < god.Order && !g.IsLast).All(g => IsUnlocked(g, owned));
        }

        /// <summary>Gods in select-screen order.</summary>
        public static List<GodDef> Ordered(IEnumerable<GodDef> gods) => gods.OrderBy(g => g.IsLast ? int.MaxValue : g.Order).ToList();

        /// <summary>The god a run uses when none was chosen: the first unlocked in order.</summary>
        public static GodDef Default(IReadOnlyList<GodDef> all, Func<string, bool> owned) =>
            Ordered(all).FirstOrDefault(g => IsUnlocked(g, owned));
    }
}
