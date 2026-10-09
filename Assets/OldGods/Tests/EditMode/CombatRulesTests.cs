using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OldGods.Rules;

namespace OldGods.Tests.EditMode
{
    static class TestContent
    {
        public static WeaponDef Weapon(string id, WeaponShape shape = WeaponShape.Projectile) => new WeaponDef
        {
            Id = id, Name = id, Description = "", Shape = shape,
            Base = new WeaponStats { Damage = 10f, Cooldown = 1f, Count = 1f, Size = 1f, Speed = 10f, Duration = 1f, Range = 10f, Knockback = 1f },
            UpgradePool = new List<WeaponUpgrade>
            {
                new WeaponUpgrade(WeaponStat.Damage, 4f),
                new WeaponUpgrade(WeaponStat.Cooldown, 0.1f),
                new WeaponUpgrade(WeaponStat.Count, 1f),
            },
        };

        public static PassiveDef Passive(string id, StatId stat = StatId.Damage, float add = 0.1f) =>
            new PassiveDef { Id = id, Name = id, Description = "", PerLevel = new StatMod(stat, add) };

        public static List<WeaponDef> Weapons(int n) => Enumerable.Range(0, n).Select(i => Weapon("w" + i)).ToList();
        public static List<PassiveDef> Passives(int n) => Enumerable.Range(0, n).Select(i => Passive("p" + i)).ToList();
    }

    public class StatTests
    {
        [Test]
        public void AddsThenPercents()
        {
            var b = StatBlock.Default();
            b.Apply(new StatMod(StatId.MaxHealth, 20f));
            b.Apply(new StatMod(StatId.MaxHealth, 0f, 0.5f));
            Assert.AreEqual(180f, b.Value(StatId.MaxHealth), 1e-3f);
            b.Clear();
            Assert.AreEqual(100f, b.Value(StatId.MaxHealth), 1e-3f);
        }

        [Test]
        public void ClampsEvasionAndMultipliers()
        {
            var b = StatBlock.Default();
            b.Apply(new StatMod(StatId.Evasion, 5f));
            Assert.AreEqual(0.75f, b.Value(StatId.Evasion), 1e-4f);
            b.Apply(new StatMod(StatId.MoveSpeed, -10f));
            Assert.AreEqual(0.1f, b.Value(StatId.MoveSpeed), 1e-4f);
        }

        [Test]
        public void DescribesMods()
        {
            Assert.AreEqual("+10% Damage", StatText.Describe(new StatMod(StatId.Damage, 0.1f)));
            Assert.AreEqual("+15 Max Health", StatText.Describe(new StatMod(StatId.MaxHealth, 15f)));
        }
    }

    public class WeaponTests
    {
        [Test]
        public void EffectiveAppliesUpgradesAndPlayerStats()
        {
            var w = new WeaponState(TestContent.Weapon("w"));
            w.AddBonus(WeaponStat.Damage, 5f);
            w.AddBonus(WeaponStat.Cooldown, 0.2f);
            var player = StatBlock.Default();
            player.Apply(new StatMod(StatId.Damage, 0.5f));
            player.Apply(new StatMod(StatId.AttackSpeed, 0.25f));
            player.Apply(new StatMod(StatId.ProjectileCount, 2f));
            var e = w.Effective(player);
            Assert.AreEqual(15f * 1.5f, e.Damage, 1e-3f);
            Assert.AreEqual(1f * 0.8f / 1.25f, e.Cooldown, 1e-3f);
            Assert.AreEqual(3, e.Count);
        }

        [Test]
        public void AurasIgnoreExtraProjectiles()
        {
            var w = new WeaponState(TestContent.Weapon("aura", WeaponShape.Aura));
            var player = StatBlock.Default();
            player.Apply(new StatMod(StatId.ProjectileCount, 3f));
            Assert.AreEqual(1, w.Effective(player).Count);
        }

        [Test]
        public void CooldownNeverDropsBelowFloor()
        {
            var w = new WeaponState(TestContent.Weapon("w"));
            w.AddBonus(WeaponStat.Cooldown, 5f);
            var player = StatBlock.Default();
            player.Apply(new StatMod(StatId.AttackSpeed, 100f));
            Assert.GreaterOrEqual(w.Effective(player).Cooldown, 0.08f);
        }

        [Test]
        public void CritsMultiplyDamage()
        {
            var e = new EffectiveWeapon { Damage = 10f, CritChance = 0.25f, CritMultiplier = 2f };
            Assert.AreEqual(20f, WeaponState.RollDamage(e, 0.1f, out bool crit));
            Assert.IsTrue(crit);
            Assert.AreEqual(10f, WeaponState.RollDamage(e, 0.9f, out crit));
            Assert.IsFalse(crit);
        }
    }

    public class XpTests
    {
        [Test]
        public void CurveRises()
        {
            Assert.AreEqual(5, XpRules.Required(1));
            for (int l = 1; l < 50; l++) Assert.Greater(XpRules.Required(l + 1), XpRules.Required(l));
        }

        [Test]
        public void TrackerCarriesOverflow()
        {
            var t = new XpTracker();
            int gained = t.Add(XpRules.Required(1) + XpRules.Required(2) + 1f);
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, t.Level);
            Assert.AreEqual(1f, t.Xp, 1e-4f);
            Assert.AreEqual(0, t.Add(0f));
        }
    }

    public class DraftTests
    {
        [Test]
        public void DealsThreeDistinctCards()
        {
            var l = new Loadout();
            var weapons = TestContent.Weapons(6);
            var passives = TestContent.Passives(6);
            for (int seed = 0; seed < 50; seed++)
            {
                var cards = DraftRules.Roll(l, weapons, passives, new Rng((ulong)seed), 0f);
                Assert.AreEqual(3, cards.Count);
                Assert.AreEqual(3, cards.Select(c => c.Id).Distinct().Count());
            }
        }

        [Test]
        public void SameSeedSameCards()
        {
            var l = new Loadout();
            var a = DraftRules.Roll(l, TestContent.Weapons(6), TestContent.Passives(6), new Rng(9), 0f);
            var b = DraftRules.Roll(l, TestContent.Weapons(6), TestContent.Passives(6), new Rng(9), 0f);
            CollectionAssert.AreEqual(a.Select(c => c.ToString()), b.Select(c => c.ToString()));
        }

        [Test]
        public void FullSlotsOnlyOfferUpgrades()
        {
            var l = new Loadout { WeaponSlots = 1, PassiveSlots = 1 };
            var weapons = TestContent.Weapons(4);
            var passives = TestContent.Passives(4);
            l.AddWeapon(weapons[0]);
            l.AddPassive(passives[0]);
            for (int seed = 0; seed < 30; seed++)
            {
                var cards = DraftRules.Roll(l, weapons, passives, new Rng((ulong)seed), 0f);
                Assert.IsTrue(cards.All(c => c.Kind == DraftKind.UpgradeWeapon || c.Kind == DraftKind.UpgradePassive));
                Assert.AreEqual(2, cards.Count);
            }
        }

        [Test]
        public void MaxedAndBanishedItemsAreNotOffered()
        {
            var l = new Loadout { WeaponSlots = 1, PassiveSlots = 0, MaxLevel = 2 };
            var weapons = TestContent.Weapons(2);
            l.AddWeapon(weapons[0]);
            l.Weapons[0].Level = 2;
            var cards = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng(1), 0f);
            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual(DraftKind.Restore, cards[0].Kind);

            var l2 = new Loadout();
            var charges = new DraftCharges();
            Assert.IsTrue(DraftRules.Banish(l2, charges, "w1"));
            Assert.IsFalse(DraftRules.Banish(l2, charges, "w0"), "only one banish");
            for (int seed = 0; seed < 30; seed++)
                Assert.IsFalse(DraftRules.Roll(l2, weapons, new List<PassiveDef>(), new Rng((ulong)seed), 0f).Any(c => c.Id == "w1"));
        }

        [Test]
        public void ExcludeKeepsRefreshedCardsDifferent()
        {
            var l = new Loadout();
            var weapons = TestContent.Weapons(6);
            var first = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng(3), 0f);
            var exclude = first.Select(c => c.Id).ToList();
            var second = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng(4), 0f, 3, exclude);
            Assert.IsFalse(second.Any(c => exclude.Contains(c.Id)));
        }

        [Test]
        public void LuckRaisesAverageRarity()
        {
            float Average(float luck)
            {
                var rng = new Rng(77);
                float sum = 0f;
                for (int i = 0; i < 5000; i++) sum += (int)DraftRules.RollRarity(rng, luck);
                return sum / 5000f;
            }
            Assert.Greater(Average(0.5f), Average(0f) + 0.2f);
        }

        [Test]
        public void ApplyLevelsAndRespectsSlots()
        {
            var l = new Loadout { WeaponSlots = 1 };
            var weapons = TestContent.Weapons(2);
            var passives = TestContent.Passives(1);
            Assert.IsTrue(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.NewWeapon, Id = "w0" }, weapons, passives));
            Assert.IsFalse(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.NewWeapon, Id = "w1" }, weapons, passives));
            var up = new DraftOption { Kind = DraftKind.UpgradeWeapon, Id = "w0" };
            up.WeaponRolls.Add(new WeaponUpgrade(WeaponStat.Damage, 4f));
            Assert.IsTrue(DraftRules.Apply(l, up, weapons, passives));
            Assert.AreEqual(2, l.Weapons[0].Level);
            Assert.AreEqual(4f, l.Weapons[0].Bonus(WeaponStat.Damage), 1e-4f);

            Assert.IsTrue(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.NewPassive, Id = "p0" }, weapons, passives));
            Assert.IsTrue(DraftRules.Apply(l, new DraftOption { Kind = DraftKind.UpgradePassive, Id = "p0", PassiveRoll = new StatMod(StatId.Damage, 0.2f) }, weapons, passives));
            var b = StatBlock.Default();
            b.ApplyAll(l.PassiveMods());
            Assert.AreEqual(1f + 0.1f + 0.2f, b.Value(StatId.Damage), 1e-4f);
        }

        [Test]
        public void RarityScalesUpgrades()
        {
            var dmg = new WeaponUpgrade(WeaponStat.Damage, 4f);
            Assert.AreEqual(4f, DraftRules.ScaleUpgrade(dmg, Rarity.Common), 1e-4f);
            Assert.Greater(DraftRules.ScaleUpgrade(dmg, Rarity.Legendary), DraftRules.ScaleUpgrade(dmg, Rarity.Rare));
            var count = new WeaponUpgrade(WeaponStat.Count, 1f);
            Assert.AreEqual(1f, DraftRules.ScaleUpgrade(count, Rarity.Rare));
            Assert.AreEqual(2f, DraftRules.ScaleUpgrade(count, Rarity.Legendary));
        }

        [Test]
        public void EpicUpgradesRollTwoStats()
        {
            var l = new Loadout { WeaponSlots = 1, PassiveSlots = 0 };
            var weapons = TestContent.Weapons(1);
            l.AddWeapon(weapons[0]);
            bool sawEpic = false;
            for (int seed = 0; seed < 400 && !sawEpic; seed++)
            {
                var card = DraftRules.Roll(l, weapons, new List<PassiveDef>(), new Rng((ulong)seed), 2f)[0];
                if (card.Rarity >= Rarity.Epic)
                {
                    sawEpic = true;
                    Assert.AreEqual(2, card.WeaponRolls.Count);
                    Assert.AreNotEqual(card.WeaponRolls[0].Stat, card.WeaponRolls[1].Stat);
                }
                else Assert.AreEqual(1, card.WeaponRolls.Count);
            }
            Assert.IsTrue(sawEpic);
        }
    }
}
