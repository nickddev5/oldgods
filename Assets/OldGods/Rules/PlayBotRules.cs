using System;
using System.Collections.Generic;
using System.Linq;

namespace OldGods.Rules
{
    /// <summary>How the play bot takes level-up cards.</summary>
    public enum BotPicks
    {
        Smart,  // weighs kind and rarity, with a seeded tie-break so seeds try different builds
        First,  // always the first card, like the old autoplay
        Random, // any card, seeded
    }

    /// <summary>
    /// The play bot's choices that do not need the engine: which card to take, what to buy
    /// between runs, and when it is stuck. Kept here so they are unit-tested.
    /// </summary>
    public static class PlayBotRules
    {
        public static BotPicks ParsePicks(string text)
        {
            if (string.IsNullOrEmpty(text)) return BotPicks.Smart;
            foreach (BotPicks p in Enum.GetValues(typeof(BotPicks)))
                if (string.Equals(p.ToString(), text, StringComparison.OrdinalIgnoreCase)) return p;
            return BotPicks.Smart;
        }

        /// <summary>
        /// A card's worth to the smart bot: new weapons first while slots are free, then
        /// weapon upgrades, then passives. Rarer is better. Restore only when hurt.
        /// </summary>
        public static float Score(DraftOption o, Loadout loadout, float healthFraction)
        {
            float rarity = 12f * (int)o.Rarity;
            switch (o.Kind)
            {
                case DraftKind.Restore: return healthFraction < 0.5f ? 300f : 1f;
                case DraftKind.NewWeapon: return (loadout.Weapons.Count < loadout.WeaponSlots ? 95f : 0f) + rarity;
                case DraftKind.UpgradeWeapon: return 70f + rarity;
                case DraftKind.NewPassive: return (loadout.Passives.Count < loadout.PassiveSlots ? 55f : 0f) + rarity;
                case DraftKind.UpgradePassive: return 45f + rarity;
                default: return 0f;
            }
        }

        /// <summary>Index of the card to take. Jitter up to 25 points lets different seeds try different builds.</summary>
        public static int PickDraft(IReadOnlyList<DraftOption> options, Loadout loadout, float healthFraction, BotPicks policy, Rng rng)
        {
            if (options == null || options.Count == 0) return 0;
            if (policy == BotPicks.First) return 0;
            if (policy == BotPicks.Random) return rng.Range(0, options.Count);
            int best = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < options.Count; i++)
            {
                float s = Score(options[i], loadout, healthFraction) + rng.Range(0f, 25f);
                if (s > bestScore) { bestScore = s; best = i; }
            }
            return best;
        }

        /// <summary>One thing the bot buys between runs: an unlock or a powerup level.</summary>
        public sealed class Purchase
        {
            public UnlockEntry Unlock;
            public PowerupDef Powerup;
            public int Cost;
            public string Name => Unlock != null ? Unlock.Name : Powerup.Name;
        }

        /// <summary>
        /// The next thing to buy with the save's Embers, or null. Gods come first: while the next
        /// god is out of reach the bot saves for it and spends only what is above its price, on
        /// the cheapest other unlock or powerup level.
        /// </summary>
        public static Purchase NextPurchase(SaveData save, IReadOnlyList<UnlockEntry> tree, IReadOnlyList<GodDef> gods)
        {
            var nextGod = tree.Where(e => e.Kind == UnlockKind.God && !save.IsUnlocked(e.Id) && GodRules.CanUnlock(e.God, gods, save.IsUnlocked))
                .OrderBy(e => e.Cost).FirstOrDefault();
            if (nextGod != null && nextGod.Cost <= save.currency) return new Purchase { Unlock = nextGod, Cost = nextGod.Cost };
            int spare = save.currency - (nextGod != null ? nextGod.Cost : 0);

            var other = tree.Where(e => e.Kind != UnlockKind.God && MetaRules.CanUnlock(save, e, gods)).OrderBy(e => e.Cost).FirstOrDefault();
            PowerupDef power = null;
            int powerCost = int.MaxValue;
            foreach (var p in MetaCatalog.Powerups)
            {
                int c = MetaRules.PowerupCost(p, MetaRules.PowerupLevel(save, p.Id));
                if (c < powerCost) { powerCost = c; power = p; }
            }
            if (other != null && other.Cost <= powerCost && other.Cost <= spare) return new Purchase { Unlock = other, Cost = other.Cost };
            if (power != null && powerCost <= spare) return new Purchase { Powerup = power, Cost = powerCost };
            return null;
        }

        /// <summary>Buys until nothing more fits. Returns what was bought, in order.</summary>
        public static List<Purchase> Spend(SaveData save, IReadOnlyList<UnlockEntry> tree, IReadOnlyList<GodDef> gods)
        {
            var bought = new List<Purchase>();
            for (int guard = 0; guard < 200; guard++)
            {
                var p = NextPurchase(save, tree, gods);
                if (p == null) break;
                bool ok = p.Unlock != null ? MetaRules.TryUnlock(save, p.Unlock, gods) : MetaRules.TryBuyPowerup(save, p.Powerup);
                if (!ok) break;
                bought.Add(p);
            }
            return bought;
        }
    }

    /// <summary>
    /// Notices when the bot wants to move but does not: a short stall asks for a jump, a long
    /// one is reported as stuck. Call Step every frame.
    /// </summary>
    public sealed class StuckWatch
    {
        public float Window = 0.75f;      // seconds between progress checks
        public float MinProgress = 0.6f;  // metres expected per window while trying to move
        public int StuckAfter = 4;        // windows without progress before it counts as stuck

        float x0, z0, timer;
        bool started, wantedAll = true;

        public int StalledWindows { get; private set; }
        /// <summary>True on the step a stall starts: try a jump.</summary>
        public bool JustStalled { get; private set; }
        /// <summary>True each time the stall lasts another StuckAfter windows: detour again.</summary>
        public bool JustStuck { get; private set; }
        /// <summary>True only the first time a stall counts as stuck: report it once.</summary>
        public bool FirstStuck => JustStuck && StalledWindows == StuckAfter;

        public void Reset() { started = false; wantedAll = true; StalledWindows = 0; timer = 0f; }

        public void Step(float x, float z, bool wantsToMove, float dt)
        {
            JustStalled = JustStuck = false;
            if (!started) { x0 = x; z0 = z; started = true; timer = 0f; return; }
            timer += dt;
            wantedAll &= wantsToMove;
            if (timer < Window) return;
            // Only a window spent wanting to move all the way through can count as a stall.
            bool wanted = wantedAll;
            timer = 0f;
            wantedAll = true;
            float dx = x - x0, dz = z - z0;
            x0 = x; z0 = z;
            if (!wanted || dx * dx + dz * dz >= MinProgress * MinProgress)
            {
                StalledWindows = 0;
                return;
            }
            StalledWindows++;
            if (StalledWindows == 1) JustStalled = true;
            if (StalledWindows % StuckAfter == 0) JustStuck = true;
        }
    }
}
