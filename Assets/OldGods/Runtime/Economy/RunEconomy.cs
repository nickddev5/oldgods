using System.Collections.Generic;
using System.Linq;
using OldGods.Rules;
using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>
    /// Gold, items, chests and drops for one run. Hooks into enemy deaths and pickups;
    /// the chests, merchant, duplicator and shrines call in here to spend and grant.
    /// </summary>
    public sealed class RunEconomy : MonoBehaviour
    {
        public static RunEconomy Instance { get; private set; }

        public readonly Wallet Wallet = new Wallet();
        public int PaidChestsOpened { get; private set; }
        public int ChestsOpened { get; private set; }
        public int ShrinesUsed { get; private set; }
        public readonly HashSet<ShrineKind> ShrineKindsUsed = new HashSet<ShrineKind>();
        /// <summary>Extra gold multiplier from Greed shrines this stage.</summary>
        public float GreedGold { get; set; }
        /// <summary>Gold offerings made at Shrines of Offering this run.</summary>
        public int Offerings { get; set; }

        RunController run;
        Rng loot;
        List<ItemDef> pool;

        public event System.Action<ItemDef> ItemGranted;

        public void Init(RunController r)
        {
            Instance = this;
            run = r;
            loot = r.Seed.Stream(RunSeed.Loot);
            var save = SaveStore.Current;
            pool = r.Content.Items.Where(i => string.IsNullOrEmpty(i.UnlockId) || save.IsUnlocked(i.UnlockId)).ToList();
            r.Horde.EnemyKilled += OnEnemyKilled;
            r.PickedUp += OnPickedUp;
            r.StageStarted += _ => GreedGold = 0f;
            Hud.ExtraLine = () => $"Gold {Wallet.Gold}";
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Hud.ExtraLine = null;
        }

        float GoldGain => (run.Combat != null ? run.Combat.Stats.Value(StatId.GoldGain) : 1f) + GreedGold;
        float Luck => run.Combat != null ? run.Combat.Stats.Value(StatId.Luck) : 0f;

        public int ChestPrice => EconomyRules.ChestPrice(PaidChestsOpened, run.StageIndex);

        void OnEnemyKilled(Vector3 at, EnemyDef def, bool byPlayer)
        {
            if (!byPlayer) return;
            // Champions (elites that are not bosses) always leave a free chest.
            if (def.IsElite && !def.Id.StartsWith("boss.")) Features.DropFreeChest(run, at, run.WorldRoot);
            if (def.GoldChance > 0f && loot.Chance(def.GoldChance))
                run.Pickups.Spawn(PickupKind.Gold, at + Vector3.right * 0.4f, EconomyRules.GoldDrop(def.IsElite, run.StageIndex));
            if (!def.IsElite)
            {
                if (loot.Chance(EconomyRules.HealDropChance)) run.Pickups.Spawn(PickupKind.Heal, at, 20f);
                else if (loot.Chance(EconomyRules.MagnetDropChance)) run.Pickups.Spawn(PickupKind.Magnet, at, 0f);
            }
            float heal = run.Combat.Items.Special("heal_on_kill");
            if (heal > 0f && loot.Chance(heal)) run.PlayerHealth.Health.Heal(2f);
        }

        void OnPickedUp(PickupKind kind, float amount)
        {
            if (kind == PickupKind.Gold) Wallet.Add(EconomyRules.ApplyGoldGain(amount, GoldGain));
        }

        public void AddGold(int amount) => Wallet.Add(EconomyRules.ApplyGoldGain(amount, GoldGain));

        public ItemDef RollItem(Rarity minimum = Rarity.Common) => EconomyRules.RollItem(pool, loot, Luck, minimum);

        public void Grant(ItemDef item)
        {
            if (item == null) return;
            run.Combat.Items.Add(item);
            run.Combat.RecomputeStats();
            string mods = string.Join(", ", item.Mods.Select(StatText.Describe));
            run.Announce(item.Name, string.IsNullOrEmpty(mods) ? item.Description : mods);
            ItemGranted?.Invoke(item);
        }

        /// <summary>Opens a chest: pays unless free, then grants a rolled item. False if the player cannot afford it.</summary>
        public bool OpenChest(bool free)
        {
            if (!free)
            {
                if (!Wallet.TrySpend(ChestPrice)) return false;
                PaidChestsOpened++;
            }
            ChestsOpened++;
            Grant(RollItem());
            return true;
        }

        public void NoteShrine(ShrineKind kind)
        {
            ShrinesUsed++;
            ShrineKindsUsed.Add(kind);
        }

        public bool TryBuy(int price) => Wallet.TrySpend(price);
    }
}
